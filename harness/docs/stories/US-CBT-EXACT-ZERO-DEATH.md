# US-CBT-EXACT-ZERO-DEATH Port PC CalcDamage exact-zero death semantics

## Status

implemented

## Lane

normal

## Product Contract

The backend combat damage path must preserve the canonical PC `CalcDamage`
death threshold: a target whose current life is exactly zero is already dead,
so the backend must not treat zero as a surviving state. The implementation and
proof must be scoped to behavior demonstrated by the current PC source; it must
not claim parity for the other unresolved damage branches.

## Relevant Product Docs

- `/var/www/vltk-mobile/backend/specs/domains/p1-skill-combat.md`
- `/var/www/vltk-mobile/backend/specs/06-gap-checklist.md`
- `/var/www/vltk-mobile/backend/specs/07-provenance.md`
- `/var/www/vltk-mobile/backend/specs/08-acceptance-audit.md`

## Acceptance Criteria

- The exact current PC source path, revision, SHA-256, and function/line mapping
  proving the `<= 0` threshold are recorded.
- The backend call path uses the same threshold and does not let an API/client
  caller override the death decision.
- Executable tests cover positive, exact-zero, and negative life with the
  canonical branch ordering and pass freshly.
- Existing unresolved `CalcDamage` branches remain explicitly marked as gaps;
  this story does not downgrade them to parity claims.

## Design Notes

- Commands: combat damage application/service path only.
- Queries: existing combat state and damage request; no new persistence.
- API: no endpoint or envelope change.
- Tables: none.
- Domain rules: preserve PC death threshold; do not infer RNG, time, or engine
  units from this slice.
- UI surfaces: Not applicable; this is server combat behavior.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Focused combat resolver/service tests for positive, zero, and negative life. |
| Integration | Not required for this pure domain slice; do not use shared PostgreSQL. |
| E2E | Not applicable to this bounded behavior wave. |
| Platform | Not applicable. |
| Release | `srcwalk review`, `ruff check`, `black --check`, and relevant specs validation. |

## Harness Delta

No Harness code or policy was changed. Durable intake/story/trace records track
the wave. Repeated Herdr cross-pane boundary attribution friction is recorded as
a separate Harness backlog item.

## Evidence

- Canonical PC: nested revision `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`,
  `KNpc.cpp` SHA-256
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`,
  life subtraction at line 2708 and `<= 0` death branch at line 2710.
- Backend: `combat_resolve.calc_damage` subtracts life then projects death for
  final life `1`, `0`, and `-1`; service proof calls the real domain function.
- Machine-readable coverage row `PC_CORE-83631120B188` matches the tracked
  override, remains `pending`/`stub/TODO`, and strict validation now rejects
  override drift.
- Fresh `story complete` verification: `649 passed`; changed-scope Ruff and
  Black passed; specs strict returned
  `OK: inventory=106183 coverage=104655 strict=True`.
- Final read-only Herdr run `orch-fac4d3e8b27333a6` finished verified with one
  clean boundary report. `RESULT.json` SHA-256:
  `461bbe489bf16fb72bf2d87e9a87c817ebaf2cd04098258e13bf3705e1ad9222`.
- Residual: no claim of C++ return parity, typed REST/OpenAPI transport, Unity
  overkill representation parity, global death hooks, or full CalcDamage;
  GAP-CBT-001 and GAP-CBT-002 remain open.
