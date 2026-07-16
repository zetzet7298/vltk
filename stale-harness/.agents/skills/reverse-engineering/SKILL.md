---
name: reverse-engineering
description: Reverse DHCD Unity/IL2CPP behavior from APK, smali, metadata, generated ISIL, ARM64 ELF binaries, encrypted AssetBundles, and iOS artifacts with reproducible provenance and bounded confidence. Use before any DHCD behavior reconstruction, native address/callee analysis, source recovery, DODAB1 or AssetBundle decoding, runtime hook, or parity claim.
---

# Reverse Engineering

Use this project-local entry point for every DHCD reverse task. It registers the
external toolkit at `/var/www/reverse-skill` while adding Harness and JX/DHCD
evidence constraints specific to this repository.

## Mandatory Setup

1. Run `srcwalk guide` before code or artifact navigation.
2. Read `/var/www/reverse-skill/AGENTS.md`.
3. Read these toolkit files in order:
   - `/var/www/reverse-skill/skills/field-journal/precedent-auth.md`
   - `/var/www/reverse-skill/skills/tool-index.md`
   - `/var/www/reverse-skill/skills/routing.md`
   - `/var/www/reverse-skill/skills/reverse-engineering/SKILL.md`
4. Route the target before analysis. For this project, normally combine:
   - `skills/apk-reverse/SKILL.md` for APK, Java, smali, and runtime entry paths.
   - `skills/radare2/SKILL.md` for read-only ELF and ARM64 CLI analysis.
   - `skills/reverse-engineering/platforms-hardware.md` for AArch64 calling conventions.
   - `skills/reverse-engineering/tools-dynamic.md` only when an authorized runtime is available.
5. If toolkit routing names `game-security/SKILL.md`, record that the module is
   absent in the current toolkit and use the explicit cross-module route above.

Do not edit `/var/www/reverse-skill` during a port task. Treat it as the external
workflow/tool source. Do not bootstrap tools marked `yes` in its tool index.

## Registered Toolchain

- Java/JADX: `/home/zet/tools/jadx/bin/jadx`
- APKTool: `/usr/bin/apktool`
- ADB: `/usr/bin/adb`
- Frida: `/home/zet/.local/bin/frida`
- radare2: `/usr/bin/r2`
- rabin2: `/usr/bin/rabin2`
- Python: `/usr/bin/python3`

Verify versions only when the task uses a tool. Never guess an executable path.
Ghidra and IDA are not registered as available in the current tool index.

## Evidence Workflow

1. Identify every input with absolute path, byte count, SHA-256, build/version,
   architecture, and role. Never mutate the canonical input.
2. Triage the highest layer first: APK/Java/smali, then IL2CPP metadata and
   generated artifacts, then exact native slices. Do not remain at Java when the
   behavior clearly lives in `.so`.
3. Map control flow before interpreting behavior. For AArch64, record caller
   register setup, callee entry, return usage, field offsets, and indirect calls.
4. Corroborate names and addresses through at least two independent forms when
   possible: metadata/ISIL, pointer-table value, raw disassembly, serialized
   config, or successful runtime observation.
5. Automate repeatable claims in a fail-closed inspector that checks input hashes
   and rejects drift before emitting JSON.
6. Compare decompiler outputs, but treat invalid IL, missing methods, guessed
   types, and reconstructed C# as navigation evidence only.
7. Record failed methods and the next exact unresolved target. Do not hide a
   dead end behind a broad statement such as "native logic unresolved".
8. Run the lane verifier, `story verify-all`, Harness audit, and `git diff --check`.
   Record a detailed trace for high-risk work.

## Confidence Contract

Classify every recovered behavior explicitly:

- `proven`: exact source/data/native/runtime evidence supports the claim.
- `high-confidence reconstruction`: control flow and multiple artifacts support
  an equivalent implementation, but exact parity is not independently proven.
- `product decision`: evidence is missing or conflicting and the chosen behavior
  is an intentional contract, not a recovered fact.

Never promote a field name, parameter name, candidate row, adjacent literal,
directory role, catalog hash, failed decoder output, or decompiler guess to
`proven`. Never describe a reconstruction or product decision as exact DHCD
parity.

## Project Boundaries

- JX remains authoritative for identity, maps, NPCs, items, base skills, and
  SPR/VFX/WAV. Apply `jx-pc-port-rule` before any PC-to-Unity port change.
- DHCD may provide behavior only when this workflow records adequate evidence.
- Do not write under `/var/www/jx-source`.
- Preserve unresolved gameplay constants as provisional. Do not invent offer
  count, weight, RNG, cost, cap, timing, ordering, or fallback behavior.
- Worker and explorer delegation must also follow the exact model, reasoning,
  and `fork_turns` requirements in the repository `AGENTS.md`.
