# Defender State Damage Evidence

## PC evidence
Reference file: `Assets/StreamingAssets/Reference/KNpc.cpp`.

Relevant PC behavior:
- `KNpc::CalcDamage` chooses defender current resist by damage type:
  - `damage_physics -> m_CurrentPhysicsResist`
  - `damage_cold -> m_CurrentColdResist`
  - `damage_fire -> m_CurrentFireResist`
  - `damage_light -> m_CurrentLightResist`
  - `damage_poison -> m_CurrentPoisonResist`
- It caps each current resist by the corresponding `m_CurrentXxxResistMax`, then global `MAX_RESIST`.
- It applies resistance after armor and other modifiers:

```cpp
nDamage -= nDamage * nRes / MAX_PERCENT;
```

`ReceiveDamage` then calls `CalcDamage` per magic attribute slot, so any active defender state that modifies current resist must affect incoming skill damage.

## Mobile parity target
`CombatRuntimeService.ApplyDamage` must convert `target.states` into `DefenderStats` before calling `DamageFormulaService`.

Current mobile implementation reads:
- `AllResP` from `target.states`
- type-specific resist states (`PhysicsResP`, `FireResP`, `ColdResP`, `LightingResP`, `PoisonResP`)
- physics armor alias through `AddDefenseV`

## Test lock
Added/updated CaiBang test:

```text
CaiBangCombatParityTests.CaiBang_117_DefenderAllResStateReducesIncomingDamage
```

The test casts Cái Bang skill `117` twice with deterministic hit RNG and identical Unity random seed:
1. baseline target with no resist state;
2. target with `AllResP=50`.

Expected result: resisted target receives less final damage. This locks the PC `CalcDamage` defender-resist path for Cái Bang active damage.
