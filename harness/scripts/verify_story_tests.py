#!/usr/bin/env python3
"""
Harness story verifier — checks the latest Unity EditMode TestResults.xml for
failures in a given story's evidence test classes.

Usage:
    verify_story_tests.py <TestClassA> [<TestClassB> ...]

Exit 0  -> every named test class has zero failures in the latest run (PASS)
Exit 1  -> at least one named class has >=1 failed test, OR no result file (FAIL)

This makes harness `verify_command` a REAL signal tied to executed tests instead
of a `true`/`echo` no-op. Re-run the EditMode suite (Unity Test Runner) to refresh
TestResults.xml, then `harness-cli story verify <id>` re-reads it.
"""
import sys, os, re, glob, html

# Unity persistentDataPath on Linux: ~/.config/unity3d/<company>/<product>/TestResults.xml
CANDIDATES = [
    "/home/zet/.config/unity3d/vltk/vltk-mobile/TestResults.xml",
    os.path.expanduser("~/.config/unity3d/vltk/vltk-mobile/TestResults.xml"),
]
# also accept an env override
if os.environ.get("VLTK_TESTRESULTS"):
    CANDIDATES.insert(0, os.environ["VLTK_TESTRESULTS"])


def find_results():
    for c in CANDIDATES:
        if os.path.isfile(c):
            return c
    # last resort: newest TestResults.xml under any unity3d config dir
    for base in ("/home/zet/.config/unity3d", os.path.expanduser("~/.config/unity3d")):
        hits = glob.glob(os.path.join(base, "vltk", "**", "TestResults.xml"), recursive=True)
        if hits:
            return max(hits, key=os.path.getmtime)
    return None


def main():
    classes = [c.strip() for c in sys.argv[1:] if c.strip()]
    if not classes:
        print("verify_story_tests: no test classes given", file=sys.stderr)
        return 1
    path = find_results()
    if not path:
        print("verify_story_tests: no TestResults.xml found — run the EditMode suite first", file=sys.stderr)
        return 1
    xml = open(path, encoding="utf-8", errors="replace").read()
    # collect failed test-case fullnames
    failed = re.findall(r'<test-case\b[^>]*\bresult="Failed"[^>]*\bfullname="([^"]*)"', xml)
    failed += re.findall(r'<test-case\b[^>]*\bfullname="([^"]*)"[^>]*\bresult="Failed"', xml)
    failed = set(failed)
    bad = {}
    for fn in failed:
        for cls in classes:
            # match ".ClassName." or ".ClassName(" boundary
            if re.search(r'[.\b]' + re.escape(cls) + r'\b', fn):
                bad.setdefault(cls, []).append(fn)
    if bad:
        for cls, fns in sorted(bad.items()):
            print(f"FAIL {cls}: {len(fns)} failing test(s)", file=sys.stderr)
            for fn in sorted(fns)[:5]:
                print(f"     - {fn}", file=sys.stderr)
        return 1
    print(f"PASS: {', '.join(classes)} (0 failures in {os.path.basename(path)})")
    return 0


if __name__ == "__main__":
    sys.exit(main())
