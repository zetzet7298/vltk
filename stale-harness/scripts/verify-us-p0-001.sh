#!/usr/bin/env bash
set -euo pipefail

# This verifier proves the story packet's documented gate, not PC evidence.
readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET_ROOT="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-001-provenance-evidence-gate"
readonly EXECPLAN="$PACKET_ROOT/execplan.md"
readonly OVERVIEW="$PACKET_ROOT/overview.md"
readonly DESIGN="$PACKET_ROOT/design.md"
readonly VALIDATION="$PACKET_ROOT/validation.md"

for packet_file in "$EXECPLAN" "$OVERVIEW" "$DESIGN" "$VALIDATION"; do
  test -f "$packet_file"
  test ! -L "$packet_file"
done

require_anchor() {
  local anchor=$1
  local file=$2

  if ! rg -Fq -- "$anchor" "$file"; then
    printf 'Missing required packet anchor %q in %s\n' "$anchor" "$file" >&2
    exit 1
  fi
}

for anchor in \
  'REQ-P0-001' \
  'OBJ-P0-02' \
  'OBJ-P0-04' \
  'DOC-GOV-02' \
  'DOC-JX-05' \
  'DOC-JX-08' \
  'B-EVIDENCE-001' \
  'B-LEGAL-001'; do
  require_anchor "$anchor" "$OVERVIEW"
done

for field in \
  '`candidate_absolute_paths`' \
  '`absolute_selected_path`' \
  '`pack_version`' \
  '`load_order_winner`' \
  '`hash_uid`' \
  '`encoding`' \
  '`normalized_path_bytes_hex`' \
  '`byte_count`' \
  '`sha256`' \
  '`resolver_evidence`' \
  '`decode_result`' \
  '`name_vi_cross_check`' \
  '`reviewer`' \
  '`reviewed_at`' \
  '`legal_status`'; do
  require_anchor "$field" "$DESIGN"
done

require_anchor 'Complete enumeration of valid candidates' "$DESIGN"
require_anchor 'internal-only' "$OVERVIEW"
require_anchor 'public distribution is prohibited' "$OVERVIEW"
require_anchor 'No runtime port or Unity implementation.' "$OVERVIEW"
require_anchor 'No vendoring or copying selected bytes.' "$OVERVIEW"
require_anchor 'No guessed candidate' "$OVERVIEW"
require_anchor 'No legal-clearance' "$OVERVIEW"
require_anchor 'It cannot prove a selected asset' "$VALIDATION"
require_anchor 'No asset fixture is introduced.' "$VALIDATION"

printf 'US-P0-001 packet contract verification passed.\n'
