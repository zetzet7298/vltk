import sys

with open("/tmp/vltk-unity-test-run.log", "r", encoding="utf-8", errors="ignore") as f:
    lines = f.readlines()

print(f"Total lines in log: {len(lines)}")
# Print the last 100 non-stacktrace lines
non_stack = []
for line in reversed(lines):
    line = line.strip()
    if not line:
        continue
    # skip stack trace elements
    if any(x in line for x in ["UnityEngine.Debug", "UnityEngine.StackTraceUtility", "System.Reflection", "UnityEngine.Events", "NUnit.Framework", "UnityEngine.TestRunner", "UnityEditor.TestTools", "UnityEngine.TestTools"]):
        continue
    if line.startswith("at ") or line.startswith("("):
        continue
    non_stack.append(line)
    if len(non_stack) >= 40:
        break

for line in reversed(non_stack):
    print(line)
