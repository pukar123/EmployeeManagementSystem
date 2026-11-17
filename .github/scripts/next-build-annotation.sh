#!/usr/bin/env bash
# Turn a failed `next build` log into one GitHub Actions error annotation.
# Usage: next-build-annotation.sh <build-log> <app-folder>
# Finds the first error in the log and puts it (at most 20 lines) into a single
# ::error line. If the error names a source file (for example
# "./src/app/page.tsx:12:5"), the annotation points at that file and line.

set -uo pipefail

log_file="$1"
app_folder="$2"
max_lines=20

if [[ ! -s "$log_file" ]]; then
  echo "::error title=next build::The build failed and wrote no output."
  exit 0
fi

# Remove colour codes so the patterns below match.
clean_log=$(sed -E 's/\x1b\[[0-9;]*[A-Za-z]//g' "$log_file")

# First line that looks like an error; fall back to the last lines of the log.
start=$(printf '%s\n' "$clean_log" | grep -n -m1 -E '(Type error|Failed to compile|Error:|error TS[0-9]+|Module not found|^\./[^ ]+:[0-9]+:[0-9]+)' | cut -d: -f1)
if [[ -n "$start" ]]; then
  excerpt=$(printf '%s\n' "$clean_log" | tail -n +"$start" | head -n "$max_lines")
else
  excerpt=$(printf '%s\n' "$clean_log" | tail -n "$max_lines")
fi

# Optional file and line, from a "./path/file.tsx:line:col" reference in the excerpt.
location=""
reference=$(printf '%s\n' "$excerpt" | grep -m1 -oE '^\./[^ :]+:[0-9]+:[0-9]+' || true)
if [[ -n "$reference" ]]; then
  file_path="${reference%%:*}"
  rest="${reference#*:}"
  line="${rest%%:*}"
  col="${rest#*:}"
  location="file=${app_folder}/${file_path#./},line=${line},col=${col},"
fi

# Escape as GitHub requires: % first, then carriage returns and newlines.
message=${excerpt//'%'/'%25'}
message=${message//$'\r'/'%0D'}
message=${message//$'\n'/'%0A'}

echo "::error ${location}title=next build failed::${message}"
