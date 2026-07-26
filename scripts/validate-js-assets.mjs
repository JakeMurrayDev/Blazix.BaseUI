#!/usr/bin/env node

import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { extname, join, relative, resolve } from "node:path";
import { spawnSync } from "node:child_process";

const repoRoot = resolve(import.meta.dirname, "..");
const options = parseOptions(process.argv.slice(2));
const wwwroot = resolve(options.wwwroot ?? join(repoRoot, "src/Blazix.BaseUI/wwwroot"));
const sourceRoot = resolve(options.sourceRoot ?? join(repoRoot, "src/Blazix.BaseUI"));
const interopSetup = resolve(options.interopSetup ?? join(repoRoot, "tests/Blazix.BaseUI.Tests/Infrastructure/JsInteropSetup.cs"));
const testRoot = resolve(options.testRoot ?? join(repoRoot, "tests/Blazix.BaseUI.Tests"));
const terser = resolve(options.terser ?? join(repoRoot, ".base-ui/node_modules/.bin/terser"));
const terserInvocation = existsSync(terser)
    ? { command: terser, arguments: [] }
    : {
        command: process.platform === "win32" ? "npx.cmd" : "npx",
        arguments: ["--yes", "terser@5.47.1"]
    };
const errors = [];

const files = readdirSync(wwwroot).filter(file => /^blazix-baseui-.*\.js$/.test(file));
const sources = files.filter(file => !file.endsWith(".min.js"));
const minified = files.filter(file => file.endsWith(".min.js"));

for (const source of sources) {
    const min = source.replace(/\.js$/, ".min.js");
    if (!minified.includes(min)) {
        errors.push(`Missing minified pair for ${source}`);
        continue;
    }

    checkSyntax(join(wwwroot, source));
    checkSyntax(join(wwwroot, min));
    checkExports(source, min);
    if (!options.skipMinifyCheck)
        checkMinification(source, min);
}

for (const min of minified) {
    const source = min.replace(/\.min\.js$/, ".js");
    if (!sources.includes(source))
        errors.push(`Orphaned minified module ${min}`);
}

const runtimeImports = checkRuntimeImports();
checkTestRegistrations();
checkTestRuntimeRegistrations(runtimeImports);

if (errors.length > 0) {
    for (const error of errors)
        console.error(`JS-ASSET: ${error}`);
    process.exit(1);
}

console.log(`Validated ${sources.length} JavaScript source/minified pairs, runtime imports, exports, and test registrations.`);

function parseOptions(args) {
    const result = {};
    for (let index = 0; index < args.length; index++) {
        const argument = args[index];
        if (argument === "--skip-minify-check") {
            result.skipMinifyCheck = true;
            continue;
        }
        if (!["--wwwroot", "--source-root", "--interop-setup", "--test-root", "--terser"].includes(argument) || !args[index + 1]) {
            console.error(`Unknown or incomplete option: ${argument}`);
            process.exit(2);
        }
        const name = argument.slice(2).replace(/-([a-z])/g, (_, letter) => letter.toUpperCase());
        result[name] = args[++index];
    }
    return result;
}

function checkSyntax(path) {
    const result = spawnSync(process.execPath, ["--check", path], { encoding: "utf8" });
    if (result.status !== 0)
        errors.push(`Invalid JavaScript syntax in ${relative(repoRoot, path)}: ${getSpawnFailure(result)}`);
}

function checkMinification(source, min) {
    const result = spawnSync(
        terserInvocation.command,
        [...terserInvocation.arguments, join(wwwroot, source), "--compress", "--mangle", "--module"],
        { encoding: "utf8" });
    if (result.status !== 0) {
        errors.push(`Terser failed for ${source}: ${getSpawnFailure(result)}`);
        return;
    }
    if (readFileSync(join(wwwroot, min), "utf8") !== result.stdout.trimEnd())
        errors.push(`Minified module is out of date: ${min}`);
}

function getSpawnFailure(result) {
    const detail = [result.stderr, result.stdout, result.error?.message]
        .find(value => typeof value === "string" && value.trim().length > 0);
    return detail?.trim() ?? `process exited with status ${result.status ?? "unknown"}`;
}

function checkExports(source, min) {
    const sourceExports = exportedNames(readFileSync(join(wwwroot, source), "utf8"));
    const minExports = exportedNames(readFileSync(join(wwwroot, min), "utf8"));
    if (sourceExports.join(",") !== minExports.join(","))
        errors.push(`Exports do not match for ${source} and ${min}: [${sourceExports}] vs [${minExports}]`);
}

function exportedNames(code) {
    const names = new Set();
    for (const match of code.matchAll(/export\s+(?:async\s+)?(?:function|class|const|let|var)\s+([A-Za-z_$][\w$]*)/g))
        names.add(match[1]);
    for (const match of code.matchAll(/export\s*\{([^}]+)\}/g)) {
        for (const part of match[1].split(",")) {
            const exported = part.trim().split(/\s+as\s+/).pop();
            if (exported)
                names.add(exported.trim());
        }
    }
    return [...names].sort();
}

function checkRuntimeImports() {
    const imports = new Set();
    for (const path of walk(sourceRoot)) {
        if (![".cs", ".razor"].includes(extname(path)))
            continue;
        const content = readFileSync(path, "utf8");
        for (const match of content.matchAll(/_content\/Blazix\.BaseUI\/(blazix-baseui-[a-z0-9-]+(?:\.min)?\.js)/g)) {
            imports.add(match[1]);
            if (!files.includes(match[1]))
                errors.push(`Runtime import in ${relative(repoRoot, path)} references missing ${match[1]}`);
        }
    }
    return imports;
}

function checkTestRegistrations() {
    const content = readFileSync(interopSetup, "utf8");
    const registered = new Set([...content.matchAll(/blazix-baseui-[a-z0-9-]+(?:\.min)?\.js/g)].map(match => match[0]));
    for (const file of registered) {
        const pair = file.endsWith(".min.js") ? file.replace(/\.min\.js$/, ".js") : file.replace(/\.js$/, ".min.js");
        if (!registered.has(pair))
            errors.push(`Test registration ${file} is missing its ${pair} counterpart in ${relative(repoRoot, interopSetup)}`);
    }
}

function checkTestRuntimeRegistrations(runtimeImports) {
    for (const path of walk(testRoot)) {
        if (extname(path) !== ".cs")
            continue;
        const content = readFileSync(path, "utf8");
        const registrations = new Set([...content.matchAll(/_content\/Blazix\.BaseUI\/(blazix-baseui-[a-z0-9-]+(?:\.min)?\.js)/g)].map(match => match[1]));
        for (const registration of registrations) {
            if (runtimeImports.has(registration))
                continue;
            const pair = registration.endsWith(".min.js")
                ? registration.replace(/\.min\.js$/, ".js")
                : registration.replace(/\.js$/, ".min.js");
            if (!registrations.has(pair) && runtimeImports.has(pair))
                errors.push(`Test registration in ${relative(repoRoot, path)} uses ${registration}, but runtime code imports ${pair}`);
        }
    }
}

function walk(directory) {
    const result = [];
    for (const entry of readdirSync(directory)) {
        if (["bin", "obj"].includes(entry))
            continue;
        const path = join(directory, entry);
        if (statSync(path).isDirectory())
            result.push(...walk(path));
        else
            result.push(path);
    }
    return result;
}
