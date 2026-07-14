# Kháng Long Hữu Hối Resource and Spread Evidence

## Skill row: `SkillId=128`
Source: `Assets/StreamingAssets/Reference/PcSkill/skills.txt` decoded with CP1258.

| Field | Value |
| --- | --- |
| SkillName | `Kháng Long Hữu Hối` (source mojibake: `Kh¸ng Long H÷u Hèi`) |
| SkillStyle | `0` |
| SkillIcon | `\spr\Ui\¼¼ÄÜÍ¼±ê\icon_sk_gb_41.spr` |
| PreCastSpr | `\spr\skill\̀́È̀\mag_bz_huo3_±¬Ơ¨Đ§¹û.spr` |
| ManCastSnd | `\sound\skill\sound_k005.wav` |
| FMCastSnd | `\sound\skill\sound_k010.wav` |
| MisslesForm | `2` |
| ChildSkillId | `48` |
| ChildSkillNum | `8` in `skills.txt`; runtime Lua overrides count (`L20=15`). |
| AttackRadius | `400` |
| Param1 | `3` in skill row; runtime Lua `skill_param1_v` gives `L20=2`. |
| Param2 | `1` |
| WaitTime | `5` |
| HorseLimit | `1` |
| CharAnimId | `11` |

## Lua level evidence
Source: `Assets/StreamingAssets/Reference/gaibang.lua`, key `kanglong_youhui`.

| Attribute | L20 value | Meaning |
| --- | ---: | --- |
| `skill_misslenum_v` | `15` | fifteen dragon missiles at L20 |
| `skill_misslesform_v` | `2` | fan/radial spread form |
| `skill_param1_v` | `2` | fan angle step in 64-direction units |
| `missle_speed_v` | `32` | PC Lua visual speed override |

## Missile row: `MissleId=48`
Source: `Assets/StreamingAssets/Reference/PcAttrib/missles.txt` decoded with CP1258.

| Field | Value |
| --- | --- |
| MissleName | `Kháng Long Hữu Hối` |
| MoveKind | `1` |
| FollowKind | `0` |
| ColFollowTarget | `1` |
| CollidRange | `1` |
| IsRangeDmg | `0` |
| DmgRange | `1` |
| LifeTime | `16` |
| Speed | `10` |
| AnimFile2 | `\spr\skill\Ø¤°ï\mag_gb_05_¿ºÁúÓĐ»Ú.spr` |
| SndFile2 | `\sound\skill\¿ºÁúÎ̃»Ú.wav` |
| AnimFile4 | `\spr\skill\Ø¤°ï\mag_gb_bz5_±¬Ơ¨Đ§¹û.spr` |
| SndFile4 | `\sound\skill\sound_k037.wav` |

## Hashed resources
Computed with the JX Pack Hash UID algorithm over normalized raw path bytes.

| Resource | Hash | Found |
| --- | ---: | --- |
| SkillIcon | `98055770.spr` | `/var/www/jx-source/pak_unpacked/update01/unknown/98055770.spr`, `/var/www/jx-source/pak_unpacked/spr/unknown/98055770.spr` |
| PreCastSpr | `b91ab706.spr` | `/var/www/jx-source/pak_unpacked/skills/unknown/b91ab706.spr` |
| ManCastSnd | `805e4929.wav` | hash computed; quick bounded lookup did not find file |
| FMCastSnd | `a9f82272.wav` | hash computed; quick bounded lookup did not find file |
| Missile AnimFile2 dragon | `a31b9f04.spr` | `/var/www/jx-source/pak_unpacked/skills/unknown/a31b9f04.spr` |
| Missile SndFile2 | `3b6bb009.wav` | `/var/www/jx-source/pak_unpacked/sound/unknown/3b6bb009.wav` |
| Missile AnimFile4 impact | `c33e96c2.spr` | `/var/www/jx-source/pak_unpacked/skills/unknown/c33e96c2.spr` |
| Missile SndFile4 | `ff4dadbd.wav` | hash computed; quick bounded lookup did not find file |

## Behavior implication
`Kháng Long Hữu Hối` is not a Phi Long-style homing lane skill. It uses `MisslesForm=2` / Lua `skill_misslesform_v=2`, so mobile must route to `SetupPcKangLongSpread` and produce fan/radial lanes based on `skill_param1_v`, with no `missileTargetOffsets` homing lane array.
