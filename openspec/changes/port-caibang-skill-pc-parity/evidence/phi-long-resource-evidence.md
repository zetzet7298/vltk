# Phi Long Tại Thiên Resource Evidence

## Skill row: `SkillId=357`
Source: `Assets/StreamingAssets/Reference/PcSkill/skills.txt` decoded with CP1258.

| Field | Value |
| --- | --- |
| SkillName | `Phi Long Tại Thiên` (source mojibake: `Phi Long T¹i Thiªn`) |
| SkillStyle | `0` |
| SkillIcon | `\spr\Ui\skill\ÁúƠ½ÔÚ̉°.spr` |
| PreCastSpr | `\spr\skill\̀́È̀\mag_bz_huo3_±¬Ơ¨Đ§¹û.spr` |
| ManCastSnd | `\sound\skill\sound_k005.wav` |
| FMCastSnd | `\sound\skill\sound_k010.wav` |
| MisslesForm | `0` |
| ChildSkillId | `166` |
| ChildSkillNum | `3` in `skills.txt`; runtime Lua overrides count via `skill_misslenum_v`, currently L20=4 in `PcCaiBangLuaLevelService` tests. |
| AttackRadius | `400` in row; runtime Lua/catalog may override. |
| Param1 | `32` |
| WaitTime | `5` |
| HorseLimit | `1` |
| CharAnimId | `11` |

## Skill-level hashed resources
Computed with the JX Pack Hash UID algorithm over normalized raw resource path bytes from the PC reference file.

| Resource | Hash | Found |
| --- | ---: | --- |
| SkillIcon `\spr\Ui\skill\ÁúƠ½ÔÚ̉°.spr` | `d97b70ca.spr` | `/var/www/jx-pc/pak_unpacked/update01/unknown/d97b70ca.spr`, `/var/www/jx-pc/pak_unpacked/spr/unknown/d97b70ca.spr` |
| PreCastSpr `\spr\skill\̀́È̀\mag_bz_huo3_±¬Ơ¨Đ§¹û.spr` | `b91ab706.spr` | `/var/www/jx-pc/pak_unpacked/skills/unknown/b91ab706.spr` |
| ManCastSnd `\sound\skill\sound_k005.wav` | `805e4929.wav` | hash computed; existence not found in quick bounded lookup |
| FMCastSnd `\sound\skill\sound_k010.wav` | `a9f82272.wav` | hash computed; existence not found in quick bounded lookup |

## Missile row: `MissleId=166` (Phi Long Tại Thiên)
Source: `Assets/StreamingAssets/Reference/PcAttrib/missles.txt` decoded with CP1258.

| Field | Value |
| --- | --- |
| MissleName | `Phi Long Tại Thiên` (source mojibake: `Phi Long T¹i Thiªn`) |
| MoveKind | `5` |
| FollowKind | `0` |
| ColFollowTarget | `1` |
| CollidRange | `1` |
| IsRangeDmg | `0` |
| DmgRange | `1` |
| LifeTime | `24` |
| Speed | `30` |
| AnimFile2 | `\spr\skill\Ø¤°ï\mag_gb_05_¿ºÁúÓĐ»Ú.spr` |
| SndFile2 | `\sound\skill\¿ºÁúÎ̃»Ú.wav` |
| AnimFile4 | `\spr\skill\Ø¤°ï\mag_gb_bz5_±¬Ơ¨Đ§¹û.spr` |
| SndFile4 | `\sound\skill\sound_k037.wav` |

## Missile hashed resources

| Resource | Hash | Found |
| --- | ---: | --- |
| AnimFile2 dragon `\spr\skill\Ø¤°ï\mag_gb_05_¿ºÁúÓĐ»Ú.spr` | `a31b9f04.spr` | `/var/www/jx-pc/pak_unpacked/skills/unknown/a31b9f04.spr` |
| SndFile2 `\sound\skill\¿ºÁúÎ̃»Ú.wav` | `3b6bb009.wav` | `/var/www/jx-pc/pak_unpacked/sound/unknown/3b6bb009.wav` |
| AnimFile4 impact `\spr\skill\Ø¤°ï\mag_gb_bz5_±¬Ơ¨Đ§¹û.spr` | `c33e96c2.spr` | `/var/www/jx-pc/pak_unpacked/skills/unknown/c33e96c2.spr` |
| SndFile4 `\sound\skill\sound_k037.wav` | `ff4dadbd.wav` | hash computed; existence not found in quick bounded lookup |

## Behavior implication
`MoveKind=5` is direct PC evidence that the Phi Long missile is target-tracking/homing. Mobile runtime should therefore preserve `getCurrentTargetPos` live target resolution for `SkillId=357` and keep per-missile lane offsets when Lua count reaches four at level 20.

## Caveat
Some displayed paths are mojibake because the reference files are CP1258/legacy encoded. Hashes above were computed from raw field bytes, not from guessed Unicode translations.
