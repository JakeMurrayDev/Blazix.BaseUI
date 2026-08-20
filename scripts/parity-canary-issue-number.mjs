#!/usr/bin/env node

import { readFileSync } from "node:fs";

const [issuesPath, title] = process.argv.slice(2);

if (!issuesPath || title === undefined) {
    console.error("Usage: node scripts/parity-canary-issue-number.mjs <issues-json-path> <title>");
    process.exit(1);
}

let issues;
try {
    issues = JSON.parse(readFileSync(issuesPath, "utf8"));
} catch (error) {
    console.error(`Could not read issues JSON: ${error.message}`);
    process.exit(1);
}

if (!Array.isArray(issues)) {
    console.error("Issues JSON must be an array.");
    process.exit(1);
}

process.stdout.write(String(issues.find(issue => issue?.title === title)?.number ?? ""));
