# Design

## Domain Model

`PauseEvidence` records native setter/caller identity, player-local versus global scope, timer state, and confidence. Missing evidence remains `unresolved`.

## Application Flow

Trace card show, selection, close, and restore paths; separately trace every `BattleSys.set_IsPause` caller and timer mutation. Compare only hash-matched native/ISIL artifacts.

## Interface Contract

Evidence-only story. It creates no runtime API and publishes no pause default.

## Data Model

No game database or configuration changes.

## UI / Platform Impact

Android IL2CPP and card UI/native boundaries only; no Unity or JX asset edits.

## Observability

Record input hashes, method starts, call-site registers, runtime trace identity, and explicit negative claims.

## Alternatives Considered

1. Infer pause from `Time.timeScale` text: rejected as malformed reconstruction evidence.
2. Treat a role-keyed pending path as global pause: rejected; scope is not proven.
