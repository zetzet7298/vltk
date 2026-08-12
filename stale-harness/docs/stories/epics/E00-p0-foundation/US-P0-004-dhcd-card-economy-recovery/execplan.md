# Exec Plan

## Goal

Recover exact DHCD card count, weight, cost, and cap, or preserve a complete
and reproducible failure boundary that prevents invented parity values.

## Scope

In scope:

- Read-only analysis of the hashed DHCD IL2CPP corpus.
- Controller/config declaration audit, exact serialized-candidate decoding,
  export failure audit, active-source discovery, and native mapping discovery.
- Evidence card, failed-method log, story packet, verifier, and reverse-ledger
  updates.
- Deterministic vectors only after exact semantics are corroborated.

Out of scope:

- Unity/client/server implementation or data defaults.
- JX resource lookup, asset selection, or writes under `/var/www/jx-pc`.
- Runtime instrumentation without an explicitly available authorized target.
- Product ADRs that replace missing reverse evidence.

## Risk Classification

Risk flags:

- Existing behavior: invented balance values would change gameplay semantics.
- External systems: evidence is stored in a separate read-only reverse corpus.
- Weak proof: generated IL/C# contains known exporter failures.
- Data/contracts: downstream deterministic deck and replay contracts depend on
  the result.

Hard gates:

- Input hashes must match the recorded corpus manifest.
- Generated declaration names and field offsets are not runtime proof.
- Packaged candidate values are not active-runtime proof without a selected
  logical path/index.
- All four requested semantics must be independently supported before closure.
- Unresolved native callees/config semantics keep `R-DHCD-001` `in_progress`.

## Work Phases

1. Admit `US-P0-004` under existing high-risk intake `#5` and link
   `US-P0-001 -> US-P0-004`.
2. Verify controller, config, recovery DLL, native binary, and metadata hashes.
3. Decode the exact `fp_` UnityFS TextAsset and validate header, schema, length,
   CRC, row count, and candidate row aggregates.
4. Record that `index_6` packages `fp_`, `index_9` packages non-`fp`, and active
   runtime selection is unresolved.
5. Generate focused ISIL, recover `DecodeAb -> FastXXTEA.Dexx`, resolve the
   embedded key literal, and validate its output as a Unity AssetBundle.
6. Record the static-key mismatch and failed-key runtime capture without
   treating either as active-source evidence.
7. Compare exact center `900` and battle `999` server binaries, including byte
   identity with the Android `fp_` TextAsset and role/version boundaries.
8. Audit declarations, reconstructed bodies, exporter logs, and Cpp2IL logs.
9. Record confirmed fields and failed techniques without deriving parity
   constants.
10. Map named ISIL method starts by unique pointer value through the BattleCore
    method-pointer table, then inspect the exact high-value ARM64 slices.
  11. Resolve both `RandomItem<T>` overloads through generic registration, lock the
      exact wrapper/selection body hashes, recover bounded weight/put-back flow,
      and map threshold calls through DodFixLib metadata/codegen slots without
      overclaiming `NextFP` endpoint/distribution semantics.
12. Validate the packet and retain `in_progress` while active config, exact RNG
    boundary, count, cost, cap, and runtime corroboration are absent.
  13. In a later iteration, recover the exact build key, resolve `NextFP`
      endpoint/distribution and referenced delegate/config bindings, add successful runtime
    corroboration and deterministic vectors, then run fresh completion proof.

## Stop Conditions

Pause and retain unresolved status if:

- A named method cannot be tied to one unique native pointer/RVA, or a required
  callee/indirect target cannot be resolved without guessing.
- Recovered code contains invalid IL, stack/type errors, default-object casts,
  or missing-method placeholders.
- A field establishes only schema shape rather than a value or formula.
- A packaged candidate cannot be tied to the runtime-selected logical path.
- A decoder output lacks valid Unity AssetBundle magic/parser acceptance, or the
  runtime capture fails encrypted AssetBundle loading.
- A proposed number comes from a name, UI assumption, product preference, or
  an unversioned/default configuration.
- Validation would require modifying the canonical reverse inputs, Unity, or
  `/var/www/jx-pc`.
