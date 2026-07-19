package catalog

import (
	"errors"
	"testing"
)

func TestValidateFailsClosedOnUnsupportedEffectAndBadHash(t *testing.T) {
	r := Release{ID: "r1", Hash: "bad", Policy: FeaturePolicy{Global: PolicyEnabled, Skill: PolicyEnabled, Faction: PolicyEnabled}, Skills: map[SkillKey]Skill{
		{ID: 1, Level: 1}: {ID: 1, Level: 1, Effects: []Effect{{Kind: EffectDamage, Value: 1}}},
	}}
	if !errors.Is(r.Validate(), ErrBadHash) {
		t.Fatalf("want bad hash")
	}
	r.Hash = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef"
	r.Skills[SkillKey{ID: 1, Level: 1}] = Skill{ID: 1, Level: 1, Effects: []Effect{{Kind: 99}}}
	if !errors.Is(r.Validate(), ErrUnsupportedEffect) {
		t.Fatalf("want unsupported effect")
	}
}
