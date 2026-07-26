#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT

mkdir -p "$fixture/wwwroot" "$fixture/src"
source_file="$fixture/wwwroot/blazix-baseui-fixture.js"
min_file="$fixture/wwwroot/blazix-baseui-fixture.min.js"
setup_file="$fixture/JsInteropSetup.cs"
terser="$repo_root/.base-ui/node_modules/.bin/terser"

printf '%s\n' 'export function initialize(value) { return value + 1; }' > "$source_file"
"$terser" "$source_file" --compress --mangle --module --output "$min_file"
printf '%s\n' 'const string Source = "blazix-baseui-fixture.js";' 'const string Min = "blazix-baseui-fixture.min.js";' > "$setup_file"
printf '%s\n' 'const string Module = "_content/Blazix.BaseUI/blazix-baseui-fixture.min.js";' > "$fixture/src/Fixture.razor"

validate=(node "$repo_root/scripts/validate-js-assets.mjs" --wwwroot "$fixture/wwwroot" --source-root "$fixture/src" --interop-setup "$setup_file" --test-root "$fixture" --terser "$terser")
"${validate[@]}" >/dev/null

printf '%s\n' 'export function initialize(value) { return value + 2; }' > "$source_file"
if "${validate[@]}" >"$fixture/output.txt" 2>&1; then
    echo "Expected stale minification fixture to fail" >&2
    exit 1
fi
grep -q "Minified module is out of date" "$fixture/output.txt"

"$terser" "$source_file" --compress --mangle --module --output "$min_file"
printf '%s\n' 'const string Source = "blazix-baseui-fixture.js";' > "$setup_file"
if "${validate[@]}" >"$fixture/output.txt" 2>&1; then
    echo "Expected unpaired test registration fixture to fail" >&2
    exit 1
fi
grep -q "missing its blazix-baseui-fixture.min.js counterpart" "$fixture/output.txt"

printf '%s\n' 'const string Source = "blazix-baseui-fixture.js";' 'const string Min = "blazix-baseui-fixture.min.js";' > "$setup_file"
printf '%s\n' 'export function different() {}' > "$min_file"
if "${validate[@]}" --skip-minify-check >"$fixture/output.txt" 2>&1; then
    echo "Expected export mismatch fixture to fail" >&2
    exit 1
fi
grep -q "Exports do not match" "$fixture/output.txt"

"$terser" "$source_file" --compress --mangle --module --output "$min_file"
printf '%s\n' 'const string Source = "blazix-baseui-fixture.js";' 'const string Min = "blazix-baseui-fixture.min.js";' > "$setup_file"
printf '%s\n' 'const string Module = "_content/Blazix.BaseUI/blazix-baseui-fixture.js";' > "$fixture/StaleTest.cs"
if "${validate[@]}" >"$fixture/output.txt" 2>&1; then
    echo "Expected stale test runtime registration fixture to fail" >&2
    exit 1
fi
grep -q "runtime code imports blazix-baseui-fixture.min.js" "$fixture/output.txt"

echo "Validated JavaScript asset success, drift, registration, runtime-path, and export fixtures."
