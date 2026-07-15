# Project Tooling Profile

This profile separates Harness inbound tools from Unity packages and dynamic
Unity MCP tools. Register only stable external entry points in Harness. Do not
register every Unity package or every live MCP tool as a separate provider.

## Harness Inbound Tools

| Provider | Kind | Capability | Stable entry point |
| --- | --- | --- | --- |
| `srcwalk` | CLI | `code-navigation` | `/home/zet/.local/bin/srcwalk` |
| `ketch` | CLI | `documentation-lookup` | `/home/zet/go/bin/ketch` |
| `semble` | MCP | `code-search` | MCP server `semble` |
| `unity-mcp` | MCP | `unity-editor-automation` | `http://127.0.0.1:8080/mcp` |
| `vltktool-unpak` | binary | `jx-pak-unpack` | `/home/zet/Projects/vltktool/unpak_tool.py` |

These five entries are registered in the local Harness database. `tool check`
reports them as present; for MCP entries this proves project configuration, while
live usability is still verified through the MCP session resources.

`ketch` currently uses its configured Exa backend. Registering a second Exa MCP
provider would add duplicate context and would not prove that the MCP is live.
Keep Exa behind `ketch` unless a live agent runtime explicitly exposes the Exa
MCP surface.

The broader `/home/zet/Projects/vltktool` directory remains the canonical JX
resolver/hash/SPR/PAK toolchain through `jx-pc-resource-resolver`. Only its
stable executable unpacker is a Harness binary provider; the directory itself
is not a binary.

## Unity MCP Live Contract

Observed on 2026-07-15:

- MCP server: `mcp-for-unity-server` `3.4.4`
- Unity: `6000.4.7f1`
- Project root: `/var/www/vltk-mobile`
- Active scene: `Assets/Scenes/Sandbox.unity`

This is an observation, not a cached schema. Every Unity task must list the live
tools/resources and read:

- `mcpforunity://project/info`
- `mcpforunity://instances`
- `mcpforunity://editor/state`
- `mcpforunity://custom-tools`
- `mcpforunity://tool-groups`

The live server groups tools into `core`, `testing`, `docs`, `scripting_ext`,
`ui`, `vfx`, `profiling`, `probuilder`, `animation`, and `asset_gen`. Activate
only the group required for the task and re-list the tool schema afterward.
An advertised group does not prove that an optional package or API key exists.

## Unity Package Source

`/var/www/vltk-mobile/Packages/manifest.json` and `packages-lock.json` are the
package authority. Relevant installed packages currently include Addressables,
Cinemachine, Input System, Localization, Memory Profiler, Profile Analyzer,
Test Framework, Timeline, UI Toolkit/uGUI, URP, and Coplay Unity MCP.

Do not copy package versions into skills. Read the manifests or use the live
package tool before relying on a package API. ProBuilder is not present in the
current manifest, so `manage_probuilder` must fail closed until package presence
is proven.
