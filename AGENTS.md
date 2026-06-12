# VLTK Mobile Agent Instructions

## User-facing language

User-facing responses must be Vietnamese. If PC JX source text is Chinese, port/user-facing text must be Vietnamese.

## PC source-of-truth for porting

Before any PC→Mobile port/audit task, inspect the PC source under:

```text
/var/www/vltksource_new
```

The PC source-of-truth is the combination of:

```text
/var/www/vltksource_new/vl_update_27/Client 6.0
/var/www/vltksource_new/vl_update_27/Server 6.0
/var/www/vltksource_new/vl_update_27/pak_unpacked
/var/www/vltksource_new/docs/port_docs
```

Canonical unpacked PAK tree:

```text
/var/www/vltksource_new/vl_update_27/pak_unpacked
```

Manifest/audit:

```text
/var/www/vltksource_new/vl_update_27/pak_unpacked/_unpack_summary.json
```

Current audit baseline:

```text
46/46 real source .pak files accounted for
401,281 / 401,640 unique entries present on disk (99.91%)
357 known undecoded entries: unsupported compression method 0x11000000
```

Rules:

1. Treat `pak_unpacked` as essential PC source, not a cache.
2. PAK contents are not image-only: they include SPR assets, Lua, TXT/INI config, map/runtime data, audio, and other logic/resources.
3. Before declaring behavior/resource missing, inspect both loose PC source and `pak_unpacked`.
4. Use `/var/www/vltktool/unpak_tool.py` only for exceptional repair/re-unpack cases; do not unpack from scratch during normal port tasks.
5. Keep provenance in code/docs: cite exact PC source path and, when available, PAK origin.
6. Do not assume `_labels.json` exists or is current. If label/taxonomy data is needed, rebuild/use `/var/www/vltktool` against the canonical unpack root.

## Harness context

Harness lives under:

```text
/var/www/vltk-mobile/harness
```

When using Harness, read and follow:

```text
/var/www/vltk-mobile/harness/AGENTS.md
/var/www/vltk-mobile/harness/README.md
/var/www/vltk-mobile/harness/docs/HARNESS.md
/var/www/vltk-mobile/harness/docs/FEATURE_INTAKE.md
/var/www/vltk-mobile/harness/docs/ARCHITECTURE.md
/var/www/vltk-mobile/harness/docs/CONTEXT_RULES.md
/var/www/vltk-mobile/harness/docs/PORT_STATUS.md
```

Use the Harness CLI from `/var/www/vltk-mobile/harness` unless explicitly directed otherwise.

## Unity constraints

- Unity package/API availability is documented in `harness/AGENTS.md`.
- URP is active; new materials must use URP shaders.
- Use Addressables/runtime catalogs instead of new `Resources.Load<T>()` usage.
- Use Input System, not legacy `Input.GetAxis` / `Input.GetKey`.
- Do not mark a port row complete unless exact PC evidence, mobile implementation, and verifier/test proof cover the stated narrow scope.

## Parallel multi-agent work rules (MANDATORY)

This repo is ported by multiple Hermes workers running in parallel. To avoid
corrupting the shared Unity Editor or thrashing the machine, every worker MUST
follow these rules.

### Two lanes — know which one you are

1. **Offline lane** (profiles `vltk-fixer`, `vltk-fixer2`, …): parser / model /
   service / logic work in C#. You have NO Unity MCP tools and you do NOT open a
   Unity Editor. You verify by reading PC source and reasoning about parser
   output, NOT by compiling in Unity. Commit to your own branch/worktree and
   hand off; the integration worker compiles and runs tests.
2. **Integration lane** (profile `vltk-unity`, exactly ONE worker): owns the
   single running Unity Editor and all `mcp_unityMCP_*` tools. Merges offline
   branches, triggers recompile, reads the console, runs the Test Runner, closes
   compile/test failures, updates `docs/PORT_STATUS.md`.

### The one-Editor rule

- There is exactly ONE Unity Editor per project path (Unity lockfile enforces
  it). NEVER start a second Editor on `/var/www/vltk-mobile`. NEVER close the
  running Editor — it is a long-lived compile/test daemon.
- Offline-lane workers MUST NOT call any `mcp_unityMCP_*` tool and MUST NOT run
  `Unity -projectPath …`. If you think you need Unity, you are in the wrong lane;
  hand off to the integration worker instead.
- Offline `dotnet build` of the Unity-generated csproj does NOT work (hybrid
  mscorlib + netstandard 2.1 → ~928 phantom errors). Do not waste time on it.
  The integration worker's Editor is the only compile oracle.

### Worktree isolation (no code conflicts)

- Every implementation worker works in its own git worktree on its own branch
  (`port/<domain>`), never directly on `dev`. This is how parallel workers avoid
  clobbering each other and the main branch.
- Do not edit files outside your worktree. Do not touch another worker's branch.

### Machine-safety limits

- Max ~4 offline workers running concurrently. RAM (not CPU) is the bottleneck:
  the Editor holds ~7GB and the box swaps past that. More workers = swap thrash =
  slower, with OOM-kill risk that loses in-progress work.
- Do NOT scan the whole PC source tree or run repo-wide SPR/PAK decoding (it
  crashes the box). Narrow to one PAK/folder first per the resource rules above.

### Definition of done (no false green)

- A card is DONE only when the integration worker has compiled the merged code
  in the real Editor with zero new CS errors AND the relevant Test Runner tests
  pass in a real artifact. `true`/`echo` verify commands do not count.
- Offline workers `kanban_block` with `review-required:` for the integration
  worker to pick up; they do not self-certify "compiles" or "tests pass".

### Encoding / decode golden rules (the #1 systemic bug class)

The single biggest source of runtime bugs in this port is wrong text decoding of
`Reference/*.{txt,ini}` files. Audited 2026-06-12: of 353 shipped reference files,
**221 are Vietnamese-localized TCVN3** (Western/windows-1252 bytes whose high chars
are TCVN3 glyph codes), 119 are ASCII, 12 are UTF-8, and only **1 is genuinely
Chinese GB2312** (`PcItemFull/helmres.txt`). Bugs found this way: maplist names,
meridian 118/13 (lost rows from swallowed tabs), title 335/363 + mojibake.

1. **`PcText.ReadLines(path, null)` / `PcItemCommon.ReadServerLines` use
   `DecodeBest` auto-detect, whose `Score()` is biased toward CJK (hanzi weight 8 >
   Vietnamese 4).** On a TCVN3 file, certain Vietnamese high-bytes get read as GBK
   lead bytes and swallow the following `0x09` TAB → column shift → lost/garbled
   rows. For a file KNOWN to be Vietnamese-TCVN3, call `PcText.ReadLinesTcvn3(path)`
   (forces windows-1252 + TCVN3, skips auto-detect). One-line swap.
2. **Not every file is Vietnamese.** Some are genuinely Chinese (GB2312) or mixed
   asset-path tables. Forcing TCVN3 on a Chinese file corrupts it — the exact
   inverse bug. Classify per-file before swapping.
3. **`DecodeBest` replication in Python LIES.** The offline replica picked TCVN3
   for title #7 but the real Editor picked GB2312 (335/363 + mojibake). To verify
   an encoding OFFLINE you MUST force-decode both ways — windows-1252+TCVN3 vs
   GB2312/GB18030 — count replacement chars / inspect sample names, and pick the
   clean one. Never trust a `DecodeBest` reimplementation, and never trust a raw
   latin1/byte split (it shows clean counts even when the real decode breaks).
4. The integration-lane Editor is the final arbiter: a decode fix is only proven
   when the parser's EditMode tests go green in a real Test Runner artifact.

### Root-cause over patch (applies to all lanes)

When a card's premise turns out to be wrong (e.g. "all 14 files are TCVN3" when
11 are actually Chinese), do NOT blindly execute the card. Scope it down, leave
evidence in a `kanban_comment`, fix only what is provably correct, and spawn
follow-up cards for the rest. A worker that corrupts data by obeying a wrong card
is worse than one that stops and reports. This is the same "no false green"
principle applied at implementation time.
