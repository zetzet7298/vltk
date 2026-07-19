package gamev1

import "testing"

func TestGameV1DescriptorTags(t *testing.T) {
	messages := File_game_v1_game_proto.Messages()
	client := messages.ByName("ClientEnvelope")
	serverHello := messages.ByName("ServerHello")
	content := messages.ByName("ContentDigest")
	policy := messages.ByName("RuntimeSkillPolicy")
	combat := messages.ByName("CombatEvent")
	checks := []struct {
		name string
		got  uint32
		want uint32
	}{
		{"ClientEnvelope.encounter_preload_ack", uint32(client.Fields().ByName("encounter_preload_ack").Number()), 23},
		{"ServerHello.active_content", uint32(serverHello.Fields().ByName("active_content").Number()), 14},
		{"ServerHello.skill_policy", uint32(serverHello.Fields().ByName("skill_policy").Number()), 16},
		{"ContentDigest.catalog_union_size", uint32(content.Fields().ByName("catalog_union_size").Number()), 4},
		{"RuntimeSkillPolicy.runtime_parity_claimed", uint32(policy.Fields().ByName("runtime_parity_claimed").Number()), 5},
		{"CombatEvent.resync_state", uint32(combat.Fields().ByName("resync_state").Number()), 29},
	}
	for _, check := range checks {
		if check.got != check.want {
			t.Fatalf("%s tag=%d want %d", check.name, check.got, check.want)
		}
	}
}
