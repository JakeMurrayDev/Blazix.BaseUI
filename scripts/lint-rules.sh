#!/usr/bin/env bash
# Blazix.BaseUI Lint Rules — textual subset
# Structural rules (R02, R03, R07, R09, R10, R11, R12, R13, R14, R15) live in
# the Roslyn analyzer project at src/Blazix.BaseUI.Analyzers/ and run during
# `dotnet build`. This script enforces the remaining text-only rules that do
# not benefit from the C# AST: R01, R04, R05, R06.
#
# Usage: bash scripts/lint-rules.sh [--rule N]

set -u

# Colors (disabled if not a terminal)
if [ -t 1 ]; then
  RED='\033[0;31m'
  GREEN='\033[0;32m'
  YELLOW='\033[0;33m'
  CYAN='\033[0;36m'
  NC='\033[0m'
else
  RED='' GREEN='' YELLOW='' CYAN='' NC=''
fi

REPO_ROOT="$(cd "$(git rev-parse --show-toplevel)" && pwd)"
SRC_DIR="$REPO_ROOT/src/Blazix.BaseUI"
RULE_FILTER=""
VIOLATIONS_FILE=$(mktemp)
trap 'rm -f "$VIOLATIONS_FILE"' EXIT

declare -A RULE_NAMES
RULE_NAMES[1]="No logic in stubs"
RULE_NAMES[4]="No partition comments"
RULE_NAMES[5]="Data attribute prefix"
RULE_NAMES[6]="No empty-string standalone attrs"

for key in "${!RULE_NAMES[@]}"; do : ; done

# Parse arguments
while [[ $# -gt 0 ]]; do
  case $1 in
    --rule)
      if [[ $# -lt 2 || -z "${2:-}" ]]; then
        echo "Missing value for --rule"
        exit 1
      fi
      RULE_FILTER="${2#0}"
      if [[ -z "${RULE_NAMES[$RULE_FILTER]+x}" ]]; then
        echo "Invalid --rule value: $2 (valid: ${!RULE_NAMES[*]})"
        exit 1
      fi
      shift 2 ;;
    *) echo "Unknown option: $1"; exit 1 ;;
  esac
done

should_run() {
  [[ -z "$RULE_FILTER" || "$RULE_FILTER" == "$1" ]]
}

check_suppression() {
  local file="$1" line_num="$2" rule_num="$3"
  local padded
  padded=$(printf "%02d" "$rule_num")
  local prev_line=$((line_num - 1))
  local current_line
  current_line=$(sed -n "${line_num}p" "$file")
  if echo "$current_line" | grep -q "lint-ignore:RULE-${padded}"; then
    return 0
  fi
  if [ "$prev_line" -gt 0 ]; then
    local prev
    prev=$(sed -n "${prev_line}p" "$file")
    if echo "$prev" | grep -q "lint-ignore:RULE-${padded}"; then
      return 0
    fi
  fi
  return 1
}

report() {
  local rule_num="$1" file="$2" line="$3" message="$4"
  local rel_path="${file#$REPO_ROOT/}"
  if check_suppression "$file" "$line" "$rule_num"; then
    return
  fi
  local formatted
  formatted=$(printf "[RULE-%02d] %s:%s - %s" "$rule_num" "$rel_path" "$line" "$message")
  echo -e "${RED}${formatted}${NC}"
  echo "RULE-${rule_num}" >> "$VIOLATIONS_FILE"
}

# ============================================================
# R01: No logic in component stubs
# ============================================================
check_rule_01() {
  while IFS= read -r -d '' cs_file; do
    local base="${cs_file%.cs}"
    local razor_file="${base}.razor"
    [[ -f "$razor_file" ]] || continue

    # Exclude non-stub files
    local fname
    fname=$(basename "$cs_file")
    case "$fname" in
      *Context.cs|*State.cs|*State\`*.cs|Enumerations.cs|EventArgs.cs|Extensions.cs) continue ;;
    esac

    # Check if file contains method bodies or property accessors beyond the stub
    # A valid stub has: namespace, using, XML docs, attributes, partial class declaration with ;
    # Flag if we find { } blocks that aren't empty class bodies
    if grep -En '(public|private|protected|internal)[[:space:]]+(override[[:space:]]+)?(async[[:space:]]+)?(void|Task|ValueTask|bool|int|string|object)[[:space:]]+[[:alnum:]_]+[[:space:]]*\(' "$cs_file" | grep -v 'partial' > /dev/null 2>&1; then
      local line_num
      line_num=$(grep -En '(public|private|protected|internal)[[:space:]]+(override[[:space:]]+)?(async[[:space:]]+)?(void|Task|ValueTask|bool|int|string|object)[[:space:]]+[[:alnum:]_]+[[:space:]]*\(' "$cs_file" | grep -v 'partial' | head -1 | cut -d: -f1)
      if [ -n "$line_num" ]; then
        report 1 "$cs_file" "$line_num" "Component stub contains method definitions — logic should be in .razor @code block"
      fi
    fi
  done < <(find "$SRC_DIR" -name "*.cs" -not -path "*/obj/*" -not -path "*/bin/*" -not -path "*/Base/*" -print0)
}

# ============================================================
# R04: No partition comments
# ============================================================
check_rule_04() {
  while IFS= read -r -d '' file; do
    grep -En '//[[:space:]]*={3,}|//[[:space:]]*-{3,}' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 4 "$file" "$line_num" "Partition comment found — do not use section dividers"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R05: Data attribute prefix
# ============================================================
check_rule_05() {
  while IFS= read -r -d '' file; do
    grep -n "data-base-ui-" "$file" 2>/dev/null | grep -v "data-blazix-base-ui-" | while IFS=: read -r line_num _; do
      report 5 "$file" "$line_num" "Uses 'data-base-ui-' instead of 'data-blazix-base-ui-'"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" -o -name "*.js" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# R06: No empty-string standalone attributes
# ============================================================
check_rule_06() {
  while IFS= read -r -d '' file; do
    # Pattern 1: AddAttribute(N, "data-*", "")
    grep -En 'AddAttribute\(.*"data-.*",[[:space:]]*""\)' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned empty string in AddAttribute"
    done
    # Pattern 2: attrs["data-*"] = ""
    grep -En '\["data-.*"\][[:space:]]*=[[:space:]]*""' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned empty string"
    done
    # Pattern 3: attrs["data-*"] = string.Empty
    grep -En '\["data-.*"\][[:space:]]*=[[:space:]]*string\.Empty' "$file" 2>/dev/null | while IFS=: read -r line_num _; do
      report 6 "$file" "$line_num" "Standalone data attribute assigned string.Empty"
    done
  done < <(find "$SRC_DIR" \( -name "*.razor" -o -name "*.cs" \) -not -path "*/obj/*" -print0)
}

# ============================================================
# Main
# ============================================================
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Blazix.BaseUI Lint Rules (textual subset)${NC}"
echo -e "${CYAN}========================================${NC}"
echo ""

should_run 1 && check_rule_01
should_run 4 && check_rule_04
should_run 5 && check_rule_05
should_run 6 && check_rule_06

echo ""
echo -e "${CYAN}========================================${NC}"
echo -e "${CYAN}Summary${NC}"
echo -e "${CYAN}========================================${NC}"

TOTAL_VIOLATIONS=0
for key in 1 4 5 6; do
  local_count=$(grep -c "^RULE-${key}$" "$VIOLATIONS_FILE" 2>/dev/null) || local_count=0
  TOTAL_VIOLATIONS=$((TOTAL_VIOLATIONS + local_count))
  if [ "$local_count" -gt 0 ]; then
    color="$RED"
  else
    color="$GREEN"
  fi
  printf "${color}RULE-%02d (%s): %d violations${NC}\n" "$key" "${RULE_NAMES[$key]}" "$local_count"
done

echo ""
if [ "$TOTAL_VIOLATIONS" -gt 0 ]; then
  echo -e "${RED}Total: $TOTAL_VIOLATIONS violations${NC}"
  exit 1
else
  echo -e "${GREEN}Total: 0 violations${NC}"
  echo -e "${YELLOW}Note:${NC} structural rules (R02, R03, R07, R09–R15) are enforced by"
  echo -e "      src/Blazix.BaseUI.Analyzers via 'dotnet build'."
  exit 0
fi
