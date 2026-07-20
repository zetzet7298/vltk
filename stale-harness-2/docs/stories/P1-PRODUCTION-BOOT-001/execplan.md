# Exec Plan

## Goal

Migrate the Production Unity scene to the existing Python FastAPI backend, remove
the Go backend, and preserve the smallest Editor-first path from login to Ba Lăng
53 REST movement without making release, parity, Android, economy, or combat
claims.

## Scope

In scope:

- Durable Harness packet and operational story row.
- Trace links to `OBJ-P1-001`, `FR-AUTH-001/002`, `FR-CHAR-001`,
  `FR-SESS-001`, `FR-WORLD-001`, `FR-MOVE-001`, `TEST-ACS-001/002`,
  and `TEST-MAP-053`.
- Unity REST seams for account create/login, role create/select, map 53 entry,
  and movement synchronization.
- Content seam for canonical Ba Lăng map `53` manifest/digest.
- Unity Editor-first seam for real FastAPI boot and deterministic first-run
  provisioning.
- Permanent deletion of `server-runtime/` and replacement of active Go-specific
  documentation/validation references.
- Logs/audit evidence sufficient for Harness trace.

Out of scope:

- Android device/FPS validation.
- Combat, target, skill, replay, PC golden, visual parity, or `PARITY_DONE`.
- Loot, inventory, equipment, wallet, economy, buy/sell, level 1-200 completion.
- Password reset/OTP.
- Character delete/restore.
- Python realtime/WebSocket design or Go protocol compatibility.
- P1 `G4`, `G5`, `G6`, release, restore drill, scale, migration, rollback.
- Edits to canonical CNPM specs, registries, gates, backlog, templates, or
  unrelated product/UI files.

## Risk Classification

Risk flags: auth, data loss, public contracts, existing behavior, weak proof, and
multi-domain. Hard gates are auth and explicit deletion of the old backend. The
owner selected Python and explicitly authorized deletion.

## Work Sequence

1. Create story packet from high-risk templates.
2. Add Harness DB story row with status `planned`, risk lane `high_risk`, and all
   proof flags false.
3. Root-review the packet, record Wave 0 trace, and move the story to
   `in_progress` without changing proof flags.
4. Add typed account/role creation calls and deterministic runner provisioning.
5. Wire Production composition to the real FastAPI runner and map 53.
6. Remove `server-runtime/` and update active references to the Python backend.
7. Run Python, Unity-boundary, focused Unity tests, and repository stale-reference
   checks.
8. Record only command-backed evidence; do not promote gates, parity, Android, or
   production readiness.

## Stop Conditions

Stop and do not mark implemented when any condition appears:

- Python account, role, map, or movement contract contradicts the typed Unity
  DTOs.
- Map `53` resolves only through alias/remap `79`.
- Auth/session proof would print raw password, refresh token, admission ticket, or
  secret material.
- Content runtime needs filesystem fallback or unpinned digest.
- Movement acceptance depends on combat, inventory, economy, or Android device
  work.
- Deletion would remove a contract or generated C# artifact still consumed by
  Unity.
- Any required `G1`, `G2`, or `G3` blocker remains for a claimed promotion.
- Any `G4`/release/platform/parity claim is requested without E2E PASS evidence,
  revision, environment, artifact hash, and reviewer sign-off.
- Harness CLI cannot record or query story state.

## Initial Status

`in_progress` after root intake review and trace. Product implementation starts in
bounded lanes; proof flags remain false, with no `implemented`, `G4`, or parity
promotion claim.
