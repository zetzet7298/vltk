# Design

## Domain Model

`EvidenceRecord` is an auditable record, not proof that a runtime port is
complete. A PAK/hashed asset record is valid only when every required field is
present and a reviewer can reproduce the resolver/decode evidence. A record
with missing, conflicting, or guessed information is `unresolved` or
`provisional`; it cannot be marked `verified`.

Required schema for each selected PAK/hashed asset/config record:

| Field | Requirement |
| --- | --- |
| `evidence_id` | Stable record identifier. |
| `kind` | Asset/config kind, such as SPR, VFX, WAV, map, UI, or config. |
| `logical_path` | Resolved logical resource path. |
| `candidate_absolute_paths` | Complete enumeration of valid candidates. |
| `absolute_selected_path` | Absolute selected original path; empty until selection. |
| `pack_version` | Source pack/version for each candidate and selected winner. |
| `load_order_winner` | Active client package/load-order evidence; mtime only after equivalent version/load order. |
| `hash_uid` | Resolver-produced Hash_UID when applicable; never invented. |
| `encoding` | Resolver-reported path encoding. |
| `normalized_path_bytes_hex` | Normalized encoded path bytes from resolver evidence. |
| `byte_count` | Exact selected-file byte count. |
| `sha256` | SHA-256 of exact selected bytes. |
| `resolver_evidence` | Resolver command, output, tool version, and timestamp. |
| `decode_result` | Decode result; SPR includes action, direction, frame, anchor, and alpha checks. |
| `name_vi_cross_check` | `_labels.json:name_vi` comparison when labels exist. |
| `reviewer` | Named JX/technical reviewer who accepted the record. |
| `reviewed_at` | Review timestamp with timezone. |
| `status` | `unresolved`, `provisional`, or `verified` under the evidence-register rules. |
| `legal_status` | Explicit legal state and, for internal evidence use, scope/owner/expiry approval reference. |
| `usage_references` | Later Unity/config references, only after an asset is actually used. |

Non-PAK evidence uses its applicable provenance form: absolute path, byte count,
SHA-256, confidence, owner, reviewer, timestamp, claim, and limitation. It must
not fabricate PAK-only UID, encoding, path-byte, label, or decode fields.

## Application Flow

1. Enumerate all valid candidates in the canonical PC source/runtime tree.
2. Use `/home/zet/Projects/vltktool` to resolve logical path, UID, encoding,
   and normalized path bytes.
3. Determine patch/version and active package load-order winner.
4. Decode and cross-check labels where applicable.
5. Record exact bytes/hash and submit the complete record to the named reviewer.
6. Keep the result unresolved/provisional or stop when evidence does not support
   a winner; only a complete reviewed record can become verified.

`/var/www/jx-source` is read-only. Exact bytes may be vendored only after a
winner is selected and the asset is actually used by an approved later story;
this story neither selects nor vendors bytes.

## Interface Contract

This story adds no API, UI, runtime command, or external contract. Its interface
is the documented evidence schema and review gate. It is tied to `REQ-P0-001`
and supports `OBJ-P0-02` provenance and `OBJ-P0-04` internal-only pilot
operations without satisfying their downstream implementation or release gates.

## Data Model

No database schema, migration, retention rule, or game persistence change is
introduced. Harness registration stores only the story metadata; asset evidence
remains a future manifest/document artifact.

## UI / Platform Impact

None. This story must not modify Unity scenes, assets, import settings, mobile
behavior, server behavior, or distribution channels.

## Observability

Each future record must retain resolver/decode command and output, tool version,
reviewer, and timestamp. Review output must identify missing fields, candidate
conflicts, absent decode/label checks, and legal-state failures. This story does
not create runtime telemetry or a verifier.

## Alternatives Considered

1. Select an apparent asset from Unity or a single PAK path: rejected because
   current Unity content and file existence do not prove JX parity or active
   load-order selection.
2. Vendor candidate bytes as documentation evidence: rejected because the
   source policy permits exact-byte vendoring only after selection and actual
   use.
3. Treat an internal evidence record as public clearance: rejected because
   `B-LEGAL-001` remains a separate blocker and public distribution is
   prohibited.
