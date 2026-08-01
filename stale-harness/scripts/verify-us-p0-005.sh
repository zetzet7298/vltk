#!/usr/bin/env bash
set -euo pipefail

readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-005-dhcd-pause-timescale-recovery"
readonly EVIDENCE="/home/zet/Projects/dhcd/docs/evidence/r-dhcd-003-pause-timescale.md"
readonly APK="/var/www/dhcd/localization_vi/output/apktool_clean_from_full"
readonly DHCD="/home/zet/Projects/dhcd"

for file in "$PACKET/overview.md" "$PACKET/design.md" "$PACKET/execplan.md" "$PACKET/validation.md" "$EVIDENCE"; do
  test -f "$file"
  test ! -L "$file"
done

require_anchor() {
  rg -Fq -- "$1" "$2" || { printf 'Missing anchor %q in %s\n' "$1" "$2" >&2; exit 1; }
}
check_hash() { printf '%s  %s\n' "$1" "$2" | sha256sum -c - >/dev/null; }

check_hash 130d09d3b1cdc57ad12eee96d77b6db9665b22dfd40312d4f750fe6f93caabe8 "$APK/lib/arm64-v8a/libil2cpp.so"
check_hash 459503b7d16ab3ae95190a180fc5bb3a361b7bb8e39651eb583725bf0888c2f4 "$APK/assets/bin/Data/Managed/Metadata/global-metadata.dat"
check_hash 9d918d6dfa4c36d05400ec0ef226897f014ab74fff43c663fb8e9dd873744522 "$DHCD/il2cpp/diffable-cs/DiffableCs/GameLogic/A5Game/BattleSys.cs"
check_hash 19c7cdc6ce2312bccec05177629d57b0d4af35e4e5b3f080a9dd80e7163a8d61 "$DHCD/il2cpp/diffable-cs/DiffableCs/GameLogic/A5Game/QuickNewLevelUpRandomSkillUI.cs"
check_hash d8880ed361ab20c9d885a7e38364a8305cd38e08e9f944c8b53b497f23c28dae "$DHCD/il2cpp/diffable-cs/DiffableCs/GameLogic/A5Game/NewLevelUpRandomSkillUI.cs"
check_hash d6c80b59ebd243a74d03e02d5c556fb238584219640b3ee18f8f81f099007eac "$DHCD/il2cpp/isil-r-dhcd-001/BattleCore.LevelRandomSkillCtrl.txt"
check_hash f4c040b55b33a76a9e437d85d525e3fc40e88572564be1205e1e743f7ca8ef5f "$DHCD/tools/cpp2il-linux-x64/Cpp2IL"
check_hash 471242cc69af3a4fc50e36409f8ae7b8a6bb689a802783f5b6587b7f83ffe16c "$DHCD/tools/inspect-r-dhcd-003-pause.py"
python3 "$DHCD/tools/inspect-r-dhcd-003-pause.py" >/dev/null

for anchor in 'Status: `unresolved`' 'Do **not** implement or claim' 'role-keyed pending-event evidence' 'tooling gap rather than proof' 'Quick UI pause acquisition/release is unresolved' 'exactly **45 direct' 'has **exactly one direct caller**' 'This proves `OnHidden` does not directly' 'terminating in an indirect `br x2`' 'not individually named** here'; do
  require_anchor "$anchor" "$EVIDENCE"
done
require_anchor 'R-DHCD-003 | P0 | reverse-owner / in_progress' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md"
require_anchor 'Quick UI, the `ReCalcTimeScale` sink identity, modal input lock, global simulation scope, timer effects, FIFO, and cross-player serialization remain unresolved' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"

if rg -Fq 'Status: `complete`' "$EVIDENCE"; then
  printf 'Pause evidence attempts unsupported closure.\n' >&2
  exit 1
fi

printf 'US-P0-005 fail-closed pause/timeScale evidence verified.\n'
