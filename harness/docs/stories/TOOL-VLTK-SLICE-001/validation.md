# Validation

## Proof Strategy

Tests construct byte tables containing non-UTF-8 data and mixed newline endings,
then prove output lines equal exact source spans and hashes match raw bytes.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | exact bytes, source order, hashes, line provenance, malformed/duplicate/missing requested IDs, unrelated duplicate tolerance |
| Integration | CLI write then `--check`; output/manifest drift fails; pair-write failure rolls back |
| E2E | TangMen slice deferred to dependent story |
| Platform | stdlib/path behavior; no text-mode source reads |
| Performance | bounded single-file scan |
| Logs/Audit | deterministic manifest without timestamp |

## Fixtures

Temporary raw tables only; no vendored candidate evidence.

## Commands

```text
python3 -m pytest /home/zet/Projects/vltktool/tests/test_extract_table_slice.py
python3 -m py_compile /home/zet/Projects/vltktool/extract_table_slice.py
```

## Acceptance Evidence

Record focused tests, independent review and a TangMen `--check` result before
using the tool output as canonical oracle evidence.
