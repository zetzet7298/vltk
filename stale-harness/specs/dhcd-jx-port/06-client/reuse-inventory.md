# Client Reuse Inventory

| Trường | Giá trị |
|---|---|
| Mục đích | Lập inventory module hiện hữu trước khi thay thế |
| Trạng thái | `provisional` |
| Owner / reviewer | Client lead / technical lead |
| Cập nhật | 2026-07-15 |
| Evidence snapshot | Unity revision `d4b1b06aef150739b97ab693741841aa3e51ea8f`; source paths below are read-only inventory evidence, not PC parity proof |

## Classification

| Module/surface | Reuse candidate | Evidence cần chốt | Quyết định |
|---|---|---|---|
| `CityDefenceService` / parser | Chỉ parser/import wave data | `DateTimeOffset.UtcNow`, runtime state và reward grant hiện tại không deterministic/canonical | Chỉ reuse parser; thay runtime orchestration/reward bằng tick/input/mirror |
| `MapEnemyDatabase` | Có thể giữ generated roster lookup | Code hiện tại chứa curated template, fallback enemy ID/spawn và map registration | Cấm fallback/default/curated path trong pilot; chỉ lookup roster versioned đã audit |
| Existing Unity player/HUD | Rendering/input seam | JX visual/UI manifest, portrait golden | Reuse từng component |
| Existing harness SQLite | Test metadata | Không phải game persistence | Chỉ dùng test tooling |
| Unity MCP scripts | Editor automation | scene/script/test reproducibility | Reuse nếu không thay runtime contract |

## Evidence-Backed Module Inventory

All rows use the Unity revision in the snapshot above. `blob` is the Git object at
that revision; `sha256` is the on-disk file digest captured on 2026-07-15. The
authoritative PC corpus remains read-only at `/var/www/jx-pc`; Unity comments
that name PC files are implementation clues, not PC source authority. No PC asset,
config, behavior, or visual winner is selected by this inventory.

| Module ID | Exact implementation evidence | Current behavior bounded by evidence | Source authority and decision | Allowed reuse / forbidden path | Adapter, shadow, migration gap | Flag, rollback, retirement |
|---|---|---|---|---|---|---|
| `CITY-DEFENCE-PARSER` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/CityDefenceService.cs:15-125`; blob `aa9d94a58901d40cb5618aa2589717cda7e20e87`; sha256 `3386dd15b36f1b9e86413e2c5028f29e381fcbe1ea80ff0e884e24552ae5fdff` | Holds a `PcCityDefenceRegistry`, exposes reads at lines 41-54, and loads it through `PcCityDefenceParser.BuildRegistry` at lines 114-123. | JX city-defence data/behavior must be proven from `/var/www/jx-pc`; this Unity service and its comments are not authority. Classification: `parser-only`. | Reuse registry/parser import seam only. Forbidden: `TriggerWave` wall-clock state (`DateTimeOffset.UtcNow`, line 60), host spawn/effect/notice orchestration (lines 69-84), and reward grant (lines 87-104). | No deterministic tick/input adapter, old/new shadow event comparison, save/replay migration fixture, or PC behavioral proof is recorded. | No feature flag may enable this runtime. Future flag requires adapter plus zero unexplained shadow divergence; rollback must retain old state/reward path and save/replay compatibility; retire only after two pilot cycles with zero divergence and reviewer approval. |
| `MAP-ENEMY-AUDITED-ROSTER` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/MapEnemyDatabase.cs:20-42,45-145`; blob `cd01495502100f6f1df8215c1b3a36aee27942ad`; sha256 `5a9a3b22337a62dff4fdb707221eef5684e36c2607c87d266bf4f918375a2429` | Loads `PcNpcSFullParser` from StreamingAssets at lines 24-41, but also defines curated `SharedTemplates`, map defaults, and default spawn points at lines 45-145. | JX `NpcS.txt` and `Region_S.dat` under `/var/www/jx-pc` are authoritative only after provenance and selected-source evidence. The curated Unity data is not authority. Classification: `audited-roster-only`. | Reuse only a versioned roster lookup whose complete PC source evidence is audited. Forbidden: curated `SharedTemplates`, `MapEnemyTemplates`, `DefaultSpawnPoints`, merge/default behavior, and synthetic/fallback mappings. | No roster adapter contract, source-to-roster audit, shadow roster comparison, spawn replay migration test, or fallback-absence test is recorded. | No pilot flag before audited roster proof. Rollback must select the prior versioned roster without introducing defaults; retire legacy lookup only after two pilot cycles, zero roster/spawn divergence, and reviewer approval. |
| `PC-PORTRAIT-PARSER` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/PcPortraitParser.cs:12-62`; blob `5000e0d9fe7812ae87e80931a102e68fa6f44694`; sha256 `e7c04699bd53268c20bb0b11dd5c53f8a8ff484e05af37b2cd7a0e9f4260d733` | Parses tab-separated portrait rows and builds an ID registry from `.ini`/`.txt` files. | PC portrait config and its referenced SPR path must be resolved from `/var/www/jx-pc` with resource provenance; current parser comments are not asset or visual proof. | Parser/registry shape may be reused behind a new adapter only. Forbidden: treating `sprPath` text as resolved asset identity or using it to claim portrait visual parity. | Missing encoding/resource-resolution evidence, portrait manifest, adapter contract, portrait golden/shadow comparison, and migration fixture. | Feature flag is prohibited until manifest and golden evidence exist. Rollback keeps existing portrait consumer; retirement requires two pilot cycles with zero adapter/golden divergence and reviewer approval. |
| `HUD-DATA-BRIDGE` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/HudDataBridge.cs:13-40,85-125`; blob `ec717881082ccaf89d9ab0f30938a117a079a2cd`; sha256 `b483c4bec73e4cc1aa6243548c0e620343b7a628835c0035487d37abf53b0ea2` | Defines `IRuntimeStateProvider` and builds a HUD snapshot from a runtime provider; the implementation clamps display values. | JX HUD layout/art/behavior remains authoritative only from `/var/www/jx-pc` evidence; the Unity interface is a rendering/input seam, not parity proof. | Reuse only the provider-to-snapshot adapter seam. Forbidden: using current clamp/default/display semantics as canonical JX HUD behavior. | Missing PC HUD manifest, adapter mapping, state/event shadow comparison, visual golden, and save/login migration coverage. | Flag requires approved adapter and state/event/visual evidence. Rollback must restore the old HUD consumer without changing runtime persistence; retire after two pilot cycles with zero divergence and reviewer approval. |
| `GOLDEN-SNAPSHOT-COMPARER` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/GoldenSnapshotComparer.cs:31-145`; blob `fb4146ff6502f0d692e6ec3cf7ffcdfcaf555313`; sha256 `460ab84fb53b8a39739d66606546ca6459212f8b2da4c6c855ffe40721904f6c` | Builds a quantized pixel signature and flags a regression above tolerance; it is an EditMode-capable comparison utility. | A comparator is test tooling only. JX visual authority still requires selected, resolved PC UI/portrait evidence from `/var/www/jx-pc`. | Reuse as a shadow/golden test utility only. Forbidden: treating a passing Unity-vs-Unity comparison as JX visual parity or runtime migration proof. | Missing approved PC-backed fixture set, capture contract, tolerance review, and migration replay linkage. | No production feature flag applies. Retirement of a legacy comparison path requires equivalent fixtures and stable zero unexplained divergence across two pilot cycles. |
| `COMBAT-RUNTIME` | `/var/www/vltk-mobile/Assets/Scripts/Sandbox/CombatRuntimeService.cs:59-165`; blob `156518b8ed541220c6887d9f38899d8c9a12d802`; sha256 `ede97a1a3c76b872429fe9c3c6f8b9a4a0f54161956b3f33cd4ec14063d74bb8` | Provides combat cast gating, mutable combat actor state, clock/cooldown state, projectile and damage collaborators. | JX combat source/data in `/var/www/jx-pc` is authority; Unity comments and current behavior are not sufficient proof. | No runtime reuse is admitted by this inventory. Permitted only as a read-only comparison subject for a separately evidenced port. Forbidden: direct migration, feature enablement, or treating current formulas/timing as canonical. | Missing PC evidence record, adapter boundary, deterministic shadow replay, migration/save fixture, and rollback rehearsal. | No flag and no retirement are permitted under US-P0-003. A future story must define those criteria after PC evidence is verified. |

## Common Contract

Every future reuse decision must retain: `module_id`, exact path/line, Unity
revision/blob/SHA-256, owner/reviewer, current behavior, source authority,
allowed reuse, forbidden path, adapter, shadow test, migration test, feature
flag, rollback, and retirement criteria. A missing field is `provisional`, not
permission to migrate. Do not replace a module solely because it is old; an ADR
and the required migration proof are mandatory.

## Acceptance

- [x] Every inventory row has exact path/line, revision/blob/SHA-256, bounded current behavior, and source-authority limitation.
- [x] `CityDefenceService` is parser-only and `MapEnemyDatabase` is audited-roster-only; their forbidden runtime/default paths are explicit.
- [x] Adapter, shadow, migration, feature-flag, rollback, and retirement proof gaps are explicit.
- [ ] Runtime migration remains unproven and out of scope for this inventory.
