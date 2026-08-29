#!/usr/bin/env bash

set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
fixture="$(mktemp -d)"
trap 'rm -rf "$fixture"' EXIT

upstream="$fixture/upstream"
output="$fixture/digest.json"
now="2026-09-01T06:22:00.000Z"

export GIT_AUTHOR_NAME="Fixture" GIT_AUTHOR_EMAIL="fixture@example.invalid"
export GIT_COMMITTER_NAME="Fixture" GIT_COMMITTER_EMAIL="fixture@example.invalid"
export GIT_AUTHOR_DATE="2026-08-10T00:00:00Z" GIT_COMMITTER_DATE="2026-08-10T00:00:00Z"

commit() {
    local subject="$1"
    shift
    for path in "$@"; do
        mkdir -p "$upstream/$(dirname "$path")"
        printf '%s\n' "$subject" >> "$upstream/$path"
    done
    git -C "$upstream" add -A
    git -C "$upstream" commit -q -m "$subject"
}

git init -q -b master "$upstream"
commit "Pin commit" "packages/react/src/menu/MenuRoot.tsx"
pin="$(git -C "$upstream" rev-parse HEAD)"

commit "[popups] Fix disabled anchor tracking" "packages/react/src/utils/popups/store.ts"
commit "[floating] Fix FloatingFocusManager return focus" "packages/react/src/floating-ui-react/components/FloatingFocusManager.tsx"
commit "[composite] Fix roving focus" "packages/react/src/internals/composite/list/CompositeList.tsx"
commit "[menu] Fix submenu hover" "packages/react/src/menu/submenu/SubmenuTrigger.tsx"
# Straddles a component directory and the shared layer, so it must appear in both buckets.
commit "[menu][popups] Share the dismissal listener" \
    "packages/react/src/menu/MenuPopup.tsx" "packages/react/src/utils/popups/dismiss.ts"
commit "[docs] Rewrite the menu page" "docs/src/app/menu/page.mdx"
# Without -z, git C-quotes any pathname holding a non-ASCII or control character, which would
# bucket it under the leading quote instead of its directory.
commit "[combobox] Fix accented filtering" "packages/react/src/combobox/utils/café.ts"
commit "[tabs] Fix a pathname holding a newline" "packages/react/src/tabs/we
ird.ts"
# A record separator inside a pathname split one commit into two records under the old in-band
# delimiter, inflating the commit count and inventing a commit id out of the pathname remainder.
commit "[select] Fix a pathname holding a record separator" "packages/react/src/select/sep"$'\036'"arator.ts"
# A two-segment path: the bucket is the top-level directory, not the file.
commit "Bump docs dependencies" "docs/package.json"
commit "Bump the lockfile" "pnpm-lock.yaml"

digest() {
    node "$repo_root/scripts/upstream-watch-digest.mjs" \
        --upstream "$upstream" \
        --ref master \
        --pin "$pin" \
        --now "$now" \
        --quiet \
        --output "$output" \
        "$@"
}

assert_json() {
    local assertion="$1"
    node -e "const fs = require('node:fs'); const result = JSON.parse(fs.readFileSync(process.argv[1], 'utf8'));
const bucket = name => result.buckets.find(entry => entry.bucket === name);
const titles = name => (bucket(name)?.commits ?? []).map(commit => commit.title);
$assertion" "$output"
}

# The watch only reports: a non-empty delta must never be signalled as a failure.
digest
assert_json "
    if (result.commitCount !== 11) throw new Error('expected 11 new commits, got ' + result.commitCount);
    if (bucket('combobox')?.commitCount !== 1) throw new Error('a non-ASCII path must bucket by directory, not by a quote character');
    if (bucket('tabs')?.commitCount !== 1) throw new Error('a pathname holding a newline must bucket by directory');
    if (bucket('select')?.commitCount !== 1) throw new Error('a pathname holding a record separator must bucket by directory');
    for (const entry of result.buckets) {
        for (const item of entry.commits) {
            if (!/^[0-9a-f]{40}$/.test(item.sha)) throw new Error('a pathname was parsed as a commit id: ' + JSON.stringify(item.sha));
        }
    }
    if (result.buckets.some(entry => entry.bucket.startsWith('\"'))) throw new Error('a bucket name was built from a quoted path');
    if (result.buckets.some(entry => entry.bucket.includes('\n'))) throw new Error('a newline leaked into a bucket name');
    if (result.issueBody.length > 60000) throw new Error('rendered body exceeded the issue body limit');
    if (result.buckets[0].bucket !== 'shared') throw new Error('shared bucket must sort first');
    if (result.buckets[0].kind !== 'shared') throw new Error('shared bucket must be kind shared');
"

# The mandatory shared bucket collects floating-ui-react, utils and internals alike.
assert_json "
    const shared = titles('shared').join(' | ');
    for (const fragment of ['disabled anchor tracking', 'FloatingFocusManager', 'roving focus']) {
        if (!shared.includes(fragment)) throw new Error('shared bucket is missing: ' + fragment);
    }
    if (bucket('utils') || bucket('internals') || bucket('floating-ui-react')) {
        throw new Error('shared directories must not also form their own buckets');
    }
"

# One commit, two buckets: bucket counts sum above the distinct commit total.
assert_json "
    if (!titles('shared').some(title => title.includes('Share the dismissal listener'))) {
        throw new Error('straddling commit missing from the shared bucket');
    }
    if (!titles('menu').some(title => title.includes('Share the dismissal listener'))) {
        throw new Error('straddling commit missing from the menu bucket');
    }
    if (bucket('menu').commitCount !== 2) throw new Error('expected 2 menu commits');
    const summed = result.buckets.reduce((total, entry) => total + entry.commitCount, 0);
    if (summed <= result.commitCount) throw new Error('expected bucket counts to exceed the distinct total');
"

# Nothing outside packages/react/src/ is silently dropped.
assert_json "
    if (bucket('docs/src/')?.commitCount !== 1) throw new Error('missing the docs/src/ bucket');
    if (bucket('docs/')?.commitCount !== 1) throw new Error('a two-segment path must bucket to its directory');
    if (bucket('(repository root)')?.commitCount !== 1) throw new Error('missing the repository-root bucket');
    for (const name of ['docs/src/', 'docs/', '(repository root)']) {
        if (bucket(name).kind !== 'other') throw new Error(name + ' must be kind other');
    }
    if (result.buckets.findIndex(entry => entry.kind === 'other') <= result.buckets.findLastIndex(entry => entry.kind === 'component')) {
        throw new Error('component buckets must sort before other buckets');
    }
"

# The title dedupes one digest per month, so it carries the period and no SHA.
assert_json "
    if (result.issueTitle !== 'Upstream watch digest — 2026-09') throw new Error('unexpected title: ' + result.issueTitle);
    if (/[0-9a-f]{40}/.test(result.issueTitle)) throw new Error('title contains a SHA');
    if (!result.issueBody.startsWith('<!-- upstream-watch-digest -->')) throw new Error('missing body marker');
    for (const fragment of [result.pin, result.head, result.compareUrl, 'Mandatory bucket']) {
        if (!result.issueBody.includes(fragment)) throw new Error('body is missing: ' + fragment);
    }
    if (/\]\(\.\.\//.test(result.issueBody)) throw new Error('body uses a relative link');
"

# An unmoved upstream still produces a digest; the evaluation happens either way.
head_sha="$(git -C "$upstream" rev-parse HEAD)"
digest --pin "$head_sha"
assert_json "
    if (result.commitCount !== 0) throw new Error('expected an empty delta');
    if (result.buckets.length !== 0) throw new Error('expected no buckets');
    if (!result.issueBody.includes('has not moved')) throw new Error('empty digest must say upstream has not moved');
"

# A long bucket states its remainder rather than truncating in silence.
for index in $(seq 1 45); do
    commit "[popups] Repeated shared change $index" "packages/react/src/utils/popups/store.ts"
done
digest
assert_json "
    if (bucket('shared').commits.length !== 49) throw new Error('JSON must carry every commit');
    const section = result.issueBody.split('### ')[1];
    if ((section.match(/^- \`[0-9a-f]{9}\` /gm) ?? []).length !== 40) throw new Error('expected 40 rendered titles');
    if (!section.includes('and 9 more in this bucket')) throw new Error('remainder must be stated');
"

# A commit that changed no files still has to surface somewhere rather than vanish, and buckets
# outside packages/react/src/ get a tighter title budget than the port-relevant ones.
git -C "$upstream" commit -q --allow-empty -m "Re-tag the release"
for index in $(seq 1 7); do
    commit "[docs] Repeated docs change $index" "docs/src/app/menu/page.mdx"
done
digest
assert_json "
    if (bucket('(no files)')?.commitCount !== 1) throw new Error('a commit touching no files must still be bucketed');
    if (!titles('(no files)').includes('Re-tag the release')) throw new Error('wrong commit in the (no files) bucket');
    if (bucket('docs/src/').commits.length !== 8) throw new Error('JSON must carry every docs commit');
    const section = result.issueBody.split('### \`docs/src/\`')[1].split('\n### ')[0];
    if ((section.match(/^- \`[0-9a-f]{9}\` /gm) ?? []).length !== 5) throw new Error('expected 5 rendered docs titles');
    if (!section.includes('and 3 more in this bucket')) throw new Error('docs remainder must be stated');
    if (result.titleBudget !== 40) throw new Error('a small delta must render at the full title budget');
"

# Force the hard-cap backstop. It only fires when headings and counts alone overflow at the smallest
# title budget, which takes far more buckets than upstream produces; long directory names reach the
# limit with fewer files.
long_directory="$(printf 'd%.0s' $(seq 1 200))"
overflow_paths=()
for index in $(seq 1 150); do
    overflow_paths+=("packages/react/src/${long_directory}${index}/file.ts")
done
commit "[all components] Overflow the issue body" "${overflow_paths[@]}"
digest
assert_json "
    if (result.truncated !== true) throw new Error('expected the hard cap to fire');
    if (result.titleBudget !== 0) throw new Error('expected the smallest title budget to be tried first');
    if (result.issueBody.length > 60000) throw new Error('body exceeded the hard cap: ' + result.issueBody.length);
    if (!result.issueBody.includes('Digest truncated')) throw new Error('truncation notice missing');
    if (!result.issueBody.includes(result.compareUrl)) throw new Error('compare URL missing from the truncation notice');
    // The exact Markdown the workflow appends after this script writes the body.
    const runLink = '\\n[View workflow run](https://github.com/JakeMurrayDev/Blazix.BaseUI/actions/runs/32451705302)\\n';
    if (result.issueBody.length + runLink.length > 65536) {
        throw new Error('body plus the run link exceeds the GitHub issue body limit');
    }
"

# A pin that is not an ancestor would produce a meaningless range, so it must fail loudly.
expect_failure() {
    if node "$repo_root/scripts/upstream-watch-digest.mjs" --upstream "$upstream" --ref master --quiet "$@" \
        > "$fixture/stdout.txt" 2> "$fixture/stderr.txt"; then
        echo "Expected the digest to fail: $*" >&2
        exit 1
    fi
}

git -C "$upstream" checkout -q -b orphan master
git -C "$upstream" checkout -q --orphan detached
git -C "$upstream" rm -rq --cached . 2>/dev/null || true
commit "Unrelated history" "packages/react/src/menu/Other.tsx"
orphan="$(git -C "$upstream" rev-parse HEAD)"
git -C "$upstream" checkout -q master

expect_failure --pin "$orphan"
grep -q "is not an ancestor of" "$fixture/stderr.txt" \
    || { echo "Expected a non-ancestor pin to be reported." >&2; exit 1; }

expect_failure --pin "not-a-sha"
grep -q "lowercase 40-character SHA" "$fixture/stderr.txt" \
    || { echo "Expected an invalid pin to be reported." >&2; exit 1; }

expect_failure --pin "$pin" --upstream "$fixture/missing-clone"
grep -q "does not exist" "$fixture/stderr.txt" \
    || { echo "Expected a missing clone to be reported." >&2; exit 1; }

# The committed pin file must stay readable by the default resolution path.
node -e "
const fs = require('node:fs');
const record = JSON.parse(fs.readFileSync(process.argv[1], 'utf8'));
if (!/^[0-9a-f]{40}\$/.test(record.pin)) throw new Error('docs/upstream-pin.json pin must be a 40-character SHA');
for (const field of ['upstreamRepository', 'upstreamRef']) {
    if (typeof record[field] !== 'string' || record[field].length === 0) {
        throw new Error('docs/upstream-pin.json field ' + field + ' must be a non-empty string');
    }
}
" "$repo_root/docs/upstream-pin.json"

echo "Validated upstream watch bucketing, shared-bucket precedence, empty delta, remainder reporting, budget degradation, the hard cap, pin validation, and the committed pin record."
