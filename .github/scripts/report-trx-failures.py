#!/usr/bin/env python3
"""Turn failed tests in .NET TRX result files into GitHub Actions error annotations.

Usage: report-trx-failures.py <results-folder> [--test-step-failed]

- Prints one ::error line per failed test (title = fully qualified test name,
  message = failure message plus the first lines of the stack trace).
- Shows at most 20 failed tests, then one summary annotation for the rest.
- Prints a pass/fail count for every TRX file, plus one ::notice with the totals.
- Exits 0 when nothing failed. Exits 1 when a test failed, or when the test
  step failed but no TRX file was written (for example the test host crashed).

Uses only the Python 3 standard library, so it runs on a plain ubuntu-latest runner.
"""

import glob
import os
import sys
import xml.etree.ElementTree as ET

MAX_ANNOTATIONS = 20
STACK_TRACE_LINES = 5
NS = {"t": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}


def escape_data(value: str) -> str:
    """Escape an annotation message as GitHub requires."""
    return value.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def escape_property(value: str) -> str:
    """Escape an annotation property (such as title), which also cannot hold ':' or ','."""
    return escape_data(value).replace(":", "%3A").replace(",", "%2C")


def text_of(element, path: str) -> str:
    found = element.find(path, NS)
    return (found.text or "").strip() if found is not None else ""


def read_trx(path: str):
    """Return (failed tests, totals) for one TRX file."""
    root = ET.parse(path).getroot()

    # Map each test id to "Namespace.Class.Method" using the test definitions.
    full_names = {}
    for unit_test in root.iterfind("t:TestDefinitions/t:UnitTest", NS):
        method = unit_test.find("t:TestMethod", NS)
        if method is None:
            continue
        class_name = method.get("className", "")
        method_name = method.get("name", unit_test.get("name", ""))
        full_names[unit_test.get("id")] = f"{class_name}.{method_name}" if class_name else method_name

    failed = []
    for result in root.iterfind("t:Results/t:UnitTestResult", NS):
        if result.get("outcome") != "Failed":
            continue
        name = full_names.get(result.get("testId")) or result.get("testName", "Unknown test")
        message = text_of(result, "t:Output/t:ErrorInfo/t:Message") or "Test failed (no message)."
        stack = text_of(result, "t:Output/t:ErrorInfo/t:StackTrace")
        if stack:
            stack_head = "\n".join(stack.splitlines()[:STACK_TRACE_LINES])
            message = f"{message}\n{stack_head}"
        failed.append((name, message))

    counters = root.find("t:ResultSummary/t:Counters", NS)
    totals = {}
    if counters is not None:
        for key in ("total", "executed", "passed", "failed"):
            totals[key] = counters.get(key, "?")
    return failed, totals


def main() -> int:
    if len(sys.argv) < 2:
        print("Usage: report-trx-failures.py <results-folder> [--test-step-failed]")
        return 2

    results_folder = sys.argv[1]
    test_step_failed = "--test-step-failed" in sys.argv[2:]
    trx_files = sorted(glob.glob(os.path.join(results_folder, "**", "*.trx"), recursive=True))

    if not trx_files:
        if test_step_failed:
            print("::error title=dotnet test::The test step failed but wrote no TRX results. "
                  "Check the test step output (the test host may have crashed).")
            return 1
        print("No TRX files found.")
        return 0

    all_failed = []
    grand_totals = {"total": 0, "passed": 0, "failed": 0}
    for trx in trx_files:
        try:
            failed, totals = read_trx(trx)
        except ET.ParseError as error:
            print(f"::error title=TRX parse error::{escape_data(f'{os.path.basename(trx)}: {error}')}")
            return 1
        summary = ", ".join(f"{k} {v}" for k, v in totals.items())
        print(f"{os.path.basename(trx)}: {summary}")
        for key in grand_totals:
            if str(totals.get(key, "")).isdigit():
                grand_totals[key] += int(totals[key])
        all_failed.extend(failed)

    # One notice annotation with the overall counts, so they show on the check run.
    print(f"::notice title=.NET test results::{len(trx_files)} test project(s): "
          f"{grand_totals['total']} tests, {grand_totals['passed']} passed, {grand_totals['failed']} failed")

    for name, message in all_failed[:MAX_ANNOTATIONS]:
        print(f"::error title={escape_property(name)}::{escape_data(message)}")

    hidden = len(all_failed) - MAX_ANNOTATIONS
    if hidden > 0:
        print(f"::error title=More test failures::{hidden} more failed tests not shown")

    if all_failed:
        print(f"{len(all_failed)} test(s) failed.")
        return 1

    if test_step_failed:
        print("::error title=dotnet test::The test step failed but no failed tests were found in the TRX files. "
              "Check the test step output.")
        return 1

    print("All tests passed.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
