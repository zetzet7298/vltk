---
name: jx-pc-resource-resolver
description: Resolve JX Online 1 / Vo Lam Truyen Ky PC PAK, SPR, Hash_UID, encoded resource paths, and runtime assets using the canonical jx-source corpus and vltktool. Use whenever a PC resource must be identified, selected, decoded, or verified.
---

# JX PC Resource Resolver

This is the sole workflow for PC PAK, SPR, Hash_UID, encoded path, and resource lookup. Apply `jx-pc-port-rule` first for source hierarchy and provenance requirements.

## Canonical Inputs

- PC corpus, read-only: `/var/www/jx-source`
- Unpacked runtime/PAK data: `/var/www/jx-source/pak_unpacked/`
- Resolver, hash, decoder, and PAK toolchain: `~/Projects/vltktool`

Do not implement a hash algorithm, guess a path encoding, derive a filename manually, or use raw filesystem searches as the resolution method.

## Resolution Workflow

1. Identify the logical resource reference in the scoped PC loose source and/or unpacked runtime config. Do not assume a particular Unity reference-file layout or NPC config filename.
2. Use `~/Projects/vltktool` to resolve the path bytes and encoding, calculate or look up the Hash_UID, and locate every matching unpacked candidate.
3. Cross-check the resolver result with the relevant `_labels.json` entry, including `name_vi`, when available.
4. For SPR assets, decode the selected SPR with `vltktool` before using it; do not infer its visual content from its UID or label.
5. Enumerate candidates and determine the active package/load-order winner. Mtime is only a tie-breaker after package version and load order are equivalent.
6. Verify the selected file under `/var/www/jx-source/pak_unpacked/` and report its absolute path, pack/version, winner, Hash_UID, path bytes/encoding, byte count, and SHA-256.

## Output And Failure Rules

- Keep the original logical path and its resolved physical file distinct in notes and code comments.
- Vendor only the exact selected bytes when the Unity port actually uses them.
- If `vltktool` cannot resolve the resource or candidates conflict, stop and report the unresolved evidence. Do not substitute a similarly named asset or an independently computed hash.
