#!/usr/bin/env bash
set -euo pipefail

readonly HARNESS_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
readonly PACKET="$HARNESS_ROOT/docs/stories/epics/E00-p0-foundation/US-P0-006-dhcd-mode-selection-recovery"
readonly EVIDENCE="/home/zet/Projects/dhcd/docs/evidence/r-dhcd-004-mode-selection.md"
readonly NATIVE_INSPECTOR="/home/zet/Projects/dhcd/tools/inspect-r-dhcd-004-mode-selection.py"
readonly PACKAGED_INSPECTOR="/home/zet/Projects/dhcd/tools/inspect-r-dhcd-004-packaged-config.py"
readonly GENERIC_INSPECTOR="/home/zet/Projects/dhcd/tools/inspect-r-dhcd-004-generic-context.py"
readonly ISIL_PREP="$HARNESS_ROOT/scripts/prepare-r-dhcd-004-isil.sh"
readonly ISIL_DURABLE="/home/zet/Projects/dhcd/il2cpp/isil-r-dhcd-004"
readonly ISIL_GENERATED="/tmp/inspect-r-dhcd-004-isil/IsilDump/BattleCore/BattleCore"
readonly AB_DIR="/var/www/dhcd/localization_vi/output/apktool_clean_from_full/assets/ab"

test -x "$ISIL_PREP"
"$ISIL_PREP"

for file in \
  "$PACKET/overview.md" "$PACKET/design.md" "$PACKET/execplan.md" "$PACKET/validation.md" \
  "$EVIDENCE" "$NATIVE_INSPECTOR" "$PACKAGED_INSPECTOR" "$GENERIC_INSPECTOR" \
  "$ISIL_GENERATED/ActorEntity.txt" "$ISIL_GENERATED/LevelItemMgr.txt" \
  "$ISIL_GENERATED/WaveRefresh.txt" "$ISIL_GENERATED/ActorEntityCreateData.txt" \
  "$AB_DIR/assets_resources_config_levelconfig.ab" \
  "$AB_DIR/assets_resources_config_resbin_multiplayerexpressionconfig.bytes.ab" \
  "$AB_DIR/index_0.bytes" "$AB_DIR/index_1.bytes"; do
  test -f "$file"
  test ! -L "$file"
done

require_anchor() {
  rg -Fq -- "$1" "$2" || { printf 'Missing anchor %q in %s\n' "$1" "$2" >&2; exit 1; }
}
check_hash() { printf '%s  %s\n' "$1" "$2" | sha256sum -c - >/dev/null; }

# Canonical inspector + input hashes.
check_hash c987c796f32930192d0a7fe6ce40368319dde746e0f4d3befe05d8ff9f1a8ae3 "$NATIVE_INSPECTOR"
check_hash 620361347fb81383950a33f6533845c03c4a503a51be3cc940ac787c4b2cfdd5 "$PACKAGED_INSPECTOR"
check_hash 32d7caffa900f00bb84d3af838a3b3497ba26e9879a42b6030b794701c421598 "$GENERIC_INSPECTOR"
check_hash 104b06efed76ad5f600f7db27ea7cc1913a7229ddc2cc8bd80c78036a7445f6f "$ISIL_DURABLE/NormalLevelLogic.txt"
check_hash 07233fa3d9fefa3a43432e1dca6ef65e2f9338dea2176720479b07daa88bf9a7 "$ISIL_DURABLE/ActorEntityMgr.txt"
check_hash 85de6bad59208ea816abdf3fa904709b7b8f8f7f06321883cdab9e2943190d9b "$ISIL_DURABLE/CollectItemEntity.txt"
check_hash ca7798f926210419bb1b3aace650613b0470ed409caf76a82b16dcaafc617f16 "$ISIL_GENERATED/ActorEntity.txt"
check_hash c47699da7e3907da43fba0f02e01caeb3c688eed1486661d1c05141517131d21 "$ISIL_GENERATED/LevelItemMgr.txt"
check_hash 3734ca034dbf88a1281a71bbab16707b6ac40445adab1c7584e355dfe8d1e31b "$ISIL_GENERATED/WaveRefresh.txt"
check_hash da56cbf4438743f935fec00a8d4da004a55b360df81a27c68598e59965b705a1 "$ISIL_GENERATED/ActorEntityCreateData.txt"
check_hash df45f88ca9f7dbc169d63165570222906a42d9620ce479208428c9fd7e22390b "$AB_DIR/assets_resources_config_levelconfig.ab"
check_hash 85446e366d4906257453cb7161a582c8e73b202112548ab693f301752aad308b "$AB_DIR/assets_resources_config_resbin_multiplayerexpressionconfig.bytes.ab"
check_hash fd3b14c73fd68db4cd91024e9d91ea5295376e653c01d3cd1f761a271db7fb0e "$AB_DIR/index_0.bytes"
check_hash e13ed21b90e17f6be358614c34967238fe20ba0fc039d084b9dd5f81fb04899d "$AB_DIR/index_1.bytes"

native_out="$(mktemp)"
packaged_out="$(mktemp)"
generic_out="$(mktemp)"
second_out="$(mktemp)"
trap 'rm -f "$native_out" "$packaged_out" "$generic_out" "$second_out"' EXIT

# Each inspector runs twice; both runs must be byte-identical (deterministic)
# and must match its pinned output digest. Drift in either fails closed.
run_twice() {
  local inspector="$1" out="$2"
  python3 "$inspector" >"$out"
  python3 "$inspector" >"$second_out"
  cmp -s "$out" "$second_out" || { printf 'non-deterministic output from %s\n' "$inspector" >&2; exit 1; }
}
run_twice "$NATIVE_INSPECTOR" "$native_out"
run_twice "$PACKAGED_INSPECTOR" "$packaged_out"
run_twice "$GENERIC_INSPECTOR" "$generic_out"

check_hash 185d8092d20ff78b756a8ee429547018553ac964c2f0500f7d65129f60dae36b "$native_out"
check_hash 9268a894cc426fd3c933552fb2dd39dd34b13d40df3f3cb7f3fe5213ef84c62b "$packaged_out"
check_hash 53c2b74137c986701ebf738702f5a93c094a1501e9af90c62f3d17fae447ddbf "$generic_out"

jq -e '
    .schema_version == 6 and
  (.isil | length) == 7 and
  .slices["NormalLevelLogic.IsMultiPlayer"].sha256 == "3eec2b3cc689183c0c59fcfc5c6236c7895073bd427baf8b0c180b3e5145e2eb" and
  .slices["ActorEntityMgr.IsMultiPlayer"].sha256 == "8ca8831f2bab79e9074ac569e53d58d19815f3e9fab5f68c055d7b751865c9b9" and
  .slices["CollectItemEntity.SetCurCollectActor"].sha256 == "4b780ad724e7cb009af5bec7d8444042f6e46260c18280881d9bde834ba9d520" and
  .slices["ActorEntityMgr.CreateActorEntity"].sha256 == "dafa43ad2f527424a9142d6ad25daa18ee49c8cbf7a26422ef57b3cf3a3a810c" and
  .slices["ActorEntityMgr.CreateActorEntityObject"].sha256 == "c176aa51152d3ec4cd6ff7e1c52872264956fa14e49124a61137e4cfe12281a3" and
  .slices["ActorEntity.Create"].sha256 == "1e3f08677acf5b5b610e971193fbc06f93b08cecc9964098d467e4d87aba99b6" and
  .slices["ActorEntityMgr.DestroyActor"].sha256 == "262b879c22ce2f2d5f7af0a9101f8f456e34383d89f1e7b742011bd3c12ce926" and
  .slices["LevelItemMgr.CreateMonster_caller"].sha256 == "369d8c08aa19ef269b441e91248f7649f0e8460b62c04d63e5f7aded28187dbe" and
  .slices["WaveRefresh.BronMonster_caller"].sha256 == "3537f1ebfe7ca728b0601662c607700e0db843ac36168bc311f7e566a70003ff" and
  .slices["ActorEntityCreateData.SetBornPos"].sha256 == "3fa46f4fb564592fedf80fa519cdde1a3d732517886c35e45197c4afec63eafd" and
  .slices["ActorEntityCreateData.SetBornPos"].bytes == 44 and
  .slices["ActorEntityCreateData.CreatePlayerCreateData"].sha256 == "03fe7b516db01ea75a9d15d665572cfba57e1f3477d317149a49b8f20e36ff03" and
  .slices["ActorEntityCreateData.CreatePlayerCreateData"].bytes == 140 and
  .slices["ActorEntityCreateData.CreateMonsterCreateData_overload1"].sha256 == "55de55b2bc62d7820cf378691ece603ca5fc718f13e61ca596f2ad7dea489df3" and
  .slices["ActorEntityCreateData.CreateMonsterCreateData_overload1"].bytes == 140 and
  .slices["ActorEntityCreateData.CreateMonsterCreateData_locked"].sha256 == "3f7a9ab96e9a1b6b41b888164c10ac77cb8fb545e80454f0d6096a2da80dd725" and
  .slices["ActorEntityCreateData.CreateMonsterCreateData_locked"].bytes == 152 and
  .slices["NormalLevelLogic.OnGameStart_caller"].sha256 == "b0585cc487ad6cfc44de748f389312ec2a3bd1ab6ca8ea0ae5996813c3c815ba" and
  .slices["NormalLevelLogic.OnGameStart_caller"].bytes == 732 and
  .slices["NormalLevelLogic.OnGameStart_caller"].virtual_address_start == "0x0160a7c4" and
  .slices["NormalLevelLogic.OnGameStart_caller"].virtual_address_end_exclusive == "0x0160aaa0" and
  (.slices | length) == 15 and
  (.resolved_methods | length) == 11 and
  ([.resolved_methods[] | .token] | sort) == ["0x060000cf","0x0600018b","0x0600018d","0x0600018e","0x0600018f","0x0600019c","0x0600019d","0x0600019e","0x06000c77","0x06000f89","0x060010ec"] and
  (any(.resolved_methods[]; .method_name == "CreateMonster" and .pointer_slot == 3976 and .token == "0x06000f89")) and
  (any(.resolved_methods[]; .method_name == "BronMonster" and .pointer_slot == 4331 and .token == "0x060010ec")) and
  (any(.resolved_methods[]; .method_name == "CreateMonsterCreateData" and .pointer_slot == 398 and .token == "0x0600018f")) and
  (any(.resolved_methods[]; .method_name == "SetBornPos" and .pointer_slot == 394 and .token == "0x0600018b" and .method_definition_index == 20734)) and
  (any(.resolved_methods[]; .method_name == "CreatePlayerCreateData" and .pointer_slot == 396 and .token == "0x0600018d" and .method_definition_index == 20736)) and
  (any(.resolved_methods[]; .method_name == "CreateMonsterCreateData" and .pointer_slot == 397 and .token == "0x0600018e" and .method_definition_index == 20737)) and
  (any(.resolved_methods[]; .method_name == "OnGameStart" and .declaring_type == "BattleCore.NormalLevelLogic" and .pointer_slot == 3190 and .token == "0x06000c77" and .method_definition_index == 23530 and .parameter_count == 0)) and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860bf8") | .bytes] | first) == "e8030032" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860bfc") | .bytes] | first) == "13200229" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860c84") | .bytes] | first) == "e8031f32" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860c88") | .bytes] | first) == "13200229" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860d10") | .bytes] | first) == "e8031f32" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860d14") | .bytes] | first) == "14200229" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x016525f4") | .bytes] | first) == "df380894" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01652588") | .bytes] | first) == "f60300aa" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x01860974") | .bytes] | first) == "08200139" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a8d0") | .bytes] | first) == "af580994" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a8dc") | .bytes] | first) == "f60300aa" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a940") | .bytes] | first) == "f60a00b4" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a95c") | .bytes] | first) == "e00316aa" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a974") | .bytes] | first) == "ff570994" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a988") | .bytes] | first) == "e2030032" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a98c") | .bytes] | first) == "e10316aa" and
  ([.caller.bounded_instruction_evidence[] | select(.address=="0x0160a994") | .bytes] | first) == "5c600994" and
  (.actor_type_factory_evidence.field.offset == "+0x14") and
  (.actor_type_factory_evidence.proven_raw_facts.factory_immediates | length) == 3 and
  (.actor_type_factory_evidence.proven_raw_facts.factory_immediates[0].raw_value == 1) and
  (.actor_type_factory_evidence.proven_raw_facts.factory_immediates[1].raw_value == 2) and
  (.actor_type_factory_evidence.proven_raw_facts.factory_immediates[2].raw_value == 2) and
  (.actor_type_factory_evidence.metadata_method_identities | length) == 4 and
  (.actor_type_factory_evidence.locked_levelitemmgr_path.chain | length) == 7 and
  (.actor_type_factory_evidence.locked_levelitemmgr_path.setbornpos_object_writes | length) == 5 and
  ([.actor_type_factory_evidence.locked_levelitemmgr_path.setbornpos_object_writes[].offset] | sort) == ["+0x48","+0x50","+0x60","+0x68","+0x78"] and
  (.actor_type_factory_evidence.locked_levelitemmgr_path.setbornpos_negative | test("\\+0x14 are not written")) and
  (.actor_type_factory_evidence.locked_normallevellogic_path.chain | length) == 7 and
  (.actor_type_factory_evidence.locked_normallevellogic_path.create_actor_entity_call_site == "0x0160a994") and
  (.actor_type_factory_evidence.locked_normallevellogic_path.setbornpos_object_writes | length) == 5 and
  (.actor_type_factory_evidence.locked_normallevellogic_path.setbornpos_negative | test("\\+0x14 are not written")) and
  (.actor_type_factory_evidence.locked_normallevellogic_path.isstartactor_register_fact | test("w2 = 1")) and
  (.actor_type_factory_evidence.locked_normallevellogic_path.isstartactor_register_fact | test("no gameplay semantic inferred")) and
  (.actor_type_factory_evidence.locked_normallevellogic_path.m_listplayer_helper_reached | test("CMP w20,#1 equal")) and
  (.actor_type_factory_evidence.locked_normallevellogic_path.m_listplayer_helper_reached | test("no player/mode/list")) and
  (.actor_type_factory_evidence.pet_negative_guard.method_name == "CreatePetCreateData") and
  (.actor_type_factory_evidence.pet_negative_guard.token == "0x06000190") and
  (.actor_type_factory_evidence.pet_negative_guard.pointer_slot == 399) and
  (.actor_type_factory_evidence.pet_negative_guard.rule | test("NOT a CreateMonsterCreateData")) and
  ((.actor_type_factory_evidence.unresolved | length) == 10) and
  (.actor_type_factory_evidence.bounded_claim | test("no mode, config, player")) and
  (.upstream_callers.direct_bl_caller_count == 24) and
  (.upstream_callers.create_player_create_data_direct_bl_caller_count == 12) and
  (.upstream_callers.m_listplayer_helper_direct_bl_caller_counts == {"0x01c52f20":1610,"0x01c545d0":93}) and
    .upstream_callers.caller_scan_scope.executable_pt_load_count == 1 and
    .upstream_callers.caller_scan_scope.segments == [{"virtual_address":"0x0","file_offset":"0x0","file_size":37899396,"flags":5,"program_header_alignment":65536,"scan_instruction_alignment":4}] and
    .upstream_callers.caller_scan_scope.total_scanned_bytes == 37899396 and
    (.upstream_callers.create_player_create_data_callers | length) == 12 and
    (all(.upstream_callers.create_player_create_data_callers[]; .containing_method.resolution == "unique" and .conditional_static_chain.status == "proven_conditional_static_chain")) and
    ([.upstream_callers.create_player_create_data_callers[].conditional_static_chain.setbornpos | length] | add) == 11 and
    ([.upstream_callers.create_player_create_data_callers[].conditional_static_chain.create_actor_entity_call_site] | sort) == ["0x015c8b7c","0x015caca0","0x015cd058","0x015f8444","0x015f9188","0x0160a994","0x01624be0","0x01629dac","0x0162d720","0x01630ad8","0x01634b9c","0x0168703c"] and
    .classifications.mode_selector.status == "unresolved" and
    .classifications.solo_coop_authority.status == "unresolved" and
    .classifications.runtime_parity.status == "unresolved" and
    .classifications.load_order_winner.status == "unresolved" and
    .classifications.field_name_provenance.status == "ISIL-correlated" and
    .classifications.parameter_name_provenance.status == "ISIL-correlated" and
  (.upstream_callers.locked_callers | length) == 3 and
  .upstream_callers.locked_callers[0].create_actor_entity_call_site == "0x0165261c" and
  .upstream_callers.locked_callers[1].create_actor_entity_call_site == "0x01683e38" and
  .upstream_callers.locked_callers[2].create_actor_entity_call_site == "0x0160a994" and
  .upstream_callers.locked_callers[2].resolved_identity.method_name == "OnGameStart" and
  .upstream_callers.locked_callers[2].resolved_identity.pointer_slot == 3190 and
  .upstream_callers.locked_callers[2].create_data_provenance.resolved_identity.method_name == "CreatePlayerCreateData" and
  (.upstream_callers.locked_callers[2].create_data_provenance.factory_raw_immediate == 1) and
  (.upstream_callers.locked_callers[2].register_setup[2] | test("w2 = 1")) and
  (.upstream_callers.locked_callers[2].register_setup[2] | test("no gameplay semantic inferred")) and
  (.upstream_callers.locked_callers[0].register_setup[0] | test("implicit this argument")) and
  (.upstream_callers.locked_callers[1].register_setup[0] | test("implicit this argument")) and
  (.upstream_callers.locked_callers[0].register_setup[0] | test("no field identity assigned")) and
  (.upstream_callers.locked_callers[1].register_setup[0] | test("no field identity assigned")) and
  .upstream_callers.locked_callers[0].x0_dereference_chain[4] == "0x01652608 ldr x0, [x8, #0x48]" and
  .upstream_callers.locked_callers[1].x0_dereference_chain[4] == "0x01683e24 ldr x0, [x8, #0x48]" and
  .upstream_callers.locked_callers[1].create_data_provenance.prologue == "0x01683d68 mov x21, x1 (locked)" and
  (.upstream_callers.locked_callers[1].create_data_provenance.source | test("AAPCS64")) and
  (.upstream_callers.locked_callers[1].create_data_provenance.source | test("not an exhaustive")) and
  (.upstream_callers.locked_callers[1].create_data_provenance.isil_correlation | test("not metadata-proven")) and
  .m_listPlayer_evidence.CreateActorEntity_direct_accesses == [] and
  .m_listPlayer_evidence.CreateActorEntityObject.gate == "raw actorType argument in w20 equals 1" and
  .m_listPlayer_evidence.CreateActorEntityObject.helper_call == "0x01862e80 calls 0x01c52f20 with x0=list and x1=created entity" and
  .m_listPlayer_evidence.DestroyActor.helper_call == "0x01862fb0 calls 0x01c545d0 with x0=list and x1=actor" and
  (.unresolved_edges | length) == 1 and
  .unresolved_edges[0].target == "0x0186c0a8" and
  .unresolved_edges[0].generic_pointer_table.matching_slot == 133 and
  (.unresolved_edges[0] | has("resolved_name") | not) and
  (.negative_findings | index("No inspected evidence proves a pilot selector, solo/co-op authority, or runtime parity.")) != null and
  (any(.negative_findings[]; test("disproven for this"))) and
  (any(.negative_findings[]; test("raw value is a constant"))) and
  (any(.negative_findings[]; test("CreatePlayerCreateData factory writes raw 1"))) and
  (any(.negative_findings[]; test("0x01860d3c is CreatePetCreateData"))) and
  (any(.negative_findings[]; test("OnGameStart reaches the m_listPlayer helper"))) and
  (any(.negative_findings[]; test("3 of 24 direct BL callers"))) and
    .upstream_callers.caller_partition.create_actor_entity_site_count == 24 and
    .upstream_callers.caller_partition.create_actor_entity_sites == ["0x015c8b7c","0x015caca0","0x015cd058","0x015cf8ec","0x015d0050","0x015def60","0x015f4608","0x015f8444","0x015f87b8","0x015f9188","0x0160a994","0x016108c8","0x0162330c","0x01623b48","0x01624be0","0x01629dac","0x0162d720","0x01630ad8","0x01634b9c","0x01651184","0x0165261c","0x0167a3f8","0x01683e38","0x0168703c"] and
    .upstream_callers.caller_partition.create_player_create_data_linked_actor_site_count == 12 and
    .upstream_callers.caller_partition.create_player_create_data_linked_actor_sites == ["0x015c8b7c","0x015caca0","0x015cd058","0x015f8444","0x015f9188","0x0160a994","0x01624be0","0x01629dac","0x0162d720","0x01630ad8","0x01634b9c","0x0168703c"] and
    .upstream_callers.caller_partition.outside_factory_site_count == 12 and
    .upstream_callers.caller_partition.outside_factory_sites == ["0x015cf8ec","0x015d0050","0x015def60","0x015f4608","0x015f87b8","0x016108c8","0x0162330c","0x01623b48","0x01651184","0x0165261c","0x0167a3f8","0x01683e38"] and
    (.upstream_callers.caller_partition.outside_factory_sites | length) == 12 and
    .upstream_callers.caller_partition.deepened_this_increment_count == 10 and
    .upstream_callers.caller_partition.deepened_this_increment == ["0x015cf8ec","0x015d0050","0x015def60","0x015f4608","0x015f87b8","0x016108c8","0x0162330c","0x01623b48","0x01651184","0x0167a3f8"] and
    (.upstream_callers.caller_partition.deepened_this_increment | length) == 10 and
    .upstream_callers.caller_partition.already_individually_locked_outside_sites == ["0x0165261c","0x01683e38"] and
    (.upstream_callers.caller_partition as $p |
      (($p.create_player_create_data_linked_actor_sites + $p.outside_factory_sites | unique | sort) == ($p.create_actor_entity_sites | unique | sort)) and
      ([$p.create_player_create_data_linked_actor_sites[] as $site | select($p.outside_factory_sites | index($site)) | $site] | length) == 0) and
  ([.upstream_callers.outside_factory_sites[].create_data_provenance.class | select(.=="factory_return_proven")] | length) == 4 and
  ([.upstream_callers.outside_factory_sites[].create_data_provenance.class | select(.=="local_initialization_unresolved_identity")] | length) == 2 and
  ([.upstream_callers.outside_factory_sites[].create_data_provenance.class | select(.=="field_local_provenance_unresolved_factory")] | length) == 4 and
  ([.upstream_callers.outside_factory_sites[].create_data_provenance.class | select(.|test("already_locked"))] | length) == 2 and
  (.m_listPlayer_evidence.helper_body_evidence | length) == 2 and
  (all(.m_listPlayer_evidence.helper_body_evidence[]; .hash_locked == true)) and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c52f20") | .generic_pointer_slot] | first) == 11733 and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c52f20") | .sha256] | first) == "2f1c91a3aa83218da3312b2d17bb2257cc8a1ba4764084b77c497d0b851e9c96" and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c52f20") | .range] | first) == {"virtual_address_start":"0x01c52f20","virtual_address_end_exclusive":"0x01c52fc0"} and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c545d0") | .generic_pointer_slot] | first) == 11741 and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c545d0") | .sha256] | first) == "9503dabaddc7e1cd4f9b473619d7dd29b5e305a773750d5a46bb3591be93ab5c" and
  ([.m_listPlayer_evidence.helper_body_evidence[] | select(.call_target=="0x01c545d0") | .range] | first) == {"virtual_address_start":"0x01c545d0","virtual_address_end_exclusive":"0x01c54638"} and
  ([.m_listPlayer_evidence.helper_dispatch_evidence[] | .x2_source_offset] | sort) == ["+0x108","+0x60","+0xf8"] and
  ([.m_listPlayer_evidence.helper_dispatch_evidence[] | select(.call_target=="0x01c52f20" and .x2_source_offset=="+0x60") | .bytes] | first) == "223140f9" and
  ([.m_listPlayer_evidence.helper_dispatch_evidence[] | select(.call_target=="0x01c545d0" and .x2_source_offset=="+0xf8") | .bytes] | first) == "027d40f9" and
  ([.m_listPlayer_evidence.helper_dispatch_evidence[] | select(.call_target=="0x01c545d0" and .x2_source_offset=="+0x108") | .bytes] | first) == "028540f9" and
  (all(.m_listPlayer_evidence.helper_dispatch_evidence[]; .binding == "generic_method_info_dispatch_via_x2_unresolved")) and
  .m_listPlayer_evidence.helper_binding.status == "unresolved" and
  (.negative_findings | length) == 10
' "$native_out" >/dev/null

jq -e '
  .schema_version == 1 and
  .unitypy_version == "1.25.0" and
  .levelconfig.by_type == {"AssetBundle":1,"MonoBehaviour":765,"MonoScript":1} and
  .levelconfig.monobehaviour_count == 765 and
  .levelconfig.shared_monoscript_pptr_pathid == 7701356557357364315 and
  .levelconfig.type_tree_schema.top_level_keys == ["m_Enabled","m_GameObject","m_LevelData","m_Name","m_Script"] and
  .levelconfig.type_tree_schema.m_LevelData_keys == ["m_LevelId","m_Monsters","m_Obstacles"] and
  .levelconfig.type_tree_schema.m_LevelId_value_type == "int" and
  .levelconfig.type_tree_schema.distinct_top_level_keysets == 1 and
  (.levelconfig.decoded_schema | test("read_typetree")) and
  (.levelconfig.decoded_schema | test("not interpreted")) and
  .levelconfig.catalog_membership.name_present == true and
  .multiplayer.by_type == {"AssetBundle":1,"TextAsset":1} and
  .multiplayer.text_asset.name == "MultiplayerExpressionConfig" and
  .multiplayer.text_asset.payload_bytes == 11207 and
  .multiplayer.text_asset.object_byte_size == 11244 and
  .multiplayer.reported_but_unreproducible_payload_bytes == 11547 and
  (.negative_findings | length) == 3
' "$packaged_out" >/dev/null

# Generic-context inspector: 0x01862ba0 -> 0x0186c0a8 (slot 133) enumeration.
jq -e '
  .schema_version == 1 and
  (.inputs | length) == 2 and
  ([.inputs[].sha256] | sort) == ["130d09d3b1cdc57ad12eee96d77b6db9665b22dfd40312d4f750fe6f93caabe8","459503b7d16ab3ae95190a180fc5bb3a361b7bb8e39651eb583725bf0888c2f4"] and
  .code_registration.virtual_address == "0x252c490" and
  .code_registration.generic_method_pointer_table == "0x24b4f18" and
  .code_registration.generic_method_pointer_count == 20150 and
  .code_registration.generic_pointer_slot_133_value == "0x0186c0a8" and
  .metadata_registration.virtual_address == "0x252cbc8" and
  .metadata_registration.generic_method_table == "0x1e0172c" and
  .metadata_registration.generic_method_table_count == 38488 and
  .metadata_registration.method_specs_table == "0x1e98b54" and
  .metadata_registration.method_specs_count == 39976 and
  .metadata_registration.generic_insts_table == "0x24f4d58" and
  .metadata_registration.generic_insts_count == 6275 and
  .generic_slot_133.method_pointer_index == 133 and
  .generic_slot_133.method_pointer_address == "0x0186c0a8" and
  .generic_slot_133.matching_row_count == 50 and
  (.generic_slot_133.rows | length) == 50 and
  .generic_slot_133.distinct_t_count == 50 and
  ([.generic_slot_133.rows[].method_pointer_index] | unique) == [133] and
  ([.generic_slot_133.rows[].method_spec.method_definition_index] | unique) == [1009] and
  ([.generic_slot_133.rows[].method_spec.method_inst_index] | unique) == [-1] and
  ([.generic_slot_133.rows[].method_spec.class_inst_index] | unique | length) == 50 and
  .method_definition_action_invoke.method_definition_index == 1009 and
  .method_definition_action_invoke.token == "0x060003f2" and
  .method_definition_action_invoke.method_name == "Invoke" and
  .method_definition_action_invoke.declaring_type == "System.Action`1" and
  .method_definition_action_invoke.parameter_count == 1 and
  .actor_entity_candidate.candidate_count == 1 and
  .actor_entity_candidate.type_name == "BattleCore.ActorEntity" and
  .actor_entity_candidate.row_index == 142 and
  .actor_entity_candidate.method_spec_index == 193 and
  .actor_entity_candidate.class_inst_index == 55 and
  .actor_entity_candidate.type_definition_index == 3521 and
  (.actor_entity_candidate.rule | test("never selected without unique caller context")) and
  .caller_context.bl_edge.call_site == "0x01862ba0" and
  .caller_context.bl_edge.target == "0x0186c0a8" and
  .caller_context.caller_method.method_name == "CreateActorEntity" and
  .caller_context.caller_method.declaring_type == "BattleCore.ActorEntityMgr" and
  .caller_context.caller_method.token == "0x0600019c" and
  .caller_context.method_info_dereference.x2_value == "0xc0000183" and
  .caller_context.method_info_dereference.x2_value_high_byte == "0xc0" and
  .caller_context.method_info_dereference.x2_value_is_file_backed_va == false and
  .caller_context.classification == "caller_specific_instantiation: unresolved" and
  (.caller_context.reason | test("cannot be made statically")) and
  (.negative_findings | length) == 5 and
  (any(.negative_findings[]; test("exactly one candidate of 50"))) and
  (.remaining_binding_target | test("decode the runtime MethodInfo value 0xc0000183"))
' "$generic_out" >/dev/null

for anchor in 'Status: `in_progress`' 'is **not** a pilot-mode selector' 'm_listPlayer.Count > 1' 'raw `actorType` argument equals `1`' '0x01862ba0 -> 0x0186c0a8' 'Next exact target:' 'Date: 2026-07-16' 'file-backed executable (PF_X)' 'implicit this argument' 'AAPCS64' 'read_typetree()' 'Locked NormalLevelLogic path (end-to-end)' 'BattleCore.NormalLevelLogic.OnGameStart' 'pointer-table slot `3190`' 'createData provenance is resolved at the three sites' '**complete static caller partition**' 'caller-specific instantiation'; do
  require_anchor "$anchor" "$EVIDENCE"
  done
semantic_guard() {
  python3 - "$@" <<'PY'
import re
import sys

claim = r"(?:mode selector|solo.?co-?op authority|runtime parity|load[- ]order winner)"
promotion = r"(?:proven|confirmed|established|verified|recovered|resolved)"
positive = re.compile(
    rf"\b{promotion}\b[^.!?;\n]{{0,100}}\b{claim}\b|"
    rf"\b{claim}\b[^.!?;\n]{{0,100}}\b{promotion}\b",
    re.I,
)
negative = re.compile(
    rf"\bno\b[^.!?;\n]{{0,60}}\b{claim}\b|"
    rf"\bnot\b[^.!?;\n]{{0,40}}\b{claim}\b|"
    rf"\b{claim}\b[^.!?;\n]{{0,60}}\b(?:not|never|unproven|unresolved|unknown)\b",
    re.I,
)

# Direct unsupported semantic assertions that bypass the promotion guard by
# stating a gameplay meaning outright (no proven/confirmed/.../resolved word).
# Five categories: level/config selects multiplayer/mode, pilot setting selects
# solo/co-op, raw actor type represents player/monster, active catalog is the
# load-order winner, static helper presence establishes co-op/solo authority.
# Each category accepts either word order: active S-V-O or passive O-V-S.
_level_or_config = r"(?:levels?|configs?|configuration|level\s*config|levelconfig|level\s*data|m_LevelData|m_LevelId|level\s*/\s*config|config\s+row)"
_pilot = r"pilot(?:[-\s]?(?:mode|setting|flag|selector|value))?"
_actor_type = r"(?:raw\s+)?actor\s?type|actorType"
_catalog = r"(?:active|current)\s+catalog|catalog\s+membership"
_helper = r"static\s+helper(?:\s+presence)?|helper\s+presence|m_listPlayer\s+helper|helper\s+call"

_select_v = r"(?:selects?|selected|selecting|determin(?:e|es|ed|ing)|choos(?:e|es|ing)|chose|chosen|picks?|picked|picking|controls?|controlled|controlling|governs?|governed|governing|drives?|driven|driving|indicates?|indicated|indicating|specifies?|specified|specifying|forces?|forced|forcing|enabl(?:e|es|ed|ing)|activat(?:e|es|ed|ing)|triggers?|triggered|triggering|sets?|setting|rout(?:e|es|ed|ing)|yields?|yielded|yielding|results?|resulted|resulting)"
_represent_v = r"(?:represents?|represented|representing|denotes?|denoted|classif(?:y|ies|ied)|identifies?|identified|designates?|designated|encodes?|encoded|signals?|signaled|stands\s+for|maps\s+to|corresponds\s+to|means)"
_catalog_v = r"(?:is|are|constitutes?|constituted|represents?|represented|establishes?|established|identifies?|identified|designates?|designated|proves?|proved)"
_helper_v = r"(?:establishes?|established|constitutes?|constituted|proves?|proved|confirms?|confirmed|determines?|determined|indicates?|indicated|implies?|implied)"

_mode_o = r"(?:multi[-\s]?player|solo|co[-\s]?op|game\s+mode|mode)"
_solo_coop_o = r"(?:solo|co[-\s]?op|single[-\s]?player)"
_role_o = r"(?:(?:a|an|the)\s+)?(?:player|monster|hero|npc|enemy|pet|protagonist)"
_winner_o = r"load[-\s]order\s+winner"
_authority_o = r"(?:solo|co[-\s]?op)\s+authority|solo/co-?\s*op\s+authority"

def _pair(subj: str, verb: str, obj: str, gap: int = 60) -> str:
    g = rf"[^.!?;\n]{{0,{gap}}}?"
    return rf"\b(?:{subj})\b{g}\b(?:{verb})\b{g}\b(?:{obj})\b|\b(?:{obj})\b{g}\b(?:{verb})\b{g}\b(?:{subj})\b"

direct = re.compile("|".join((
    _pair(_level_or_config, _select_v, _mode_o),
    _pair(_pilot, _select_v, _solo_coop_o),
    _pair(_actor_type, _represent_v, _role_o),
    _pair(_catalog, _catalog_v, _winner_o),
    _pair(_helper, _helper_v, _authority_o),
)), re.I)

# Clause-level negation / unresolved cues that permit a direct assertion.
negation_cue = re.compile(
    r"\b(?:no|not|never|n't|cannot|without|absent|neither|nor|"
    r"unresolved|unproven|unknown|disproven|uninterpreted|uninferred|"
    r"skipped|not\s+(?:a|an|the)|does\s+not|do\s+not|is\s+not|are\s+not|"
    r"was\s+not|were\s+not|remains\s+unresolved|stays\s+unresolved)\b",
    re.I,
)
epistemic_cue = re.compile(
    r"\b(?:open\s+question|question\s+is|whether|unknown\s+whether|unresolved\s+whether)\b",
    re.I,
)
leading_negation_cue = re.compile(
    r"\b(?:no|not|never|neither)\s+(?:(?:a|an|the|any)\s+)?$",
    re.I,
)
framing_negation_cue = re.compile(
    r"\b(?:unproven|unresolved|unknown|disproven)\s+(?:that|whether)\s+(?:(?:a|an|the)\s+)?$",
    re.I,
)
trailing_negation_cue = re.compile(
    r"^\s+(?:is|are|was|were|remains?|stays?)\s+(?:unproven|unresolved|unknown|disproven)\b",
    re.I,
)

def _strip_quotes(text: str) -> str:
    # ponytail: drop quoted spans so a quoted negative gate's wording cannot
    # pose as a direct positive assertion.
    return re.sub(r'"[^"]*"', " ", text)

# Subject carried into a subjectless coordinated predicate ("S V1 O1 and V2 O2").
_subj_re = re.compile(
    rf"\b(?:{_level_or_config}|{_pilot}|{_actor_type}|{_catalog}|{_helper})\b", re.I)
# Split a clause before a coordinated predicate: a coordinating conjunction
# followed (after optional auxiliaries/negations) by an action verb. Copula
# alone is not a split trigger, so ordinary technical prose is not fragmented;
# the conjunction alone is consumed so each coordinand keeps its own negation.
_lead = r"(?:not|n't|no|never|neither|does|do|is|are|was|were|has|have|had|can|could|will|would|shall|should|may|might|must|also|then|still|merely|simply|even)\s+"
_conj_split = re.compile(
    rf"\b(?:and|or|nor)\s+(?=(?:{_lead})*(?:{_select_v}|{_represent_v}|{_helper_v})\b)",
    re.I)

def _direct_violation(clause: str) -> bool:
    unquoted = _strip_quotes(clause)
    anchor_match = _subj_re.search(unquoted)
    anchor = anchor_match.group(0) if anchor_match else ""
    for coord in _conj_split.split(unquoted):
        # A subjectless coordinand inherits the clause subject ("... and selects X").
        probe = coord if _subj_re.search(coord) else f"{anchor} {coord}"
        match = direct.search(probe)
        if (
            match
            and not negation_cue.search(match.group(0))
            and not leading_negation_cue.search(probe[: match.start()])
            and not framing_negation_cue.search(probe[: match.start()])
            and not epistemic_cue.search(probe[: match.start()])
            and not trailing_negation_cue.search(probe[match.end() :])
        ):
            return True
    return False

def promoted(text: str) -> bool:
    # Split contrast clauses so a negative claim cannot mask a nearby positive one.
    clauses = re.split(r"(?<=[.!?;])\s+|\n+|\b(?:but|however)\b", text, flags=re.I)
    for clause in clauses:
        if positive.search(clause) and not negative.search(clause):
            return True
        if _direct_violation(clause):
            return True
    return False

fixtures = {
    # --- promotion guard (unchanged) ---
    "Proven mode selector": True,
    "The mode selector is proven": True,
    "Runtime parity confirmed": True,
    "confirmed the runtime parity": True,
    "solo/co-op authority established": True,
    "The load-order winner is verified": True,
    "No runtime parity is proven, but the mode selector is confirmed": True,
    "No proven mode selector exists": False,
    "runtime parity is not proven": False,
    "mode selector remains unresolved": False,
    "solo/co-op authority is unknown": False,
    "load-order winner is unproven": False,
    # --- level/config selects multiplayer/mode (both orders, both polarities) ---
    "The level config selects multiplayer mode": True,
    "the level selects multiplayer": True,
    "Multiplayer mode is selected by the level config": True,
    "levelconfig determines solo mode": True,
    "this config chooses co-op": True,
    "the level data drives the game mode": True,
    "game mode is driven by the level data": True,
    "The level config does not select multiplayer mode": False,
    "multiplayer mode is not selected by the config": False,
    "the config does not determine mode": False,
    # --- pilot setting selects solo/co-op (both orders, both polarities) ---
    "The pilot setting selects solo": True,
    "solo is selected by the pilot setting": True,
    "the pilot flag chooses co-op": True,
    "The pilot does not select solo": False,
    "solo is not selected by the pilot": False,
    "pilot selection of solo remains unresolved": False,
    # --- raw actor type represents player/monster (both orders, both polarities) ---
    "The raw actor type represents the player": True,
    "the player is represented by the raw actor type": True,
    "actorType 1 represents a monster": True,
    "actor type denotes the hero": True,
    "The raw actor type does not represent a player": False,
    "the player is not represented by the raw actor type": False,
    "no actor type represents a monster": False,
    "actor type remains uninterpreted": False,
    # --- active catalog is load-order winner (both orders, both polarities) ---
    "The active catalog is the load-order winner": True,
    "the load-order winner is the active catalog": True,
    "the current catalog constitutes the load-order winner": True,
    "The active catalog is not the load-order winner": False,
    "the load-order winner is not the active catalog": False,
    "the active catalog remains unresolved as winner": False,
    # --- static helper presence establishes co-op/solo authority (both orders, both polarities) ---
    "The static helper presence establishes co-op authority": True,
    "co-op authority is established by the static helper": True,
    "the m_listPlayer helper proves solo authority": True,
    "The static helper does not establish co-op authority": False,
    "co-op authority is not established by the static helper": False,
    "the static helper authority remains unknown": False,
    # --- contrast clauses: positive direct assertion after but/however ---
    "The catalog is a string presence, but the active catalog is the load-order winner": True,
    "The level config does not select mode, however the pilot setting selects co-op": True,
    "actorType is a raw value; the raw actor type represents the player": True,
    "No mode is proven, but the level config selects multiplayer": True,
    "The pilot is not a selector, but the pilot setting selects solo": True,
    # --- contrast/negated wrappers that remain permitted ---
    "it is unproven that the level config selects multiplayer": False,
    "whether the pilot selects solo remains unresolved": False,
    "the claim that actor type represents the player is disproven": False,
    "the level config selects mode is disproven for all paths": False,
    # --- quoted negative gates (permitted) ---
    'the gate is "no mode, config, player"': False,
    'CreateActorEntityObject.gate == "raw actorType argument in w20 equals 1"': False,
    "no player/mode/list inferred": False,
    # --- force/enable/activate/trigger/set/route/yield/result selection verbs (both orders) ---
    "The config forces multiplayer": True,
    "This level activates co-op mode": True,
    "the pilot enables co-op": True,
    "the config triggers solo mode": True,
    "the level yields multiplayer mode": True,
    "the config results in solo mode": True,
    "multiplayer is enabled by the config": True,
    "co-op mode is triggered by the level": True,
    "The config does not force multiplayer": False,
    "the config does not enable co-op": False,
    "co-op is not enabled by the pilot": False,
    "the level does not trigger solo mode": False,
    # --- conjunction split: a negated first assertion must not mask a positive second ---
    "The config does not select solo and selects multiplayer": True,
    "the config selects solo and does not select multiplayer": True,
    "the pilot does not enable solo and enables co-op": True,
    "The config does not select solo and does not select multiplayer": False,
    "the config does not force solo or multiplayer": False,
    "Whether this level activates co-op mode remains unresolved": False,
    "The config does not select solo or multiplayer": False,
    "The level config selects multiplayer, not solo.": True,
    "The open question is whether the level config selects multiplayer mode.": False,
}
for text, expected in fixtures.items():
    actual = promoted(text)
    if actual != expected:
        raise SystemExit(f"semantic-guard fixture failed: {text!r}: {actual} != {expected}")
for path in sys.argv[1:]:
    text = open(path, encoding="utf-8").read()
    for number, sentence in enumerate(re.split(r"(?<=[.!?;])\s+|\n+", text), 1):
        if promoted(sentence):
            raise SystemExit(f"unsupported semantic assertion: {path}:{number}: {sentence[:160]}")
PY
}
semantic_guard "$EVIDENCE" "$PACKET"/*.md "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md" "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"
require_anchor 'R-DHCD-004 | P0 | reverse-owner / in_progress' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md"
require_anchor 'no pilot or solo/co-op parity inference' "$HARNESS_ROOT/specs/dhcd-jx-port/10-research/unresolved-rules.md"
require_anchor 'No semantic player classification' "$PACKET/overview.md"
require_anchor '0x0186C0A8' "$PACKET/validation.md"

printf 'US-P0-006 fail-closed mode-selection evidence verified.\n'
