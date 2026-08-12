#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
INVENTORY="$ROOT/specs/dhcd-jx-port/06-client/reuse-inventory.md"
PACKET="$ROOT/docs/stories/epics/E00-p0-foundation/US-P0-003-reuse-inventory"
UNITY_ROOT="/var/www/vltk-mobile"
EXPECTED_REVISION="d4b1b06aef150739b97ab693741841aa3e51ea8f"

fail() {
  printf 'US-P0-003 verification failed: %s\n' "$*" >&2
  exit 1
}

require_file() {
  [[ -f "$1" ]] || fail "missing required file: $1"
}

require_text() {
  local file="$1"
  local text="$2"
  grep -Fq -- "$text" "$file" || fail "missing required evidence in $file: $text"
}

require_file "$INVENTORY"
for file in overview.md design.md execplan.md validation.md; do
  require_file "$PACKET/$file"
done

require_text "$PACKET/overview.md" "REQ-P0-011"
require_text "$PACKET/overview.md" "DOC-CLIENT-01"
require_text "$PACKET/overview.md" "DOC-CLIENT-04"
require_text "$PACKET/design.md" "parser-only"
require_text "$PACKET/design.md" "audited-roster-only"
require_text "$PACKET/validation.md" "runtime migration remains explicitly unproven"
require_text "$PACKET/execplan.md" "US-P0-001 -> US-P0-003"
require_text "$INVENTORY" "Runtime migration remains unproven and out of scope for this inventory."
require_text "$INVENTORY" 'Classification: `parser-only`.'
require_text "$INVENTORY" 'Forbidden: `TriggerWave` wall-clock state (`DateTimeOffset.UtcNow`, line 60)'
require_text "$INVENTORY" 'Classification: `audited-roster-only`.'
require_text "$INVENTORY" 'Forbidden: curated `SharedTemplates`, `MapEnemyTemplates`, `DefaultSpawnPoints`, merge/default behavior, and synthetic/fallback mappings.'
require_text "$INVENTORY" "No feature flag may enable this runtime."
require_text "$INVENTORY" "No pilot flag before audited roster proof."
require_text "$INVENTORY" "adapter, shadow test, migration test, feature"

actual_revision="$(git -C "$UNITY_ROOT" rev-parse HEAD)"
[[ "$actual_revision" == "$EXPECTED_REVISION" ]] ||
  fail "Unity revision changed: expected $EXPECTED_REVISION, got $actual_revision"

check_source() {
  local relative_path="$1"
  local blob="$2"
  local sha256="$3"
  local line_range="$4"
  local absolute_path="$UNITY_ROOT/$relative_path"

  require_file "$absolute_path"
  require_text "$INVENTORY" "$absolute_path:$line_range"
  require_text "$INVENTORY" "blob \`$blob\`"
  require_text "$INVENTORY" "sha256 \`$sha256\`"
  [[ "$(git -C "$UNITY_ROOT" rev-parse "HEAD:$relative_path")" == "$blob" ]] ||
    fail "Git blob mismatch for $relative_path"
  [[ "$(sha256sum "$absolute_path" | awk '{print $1}')" == "$sha256" ]] ||
    fail "SHA-256 mismatch for $relative_path"
}

check_source "Assets/Scripts/Sandbox/CityDefenceService.cs" \
  "aa9d94a58901d40cb5618aa2589717cda7e20e87" \
  "3386dd15b36f1b9e86413e2c5028f29e381fcbe1ea80ff0e884e24552ae5fdff" \
  "15-125"
check_source "Assets/Scripts/Sandbox/MapEnemyDatabase.cs" \
  "cd01495502100f6f1df8215c1b3a36aee27942ad" \
  "5a9a3b22337a62dff4fdb707221eef5684e36c2607c87d266bf4f918375a2429" \
  "20-42,45-145"
check_source "Assets/Scripts/Sandbox/PcPortraitParser.cs" \
  "5000e0d9fe7812ae87e80931a102e68fa6f44694" \
  "e7c04699bd53268c20bb0b11dd5c53f8a8ff484e05af37b2cd7a0e9f4260d733" \
  "12-62"
check_source "Assets/Scripts/Sandbox/HudDataBridge.cs" \
  "ec717881082ccaf89d9ab0f30938a117a079a2cd" \
  "b483c4bec73e4cc1aa6243548c0e620343b7a628835c0035487d37abf53b0ea2" \
  "13-40,85-125"
check_source "Assets/Scripts/Sandbox/GoldenSnapshotComparer.cs" \
  "fb4146ff6502f0d692e6ec3cf7ffcdfcaf555313" \
  "460ab84fb53b8a39739d66606546ca6459212f8b2da4c6c855ffe40721904f6c" \
  "31-145"
check_source "Assets/Scripts/Sandbox/CombatRuntimeService.cs" \
  "156518b8ed541220c6887d9f38899d8c9a12d802" \
  "ede97a1a3c76b872429fe9c3c6f8b9a4a0f54161956b3f33cd4ec14063d74bb8" \
  "59-165"

if rg -n -i --glob '*.md' \
  '(runtime migration (is )?complete|runtime migration completed|migration complete)' \
  "$PACKET" "$INVENTORY" | rg -v 'No .*runtime migration complete|no runtime migration-complete|runtime migration remains unproven|does not.*runtime migration|cannot prove a Unity runtime migration|not a migration approval|out of scope for this inventory|Fail if .*claims runtime migration complete'; then
  fail "found an unqualified runtime migration completion claim"
fi

printf 'US-P0-003 inventory contract verified; runtime migration remains unproven.\n'
