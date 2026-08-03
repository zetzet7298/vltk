# 08 — Research: visual + VFX + audio bridge (IActorVisual, không sửa Sandbox)

Type: `research`
Status: ``resolved``
Blocked by: 01

## Question

Bridge JX visual/VFX/audio vào Survivor qua `IActorVisual` adapter, KHÔNG sửa Sandbox. Cần:

1. Sandbox visual services public surface: `MalePlayerVisual`/`FemalePlayerVisual`,
   `PcNpcVisual` + `NpcTemplateRegistry`, `SkillEffectVisualService`, `MissileSpawner`,
   `PcText` (Tcvn3Table/GBK) — API nào adapter có thể gọi read-only.
2. Adapter design cho `IActorVisual` (proxy P1 màu → JX bridge P1.5): spawn/replace/play-anim/
   play-skill-visual/play-missile; contract boundary giữ Sandbox nguyên vẹn.
3. SPR staging path (`Assets/StreamingAssets/Sprites/{hash}.spr`) + fail-closed check hiện có
   trong Sandbox.
4. Audio: có `AudioMgr`/SFX/BGM nào trong Sandbox không? JX asset âm thanh khả dụng gì? Pipeline
   play SFX/BGM + mixer.
5. VFX: `SkillEffectVisualService` parity surface (precast SPR + missile SPR) — gọi qua adapter.

## Output

Ghi `research/visual-vfx-audio-bridge.md`: API table, adapter contract, SPR fail-closed note,
audio inventory + pipeline, VFX parity surface. Đọc `Assets/Scripts/Sandbox/` (grep visual/
service/audio), `package.ini`, jx-source audio dir.

## Answer

Sandbox ĐÃ có surface đủ — adapter gọi read-only, KHÔNG sửa Sandbox:
- **Visual**: `IPlayerVisual`/`MalePlayerVisual`/`FemalePlayerVisual` (player) + `PcNpcVisual`+`NpcTemplateRegistry` (monster); adapter qua `IActorVisual` (P1 proxy màu → P1.5 JX bridge).
- **VFX**: `SkillEffectVisualService` (data-driven từ `missles1.txt`, **fail-closed sẵn**) + `PcSkillVisualAutoMapper`/`PcMissileFullVisualParser`; `MissileSpawner` gắn host Sandbox → adapter không dùng.
- **SPR root** thực tế = project `/SpritesRuntime` (67,499 file), KHÔNG phải `StreamingAssets/Sprites` như issue/AGENTS ghi → cần update note (SprRuntimeService.FindSprDataInRoot/ComputePathUidHex).
- **Audio**: `AudioService.PlaySkillCast(pcPath)` + `MusicService`/`SoundEffectService`/`SoundListService`/`MapMusicService`; SFX skill staged (`StreamingAssets/sound/skill/` 28 wav); **BGM ogg chưa staged, mixer chưa có** → own pipeline.
Full: research/visual-vfx-audio-bridge.md
