---
name: unity-mcp-orchestrator
description: Operate the Unity Editor through the live MCP resource-first workflow. Use for Unity scenes, GameObjects, scripts, assets, editor state, tests, and verification.
---

# Unity MCP Orchestrator

Use the live MCP surface, not cached tool counts or copied schemas. At the start
of each task, list resources and tools, then read
`mcpforunity://custom-tools` and `mcpforunity://tool-groups`. Invoke the exact
bare tool names and current parameters advertised by that session.

## Resource-First Workflow

1. Read `mcpforunity://project/info` for project root, name, Unity version,
   target platform, and assets path. Do not claim it lists packages or tool
   capabilities.
2. Read `mcpforunity://instances`. If multiple editors exist, select the exact
   instance with `set_active_instance`.
3. Read `mcpforunity://editor/state`. Resource payloads are wrapped under
   `data`; proceed only when `data.advice.ready_for_tools` is true and
   compilation, domain reload, asset refresh, tests, and Play Mode transitions
   are not blocking.
4. Read the target scene, GameObject, component, asset, menu, or package state
   before mutation. Use paging and summary-first queries for large results.
5. Activate only the required optional group with
   `manage_tools(action="activate", group="<group>")`, then re-list tools.
6. Apply the smallest mutation, re-read the target and editor state, and use
   `read_console` to verify the result.

## Scripts

Use the current script tools for C# changes. After each mutation, wait until
`data.compilation.is_compiling` and
`data.compilation.is_domain_reload_pending` are false, then read console errors
before attaching or exercising the type. Use `refresh_unity` only when external
changes are dirty or a live tool explicitly requires refresh; do not refresh
after every script edit by habit.

## Tests And Visual Verification

- Activate the `testing` group when necessary. The current live schema accepts
  `mode`, `test_names`, `group_names`, `category_names`, `assembly_names`,
  `include_failed_tests`, `include_details`, and `init_timeout`; re-check the
  schema before invoking it.
- Start `run_tests`, retain `job_id`, and call `get_test_job` with that ID. Use
  `wait_timeout` to avoid tight polling.
- Read `read_console` after script, scene, package, or test changes. Resolve
  errors before continuing.
- For visual work, identify the target resource before using camera, scene, UI,
  animation, VFX, or profiling tools. Capture while the intended state is live.

## Recovery

When a tool fails, re-list the live catalog, re-read editor state, confirm the
active instance and target, wait for blocking activity to clear, and retry only
with the current schema. A tool group being advertised does not prove its
optional Unity package, API key, or runtime dependency is installed.
