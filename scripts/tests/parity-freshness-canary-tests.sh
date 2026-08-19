#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT

pin="1111111111111111111111111111111111111111"
other_pin="2222222222222222222222222222222222222222"
now="2026-02-01T00:00:00.000Z"
output="$fixture/output.json"

write_fixture() {
    local set_name="${1:-chromium-linux-x64}"
    local platform_pin="${2:-$pin}"

    rm -rf "$fixture/baselines"
    mkdir -p "$fixture/baselines/$set_name"
    printf '{"schemaVersion":3,"captureSchemaVersion":3,"declaredRepositoryPin":"%s"}\n' \
        "$pin" > "$fixture/baselines/metadata.json"
    printf '%s\n' \
        "{\"schemaVersion\":3,\"captureSchemaVersion\":3,\"upstreamSha\":\"$platform_pin\",\"platform\":{\"browser\":\"chromium\",\"browserVersion\":\"1\",\"os\":\"linux\",\"architecture\":\"x64\"},\"generatedAtUtc\":\"2026-01-01T00:00:00.000Z\",\"fixtureManifestHash\":\"fixture\",\"aliasManifestHash\":\"alias\",\"stylesheetHash\":\"style\",\"fixtures\":[]}" \
        > "$fixture/baselines/$set_name/metadata.json"
    printf '[]\n' > "$fixture/waivers.json"
}

canary() {
    node "$repo_root/scripts/parity-freshness-canary.mjs" \
        --baselines "$fixture/baselines" \
        --waivers "$fixture/waivers.json" \
        --now "$now" \
        "$@"
}

assert_json() {
    local assertion="$1"
    node -e "const fs = require('node:fs'); const result = JSON.parse(fs.readFileSync(process.argv[1], 'utf8')); $assertion" "$output"
}

expect_failure() {
    if canary "$@" > "$output" 2> "$fixture/stderr.txt"; then
        echo "Expected canary fixture to fail: $*" >&2
        exit 1
    else
        local status=$?
        if [[ "$status" -ne 1 ]]; then
            echo "Expected canary fixture to exit 1, got $status: $*" >&2
            exit 1
        fi
    fi
}

write_valid_waiver() {
    local extra="${1:-}"
    local expires="${2:-2026-03-01}"
    printf '%s\n' \
        "[{\"fixture\":\"switch/hero@light\",\"leg\":\"BlazorServer\",\"step\":\"initial\",\"nodePath\":\"main/button\",\"kind\":\"Attribute\",\"property\":\"aria-checked\",\"reason\":\"Fixture reason\",\"disposition\":\"accepted-limitation\",\"docLink\":\"docs/audits/parity-limitations.md\",\"expires\":\"$expires\"$extra}]" \
        > "$fixture/waivers.json"
}

write_fixture
canary --observed-sha "$pin" > "$output"
assert_json "if (result.status !== 'fresh') throw new Error('expected fresh status');"

write_fixture
expect_failure --observed-sha "$other_pin"
assert_json "
    if (result.status !== 'drift') throw new Error('expected drift status');
    if (!result.issueBody.startsWith('<!-- parity-freshness-canary -->')) throw new Error('missing marker');
    for (const value of ['$pin', '$other_pin', result.refreshCommand]) {
        if (!result.issueBody.includes(value)) throw new Error('missing drift detail');
    }
    if (/[0-9a-f]{40}/.test(result.issueTitle)) throw new Error('title contains a SHA');
"

write_fixture "chromium-linux-x64" "$other_pin"
expect_failure --observed-sha "$pin"
assert_json "
    if (result.status !== 'invalid') throw new Error('expected invalid provenance');
    if (!result.issues.some(issue => issue.code === 'provenance')) throw new Error('missing provenance issue');
"

write_fixture "wrong-platform-name"
expect_failure --observed-sha "$pin"
assert_json "
    if (result.status !== 'invalid') throw new Error('expected invalid directory status');
    if (!result.issues.some(issue => issue.code === 'provenance')) throw new Error('missing directory provenance issue');
"

write_fixture
write_valid_waiver ',"unexpected":true'
expect_failure --observed-sha "$pin"
assert_json "
    if (result.status !== 'invalid') throw new Error('expected invalid unknown field status');
    if (!result.issues.some(issue => issue.code === 'waiver' && issue.message.includes('unknown'))) throw new Error('missing unknown-field waiver issue');
"

write_fixture
write_valid_waiver '' "2026-01-31"
expect_failure --observed-sha "$pin"
assert_json "
    if (result.status !== 'invalid') throw new Error('expected invalid expiry status');
    if (!result.issues.some(issue => issue.code === 'waiver' && issue.message.includes('later than'))) throw new Error('missing expiry waiver issue');
"

write_fixture
write_valid_waiver
node -e "
    const fs = require('node:fs');
    const path = process.argv[1];
    const entries = JSON.parse(fs.readFileSync(path, 'utf8'));
    entries.push({ ...entries[0], reason: 'A separately worded duplicate' });
    fs.writeFileSync(path, JSON.stringify(entries));
" "$fixture/waivers.json"
expect_failure --observed-sha "$pin"
assert_json "
    if (result.status !== 'invalid') throw new Error('expected invalid duplicate status');
    if (!result.issues.some(issue => issue.code === 'waiver' && issue.message.includes('six-field identity'))) throw new Error('missing duplicate waiver issue');
"

write_fixture
canary --offline --upstream-repo "owner-that-does-not-exist/repository-that-does-not-exist" > "$output"
assert_json "
    if (result.status !== 'fresh') throw new Error('expected fresh offline status');
    if (result.observedRevision !== null) throw new Error('offline mode observed a revision');
"

node "$repo_root/scripts/parity-freshness-canary.mjs" --offline > "$output"
assert_json "if (result.status !== 'fresh') throw new Error('expected real repository tree to be valid');"

echo "Validated parity freshness, drift, provenance, waiver, offline, and repository fixtures."
