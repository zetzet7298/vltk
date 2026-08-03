# 08 — Research: Visual + VFX + Audio Bridge (IActorVisual adapter)

Status: `done` (sub-agent `research/visual-vfx-audio-bridge`)
Date: 2026-08-02
Scope: bridge JX visual/VFX/audio vào Survivor qua `IActorVisual`, KHÔNG sửa Sandbox.
Rule tuân thủ: fail-closed SPR, không bịa path, cite file path cho mọi claim.

---

## 0. Tóm tắt

- Sandbox đã có đủ public surface để adapter gọi read-only: `SandboxManager.Instance.SkillEffectVisual`
  (VFX) + `SandboxManager.Instance.AudioService` (audio) + `MalePlayerVisual`/`FemalePlayerVisual`/
  `PcNpcVisual` (character SPR playback, self-contained MonoBehaviour).
- `SkillEffectVisualService` tự resolve precast/missile/impact SPR + sound từ PC data
  (`missles1.txt` 57 cột qua `PcSkillVisualAutoMapper`), đã fail-closed sẵn:
  không có PC sprite → `phase = Finished`, durations = 0, không bịa timeline.
- SPR staging root THỰC TẾ = project root `/SpritesRuntime` (67,499 file), KHÔNG phải
  `Assets/StreamingAssets/Sprites` (1,160 file — root này KHÔNG nằm trong search paths mặc định của
  `SprRuntimeService`). Ghi chú trong AGENTS.md/issue hơi lệch; adapter phải theo code.
- Audio: `AudioService.PlaySkillCast(pcSoundPath)` đã handle PC path `\sound\skill\*.wav` →
  `Assets/StreamingAssets/sound/skill/` (28 wav). BGM: `AudioService.PlayBGM(id)` với def mặc định
  `Audio/BGM/*.ogg`. MusicService/PcMusic registry tồn tại nhưng chỉ metadata (chưa có clip staged).
- Adapter contract đề xuất: giữ `IActorVisual` 4 method hiện có, thêm optional interface
  `IJxVisualBridge` (spawn/play-cast-vfx) — ProxyActorVisual giữ nguyên cho P1, bridge P1.5 là
  implementation thứ 2.

---

## 1. Sandbox visual services — public surface (API table)

Assembly: `VLTK.Sandbox.Runtime` (`Assets/Scripts/Sandbox/VLTK.Sandbox.asmdef`).
Survivor asmdef `VLTK.Survivor.Runtime` đã reference Sandbox (`Assets/Scripts/Survivor/VLTK.Survivor.Runtime.asmdef`)
→ adapter gọi trực tiếp được, không cần sửa asmdef.

### 1.1 `IPlayerVisual` — `Assets/Scripts/Sandbox/IPlayerVisual.cs` (interface, 50 dòng)

| Member | Type | Read-only-safe? | Ghi chú |
|---|---|---|---|
| `currentAction` | `PlayerVisualAction` prop | có (set = state machine) | enum: Idle/Move/Walk/Attack/Attack1/Magic/Sit/Ride*/... |
| `currentWeapon` | `PcWeaponType` prop | có | EmptyHand..? |
| `walkMode` / `isMeditating` / `isMounted` / `direction` / `playAutomatically` | props | có | walkMode → WK01 thay RN01 |
| `LoadedPartCount` / `ActionPartsRefreshCount` / `CurrentFrameInDirection` | int props (get) | có | diagnostics |
| `HasAllRequiredParts` / `MissingRequiredPartCount` / `LastMissingRequiredParts` | get-only | có | **fail-closed status surface** |
| `LastMoveInput` / `IsMounted` | get-only | có | |
| `GetCurrentDirection()` / `GetRiderSortingOrder()` | int | có | |
| `SetMoveInput(Vector2)` | void | có | drive action+direction từ input |
| `SetAction(PlayerVisualAction)` | void | có | sticky meditate/mounted remap |
| `SetMounted(bool)` / `SetWeapon(PcWeaponType[,int])` / `SetDirection(int)` / `SetEquipVariant(slot,variant)` | void | có | |
| `SetLogicalActionProgress(float)` | void | có | cast progress; <0 resume Tick |
| `Tick(float dt)` | void | có | frame advance (auto nếu playAutomatically) |

### 1.2 `MalePlayerVisual` / `FemalePlayerVisual` — Sandbox/*.cs

- `MalePlayerVisual.cs` (628 dòng), `FemalePlayerVisual.cs` (574 dòng): `sealed MonoBehaviour, IPlayerVisual`.
- Khởi tạo: `Awake()` → `RefreshActionParts(force:true)`; tự chạy `Update()` nếu `playAutomatically`.
- SPR load: `ReadSprData(spritesRoot, sourcePath)` (MalePlayerVisual.cs:595) — direct file read,
  uid = `SprRuntimeService.ComputePathUidHex(sourcePath)` (GB2312 signed, default), fallback tên file.
  Static `ClipCache` cleared ở `SubsystemRegistration` (domain-reload off safe).
- Field config: `referencePixel`, `pixelsPerUnit`, `spritesRootOverride`, `armorVariant/headVariant/
  hairVariant/weaponVariant/mountHorseVariant`, frame rates per action.
- `MalePlayerSpriteCatalog` (cùng folder): variant mapping + `DirectionFromMove`, `SortingOffset`.
- Part source paths: từ `male_player_sprites.json` / `female_player_sprites.json`
  (`Assets/StreamingAssets/male_player_sprites.json` — có trong listing).
- **Adapter dùng**: add component lên actor GameObject, set `spritesRootOverride` (nếu cần), set weapon/
  equip variant theo Survivor hero, gọi `SetMoveInput` + `SetAction` hàng frame. Read-only cho Sandbox.

### 1.3 `PcNpcVisual` — `Assets/Scripts/Sandbox/PcNpcVisual.cs` (396 dòng)

- `sealed MonoBehaviour`, KHÔNG implement IPlayerVisual. API: `Configure(standPath, walkPath, refPixel)`,
  `SetMoveInput(Vector2)`, `Tick(float)`, props `HasWalkClip/HasAnyClip/HasShadow/FramesPerDirection/DirectionCount`.
- Shadow mặc định `spr\npcres\man\MA_YY_999_ST01.spr` / `RN01.spr` (PcNpcVisual.cs:33-34).
- Sprite path từ `NpcTemplate.spriteSourceId` (template data) → resolve qua registry assets.
- **Adapter dùng cho monster**: stand/walk 2-clip đơn giản, đủ cho Survivor monster (không cần 8-action).

### 1.4 `NpcTemplateRegistry` — `Assets/Scripts/Sandbox/NpcTemplateRegistry.cs` (107 dòng)

| Member | Type | Ghi chú |
|---|---|---|
| `Register(NpcTemplate)` / `Resolve(int)` / `Contains(int)` / `All` / `Count` | pure C# | template: `templateId, spriteSourceId, scriptRef, DisplayName, spriteResolved` |
| `ValidateResources()` | list issues | cần `IAssetRegistry`; null registry → all unreported |

Template data nguồn: `NpcSFullService` / `PcNpcSFullParser` (StreamingAssets/Reference/PcNpcS.txt).
Adapter monster visual: resolve template → `spriteSourceId` → SPR path → `PcNpcVisual.Configure`.

### 1.5 `SkillEffectVisualService` — `Assets/Scripts/Sandbox/SkillEffectVisualService.cs` (1,599 dòng)

`public class` (không phải MonoBehaviour — được Update ngoài). Surface chính:

| Member | Signature | Read-only-safe | Ghi chú |
|---|---|---|---|
| ctor | `(SprRuntimeService)` / `(SprRuntimeService, SkillCatalog)` | — | catalog = CombatSkillCatalog |
| `OnCastSound` | `Action<string>` (pcPath) | — | **event; Sandbox wire sẵn** (mục 4) |
| `OnMissileCollided` | `Action<ActiveSkillEffect,int,Vector2>` | — | |
| `PlaySkillCast(skill, casterPos, targetPos, skillLevel[, getCurrentTargetPos, onMissileCollided, suppressCastAudio])` | → `ActiveSkillEffect` | ✅ | full precast→missile→impact; data-driven; fail-closed |
| `PlayHitFlash(pos, color, dur)` / `PlayBuffAura(pos, color, dur, radius, label)` | → `ActiveSkillEffect` | ✅ | fallback FX |
| `SpawnAuthoritativeMissile(id, skill, from, to, level)` + `UpdateAuthoritativeMissile` / `CollideAuthoritativeMissile` / `VanishAuthoritativeMissile` | server-owned | ✅ | dành P3 backend |
| `SynchronizeStateAuras(actorId, ...)` / `RemoveStateAurasForActor(int)` / `ClearActiveEffects()` | int | ✅ | cleanup |
| `Update(float dt)` / `ActiveEffectCount` / `GetActiveEffects()` | | ✅ | adapter gọi Update mỗi frame |
| `ResolveStateAuraDurationSeconds(skill, level)` | static | ✅ | 18Hz tick → s |

`ActiveSkillEffect` public fields (dòng 1451+): phase, preCastSprite, missileSprite, preCastDuration,
missileForm, missileSpeed(324f default = PC 18/tick×18Hz), missileCount, pcPreCast/Missile/Impact SpriteKey +
TotalFrames/Directions/IntervalTicks, flightSoundPath, impactSoundPath, color, isAura, authoritativeLifecycle.

Fail-closed trong `PlaySkillCast` (dòng 620-629):
```csharp
if (!effect.isAura && !effect.HasPcPreCastSprite && !effect.HasPcMissileSprite && !effect.HasPcImpactSprite)
{
    // ponytail: no canonical PC art. Fail closed, not fake a timeline.
    effect.preCastDuration = 0f; effect.impactDuration = 0f; effect.missileDuration = 0f;
    effect.missileCount = 0; effect.phase = SkillEffectPhase.Finished;
}
```
→ adapter KHÔNG cần tự check: service đã trả Finished khi thiếu art.

### 1.6 `PcSkillVisualAutoMapper` — `Assets/Scripts/Sandbox/PcSkillVisualAutoMapper.cs`

- `PcSkillVisualConfig`: `skillId, missileId, preCastSprPath(hasPreCast), flightSprPath(flightFrames/
  flightDirections/flightIntervalTicks), missileSpeed, missileLifetime, isStationary, explodeSprPath,
  flightSoundPath (SndFile2 = MS_DoFly), impactSoundPath (SndFile4 = MS_DoCollision), hasStateAura...`.
- Public: `GetVisualConfig(SkillDefinition)` (dòng 150), `PreCacheAll(SkillCatalog)` (165),
  `Initialize(streamingAssetsPath)`, static `SprPathToKey`.
- Nguồn data: `PcMissileFullVisualParser` đọc `Assets/StreamingAssets/Reference/PcAttrib/missles1.txt`
  (514 dòng, 57 cột). Audio-only missile (SndFile2 nhưng không flight SPR, ví dụ 352/128) vẫn cho
  `flightSoundPath` — parity audio giữ được dù visual Finished.

### 1.7 Renderer của VFX (không phải service, note thôi)

- `Assets/Scripts/UI/SkillEffectWorldOverlay.cs` (MonoBehaviour, GameHudController tự add dòng 241-242):
  vẽ precast ring + missile dots + impact bằng LineRenderer/SpriteRenderer, layer `SkillFxLayerName`,
  `sortingOrder = 32000`. Overlay này là UI-world presenter gắn với GameHud — **Survivor KHÔNG nên reuse**
  (nó đọc camera riêng + layer golden-capture). Adapter tự dựng presenter đơn giản hơn từ `ActiveSkillEffect`
  (hoặc port nhỏ vào Survivor asmdef).
- `Assets/Scripts/UI/SkillEffectRenderer.cs` (class thuần, ctor `(service, camera)`, `Render()`):
  dùng cho golden snapshot; có thể reuse pattern nhưng camera binding khác.

### 1.8 `MissileSpawner` — `Assets/Scripts/Sandbox/MissileSpawner.cs` (184 dòng)

| Member | Ghi chú |
|---|---|
| `SpawnMissiles(skill, origin, target, childCount, speedOverride)` | tạo ProjectileInstance qua ProjectileService; forms Single/Fan/Surround/Chain/None |
| `UpdateMissiles(dt, targets)` | step + collision 16px, `OnMissileHit` event |
| `AttachHost(IMissileSpawnerHost)` | host hooks = Sandbox-specific (OnSpawnStart/ShowSkillEffect/PlayMissileSFX/...) |

**Kết luận adapter**: `MissileSpawner` gắn chặt ProjectileService + IMissileSpawnerHost (host = Sandbox
combat layer). Survivor đã có `Projectile.cs` + damage riêng → **KHÔNG dùng MissileSpawner**; chỉ dùng
`SkillEffectVisualService.PlaySkillCast` cho visual (nó tự bay missile visual không cần host).

### 1.9 `PcText` — `Assets/Scripts/PortData/PcText.cs` (197 dòng)

| Member | Ghi chú |
|---|---|
| `ReadLines(path, Encoding)` | UTF-8/GBK |
| `ReadLinesTcvn3(path)` | TCVN3 (PcAllFactionLearnedDisplaySkills.txt) |
| `Tcvn3ToBytesMultiple(string)` | sang byte map |
| static `Tcvn3Table` (private) | dùng cho name decode; adapter không cần trực tiếp nếu dùng catalog DisplayName |

---

## 2. Adapter design cho IActorVisual

### 2.1 Hiện trạng P1

`Assets/Scripts/Survivor/Actor/IActorVisual.cs` (6 dòng):
```csharp
public interface IActorVisual
{
    void SyncPosition(Vector3 worldPos);
    void SetDirection(int dirIndex8);
    void PlayMove(bool moving);
    void SetAlive(bool alive);
}
```
`ProxyActorVisual` (P1): SpriteRenderer màu, 4 method no-op/đơn giản. Dùng bởi `SurvivorPlayer`/
`SurvivorMonster` (AddComponent + SetColor theo actor).

### 2.2 Contract boundary (giữ Sandbox nguyên vẹn)

- Sandbox KHÔNG sửa. Adapter = class MỚI trong `VLTK.Survivor` (cùng asmdef, đã ref Sandbox).
- Gọi read-only: khởi tạo service instance riêng (`new SprRuntimeService()`,
  `new SkillEffectVisualService(spr, catalog)`) — KHÔNG bắt buộc qua `SandboxManager.Instance`.
  Nhưng nếu cần skill catalog + data-driven visual: `SandboxManager.Instance.CombatSkillCatalog` là public
  (SandboxManager.cs:1644 area). Note: SandboxManager là singleton scene-bound — Survivor scene
  (Survivor.unity) hiện KHÔNG có SandboxManager; nếu bridge cần catalog phải tự build
  (`PcCombatCatalogFactory.CreateNoviceCoreSectAndModCatalog`) hoặc dùng SandboxManager khi 2 scene chạy
  chung. **Khuyến nghị**: adapter nhận `SkillCatalog` qua injection (SurvivorGameDirector khởi tạo), tránh
  singleton dependency.
- `IActorVisual` giữ 4 method (SurvivorPlayer/Monster không đổi). Bridge implement:
  - `SyncPosition` → `transform.position` (+ sort order theo Y nếu cần)
  - `SetDirection` → `SetDirection(dirIndex8)` / `SetDirection` của visual
  - `PlayMove(bool)` → `SetMoveInput(moving ? lastDir : zero)` (visual tự chọn Move/Idle action)
  - `SetAlive(bool)` → `gameObject.SetActive` + dừng Tick khi dead

### 2.3 Optional: `IJxVisualBridge` (P1.5 surface, KHÔNG phá interface cũ)

```csharp
// Assets/Scripts/Survivor/Actor/IJxVisualBridge.cs (đề xuất, chưa implement)
public interface IJxVisualBridge
{
    void SpawnPlayerVisual(SurvivorHeroSpec spec);   // Male/FemalePlayerVisual + variants
    void SpawnMonsterVisual(NpcTemplate template);   // PcNpcVisual stand/walk
    void PlaySkillCast(SkillDefinition skill, Vector2 casterPos, Vector2 targetPos, int level,
                       Action<ActiveSkillEffect,int,Vector2> onHit = null); // → SkillEffectVisualService
    void UpdateVisuals(float dt);                     // Tick visual + skill effect service
    void Clear();
}
```
- `SurvivorHeroSpec`: faction/gender/weapon/armorVariant — map từ `PlayerAppearanceMapper`
  (`Assets/Scripts/Sandbox/PlayerAppearanceMapper.cs`) nếu muốn variant chuẩn PC.
- Skill cast từ SurvivorPlayer: `SkillDefinition` đã có sẵn trong catalog; game loop vẫn tự tính damage,
  `PlaySkillCast` chỉ trình diễn (missile visual tự bay, không gây damage — service không có damage callback
  local, chỉ `OnMissileCollided` event optional).

### 2.4 Rủi ro contract

- `MalePlayerVisual` cần `SpriteRenderer` trên cùng GameObject + sorting order; P1 Proxy cũng dùng
  SpriteRenderer — replace = destroy proxy, add player visual.
- `MalePlayerVisual` referencePixel 160,200 / pixelsPerUnit 1 = PC pixel scale; Survivor camera ortho size 6
  + XY world units — cần scale factor khi sync position (số liệu để to-spec chốt).
- `PcNpcVisual` chỉ 2 clips (stand/walk) + direction từ move input — đủ cho monster AI chase/contact;
  attack animation monster = không có (fail-closed, giữ proxy hit-flash nếu cần).

---

## 3. SPR staging path + fail-closed

### 3.1 Thực tế staging (code, không phải doc)

- `SprRuntimeService` default root = `Path.Combine(Application.dataPath, "..", "SpritesRuntime")`
  → **project root `/SpritesRuntime`** (`Assets/Scripts/Sprites/SprRuntimeService.cs:55-64`). Hiện có
  **67,499 file `.spr`** (đã kiểm tra disk).
- Search roots (`EnumerateSpriteRoots`, SprRuntimeService.cs:286-305): `_spritesRoot`,
  `<root>/SkillIcons`, `<project>/Generated/MapSprites`, `Generated/NpcSprites`, `Generated/ObjectSprites`.
- Lookup thứ tự trong 1 root (`FindSprDataInRoot`, :316-378): `{sanitizedKey}.spr` →
  `{filename}.spr` → `{uid8hex}.spr` (filename 8-hex) → **signed** `ComputePathUidHex(path)` (PC-accurate,
  comment ghi rõ unsigned miss `\spr\Ui\技能图标\icon_sk_ty_at.spr`) → unsigned hash.
- **`Assets/StreamingAssets/Sprites/` (1,160 file) KHÔNG nằm trong search paths mặc định** — chỉ được tìm
  nếu ctor nhận root tường minh. Đây là điểm lệch giữa AGENTS.md/issue ("Assets/StreamingAssets/Sprites/
  {hash}.spr") và code. Adapter dùng mặc định → không đụng StreamingAssets/Sprites.

### 3.2 Fail-closed hiện có

- `ResolveSprite` miss → `_missCache`, log Warn, return null (không fallback procedural trong path chính;
  `ResolveSpriteOrDefault` mới có fallback màu — chỉ dùng khi chủ động).
- Player visual: `ReadSprData` null → `LogMissing` → `HasAllRequiredParts=false` +
  `MissingRequiredPartCount` (IPlayerVisual exposes) — visual giữ frame cũ, không crash.
- Skill VFX: thiếu cả 3 sprite → `phase = Finished` ngay (SkillEffectVisualService.cs:620-629).
- `SprRuntimeService` có `SprValidator` diagnostic (`GetDiagnostic/GetAllDiagnostics`).

### 3.3 Staging SPR mới (khi cần thêm art cho skill Survivor)

- Tool: `C:/Projects/vltktool/` — `resolve_uid.py`, `extract_item_spr.py`, `find_spr_by_image.py`,
  `scan_required_spr.py`, `spr_encoder.py`.
- Winner theo package priority: `C:/Projects/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/
  client/package.ini` — 31 paks, `sound.pak` idx 30, `spr.pak` idx 23, `update01.pak` idx 21 (Vietnamese
  override). Rule: dùng vltktool resolve, không tự suy luận.
- KHÔNG bịa path: missile/precast path từ `PcSkills.txt` (PreCastSpr col) + `missles1.txt` (AnimFile1..4)
  = GBK bytes → hash `SprRuntimeService.ComputePathUidHex` (GB2312 signed+unsigned). Path thật nằm trong
  `PcSkillVisualConfig`/`PcMissileFullVisual` — adapter đọc từ đó, không tự hash.

---

## 4. Audio: inventory + pipeline

### 4.1 AudioService — `Assets/Scripts/Sandbox/AudioService.cs` (421 dòng) — CÓ, dùng được

- Pure C#, `Initialize(Transform root)` tạo BGM source + 8-source SFX pool. Không cần scene setup.
- API: `PlayBGM(id)`, `StopBGM()`, `PlaySFX(id, volScale)`, `PlayCombatSFX(action)`, `PlayUISFX(action)`,
  **`PlaySkillCast(pcSoundPath, volScale)`** — convert `\sound\skill\sound_k001.wav` →
  `StreamingAssets/sound/skill/sound_k001.wav`, category Combat, load async + cache.
- `BgmEnabled`/`SfxEnabled` toggles, `SetCategoryVolume(AudioCategory, v)` — category volume: BGM 0.6,
  SFX 0.8, Ambient 0.4, Combat 0.7, UI 0.5 (mặc định). Không có AudioMixer asset — volume thuần
  category dict. (Mixer groups chưa tồn tại; nếu cần ducking/durable bus → to-spec quyết, đừng thêm.)
- Missing clip: `WarnMissingClipOnce` → log 1 lần, không crash. Fail-closed OK.

### 4.2 JX audio staged hiện có (disk check)

| Path | Nội dung |
|---|---|
| `Assets/StreamingAssets/sound/skill/` | **28 file wav** (`sound_k001..004.wav` + tên GBK tiếng Trung như `亢龙无悔.wav`, `飘雪穿云.wav` = flight/impact sound từ missles.txt SndFile2/4) |
| `Assets/StreamingAssets/Reference/PcMusic/` | `musicset.txt` + `musicfightset.ini` (metadata registry, chưa có clip audio) |
| `Assets/StreamingAssets/Audio/BGM|SFX|Ambient` | def mặc định của AudioService trỏ `Audio/BGM/*.ogg`... — disk chỉ thấy `Audio/SFX/ui_click.wav`; **BGM/Ambient chưa staged** → PlayBGM sẽ warn missing. |
| `cache/server_offline/jxser/server1/pak/maps.pak` (+.mps) | runtime PAK local; `package.ini` trong cache chỉ 4 mục server — KHÔNG phải client full pak |
| jx-source loose wav | `find` toàn tree: KHÔNG có wav rời — audio chỉ nằm trong PAK (client `sound.pak`, package.ini idx 30) |

→ Inventory: skill cast/impact SFX = có sẵn 28 wav + `skill.manCastSndPath`/`fmCastSndPath`
(`SkillDefinition.cs:127-128`, col 7/8 Skills.txt) + `config.flightSoundPath`/`impactSoundPath` (SndFile2/4).
BGM = chỉ metadata; cần extract từ `sound.pak` (vltktool) hoặc tự author rồi stage `Audio/BGM/`.

### 4.3 Các service audio khác (có nhưng KHÔNG cần cho bridge)

- `MusicService` (Sandbox/MusicService.cs, 66 dòng): registry `PcMusicEntry` từ musicset.txt; metadata
  only — không phát.
- `SoundEffectService` (132 dòng): `TryLoadAudioClip(soundId)` qua Resources; `Reference/PcSound`
  **KHÔNG tồn tại trên disk** → `LoadFromStreamingAssets()` trả registry rỗng + warn. Bỏ qua.
- `SoundListService` (46 dòng): registry tham chiếu; không phát.
- `MapMusicService` (36 dòng): map → music id mapping (WeatherMusicIndexService); metadata.

### 4.4 Pipeline đề xuất cho Survivor (adapter)

1. Adapter/GameDirector tạo `AudioService`, `Initialize(root)`.
2. Wire `SkillEffectVisualService.OnCastSound = path => audio.PlaySkillCast(path)` — đúng y như
   SandboxManager.cs:1668.
3. SFX game loop (hit/xp/levelup) → `PlaySFX` với def tự đăng ký qua `AddDef`? **AddDef là private** —
   chỉ có 17 def mặc định. Survivor cần thêm def → 2 lựa chọn: (a) gọi `PlaySkillCast` cho path PC bất kỳ
   (không cần def — đường nhanh), (b) nhánh nhỏ dùng `LoadClipAsync` + source pool riêng trong adapter.
   Khuyến nghị (a) cho PC-named sound, (b) cho UI/levelup nếu cần id-based.
4. BGM: `PlayBGM("bgm_balang")` chạy được ngay nếu stage `Audio/BGM/balang.ogg` (hiện thiếu — to-spec
   chốt nguồn: extract sound.pak qua vltktool hay author mới).
5. Mixer: hiện không có AudioMixer; volume theo category. Nếu Survivor cần master/duck → tạo mixer asset
   + gán vào `AudioSource.outputAudioMixerGroup` (sửa cần đụng AudioService — KHÔNG; làm adapter-side:
   không thể vì pool private → để to-spec quyết: chấp nhận category volume hoặc mở rộng Sandbox (ngoài scope)).

---

## 5. VFX parity surface qua adapter

### 5.1 Parity surface (đã data-driven, không cần hardcode)

`PlaySkillCast(skill, caster, target, level)` tự:
1. PreCast: `preCastSprite` từ `skill.effectSourceId.sourcePath` (PreCastSpr col) + data-driven
   `pcPreCastSpriteKey` từ `PcSkillVisualAutoMapper` (missles1.txt) — duration `max(0.25, waitTime/16)` s.
2. Missile: `missileSprite` từ childSkillId → missles1.txt AnimFile (flightFrames/Directions/IntervalTicks,
   `missileSpeed` 324 px/s default, PC tick sim `PcMissileTickSeconds` khi follow-kind), fan/surround
   positions data-driven (`pcMissilePositions`...), `flightSoundPath` fired lúc bay.
3. Impact: `pcImpactSpriteKey` (explode SPR) + `impactDuration` + `impactSoundPath` (SndFile4) lúc nổ.
4. State aura: `isAura` + `stateAuraSprPath` (state attr value2 ticks/18 = duration; -1 = vô hạn).
5. Audio-only missile (không SPR): visual Finished nhưng `flightSoundPath`/`impactSoundPath` vẫn cháy.

Fail-closed đã nằm trong service (mục 1.5) — adapter chỉ cần:
- Bỏ qua effect `phase == Finished` sau khi gọi (trả null effect hoặc effect finished — check `ActiveEffectCount`).
- Không tự vẽ fallback ring trừ khi chủ động (ProxyActorVisual màu vẫn là baseline P1; `PlayHitFlash`/
  `PlayBuffAura` có sẵn nếu muốn placeholder impact).

### 5.2 Presenter

- `SkillEffectWorldOverlay` (Sandbox UI) KHÔNG dùng chung (camera/layer/golden-capture binding).
- Adapter dựng presenter riêng trong Survivor: mỗi `ActiveSkillEffect` → GameObject với SpriteRenderer
  (precast/missile/impact sprites) + optional LineRenderer trail; cập nhật từ `GetActiveEffects()` +
  `effect.Update` nội bộ (service tự advance phase). Số dòng ~100-150, port pattern từ
  `SkillEffectWorldOverlay.cs` nhưng bỏ golden layer + camera scale (dùng ortho size trực tiếp).
- Survivor camera: Main Camera ortho (Survivor.unity) — `WorldToScreenScale` pattern từ
  `SkillEffectRenderer.cs:31` có thể tham khảo nhưng không bắt buộc.

---

## 6. Quyết định để to-spec (chưa chốt ở research)

1. Nguồn hero spec: dùng `PlayerAppearanceMapper` variant PC hay self-author (Survivor hero riêng)?
2. BGM: extract `sound.pak` qua vltktool vs author mới — `Audio/BGM/*.ogg` hiện thiếu trên disk.
3. Mixer: chấp nhận category volume vs cần master bus (đụng Sandbox = ngoài scope).
4. Monster attack visual: PcNpcVisual không có attack clip — giữ hit-flash proxy hay bỏ qua (fail-closed).

## 7. Nguồn chính (cited)

- `Assets/Scripts/Sandbox/IPlayerVisual.cs`, `MalePlayerVisual.cs`, `FemalePlayerVisual.cs`, `PcNpcVisual.cs`, `NpcTemplateRegistry.cs`
- `Assets/Scripts/Sandbox/SkillEffectVisualService.cs`, `PcSkillVisualAutoMapper.cs`, `PcMissileFullVisualParser.cs`, `MissileSpawner.cs`
- `Assets/Scripts/Sandbox/AudioService.cs`, `MusicService.cs`, `SoundEffectService.cs`, `SoundListService.cs`, `MapMusicService.cs`, `SandboxManager.cs` (1644, 1666-1668, 258, 770)
- `Assets/Scripts/Sprites/SprRuntimeService.cs` (root/`FindSprDataInRoot`/`ComputePathUidHex`)
- `Assets/Scripts/PortData/PcText.cs`; `Assets/Scripts/Model/SkillDefinition.cs` (47, 56, 58, 119, 127-128, 143)
- `Assets/Scripts/UI/SkillEffectWorldOverlay.cs`, `SkillEffectRenderer.cs`
- `Assets/Scripts/Survivor/Actor/IActorVisual.cs`, `ProxyActorVisual.cs`; `VLTK.Survivor.Runtime.asmdef`
- `Assets/StreamingAssets/sound/skill/` (28 wav), `Reference/PcAttrib/missles1.txt` (514 dòng),
  `Reference/PcMusic/musicset.txt`, `Reference/PcNpcS.txt`
- `C:/Projects/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/package.ini` (31 paks, sound.pak idx 30)
- `C:/Projects/vltktool/` (resolve_uid.py, extract_item_spr.py, find_spr_by_image.py, scan_required_spr.py)
