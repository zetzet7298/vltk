# US-PARITY-NEXT-SERVER-SLICE Port one server-owned PC behavior

## Status

implemented

## Lane

normal

## Product Contract

Select and port exactly one bounded canonical PC behavior from skill,
item/inventory, or player progression/persistence whose inputs and mutable state
are already owned by the backend server and reached through a real application
caller. Preserve source ordering, indices, integer/time/RNG and data dependencies
that can be proven. Do not introduce client-authored authoritative state or use a
pure helper/test-only call as runtime parity evidence.

## Relevant Product Docs

- `/var/www/vltk-mobile/backend/specs/06-gap-checklist.md`
- `/var/www/vltk-mobile/backend/specs/domains/p1-skill-combat.md`
- `/var/www/vltk-mobile/backend/specs/domains/p1-player-content.md`
- `/var/www/vltk-mobile/domains/server-runtime/README.md`
- `/var/www/vltk-mobile/contracts/legacy-mapping.md`

## Acceptance Criteria

- Three fresh read-only domain scouts independently identify exact current PC
  behavior/dependencies and backend state ownership/call path; root selects one
  candidate or records a precise dependency blocker.
- The selected source unit records nested PC revision, exact path, SHA-256,
  function/line or data record, variant/Include chain and unresolved semantics.
- A single writer changes one non-overlapping domain/application/test/spec slice;
  no public DTO, migration or cross-module direct table access is added.
- Focused executable tests invoke the real implementation through an application
  caller and cover success, boundary/ordering and no-op/failure behavior.
- Remaining behavior stays `stub/TODO`, `metadata-only`, `missing` or an explicit
  gap; the story never claims aggregate domain parity.

## Design Notes

- Commands: one server-owned vertical use case selected after scout collection.
- Queries: existing repository/application ports only.
- API: no shape change; legacy request data is not promoted to authority.
- Tables: no migration; integration/e2e require a proven disposable PostgreSQL.
- Domain rules: exact PC semantics only, with 0/1 indexing, encoding, time and RNG
  left explicit when unresolved.
- UI surfaces: Not applicable.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Focused domain plus real application/service cases for the selected behavior. |
| Integration | Only when necessary and after isolated PostgreSQL proof. |
| E2E | Not applicable unless the selected behavior has an existing safe in-process seam. |
| Platform | Not applicable. |
| Release | `srcwalk review`, changed-scope Ruff/Black, selected unit suite, strict specs validator. |

## Harness Delta

Intake #9 and this story track a fresh bounded wave. Herdr runs three read-only
scouts before one writer; scout/reviewer panes are never reused.

## Evidence

- Herdr run `orch-fd172130cc1b4c05` collected three clean read-only scouts,
  one single-writer implementation attempt, one fresh reviewer, one compatible
  writer reuse for corrections, and one fresh proof auditor. The first writer
  boundary ambiguity was limited to inability to stat the newly created exact
  owned test path at baseline; the correction attempt and all read-only lanes
  collected cleanly.
- Canonical nested PC revision:
  `d4bfc04a3dbb8f964be1ee8cd9b6dec6fc4e1b91`. Exact evidence:
  `KNpc.h:34-42` SHA-256
  `20968d2ca297e088df2d4399a250ba5a89d645d733887a854ce085764add5120`,
  `KNpc.cpp:1977,3654-3732` SHA-256
  `f8e274b459850e9c9a90442d9b5dc9a606eaaa200b15691c11c0d9b461fb6cea`,
  and `KSkills.cpp:3162-3188` SHA-256
  `c0c1b3c289923a305438c1a0d1b002a826fb9fb19127ecf05afb5b7b4644113e`.
- The bounded server-owned branch now maps raw `NPCATTRIB` cost types 0..5 to
  mana/stamina/life value and percent costs, uses maximum resources for positive
  percent calculation, allows mana/stamina exact depletion, preserves one HP
  for life costs, rejects invalid/negative configurations before any resource,
  cooldown, effects or flush side effect, and reports one consistent next-ready
  interval. Skill 210 raw type 1 now consumes stamina rather than mana.
- Root proof: `pytest -q tests/unit/modules/skill` → `181 passed`;
  `pytest -q tests/unit/modules/combat` → `649 passed`; changed-file Ruff and
  Black pass; `git diff --check` pass; strict specs →
  `OK: inventory=106183 coverage=104655 strict=True`.
- `Skills.rar` source-table winner remains unresolved because `vltktool`
  `extract_table_slice.py` rejected malformed row 465. Negative raw cost is an
  explicit fail-closed backend divergence until that content provenance is
  proven. Full cast context/style/clock and aggregate combat remain
  `GAP-SKL-002`/`GAP-CBT-002`, `stub/TODO`; no integration/e2e/database claim.
- Fresh Harness `story complete` reran the bounded command successfully:
  `181 passed`, Ruff pass, Black `7 files would be left unchanged`, strict
  `OK: inventory=106183 coverage=104655 strict=True`, and `git diff --check`
  pass; durable story status is `implemented` with unit proof `yes`.
