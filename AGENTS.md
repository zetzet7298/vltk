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
