#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT=/var/www/vltk-mobile
HARNESS_ROOT="$PROJECT_ROOT/harness"
AGENTS_ROOT="$HARNESS_ROOT/.agents"
SKILLS_ROOT="$AGENTS_ROOT/skills"
VALIDATOR=/home/zet/.codex/skills/.system/skill-creator/scripts/quick_validate.py

expected_skills=(
  jx-enemy-port
  jx-hud-port
  jx-map-port
  jx-pc-port-rule
  jx-pc-resource-resolver
  jx-player-visual
  jx-skill-port
  jx-skill-ui-port
  reverse-engineering
  unity-mcp-orchestrator
)

test -d "$SKILLS_ROOT"
test ! -L "$AGENTS_ROOT"
test ! -L "$SKILLS_ROOT"

shopt -s dotglob nullglob
agent_children=("$AGENTS_ROOT"/*)
test "${#agent_children[@]}" -eq 1
test "${agent_children[0]}" = "$SKILLS_ROOT"

skill_children=("$SKILLS_ROOT"/*)
test "${#skill_children[@]}" -eq "${#expected_skills[@]}"
for skill in "${expected_skills[@]}"; do
  test -d "$SKILLS_ROOT/$skill"
  python3 "$VALIDATOR" "$SKILLS_ROOT/$skill"
done

test -z "$(find "$SKILLS_ROOT" -type l -print -quit)"
test -z "$(find "$SKILLS_ROOT" \( -type d \( -name evals -o -name __pycache__ \) -o -type f -name '*.pyc' \) -print -quit)"
test -z "$(find "$SKILLS_ROOT" -type d -empty -print -quit)"

legacy_pattern='jxwin-kinnox|/mnt/jxwin|/var/www/vhst|unityMCP_|file:///|bak/skills|/var/www/vltk-mobile/\.agents/skills'
if rg -n -uu "$legacy_pattern" "$SKILLS_ROOT"; then
  echo "Legacy skill reference found." >&2
  exit 1
fi

for alternate in \
  "$PROJECT_ROOT/.agents" \
  "$PROJECT_ROOT/.agent/unity-mcp-skill" \
  "$PROJECT_ROOT/bak/skills" \
  "$PROJECT_ROOT/.codex/skills" \
  "$PROJECT_ROOT/.factory/skills" \
  "$PROJECT_ROOT/.opencode/skills" \
  "$PROJECT_ROOT/.pi/skills" \
  "$PROJECT_ROOT/.kiro/skills" \
  "$HARNESS_ROOT/.codex/skills" \
  "$HARNESS_ROOT/.pi/skills"; do
  test ! -e "$alternate"
  test ! -L "$alternate"
done

test ! -e "$HARNESS_ROOT/AGENTS copy.md"
root_archives=("$PROJECT_ROOT"/*.skill)
test "${#root_archives[@]}" -eq 0

test -f "$PROJECT_ROOT/.agent/mcp_config.json"
test -f "$HARNESS_ROOT/.codex/config.toml"
python3 - "$HARNESS_ROOT/.codex/config.toml" <<'PY'
import sys
import tomllib
from pathlib import Path

config = tomllib.loads(Path(sys.argv[1]).read_text(encoding="utf-8"))
servers = config.get("mcp_servers", {})
for name in ("semble", "unityMCP"):
    if name not in servers:
        raise SystemExit(f"Missing MCP config section: {name}")
PY

if rg -n -uu 'bak/skills|/var/www/vltk-mobile/\.agents/skills' "$PROJECT_ROOT/scripts" -g '*.py'; then
  echo "Active verifier still points at a removed skill root." >&2
  exit 1
fi

python3 - "$SKILLS_ROOT" <<'PY'
import ast
import re
import sys
from pathlib import Path

root = Path(sys.argv[1])
missing = []
for path in root.rglob("*.py"):
    ast.parse(path.read_text(encoding="utf-8"), filename=str(path))

for markdown in root.rglob("*.md"):
    text = markdown.read_text(encoding="utf-8")
    for target in re.findall(r"\[[^\]]+\]\(([^)]+)\)", text):
        if "://" in target or target.startswith("#"):
            continue
        resolved = (markdown.parent / target.split("#", 1)[0]).resolve()
        if not resolved.exists():
            missing.append(f"{markdown}: {target}")
    for target in re.findall(r"`((?:/var/www|/home/zet|~/)[^`\n]+)`", text):
        if any(char in target for char in "<>{}*") or "..." in target:
            continue
        resolved = Path(target.replace("~", "/home/zet", 1).rstrip(".,;:"))
        if not resolved.exists():
            missing.append(f"{markdown}: {target}")

if missing:
    raise SystemExit("Missing local references:\n" + "\n".join(missing))
PY

python3 "$SKILLS_ROOT/jx-map-port/scripts/list_maps.py" --id 53 | rg 'id=53'
python3 "$SKILLS_ROOT/jx-map-port/scripts/jx_map_port.py" --help >/dev/null

"$HARNESS_ROOT/scripts/bin/harness-cli" tool check --json >/dev/null
"$HARNESS_ROOT/scripts/bin/harness-cli" query tools --json | python3 -c '
import json
import sys

expected = {
    "srcwalk": ("cli", "code-navigation"),
      "ketch": ("cli", "documentation-lookup"),
      "reverse-engineering": ("skill", "dhcd-reverse-engineering"),
      "semble": ("mcp", "code-search"),
    "unity-mcp": ("mcp", "unity-editor-automation"),
    "vltktool-unpak": ("binary", "jx-pak-unpack"),
}
records = {
    row["name"]: row
    for row in json.load(sys.stdin)
    if row.get("source") == "registered"
}
if set(records) != set(expected):
    raise SystemExit(f"Unexpected registered tools: {sorted(records)}")
for name, (kind, capability) in expected.items():
    row = records[name]
    if (row["kind"], row["capability"], row["status"]) != (kind, capability, "present"):
        raise SystemExit(f"Invalid tool state for {name}: {row}")
'

echo "US-001 verification passed."
