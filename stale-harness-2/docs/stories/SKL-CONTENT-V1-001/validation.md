# SKL-CONTENT-V1-001 Validation

## Proof Strategy

Smallest proof: protoc compiles contract, Python compiler emits deterministic JSON/PB/manifest, schema accepts dev manifest, release validator rejects test key, generated files match across two writes.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | `scripts.skill_port_tests.test_compiler` counts, protobuf shape, manifest schema/signing gate, negative IR/hash/source cases |
| Integration | `python3 -m scripts.skill_port --check` generated set current |
| Contract | `protoc 25.1` descriptor for `content/v1/skill_catalog.proto`; harness spec validator premerge |
| Release gate | `harness/scripts/spec-validator/validate.py --mode release` rejects `test-only-skill-port-ed25519-fixture-v1` |

## Fixtures

- Test-only deterministic Ed25519 key id: `test-only-skill-port-ed25519-fixture-v1`.
- Fixed build timestamp: `1970-01-01T00:00:00Z`.

## Commands

```text
protoc --proto_path=harness/specs/jx-pc-mobile-port/contracts --python_out=scripts/skill_port/gen harness/specs/jx-pc-mobile-port/contracts/content/v1/skill_catalog.proto
python3 -m unittest scripts.skill_port_tests.test_compiler
python3 -m scripts.skill_port --check
cd harness && python3 scripts/spec-validator/validate.py --mode premerge
cd harness && python3 scripts/spec-validator/validate.py --mode release
```

## Acceptance Evidence

- Unit: 11 tests PASS.
- Compiler check: PASS, rows=242 fields=9196 missiles=513 states=49 golden_ready=0.
- Spec validator premerge: PASS.
- Release validator: expected FAIL on development signing key.
- Double-run generated file SHA-256 list identical across two writes.
