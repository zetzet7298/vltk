#!/usr/bin/env bash
set -euo pipefail

# This verifies a fail-closed reverse packet; it does not approve gameplay values.
readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET_ROOT="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-004-dhcd-card-economy-recovery"
readonly REVERSE_SKILL="$HARNESS_ROOT/.agents/skills/reverse-engineering"
readonly REVERSE_SKILL_VALIDATOR="/home/zet/.codex/skills/.system/skill-creator/scripts/quick_validate.py"
readonly QUEUE="$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md"
readonly UNRESOLVED="$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"
readonly DHCD_ROOT="/home/zet/Projects/dhcd"
  readonly EVIDENCE="$DHCD_ROOT/docs/evidence/r-dhcd-001-card-economy.md"
  readonly ACTIVE_CONFIG_EVIDENCE="$DHCD_ROOT/docs/evidence/r-dhcd-001-active-config-selection.md"
  readonly BUILD_KEY_EVIDENCE="$DHCD_ROOT/docs/evidence/r-dhcd-001-build-key.md"
readonly FAILED="$DHCD_ROOT/docs/evidence/r-dhcd-001-failed-methods.md"
readonly INSPECTOR="$DHCD_ROOT/tools/inspect-r-dhcd-001-fp-randomskill.py"
readonly DECODER_PROBE="$DHCD_ROOT/tools/decode-r-dhcd-001-randomskill.py"
readonly SERVER_INSPECTOR="$DHCD_ROOT/tools/inspect-r-dhcd-001-server-randomskill.py"
readonly NATIVE_MAPPER="$DHCD_ROOT/tools/inspect-r-dhcd-001-native-method-map.py"
  readonly SELECTOR_INSPECTOR="$DHCD_ROOT/tools/inspect-r-dhcd-001-selector-wrapper.py"
  readonly RNG_CLOSURE_INSPECTOR="$DHCD_ROOT/tools/inspect-r-dhcd-001-rng-closure.py"
  readonly BUILD_KEY_PROBE="$DHCD_ROOT/tools/probe-r-dhcd-001-build-key-artifacts.py"
readonly IOS_INSPECTOR="$DHCD_ROOT/tools/inspect-r-dhcd-001-ios-randomskill.py"
  readonly ISIL_ROOT="$DHCD_ROOT/il2cpp/isil-r-dhcd-001"
  readonly RESBIN_ISIL="$ISIL_ROOT/BattleCore.ResBinUtil.txt"
readonly ASSET_ROOT="/var/www/dhcd/localization_vi/output/apktool_clean_from_full/assets/ab"
readonly ROOT_CATALOG="$ASSET_ROOT/root.bytes"
readonly INDEX_6="$ASSET_ROOT/index_6.bytes"
readonly INDEX_9="$ASSET_ROOT/index_9.bytes"
readonly FP_BUNDLE="$ASSET_ROOT/assets_resources_config_resbin_fp_randomskillconfig.bytes.ab"
readonly NON_FP_BUNDLE="$ASSET_ROOT/assets_resources_config_resbin_randomskillconfig.bytes.ab"
readonly ENC_LIST="$ASSET_ROOT/enc_list.bytes"
readonly CAB_LIST="$ASSET_ROOT/cab_list.bytes"
readonly BOOTSTRAP="/var/www/dhcd/localization_vi/output/apktool_clean_from_full/smali/com/dodjoy/dodlib/AbKeyBootstrap.smali"
readonly SERVER_KEY="/var/www/dhcd/服务端/extracted_server/data/baozou_web_zs/ver/ab_key.php"
readonly RUNTIME_LOG="/var/www/dhcd/localization_vi/output/manual_after_agree_logcat.txt"
readonly CENTER_CANDIDATE="/var/www/dhcd/服务端/extracted_server/data/home/mmog/center_server_900/runenv/match_svr/cfg/res/RandomSkillConfig.bin"
readonly BATTLE_FP_CANDIDATE="/var/www/dhcd/服务端/extracted_server/data/home/mmog/battle_server_999/runenv/battle_svr/cfg/battle_data/ResBin_fp/RandomSkillConfig.bin"
readonly PATCH_TOOL="/var/www/dhcd/localization_vi/tools/patch_client_assetbundle_full.py"
readonly IOS_ROOT="/var/www/dhcd/服务端/extracted_server/data/webios/ios351"
readonly IOS_INDEX_6="$IOS_ROOT/index_6.bytes"
readonly IOS_INDEX_9="$IOS_ROOT/index_9.bytes"
readonly IOS_FP_BUNDLE="$IOS_ROOT/assets_resources_config_resbin_fp_randomskillconfig.bytes.ab"
readonly IOS_NON_FP_BUNDLE="$IOS_ROOT/assets_resources_config_resbin_randomskillconfig.bytes.ab"

for file in \
  "$PACKET_ROOT/overview.md" \
  "$PACKET_ROOT/design.md" \
  "$PACKET_ROOT/execplan.md" \
    "$PACKET_ROOT/validation.md" \
    "$REVERSE_SKILL/SKILL.md" \
    "$REVERSE_SKILL/agents/openai.yaml" \
    "$REVERSE_SKILL_VALIDATOR" \
  "$QUEUE" \
  "$UNRESOLVED" \
      "$EVIDENCE" \
      "$ACTIVE_CONFIG_EVIDENCE" \
      "$BUILD_KEY_EVIDENCE" \
    "$FAILED" \
    "$INSPECTOR" \
    "$DECODER_PROBE" \
      "$SERVER_INSPECTOR" \
      "$NATIVE_MAPPER" \
      "$SELECTOR_INSPECTOR" \
      "$RNG_CLOSURE_INSPECTOR" \
      "$BUILD_KEY_PROBE" \
      "$IOS_INSPECTOR" \
    "$ISIL_ROOT/DodGame.AssetBundleUtil.txt" \
    "$ISIL_ROOT/FastXXTEA.txt" \
    "$ISIL_ROOT/DodGame.AssetBundleEncMgr.txt" \
      "$ISIL_ROOT/BattleCore.LevelRandomSkillCtrl.txt" \
      "$ISIL_ROOT/BattleCore.SkillConfigMgr.txt" \
    "$ROOT_CATALOG" \
    "$INDEX_6" \
    "$INDEX_9" \
    "$FP_BUNDLE" \
    "$NON_FP_BUNDLE" \
    "$ENC_LIST" \
    "$CAB_LIST" \
    "$BOOTSTRAP" \
    "$SERVER_KEY" \
    "$RUNTIME_LOG" \
    "$CENTER_CANDIDATE" \
      "$BATTLE_FP_CANDIDATE" \
      "$PATCH_TOOL" \
      "$IOS_INDEX_6" \
      "$IOS_INDEX_9" \
      "$IOS_FP_BUNDLE" \
      "$IOS_NON_FP_BUNDLE"; do
  test -f "$file"
  test ! -L "$file"
done

python3 "$REVERSE_SKILL_VALIDATOR" "$REVERSE_SKILL" >/dev/null
require_skill_anchor() {
  local anchor=$1

  if ! rg -Fq -- "$anchor" "$REVERSE_SKILL/SKILL.md"; then
    printf 'Missing reverse skill anchor %q\n' "$anchor" >&2
    exit 1
  fi
}

for anchor in \
  '/var/www/reverse-skill/skills/routing.md' \
  '`proven`' \
  '`high-confidence reconstruction`' \
  '`product decision`' \
  'Never describe a reconstruction or product decision as exact DHCD'; do
  require_skill_anchor "$anchor"
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
  '`REQ-P0-005`' \
  'US-P0-001 -> US-P0-004' \
  'Exact values recovered: serialized candidate rows only, not active semantics.' \
  '`US-P0-004` and `R-DHCD-001` remain `in_progress`' \
  'No invented card count, weight curve, price, cost, or cap.'; do
  require_anchor "$anchor" "$PACKET_ROOT"
done

for anchor in \
  '| Status / confidence | `unresolved` |' \
    '| Exact values recovered | serialized Android/server candidate rows only; active/runtime semantics unresolved |' \
  'Android asset root declares version `1.304`' \
  '`fp_` in `index_6` and non-`fp` in `index_9`' \
  '2,023 big-endian 22-byte rows' \
  '`CanRepeatSelect` is `1` in all 2,023 candidate rows' \
  'is the serialized row count, not an offer/card/deck count' \
  '`0x252CBC8`' \
  '`0x273D998`' \
    'reads all 5,082' \
  '`LevelUpRandomWeight`' \
  '`FirstLevelRandomSkillWeight`' \
    '`CanRepeatSelect`' \
    'does not prove maximum copies' \
        'method-definition indices are recovered' \
    '`DecodeAb -> FastXXTEA.Dexx`' \
    '`0xa0004ed5`' \
    '`cbf70c8bebb0ea00d46b8054abef94db`' \
    '`c70b77ae6f6102036edec93aaeb740d3`' \
    'not treated as a plaintext oracle' \
    '1,879 center rows are an exact subset' \
      'The 144' \
      'failed-key environment' \
      '`DODAB1`' \
        'slots `4182` through `4207`' \
        '`0x00b5b808` as `Math.Min(candidateCount, randomCount)`' \
        '`0x01542908` returns the collection' \
      '15 direct' \
          'context slot loads at `+8` and `+0x10`' \
          '`[0x015429fc,0x01543000)`' \
          '`0x00c5b018`' \
          '`0x00c50664`' \
          'high-confidence reconstruction of weighted selection with per-item replacement'; do
    require_anchor "$anchor" "$EVIDENCE"
  done

  for anchor in \
    'E-DHCD-R001-active-config-selection' \
    '**Unresolved; no winner selected.**' \
    'Therefore neither the' \
    'path is an active-config winner.'; do
    require_anchor "$anchor" "$ACTIVE_CONFIG_EVIDENCE"
  done

  for anchor in \
    'R-DHCD-001 Android 1.304 Build/Server-Key Recovery' \
    'Status: `blocked`' \
    '"apk_count": 14' \
    'Thus no local packaged build supplies' \
    'direct server-key response.'; do
    require_anchor "$anchor" "$BUILD_KEY_EVIDENCE"
  done

for anchor in \
  'Could not find type definition' \
  'Invalid IL' \
    '`Method not found`' \
      'direct pointer-value matching is required' \
        'All 26 `LevelRandomSkillCtrl` methods match slots `4182`-`4207`' \
          'Generic registration resolves wrapper `0x01542908` to definition `24133`' \
          '1,540-byte pre-`RET` body hash' \
          'selection body `0x015429fc` to definition `24134`' \
            '`TSRandom.Next(FP, FP)`' \
            'TSRandom.NextFP` endpoint/distribution' \
        'Inspect iOS `1.351` DODAB1 candidates' \
    'Recover native decoder' \
    'Probe non-`fp` with embedded key' \
    '`md5_ex` is not a decoded/plaintext hash' \
    'failed-key environment' \
    'no active card economy semantic was recovered.'; do
  require_anchor "$anchor" "$FAILED"
done

  require_anchor 'R-DHCD-001 | P0 | reverse-owner / in_progress' "$QUEUE"
  require_anchor '`0x01542908`' "$QUEUE"
  require_anchor '`0x015429fc`' "$QUEUE"
  require_anchor '`0x00c4fba0`/`0x00cdf598`' "$QUEUE"
  require_anchor 'active config/weight binding, offer count, cost, and cap remain unresolved' "$UNRESOLVED"
  require_anchor 'remaining-count loop' "$UNRESOLVED"
  require_anchor '`Math.Min` clamping' "$UNRESOLVED"

check_hash() {
  local expected=$1
  local path=$2
  printf '%s  %s\n' "$expected" "$path" | sha256sum -c - >/dev/null
}

check_hash 6dc46223cd3c7f8517683587448ef1fd1a27ce180930b300723726b0074a8fbf \
  "$DHCD_ROOT/reconstructed-types/BattleCore/BattleCore.LevelRandomSkillCtrl.cs"
check_hash 7a0d40ecf883209e9d3d36904ec86ee75419e0e5375796b886ada7b77feae67b \
  "$DHCD_ROOT/reconstructed-types/GameLogic/A5Game.BattleLearnSkillCtrl.cs"
check_hash c6caa86efc094e2a6f01f8dec513e77a5b77885394d867429364c277e36ae446 \
  "$DHCD_ROOT/reconstructed-types/BattleCore/BattleCore.RandomSkillConfig.cs"
check_hash 264d8b9a8b883b1f14f017fad8bca31345334dcc021d4aee9a3b344f642b1314 \
  "$DHCD_ROOT/il2cpp/dll-il-recovery/BattleCore.dll"
check_hash 1c1383090c073027d9aa05c0a518662548ae78fd5f0463d46f0629652d82e3c5 \
  "$ROOT_CATALOG"
check_hash abd71cb63abeca97dbcfeb71930b31ea59b921383304086d6661c00129b48018 \
  "$INDEX_6"
check_hash c6f0d8b618c7a52a5ffe97e8b70caf8a2a7a12b8cf0ee9eebb2296f60428d989 \
  "$INDEX_9"
check_hash 179c26d6ad4837b2c6aee7dabd4ae6de6ebb76f5066247fba5210dcf5b751acb \
  "$FP_BUNDLE"
check_hash d7a704b40df8c3d735db1b3c8ddc391c47ad4273b03ca7c354ade905354d189b \
  "$NON_FP_BUNDLE"
check_hash 0c31192928beee5d42bd866a65b56a783ac37b05be131e6dc1203cb6940fe08c \
  "$INSPECTOR"
check_hash 180118f7bfa5b4a2f69bfc6eb23a585e4fb6a403fdb574a6f068f83f2b1d95e9 \
  "$DECODER_PROBE"
  check_hash 25f8886194f7ae9ddb34c8a975ec62e9f271af765b2d2a2538beca1f27ec249f \
    "$SERVER_INSPECTOR"
  check_hash 3979879597220ca3866cc91bb8c3a75c10d8d22f273e8ac23d338c24af3d2a0f \
    "$NATIVE_MAPPER"
  check_hash cfd30d4eb6129bc1b8bdd2c69264aa19e767ec274fb8b6f764b52f9b79870626 \
        "$SELECTOR_INSPECTOR"
  check_hash 62de6c0e479f33765e50603720949d067c74c30c25bdedc31962f7da39901457 \
    "$BUILD_KEY_PROBE"
  check_hash 51af4a704f3e2cc8ccc637915b5736fee35c350c3ab71b0135b91c5820d7509e \
    "$IOS_INSPECTOR"
check_hash 92ba22946e5f0de15ea73860869c0b5406a080440f2365bfb8d925bea5aa21b5 \
  "$ISIL_ROOT/DodGame.AssetBundleUtil.txt"
check_hash 69a3f76c18e16f922f24290c349eb34cac87e602f5db8f549e9d99c7cab1a2f3 \
  "$ISIL_ROOT/FastXXTEA.txt"
check_hash a183b04846407b234a7eee79a16f3a409f5ca53e03e31ff0a794ac098c8eba23 \
  "$ISIL_ROOT/DodGame.AssetBundleEncMgr.txt"
  check_hash d6c80b59ebd243a74d03e02d5c556fb238584219640b3ee18f8f81f099007eac \
    "$ISIL_ROOT/BattleCore.LevelRandomSkillCtrl.txt"
  check_hash 97b7645a2f119a9d45fbc305100da9b0d088a8a61825d9bd1de8dd2151d40b1b \
      "$ISIL_ROOT/BattleCore.SkillConfigMgr.txt"
  check_hash ff79785aee75f0c036f0c51db9e55673b0062e1450914ac92bd974fc95167813 \
    "$RESBIN_ISIL"
check_hash 4c5724de9dedd24b7046c0507bc2390676d807e5f1270b8f17b01adf16911775 \
  "$ENC_LIST"
check_hash b55c06ff6c85438eb441d4e6ac31df45af07ddaf934f6e0a3ebe741468b970a4 \
  "$CAB_LIST"
check_hash 82d43b53b8085d13d720b866f4703420ba774884cae6dcf61f211195f3aa7e09 \
  "$BOOTSTRAP"
check_hash 358f77fca7496aa3e2aadce099860925e6ed581d5c1fc551f98e29042ba5c16d \
  "$SERVER_KEY"
check_hash e945f3bc93feb7b56a028c0e848f0fb679e6795cc0d5593b89880374358f0e62 \
  "$RUNTIME_LOG"
check_hash de08a06bda71b813d06a6a6990eadb05612a95d05503056ddae2fceaf53c3b50 \
  "$CENTER_CANDIDATE"
check_hash 2ebb397f26f740d0f63230d1fc51032d08b19a9ede7b240c26552cbb18f7442d \
  "$BATTLE_FP_CANDIDATE"
  check_hash c2005ac4cd3cc3f74c7cedd07412fd5c2375b39f212d152b09879ed0d8dac9d5 \
    "$PATCH_TOOL"
  check_hash 664188d001294751e545b4e0fa6a5be8d79653fb45836098f8752ba7f5babf68 \
    "$IOS_INDEX_6"
  check_hash 5c91acec11a9fa8897833b169e1bb38ba6ed2459b34d375e57091af514a9187a \
    "$IOS_INDEX_9"
  check_hash 8caf3000d0060c7c47bcd6cd3f0feb19dc631359d1fecfba59704c352bd2b889 \
    "$IOS_FP_BUNDLE"
  check_hash 1433e749d7530311f85ea78622c449da0dd84c3a6bdb9bfa9060b4888819961c \
    "$IOS_NON_FP_BUNDLE"

sha256sum -c "$DHCD_ROOT/input/manifest.sha256" >/dev/null

inspector_output="$(python3 "$INSPECTOR")"
jq -e '
  .artifact_role == "serialized candidate; active runtime selection unresolved" and
  .text_asset_name == "RandomSkillConfig" and
  .text_asset_bytes == 44526 and
  .header.tag == "0x2def" and
  .header.version == 1 and
  .header.row_count == 2023 and
  .header.stored_crc32_complement == "0xd158a528" and
  .header.calculated_crc32_complement == "0xd158a528" and
  .row_schema == ">IIIiiBB" and
  .row_bytes == 22 and
  .aggregates.can_repeat_select_counts["1"] == 2023 and
  .aggregates.depends_on_handbook_counts["0"] == 1406 and
  .aggregates.depends_on_handbook_counts["1"] == 617
  ' <<<"$inspector_output" >/dev/null

server_output="$(python3 "$SERVER_INSPECTOR")"
jq -e '
  .artifact_role == "serialized server candidates; Android active selection unresolved" and
  .row_schema == ">IIIiiBB" and
  .row_bytes == 22 and
  .candidates[0].name == "center_900_non_fp" and
  .candidates[0].header.row_count == 1879 and
  .candidates[0].header.stored_crc32_complement == "0xd9e6b708" and
  .candidates[0].aggregates.can_repeat_select_counts["1"] == 1879 and
  .candidates[1].name == "battle_999_fp" and
  .candidates[1].header.row_count == 2023 and
  .comparison.center_is_exact_subset_of_fp == true and
  .comparison.shared_rows == 1879 and
  .comparison.center_only_rows == 0 and
  .comparison.fp_only_rows == 144 and
  .comparison.fp_only_pool_counts["999"] == 144 and
  .comparison.fp_only_level_up_weight_counts["200"] == 144 and
  .comparison.fp_only_first_level_weight_counts["0"] == 144 and
  .comparison.fp_only_can_repeat_select_counts["1"] == 144 and
  .comparison.fp_only_depends_on_handbook_counts["1"] == 144 and
  .android_fp.byte_identical_to_battle_999_fp == true and
  .candidate_bytes_are_distinct == true
  ' <<<"$server_output" >/dev/null

native_map_output="$(python3 "$NATIVE_MAPPER")"
jq -e '
  .artifact_role == "named ISIL method to BattleCore pointer-slot mapping; card economy semantics remain unresolved" and
  .pointer_table.virtual_address == "0x273d998" and
  .pointer_table.file_offset == "0x272d998" and
  .pointer_table.entry_count == 5082 and
  .metadata_method_definition_indices_recovered == false and
  (.level_random_skill_ctrl | length) == 26 and
  (.level_random_skill_ctrl | all(.unique_pointer_match == true)) and
  (.level_random_skill_ctrl[] | select(.method == "RequestRandomSkill") | .pointer_slot) == 4188 and
  (.level_random_skill_ctrl[] | select(.method == "GetLvUpRandomSkillParam") | .pointer_slot) == 4191 and
  (.level_random_skill_ctrl[] | select(.method == "GetRandomSkillShopParam") | .pointer_slot) == 4195 and
  (.level_random_skill_ctrl[] | select(.method == "RandomLibraryListToParam") | .pointer_slot) == 4200 and
  (.level_random_skill_ctrl[] | select(.method == "SpecialLevelRandomSkillParam") | .pointer_slot) == 4201 and
  (.skill_config_mgr[] | select(.method == "GetRandomSkillLibrarysById") | .pointer_slot) == 2587 and
  (.skill_config_mgr[] | select(.method == "GetRandomSkillCfgList") | .pointer_slot) == 2588
  ' <<<"$native_map_output" >/dev/null

    selector_output="$(python3 "$SELECTOR_INSPECTOR")"
  jq -e '
    .artifact_role == "shared/generic selected-collection wrapper plus weight-selection body; RNG boundary, active config, count, cost, and cap unresolved" and
    .function.virtual_address == "0x01542908" and
  .function.bytes == 244 and
  .function.sha256 == "e4ea42e8a1eb55f530c3e361a79dd5bdae38cb2ccfdfd13c3192f0b44431c1ef" and
  .function.context_slot_offsets == [8, 16] and
  .function.indirect_call_addresses == ["0x01542990", "0x015429dc"] and
  .function.direct_caller_count == 15 and
  (.function.direct_callers | index("0x01661018")) != null and
  (.function.direct_callers | index("0x01662024")) != null and
  .battle_core_pointer_table.entry_count == 5082 and
  .battle_core_pointer_table.matching_slots == [] and
  .generic_registration.generic_method_pointer_count == 20150 and
  .generic_registration.generic_method_pointer_index == 19781 and
  .generic_registration.method_definition.method_definition_index == 24133 and
  .generic_registration.method_definition.declaring_type == "BattleCore.LevelBootyHelper" and
  .generic_registration.method_definition.method_name == "RandomItem" and
  .generic_registration.method_definition.token == "0x06000ed2" and
  .generic_registration.random_skill_method_spec_index == 36836 and
  .generic_registration.random_skill_generic_inst_index == 3447 and
    .generic_registration.random_skill_type == "BattleCore.RandomSkillConfig" and
    (.generic_registration.matching_rows | length) == 9 and
    .selection_body.virtual_address == "0x015429fc" and
    .selection_body.end_exclusive_before_return == "0x01543000" and
    .selection_body.bytes_before_return == 1540 and
    .selection_body.sha256_before_return == "0ce8defce09b25672082551cddfcc7d353e89d98721310e828e76e86ac727b36" and
    .selection_body.normal_return_address == "0x01543000" and
    .selection_body.exception_helper_address == "0x01543004" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542ccc") | .target) == "0x00c5b018" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542de4") | .target) == "0x00c5b018" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542df4") | .target) == "0x00c50664" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542f50") | .target) == "0x01c14418" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542fcc") | .target) == "0x01c14418" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542cbc") | .target) == "0x00c4fba0" and
    (.selection_body.direct_calls[] | select(.call_site == "0x01542ce4") | .target) == "0x00cdf598" and
    (.selection_body.helper_bodies[] | select(.virtual_address == "0x00c5b018") |
      .bytes == 8 and .hex == "007c60d3c0035fd6" and
      .sha256 == "f69f807f92d8b958cab3f922d9bdfeb3cc088dcaed4d1a1b56f3ed5e49fa4c12") and
    (.selection_body.helper_bodies[] | select(.virtual_address == "0x00c50664") |
      .bytes == 12 and .hex == "1f0001ebe0a79f1ac0035fd6" and
      .sha256 == "82e66055e550730a047f1a020db2ec83799b9728ed8181b40a93c2952a7a4071") and
    .selection_body.weight_pass_indirect_call == "0x01542b88" and
    .selection_body.cumulative_weight_add == "0x01542bc0" and
    .selection_body.weighted_hit_comparison_call == "0x01542df4" and
    .selection_body.put_back_predicate_indirect_call == "0x01542e68" and
    .selection_body.put_back_true_target == "0x01542fd0" and
    .selection_body.downstream_weight_subtract == "0x01542f48" and
    .selection_body.weight_table_update_calls == ["0x01542f50", "0x01542fcc"] and
      .selection_body.threshold_path_calls == ["0x00c4fba0", "0x00cdf598"] and
      .selection_body.threshold_call_register_setup.fp_zero_conversion.call_site == "0x01542cbc" and
      .selection_body.threshold_call_register_setup.fp_zero_conversion.x0 == "0 (Int32 source)" and
      .selection_body.threshold_call_register_setup.random_fp_range.call_site == "0x01542ce4" and
      .selection_body.threshold_call_register_setup.random_fp_range.x1 == "x27 (FP zero)" and
      .selection_body.threshold_call_register_setup.random_fp_range.x2 == "x0 from 0x01542ccc (FP cumulative total)" and
      (.selection_body.threshold_callees[] | select(.virtual_address == "0x00c4fba0") |
        .end_exclusive == "0x00c4fba8" and .bytes == 8 and
        .sha256 == "f69f807f92d8b958cab3f922d9bdfeb3cc088dcaed4d1a1b56f3ed5e49fa4c12" and
        .pointer_slot == 34 and .method_definition.method_definition_index == 17615 and
        .method_definition.declaring_type == "BattleCore.FP" and
        .method_definition.method_name == "op_Implicit" and .method_definition.token == "0x06000023") and
      (.selection_body.threshold_callees[] | select(.virtual_address == "0x00cdf598") |
        .end_exclusive == "0x00cdf6d0" and .bytes == 312 and
        .sha256 == "683987e19a403274e53c2855f6bd2ce8be79ecaf42603146f3cc5ada278b7c7c" and
        .pointer_slot == 96 and .method_definition.method_definition_index == 17677 and
        .method_definition.declaring_type == "BattleCore.TSRandom" and
        .method_definition.method_name == "Next" and .method_definition.token == "0x06000061" and
        .method_definition.parameter_count == 2) and
      .selection_body.threshold_distribution_boundary_recovered == false and
    .selection_generic_registration.generic_method_pointer_index == 19782 and
    .selection_generic_registration.method_definition.method_definition_index == 24134 and
    .selection_generic_registration.method_definition.declaring_type == "BattleCore.LevelBootyHelper" and
    .selection_generic_registration.method_definition.method_name == "RandomItem" and
    .selection_generic_registration.method_definition.token == "0x06000ed3" and
    .selection_generic_registration.method_definition.parameter_count == 6 and
    (.selection_generic_registration.matching_rows | length) == 2 and
    (.selection_generic_registration.matching_rows | map(.row_index)) == [35449, 35450] and
    (.selection_generic_registration.matching_rows | map(.method_spec_index)) == [36843, 36844] and
    .wrapper_contains_direct_rng_weight_or_duplicate_rule == false and
    .selection_weight_accumulation_recovered == true and
    .selection_put_back_control_flow_recovered == true and
    .selection_rng_boundary_semantics_recovered == false and
    .active_runtime_config_recovered == false and
    .count_cost_cap_recovered == false
      ' <<<"$selector_output" >/dev/null

  rng_closure_output="$(python3 "$RNG_CLOSURE_INSPECTOR")"
  jq -e '
    .artifact_role == "hash-locked static RNG closure; no endpoint, distribution, or runtime-parity claim" and
    .inputs.native_sha256 == "130d09d3b1cdc57ad12eee96d77b6db9665b22dfd40312d4f750fe6f93caabe8" and
    .inputs.metadata_sha256 == "459503b7d16ab3ae95190a180fc5bb3a361b7bb8e39651eb583725bf0888c2f4" and
    .method.codegen_pointer_slot == 96 and .method.method_definition_index == 17677 and
    .method.token == "0x06000061" and .method.target == "[0x00cdf598,0x00cdf6d0)" and
    .method.sha256 == "683987e19a403274e53c2855f6bd2ce8be79ecaf42603146f3cc5ada278b7c7c" and
    .direct_call_targets["0x01542ce4"] == "0x00cdf598" and
    .raw_instruction_words["0x00cdf710"] == "137c0153" and
    .raw_instruction_words["0x00cdf740"] == "e07b0032" and
    .raw_static_edges.caller_strict_comparison_call_site == "0x01542df4" and
    .active_config_recovered == false and .count_cost_cap_recovered == false and
    .endpoint_reachability_recovered == false and .distribution_recovered == false and
    .runtime_parity_recovered == false
  ' <<<"$rng_closure_output" >/dev/null

ios_output="$(python3 "$IOS_INSPECTOR")"
jq -e '
  .artifact_role == "iOS 1.351 packaged candidates; active selection and DODAB1 crypto/compression semantics unresolved" and
  (.candidates | length) == 2 and
  (.candidates | all(
    .header.magic == "DODAB1" and
    .header.version == 2 and
    .header.label == "ios-config-20260508" and
    .header.payload_offset == 51 and
    .header.bytes_after_header_minus_declared_size == 48 and
    .header.file_bytes_minus_declared_size == 99 and
    .catalog_md5_equals_wrapper_md5 == false
  )) and
  (.candidates[] | select(.name == "ios351_fp_randomskill") |
    .asset_bytes == 2932 and .header.declared_size_le_u64 == 2833 and .catalog.size == 2833) and
  (.candidates[] | select(.name == "ios351_non_fp_randomskill") |
    .asset_bytes == 2937 and .header.declared_size_le_u64 == 2838 and .catalog.size == 2838)
  ' <<<"$ios_output" >/dev/null

if decoder_output="$(python3 "$DECODER_PROBE" 2>&1)"; then
  printf 'Static-key decoder unexpectedly accepted the encrypted candidate.\n' >&2
  exit 1
fi
if ! rg -Fq \
  "decoded asset has unexpected magic: b'\\x17\\x1a\\xe9\\xde\\x97N\\xa4\\x8d'; static_key_result_md5=c70b77ae6f6102036edec93aaeb740d3" \
  <<<"$decoder_output"; then
  printf 'Static-key decoder did not reproduce the recorded fail-closed boundary.\n' >&2
  exit 1
fi

require_anchor 'Method: System.UInt32[] GetEncodeKey()' "$ISIL_ROOT/DodGame.AssetBundleUtil.txt"
require_anchor '0x0180E1AC B 0x180E1B0' "$ISIL_ROOT/DodGame.AssetBundleUtil.txt"
require_anchor 'Method: System.Byte[] Dexx(System.Byte[] textData, System.UInt32[] k)' "$ISIL_ROOT/FastXXTEA.txt"
require_anchor '0x0180E1B0 SUB X31, X31, 0x80' "$ISIL_ROOT/FastXXTEA.txt"
require_anchor 'Method: System.Boolean IsAbFileEncrypt(System.String abFilePath)' "$ISIL_ROOT/DodGame.AssetBundleEncMgr.txt"
require_anchor 'server key prepared, len=20' "$RUNTIME_LOG"
require_anchor "Failed to decompress data for the AssetBundle 'Memory'." "$RUNTIME_LOG"
require_anchor 'DodGame.AssetBundleWWWLoader:LoadEncAssetBudnle(String)' "$RUNTIME_LOG"
require_anchor 'doX%ILyDODme#8X*^#sf' "$BOOTSTRAP"
require_anchor 'doX%ILyDODme#8X*^#sf' "$SERVER_KEY"
require_anchor 'item["md5_ex"] = md5' "$PATCH_TOOL"

jq -e --arg path 'assets_resources_config_resbin_randomskillconfig.bytes.ab' \
  'any(.[]; . == $path)' "$ENC_LIST" >/dev/null
jq -e --arg path 'assets_resources_config_resbin_randomskillconfig.bytes.ab' \
  'all(.[]; . != $path)' "$CAB_LIST" >/dev/null

jq -e --arg path 'assets_resources_config_resbin_fp_randomskillconfig.bytes.ab' '
  any(.list[]; .path == $path and .size == 47603 and
    .md5 == "f1be686672eb2777d3ef0b10e2557a80")
' "$INDEX_6" >/dev/null
jq -e --arg path 'assets_resources_config_resbin_randomskillconfig.bytes.ab' '
  any(.list[]; .path == $path and .size == 47595 and
    .md5 == "2e6367db8b68e65599d310136857de02" and
    .md5_ex == "cbf70c8bebb0ea00d46b8054abef94db")
' "$INDEX_9" >/dev/null

if rg -Fq 'Status: `complete`' "$FAILED" || rg -Fq '| Active runtime winner | `verified` |' "$EVIDENCE"; then
  printf 'Reverse packet attempts to close an unresolved evidence gate.\n' >&2
  exit 1
fi

printf 'US-P0-004 fail-closed DHCD card economy packet verification passed.\n'
