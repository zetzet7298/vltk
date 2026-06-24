# HUD Full Port — Story Slice Plan

**Intake ID:** #24 (high-risk, new initiative)
**Source of truth:** `/var/www/vltk-mobile/vltkunity` (Unity project) — NOT jx-source PC.
**Target:** `/var/www/vltk-mobile` (vltk-mobile)

## Inventory snapshot

### vltkunity source (`client/Assets/Resources/WorldGameUI/Prefabs/` = 91 prefabs; `client/Assets/Scripts/UI/` = 40 scripts)
Groups: BiKip(7), Chat(8), Equipment(2), HoatDong(11), PhucLoi(5), QuaDangNhap(2), Quest(2), Rank(2), Shop(3), Skill(3), Storage(3), TinMoi(1), + 42 top-level prefabs (MiniMap, TopBar, Money, Avatar, NpcDialog, Enhance, Train, Guild, MailBox, Login, Map, Quest, Skill, Stronger, FindMaster, FirstRecharge, ...)

### vltk-mobile target (~40 PanelService + 6 VltkUnityAdapters already exist)
Existing adapters: Bag, Chat, Equipment, MiniMap, Skill, TopBar.
Existing PanelService: Achievement, Arena, Auction, Bag, BattleMap, Character, ChatRoom, Compound, DailyTask, Exchange, FactionBonus, Fashion, FlipCard, Foundry, Friend, Guild, HongBao, HuaShan, HuoYueDu, Inventory, LoadingScreen, Mail, Mall, Map, Meridian, Mount, NpcDialog, PcSkill, QuestTask, Ranking, Reputation, Settings, SignIn, SkillTree, Stall, StallBrowse, SystemMenu, Team, Title, TitleEffect, TreasureHunt, TreasureMall, Vip, WorldBoss.

**Conclusion:** Most panels already ported. "100% HUD" = **gap-fill + parity verification**, not greenfield port.

## Story slices (each = one 8-step pipeline run)

| Slice | vltkunity group | vltk-mobile (existing) | Work type |
|-------|-----------------|------------------------|-----------|
| **S1 Chat** | Chat (8) | ChatRoomPanelService + ChatVltkUnityAdapter | parity |
| **S2 Equipment/Char** | Equipment (2) + top-level Status | EquipmentPanelVltkUnityAdapter + CharacterPanelService | parity |
| **S3 Skill** | Skill (3) | SkillPanelVltkUnityAdapter + SkillTree/PcSkill | parity |
| **S4 Storage/Bag/Inv** | Storage (3) + Bag | BagPanelVltkUnityAdapter + Inventory/Exchange | parity |
| **S5 HUD chrome** | MiniMap, TopBar, Money, Avatar, DeviceStatus, ProgressBar | MiniMapVltkUnityAdapter + TopBarVltkUnityAdapter | parity + gap-fill (4/6 missing) |

### S5 detail — cross-framework port (uGUI → UI Toolkit)

- **Plan:** `harness/planning/s5-plan.md` (authoritative step-by-step)
- **Recon:** `harness/intake/s5-source-recon.md` · **Research:** `harness/research/s5-research.md` · **Story:** `docs/stories/story-hud-chrome-s5.md`
- **Shape:** 2/6 parity verify (MiniMap, TopBar), 4/6 gap-fill (Money, Avatar, DeviceStatus, ProgressBar).
- **Critical path:** widen `HudSnapshot`/`IRuntimeStateProvider` (stamina + real MP/EXP maxima) before TopBar parity — compile break across all implementors.
- **Key gaps (from recon):** 🔴 T1/T2 stamina bar shows HP data (contract bug), 🔴 M1 minimap dot formula divergence, 🔴 Y1/Y2 Money widget missing; 🟡 M3 coord order, T3 MP max=100, T5/P3 visual/sprite parity, A1/D1 widgets missing.
- **Test category:** reuse existing `HUD` (NOT a new `HUDChrome` category) — `TopBarVltkUnityAdapterTests` already uses `[Category("HUD")]`. Dev loop: `category_names=["HUD"]`; full suite only pre-push.
| **S6 HoatDong** | HoatDong (11) | HuoYueDu + DailyTask + Team | gap-fill |
| **S7 BiKip** | BiKip (7: MiJi, TamPhap, YeuQuyet, QuenBiKip, TangThanPhap) | Foundry/Compound? | gap-fill (LIKELY MISSING) |
| **S8 Welfare/Login** | PhucLoi (5) + QuaDangNhap (2) + FirstRecharge + SignIn | SignInPanelService | gap-fill |
| **S9 Rank** | Rank (2) | RankingPanelService | parity |
| **S10 Shop/Mall** | Shop (3) | Mall + TreasureMall | parity |
| **S11 Quest** | Quest (2) | QuestTaskPanelService | parity |
| **S12 News/Mail** | TinMoi (1) + News + MailBox | MailPanelService | parity |
| **S13 Misc top-level** | Guild, Train, Enhance, NpcDialog, Mount, Friend, Stronger, FindMaster, PointSetting | Guild, Mount, NpcDialog, Friend + others | gap-fill |

## Priority
1. **Gap-fill slices first** (S7 BiKip, S8 Welfare, S6 HoatDong) — these are most likely incomplete.
2. **Parity slices** (S1–S5, S9–S13) — verify each panel opens from buttons, all sub-widgets present.

## Pipeline per slice
Run the 8-step chain (intake→recon→research→plan→implement→[parity+quality review]→finalize) with `{task}` = the specific slice, source repo = vltkunity. Each slice is independently shippable.
