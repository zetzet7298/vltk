# Design

One shared enum owns physical variants: `Attack`/`Attack1`; mounted visual conversion owns `RideAttack`/`RideAttack1`/`RideMagic`. Catalog owns source paths. Controller delegates resolver to catalog; no second CharAnim table.

Equipment retains both the broad PC family and the concrete resource variant.
Short variants are `1-6,19-22`, long variants are `7-12,23-26`, and dual
variants are `13-18,27-30`. Knife/staff/dual-hammer subclasses swap only the two
physical banks; magic and mounted banks stay canonical.

Forced action clock follows the current canonical branch: start with 20 ticks,
integer-divide by `(100 + speed)`, round down to an even tick count, minimum one,
and run at `GAME_FPS=18`. Effect emission is at integer
`floor(totalTicks * 60 / 100)` and recovery at the final tick.
`ignoredWaitTimeSeconds` preserves caller ABI but is not consumed. Movement input
and target movement lock only while the clock runs. HorseLimit gates before
resolve/visual mutation.

`KNpcRes::Draw` scales shadow independently, then scans numeric player parts from
Head `(0)` through HorseRear `(14)`. The first loaded part computes one
direction-inclusive absolute sprite index from logical action progress; every
following body, hair, hand, weapon and horse layer receives that exact index.
Unity mirrors this driver rule. It does not normalize each layer independently:
an absent index disables that renderer and a required absence keeps the visual
fail-closed. Direct visual tests retain the standalone cadence path only outside
the controller-owned logical cast clock.

This story owns player cast pose/layer playback and the shared cast clock. Skill
missile/start/fly/collide/vanish VFX, attached buff/debuff loops and audio remain
per-skill evidence in `SKL-ALL-PARITY-001`; green pose tests cannot close those
presentation dimensions.
