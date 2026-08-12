# US-RUN-PROTO-CODEC Validation

## Proof Strategy

Regeneration must produce no diff and binary round-trips must preserve envelope
fields while rejecting malformed length/multiple-message payloads.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Client/server envelope round-trip; truncated/oversized/multiple frame. |
| Integration | Not applicable until WSS child. |
| E2E | Not applicable. |
| Platform | Codegen works from declared dev environment. |
| Performance | Frame-size bound only. |
| Logs/Audit | No secret payload logging. |

## Fixtures

- Canonical `game.proto` and deterministic sample envelopes.

## Commands

```text
cd /var/www/vltk-mobile/backend
pytest -q tests/unit/modules/runtime/test_game_v1_codec.py
python scripts/generate_game_v1_proto.py --check
```

## Acceptance Evidence

- Herdr run `orch-bd4f1a22adde9aa2` finished verified with 11 collected
  attempts, 11 clean boundary reports, zero unowned changes and zero warnings.
  `RESULT.json` SHA-256:
  `7ff5a1e44d4cd8b0e634c4b56537abdbb09ffa451e4df77912bf8fe78a607452`.
- Canonical source is
  `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`, SHA-256
  `63e2337cf743e4c9935e5d68b1eeed29d702b36a30cb4ec426243b4a42172b42`;
  generator policy pins `libprotoc 25.1` and `protobuf==6.33.6`.
- Repo-local generated artifacts are `game_pb2.py`, `game_pb2.pyi` and a
  deterministic manifest. `--check` regenerates in a temporary directory and
  detects missing/stale/owned-extra artifacts without mutating the checked
  output; default cleanup preserves manual suffix-matching files without the
  protoc generated marker.
- Strict codec proof covers all four client and six server payload variants,
  canonical varint framing, empty/zero/truncated/non-minimal/overflow/oversize,
  trailing/concatenated, invalid wire, wrong concrete direction, multiple or
  missing payloads, recursive unknown fields and secret-safe finite errors.
- Fresh `story complete` verification collected `32 passed`; generation drift
  check and Ruff passed, Black checked 6 non-generated files, and
  `git diff --check` passed.
- Final independent proof accepted a bounded repo-local codec delivery only.
  Git staging/commit remains a release residual; WSS/close behavior, session,
  DB/UoW, checkpoint/outbox, Unity and full PC framing compatibility remain
  outside this story. Tags 10-13 overlap and merged protobuf-valid bodies are
  documented wire-level limitations, not production compatibility proof.
