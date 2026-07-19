package combat

import (
	"crypto/hmac"
	"crypto/sha256"
	"encoding/binary"
	"encoding/hex"
	"errors"
	"fmt"
	"sort"

	"vltk.dev/server-runtime/catalog"
)

const (
	TickRateHz              = catalog.TickRateHz
	MaxActors               = catalog.MaxActors
	MaxLightEntities        = catalog.MaxLightEntities
	ReconnectGraceTicks     = 15 * TickRateHz
	CheckpointIntervalTicks = 5 * TickRateHz
)

var (
	ErrCapacity         = errors.New("combat: capacity exceeded")
	ErrContentMismatch  = errors.New("combat: content release/hash mismatch")
	ErrUnknownActor     = errors.New("combat: unknown actor")
	ErrUnknownSkill     = errors.New("combat: unknown skill")
	ErrCooldown         = errors.New("combat: skill on cooldown")
	ErrBusy             = errors.New("combat: actor busy")
	ErrInsufficientMana = errors.New("combat: insufficient mana")
	ErrDuplicateCommand = errors.New("combat: duplicate command")
	ErrBadCheckpoint    = errors.New("combat: bad checkpoint")
)

type Tick uint64

type EntityID string

type Vec2 struct {
	X int64 `json:"x"`
	Y int64 `json:"y"`
}

type Faction uint8

const (
	FactionNeutral Faction = iota
	FactionPlayer
	FactionMonster
)

type Actor struct {
	ID        EntityID          `json:"id"`
	Faction   Faction           `json:"faction"`
	Pos       Vec2              `json:"pos"`
	FacingMR  uint32            `json:"facing_mr"`
	HP        int64             `json:"hp"`
	MaxHP     int64             `json:"max_hp"`
	Mana      int64             `json:"mana"`
	MaxMana   int64             `json:"max_mana"`
	Version   uint64            `json:"version"`
	Cooldowns map[uint32]Tick   `json:"cooldowns,omitempty"`
	Statuses  map[uint32]Status `json:"statuses,omitempty"`
	RecoverAt Tick              `json:"recover_at"`
}

type Status struct {
	ID        uint32 `json:"id"`
	Stacks    uint32 `json:"stacks"`
	ExpiresAt Tick   `json:"expires_at"`
}

type CastIntent struct {
	CommandID        string
	SessionEpoch     uint64
	ClientSeq        uint64
	CasterID         EntityID
	SkillID          uint32
	SkillLevel       uint32
	TargetID         EntityID
	Aim              Vec2
	TargetTick       Tick
	ContentReleaseID string
	ContentHash      string
}

type CommandOutcome uint8

const (
	OutcomeScheduled CommandOutcome = iota + 1
	OutcomeCommitted
	OutcomeRejected
)

type CommandResult struct {
	CommandID string
	ClientSeq uint64
	Outcome   CommandOutcome
	Code      string
	Detail    string
	Tick      Tick
}

type EventKind uint8

const (
	EventCastStarted EventKind = iota + 1
	EventCastCancelled
	EventMissileSpawned
	EventMissileCollided
	EventMissileVanished
	EventHit
	EventHeal
	EventResourceChanged
	EventStatusApplied
	EventStatusRefreshed
	EventStatusExpired
	EventStatusRemoved
	EventRecoveryEnded
	EventDeath
)

type ResultFlag uint8

const (
	ResultCritical ResultFlag = iota + 1
	ResultDodged
	ResultKillingBlow
)

type StatusDelta struct {
	EffectID      uint32 `json:"effect_id"`
	Stacks        uint32 `json:"stacks"`
	ExpiresAtTick Tick   `json:"expires_at_tick"`
	Removed       bool   `json:"removed"`
}

type Event struct {
	ID              string               `json:"id"`
	Kind            EventKind            `json:"kind"`
	Tick            Tick                 `json:"tick"`
	SourceID        EntityID             `json:"source_id"`
	TargetID        EntityID             `json:"target_id"`
	SkillID         uint32               `json:"skill_id"`
	SkillLevel      uint32               `json:"skill_level"`
	HitIndex        uint32               `json:"hit_index"`
	Value           int64                `json:"value"`
	School          catalog.DamageSchool `json:"school"`
	TargetHPAfter   int64                `json:"target_hp_after"`
	TargetManaAfter int64                `json:"target_mana_after"`
	Impact          Vec2                 `json:"impact"`
	MissileID       uint32               `json:"missile_id"`
	AnimationID     uint32               `json:"animation_id"`
	VisualEffectID  uint32               `json:"visual_effect_id"`
	CastID          string               `json:"cast_id"`
	RNGAuditToken   []byte               `json:"rng_audit_token,omitempty"`
	StatusEffects   []StatusDelta        `json:"status_effects,omitempty"`
	Results         []ResultFlag         `json:"results,omitempty"`
}

type scheduledKind uint8

const (
	scheduledResolveCast scheduledKind = iota + 1
	scheduledMissileImpact
	scheduledStatusExpire
	scheduledRecoveryEnd
)

type scheduled struct {
	At       Tick          `json:"at"`
	Order    uint64        `json:"order"`
	Kind     scheduledKind `json:"kind"`
	Cast     activeCast    `json:"cast"`
	ActorID  EntityID      `json:"actor_id"`
	StatusID uint32        `json:"status_id"`
}

type activeCast struct {
	CastID   string        `json:"cast_id"`
	Intent   CastIntent    `json:"intent"`
	Skill    catalog.Skill `json:"skill"`
	HitIndex uint32        `json:"hit_index"`
}

type Instance struct {
	id            string
	release       catalog.Release
	tick          Tick
	actors        map[EntityID]*Actor
	lightEntities int
	scheduled     []scheduled
	nextOrder     uint64
	nextEventSeq  uint64
	lastEvents    []Event
	processed     map[string]CommandResult
	rng           splitmix64
	auditKey      []byte
}

type Option func(*Instance)

func WithAuditKey(key []byte) Option {
	return func(i *Instance) { i.auditKey = append([]byte(nil), key...) }
}

func NewInstance(id string, release catalog.Release, seed uint64, opts ...Option) (*Instance, error) {
	if err := release.Validate(); err != nil {
		return nil, err
	}
	if id == "" {
		return nil, errors.New("combat: empty instance id")
	}
	i := &Instance{
		id:        id,
		release:   release,
		actors:    make(map[EntityID]*Actor),
		processed: make(map[string]CommandResult),
		rng:       splitmix64{state: seed},
	}
	for _, opt := range opts {
		opt(i)
	}
	return i, nil
}

func (i *Instance) ID() string          { return i.id }
func (i *Instance) Tick() Tick          { return i.tick }
func (i *Instance) ReleaseID() string   { return i.release.ID }
func (i *Instance) ReleaseHash() string { return i.release.Hash }

func (i *Instance) AddActor(actor Actor) error {
	if len(i.actors) >= MaxActors {
		return ErrCapacity
	}
	if actor.ID == "" {
		return ErrUnknownActor
	}
	if actor.Cooldowns == nil {
		actor.Cooldowns = make(map[uint32]Tick)
	}
	if actor.Statuses == nil {
		actor.Statuses = make(map[uint32]Status)
	}
	copyActor := actor
	i.actors[actor.ID] = &copyActor
	return nil
}

func (i *Instance) AddLightEntity() error {
	if i.lightEntities >= MaxLightEntities {
		return ErrCapacity
	}
	i.lightEntities++
	return nil
}

func (i *Instance) Actor(id EntityID) (Actor, bool) {
	a, ok := i.actors[id]
	if !ok {
		return Actor{}, false
	}
	return cloneActor(*a), true
}

func (i *Instance) DrainEvents() []Event {
	if len(i.lastEvents) == 0 {
		return nil
	}
	out := append([]Event(nil), i.lastEvents...)
	i.lastEvents = nil
	return out
}

func (i *Instance) ProcessCast(intent CastIntent) CommandResult {
	key := commandKey(intent)
	if previous, ok := i.processed[key]; ok {
		return previous
	}
	result := i.processCast(intent)
	i.processed[key] = result
	return result
}

func (i *Instance) processCast(intent CastIntent) CommandResult {
	reject := func(err error) CommandResult {
		return CommandResult{CommandID: intent.CommandID, ClientSeq: intent.ClientSeq, Outcome: OutcomeRejected, Code: errorCode(err), Detail: err.Error(), Tick: i.tick}
	}
	if intent.ContentReleaseID != i.release.ID || intent.ContentHash != i.release.Hash {
		return reject(ErrContentMismatch)
	}
	caster := i.actors[intent.CasterID]
	if caster == nil || caster.HP <= 0 {
		return reject(ErrUnknownActor)
	}
	skill, ok := i.release.Skill(intent.SkillID, intent.SkillLevel)
	if !ok {
		return reject(ErrUnknownSkill)
	}
	if until := caster.Cooldowns[skill.ID]; until > i.tick {
		return reject(ErrCooldown)
	}
	if caster.RecoverAt > i.tick {
		return reject(ErrBusy)
	}
	if caster.Mana < skill.ManaCost {
		return reject(ErrInsufficientMana)
	}
	caster.Mana -= skill.ManaCost
	caster.Version++
	caster.Cooldowns[skill.ID] = i.tick + Tick(skill.CooldownTicks)
	caster.RecoverAt = i.tick + Tick(skill.CastTicks+skill.RecoveryTicks)
	cast := activeCast{CastID: intent.CommandID, Intent: intent, Skill: skill}
	i.emit(Event{Kind: EventCastStarted, SourceID: intent.CasterID, TargetID: intent.TargetID, SkillID: skill.ID, SkillLevel: skill.Level, CastID: cast.CastID, AnimationID: skill.AnimationID})
	i.schedule(i.tick+Tick(skill.CastTicks), scheduledResolveCast, cast, "", 0)
	if skill.RecoveryTicks > 0 {
		i.schedule(caster.RecoverAt, scheduledRecoveryEnd, activeCast{}, caster.ID, 0)
	}
	return CommandResult{CommandID: intent.CommandID, ClientSeq: intent.ClientSeq, Outcome: OutcomeScheduled, Code: "SCHEDULED", Tick: i.tick}
}

func (i *Instance) Advance(n uint32) []Event {
	var out []Event
	for step := uint32(0); step < n; step++ {
		i.tick++
		out = append(out, i.runDue()...)
	}
	return out
}

func (i *Instance) runDue() []Event {
	sort.SliceStable(i.scheduled, func(a, b int) bool {
		if i.scheduled[a].At == i.scheduled[b].At {
			return i.scheduled[a].Order < i.scheduled[b].Order
		}
		return i.scheduled[a].At < i.scheduled[b].At
	})
	for len(i.scheduled) > 0 && i.scheduled[0].At <= i.tick {
		item := i.scheduled[0]
		i.scheduled = i.scheduled[1:]
		before := i.nextEventSeq
		switch item.Kind {
		case scheduledResolveCast:
			i.resolveCast(item.Cast)
		case scheduledMissileImpact:
			i.resolveHit(item.Cast)
			i.emit(Event{Kind: EventMissileVanished, SourceID: item.Cast.Intent.CasterID, TargetID: item.Cast.Intent.TargetID, SkillID: item.Cast.Skill.ID, SkillLevel: item.Cast.Skill.Level, CastID: item.Cast.CastID, MissileID: item.Cast.Skill.MissileID})
		case scheduledStatusExpire:
			i.expireStatus(item.ActorID, item.StatusID)
		case scheduledRecoveryEnd:
			i.emit(Event{Kind: EventRecoveryEnded, SourceID: item.ActorID})
		}
		_ = before
	}
	return i.DrainEvents()
}

func (i *Instance) resolveCast(c activeCast) {
	caster := i.actors[c.Intent.CasterID]
	target := i.actors[c.Intent.TargetID]
	if caster == nil || caster.HP <= 0 || target == nil || target.HP <= 0 || distance(caster.Pos, target.Pos) > c.Skill.RangeMilli {
		i.emit(Event{Kind: EventCastCancelled, SourceID: c.Intent.CasterID, TargetID: c.Intent.TargetID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, CastID: c.CastID})
		return
	}
	if c.Skill.MissileID != 0 {
		fly := Tick(ceilDiv(distance(caster.Pos, target.Pos), c.Skill.MissileSpeedPerTick))
		if fly == 0 {
			fly = 1
		}
		i.emit(Event{Kind: EventMissileSpawned, SourceID: c.Intent.CasterID, TargetID: c.Intent.TargetID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, CastID: c.CastID, MissileID: c.Skill.MissileID, Impact: target.Pos})
		i.schedule(i.tick+fly, scheduledMissileImpact, c, "", 0)
		return
	}
	i.resolveHit(c)
}

func (i *Instance) resolveHit(c activeCast) {
	if c.Skill.MissileID != 0 {
		i.emit(Event{Kind: EventMissileCollided, SourceID: c.Intent.CasterID, TargetID: c.Intent.TargetID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, CastID: c.CastID, MissileID: c.Skill.MissileID})
	}
	target := i.actors[c.Intent.TargetID]
	if target == nil || target.HP <= 0 {
		return
	}
	for idx, effect := range c.Skill.Effects {
		hitIndex := uint32(idx)
		landed, token := i.effectLands(c, hitIndex, effect.ChancePermille)
		if !landed {
			continue
		}
		switch effect.Kind {
		case catalog.EffectDamage:
			i.applyDamage(c, hitIndex, target, effect, token)
		case catalog.EffectHeal:
			i.applyHeal(c, hitIndex, target, effect, token)
		case catalog.EffectResource:
			target.Mana = min64(target.MaxMana, target.Mana+effect.Value)
			target.Version++
			i.emit(Event{Kind: EventResourceChanged, SourceID: c.Intent.CasterID, TargetID: target.ID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, HitIndex: hitIndex, Value: effect.Value, TargetManaAfter: target.Mana, CastID: c.CastID, RNGAuditToken: token})
		case catalog.EffectStatus:
			i.applyStatus(c, target, effect.Status, token)
		}
	}
}

func (i *Instance) applyDamage(c activeCast, hit uint32, target *Actor, effect catalog.Effect, token []byte) {
	value := effect.Value
	target.HP = max64(0, target.HP-value)
	target.Version++
	flags := []ResultFlag(nil)
	if target.HP == 0 {
		flags = append(flags, ResultKillingBlow)
	}
	i.emit(Event{Kind: EventHit, SourceID: c.Intent.CasterID, TargetID: target.ID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, HitIndex: hit, Value: value, School: effect.School, TargetHPAfter: target.HP, CastID: c.CastID, RNGAuditToken: token, Results: flags, Impact: target.Pos, VisualEffectID: c.Skill.VisualEffectID})
	if target.HP == 0 {
		i.emit(Event{Kind: EventDeath, SourceID: c.Intent.CasterID, TargetID: target.ID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, CastID: c.CastID})
	}
}

func (i *Instance) applyHeal(c activeCast, hit uint32, target *Actor, effect catalog.Effect, token []byte) {
	value := effect.Value
	target.HP = min64(target.MaxHP, target.HP+value)
	target.Version++
	i.emit(Event{Kind: EventHeal, SourceID: c.Intent.CasterID, TargetID: target.ID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, HitIndex: hit, Value: value, TargetHPAfter: target.HP, CastID: c.CastID, RNGAuditToken: token, Impact: target.Pos, VisualEffectID: c.Skill.VisualEffectID})
}

func (i *Instance) applyStatus(c activeCast, target *Actor, def catalog.Status, token []byte) {
	existing, exists := target.Statuses[def.ID]
	if existing.Stacks < def.MaxStacks {
		existing.Stacks++
	}
	if !exists {
		existing.Stacks = 1
	}
	existing.ID = def.ID
	existing.ExpiresAt = i.tick + Tick(def.DurationTicks)
	target.Statuses[def.ID] = existing
	target.Version++
	kind := EventStatusApplied
	if exists {
		kind = EventStatusRefreshed
	}
	i.emit(Event{Kind: kind, SourceID: c.Intent.CasterID, TargetID: target.ID, SkillID: c.Skill.ID, SkillLevel: c.Skill.Level, CastID: c.CastID, RNGAuditToken: token, StatusEffects: []StatusDelta{{EffectID: def.ID, Stacks: existing.Stacks, ExpiresAtTick: existing.ExpiresAt}}})
	i.schedule(existing.ExpiresAt, scheduledStatusExpire, activeCast{}, target.ID, def.ID)
}

func (i *Instance) RemoveStatus(actorID EntityID, statusID uint32) bool {
	actor := i.actors[actorID]
	if actor == nil {
		return false
	}
	if _, ok := actor.Statuses[statusID]; !ok {
		return false
	}
	delete(actor.Statuses, statusID)
	actor.Version++
	i.emit(Event{Kind: EventStatusRemoved, TargetID: actorID, StatusEffects: []StatusDelta{{EffectID: statusID, Removed: true}}})
	return true
}

func (i *Instance) expireStatus(actorID EntityID, statusID uint32) {
	actor := i.actors[actorID]
	if actor == nil {
		return
	}
	status, ok := actor.Statuses[statusID]
	if !ok || status.ExpiresAt != i.tick {
		return
	}
	delete(actor.Statuses, statusID)
	actor.Version++
	i.emit(Event{Kind: EventStatusExpired, TargetID: actorID, StatusEffects: []StatusDelta{{EffectID: statusID, Removed: true}}})
}

func (i *Instance) effectLands(c activeCast, hit uint32, permille uint32) (bool, []byte) {
	if permille == 0 || permille >= 1000 {
		return true, nil
	}
	roll := i.rng.Next() % 1000
	token := i.auditToken(c.CastID, hit, roll)
	return roll < uint64(permille), token
}

func (i *Instance) emit(e Event) {
	i.nextEventSeq++
	e.ID = fmt.Sprintf("%s:%d", i.id, i.nextEventSeq)
	e.Tick = i.tick
	if e.RNGAuditToken == nil && e.CastID != "" {
		e.RNGAuditToken = i.auditToken(e.CastID, e.HitIndex, uint64(i.nextEventSeq))
	}
	// Event sink lives in persistence/transport seam; tests use DrainEvents via checkpoint replay.
	i.lastEvents = append(i.lastEvents, e)
}

func (i *Instance) schedule(at Tick, kind scheduledKind, cast activeCast, actorID EntityID, statusID uint32) {
	i.nextOrder++
	i.scheduled = append(i.scheduled, scheduled{At: at, Order: i.nextOrder, Kind: kind, Cast: cast, ActorID: actorID, StatusID: statusID})
}

func commandKey(intent CastIntent) string {
	if intent.CommandID != "" {
		return fmt.Sprintf("%d/%s", intent.SessionEpoch, intent.CommandID)
	}
	return fmt.Sprintf("%d/%d", intent.SessionEpoch, intent.ClientSeq)
}

func errorCode(err error) string {
	switch {
	case errors.Is(err, ErrContentMismatch):
		return "CONTENT_MISMATCH"
	case errors.Is(err, ErrUnknownActor):
		return "UNKNOWN_ACTOR"
	case errors.Is(err, ErrUnknownSkill):
		return "UNKNOWN_SKILL"
	case errors.Is(err, ErrCooldown):
		return "COOLDOWN"
	case errors.Is(err, ErrBusy):
		return "BUSY"
	case errors.Is(err, ErrInsufficientMana):
		return "INSUFFICIENT_MANA"
	default:
		return "REJECTED"
	}
}

type splitmix64 struct{ state uint64 }

func (s *splitmix64) Next() uint64 {
	s.state += 0x9e3779b97f4a7c15
	z := s.state
	z = (z ^ (z >> 30)) * 0xbf58476d1ce4e5b9
	z = (z ^ (z >> 27)) * 0x94d049bb133111eb
	return z ^ (z >> 31)
}

func (i *Instance) auditToken(castID string, hit uint32, roll uint64) []byte {
	key := i.auditKey
	if len(key) == 0 {
		sum := sha256.Sum256([]byte(i.id + ":audit"))
		key = sum[:]
	}
	mac := hmac.New(sha256.New, key)
	mac.Write([]byte(i.id))
	mac.Write([]byte(castID))
	var b [20]byte
	binary.BigEndian.PutUint64(b[0:8], uint64(i.tick))
	binary.BigEndian.PutUint32(b[8:12], hit)
	binary.BigEndian.PutUint64(b[12:20], roll)
	mac.Write(b[:])
	out := mac.Sum(nil)
	return out[:16]
}

func AuditTokenHex(token []byte) string { return hex.EncodeToString(token) }

func distance(a, b Vec2) int64 {
	dx := abs64(a.X - b.X)
	dy := abs64(a.Y - b.Y)
	// Manhattan distance: deterministic, cheap, content can compensate ranges.
	return dx + dy
}

func ceilDiv(a, b int64) int64 {
	if a <= 0 {
		return 0
	}
	return (a + b - 1) / b
}

func cloneActor(a Actor) Actor {
	a.Cooldowns = cloneMap(a.Cooldowns)
	a.Statuses = cloneMap(a.Statuses)
	return a
}

func cloneMap[K comparable, V any](in map[K]V) map[K]V {
	if in == nil {
		return nil
	}
	out := make(map[K]V, len(in))
	for k, v := range in {
		out[k] = v
	}
	return out
}

func abs64(v int64) int64 {
	if v < 0 {
		return -v
	}
	return v
}
func min64(a, b int64) int64 {
	if a < b {
		return a
	}
	return b
}
func max64(a, b int64) int64 {
	if a > b {
		return a
	}
	return b
}
