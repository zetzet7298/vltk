package catalog

import (
	"errors"
	"fmt"
	"regexp"
)

const (
	TickRateHz       = 18
	MaxActors        = 64
	MaxLightEntities = 128
	MaxEffectNodes   = 16
	HashLengthSHA256 = 64
)

var hashRE = regexp.MustCompile(`^[a-f0-9]{64}$`)

var (
	ErrBadRelease        = errors.New("catalog: bad release")
	ErrBadHash           = errors.New("catalog: bad content hash")
	ErrUnsupportedEffect = errors.New("catalog: unsupported effect node")
	ErrPolicyDenied      = errors.New("catalog: policy denied")
)

type PolicyMode uint8

const (
	PolicyDisabled PolicyMode = iota
	PolicyEnabled
	PolicyAuditOnly
)

type FeaturePolicy struct {
	Global  PolicyMode
	Skill   PolicyMode
	Faction PolicyMode
}

func (p FeaturePolicy) AllowsRuntime() bool {
	return p.Global != PolicyDisabled && p.Skill != PolicyDisabled && p.Faction != PolicyDisabled
}

type Release struct {
	ID     string
	Hash   string
	Policy FeaturePolicy
	Skills map[SkillKey]Skill
}

type SkillKey struct {
	ID    uint32
	Level uint32
}

type EffectKind uint8

const (
	EffectDamage EffectKind = iota + 1
	EffectHeal
	EffectResource
	EffectStatus
)

type DamageSchool uint8

const (
	SchoolPhysical DamageSchool = iota + 1
	SchoolMetal
	SchoolWood
	SchoolWater
	SchoolFire
	SchoolEarth
	SchoolInternal
)

type Skill struct {
	ID                  uint32
	Level               uint32
	ManaCost            int64
	CastTicks           uint32
	RecoveryTicks       uint32
	CooldownTicks       uint32
	RangeMilli          int64
	MissileID           uint32
	MissileSpeedPerTick int64
	AnimationID         uint32
	VisualEffectID      uint32
	Effects             []Effect
}

type Effect struct {
	Kind           EffectKind
	Value          int64
	School         DamageSchool
	ChancePermille uint32
	Status         Status
}

type Status struct {
	ID            uint32
	DurationTicks uint32
	MaxStacks     uint32
}

func (r Release) Validate() error {
	if r.ID == "" {
		return fmt.Errorf("%w: empty id", ErrBadRelease)
	}
	if !hashRE.MatchString(r.Hash) {
		return fmt.Errorf("%w: want lowercase sha256 hex", ErrBadHash)
	}
	if !r.Policy.AllowsRuntime() {
		return ErrPolicyDenied
	}
	if len(r.Skills) == 0 {
		return fmt.Errorf("%w: no skills", ErrBadRelease)
	}
	for key, skill := range r.Skills {
		if key.ID == 0 || key.Level == 0 || key.ID != skill.ID || key.Level != skill.Level {
			return fmt.Errorf("%w: bad skill key %+v", ErrBadRelease, key)
		}
		if skill.ManaCost < 0 || skill.RangeMilli < 0 || skill.MissileSpeedPerTick < 0 {
			return fmt.Errorf("%w: negative numeric field", ErrBadRelease)
		}
		if len(skill.Effects) == 0 || len(skill.Effects) > MaxEffectNodes {
			return fmt.Errorf("%w: effect count %d", ErrBadRelease, len(skill.Effects))
		}
		if skill.MissileID != 0 && skill.MissileSpeedPerTick <= 0 {
			return fmt.Errorf("%w: missile without speed", ErrBadRelease)
		}
		for _, effect := range skill.Effects {
			if effect.ChancePermille > 1000 {
				return fmt.Errorf("%w: chance > 1000", ErrBadRelease)
			}
			switch effect.Kind {
			case EffectDamage, EffectHeal, EffectResource:
				if effect.Value < 0 {
					return fmt.Errorf("%w: negative effect value", ErrBadRelease)
				}
			case EffectStatus:
				if effect.Status.ID == 0 || effect.Status.DurationTicks == 0 || effect.Status.MaxStacks == 0 {
					return fmt.Errorf("%w: bad status", ErrBadRelease)
				}
			default:
				return fmt.Errorf("%w: kind %d", ErrUnsupportedEffect, effect.Kind)
			}
		}
	}
	return nil
}

func (r Release) Skill(id, level uint32) (Skill, bool) {
	s, ok := r.Skills[SkillKey{ID: id, Level: level}]
	return s, ok
}
