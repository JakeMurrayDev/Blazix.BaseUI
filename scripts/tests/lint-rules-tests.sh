#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT

run_failure_case() {
    local rule="$1" expected="$2"
    if bash "$repo_root/scripts/lint-rules.sh" --source "$fixture" --rule "$rule" >"$fixture/output.txt" 2>&1; then
        echo "Expected RULE-$rule fixture to fail" >&2
        exit 1
    fi
    grep -q "$expected" "$fixture/output.txt"
}

mkdir -p "$fixture/Stub"
printf '%s\n' '@code { }' > "$fixture/Stub/Widget.razor"
printf '%s\n' 'namespace Fixture;' 'public partial class Widget' '{' '    public void Fail() { }' '}' > "$fixture/Stub/Widget.cs"
run_failure_case 1 "RULE-01"

printf '%s\n' '// ----- invalid partition' > "$fixture/Partition.cs"
run_failure_case 4 "RULE-04"

printf '%s\n' 'const marker = "data-base-ui-open";' > "$fixture/Data.js"
run_failure_case 5 "RULE-05"

printf '%s\n' 'attrs["data-open"] = "";' > "$fixture/Empty.cs"
run_failure_case 6 "RULE-06"

rm -rf "$fixture/Stub" "$fixture/Partition.cs" "$fixture/Data.js" "$fixture/Empty.cs"
printf '%s\n' 'public sealed class Valid;' > "$fixture/Valid.cs"
bash "$repo_root/scripts/lint-rules.sh" --source "$fixture" >"$fixture/output.txt"
grep -q "dotnet build' (R02, R03, R07, R09-R19)" "$fixture/output.txt"

echo "Validated positive and negative fixtures for textual lint rules 01, 04, 05, and 06."
