#!/usr/bin/env node

import { existsSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { join, resolve } from "node:path";

const ShaPattern = /^[0-9a-f]{40}$/;
const RefreshCommand = "cd tests/Blazix.BaseUI.Parity.Tests/react-fixtures && pnpm parity:baseline";
// Constant, and deliberately free of SHAs and dates: the workflow deduplicates by exact title,
// and one issue covers both drift and invalid provenance/waiver metadata.
const IssueTitle = "Parity baselines are stale or inconsistent";
const WaiverFields = new Set([
    "fixture", "leg", "step", "nodePath", "kind", "property", "propertyMatch", "reason",
    "disposition", "docLink", "expires"
]);
const WaiverLegs = new Set(["BlazorServer", "BlazorWasm"]);
const FindingKinds = new Set([
    "Structure", "CorrespondenceUncertain", "Attribute", "AriaSnapshot", "ComputedStyle",
    "CustomProperty", "Geometry", "Focus", "Console", "Marker", "Timeline", "Pixel",
    "SelectorUnresolved", "SelectorNonActionable", "ActionCompletionUnmet", "FixtureError"
]);
const IdentityFields = ["fixture", "leg", "step", "nodePath", "kind", "property"];
const RequiredWaiverStrings = ["fixture", "step", "nodePath", "property", "reason", "docLink"];

const repoRoot = resolve(import.meta.dirname, "..");
const options = parseOptions(process.argv.slice(2));
const now = parseNow(options.now);
const baselines = resolve(options.baselines ?? join(repoRoot, "tests/Blazix.BaseUI.Parity.Tests/baselines"));
const waivers = resolve(options.waivers ?? join(repoRoot, "tests/Blazix.BaseUI.Parity.Tests/waivers/waivers.json"));
const upstream = { repository: options.upstreamRepo ?? "mui/base-ui", ref: options.upstreamRef ?? "master" };
const issues = [];
const provenance = validateProvenance(baselines, now, issues);
const waiverCount = validateWaivers(waivers, now, issues);
let observedRevision = null;
let upstreamFailed = false;
let drifted = false;

if (!options.offline) {
    if (options.observedSha) {
        observedRevision = options.observedSha;
    } else {
        try {
            observedRevision = await fetchUpstreamRevision(upstream);
        } catch (error) {
            upstreamFailed = true;
            issues.push({ code: "upstream-unreachable", message: `Could not read the tracked upstream revision: ${error.message}` });
        }
    }
    drifted = observedRevision !== null && observedRevision !== provenance.declaredPin;
}

const hasValidationIssue = issues.some(issue => issue.code === "provenance" || issue.code === "waiver");
const status = hasValidationIssue
    ? "invalid"
    : upstreamFailed
        ? "error"
        : drifted
            ? "drift"
            : "fresh";
const result = {
    status,
    checkedAtUtc: now.toISOString(),
    declaredPin: provenance.declaredPin,
    observedRevision,
    upstream,
    platformSets: provenance.platformSets,
    oldestBaselineAgeDays: provenance.platformSets.length > 0
        ? Math.max(...provenance.platformSets.map(set => set.ageDays).filter(Number.isInteger))
        : null,
    waiverCount,
    issues,
    refreshCommand: RefreshCommand,
    issueTitle: IssueTitle,
    issueBody: ""
};

if (!Number.isFinite(result.oldestBaselineAgeDays))
    result.oldestBaselineAgeDays = null;

result.issueBody = buildIssueBody(result);
const json = `${JSON.stringify(result, null, 2)}\n`;

if (options.output)
    writeFileSync(resolve(options.output), json);

process.stdout.write(json);
process.exitCode = status === "fresh" ? 0 : 1;

function parseOptions(args) {
    const result = {};
    const valueOptions = new Set([
        "--baselines", "--waivers", "--observed-sha", "--upstream-repo", "--upstream-ref",
        "--output", "--now"
    ]);

    for (let index = 0; index < args.length; index++) {
        const argument = args[index];
        if (argument === "--offline") {
            result.offline = true;
            continue;
        }
        if (!valueOptions.has(argument) || !args[index + 1] || args[index + 1].startsWith("--")) {
            console.error(`Unknown or incomplete option: ${argument}`);
            process.exit(1);
        }
        const name = argument.slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
        result[name] = args[++index];
    }
    return result;
}

function parseNow(value) {
    const result = value ? new Date(value) : new Date();
    if (Number.isNaN(result.getTime())) {
        console.error(`Invalid ISO 8601 value for --now: ${value}`);
        process.exit(1);
    }
    return result;
}

function validateProvenance(directory, nowValue, resultIssues) {
    const rootPath = join(directory, "metadata.json");
    const root = readObject(rootPath, "Root baseline metadata", resultIssues);
    let declaredPin = null;

    if (root) {
        if (!Number.isInteger(root.schemaVersion))
            addIssue(resultIssues, "provenance", "Root metadata schemaVersion must be an integer.");
        if (!Number.isInteger(root.captureSchemaVersion))
            addIssue(resultIssues, "provenance", "Root metadata captureSchemaVersion must be an integer.");
        if (typeof root.declaredRepositoryPin !== "string" || !ShaPattern.test(root.declaredRepositoryPin)) {
            addIssue(resultIssues, "provenance", "Root metadata declaredRepositoryPin must be a lowercase 40-character SHA.");
        } else {
            declaredPin = root.declaredRepositoryPin;
        }
    }

    let entries = [];
    try {
        entries = readdirSync(directory, { withFileTypes: true });
    } catch (error) {
        addIssue(resultIssues, "provenance", `Could not enumerate baseline sets: ${error.message}`);
    }

    const setNames = entries
        .filter(entry => entry.isDirectory() && existsSync(join(directory, entry.name, "metadata.json")))
        .map(entry => entry.name)
        .sort();
    if (setNames.length === 0)
        addIssue(resultIssues, "provenance", "At least one platform baseline set with metadata.json is required.");

    const platformSets = [];
    for (const setName of setNames) {
        const metadata = readObject(join(directory, setName, "metadata.json"), `Platform set '${setName}' metadata`, resultIssues);
        if (!metadata)
            continue;

        const upstreamSha = typeof metadata.upstreamSha === "string" ? metadata.upstreamSha : null;
        if (!upstreamSha || !ShaPattern.test(upstreamSha))
            addIssue(resultIssues, "provenance", `Platform set '${setName}' upstreamSha must be a lowercase 40-character SHA.`);
        if (upstreamSha !== declaredPin)
            addIssue(resultIssues, "provenance", `Platform set '${setName}' upstreamSha does not equal declaredRepositoryPin.`);
        if (!root || metadata.schemaVersion !== root.schemaVersion)
            addIssue(resultIssues, "provenance", `Platform set '${setName}' schemaVersion does not equal the root value.`);
        if (!root || metadata.captureSchemaVersion !== root.captureSchemaVersion)
            addIssue(resultIssues, "provenance", `Platform set '${setName}' captureSchemaVersion does not equal the root value.`);

        const generatedAtUtc = typeof metadata.generatedAtUtc === "string" ? metadata.generatedAtUtc : null;
        const generatedTime = generatedAtUtc === null ? Number.NaN : Date.parse(generatedAtUtc);
        if (!Number.isFinite(generatedTime))
            addIssue(resultIssues, "provenance", `Platform set '${setName}' generatedAtUtc must parse as a date.`);

        const platform = isObject(metadata.platform) ? metadata.platform : {};
        const browser = readPlatformString(platform, "browser", setName, resultIssues);
        const os = readPlatformString(platform, "os", setName, resultIssues);
        const architecture = readPlatformString(platform, "architecture", setName, resultIssues);
        if (browser && os && architecture && setName !== `${browser}-${os}-${architecture}`)
            addIssue(resultIssues, "provenance", `Platform set directory '${setName}' must equal '${browser}-${os}-${architecture}'.`);

        platformSets.push({
            set: setName,
            upstreamSha,
            generatedAtUtc,
            ageDays: Number.isFinite(generatedTime)
                ? Math.floor((nowValue.getTime() - generatedTime) / 86_400_000)
                : null
        });
    }

    return { declaredPin, platformSets };
}

function readObject(path, label, resultIssues) {
    if (!existsSync(path)) {
        addIssue(resultIssues, "provenance", `${label} does not exist at ${path}.`);
        return null;
    }
    try {
        const value = JSON.parse(readFileSync(path, "utf8"));
        if (!isObject(value)) {
            addIssue(resultIssues, "provenance", `${label} must be a JSON object.`);
            return null;
        }
        return value;
    } catch (error) {
        addIssue(resultIssues, "provenance", `${label} contains invalid JSON: ${error.message}`);
        return null;
    }
}

function readPlatformString(platform, field, setName, resultIssues) {
    const value = platform[field];
    if (typeof value !== "string" || value.trim().length === 0) {
        addIssue(resultIssues, "provenance", `Platform set '${setName}' platform.${field} must be a non-empty string.`);
        return null;
    }
    return value;
}

// This is intentionally a strict subset mirror of the offline rules in the authoritative
// tests/Blazix.BaseUI.Parity.Tests/Blazix.BaseUI.Parity.Tests/Waivers/WaiverFile.cs loader.
function validateWaivers(path, nowValue, resultIssues) {
    let entries;
    try {
        entries = JSON.parse(readFileSync(path, "utf8"));
    } catch (error) {
        addIssue(resultIssues, "waiver", `Waiver file could not be parsed: ${error.message}`);
        return 0;
    }
    if (!Array.isArray(entries)) {
        addIssue(resultIssues, "waiver", "Waiver file must be a JSON array.");
        return 0;
    }

    const identities = new Set();
    const today = nowValue.toISOString().slice(0, 10);
    entries.forEach((entry, index) => {
        if (!isObject(entry)) {
            addIssue(resultIssues, "waiver", `Waiver entry ${index} must be a JSON object.`);
            return;
        }

        for (const field of Object.keys(entry)) {
            if (!WaiverFields.has(field))
                addIssue(resultIssues, "waiver", `Waiver entry ${index} field '${field}' is unknown.`);
        }
        for (const field of RequiredWaiverStrings) {
            if (typeof entry[field] !== "string" || entry[field].trim().length === 0)
                addIssue(resultIssues, "waiver", `Waiver entry ${index} field '${field}' must be a non-empty string.`);
        }
        if (!WaiverLegs.has(entry.leg))
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'leg' must be BlazorServer or BlazorWasm.`);
        if (!FindingKinds.has(entry.kind))
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'kind' has an invalid FindingKind value.`);
        if (!["accepted-limitation", "deferred-defect"].includes(entry.disposition))
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'disposition' must be accepted-limitation or deferred-defect.`);
        if (Object.hasOwn(entry, "propertyMatch") && !["exact", "prefix"].includes(entry.propertyMatch)) {
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'propertyMatch' must be exact or prefix.`);
        }
        if (!isIsoDate(entry.expires)) {
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'expires' must be an ISO date in YYYY-MM-DD form.`);
        } else if (entry.expires <= today) {
            addIssue(resultIssues, "waiver", `Waiver entry ${index} field 'expires' must be later than ${today}.`);
        }

        if (IdentityFields.every(field => typeof entry[field] === "string")) {
            const identity = JSON.stringify(IdentityFields.map(field => entry[field]));
            if (identities.has(identity))
                addIssue(resultIssues, "waiver", `Waiver entry ${index} duplicates an earlier six-field identity.`);
            else
                identities.add(identity);
        }
    });

    return entries.length;
}

function isIsoDate(value) {
    if (typeof value !== "string" || !/^\d{4}-\d{2}-\d{2}$/.test(value))
        return false;
    const parsed = new Date(`${value}T00:00:00.000Z`);
    return !Number.isNaN(parsed.getTime()) && parsed.toISOString().slice(0, 10) === value;
}

async function fetchUpstreamRevision(target) {
    const repository = target.repository.split("/").map(encodeURIComponent).join("/");
    const url = `https://api.github.com/repos/${repository}/commits/${encodeURIComponent(target.ref)}`;
    const headers = {
        Accept: "application/vnd.github+json",
        "User-Agent": "Blazix.BaseUI-parity-freshness-canary"
    };
    if (process.env.GITHUB_TOKEN?.trim())
        headers.Authorization = `Bearer ${process.env.GITHUB_TOKEN.trim()}`;

    const response = await fetch(url, { headers });
    if (!response.ok)
        throw new Error(`GitHub API returned HTTP ${response.status}.`);

    let payload;
    try {
        payload = await response.json();
    } catch (error) {
        throw new Error(`GitHub API returned invalid JSON: ${error.message}`);
    }
    if (!isObject(payload) || typeof payload.sha !== "string" || !ShaPattern.test(payload.sha))
        throw new Error("GitHub API response did not contain a valid commit SHA.");
    return payload.sha;
}

function buildIssueBody(resultValue) {
    const platformLines = resultValue.platformSets.length === 0
        ? ["_No platform baseline sets were readable._"]
        : [
            "| Platform set | Generated at (UTC) | Age (days) |",
            "| --- | --- | ---: |",
            ...resultValue.platformSets.map(set =>
                `| \`${set.set}\` | ${set.generatedAtUtc ?? "unknown"} | ${set.ageDays ?? "unknown"} |`)
        ];
    const lines = [
        "<!-- parity-freshness-canary -->",
        "# Parity baseline freshness",
        "",
        `- Recorded pin: \`${resultValue.declaredPin ?? "unknown"}\``,
        `- Observed revision: \`${resultValue.observedRevision ?? "not checked"}\``,
        "",
        ...platformLines,
        "",
        "Refresh the committed baselines with:",
        "",
        "```sh",
        resultValue.refreshCommand,
        "```"
    ];

    if (["invalid", "error"].includes(resultValue.status)) {
        lines.push("", "## Validation issues", "");
        for (const issue of resultValue.issues)
            lines.push(`- **${issue.code}**: ${issue.message}`);
    }
    return `${lines.join("\n")}\n`;
}

function isObject(value) {
    return value !== null && typeof value === "object" && !Array.isArray(value);
}

function addIssue(resultIssues, code, message) {
    resultIssues.push({ code, message });
}
