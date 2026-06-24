# HUD Port Recon — vltkunity → vltk-mobile

**Task:** Port toàn bộ HUD của game `/var/www/vltk-mobile/vltkunity` sang `vltk-mobile`,
đảm bảo 100% UI, UX, các panel khi ấn các nút phải đầy đủ sau khi port.

**Acceptance level:** reviewed

---

## ⚠️ Blocker: Insufficient tool access for read-only recon

This Source Inspector role requires read-only inspection of source files
(PC source `/var/www/jx-source/...`, the `vltkunity` Unity project, and
`Assets/StreamingAssets/Reference/`). Hard constraints forbid editing files
and forbid guessing/inventing any data point.

In this delegated session the **only** tools available to the worker are
`edit` and `write`. There is **no read tool, no shell/bash tool, no
semantic_grep / semantic_inspect, no hypa_grep / hypa_read** available in
the function set. As a result, the worker cannot:

- list or read files under `/var/www/vltk-mobile/vltkunity`,
- list or read PC source under
  `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem`,
- inspect Unity reference files in `Assets/StreamingAssets/Reference/`,
- diff the current `vltk-mobile` HUD implementation against the source.

Producing a faithful recon without read access would require **guessing**
file paths, asset names, panel IDs, and UI structures — which directly
violates the hard constraints ("Do NOT guess or invent values. Every data
point must come from the original source."). Therefore no fabricated
data/logic is reported here.

## What was searched / attempted

- Attempted filesystem inspection of `/var/www/vltk-mobile/vltkunity`
  (source Unity project for HUD port) → **could not read** (no read/shell
  tool in function set).
- Attempted inspection of PC source
  `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem`
  for HUD/UI/panel logic → **could not read** (no read/shell tool).
- Attempted inspection of
  `/var/www/vltk-mobile/Assets/StreamingAssets/Reference/` (Skills.txt,
  NpcS.txt, etc.) → **could not read** (no read/shell tool).

No files were opened; no values were extracted.

## Files inspected
None (read tooling unavailable in this delegated session).

## Key data/logic extracted
None — cannot extract without read access; refused to fabricate per
hard constraints.

## Chinese to Vietnamese translations needed
Unknown — cannot inspect source text without read access.

## Current Unity gaps
Unknown — cannot compare current `vltk-mobile` HUD against `vltkunity`
source without read access.

## Port scope
The intended scope (per task): port the **complete HUD** from
`vltkunity` into `vltk-mobile`, including 100% of UI elements, UX flows,
and all panels triggered by button presses (e.g. character, inventory,
skill, map/minimap, chat, hotbar, HP/MP/EXP bars, settings, etc.).
Exact panel list, asset paths, and button→panel mappings **could not be
enumerated** without read access and must be determined by a worker that
has read/shell/semantic_* tools equipped.

## Recommendation to parent orchestrator

Re-delegate this Source Inspector task to a worker session that has the
following tools equipped (per project AGENTS.md tooling preference):

- `semantic_query`, `semantic_grep`, `semantic_inspect`, `semantic_show`
  (pi-srcwalk), and/or `hypa_grep` / `hypa_read` (pi-hypa), and a
  shell/bash + read tool for raw asset/text inspection.
- Unity MCP tools (if HUD prefabs/scenes need structural inspection).

Suggested recon plan for that worker (concrete entry points to try):

1. Map `vltkunity` HUD source:
   - `find /var/www/vltk-mobile/vltkunity/Assets -iname '*HUD*'`
   - grep for `Canvas`, `Panel`, UI controller scripts (`*UIController*`,
     `*HUD*`, `*Panel*`, `*Window*`).
   - Enumerate every button and its onClick → target panel/open method.
2. Cross-reference PC UI source for original panel behavior:
   - Ui3 SPR art, panel layouts (jx-hud-port skill).
   - PC source dir above for any UI/UI3 lua/script and asset paths.
3. Diff against current `vltk-mobile` HUD implementation
   (`Assets/.../HUD`, `UI`, `Canvas` scripts) to find missing panels and
   broken button wiring.
4. Localize any Chinese UI strings to Vietnamese (preserve source mapping).

## Acceptance report

This recon did not complete because the delegated worker lacks read
tooling. No source data can be truthfully reported without violating the
"no guessing" hard constraint. See acceptance-report JSON below.

```acceptance-report
{
  "criteriaSatisfied": [
    {
      "id": "criterion-1",
      "status": "not-satisfied",
      "evidence": "Could not implement/inspect: worker session only has edit+write tools; no read/shell/semantic_* tools available. Refused to fabricate source data per hard constraints."
    },
    {
      "id": "criterion-2",
      "status": "not-satisfied",
      "evidence": "No source files could be opened, so no verifiable evidence could be produced for an independent review."
    }
  ],
  "changedFiles": [
    "/var/www/vltk-mobile/harness/intake/source-recon.md"
  ],
  "testsAddedOrUpdated": [],
  "commandsRun": [
    {
      "command": "(write) create intake/source-recon.md",
      "result": "passed",
      "summary": "Created placeholder recon doc; could not perform actual recon due to missing read tooling."
    }
  ],
  "validationOutput": [],
  "residualRisks": [
    "Worker lacks read/shell/semantic tools — cannot perform read-only source inspection at all in this session.",
    "No fabricated data added, but recon is effectively empty and the port task cannot proceed from this worker.",
    "Task must be re-delegated with proper read tooling (semantic_*/hypa_*/shell/read + Unity MCP)."
  ],
  "noStagedFiles": true,
  "notes": "Only this recon markdown file was written. The original intake/source-recon.md was a pre-existing placeholder and was overwritten with this honest blocker report. No source files were edited. Recommend re-delegation with read tooling."
}
```
