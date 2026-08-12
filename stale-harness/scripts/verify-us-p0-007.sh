#!/usr/bin/env bash
set -euo pipefail

readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-007-dhcd-modal-queue-recovery"
readonly EVIDENCE="/home/zet/Projects/dhcd/docs/evidence/r-dhcd-002-modal-queue.md"
readonly DHCD="/home/zet/Projects/dhcd"
readonly APK="/var/www/dhcd/localization_vi/output/apktool_clean_from_full"
readonly MAPPER="$DHCD/tools/inspect-r-dhcd-001-native-method-map.py"

for file in "$PACKET/overview.md" "$PACKET/design.md" "$PACKET/execplan.md" "$PACKET/validation.md" "$EVIDENCE" "$MAPPER"; do
  test -f "$file"
  test ! -L "$file"
done

require_anchor() {
  rg -Fq -- "$1" "$2" || { printf 'Missing anchor %q in %s\n' "$1" "$2" >&2; exit 1; }
}
check_hash() { printf '%s  %s\n' "$1" "$2" | sha256sum -c - >/dev/null; }

check_hash 130d09d3b1cdc57ad12eee96d77b6db9665b22dfd40312d4f750fe6f93caabe8 "$APK/lib/arm64-v8a/libil2cpp.so"
check_hash 459503b7d16ab3ae95190a180fc5bb3a361b7bb8e39651eb583725bf0888c2f4 "$APK/assets/bin/Data/Managed/Metadata/global-metadata.dat"
check_hash d6c80b59ebd243a74d03e02d5c556fb238584219640b3ee18f8f81f099007eac "$DHCD/il2cpp/isil-r-dhcd-001/BattleCore.LevelRandomSkillCtrl.txt"
check_hash 48c9007904296cbc10c52369306726133e949402faebc1b9198fe8de0160b2ae "$DHCD/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/LevelRandomSkillCtrl.cs"
check_hash 5982e170c7bb7275f673c36bc558d8902647e96823ae480a19a98fd787f287ba "$DHCD/il2cpp/diffable-cs/DiffableCs/BattleCore/BattleCore/PlayerRandomSkillData.cs"
check_hash 3fe95a17570320ea7d22846d01396dbd42db78fced5c860bd3c9b08a71ec2ed8 "$DHCD/il2cpp/diffable-cs/DiffableCs/GameLogic/A5Game/BattleLearnSkillCtrl.cs"
check_hash 7a0d40ecf883209e9d3d36904ec86ee75419e0e5375796b886ada7b77feae67b "$DHCD/reconstructed-types/GameLogic/A5Game.BattleLearnSkillCtrl.cs"
check_hash 3979879597220ca3866cc91bb8c3a75c10d8d22f273e8ac23d338c24af3d2a0f "$MAPPER"
python3 "$MAPPER" >/dev/null

for anchor in 'Status: `provisional`' 'native branch proven; target semantics unresolved' 'Client modal and input-lock search boundary' '**Unresolved / fail closed:**'; do
  require_anchor "$anchor" "$EVIDENCE"
done
require_anchor 'R-DHCD-002 | P0 | reverse-owner / queued' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md"
require_anchor 'Quick UI, the `ReCalcTimeScale` sink identity, modal input lock, global simulation scope, timer effects, FIFO, and cross-player serialization remain unresolved' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"

printf 'US-P0-007 fail-closed modal-queue evidence verified.\n'
