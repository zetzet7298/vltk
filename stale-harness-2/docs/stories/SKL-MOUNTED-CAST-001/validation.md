# Validation

## PC authority and asset provenance

The PC loader is disk-first only when a loose file exists; this canonical tree has no
loose `spr/npcres` player tree, so the selected winner is package index `23` (`spr.pak`)
according to `package.ini`. No historical `050`/`016` candidate is used.

`PcPlayerCastSprites.provenance.json` schema v2 records 273 currently requested logical SPR rows:

- 237 exact UID bytes staged, 36 missing (15 required, 21 optional).
- Required holes remain fail-closed only for female `FM_HR_019` cast/mount rows.
  Optional holes include male `MA_SH_019` and female long-weapon left overlays.
  Three shared female mounted `FM_LW_000` rows carry exact per-cell
  required/optional attribution; male mounted `MA_LW_000` is required for all
  of its requested cells.
- Fictional `AT08` rows were removed: PC hidden Attack/Attack1 use `MG01`, while
  hidden Magic uses `MG02`.
- `PcPlayerMountedMotionSprites.provenance.json` records 84 `RD01`/`HW01`/`HR01`
  mounted locomotion rows: 78 staged, 3 required female-hair holes, 3 optional
  male-shoulder holes.
- `PcHorseBodySprites.provenance.json` records the five exact horse bodies
  (`horse001/003/005/007/009`), all staged from `spr.pak` index 23.

Every staged record contains the vltktool UID/path bytes, package winner, SHA-256,
byte count and SPR total-frame metadata. The current parser records
`directions: null` and `interval_ticks: null`; direction count is a separately
pinned player-resource rule, not metadata claimed from the SPR header. No fallback
variant is silently substituted.

## Behavioral and renderer proof

`PcMountedCastPresentationParityTests` covers both sexes, foot/mounted empty, short,
long, dual and hidden families; `HA01`/`HA02`/`HM01`; CharAnim `9/10/11`; HorseLimit;
the 60% effect point; 100% recovery; and ignored WaitTime.

`PcPlayerHandMotionParityTests` retains 60 provenance-backed family-default cases
(2 sexes × 2 mount states × 5 weapon families × 3 cast actions). It checks every
integer logical tick `0..19` through all eight directions and verifies the
first-loaded-layer absolute index on all staged layers.

The same fixture adds 360 subclass cases (30 melee variants × 2 sexes × 2 mount
states × 3 cast actions): 57,600 direction-tick visits and 172,800 Body/LH/RH
absolute-index assertions. It pins sword/knife, spear/staff and
dual-sword/dual-hammer physical-order swaps, exact `LW/RW` variant paths, and
fail-closed required overlays when bytes are outside the default provenance set.

The shared effect lifecycle follow-up now has a focused four-case fixture for an
active missile, a stationary zone, a persistent state aura, and a passive row
with no canonical visual. `CreateSubEffect` reuses the canonical cast path and
keeps a missing-visual sub-skill `Finished` instead of reviving it as a generic
impact; lifecycle depth/ancestor guards remain bounded.

Latest Unity evidence (Unity `6000.4.7f1`, instance
`vltk-mobile@244c0d539f780309`):

- EditMode focused mounted-cast presentation fixture: 33/33 (`61d6b89aa7df4df0878b807b7d0a2072`).
- EditMode post-correction player/mount/cast/equipment plus movement/animation/VFX/audio/golden group: 572/572 (`5c096424dacd42c48671cadf267902c4`).
- EditMode lifecycle follow-up (including the new four-case fixture): 576/576 (`d8bcd414d55644bbb7bc1e776eba880a`).
- Focused lifecycle fixture: 4/4 (`9921826edea44383a1cf001f26f6a771`).
- PlayMode golden capture after the lifecycle follow-up: 2/2 (`4c772529d79540d79b247f08fcff040a`).
- Mounted recast follow-up: focused parser/runtime/missile-source group 19/19
  (`b3ff3f14087a42b29f8a489b857826cb`), including foot/mounted/zero/aura,
  mod-parser propagation and all seven canonical mounted IDs. Final related
  presentation/runtime group 595/595 (`fd0ca38cee244ddebb2a647e873e4382`).
- Latest PlayMode golden after runtime cadence wiring: 2/2
  (`28cfe580ef0e48c3b871a3f7c9c1049c`).
- Unity compile after forced script refresh: 0 compile errors. The PlayMode test
  runner emits its internal `Saving results to .../TestResults.xml` diagnostic as
  Console type `Exception`; both tests pass and no gameplay stack trace is attached.
  Existing unrelated performance/deprecation warnings remain outside this wave.

An unrelated existing test remains red when run separately:
`CombatSkillSlotTests.CombatRuntime_BuffStates_ApplyAddedDamageAndResistances`
computes both compared damage values as zero. No file in that combat-damage slice
was changed for this presentation wave.

## Commands

```bash
cd /var/www/vltk-mobile
python3 scripts/compile_scripts.py
git diff --check
# Unity EditMode filters:
# PcPlayerHandMotionParityTests, FemalePlayerVisualTests, MalePlayerVisualTests,
# MountVisualTests, PcMountedCastPresentationParityTests,
# TangMenFlyEventRuntimeTests, PcSkillVisualAutoMapperAudioTests, GoldenSnapshotTests
# Unity PlayMode filter: GoldenSnapshotCapturePlayModeTests
```

## Limits and release gate

The behavioral suites and staged-byte checks pass, but the full PC visual matrix is not
complete: required female hair rows are absent from the supplied PC package set.
Non-default weapon overlay variants have catalog/runtime path and fail-closed proof,
not a complete package-winner/byte provenance set.
PC-vs-Unity framebuffer/SSIM, camera composition under every direction,
Android/device playback, and a live-PC golden capture are still unresolved. Do not claim
99% pixel parity or ship-ready completeness until those exact PC bytes and device/golden
comparisons are supplied.
