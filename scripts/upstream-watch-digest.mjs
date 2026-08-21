#!/usr/bin/env node

// Monthly upstream watch (issue #156, cadence #149). Diffs the recorded sync pin against upstream
// master and buckets the new commits per component directory, plus the mandatory shared
// popup/positioning/focus bucket required by docs/audits/METHODOLOGY.md.
//
// This reports only. It never renders a sweep-or-skip verdict and never fails on a non-empty
// delta — the verdict is a human call at each monthly evaluation.

import { existsSync, readFileSync, writeFileSync } from "node:fs";
import { execFileSync } from "node:child_process";
import { resolve, join } from "node:path";

const ShaPattern = /^[0-9a-f]{40}$/;
const ReactSourceRoot = "packages/react/src/";
// The shared popup/positioning/focus layer. floating-ui-react holds FloatingFocusManager and the
// positioning/dismissal primitives; utils holds the popups helpers, usePositioner, FocusGuard and
// InternalBackdrop; internals holds composite plus useAnchorPositioning and useOpenChangeComplete.
// Deliberately whole directories rather than a file list: the failure this bucket exists to prevent
// is a *missed* shared fix (the first-pass Popover audit missed four), so it over-includes on
// purpose.
const SharedDirectories = new Set(["floating-ui-react", "utils", "internals"]);
const SharedBucket = "shared";
const RootBucket = "(repository root)";
const NoFilesBucket = "(no files)";
// GitHub rejects issue bodies over 65,536 characters, and a three-month gap already renders past
// that (435 commits measured at ~66k). So the body is rendered at the largest title budget that
// fits, degrading rather than failing to post. Every elision is stated with the compare link, and
// the JSON output always carries every commit regardless of what the body could show.
const IssueBodyLimit = 60_000;
const TitleBudgets = [40, 20, 10, 5, 0];
// Buckets outside packages/react/src/ are reported for completeness, not for sweeping, so they
// never consume the budget that the shared and component buckets need.
const OtherBucketTitles = 5;

const repoRoot = resolve(import.meta.dirname, "..");
const options = parseOptions(process.argv.slice(2));
const now = parseNow(options.now);
const pinFile = resolve(options.pinFile ?? join(repoRoot, "docs/upstream-pin.json"));
const pinRecord = readPinRecord(pinFile);
const pin = options.pin ?? pinRecord.pin;
const upstreamRepository = options.upstreamRepo ?? pinRecord.upstreamRepository;
const upstreamRef = options.ref ?? `origin/${pinRecord.upstreamRef}`;
const repository = options.repository ?? "JakeMurrayDev/Blazix.BaseUI";
if (!options.upstream)
    fail("--upstream <git-directory> is required.");
const upstream = resolve(options.upstream);

if (!ShaPattern.test(pin))
    fail(`Pin must be a lowercase 40-character SHA, got '${pin}'.`);
if (!existsSync(upstream))
    fail(`Upstream clone does not exist at ${upstream}.`);

const head = git(["rev-parse", upstreamRef]);
if (!ShaPattern.test(head))
    fail(`'${upstreamRef}' did not resolve to a commit SHA in ${upstream}.`);
// A pin that is not an ancestor of the ref means the clone lacks the pin or upstream rewrote
// history. Either way the range would be meaningless, so refuse rather than digest a bogus delta.
if (!isAncestor(pin, head))
    fail(`Pin ${pin} is not an ancestor of ${upstreamRef} (${head}) in ${upstream}.`);

const commits = readCommits(`${pin}..${head}`);
const buckets = bucketCommits(commits);
const result = {
    generatedAtUtc: now.toISOString(),
    period: now.toISOString().slice(0, 7),
    pin,
    pinCommittedOn: pinRecord.pinCommittedOn,
    cycle: pinRecord.cycle,
    cycleIssue: pinRecord.cycleIssue,
    upstreamRepository,
    upstreamRef,
    head,
    commitCount: commits.length,
    compareUrl: `https://github.com/${upstreamRepository}/compare/${pin}...${head}`,
    repository,
    buckets,
    issueTitle: `Upstream watch digest — ${now.toISOString().slice(0, 7)}`,
    titleBudget: TitleBudgets[0],
    issueBody: ""
};

for (const budget of TitleBudgets) {
    result.titleBudget = budget;
    result.issueBody = buildIssueBody(result, budget);
    if (result.issueBody.length <= IssueBodyLimit)
        break;
}
const json = `${JSON.stringify(result, null, 2)}\n`;

if (options.output)
    writeFileSync(resolve(options.output), json);
if (options.issueBody)
    writeFileSync(resolve(options.issueBody), result.issueBody);
if (options.issueTitle)
    writeFileSync(resolve(options.issueTitle), result.issueTitle);
if (!options.quiet)
    process.stdout.write(json);

function parseOptions(args) {
    const parsed = {};
    const valueOptions = new Set([
        "--upstream", "--pin", "--pin-file", "--ref", "--upstream-repo", "--repository", "--output",
        "--issue-body", "--issue-title", "--now"
    ]);

    for (let index = 0; index < args.length; index++) {
        const argument = args[index];
        if (argument === "--quiet") {
            parsed.quiet = true;
            continue;
        }
        if (!valueOptions.has(argument) || !args[index + 1] || args[index + 1].startsWith("--"))
            fail(`Unknown or incomplete option: ${argument}`);
        const name = argument.slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
        parsed[name] = args[++index];
    }
    return parsed;
}

function parseNow(value) {
    const parsed = value ? new Date(value) : new Date();
    if (Number.isNaN(parsed.getTime()))
        fail(`Invalid ISO 8601 value for --now: ${value}`);
    return parsed;
}

function readPinRecord(path) {
    let record;
    try {
        record = JSON.parse(readFileSync(path, "utf8"));
    } catch (error) {
        fail(`Pin file ${path} could not be read: ${error.message}`);
    }
    if (record === null || typeof record !== "object" || Array.isArray(record))
        fail(`Pin file ${path} must contain a JSON object.`);
    for (const field of ["pin", "upstreamRepository", "upstreamRef"]) {
        if (typeof record[field] !== "string" || record[field].trim().length === 0)
            fail(`Pin file ${path} field '${field}' must be a non-empty string.`);
    }
    return record;
}

// core.quotePath=false keeps a non-ASCII path from arriving as an escaped, double-quoted string,
// which would bucket it under a leading quote character.
function git(args) {
    const argv = ["-C", upstream, "-c", "core.quotePath=false", ...args];
    return execFileSync("git", argv, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 }).trim();
}

function isAncestor(ancestor, descendant) {
    try {
        execFileSync("git", ["-C", upstream, "merge-base", "--is-ancestor", ancestor, descendant], { stdio: "ignore" });
        return true;
    } catch {
        return false;
    }
}

// %x00 separates commit records and %x1f separates fields, so neither a subject containing a
// newline nor a path containing spaces can be misread as a record boundary.
function readCommits(range) {
    const output = git(["log", "--name-only", "--format=%x00%H%x1f%s", range]);
    return output
        .split("\0")
        .slice(1)
        .map(record => {
            const [header, ...pathLines] = record.split("\n");
            const [sha, title] = header.split("\x1f");
            return {
                sha,
                title,
                paths: pathLines.map(line => line.trim()).filter(line => line.length > 0)
            };
        });
}

function bucketFor(path) {
    if (path.startsWith(ReactSourceRoot)) {
        const rest = path.slice(ReactSourceRoot.length);
        const separator = rest.indexOf("/");
        if (separator < 0)
            return { name: ReactSourceRoot, kind: "other" };
        const directory = rest.slice(0, separator);
        return SharedDirectories.has(directory)
            ? { name: SharedBucket, kind: "shared" }
            : { name: directory, kind: "component" };
    }
    // A two-segment path is a file inside a top-level directory ("docs/package.json"), so only
    // the first segment names the bucket; deeper paths keep two ("docs/src/").
    const segments = path.split("/");
    const depth = Math.min(segments.length - 1, 2);
    return {
        name: depth === 0 ? RootBucket : `${segments.slice(0, depth).join("/")}/`,
        kind: "other"
    };
}

// A commit lands in every bucket it touches, so bucket counts sum above the distinct commit total.
// That mirrors the rubric's one-disposition-row-per-(commit, component) granularity.
function bucketCommits(commitList) {
    const byName = new Map();

    for (const commit of commitList) {
        const targets = commit.paths.length === 0
            ? [{ name: NoFilesBucket, kind: "other" }]
            : commit.paths.map(bucketFor);
        const seen = new Set();
        for (const target of targets) {
            if (seen.has(target.name))
                continue;
            seen.add(target.name);
            if (!byName.has(target.name))
                byName.set(target.name, { bucket: target.name, kind: target.kind, commits: [] });
            byName.get(target.name).commits.push({ sha: commit.sha, title: commit.title });
        }
    }

    const rank = { shared: 0, component: 1, other: 2 };
    return [...byName.values()]
        .map(entry => ({ ...entry, commitCount: entry.commits.length }))
        .sort((left, right) =>
            rank[left.kind] - rank[right.kind]
            || right.commitCount - left.commitCount
            || left.bucket.localeCompare(right.bucket));
}

function buildIssueBody(digest, budget) {
    const shortPin = digest.pin.slice(0, 9);
    const shortHead = digest.head.slice(0, 9);
    const cycleReference = Number.isInteger(digest.cycle)
        ? `cycle ${digest.cycle}, #${digest.cycleIssue}`
        : "unrecorded cycle";
    const lines = [
        "<!-- upstream-watch-digest -->",
        `# Upstream watch digest — ${digest.period}`,
        "",
        "Report only. The sweep-or-skip verdict for this evaluation is a human call —",
        `see [docs/upstream-sync-strategy.md](${blob(digest, "docs/upstream-sync-strategy.md")}).`,
        "",
        `- Pin (${cycleReference}): \`${digest.pin}\`${digest.pinCommittedOn ? ` (${digest.pinCommittedOn})` : ""}`,
        `- Upstream \`${digest.upstreamRepository}\` \`${digest.upstreamRef}\`: \`${digest.head}\``,
        `- New commits: **${digest.commitCount}** — [compare \`${shortPin}…${shortHead}\`](${digest.compareUrl})`,
        `- Generated: ${digest.generatedAtUtc}`,
        ""
    ];

    if (digest.commitCount === 0) {
        lines.push(
            "Upstream has not moved since the pin. A zero delta is itself a decision input:",
            "the evaluation still happens, and a skip month creates no tickets.",
            ""
        );
        return `${lines.join("\n")}\n`;
    }

    lines.push(
        "## Buckets",
        "",
        "| Bucket | Commits |",
        "| --- | ---: |",
        ...digest.buckets.map(entry => `| ${bucketLabel(entry)} | ${entry.commitCount} |`),
        "",
        `A commit lands in every bucket it touches, so bucket counts sum above the distinct total of ${digest.commitCount}.`,
        "That matches the rubric's one-disposition-row-per-(commit, component) granularity in",
        `[docs/audits/METHODOLOGY.md](${blob(digest, "docs/audits/METHODOLOGY.md")}).`,
        "",
        "## Commits per bucket",
        ""
    );

    let dividerEmitted = false;
    for (const entry of digest.buckets) {
        if (entry.kind === "other" && !dividerEmitted) {
            dividerEmitted = true;
            lines.push("## Outside `packages/react/src/`", "",
                "Reported for completeness; these buckets do not carry port behavior.", "");
        }
        lines.push(`### ${bucketLabel(entry)} — ${entry.commitCount} commit${entry.commitCount === 1 ? "" : "s"}`, "");
        if (entry.kind === "shared") {
            lines.push(
                "Mandatory bucket. Covers `packages/react/src/{floating-ui-react,utils,internals}/` —",
                "FloatingFocusManager, the popups utilities, positioning and composite. Sweep this layer",
                "first; the first-pass Popover audit missed four shared fixes by skipping it.",
                ""
            );
        }
        const limit = entry.kind === "other" ? Math.min(budget, OtherBucketTitles) : budget;
        const shown = entry.commits.slice(0, limit);
        lines.push(...shown.map(commit => `- \`${commit.sha.slice(0, 9)}\` ${commit.title}`));
        if (entry.commits.length > shown.length) {
            lines.push(`- _…and ${entry.commits.length - shown.length} more in this bucket; see the compare link above._`);
        }
        lines.push("");
    }

    return `${lines.join("\n")}\n`;
}

function blob(digest, path) {
    return `https://github.com/${digest.repository}/blob/master/${path}`;
}

function bucketLabel(entry) {
    if (entry.kind === "shared")
        return "**shared** (popup / positioning / focus)";
    return `\`${entry.bucket}\``;
}

function fail(message) {
    console.error(message);
    process.exit(1);
}
