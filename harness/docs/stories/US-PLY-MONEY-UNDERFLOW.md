# US-PLY-MONEY-UNDERFLOW Preserve PC money underflow rejection

## Status

implemented

## Lane

normal

## Product Contract

When an authoritative player-money adjustment would make the balance negative,
the backend must reject it without changing state or performing persistence side
effects, matching the bounded current PC `KInventory::AddMoney` behavior. Exact
zero, positive grants and affordable deductions remain valid. The failure must
be observable to the application caller rather than silently clamping to zero.

## Relevant Product Docs

- `/var/www/vltk-mobile/backend/specs/06-gap-checklist.md`
- `/var/www/vltk-mobile/backend/specs/domains/p1-player-content.md`
- `/var/www/vltk-mobile/domains/server-runtime/README.md`
- `/var/www/vltk-mobile/contracts/legacy-mapping.md`

## Acceptance Criteria

- PC revision/path/SHA and ordered `KPlayer::Pay|Earn` →
  `KItemList::CostMoney|AddMoney` → `KInventory::AddMoney` behavior are recorded.
- Backend loads current money from server-owned `PlayerState`; callers cannot
  supply the current balance.
- Underflow rejects with unchanged money and no flush/refresh; exact-zero,
  affordable deduction and positive grant update once and return the new balance.
- A real `PlayerStateService` fake-UoW test proves success and failure ordering;
  an internal mission-reward caller remains compatible.
- Full target wallet/ledger, int overflow, repository-room sync, repute and public
  API migration remain explicit non-goals/gaps.

## Design Notes

- Commands: one internal authoritative money adjustment.
- Queries: existing player repository lookup.
- API: no endpoint or DTO shape change.
- Tables: no model or migration change.
- Domain rules: reject final balance below zero; do not clamp.
- UI surfaces: Not applicable.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | PlayerStateService fake-UoW success, exact-zero and underflow no-side-effect cases. |
| Integration | Not required for this bounded application rule; no shared PostgreSQL. |
| E2E | Not applicable. |
| Platform | Not applicable. |
| Release | `srcwalk review`, changed-scope Ruff/Black, player unit tests, strict specs. |

## Harness Delta

Intake #10 and this story track the bounded wave. Three fresh read-only Herdr
lanes establish the source/caller/failure contract before one writer.

## Evidence

- Herdr run `orch-f649a849ab85f21c` finished verified; `RESULT.json` SHA-256
  `89f038f7d46c2e415bf5be4d7a80ed0d7894a0ed9166bfaf37183ed89d29496a`.
- Canonical nested PC revision `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`;
  exact file hashes/ranges and ordered Pay/Earn call chains are recorded in
  `/var/www/vltk-mobile/backend/specs/07-provenance.md`.
- Backend implementation is in `PlayerStateService.add_money` with dedicated
  409 sentinel; executable proof is
  `tests/unit/modules/player/test_service_money.py` using the real service and
  real `RealRewardGrant` caller with an event-log fake UoW.
- Fresh root proof: Harness completion command collected `735 passed` across the
  player suite and mission reward unit tests; changed-file Ruff/Black pass, strict specs validator returns
  `OK: inventory=106183 coverage=104655 strict=True`, and `git diff --check`
  passes.
- Review findings were integrated: HTTP 409 is asserted, the PC call-chain
  direction was corrected, `KItemList::CostMoney:1924-1936` was pinned, and the
  status was conservatively lowered to bounded executable application-service
  proof rather than `runtime-wired`.
- Overflow/max balance, per-room inventory, repository-room drain,
  wallet/ledger, repute, runtime sync and PC BOOL public-return parity remain
  explicit gaps.
