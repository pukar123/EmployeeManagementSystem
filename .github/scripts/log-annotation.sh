#!/usr/bin/env bash
# Turn the end of a log file into one GitHub Actions error annotation, so a
# failing step can be diagnosed from the check run without the raw logs.
# Usage: log-annotation.sh <title> <log-file> [max-lines]

set -uo pipefail

title="$1"
log_file="$2"
max_lines="${3:-30}"

if [[ ! -s "$log_file" ]]; then
  echo "::error title=${title}::The step failed and wrote no output."
  exit 0
fi

# Remove colour codes, keep the last lines (EF and sqlcmd print the cause last).
excerpt=$(sed -E 's/\x1b\[[0-9;]*[A-Za-z]//g' "$log_file" | tail -n "$max_lines")

# Escape as GitHub requires: % first, then carriage returns and newlines.
message=${excerpt//'%'/'%25'}
message=${message//$'\r'/'%0D'}
message=${message//$'\n'/'%0A'}

echo "::error title=${title}::${message}"
