#!/usr/bin/env bash
set -euo pipefail

readonly DHCD_ROOT="/home/zet/Projects/dhcd"
readonly CPP2IL="$DHCD_ROOT/tools/cpp2il-linux-x64/Cpp2IL"
readonly APK="/var/www/dhcd/localization_vi/output/apktool_clean_from_full"
readonly NATIVE="$APK/lib/arm64-v8a/libil2cpp.so"
readonly METADATA="$APK/assets/bin/Data/Managed/Metadata/global-metadata.dat"
readonly GLOBAL_MANAGERS="$APK/assets/bin/Data/globalgamemanagers"
readonly OUTPUT_ROOT="/tmp/inspect-r-dhcd-004-isil"
readonly OUTPUT_DIR="$OUTPUT_ROOT/IsilDump/BattleCore/BattleCore"
readonly LOCK_FILE="/tmp/prepare-r-dhcd-004-isil.lock"

check_hash() { printf '%s  %s\n' "$1" "$2" | sha256sum -c - >/dev/null; }

outputs_ready() {
  test -f "$OUTPUT_DIR/ActorEntity.txt" && test ! -L "$OUTPUT_DIR/ActorEntity.txt" &&
    test -f "$OUTPUT_DIR/LevelItemMgr.txt" && test ! -L "$OUTPUT_DIR/LevelItemMgr.txt" &&
    test -f "$OUTPUT_DIR/WaveRefresh.txt" && test ! -L "$OUTPUT_DIR/WaveRefresh.txt" &&
    test -f "$OUTPUT_DIR/ActorEntityCreateData.txt" && test ! -L "$OUTPUT_DIR/ActorEntityCreateData.txt" &&
    check_hash ca7798f926210419bb1b3aace650613b0470ed409caf76a82b16dcaafc617f16 "$OUTPUT_DIR/ActorEntity.txt" &&
    check_hash c47699da7e3907da43fba0f02e01caeb3c688eed1486661d1c05141517131d21 "$OUTPUT_DIR/LevelItemMgr.txt" &&
    check_hash 3734ca034dbf88a1281a71bbab16707b6ac40445adab1c7584e355dfe8d1e31b "$OUTPUT_DIR/WaveRefresh.txt" &&
    check_hash da56cbf4438743f935fec00a8d4da004a55b360df81a27c68598e59965b705a1 "$OUTPUT_DIR/ActorEntityCreateData.txt"
}

for file in "$CPP2IL" "$NATIVE" "$METADATA" "$GLOBAL_MANAGERS"; do
  test -f "$file"
  test ! -L "$file"
done
check_hash f4c040b55b33a76a9e437d85d525e3fc40e88572564be1205e1e743f7ca8ef5f "$CPP2IL"
check_hash 130d09d3b1cdc57ad12eee96d77b6db9665b22dfd40312d4f750fe6f93caabe8 "$NATIVE"
check_hash 459503b7d16ab3ae95190a180fc5bb3a361b7bb8e39651eb583725bf0888c2f4 "$METADATA"
check_hash 0775ec6d827780b4e2f52ae9ff7d7a65543cdc862b3db091227ce996bcda0e5e "$GLOBAL_MANAGERS"

exec 9>"$LOCK_FILE"
flock 9
if outputs_ready; then
  printf 'R-DHCD-004 generated ISIL ready.\n'
  exit 0
fi

stage="$(mktemp -d /tmp/r-dhcd-004-isil.XXXXXX)"
log="$stage/cpp2il.log"
trap 'rm -rf "$stage"' EXIT

if ! (
  cd "$DHCD_ROOT"
  NO_COLOR=1 "$CPP2IL" \
    --force-binary-path "$NATIVE" \
    --force-metadata-path "$METADATA" \
    --force-unity-version 2020.3.21f1 \
    --output-as isil \
    --output-to "$stage/output"
) >"$log" 2>&1; then
  tail -n 80 "$log" >&2
  exit 1
fi

readonly STAGED="$stage/output/IsilDump/BattleCore/BattleCore"
check_hash ca7798f926210419bb1b3aace650613b0470ed409caf76a82b16dcaafc617f16 "$STAGED/ActorEntity.txt"
check_hash c47699da7e3907da43fba0f02e01caeb3c688eed1486661d1c05141517131d21 "$STAGED/LevelItemMgr.txt"
check_hash 3734ca034dbf88a1281a71bbab16707b6ac40445adab1c7584e355dfe8d1e31b "$STAGED/WaveRefresh.txt"
check_hash da56cbf4438743f935fec00a8d4da004a55b360df81a27c68598e59965b705a1 "$STAGED/ActorEntityCreateData.txt"

mkdir -p "$OUTPUT_DIR"
install -m 0644 "$STAGED/ActorEntity.txt" "$OUTPUT_DIR/ActorEntity.txt"
install -m 0644 "$STAGED/LevelItemMgr.txt" "$OUTPUT_DIR/LevelItemMgr.txt"
install -m 0644 "$STAGED/WaveRefresh.txt" "$OUTPUT_DIR/WaveRefresh.txt"
install -m 0644 "$STAGED/ActorEntityCreateData.txt" "$OUTPUT_DIR/ActorEntityCreateData.txt"
outputs_ready
printf 'R-DHCD-004 generated ISIL reproduced from pinned inputs.\n'
