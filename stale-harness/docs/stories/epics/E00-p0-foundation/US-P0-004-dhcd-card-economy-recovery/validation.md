# Validation

## Proof Strategy

`scripts/verify-us-p0-004.sh` proves the current fail-closed evidence packet:
the required files exist, authoritative hashes still match, the native corpus
manifest verifies, the serialized `fp_` candidate passes header/schema/CRC
checks, and every remaining semantic is explicitly bounded. It does not prove
active runtime selection or approve gameplay constants. It also verifies the
recovered native decoder evidence and requires the embedded-key probe to fail
with the recorded invalid UnityFS magic. It also verifies fail-closed iOS
DODAB1 envelope facts and unique named-ISIL-to-native-pointer mappings. Catalog
`md5_ex` remains metadata and is not treated as a plaintext oracle. The selector
inspector also locks both generic method identities, the 1,540-byte
`RandomItem<T>` pre-`RET` body, decoded BL targets, and exact shift/comparison
 helper bodies while explicitly rejecting unproven `NextFP` endpoint/distribution
 semantics.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Verify story/evidence/log anchors, exact hashes, Android/server/iOS candidate inspectors, subset comparison, native slot mapping, selector body/call/helper invariants, and deterministic static-key failure. |
| Integration | Verify native manifest, ISIL decoder path and method starts, generic registration joins, Android/iOS catalog membership, server role copies, failed runtime capture, and Harness story/dependency registration. |
| E2E | Pending native callee/config resolution, successful runtime corroboration, and deterministic vectors. |
| Platform | Not applicable: no runtime or Unity platform change. |
| Performance | Not applicable. |
| Logs/Audit | Preserve Cpp2IL pointer-table evidence and ILSpy failure output. |

## Negative Validation

- Serialized candidate-row values may be recorded only as candidate evidence with
  their exact bundle, version, and hash caveats; fail if any is presented as an
  active runtime count, offer count, weight, cost, cap, or algorithm.
- Fail if `CanRepeatSelect` is treated as a maximum-copy value.
- Fail if header row count `2023` is treated as card/offer/deck count.
- Fail if malformed generated C# is described as original source or exact
  behavior.
- Fail if the embedded key's mismatching output or failed runtime capture is
  described as successful non-`fp` decoding or active-source proof.
- Fail if `md5_ex`, server role names, or 1,879/2,023 serialized row counts are
  treated as a plaintext oracle, active winner, or offer/card count.
- Fail if the story or reverse queue is marked complete while native selection
  callees, active-source proof, and deterministic vectors are absent.

## Fixtures

No binary is copied into the Harness repository. The verifier reads the exact
local corpus paths recorded in the evidence card and manifest.

## Commands

```text
scripts/verify-us-p0-004.sh
scripts/bin/harness-cli story verify US-P0-004
scripts/bin/harness-cli story verify-all
scripts/bin/harness-cli audit
git diff --check
```

## Acceptance Evidence

- Controller/config/recovery DLL hashes match the recorded values.
- All native input hashes pass `/home/zet/Projects/dhcd/input/manifest.sha256`.
- The exact Android `1.304` `fp_` candidate decodes into 2,023 validated rows;
  its outer and TextAsset hashes, schema, length, and CRC match.
- `index_6` packages `fp_` and `index_9` packages non-`fp`; runtime selection
  remains unresolved.
- Hashed ISIL proves `DecodeAb -> FastXXTEA.Dexx`; the non-`fp` filename is in
  `enc_list.bytes`, but the embedded key produces non-Unity prefix
  `171ae9de974ea48d` and MD5 `c70b77ae6f6102036edec93aaeb740d3`.
- Server role binaries prove 1,879 shared rows and 144 `fp_`-only pool-`999`
  rows; battle `999` `ResBin_fp` is byte-identical to Android `fp_`. Active
  Android selection remains unresolved.
- iOS `1.351` catalogs package both logical names in DODAB1 wrappers whose
  visible header sizes match catalog sizes; their opaque fields, remaining 48
  bytes, plaintext, decoder semantics, and active selection remain unresolved.
- The available Android log records successful key bootstrap followed by
  encrypted AssetBundle decompression failures, so it is rejected as
  active-source evidence.
- The native inspector uniquely maps all 26 named `LevelRandomSkillCtrl` ISIL
  starts to slots `4182`-`4207` and two config getters to `2587`-`2588`.
  The selector inspector resolves wrapper/selection method definitions
  `24133`/`24134`, locks range `[0x015429fc,0x01543000)` and its SHA-256, and
 proves cumulative weights plus predicate-controlled replacement, and maps the
 threshold path to `FP.op_Implicit(Int32)` plus `TSRandom.Next(FP, FP)`. Its
 `NextFP` endpoint/distribution, active config binding, and the complete
 count/cost/cap rule remain unresolved.
- Exact values recovered: serialized candidate rows only, not active semantics.
- `US-P0-004` and `R-DHCD-001` remain `in_progress` pending active-source proof,
 `NextFP` distribution/config resolution, corroboration, deterministic vectors, and reviewer
  evidence.
