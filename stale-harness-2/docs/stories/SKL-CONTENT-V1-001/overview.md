# SKL-CONTENT-V1-001 Overview

## Current Behavior

SkillPort compiler emits deterministic JSON for 242 rows, but protobuf and manifest.v1-compatible dev artifacts are missing.

## Target Behavior

Compiler owns canonical `content.v1` SkillPort protobuf contract, Python binding, deterministic server/client `.pb` projections, manifest.v1-compatible hash-bound artifact set, and explicit non-production signing gate.

## Affected Users

- Content pipeline operator.
- Server/Unity adapter implementers.

## Affected Product Docs

- `harness/specs/jx-pc-mobile-port/contracts/content/v1/skill_catalog.proto`
- `harness/specs/jx-pc-mobile-port/contracts/content/manifest.v1.schema.json`
- `harness/specs/jx-pc-mobile-port/contracts/proto/game/v1/game.proto`

## Non-Goals

- Go runtime adapter.
- Unity consumer adapter.
- Production signing key.
- PARITY_DONE/GOLDEN_READY promotion.
