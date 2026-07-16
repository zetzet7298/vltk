# Design

## Domain Model

`ModalQueueEvidence` records role lookup, pending-event branches, response state, and lock scope. Labels from missing callee bodies remain reconstruction.

## Application Flow

Trace request, pending branch, resend/dequeue, trigger, selection response, and modal show/close paths with exact native addresses.

## Interface Contract

Evidence-only; no client input API or event schema changes.

## Data Model

No persistence or configuration change.

## UI / Platform Impact

Android ARM64 controller and reconstructed client UI are evidence surfaces only.

## Observability

Record per-role register setup, native method slots, artifact hashes, runtime timestamps, and explicit non-claims.

## Alternatives Considered

1. Promote `Queue` type/name to FIFO parity: rejected without method bodies.
2. Infer input lock from modal names: rejected without hash-checked caller mapping.
