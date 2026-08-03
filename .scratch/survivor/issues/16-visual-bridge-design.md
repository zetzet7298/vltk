# 16 — Decision: visual bridge (actor / missile / effect) via IActorVisual

Type: `grilling`
Status: `closed`
Blocked by: 08
Closed: 2026-08-02 (P1.5 implement + verified)

## Question

Thiết kế adapter `IActorVisual` JX bridge (P1.5): spawn player (`MalePlayerVisual`/
`FemalePlayerVisual`), monster (`PcNpcVisual`), missile + skill-effect (`SkillEffectVisualService`/
`MissileSpawner`) qua Sandbox API read-only (KHÔNG sửa Sandbox). Quyết định adapter contract +
SPR fail-closed gate + fallback proxy màu (P1). Dựa research 08.

## Decisions

### Scope (Q1): player visual only (A)

P1.5 = `MalePlayerVisual` adapter prove `IActorVisual` seam. Monster giữ `ProxyActorVisual`.
VFX (`SkillEffectVisualService`) + audio (`AudioService`) = P2 (đi cùng skill library ticket 04/13).
Lý do: AGENTS define P1.5 = "MalePlayerVisual adapter"; VFX cần SkillCatalog (chỉ có ý nghĩa khi skill
library port); audio wire dead nếu không có skill cast.

### Hero spec + scale (Q2): (A1) + (S1)

- (A1) 1 default male hero, variant hardcoded `MA_BD_019` (catalog `ArmorVariant=019`), self-contained
  config trong adapter. Hero select/variant library = P2.
- (S1) `pixelsPerUnit` native field. Setup thực tế: **ppu=40** (không phải 180 dự kiến). Lý do: SPR part
  cropped tight (body sprite ~64px, không 200px full frame) → assembled char 0.7 unit @ ppu 110. ppu=40
  → char 1.9 unit tall (16% view height), survivor feel portrait mobile.

### Fail-closed (Q3): sentinel probe (F4)

Probe disk `SpritesRuntime/{filename}.spr` TRƯỚC khi add `MalePlayerVisual`; miss → `ProxyActorVisual`
thẳng. Không create-destroy-flicker. Sentinel = `MA_BD_019_ST01.spr` (filename, không phải uid —
SpritesRuntime lưu theo filename cho batch này).
Lý do bỏ `SprRuntimeService` dep: asmdef Survivor chưa ref VLTK.Sprites; filename probe đủ (lookup
thứ tự trong root: filename trước uid).

### Structure (Q4): (C1) + (U1), Y-sort defer

- (C1) `JxPlayerVisual : MonoBehaviour, IActorVisual` wrapper + inner `MaleBridge` forward
  IActorVisual → MalePlayerVisual. Director.SpawnPlayer 1 dòng: `AddComponent<JxPlayerVisual>()`.
- (U1) `playAutomatically=true` native (MalePlayerVisual tự Update frame).
- **Y-sort defer**: MalePlayerVisual own sortingOrder (`MapRenderer.PlayerSortingOrder` + per-part offset),
  không override. Monster P1.5 vẫn Proxy → Y-sort refine = P2 khi monster cũng PC visual.

## Implementation

`Assets/Scripts/Survivor/Actor/JxPlayerVisual.cs` (~70 dòng):
- `Awake`: probe sentinel → `MalePlayerVisual` (+ `RefreshActionParts(force:true)` sau set ppu vì
  Awake chạy ppu=1 default) hoặc `ProxyActorVisual`.
- `MaleBridge`: SyncPosition (transform), SetDirection, PlayMove (SetAction Move/Idle), SetAlive
  (disable SR + component).
- Config fields: `defaultStandPath`, `pixelsPerUnit=40`, `fallbackColor`, `fallbackSize`.

`Assets/Scripts/Survivor/SurvivorGameDirector.cs` SpawnPlayer: swap `AddComponent<ProxyActorVisual>`
→ `AddComponent<JxPlayerVisual>()`.

## Verification (play mode probe)

- `MalePlayerVisual.HasAllRequiredParts=true`, `MissingRequiredPartCount=0`, `LoadedPartCount=8`
  (Shadow/Body/Head/Hair/LeftHand/RightHand/LeftWeapon/RightWeapon).
- `charBoundsH=1.925 unit` @ ppu=40 (đúng scale).
- Screenshot `survivor-p15-bridge-with-monsters.png`: JX character (blue tunic, white sash, topknot
  hair — martial artist) + 10 red monster proxies spawn.
- Warning `[MalePlayer] SPR file not staged: MA_SH_019_ST01.spr` = **noise**, không fail. Catalog
  `IsShoulderRequired(019)=false` (SH_019 absent per package.ini audit, kept for provenance).
- Spawning logic P1 không đổi: force tick → 12 monsters.

## Note

- Editor unfocused = Time không advance (frame stuck). Play test cần editor focus hoặc build.
  Không bug, chỉ hạn chế smoke-probe tự động.
- Bridge prove `IActorVisual` seam hoạt động: P1 proxy màu → P1.5 JX SPR, 0 sửa Sandbox.
