// Turn an ESLint JSON report into GitHub Actions annotations.
// Usage: node eslint-annotations.mjs <eslint-report.json>
// Errors become ::error lines (file, line, column, rule); at most 20 are shown,
// then one summary line. Warnings are counted but not annotated.
// Exits 1 when the report contains any lint error, otherwise 0.

import { readFileSync, existsSync } from "node:fs";
import path from "node:path";

const MAX_ANNOTATIONS = 20;
const reportPath = process.argv[2];

const escapeData = (value) =>
  String(value).replace(/%/g, "%25").replace(/\r/g, "%0D").replace(/\n/g, "%0A");
const escapeProperty = (value) =>
  escapeData(value).replace(/:/g, "%3A").replace(/,/g, "%2C");

if (!reportPath || !existsSync(reportPath)) {
  console.log("::error title=ESLint::ESLint did not write a JSON report. Check the lint step output.");
  process.exit(1);
}

const workspace = process.env.GITHUB_WORKSPACE ?? process.cwd();
const results = JSON.parse(readFileSync(reportPath, "utf8"));

const errors = [];
let warningCount = 0;
for (const result of results) {
  const file = path.relative(workspace, result.filePath).split(path.sep).join("/");
  for (const message of result.messages) {
    if (message.severity === 2) {
      errors.push({ file, ...message });
    } else {
      warningCount += 1;
    }
  }
}

for (const error of errors.slice(0, MAX_ANNOTATIONS)) {
  const rule = error.ruleId ?? "parse-error";
  const properties = [
    `file=${escapeProperty(error.file)}`,
    `line=${error.line ?? 1}`,
    `col=${error.column ?? 1}`,
    `title=${escapeProperty(`ESLint ${rule}`)}`,
  ].join(",");
  console.log(`::error ${properties}::${escapeData(`${error.message} (${rule})`)}`);
}

if (errors.length > MAX_ANNOTATIONS) {
  console.log(`::error title=More lint errors::${errors.length - MAX_ANNOTATIONS} more lint errors not shown`);
}

console.log(`ESLint: ${errors.length} error(s), ${warningCount} warning(s).`);
process.exit(errors.length > 0 ? 1 : 0);
