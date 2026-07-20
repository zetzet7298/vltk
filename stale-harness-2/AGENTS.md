# Agent Instructions

## PC Source Of Truth

- PC docs chuẩn: `/var/www/jx-source/01_tinh_kiem_source/tai-lieu-game`.
- Canonical PC source duy nhất là `/var/www/jx-source`; coi toàn bộ cây này là read-only.
- Canonical runtime/PAK đã unpack là `/var/www/jx-source/pak_unpacked/`.
- Index/audit hiện hành: `/var/www/jx-source/docs/SOURCE_INDEX.md` và `/var/www/jx-source/docs/SCAN_REPORT_TINH_KIEM.md`.
- C++/source tree cần tra trước khi port: `/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/`.

## Canonical PC Rules

- Với PAK, SPR, DAT, Hash_UID hoặc encoded config, bắt buộc dùng `~/Projects/vltktool`; không tự hash/decode hoặc đoán encoding.
- Với SPR thì nên dùng hash/disk path không nên dùng logical path. logical path Chỉ để hiểu logic,behavior code của Pc
- SPR có text/UI: luôn kiểm tra `bin/client/package.ini` để chọn **winner theo package priority** (ví dụ Vietnamese override `update01.pak` có thể ghi đè `spr.pak`); không dùng fallback tiếng Trung chỉ vì logical path trùng.
- Resolve logical path → UID bằng `vltktool resolve_uid.py`, extract đúng frame winner bằng `vltktool extract_item_spr.py`, rồi `cmp` với PNG Unity và lưu UID/package/frame + SHA-256 vào provenance trước khi dùng.
- Không copy candidate chỉ để làm evidence. Chỉ vendor exact bytes vào repo-local slice khi asset/config đã được chọn và thực sự dùng.
- Không sửa bất kỳ file nào dưới `/var/www/jx-source`.

## Unity MCP Skill Matrix

Use `/var/www/vltk-mobile/harness/.agents/skills/unity-mcp-orchestrator/SKILL.md` Orchestrate Unity Editor via MCP (Model Context Protocol) tools and resources. Use when working with Unity projects through MCP for Unity - creating/modifying GameObjects, editing scripts, managing scenes, running tests, or any Unity Editor automation. Provides best practices, tool schemas, and workflow patterns for effective Unity-MCP integration.

## Unity Refactor And Fast-Iteration Rules

These rules bind every agent and subagent. Delegation requires a bounded
assembly/module scope and an explicit validation target.

### Refactor Boundaries

- Reduce `Assets/Scripts/Sandbox/` toward roughly **5-10 cohesive assemblies**;
  add neither broad subsystems nor assemblies per class, fixture, parser, or
  narrow feature. Prove every edge from code, starting with:

  ```text
  VLTK.Model
      <- VLTK.Gameplay.Domain
          <- VLTK.PortData / VLTK.Combat / VLTK.World
              <- VLTK.Sandbox.Runtime
                  <- VLTK.UI
  ```

  Arrows point from consumer to dependency. Keep edges one-way; inner/domain/data
  code must not reference UI, Editor, tests, or scene composition. Reject cycles
  and reuse `Core`, `Resources`, `Sprites`, and `Backend`.
- Before any script move or `.asmdef` edit, use `srcwalk` to map symbols,
  callers, dependencies, consumers, and tests; record the intended owner and
  dependency direction. Folder names are not evidence.
- Refactor one compiling/testable slice at a time; separate behavior from
  moves/boundaries unless explicitly coupled. Move scripts with their `.meta`;
  never recreate them or change GUIDs.
- Do not change global defines, Enter Play Mode, Code Optimization, Auto Refresh,
  or hot-reload packages unless the current request explicitly authorizes it.

### Dead-code Cleanup

- Before deleting, run `srcwalk assess`, `trace callers`, and `deps`; also check
  tests, reflection/string lookup, serialization, runtime-init/`MonoBehaviour`
  roots, generated hooks, and Editor tooling project-wide. Zero callers is not
  proof; keep anything with unresolved dynamic, serialized, provenance, or
  external reach.
- Delete script + `.meta` only after ownership/GUID review. Never remove tests,
  weaken assertions, discard source/provenance for speed, or mix cleanup with
  unrelated API/data changes. Then run `srcwalk review`, clean compile, zero new
  Console errors, focused EditMode, and relevant PlayMode smoke; report files/LOC,
  evidence/caveats, and claim speed only from comparable measurements.

### Compile And Test Loop

- Daily loop uses Debug Code Optimization:

  ```text
  Play once -> supported method-body edit -> Fast Script Reload
    -> live check -> smallest related EditMode batch
  ```

- Use `scripts/run_unity_test_profile.py` when available. `M:path` is only for a
  known existing method-body edit; never use explicit paths/profiles to hide
  structural work or bypass automatic escalation.
- Fast Script Reload must already be installed/healthy; it is never completion
  proof, and production code must not bend to fit it. Unsupported or structural
  edits (fields, serialization, inheritance, public/generic API, moves, asmdefs)
  require normal full compile, zero new Console errors, focused tests, and the
  relevant integration/PlayMode smoke.
- Use focused EditMode for pure logic; reserve PlayMode for Unity lifecycle,
  scenes, physics, animation, input, rendering, or platform behavior. Unity MCP
  edits already request import/compile: do not refresh again; wait for reload and
  read Console errors before testing.
- Use Release only for profiling/final verification. Performance claims require
  comparable before/after Unity/Bee evidence with mode, assembly size, duration,
  dependents, and exact proof. Never weaken/delete/exclude tests for speed;
  profile/define changes need explicit authorization and a full-verification path.

<!-- HARNESS:BEGIN -->
## Harness

Choose the request class before any Harness operation.

- When the requested outcome is only an answer, explanation, review, diagnosis,
  plan, or status report: inspect only the material needed to respond. Keep the
  task read-only. Do not bootstrap, initialize or migrate a database, record
  intake, or record a trace.
- When the user explicitly asks to change, build, fix, or write repository
  artifacts: first run `scripts/bootstrap-harness.sh`
  on macOS/Linux or `.\scripts\bootstrap-harness.ps1` on Windows. Then use
  `docs/FEATURE_INTAKE.md` to classify and record the request, query
  `scripts/bin/harness-cli query matrix --active --summary` on macOS/Linux or
  `.\scripts\bin\harness-cli.exe query matrix --active --summary` on Windows,
  and retrieve only the lane- and task-specific context described in
  `docs/CONTEXT_RULES.md`.
<!-- HARNESS:END -->
