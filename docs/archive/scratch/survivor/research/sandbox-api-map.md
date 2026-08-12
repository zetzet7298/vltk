# Bản đồ API Sandbox (read-only) — cho implement agent Survivor

> SCOUT recon. Không sửa file. Chỉ báo cáo API có sẵn dùng được.
> Mọi class nằm namespace `VLTK.Sandbox` trừ khi ghi rõ.
> Root SPR = project root `/SpritesRuntime` (67.499 file) — KHÔNG phải `Assets/StreamingAssets/Sprites` (1.160 file).

---

## 1. SprRuntimeService — root + resolve UID → sprite

File: `Assets/Scripts/Sprites/SprRuntimeService.cs`
Namespace: `VLTK.Sprites`

- Root thực tế: `public const string DefaultSpritesRoot = "SpritesRuntime"`. Constructor `SprRuntimeService(string streamingAssetsRoot = null)` — khi null resolve về `Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultSpritesRoot))` = `<project>/SpritesRuntime`.
- `EnumerateSpriteRoots()` (private) cũng quét thêm các root phụ: `{root}/SkillIcons`, `<projectRoot>/Generated/MapSprites`, `Generated/NpcSprites`, `Generated/ObjectSprites` — **đều dưới project root**, không phải trong `Assets/`.

### ComputePathUidHex — GB2312 signed + unsigned
```csharp
public static uint   ComputePathUid(string path, string encodingName = "GB2312", bool signedBytes = true);
public static string ComputePathUidHex(string path, string encodingName = "GB2312", bool signedBytes = true);
public static string NormalizeResourcePath(string path);  // "a\b" → "\a\b"
```
- Thuật toán: normalize path (backslash, thêm `\` đầu), mã hoá bytes theo GB2312, mỗi byte: `int signed = bytes[i] >= 128 ? bytes[i]-256 : bytes[i]`; `c = signedBytes ? signed : bytes[i]`; A–Z → +32; `value = ((value + index*c) % 0x8000000B) * 0xFFFFFFEF`; kết cuối `^ 0x12345678`.
- `signedBytes:true` = PC-accurate (đúng cho path CJK như `\spr\Ui\技能图标\...`, PC UID `c4454165`); `signedBytes:false` = unsigned (miss path CJK).
- Caller nên thử signed trước, unsigned sau.

### FindSprDataInRoot — signature + thứ tự resolve
```csharp
private static byte[] FindSprDataInRoot(string root, string sanitizedKey, string originalName);
```
Thứ tự tìm `.spr` trong root (signed-hash ưu tiên trước unsigned):
1. `{sanitizedKey}.spr` (key = tên đã sanitize)
2. `{nameKey}.spr` (filename của originalName)
3. `{uidFromPath}.spr` (nếu originalName là hex-8 ký tự)
4. `{ComputePathUidHex(originalName, signed:true)}.spr`
5. `{ComputePathUidHex(originalName, signed:false)}.spr`

### API public chính (resolve + fail-closed)
```csharp
public Sprite   ResolveSprite(string spriteName, int fallbackWidth = 32, int fallbackHeight = 32);
public Texture2D ResolveTexture(string spriteName, int frameIndex = 0);   // raw texture, tự chọn pivot
public Sprite   ResolveSpriteOrDefault(string spriteName, int width = 32, int height = 32); // fallback procedural màu
public int      PreloadAll();          // đọc mọi *.spr trong root, warm cache
public SprDiagnostic GetDiagnostic(string spriteName);
public void     ClearCache();
public int      CacheCount; public int MissCount; public int DiagnosticCount;
```
- Fail-closed: không tìm thấy / decode lỗi → **trả `null`** (không vẽ sprite rác), log warn `SprRuntime` + ghi vào `_missCache`. `ResolveSpriteOrDefault` là ngoại lệ (trả procedural màu).
- Pivot từ header SPR: `pivot = (centerX/width, centerY/height)`; pixelsPerUnit=100 trong ResolveSprite; `ResolveTexture` để caller tự build sprite.

---

## 2. Parser PcSkills.txt / PcAllFactionLearnedDisplaySkills.txt / missles1.txt

### PcConfigParser — parser RUNTIME chính cho PcSkills.txt
File: `Assets/Scripts/Core/PcConfigParser.cs` — **Namespace: `VLTK.Core`**
```csharp
public static PcConfigManifest LoadAll(string streamingAssetsPath);            // PcSkills.txt + PcNpcS.txt + PcMissles.txt
public static List<SkillDefinition> ParseSkills(string path);
public static List<SkillDefinition> ParseSkillsLines(IReadOnlyList<string> lines);
public static int MergeMissilesWithoutOverwriting(List<PcMissileEntry> target, IEnumerable<PcMissileEntry> fallback);
public static List<PcMissileEntry> ParseMissiles(string path);                 // PcMissles.txt
```
- File staged `Assets/StreamingAssets/Reference/PcSkills.txt` (113 cột) đã là **UTF-8 tiếng Việt** ("Công kích vật lý"). `ParseSkills` đọc `File.ReadAllLines` (UTF-8 mặc định). **KHÔNG GBK** ở bản mobile staged — lưu ý này khác AGENTS.md (GBK chỉ đúng cho PC-source skills.txt).
- **Col map KHÔNG hardcode theo index** — dùng `HeaderCol(BuildHeaderIndex(header), "TênCột")` (theo tên, không lệch). Gồm: WaitTime, ClientSend, SkillCostType, CostValue, TimePerCast, TimePerCastOnHorse, IsPhysical, Target*, ByMissle, IsUseAR, ReqLevel, MaxLevel, EqtLimit, HorseLimit, DoHurt, WeaponSkill, **LvlSetScript**, LvlSetting1, LevelUpScript.
- Map cố định (0-indexed, từ header thật đã verify): col0 name / 2 skillId / 3 Attrib / 4 SkillStyle / 5 SkillIcon / 6 PreCastSpr / 7 ManCastSnd / 8 FMCastSnd / 9 stateSpecialId / 11 IsAura / 14 AttackRadius / 16 missilesGenerate / 18 CharClass / 19 missileForm / 20 childSkillId / 21 childSkillLevel / 22 childSkillNum / 24 charAnimId / 26 isMelee / 27 WaitTime.
- **Faction**: `CombatFactionExt.FactionFromLuaScript(lvlSetScript)` từ LvlSetScript (LvlSetScriptCol đúng ở đây do theo tên).
- Header thật (verify từ file): `Series@68, ShowAddition@69, LvlSetScript@70, LevelUpScript@111`.

### PcSkillFullParser — parser audit (hardcoded col, CHỨA BUG LvlSetScriptCol)
File: `Assets/Scripts/Combat/PcSkillFullParser.cs` (namespace `VLTK.Sandbox`)
```csharp
public static List<PcSkillEntry> ParseFile(string path);
public static PcSkillEntry ParseRow(string[] cols, int idHint = 0);
```
- Dùng `PcText.ReadLinesTcvn3(path)` (skills.txt PC source).
- **BUG (đã verify so header thật)**: `public const int LvlSetScriptCol = 71;` — **đúng phải là 70**. Tương tự `ReqLevelCol = 53` (đúng 52), `MaxLevelCol = 54` (đúng 53), `LevelUpScriptCol = 112` (đúng 111). Hardcode lệch +1 cả dãy. → Đây là parser audit/catalog, **KHÔNG phải runtime**; runtime dùng PcConfigParser (theo tên, đúng).

### PcSkills1FullParser — parser audit skills1_full.txt
File: `Assets/Scripts/Combat/PcSkills1FullParser.cs`
```csharp
public static PcSkills1FullCatalog ParseFile(string absolutePath);
public static PcSkills1FullCatalog ParseLines(IReadOnlyList<string> lines);
public static PcSkills1FullRow ParseRow(string[] cols, int sourceRowNumber);
```
- File `Reference/PcSkill/skills1_full.txt`, `PcText.ReadLinesTcvn3`. Hằng: SkillNameCol=0, SkillIdCol=2, SkillIconCol=5, MaxLevelCol=54, **LvlSetScriptCol=71 (CŨNG BUG, đúng 70)**, ExpectedColumnCount=115.

### PcAllFactionLearnedDisplaySkills.txt
- File: `Assets/StreamingAssets/Reference/PcAllFactionLearnedDisplaySkills.txt` (+ `.provenance.json`).
- Đọc ở `PcCombatCatalogFactory.cs` (dòng 243-262): `Resources.Load<TextAsset>("Reference/PcAllFactionLearnedDisplaySkills")` → fallback đọc file → nạp qua `PcConfigParser.ParseSkillsLines(lines)`. TCVN3/UTF-8 tiếng Việt.
- Dùng để hợp skill đã học-hiển-thị theo phái khi skill chưa được faction đăng ký.

### missles1.txt / missles.txt — PcMissleParser + PcMissileRegistry
File: `Assets/Scripts/Combat/PcMissleParser.cs` (namespace `VLTK.Sandbox`)
```csharp
public static List<PcMissleEntry> ParseFile(string path);
public static PcMissleRegistry BuildRegistry(string dir);
```
- `PcMissleEntry`: missleId, nameRaw, moveKind, followKind, colFollowTarget, missleHeight, collidRange, isRangeDmg.
- Col (0-indexed): MissleIdCol=0, MissleNameCol=1, MoveKindCol=2, FollowKindCol=3, ColFollowTargetCol=4, MissleHeightCol=5, CollidRangeCol=6, IsRangeDmgCol=7.
- File staged `Reference/PcAttrib/missles1.txt` = **57 cột, TCVN3** (verify thực tế). `PcMissileRegistry.Initialize(streamingAssetsPath)` ưu tiên `Reference/PcAttrib/missles1.txt` (TCVN3 qua `PcText.ReadLinesTcvn3`), fallback `Reference/PcMissles.txt`, gộp thêm `Reference/ModMissles.txt`.

### PcModMissileParser — parser mở rộng (id ≥ 300)
File: `Assets/Scripts/Sandbox/PcModMissileParser.cs`
```csharp
public static List<PcModMissileRow> ParseFile(string absolutePath, int minMissileId = 0);
public static List<PcModMissileRow> ParseLines(IEnumerable<string> lines, int minMissileId = 0);
public static List<PcMissileEntry> ToMissileEntries(List<PcModMissileRow> rows);
```
- Col (0-indexed): 0 id / 1 name / 6 minRadius / 8 maxRadius / 10 lifetime / 11 speed / 14 count / 18 flyEventId / 20 collideEventId / 21 vanishEventId / 29-38 sprFile (chọn cột != rỗng đầu tiên trong 29,32,35,38).
- `public static class PcMissileRegistry` (static) — `Initialize(string streamingAssetsPath)`, `TryGet(int id, out PcMissileEntry)`, `Count`.

### PcText — reader dùng chung (encoding)
File: `Assets/Scripts/PortData/PcText.cs` (namespace `VLTK.Sandbox`, `internal static`)
```csharp
public static string[] ReadLines(string absolutePath, Encoding encoding);       // decode theo encoding chỉ định
public static string[] ReadLinesTcvn3(string absolutePath);                     // western ANSI + TCVN3→Unicode (chắc chắn cho objdata/npc/missles1)
public static byte[][] Tcvn3ToBytesMultiple(string text);                       // sinh các byte-candidate TCVN3
```
- `DecodeBest` (private): scoring giữa UTF-8 / GB18030 / GB2312 / windows-1252±TCVN3 / iso-8859-1±TCVN3. Việt +4, CJK +8 → CJK thắng file Trung, TCVN3 thắng file Việt.
- **Quy tắc dùng**: file tên tiếng Việt (PcAllFaction..., missles1, npcs, objdata) → `ReadLinesTcvn3`. File Trung (PcSkills.txt PC-source GBK) → `ReadLines` với GB2312/GB18030.

---

## 3. SkillEffectVisualService — precast + missile SPR render

File: `Assets/Scripts/Sandbox/SkillEffectVisualService.cs`
```csharp
public class SkillEffectVisualService
{
    public Action<string> OnCastSound;                       // SandboxManager wire → AudioService.PlaySkillCast
    public Action<ActiveSkillEffect, int, Vector2> OnMissileCollided;
    public SkillEffectVisualService(SprRuntimeService sprService);
    public SkillEffectVisualService(SprRuntimeService sprService, SkillCatalog catalog);

    public ActiveSkillEffect PlaySkillCast(SkillDefinition skill, Vector2 casterPos, Vector2 targetPos, int skillLevel);
    public ActiveSkillEffect PlaySkillCast(SkillDefinition skill, Vector2 casterPos, Vector2 targetPos, int skillLevel,
        Func<Vector2> getCurrentTargetPos, Action<ActiveSkillEffect, int, Vector2> onMissileCollided = null,
        bool suppressCastAudio = false);
    public ActiveSkillEffect SpawnAuthoritativeMissile(string missileInstanceId, SkillDefinition skill, Vector2 casterPos, Vector2 targetPos, int skillLevel);
    public bool UpdateAuthoritativeMissile(string missileInstanceId, Vector2 worldPosition, bool playFlightSound);
    public bool CollideAuthoritativeMissile(string missileInstanceId, Vector2 worldPosition, bool playConfiguredImpactSound = true);
    public bool VanishAuthoritativeMissile(string missileInstanceId);
    public ActiveSkillEffect PlayHitFlash(Vector2 targetPos, Color color, float durationSeconds = 0.35f);
    public ActiveSkillEffect PlayBuffAura(Vector2 centerPos, Color color, float durationSeconds = 1.2f, float radius = 48f, string label = "BuffAura");
    public int ClearActiveEffects();
    public int RemoveStateAurasForActor(int actorId);
    public int SynchronizeStateAuras(CombatActorState actor, Vector2 position, Func<Vector2> getCurrentActorPos = null);
    public List<ActiveSkillEffect> GetActiveEffects();
    public void Update(float dt);
    public int ActiveEffectCount;
}
```
- **Data-driven**: `ConfigureDataDrivenVisuals` → `PcSkillVisualAutoMapper.GetVisualConfig(skill)` từ missles1.txt (childSkillId → missile → SPR path + anim + light color). Không hardcode per-faction.
- PreCast: `fx.preCastDuration = max(0.25, skill.waitTime / 16f)` (PC WaitTime col 27 /16). Missile speed mặc định 324 (PC missile 48: 18 units/tick × 18). Pha: PreCast → Missile → Impact → Finished.
- `effect.castSoundPath = skill.manCastSndPath` (col 7) → `OnCastSound?.Invoke(...)` ở frame cast.
- **Fail-closed**: nếu không `isAura` && không có PC preCast/missile/impact sprite → set mọi duration = 0, `phase = Finished` (không fake timeline). (ponytail comment)
- `PcMissileTickSeconds = 1f/18f`; `PcFollowRetargetCounterMax = 8`; homing MoveKind=5 dùng follow-tick simulation.

### SkillCatalog / SkillDefinition (data object)
- `SkillCatalog.Resolve(int skillId)` (dòng dùng trong service). Fields liên quan: skillId, DisplayName, waitTime, timePerCast, missileForm (enum `SkillMissileForm`), childSkillId, childSkillNum, isMelee, attackRadius, manCastSndPath, fmCastSndPath, effectSourceId (SourceAssetId.sourcePath), stateSpecialId, GetPcLevelData(level).

---

## 4. AudioService / SoundEffectService — PlaySkillCast + skill SFX

### AudioService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/AudioService.cs`
```csharp
public class AudioService
{
    public void PlaySkillCast(string pcSoundPath, float volumeScale = 1f);   // e.g. "\sound\skill\sound_k001.wav"
    public Task PlaySkillCastAsync(string pcSoundPath, float volumeScale = 1f);
    public void PlaySFX(string id);
    public void PlayUISFX(string action);
    public void PlayBGM(string id);  // (có trong class)
    public float GetCategoryVolume(AudioCategory category);
    public void SetCategoryVolume(AudioCategory category, float volume);
}
```
- `PlaySkillCast` convert `\sound\skill\sound_k001.wav` → `sound/skill/sound_k001.wav` relative StreamingAssets → `LoadClipAsync`. Volume = `GetCategoryVolume(AudioCategory.Combat) * volumeScale`, `PlayOneShot`. Bỏ qua nếu `!SfxEnabled`.
- **Skill SFX staged**: `Assets/StreamingAssets/sound/skill/` — **28 file `sound_k*.wav`** (sound_k001 → sound_k028, đã verify). `AudioCategory` enum: BGM/SFX/Ambient/Combat/UI.
- Wiring: `SandboxManager.cs:1668` `SkillEffectVisual.OnCastSound = pcPath => AudioService?.PlaySkillCast(pcPath)`.

### SoundEffectService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/SoundEffectService.cs` (registry `soundeffect.txt`)
```csharp
public static SoundEffectService LoadFromStreamingAssets();                 // DefaultStreamingDir = "Reference/PcSound"
public PcSoundEffectEntry GetSound(int soundId);
public IReadOnlyList<PcSoundEffectEntry> GetByCategory(int category);
public string GetSoundPath(int soundId);
public AudioClip TryLoadAudioClip(int soundId);
public int Play(int soundId, Vector3 pos);
public bool Stop(int handle);
public string GetCategoryName(int category);
```
- Lưu ý: `PlaySkillCast` KHÔNG nằm ở SoundEffectService — nó ở AudioService. SoundEffectService là catalog + Play theo soundId.

---

## 5. PcNpcVisual + NpcTemplateRegistry — visual NPC từ res/template

### PcNpcVisual (namespace `VLTK.Sandbox`) — MonoBehaviour render NPC SPR
File: `Assets/Scripts/Sandbox/PcNpcVisual.cs`
```csharp
public sealed class PcNpcVisual : MonoBehaviour
{
    public string standSourcePath;  public string walkSourcePath;
    public int direction; public float frameRate = 8f; public float pixelsPerUnit = 1f;
    public Vector2 referencePixel = new Vector2(160f, 192f);
    public bool moving; public bool renderShadow = true;
    public bool HasWalkClip; public bool HasAnyClip; public bool HasShadow;
    public int FramesPerDirection; public int DirectionCount;
    public void Configure(string standPath, string walkPath, Vector2? refPixel = null);  // load SPR stand+walk
    public void SetMoveInput(Vector2 move);
    public void Tick(float deltaTime);
}
```
- Sprite pivot `(0f,1f)` (top-left), ppu configurable. Shadow mặc định `spr\npcres\man\MA_YY_999_ST01.spr` (stand) / `MA_YY_999_RN01.spr` (walk). Decode qua `SprDecoder`.

### NpcTemplateRegistry (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/NpcTemplateRegistry.cs`
```csharp
public class NpcTemplateRegistry
{
    public NpcTemplateRegistry(IAssetRegistry assets = null);
    public void Register(NpcTemplate template);
    public NpcTemplate Resolve(int templateId);
    public bool Contains(int templateId);
    public List<NpcResourceIssue> ValidateResources();  // resolve sprite + script qua IAssetRegistry
    public int Count; public IReadOnlyCollection<NpcTemplate> All;
}
```
- `NpcTemplate` (file `Assets/Scripts/Model/NpcTemplate.cs`): templateId, nameRaw, nameNormalized, level, maxLife, attack, defense, kind, series, walkSpeed, runSpeed, visionRadius, activeRadius, aiMode, aiParams, `SourceAssetId spriteSourceId` (body .spr), spriteClipRef, scriptRef, levelScriptRef, spriteResolved, scriptResolved.

### Adapter pattern tham khảo — JxPlayerVisual (P1.5 bridge)
File: `Assets/Scripts/Survivor/Actor/JxPlayerVisual.cs` (namespace `VLTK.Survivor`)
```csharp
public sealed class JxPlayerVisual : MonoBehaviour, IActorVisual
{
    // ProbeSentinel() trước → nếu SPR stand staged → AddComponent<MalePlayerVisual>() + wrap MaleBridge(mpv)
    // ngược lại → AddProxy() (ProxyActorVisual).
}
```
- Interface `IActorVisual` (file `Assets/Scripts/Survivor/Actor/IActorVisual.cs`): `void SyncPosition(Vector3)`, `void SetDirection(int dirIndex8)`, `void PlayMove(bool)`, `void SetAlive(bool)`.
- `MalePlayerVisual` ở `Assets/Scripts/Sandbox/MalePlayerVisual.cs` (PcNpcVisual-derivative, `RefreshActionParts(force:true)`). Dùng cho NPC: wrap `PcNpcVisual` tương tự (Configure + Tick trong `IActorVisual`).
- `ProxyActorVisual` / `ProxyVisuals` = placeholder màu P1.

---

## 6. PcSkillVisualAutoMapper / PcMissileFullVisualParser — API read-only

### PcSkillVisualAutoMapper (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/PcSkillVisualAutoMapper.cs`
```csharp
public sealed class PcSkillVisualAutoMapper
{
    public void Initialize(string streamingAssetsPath);   // parse missles1.txt: Reference/PcAttrib/missles1.txt (fallback PcMissles.txt)
    public PcSkillVisualConfig GetVisualConfig(SkillDefinition skill);   // childSkillId → missile → config (cached theo skillId)
    public void PreCacheAll(SkillCatalog catalog);        // batch warm cache
    public static string SprPathToKey(string pcPath);     // trả nguyên path (identity)
    public static PcStateAuraData GetStateAuraData(int stateId);  // state 6-49 từ 状态与光效图形对照表.txt, 1-5 built-in no SPR
    public int SkillsProcessed; public int VisualsFound; public int VisualsMissing; public int CacheCount; public int MissileVisualCount;
}
```
- `PcSkillVisualConfig` fields: skillId, missileId, preCastSprPath/hasPreCast, flightSprPath/flightFrames/flightDirections/flightIntervalTicks/missileSpeed/missileLifetime, isStationary/moveKind, explodeSprPath/explodeFrames/..., lightColor/lightRadius, isMelee/hasMissile/isRangeDmg/dmgRange, stateAuraSprPath/..., flightSoundPath/impactSoundPath. Helpers: `FlightDurationSeconds`, `SpeedWorldPerSec = missileSpeed*18`, `HasFlightVisual`, `HasExplodeVisual`, `HasAnyVisual`.
- `PcStateAuraData` (struct): sprPath, totalFrames, frameStart, frameEnd, intervalTicks, directions, position (1=head/2=feet/3=body).

### PcMissileFullVisualParser (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/PcMissileFullVisualParser.cs`
```csharp
public static PcMissileFullVisualRegistry ParseFromFile(string path);   // đọc missles1.txt → registry
public class PcMissileFullVisual {
    int missileId; int moveKind; int followKind; int missleHeight; int speed; int lifetime;
    int zspeed; int zacc; int collidRange; int isRangeDmg; int dmgRange; int dmgInterval;
    int loopPlay; int subLoop; int subStart; int subStop; int responseSkill; int canDestroy;
    int colVanish; int canSlow; int canColFriend; int autoExplode;
    MissileAnimSlot[] flightAnims[4]; MissileAnimSlot[] explodeAnims[4];
    int redLum; int greenLum; int blueLum; int lightRadius;
    string PrimaryFlightSpr; string PrimaryExplodeSpr; bool IsStationary; Color LightColor;
}
```
- `MissileAnimSlot`: slot SPR cho status 1=flight, 3=collide/explode. AutoMapper dùng `PrimaryFlightSpr`/`PrimaryExplodeSpr`.

---

## 7. Các service khác implement agent cần

### TextResourceService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/TextResourceService.cs`. `DefaultStreamingDir = "Reference/PcText"`.
```csharp
static TextResourceService LoadFromStreamingAssets();
string GetVietnamese(string key); string GetChinese(string key);
string GetOrVietnamese(string key, string fallback);
IEnumerable<string> GetAllKeys(); IReadOnlyList<PcTextResourceEntry> All;
```
Tra cứu text theo key (bảng song ngữ Việt/Trung).

### PcSaveSlotService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/PcSaveSlotService.cs`. `MaxSlots = 5`.
```csharp
bool SaveGame(int slotId, PlayerSnapshot snapshot);
PlayerSnapshot LoadGame(int slotId); bool DeleteSave(int slotId);
IReadOnlyList<SaveSlotData> GetAllSlots();  // + auto-save (HasAutoSave, SaveAuto/...)
```
`PlayerSnapshot`: slotId, playerName, playerLevel, mapId, playTimeSec, saveTimeUnix, faction, gold, learnedSkillIds, inventoryItemIds, serializedState.

### BaseClientData — **KHÔNG TÌM THẤY**
- Không tồn tại class `BaseClientData` trong toàn bộ `Assets/Scripts` (đã grep toàn repo, 0 kết quả). Nếu AGENTS/đặc tả trỏ tới nó → implement agent phải tạo mới hoặc dùng nơi khác (xem `RuntimeState`, `PcConfigManifest` trong `VLTK.Core`).

### MusicService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/MusicService.cs`. `DefaultStreamingDir = "Reference/PcMusic"`.
```csharp
event Action OnMusicLoaded; void AttachRegistry(PcMusicRegistry reg);
PcMusicEntry GetTrack(int trackId); IReadOnlyList<PcMusicEntry> GetByScene(int sceneType);
IEnumerable<PcMusicEntry> GetAllTracks(); static MusicService LoadFromStreamingAssets(string subdir = null);
```
BGM registry (PC music/*).

### SoundListService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/SoundListService.cs`. `DefaultStreamingDir = "Reference/PcSound"` (khác SoundEffect).
```csharp
static SoundListService LoadFromStreamingAssets();
PcSoundListEntry GetSound(int id); IReadOnlyList<PcSoundListEntry> GetByCategory(int category);
IReadOnlyList<PcSoundListEntry> All;
```

### MapMusicService (namespace `VLTK.Sandbox`)
File: `Assets/Scripts/Sandbox/MapMusicService.cs`. `LoadFromStreamingAssets(string subDir = "Reference/PcMap")`.
```csharp
PcMapMusicEntry Get(int mapId); GetMusicForMap(int mapId); GetAll();
int GetDayMusic(int mapId); int GetNightMusic(int mapId);
int GetBattleMusic(int mapId); int GetDefaultMusic(int mapId);
```
Map day/night/battle music lookup.

### MissleCatalogService (namespace `VLTK.Sandbox`) — helper
File: `Assets/Scripts/Sandbox/MissleCatalogService.cs`.
```csharp
static MissleCatalogService LoadFromStreamingAssets();
PcMissleEntry GetMissle(int missleId); IEnumerable<PcMissleEntry> GetAllMissles();
IReadOnlyList<PcMissleEntry> GetByMoveKind(int moveKind); GetByFollowKind(int followKind);
```

---

## Lưu ý quan trọng cho implement agent

1. **LvlSetScript**: runtime `PcConfigParser` đúng (theo tên HeaderCol). BUG hardcode chỉ ở `PcSkillFullParser.LvlSetScriptCol=71` và `PcSkills1FullParser.LvlSetScriptCol=71` — **cả hai đúng phải = 70** (verify từ file 113 cột: Series@68, ShowAddition@69, LvlSetScript@70). Kèm lệch +1 cả ReqLevel/MaxLevel/LevelUpScript trong PcSkillFullParser.
2. **Encoding**: bản staged `PcSkills.txt` là **UTF-8 Việt** (File.ReadAllLines). `missles1.txt` / `PcAllFactionLearnedDisplaySkills.txt` / `PcSkill/skills1_full.txt` = **TCVN3** → `PcText.ReadLinesTcvn3`. Đừng decode PcSkills.txt bằng GBK.
3. **Fail-closed**: SprRuntimeService trả null khi thiếu SPR; SkillEffectVisualService `Finished` ngay khi không có PC art — đây là hành vi chuẩn, không phải bug.
4. **Skill SFX**: 28 wav `sound_k*.wav` tại `Assets/StreamingAssets/sound/skill/`; path PC `\sound\skill\sound_kXXX.wav` → `AudioService.PlaySkillCast`.

## File đã ghi
`.scratch/survivor/research/sandbox-api-map.md`
