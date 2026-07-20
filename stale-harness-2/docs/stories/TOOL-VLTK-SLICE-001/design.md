# Design

## Domain Model

A source table contains one raw header line and raw tab-separated rows. A slice
contains the same header plus exactly the requested unique rows in source order.

## Application Flow

The CLI reads bytes, resolves an ASCII header column, validates requested IDs,
composes exact raw lines, computes hashes, and either writes the slice/manifest
with rollback on pair failure or compares them in `--check` mode.

## Interface Contract

Required arguments: input table, ASCII key column, comma-separated IDs, output
slice and manifest. Missing/duplicate requested IDs, duplicate source rows for a
requested ID, malformed rows, output=input, or drift in check mode fail nonzero.
Duplicate keys outside the reviewed request do not invalidate the bounded slice.
Output/manifest ancestor collisions and final-component symlinks are rejected.

## Data Model

The deterministic JSON manifest records schema, source path/hash/size, slice
hash/size, key column, requested/selected IDs, source line numbers and
`encoding: byte-preserving`; it contains no timestamp.

## UI / Platform Impact

Python stdlib only; binary-safe on Linux/macOS/Windows-supported paths.

## Observability

Exit code and manifest are the audit surface. No source content is decoded.

## Alternatives Considered

Manual hashing/slicing and text decoding are rejected because they violate the
canonical PC evidence rules.
