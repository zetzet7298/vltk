#!/usr/bin/env bash
set -euo pipefail

# This validates fail-closed documentation only; it never inspects PC source.
readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET_ROOT="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-002-arena-candidate-provenance"
readonly AUDIT="$HARNESS_ROOT/specs/dhcd-jx-port/05-jx-parity/arena-candidate-audit.md"
readonly EXECPLAN="$PACKET_ROOT/execplan.md"
readonly OVERVIEW="$PACKET_ROOT/overview.md"
readonly DESIGN="$PACKET_ROOT/design.md"
readonly VALIDATION="$PACKET_ROOT/validation.md"

for file in "$EXECPLAN" "$OVERVIEW" "$DESIGN" "$VALIDATION" "$AUDIT"; do
  test -f "$file"
  test ! -L "$file"
done

require_anchor() {
  local anchor=$1
  local file=$2

  if ! rg -Fq -- "$anchor" "$file"; then
    printf 'Missing required anchor %q in %s\n' "$anchor" "$file" >&2
    exit 1
  fi
}

for anchor in \
  'REQ-P0-002' \
  'yanwuchang' \
  'jingjichang' \
  'shiliantang' \
  '209/210/211' \
  '975' \
  '925' \
  'US-P0-001 -> US-P0-002' \
  'No Unity map import' \
  'No byte vendoring'; do
  require_anchor "$anchor" "$PACKET_ROOT"
done

for field in \
  '`logical_map_path`' \
  '`absolute_candidate_paths`' \
  '`pack_version`' \
  '`load_order_winner`' \
  '`hash_uid`' \
  '`encoding`' \
  '`normalized_path_bytes_hex`' \
  '`byte_count`' \
  '`sha256`' \
  '`region_c_decode`' \
  '`region_s_decode`' \
  '`terrain_decode`' \
  '`minimap_decode`'; do
  require_anchor "$field" "$DESIGN"
done

for candidate in yanwuchang jingjichang shiliantang; do
  require_anchor "\`$candidate\`" "$AUDIT"
done

require_anchor 'Selection: none.' "$AUDIT"
require_anchor 'All candidate `Region_C`, `Region_S`, terrain, and minimap decode fields are' "$AUDIT"
require_anchor '`unresolved`' "$AUDIT"
require_anchor 'No candidate is selected,' "$AUDIT"
require_anchor 'do not prove package winner or collision data' "$AUDIT"

if rg -Fq 'Selection: selected' "$AUDIT"; then
  printf 'Audit attempts to select a candidate without this verifier accepting winner evidence.\n' >&2
  exit 1
fi

printf 'US-P0-002 fail-closed packet and audit verification passed.\n'
