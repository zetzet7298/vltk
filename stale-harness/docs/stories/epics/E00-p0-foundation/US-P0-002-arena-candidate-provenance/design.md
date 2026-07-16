# Design

## Domain Model

`ArenaCandidateEvidence` represents one named candidate, every valid source
candidate discovered for it, and a fail-closed selection state. Names and
numeric IDs are discovery inputs only. A `selected` state is prohibited until
the map's logical path, exact source bytes, package/version, active load-order
winner, resolver path evidence, Region_C decode, and Region_S decode are all
recorded and reviewed.

Required fields per discovered source candidate:

| Field | Requirement |
| --- | --- |
| `candidate_name` | `yanwuchang`, `jingjichang`, or `shiliantang`. |
| `candidate_map_ids` | Discovery IDs: `209/210/211`, `975`, or `925`; not identity proof. |
| `logical_map_path` | Resolver/source-proven map path or `unresolved`. |
| `absolute_candidate_paths` | Every matching absolute canonical path. |
| `pack_version` | Source package/version evidence. |
| `load_order_winner` | Active client winner evidence; mtime only after equivalent package/load order. |
| `hash_uid` | vltktool-produced UID when applicable. |
| `encoding` | Resolver-reported path encoding. |
| `normalized_path_bytes_hex` | Resolver-reported normalized encoded path bytes. |
| `byte_count` | Exact file byte count. |
| `sha256` | Exact file SHA-256. |
| `resolver_command_and_version` | Exact resolver command/output and vltktool version. |
| `name_vi_cross_check` | `_labels.json:name_vi` result when available. |
| `region_c_decode` | Collision/height/bounds decode result or `unresolved`. |
| `region_s_decode` | Spawn/NPC placement decode result or `unresolved`. |
| `terrain_decode` | Terrain/scene/tileset result or `unresolved`. |
| `minimap_decode` | Minimap source/transform result or `unresolved`. |
| `reviewer` / `reviewed_at` | Named reviewer and timestamp before verified/selected status. |
| `status` | `unresolved`, `provisional`, or `verified`; no selection implies no pilot arena. |

## Application Flow

1. Start with the required queue: `yanwuchang`, then `jingjichang`, then
   `shiliantang`.
2. Resolve logical/resource candidates with vltktool; enumerate every matching
   canonical source/runtime path.
3. Establish package version and active load-order winner before interpreting
   map-region data.
4. Record exact byte hashes, UID/encoding/path bytes, and labels.
5. Decode Region_C/Region_S/terrain/minimap only for a source candidate actually
   tied to the logical map record.
6. Keep all unavailable/conflicting data as `unresolved`; do not substitute
   loose-script or textual evidence.

## Interface Contract

This story creates no runtime interface. Its output is the append-only
fact/unresolved record in `arena-candidate-audit.md`, consumed by later map
conversion and Unity validation stories.

## Data Model

No database, migration, save, or game data changes are introduced. The Harness
dependency is `US-P0-001 -> US-P0-002`; no downstream edges are added.

## UI / Platform Impact

None. The expected later portrait map/collision/minimap test is explicitly out
of scope until a map winner exists.

## Observability

Every resolved value must include its command/tool version/source path. Every
unresolved value must state whether the missing proof is logical mapping,
package/load-order, source bytes, or decode evidence.

## Alternatives Considered

1. Choose a map from `minimap.html` or a dungeon-script name: rejected because
   textual/script evidence does not prove map identity, winner, or collision.
2. Choose the most recent file: rejected because mtime is not package/load-order
   evidence.
3. Decode a similarly named map: rejected because it is not evidence for the
   named candidate.
