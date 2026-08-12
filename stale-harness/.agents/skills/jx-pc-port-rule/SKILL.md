---
name: jx-pc-port-rule
description: Mandatory source-of-truth and provenance guardrail for every JX Online 1 / Vo Lam Truyen Ky PC-to-Unity port. Use before porting any PC behavior, data, UI, map, NPC, item, skill, sprite, effect, sound, or config.
---

# JX PC Port Rule

Use this skill first for every PC-to-Unity port. It defines the only source hierarchy and provenance standard. Use a specialist skill only after this guardrail; use `jx-pc-resource-resolver` only for PAK, SPR, Hash_UID, encoded path, and resource resolution.

## Canonical Sources

- `/var/www/jx-pc` is the sole canonical PC corpus and is read-only.
- Start with the scoped loose source and documentation:
  - Docs: `/var/www/jx-pc/01_tinh_kiem_source/tai-lieu-game`
  - Source index: `/var/www/jx-pc/docs/SOURCE_INDEX.md`
  - Audit report: `/var/www/jx-pc/docs/SCAN_REPORT_TINH_KIEM.md`
  - C++ and client source: `/var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/`
- Then inspect the canonical unpacked runtime data at `/var/www/jx-pc/pak_unpacked/`.
- For JX x DHCD work, JX is authoritative for identity, maps, NPCs, items, skill base data, and SPR/VFX/WAV. Use DHCD behavior only with matching evidence or reverse-engineering evidence.
- Unity code, generated assets, screenshots, prior ports, and guesses are implementation clues, never proof.

## Porting Rules

1. Establish the PC behavior, data, and visual evidence before changing Unity code, data, or assets.
2. Compare that evidence with the current Unity implementation, then make the smallest change that matches the PC result.
3. Do not invent names, formulas, timing, frames, effects, coordinates, fallback behavior, or asset mappings.
4. Preserve original encodings while inspecting PC files. Localize user-facing Unity text only while retaining the source mapping.
5. Do not modify anything under `/var/www/jx-pc`.

## Provenance Record

For every selected source, config, or asset, record:

- Absolute original path, pack/version, and active load-order winner.
- Hash_UID, encoding/path bytes, byte count, and SHA-256 when applicable.
- The PC value or behavior ported and its destination in Unity.

Enumerate all valid candidates before selecting a winner. Use package/load order to choose the winner; use mtime only when package/version and load order are otherwise equivalent. Do not copy candidates merely as evidence; vendor exact bytes only after selecting an asset/config that Unity actually uses.

## Missing Or Conflicting Evidence

Fail closed. Mark the port blocked or provisional and document the conflict. Make a provisional change only when the user explicitly authorizes it; do not silently substitute a different source or resource.
