# Execution Plan: DHCD Feature Docs — Gap Closure

Date: 2026-07-16

## Status

Completed

## Outcome

Đóng 2 gap material cho clone survivor + triage 88 gap còn lại. Cập nhật feature
docs (bump `[? unresolved]` → `[✓ verified]` / `[~ inferred]` khi đóng). Tạo
build-spec-ready state cho SURVIVOR_PLAN mới (follow-up).

Phục vụ: feature docs hiện 90 gap (12.5%), 2 chặn Phase clone (core/06 damage
formula, core/08 pause sink). Đóng 2 material + triage → build spec grounded.

## Context

- Feature docs (done): `C:/Projects/dhcd/docs/features/` — 19 doc, 553V/77I/90U.
  Xem `meta/00-overview.md` Known Gaps + INDEX mỗi doc.
- Plan feature-docs (completed): `docs/plans/completed/dhcd-feature-docs-plan.md`.
- Source (read-only WSL primary): `//wsl.localhost/Ubuntu-24.04/var/www/dhcd/` —
  server `服务端/extracted_server/` (652 .txt config), client `客户端/`, video.
- RE escalation: `//wsl.localhost/Ubuntu-24.04/var/www/reverse-skill/`
  (skills/MASTER-ROUTING.md) — ida-reverse/radare2/binary-diff.
- IL2CPP corpus: `C:/Projects/dhcd/{reconstructed-types,diffable-cs,isil-r-dhcd-*}`.
- Blanket pre-approve owner còn hiệu lực (xem completed plan Decisions).

## Scope

**In scope:**

- **core/06 damage formula** — static ISIL + native ARM64 + mine server config
  damage-related (`SkillBaseConfg`, `SkillEffectFuncConfg`, `AttrDataConfig`,
  `ActorEffectConfig`, `BuffAttrConfig`, `BattleDamageNumConfig`,
  `BattleDamageAbbreviationConfig`). Reconstruct arithmetic (attr → element →
  crit → block/reduce → final).
- **core/08 pause sink identity** — static IL/native xác owner subsystem cho
  timescale sinks {0, 1, 1.5, 2}.
- **Triage 88 gap còn lại** — rubric A+C: verdict filter (meta defer P3, focus
  10 core) + essential-vs-polish. Đóng essential trong direct-clone group; polish
  + meta defer.
- Update feature docs: bump tag + Known Gaps section + INDEX status note.

**Out of scope:**

- Live probe Frida (C) — reserve, **escalate Human** nếu static+server bí.
- Server wire-protocol reverse (ADR #1).
- SURVIVOR_PLAN mới (build spec) = follow-up sau gap closure.

## Approach

1. **core/06 TRƯỚC** (hardest, longest, risk nhất) — de-risk sớm: biết có cần
   escalate Human live probe (C) hay không trước khi đầu tư phần còn lại.
   - Static: ISIL dump sâu `SkillDamageHelper`, `BulletMgr`, `ColliderDamageCmpt`,
     native ARM64 analysis qua reverse-skill (binary-diff từ method-map nếu có).
   - Server-mine: reconstruct formula param từ config plaintext (attr damage,
     element mult, crit rate/mult, block/reduce, toughness/ElementBreak).
   - Cross-check: video damage number floating (back-solve, weak nhưng confirm).
2. **core/08 + triage 88 SONG SONG** (độc lập, dễ hơn):
   - core/08: static IL/native xác `set_IsPause`/`ReCalcTimeScale` sink owner.
   - Triage: Lead scan 88 gap, rubric A+C, spawn peer đóng essential.
3. **Update docs**: bump tag, Known Gaps, INDEX status. Commit dhcd (git).
4. **Report**: gap đóng / stay-open / escalate list.

Update approach khi evidence đổi (vd: server config thiếu param → cần native sâu hơn).

## Evidence Update (2026-08-08, Lead recon trước khi spawn)

**Native client input GONE**: `libil2cpp.so` + `global-metadata.dat` + APK + emulator
đều không còn trên WSL/Windows (r-dhcd-003 input path đã bị xoá; chỉ còn
diffable-cs/recon/ISIL dump durable). Live probe (C) = không chỉ cấm mà **bất khả
thi** (emulator + APK mất). Không cần escalate live-probe.

**New primary cho core/06 + triage**: server `battle_server_999/runenv/battle_svr/bin/BattleCore.dll`
(.NET assembly + PDB, decompile CLEAN qua ilspycmd) = battle sim authoritative
cùng class set với client BattleCore (SkillDamageHelper, ColliderDamageCmpt,
BulletMgr, NpcEntity, LevelExpCalc, LevelWave, DefaultAttConfig, ElementToughness...).
Artifact staged: `tools/server-bin/BattleCore.dll` SHA `6bdfc0ba...`;
`battle_svr.pdb` SHA `c639688c...`. Pivot: native ARM64 → server .NET decompile
+ client IL recovery (`il2cpp/dll-il-recovery/`) + server config + video.

**Client IL recovery**: `il2cpp/dll-il-recovery/GameLogic.dll|BattleCore.dll`
decompile được nhưng body `SkillDamageHelper`/`ReCalcTimeScale` cũng corrupted
(như recon). Call-site scan (grep `set_IsPause`/`ReCalcTimeScale` trong
decompiled output) vẫn dùng được cho caller identity (C-B).

**Verified nhanh (Lead, trước spawn)**: server `SkillDamageHelper.CalWeaponDamage`
body đọc được — guard + `ProcessSkillAttr(49)` + `val *= 1+SkillDamageRatio` +
switch AttrType (1/2/3/11-14) + `HurtAddRatio` bonus + `m_casterAtk`. `s_nodeath`
= `DebugFilterDamage` cap damage=1 (`[Conditional DOD_DEBUG]`) — đóng gap cũ.

## Risks And Recovery

- **UPDATE 2026-07-16 (Lead recon):** native client (libil2cpp.so + metadata +
  APK + emulator) **đã MẤT** → live probe (C) **bất khả thi**, k chỉ cấm.
  Loại luôn owner-level risk. **Server `battle_server_999/BattleCore.dll` = .NET
  + PDB, decompile sạch** = authoritative battle logic source (cùng class set
  client, body đọc trọn vd `SkillDamageHelper.CalWeaponDamage`). = primary mới
  cho core/06 + triage, vượt IL2CPP malformed. Cân nâng ADR #1 (follow-up).

- **core/06 IL malformed intrinsic** — static có thể k recover arithmetic đầy đủ.
  Mitigation: server-mine + native + video back-solve multi-path. Nếu vẫn bí →
  **stop, escalate Human** xin phép (C) live probe Frida (owner-level: chạy client
  + inject). KHÔNG tự chạy client.
- **core/08 sink owner k xác định được từ static** — fallback: live probe hoặc
  accept `[~ inferred]` (sink set đã biết, owner = chi tiết).
- **Triage over/under-scoped** — essential vs polish主观. Mitigation: Lead rubric
  rõ (essential = feature k build faithful thiếu; polish = build được detail sau),
  Supervisor reconcile sample.
- **reverse-skill peer overload** — RE nặng, peer có thể loop. Mitigation: Lead
  bounded (time-box per gap), notifyOnFinish, KHÔNG poll.

Recovery: append-only plan, mỗi gap có trail (RE method tried + result), bump tag
chỉ khi ≥2 nguồn.

## Progress

- [x] Grilling gap-closure — 4 decisions lock (Q1-Q4).
- [x] Plan doc.
- [x] Spawn Lead gap-closure (`b9bb4e76`).
- [x] core/06 resolved — damage formula 14 bước + 4 element `[✓ verified]` (server BattleCore.dll decompile, DUAL review).
- [x] core/08 resolved — pause sink identity = `Time.set_timeScale` `[✓ verified]`.
- [x] Triage 88 + đóng essential (90U→18U core).
- [x] Update feature docs + INDEX + commit (6 commit dhcd + ADR #3).
- [x] Final report + move plan → completed.

## Decisions

- **Q1 → B**: Đóng 2 material (core/06, core/08) + triage 88.
- **Q2 → B/A**: core/06 = static + server-mine; core/08 = static IL/native.
  (C) live probe Frida = reserve, escalate Human.
- **Q3 → A+C**: Triage verdict filter (meta defer P3, focus core) + essential-vs-polish.
- **Q4 → A+(i)(ii)**: Reuse Lead+Peer pattern (batch 17 proven), peer load
  reverse-skill on-demand; core/06 trước, core/08+triage song song.
- **Blanket pre-approve** (từ feature-docs plan): Supervisor drive completion,
  chỉ escalate live-probe / contradiction-k-resolve / material risk.

## Validation

- **Gap "closed"** = bump `[? unresolved]` → `[✓ verified]` (≥2 nguồn: server+IL,
  hoặc server+native, hoặc IL+video) HOẶC `[~ inferred]` (1 nguồn + reasoning
  documented). Criterion per CONVENTIONS.
- **Gap "stays open"** = vẫn `[? unresolved]` sau exhausting RE → escalate list
  cho Human (live probe decision).
- **Triage audit**: 88 gap = có verdict (block-clone/essential / polish / defer-P3),
  ghi Known Gaps hoặc INDEX note.
- **Doc integrity**: Known Gaps section update, INDEX status note, git commit dhcd.
- **No regression**: tag bump k phá citation/existing verified claim.

## Result

**DONE 2026-07-16.** Gap closure hoàn chỉnh:

- **2 material gap đóng**: core/06 damage formula (14 bước + 4 element, server BattleCore.dll decompile verified, DUAL review) + core/08 pause sink identity (`Time.set_timeScale`, IL recovery + r-dhcd-003).
- **Triage 88**: essential đóng (card count ISIL, levelup call site, reroll economy, wave EndType enum, MonsterEntity, Type semantics, OutputType, CalAddVal, DoStartLevel...); polish + meta defer P3 với per-entry reason.
- **Claim stats core 10 doc: 444V/55I/18U** (90U→18U, 80% gap reduction). Còn 18U = polish (FRAME_CMD full set, input lock, modal→stack, Param1 max) — k block clone.
- **ADR #3 mới**: server `BattleCore.dll` (.NET+PDB) = battle logic primary (vượt IL2CPP malformed). Native client mất → live probe bất khả thi (loại owner-level risk).
- **Git dhcd**: 6 commit gap-closure + ADR #3, clean (HEAD past 65604bc + ADR commit).
- **Agents**: Lead `b9bb4e76` self-archived, 3 peer đóng sạch (C-A DUAL, C-T 2 REVISE rounds), orphan `6b4e934e` archived by Supervisor.
- **Notebook**: Lead tự viết completion entry (5 anti-pattern, Count:4) + Supervisor finish-guard entry bump count=3.
- **Finish-guard false-positive**: lần 3 cùng pattern, ruling verify 3/3 đúng (inspect archivedAt+artifact → ACCEPT).

**Follow-up:** (1) SURVIVOR_PLAN mới (build spec) — giờ build-spec-ready; (2) ADR #1 có thể amend reference ADR #3 cho battle logic; (3) 18U polish còn lại = defer đến Phase build tương ứng.

**Limitations:** DHCD native client mất = giới hạn RE DHCD tương lai (UI client-only, anti-tamper runtime k recover). 18U core polish k chặn clone.
