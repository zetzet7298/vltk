# SKL-CONTENT-V1-001 Design

## Domain Model

`content.v1.SkillCatalog` mirrors evidence already in compiler IR: 242 union rows, faction progression, exposure/blocker states, integer static fields, typed lifecycle relations, presentation fields, asset dependencies, runtime policy, and reproducibility metadata.

## Interface Contract

- `SkillCatalog`: full compiler-owned canonical catalog.
- `ServerSkillCatalog`: server projection, static non-presentation fields and lifecycle refs.
- `ClientSkillCatalog`: Unity projection, faction/presentation/asset dependency fields.
- `game.v1.ContentDigest.client_projection_sha256` uses new tag 7 for exact client projection negotiation.

## Data Model

No DB migration. Generated manifest is manifest.v1 JSON with artifact size/SHA-256 entries, `contentDigest`, `runtimeSkillPolicy`, `manifestSha256`, and Ed25519 signature.

## UI / Platform Impact

Unity adapter must consume `skill_port.client.pb` later. Current lane only writes StreamingAssets artifacts.

## Observability

`skill_port.release_gate.json` records test-only signing status. Spec validator release mode rejects the test-only key.

## Alternatives Considered

1. JSON-only manifest hashes. Rejected: compiler-contract gap stays open.
2. Production-like key. Rejected: no production secret allowed.
