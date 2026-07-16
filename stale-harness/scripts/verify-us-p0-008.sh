#!/usr/bin/env bash
set -euo pipefail

readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-008-dhcd-drop-xp-recovery"
readonly EVIDENCE="/home/zet/Projects/dhcd/docs/evidence/r-dhcd-006-drop-xp.md"
readonly AB="/var/www/dhcd/localization_vi/output/apktool_clean_from_full/assets/ab"

for file in "$PACKET/overview.md" "$PACKET/design.md" "$PACKET/execplan.md" "$PACKET/validation.md" "$EVIDENCE"; do
  test -f "$file"
  test ! -L "$file"
done

require_anchor() {
  rg -Fq -- "$1" "$2" || { printf 'Missing anchor %q in %s\n' "$1" "$2" >&2; exit 1; }
}
check_hash() { printf '%s  %s\n' "$1" "$2" | sha256sum -c - >/dev/null; }

check_hash e46a50bee7e6b1da98678fc880b7c4a3c337b09d42c619011b99b7a61cc8cc5f "$AB/index_3.bytes"
check_hash c3d67baa0fae7b2a4e043fad1071a6ded0334aa01dadc655790b4a74aba5e129 "$AB/index_5.bytes"
check_hash 541c5a68be8a088f9739d8af65d426db62ec440348f58d8ff5cc90be278f0b5b "$AB/index_7.bytes"
check_hash 3012ce91b7c11517ab120ccfd030bed61431023cebf1912daa770a3fdab7859c "$AB/assets_resources_config_resbin_fp_levelbaseconfig.bytes.ab"
check_hash 6b5bbe300e199ff895f55ed7fdde0e15ef49a23637f64a25e6b1858fa575ff62 "$AB/assets_resources_config_resbin_fp_collectitemconfig.bytes.ab"
check_hash ae9da776d9bde4e88bf71b3e21e80f62af989a92d4f3f3e02a0357663d842a0f "$AB/assets_resources_config_resbin_fp_collectitempoolconfig.bytes.ab"
check_hash de803c86dacf8502d478f2a83a4d9892b579745b837ec2a8b8ecfbf168236468 "$AB/assets_resources_config_resbin_playerexpconfig.bytes.ab"
check_hash f8bcc12a669756c893883227784dfb860b7731377c503ca0d441dc30fef510d1 "$AB/assets_resources_config_resbin_fp_bootyconfig.bytes.ab"

for anchor in 'Status: blocked; no reward constants recovered.' 'No numeric reward constant is claimed.' 'not a VFS/update winner' 'No on-disk digest comparison' '## Exact next target'; do
  require_anchor "$anchor" "$EVIDENCE"
done
require_anchor 'R-DHCD-006 | P0 | economy-owner / queued' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md"
require_anchor 'candidate rows are not active reward constants' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"

printf 'US-P0-008 fail-closed drop/XP evidence verified.\n'
