# Overview

## Current Behavior

`REQ-P0-002` is `in_progress`; `DOC-JX-01` remains blocked. The candidate
audit now enumerates reproducible facts for `yanwuchang` (IDs `209/210/211`),
`jingjichang` (`975`), and `shiliantang` (`925`): nine `maplist.ini` packages
with sizes/SHA-256, three byte-identical loose `.wor` copies per candidate,
and loose `Region_C.dat` presence (45/45/41, undecoded). It still has no winner,
no decoded Region_C collision golden, no Region_S spawn evidence (Region_S is
absent for all three), no terrain/minimap decode, and no selected source-file
provenance. No Unity map import and No byte vendoring remain in force.

## Target Behavior

The audit will contain a bounded record for all three candidates in mandated
queue order. Each record either has exact resolver/package/decode provenance or
states the missing evidence. A candidate may be selected only after its active
package/load-order winner and Region_C/Region_S source evidence are complete;
this story must otherwise remain fail-closed.

## Affected Users

- JX map owner producing candidate evidence.
- JX reviewer checking map identity, package winner, and decode facts.
- Technical reviewer enforcing the P0 gate.

## Affected Product Docs

- `specs/dhcd-jx-port/01-governance/traceability.md` (`REQ-P0-002`).
- `specs/dhcd-jx-port/02-product/mode-catalog.md` (candidate queue and gate).
- `specs/dhcd-jx-port/05-jx-parity/maps.md` (`DOC-JX-01`).
- `specs/dhcd-jx-port/05-jx-parity/arena-candidate-audit.md`.

## Non-Goals

- No Unity map import, collision test, scene, runtime port, or pilot enablement.
- No selected winner from textual IDs/hash/name evidence alone.
- No byte vendoring, copying, or writes under `/var/www/jx-source`.
- No legal-clearance or internal/public distribution claim.
