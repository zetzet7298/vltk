# P1 Production Boot: Python-backed Editor-first login to Ba Lăng 53

## Current Behavior

P1 previously carried a Go proof backend under `server-runtime/`, but the
Production scene only created map/avatar/joystick composition and never invoked
the Go boot coordinator. The repository also already had a Python FastAPI game
backend and a typed Unity REST client for login, roles, map entry, movement,
skills, combat, and items.

The selected architecture is now Python-only for the game server. The Go backend
is being removed and the Production scene is being wired to the existing
FastAPI client. Canonical map `53` runtime artifacts continue to reject map `79`,
TestData, filesystem, loose-path, and absolute-path fallback.

Earlier Go/PostgreSQL and WSS results are historical evidence only after the
architecture change. They do not prove the Python-backed Production path and do
not promote Harness proof flags.

Current CNPM baseline remains `G0 READY`; `G1/G2/G3 BLOCKED`; and `G4/G5/G6
UNVERIFIED`. Known blockers are: UI flag signature metadata is checked without
cryptographic Ed25519 verification; the map artifact has no production signature;
the catalog audit reports 182 unresolved legacy SPR references; an
existing-database migration path is not proven; and live REST-to-WSS Editor E2E
plus Android evidence are absent. The story remains `in_progress`; all four
Harness proof flags remain false.

## Target Behavior

Implement the smallest Python-backed Production boot slice:

1. Load the real FastAPI backend config from Resources/StreamingAssets.
2. Login, or create the deterministic Editor-first account then retry login.
3. Select the first role, or create a deterministic role when none exists.
4. Enter canonical Ba Lăng map `53` using valid positive PC/server coordinates.
5. Bind REST movement synchronization to the production avatar.
6. Keep map/avatar/joystick composition independent from backend availability so
   connection errors are visible without destroying the scene.

Initial implementation target is Editor-first only and may stop at
`FUNCTIONAL` / `UNVERIFIED`. This story does not claim P1 `G4` completion,
production readiness, runtime parity, or Android readiness.

## Affected Users

- Player: can reach first playable session in Editor/dev environment once the
  implementation lanes integrate and pass their checks.
- Backend writer: has one canonical Python implementation for account, role, map,
  and movement behavior.
- Content writer: receives bounded Ba Lăng 53 content/bootstrap scope.
- Unity writer: receives bounded FastAPI Editor-first boot/movement scope.
- QA/Harness: receives traceability, validation layers, and stop conditions for
  implementation and review.

## Traceability

| Kind | IDs | Source |
| --- | --- | --- |
| Objective | `OBJ-P1-001` | `harness/specs/jx-pc-mobile-port/01-yeu-cau.md` |
| Functional requirements | `FR-AUTH-001`, `FR-AUTH-002`, `FR-CHAR-001`, `FR-SESS-001`, `FR-WORLD-001`, `FR-MOVE-001` | `harness/specs/jx-pc-mobile-port/01-yeu-cau.md`, `02-mo-hinh-yeu-cau.md` |
| Acceptance tests | `TEST-ACS-001`, `TEST-ACS-002`, `TEST-MAP-053` | `harness/specs/jx-pc-mobile-port/domains/account-character-session.md`, registry tests/requirements, `as-is/gaps.yaml`, `as-is/contradictions.yaml` |
| Gate context | `G0` ready; `G1/G2/G3` blocked; `G4/G5/G6` unverified | Work order known facts; acceptance plan gate semantics |

`TEST-ACS-001/002` are linked for account/character/session traceability. Their
delete/restore and full recovery portions remain out of scope here unless a later
approved split expands this story.

## Explicit Non-goals

- Android device/FPS proof.
- Combat behavior, target selection, skill runtime, parity golden, or promotion.
- Loot, inventory, equipment, wallet, economy, buy/sell, ACK-after-commit proof.
- Password reset/OTP flow.
- Character delete/restore flow.
- Go P1 REST/WSS compatibility or realtime transport migration.
- P1 `G4` vertical-slice completion.
- `G5` scale or `G6` release/restore/rollback.
- Cross-phase G4 promotion or `PARITY_DONE` claim.
