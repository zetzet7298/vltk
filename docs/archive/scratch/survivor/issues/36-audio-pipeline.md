# 36 — Audio (mixer + SFX + own BGM)

**What to build:** `SurvivorAudioMgr` + AudioMixer (master/bgm/sfx): SFX trigger đúng event (hit/cast/pickup/levelup/die; skill cast qua `AudioService.PlaySkillCast` fail-closed, skill SFX staged 28 wav), BGM 3 track menu/battle/boss (own pipeline — ogg JX chưa staged), volume API cho settings.

**Blocked by:** None — can start immediately.

**Status:** verified

- [x] SFX trigger đúng event: hit/cast/pickup/levelup/die + skill cast (staged)
- [x] BGM 3 track chuyển đúng ngữ cảnh: menu → battle → boss
- [x] Mixer 3 bus (master/bgm/sfx) volume riêng, API cho settings ticket 40
- [x] Chưa staged audio → im lặng, không crash
- [ ] PlayMode manual: nghe rõ SFX/BGM + đổi volume mượt

**Verification (orchestrator):** EditMode 84/84 PASSED. Mixer generated: Assets/Survivor/Audio/Survivor.mixer (Master→BGM/SFX + snapshots + exposed params). Generator rewritten: AudioMixerController internal API Unity 6 — CreateMixerControllerAtPath (static), CreateNewGroup khong tu noi children → set thang, AudioGroupParameterPath(group, GUID) (AudioParameterPath abstract), set startSnapshot/currentSnapshot.
