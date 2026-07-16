# Design

## Domain Model

`CardEconomyEvidence` is a bounded reverse claim with input provenance,
reproducible commands, confirmed facts, failed methods, confidence, and a
follow-up gate. A field name or method signature is navigation evidence only.

| Requested semantic | Confirmed evidence | Current result |
| --- | --- | --- |
| Card/offer count | Native slices prove `randomCount`/`randomNum` are caller inputs and track a remaining-count loop. | `unresolved`: the source and exact value of those arguments are not mapped. |
| Selection weight | A hashed Android `1.304` `fp_` TextAsset contains 2,023 schema-valid rows. Native `RandomItem<T>` accumulates delegate weights, calls `FP.op_Implicit(0)` then `TSRandom.Next(FP zero, FP cumulative-total)`, compares the returned threshold against cumulative values, and branches between put-back and zero/subtract updates. | `high-confidence reconstruction`: weighted selection with per-item replacement and its threshold-call identity are recovered, but active source/field binding and `NextFP` endpoint/distribution semantics remain unresolved. |
| Cost/reroll price | Shop/reroll method signatures exist. | `unresolved`: inspected declarations expose no authoritative cost value or formula. |
| Copy/cap | `CanRepeatSelect=1` in every decoded `fp_` candidate row. | `unresolved`: a repeat flag does not prove maximum copies or cap behavior. |

The four requested semantics form one completion gate. Partial declaration
evidence cannot mark this story implemented.

## Application Flow

1. Verify the SHA-256 and byte count of native inputs, metadata, recovery DLL,
   and reconstructed controller/config files.
2. Decode packaged serialized candidates with exact bundle/TextAsset hashes,
   schema length, version, and CRC validation.
3. Prove which `fp_` or non-`fp` logical path the runtime actually selects.
4. Record generated declarations and malformed-body limitations separately.
5. Uniquely match named ISIL starts to the 5,082 pointer entries starting at
   native table `0x273D998`; do not infer slots from adjacency.
6. Disassemble only the matched ARM64 methods from the hashed `libil2cpp.so`,
    resolve generic registration, exact body hashes, selection callees/config
    references, and cross-check call sites.
7. Add deterministic vectors only when two independent evidence forms agree.
8. Keep every unsupported semantic `unresolved` and leave the story active.

## Interface Contract

This story creates no runtime API. Its outputs are the evidence card, failed
method log, story packet, and verifier. Downstream deck/gameplay work may read
confirmed facts but must reject unresolved values.

## Data Model

No game database or configuration is changed. Harness stores the story status,
proof command, dependency, evidence summary, and detailed trace. The dependency
is `US-P0-001 -> US-P0-004` because reverse claims must retain the provenance
gate.

## UI / Platform Impact

None. Unity, mobile UI, server, and JX PC assets are outside this evidence-only
slice.

## Observability

The evidence card records absolute paths, catalog membership, byte counts,
SHA-256 values, TextAsset schema/header/CRC, iOS envelope fields, tool versions,
commands, native registration/table locations, unique named-method slot matches,
exact helper/body bytes, decoded BL targets, confidence, and unresolved callees. The failed-
method log records every attempted technique that cannot support a semantic
claim.

## Alternatives Considered

1. Treat decompiled constants and pseudocode as exact C#: rejected because the
   exporters report invalid IL, missing methods, bad casts, and stack errors.
2. Treat `CanRepeatSelect` as a cap: rejected because a repeat flag does not
   encode maximum copies.
3. Infer cost from UI or method names: rejected because names are navigation
   evidence, not numeric behavior.
4. Publish product defaults while reverse is incomplete: rejected because that
   would violate the fail-closed parity contract.
