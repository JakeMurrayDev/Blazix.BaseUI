#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/.." && pwd)"
wwwroot="$repo_root/src/Blazix.BaseUI/wwwroot"
terser="$repo_root/.base-ui/node_modules/.bin/terser"
check_only=false

if [[ ${1:-} == "--check" ]]; then
    check_only=true
    shift
fi

if [[ $# -eq 0 ]]; then
    echo "Usage: scripts/minify-js.sh [--check] <component-name|source.js> [...]" >&2
    exit 2
fi

if [[ ! -x "$terser" ]]; then
    echo "Vendored Terser was not found at $terser" >&2
    exit 1
fi

for input in "$@"; do
    if [[ "$input" == *.js ]]; then
        source_file="$input"
        if [[ "$source_file" != /* ]]; then
            source_file="$repo_root/$source_file"
        fi
    else
        source_file="$wwwroot/blazix-baseui-$input.js"
    fi

    if [[ "$source_file" == *.min.js ]]; then
        echo "Pass the readable source module, not a minified file: $source_file" >&2
        exit 2
    fi

    if [[ ! -f "$source_file" ]]; then
        echo "JavaScript source module was not found: $source_file" >&2
        exit 1
    fi

    minified_file="${source_file%.js}.min.js"

    if [[ "$check_only" == true ]]; then
        if [[ ! -f "$minified_file" ]]; then
            echo "Minified module was not found: $minified_file" >&2
            exit 1
        fi

        generated_minified="$("$terser" "$source_file" --compress --mangle --module)"
        if ! cmp -s "$minified_file" <(printf "%s" "$generated_minified"); then
            echo "Minified module is out of date: $minified_file" >&2
            exit 1
        fi
    else
        "$terser" "$source_file" --compress --mangle --module --output "$minified_file"
    fi

    node --check "$source_file"
    node --check "$minified_file"
    echo "Verified ${minified_file#"$repo_root/"}"
done
