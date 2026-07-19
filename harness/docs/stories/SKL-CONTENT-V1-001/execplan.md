# SKL-CONTENT-V1-001 Exec Plan

## Goal

Close compiler-contract gap for SkillPort content artifacts.

## Scope

In scope:

- `harness/` contract/spec/validator/story records.
- `scripts/skill_port/` compiler and generated Python binding.
- `scripts/skill_port_tests/` focused tests.
- `Assets/StreamingAssets/Generated/SkillPort/` generated artifacts.

Out of scope:

- Go consumer implementation.
- Unity consumer implementation.
- Production key material.

## Risk Classification

Risk flags:

- Public contracts.
- Existing behavior.
- Weak proof.
- Audit/security.
- Cross-platform.

Hard gates:

- Signing/release gate must fail closed for test-only key in production.

## Work Phases

1. Add additive `content.v1` protobuf and generated Python binding.
2. Emit deterministic protobuf projections beside existing JSON.
3. Replace internal manifest with manifest.v1-compatible dev artifact.
4. Add schema/protoc/release-gate tests and validator checks.
5. Regenerate artifacts and verify double-run determinism.

## Stop Conditions

Pause if production signing is requested or runtime evidence is required to unblock GOLDEN_READY.
