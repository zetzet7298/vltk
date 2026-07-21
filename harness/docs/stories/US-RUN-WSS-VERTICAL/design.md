# US-RUN-WSS-VERTICAL Design

## Domain Model

Uses runtime core without framework imports.

## Application Flow

Binary boundary → app-scoped endpoint → opaque ticket/epoch fence → durable
outcome preflight → admission/input ordering → authoritative tick/combat port →
durable UoW → result/resources snapshot.

## Interface Contract

Exactly canonical `game.v1`; no JSON fallback or client-authored
vitals/status/clock. This bounded vertical advertises and enforces one move per
batch, UUID command IDs and canonical axis/facing bounds.

## Data Model

Uses completed identity and PostgreSQL checkpoint/idempotency/outbox adapters.
The idempotency key is stable for realm/character/epoch/client sequence; the
raw-frame hash distinguishes altered retries before runtime mutation.

## UI / Platform Impact

Requires a separate Unity adapter/canary, ticket issuer, production combat
composition and distributed fencing child before production cutover.

## Observability

Metrics, structured logs and capacity benchmarks remain an explicit operations
follow-up; this story does not claim those production rollout gates.

## Alternatives Considered

1. Request-scoped combat REST: rejected by decision 0008.
