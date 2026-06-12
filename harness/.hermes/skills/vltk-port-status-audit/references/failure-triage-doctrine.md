# Failure triage doctrine — after un-gating, before fixing

This is the phase **after** the five audit checks: the test suite has been un-gated (the
`VLTK_ENABLE_TESTS` symbol got defined), it compiles, and now it's RED with N real failures. Your
job is to drive N down **without faking green**. This file is the playbook for that sweep.

## The core classification: TEST-DRIFT vs PROD-GAP

Read every failure against (a) the real production method and (b) the actual shipped PC reference
file / PC source ground truth. Then bucket it:

- **TEST-DRIFT** — the test's expectation is wrong vs PC ground truth (test bit-rotted after the
  production API moved, or hardcodes a phantom value the data never had). **Fix the test** to match
  reality. Keep the assertion's intent; never weaken it. If the production API the test wants
  genuinely doesn't exist, `Assert.Ignore` + a TODO — do not silently drop the case.
- **PROD-GAP** — production is wrong vs PC ground truth. Two sub-cases:
  - *tractable*: a surgical, low-risk fix (one parser column, one decode call, one label string).
    Fix the **production** code, re-run the affected tests, leave the test assertion alone.
  - *model-level*: needs a schema rewrite, composite-key refactor, or parser+test-lookup change
    together. **Leave the test RED**, log a Harness backlog item with the exact root cause, move on.
    Do NOT surgically half-fix it mid-sweep.

**CẤM fake-green.** A red test for genuinely-broken code is the correct, honest state. Weakening an
assertion or `Assert.Ignore`-ing a real PROD-GAP to make the number drop is the one unforgivable move.

## Verify triage EMPIRICALLY before applying any fix (hard-won)

A triage subagent (or your own first hypothesis) will produce a confident, plausible root cause.
**It is a self-report, not proof.** Before you touch code, run the cheapest experiment that would
*disprove* it.

Worked example (2026-06-12, the lesson that earned this section): a subagent claimed the Visual SPR
cluster (39 fails, `LoadedPartCount` over-counting by +1/+2) was caused by two orphan staged SPRs
`48fa4044.spr`/`b4196106.spr` colliding with woman LW/RW path hashes, and that deleting them would
turn the cluster green. The hashes *did* match `FM_LW_000_RN01.spr`/`FM_RW_000_RN01.spr`, and PC
ground truth *did* confirm woman has no LW/RW (440 files = BD/HD/HR/LH/RH ×88). Everything lined up.
**The deletion test disproved it anyway**: removing both files left `LoadedPartCount` unchanged
(still 6/7). That proved the `*_FromStagedSprFiles` runtime does NOT resolve parts from
`StreamingAssets/Sprites/<uid>.spr` at all — the cluster is model-level (part-count counts
non-required spec slots), not staging. Had I trusted the triage, I'd have committed a no-op "fix"
and mislabeled the root cause. I restored the files (`git checkout --`) and re-classified as #13.

The pattern: **change the thing the triage says is causal, observe whether the symptom moves.** If
it doesn't move, the triage is wrong no matter how good the evidence chain looked. This is the
deletion/mutation test — the single highest-leverage move in this whole sweep.

When delegating triage to subagents: scope them READ-ONLY (no edits, no recompile — Unity is a
single-writer resource, parallel recompiles clobber each other), cap ~16 tool calls, forbid wide
`srcwalk` (900K-token timeouts), and treat every returned classification as a hypothesis to verify,
not a fact to apply. Fan-out is for *gathering* candidate root causes in parallel; you apply fixes
serially through one Unity loop after verifying each.

## Ground-truth lookups that recur

- PC player/mount sprite tags live in `…/Client 6.0/spr/npcres/{man,woman}`. Verified: `woman`=440
  files, 5 tags (BD/HD/HR/LH/RH ×88), **no LW/RW/YY**; `man` also has **no MA_LW/MA_RW** (weapon
  layers ship elsewhere by weapon type). So any test asserting 5 woman parts is correct PC truth.
- Reference `.txt` tables are often TCVN3 Vietnamese (Windows-1252 bytes + TCVN3 glyph codes), not
  GB2312 — a GBK decoder eats the tab separator and shifts every column (symptom: a speed value
  reads as a wildly wrong neighbour-column value like -40960 vs 156). Fix: read via the project's
  tab-safe TCVN3 reader (`PcText.ReadLinesTcvn3`), not `DecodeBest`. Confirm encoding before
  blaming the parser.
- Phantom hardcoded ids: when a test's expected-id array fails, dump the real source id sequence —
  it may skip an id (e.g. `missles1.txt` jumps 444→446, no 445). The test hardcoding 445 is
  TEST-DRIFT; drop the phantom, don't invent data.

## Recording the sweep in PORT_STATUS

Append a dated "Fixes landed (N → M failures)" subsection with a commit|fix|tests-recovered|backlog
table, then a "Newly triaged this batch (left red — model-level)" list with exact root cause +
backlog id per cluster. Correct any earlier PORT_STATUS claim you've since disproven (e.g. the
Visual cluster's old "hash-collision staging" line got rewritten to "disproven via deletion test;
model-level"). Cite the real job id + `total/passed/failed/skipped` from the run.

## Harness backlog mechanics (gotchas)

- `harness-cli backlog list/add` may not exist in a given build → use `sqlite3 harness.db` directly.
- Backlog columns seen: `current_pain` (not `lane`); `risk` has a CHECK constraint
  `IN ('tiny','normal','high_risk')` — use exactly those literals on insert or it rejects.
- Test namespace for `run_tests` is the real C# namespace (e.g. `VLTK.Tests.Sandbox.*`), NOT the
  reporting fullname which prefixes `.EditMode.`. Using the report name returns 0 matched tests.
- The MCP `run_tests` loop-warning is a false positive; the job still starts. After a fix, force a
  scripts recompile + wait ~25-30s before running, or the test runs against stale assemblies.
- TestResults.xml is at the absolute Unity path `~/.config/unity3d/<org>/<project>/TestResults.xml`.
