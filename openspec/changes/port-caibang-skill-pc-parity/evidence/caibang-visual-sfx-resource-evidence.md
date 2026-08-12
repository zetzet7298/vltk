# Cai Bang Visual / SFX Resource Binding Evidence (Phase 5)

All UIDs below are the JX Pack Hash computed with the runtime algorithm
`SprRuntimeService.ComputePathUid(path, "GB2312", signedBytes: true)` — the exact
hash the mobile resolver uses at render time. Each PC path was re-hashed and the
resulting `{uid}.spr` was confirmed present on disk in the mobile runtime sprite
root `SpritesRuntime/` (and in the PC source `pak_unpacked/skills/unknown/`).

## Hash algorithm parity proof
The runtime signed-GB2312 hash reproduces the previously recorded missile UIDs exactly,
proving the proper Chinese paths (not CP1258 mojibake) are the hash source of truth:

| PC path (proper GBK) | Runtime signed UID | Matches prior evidence |
| --- | --- | --- |
| `\spr\skill\丐帮\mag_gb_05_亢龙有悔.spr` | `a31b9f04` | yes (phi-long-resource-evidence) |
| `\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr` | `c33e96c2` | yes (phi-long-resource-evidence) |

## Missile / impact / pre-cast SPR (already bound)

| Resource | PC path | UID | In `SpritesRuntime/` |
| --- | --- | --- | --- |
| Dragon flight | `\spr\skill\丐帮\mag_gb_05_亢龙有悔.spr` | `a31b9f04` | yes |
| Impact burst | `\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr` | `c33e96c2` | yes |
| Pre-cast (117/128/357) | `\spr\skill\天忍\mag_bz_huo3_爆炸效果.spr` | `b91ab706` | yes |

## State-aura SPR (buff skills)
Source: `PcSkillVisualAutoMapper.GetStateAuraData` (PC `状态与光效图形对照表.txt`).

| State | Skill(s) | PC path | UID | In `SpritesRuntime/` |
| --- | --- | --- | --- | --- |
| 43 | Túy Điệp Cuồng Vũ (130) | `\spr\skill\丐帮\mag_gb_11_醉蝶狂舞.spr` | `7d34af1d` | yes |
| 44 | Đả Cẩu Trận (277) | `\spr\skill\丐帮\mag_gb_12_打狗阵.spr` | `202667bb` | yes |

State-43 anim metadata (auto-mapper): pos=3 (body), frameStart=4, frameEnd=12, totalFrames=16, dir=1, interval=1.
State-44 anim metadata (auto-mapper): pos=2 (feet), frameStart=0, frameEnd=0, totalFrames=8, dir=1, interval=1.

## Skill icon SPR

| Skill | PC path | UID | In `SpritesRuntime/` |
| --- | --- | --- | --- |
| Kháng Long Hữu Hối (128) | `\spr\Ui\技能图标\icon_sk_gb_41.spr` | `98055770` | yes |
| Phi Long Tại Thiên (357) | (icon UID `d97b70ca`, also staged in `Assets/StreamingAssets/Sprites/SkillIconsPc/`) | `d97b70ca` | yes |

## Cast SFX (PC skills.txt cols 7/8 → AudioService)
`AudioService.PlaySkillCast` converts a PC path `\sound\skill\sound_kNNN.wav` to
`sound/skill/sound_kNNN.wav` and loads it from StreamingAssets. The clips are bound by
plain name (not hashed), present in both `Assets/StreamingAssets/sound/skill/` and
`AudioRuntime/Skill/`.

| Clip | Used by (ManCast/FMCast) | Present |
| --- | --- | --- |
| `sound_k005.wav` | 128/357 ManCast | yes |
| `sound_k010.wav` | 128/357 FMCast | yes |
| `sound_k037.wav` | missile 166 SndFile4 (flight) | yes |

## Implication for Phase 5
The visual and SFX resources for the Cai Bang acceptance skills (Phi Long 357,
Kháng Long 128, and the Túy Điệp/Đả Cẩu state auras) are already physically bound in the
mobile runtime roots and resolve through the existing signed-hash path in
`SprRuntimeService` and the name-based path in `AudioService`. No re-import is required;
the remaining Phase 5 work is verification coverage (sprite existence, frame metadata,
non-null SFX, and auto-mapper resource wiring), captured by
`CaiBangVisualResourceParityTests`.
