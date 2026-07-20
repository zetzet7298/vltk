# Design — Commit and push all working-tree changes

## Domain Model

No product domain changes. This is source-control state management.

## Application Flow

`status → inspect → stage -A → cached review → commit → fast-forward push → verify SHA`.

## Data Model

No runtime database changes. Ignored Harness SQLite state stays local.

## UI / Platform Impact

No runtime/UI impact.

## Observability

Record intake, story proof, commit SHA, push result, and final clean-tree state in Harness trace.

## Alternatives Considered

1. Commit only Harness files: rejected because user explicitly requested all changes.
2. Force push: rejected; remote history must remain protected.
3. Commit ignored runtime state: rejected by project ignore policy.
