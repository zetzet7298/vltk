# HARNESS-UNITY-REFACTOR-RULES-001 Unity Refactor Guardrails

## Status

implemented

## Lane

normal

## Product Contract

Every current and future agent working on the Unity project follows durable
assembly-boundary, compile-loop, and validation guardrails before and during the
planned refactor.

## Relevant Product Docs

- `AGENTS.md`
- `docs/decisions/0008-unity-refactor-fast-iteration-guardrails.md`

## Acceptance Criteria

- `AGENTS.md` makes the Unity refactor rules explicitly apply to agents and
  subagents across future sessions.
- Rules prevent new monolithic Sandbox growth, circular assembly dependencies,
  unsafe script moves, and mixed behavior/boundary refactors by default.
- Rules define the focused EditMode, full compilation, Console, and engine-boundary
  proof expected for Unity changes.
- Rules distinguish hot reload as an inner-loop aid from completion proof and
  prohibit unrequested global Editor/package changes.
- Rules require before/after evidence for compile-performance claims and forbid
  weakening tests for speed.

## Design Notes

- Domain rules: dependency direction must be established from code evidence,
  with inner assemblies independent of UI, Editor, tests, and scene orchestration.
- UI surfaces: none.
- Durable policy entrypoint: `AGENTS.md`.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Mechanical presence check for the durable rule section and core guardrails. |
| Integration | Review rule consistency with Harness authority and Unity MCP workflow. |
| E2E | Not applicable; this change does not alter game behavior. |
| Platform | Not applicable; Markdown policy only. |
| Release | `srcwalk review` plus exact-file inspection and Harness trace. |

## Harness Delta

Adds durable Unity refactor and fast-iteration instructions to the bounded agent
authority entrypoint.

## Evidence

- Intake `#15` classified this Harness improvement as normal lane.
- `srcwalk review --scope . --limit 10` inspected the tracked `AGENTS.md` diff.
- `harness-cli story verify HARNESS-UNITY-REFACTOR-RULES-001` passed.
- Detailed Harness trace `#47` achieved tier 3/3.
