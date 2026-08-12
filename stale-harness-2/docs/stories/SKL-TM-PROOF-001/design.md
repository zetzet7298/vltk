# Design

## Domain Model

The proof domain separates three sets: PC learned skills, Unity panel roots, and
child/support/event skills. Their reviewed classification becomes the input to
canonical static rows, relationships, and a hash-pinned oracle artifact.

## Application Flow

PC membership evidence comes from active `skills_table.lua` TangMen groups plus
server `skillbook.lua` grants for levels 90/120/150; `skills.txt` and TangMen Lua
provide static fields and relationships. Reviewed exact slices are copied
byte-preserving, transformed by a deterministic stdlib generator, and compared
with the production catalog.

The reviewed static slice is `Assets/StreamingAssets/Reference/PcTangMenSkills.txt`;
its vltktool provenance manifest pins source and slice hashes without decoding.

## Interface Contract

The verifier consumes the reviewed `membership-classification.json`, asserts
the source-layer union and unresolved set, then checks populated static
fields, child edges and `58 -> 227`. PC evidence proves learned membership but
does not by itself prove UI order; order remains an explicit contract. Absent
source cells remain explicit and are never silently `0`.

## Data Model

Artifacts carry source/oracle SHA-256 and provenance. Encoded PAK/SPR/DAT or
hash evidence must be resolved only with `~/Projects/vltktool`.

## UI / Platform Impact

No runtime/UI/platform behavior is changed by this proof story.

## Observability

Harness validation records generator check, artifact hashes, verifier result and
independent review; no runtime parity claim is emitted.

## Alternatives Considered

Implementation-derived expectations and a generic all-faction framework are
rejected as circular or too broad for this wave.
