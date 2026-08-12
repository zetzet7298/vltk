# US-RUN-PROTO-CODEC Design

## Domain Model

No domain types depend on generated messages.

## Application Flow

Boundary adapter decodes one length-delimited `ClientEnvelope` and encodes one
`ServerEnvelope`; conversion to application commands belongs outside generated code.

## Interface Contract

Canonical proto bytes and field numbers remain unchanged. Generated artifacts are
checked for drift from the canonical source.

## Data Model

No tables or migrations.

## UI / Platform Impact

Adds Python runtime/codegen dependencies and a reproducible generation script.

## Observability

Malformed length, multiple envelopes and unknown payload types return bounded
protocol errors without logging tickets or payload secrets.

## Alternatives Considered

1. Hand-written codec: rejected by decision 0008.
