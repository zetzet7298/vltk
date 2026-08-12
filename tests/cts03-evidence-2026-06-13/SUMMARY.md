# CTS-03 evidence (2026-06-13, offline lane)

Sources:
- Unity Editor: 6000.4.7f1, project /home/zet/.hermes/kanban/boards/vltk-client-test-stabilization/workspaces/t_9bc179e5
- Implementation branch: fullstack/cts03-gamehud-sandbox-nre
- Implementation commit: 62fc1fe47 (pushed to origin/fullstack/cts03-gamehud-sandbox-nre)
- Worktree: git worktree on dev (off dev, not yet merged)

## What was hardened (CTS-03 contract)

### GameHudController (`Assets/Scripts/UI/GameHudController.cs`)

1. `OnIconBarClick(int)` — wrapped in try/catch with a `SubsystemLog.Warn` fallback.
   A bad click now soft-fails instead of NRE-ing the whole UI thread.
2. `BuildIconBarRows` — extracted per-index logic into a static
   `AppendIconBarRuntimeRows` helper that tolerates `manager == null`
   (via `manager?.XxxService?.Count ?? 0`). Wrapped in try/catch so a
   service-side failure logs and falls back to the
   "Runtime service chưa sẵn sàng..." placeholder row rather than
   tearing the panel.

### SandboxManager (`Assets/Scripts/Sandbox/SandboxManager.cs`)

`InitializeSubsystems()` — re-ordered + wrapped each fragile step in try/catch:

1. **BootReport auto-create**: `if (BootReport == null) BootReport = new SandboxBootReport();`
   at the top of the method. The previous contract required `Awake` to have
   run first; the new contract lets any caller (test fixture, custom boot
   orchestrator) invoke `InitializeSubsystems()` directly without a prior
   `Awake`.
2. **InitSubsystem calls** (Game/Camera/UI/World/Debug/Services) wrapped
   individually so a single failed subsystem root does not abort the
   rest. Errors are recorded to `BootReport` and logged as warnings.
3. **MapManager construction + LoadCatalog** — outer try wraps construction
   (sets `MapManager = null` on failure so subsequent `MapManager != null`
   checks still hold); inner try wraps `LoadCatalog`.
4. **Item/drop/skill loading** — wrapped; on failure the four fields are
   cleared to null so other services can probe via null checks.
5. **EnsureMobileUiPanels / PlacePlayerAtDefaultSpawn** — wrapped.
6. **MapManager.LoadMap** — null-checked against `MapManager`.
7. **OnBootComplete** — wrapped so a misbehaving subscriber cannot crash
   the post-boot callback.

IsInitialized is set after the subsystem roots are created so HUD wiring
proceeds regardless of catalog completeness.

## Tests added

### `Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs`

- `OnIconBarClick_NullSandboxManager_DoesNotThrow_AndOpensPanelWithServiceProbeLines` —
  forces `SandboxManager.Instance = null` via reflection, then calls
  `OnIconBarClick(0)` and asserts the panel opens with the per-index
  service-probe line ("Đấu trường PC loaded: 0").
- `OnIconBarClick_NullSandboxManager_AllIndices_RenderServicePlaceholder` —
  same setup, iterates 0..6 and asserts each click does not throw and
  the panel still cites the PC source.
- `OnIconBarClick_NullBoundRoot_DoesNotThrow` — sets `_boundRoot = null`
  via reflection and asserts `OnIconBarClick(0)` does not NRE on the
  `_boundRoot?.Q(...)` call sites.

### `Assets/Tests/EditMode/Sandbox/SandboxManagerFastBootTests.cs`

- `InitializeSubsystems_MinimalSetup_DoesNotThrow_AndSetsIsInitializedTrue` —
  invokes `InitializeSubsystems` via reflection on a fresh GameObject.
  Asserts no throw, `IsInitialized = true`, `BootReport != null`.
- `InitializeSubsystems_MinimalSetup_AssetRegistryIsAlwaysConstructed` —
  same setup, asserts `AssetRegistry != null` (required by
  MinimapService and other HUD services).
- `InitializeSubsystems_MinimalSetup_FastEditorBoot_ResolvesFastEditor` —
  sets `useFastEditorBoot = true`, invokes `InitializeSubsystems`,
  asserts `ActiveBootProfile = SandboxBootProfile.FastEditor`.

These tests invoke `InitializeSubsystems` via reflection rather than
relying on `AddComponent → Awake`. The Awake guard
(`Instance != null && Instance != this`) trips on stale Instance in
EditMode test isolation (cf. PORT_STATUS CTS-02 note), so direct
invocation is the only way to exercise the boot logic in a single test
method.

## Gate G3 — Unity EditMode

### Full suite
- **2315/2315 pass, 0 fail, 4 skipped** (pre-existing `Ignore` markers)
- Result state: Skipped:Ignored (skipped due to [Ignore] markers, not
  failures)
- Total: 2319 testcasecount, duration 137.57 s
- File: `_unity_editmode_full.xml` (~1.9 MB)
- Editor: 6000.4.7f1, batchmode + nographics

### Focused CTS-03 filter (`VLTK.Tests.Sandbox.GameHudControllerTests.OnIconBarClick|VLTK.Tests.Sandbox.SandboxManagerFastBootTests|VLTK.Tests.Sandbox.GameHudControllerTests.PcIconBarButtons_OpenRuntimeBackedPanels`)

8/8 pass:

| Test | Class | Duration |
|---|---|---|
| PcIconBarButtons_OpenRuntimeBackedPanels | GameHudControllerTests | 0.015 s |
| OnIconBarClick_NullBoundRoot_DoesNotThrow | GameHudControllerTests | 0.123 s |
| OnIconBarClick_NullSandboxManager_AllIndices_RenderServicePlaceholder | GameHudControllerTests | 0.027 s |
| OnIconBarClick_NullSandboxManager_DoesNotThrow_AndOpensPanelWithServiceProbeLines | GameHudControllerTests | 0.009 s |
| Awake_DefaultEditorFastBoot_SkipsOptionalServicesAndDefaultMap | SandboxManagerFastBootTests | 0.003 s |
| InitializeSubsystems_MinimalSetup_AssetRegistryIsAlwaysConstructed | SandboxManagerFastBootTests | 32.93 s |
| InitializeSubsystems_MinimalSetup_DoesNotThrow_AndSetsIsInitializedTrue | SandboxManagerFastBootTests | 31.06 s |
| InitializeSubsystems_MinimalSetup_FastEditorBoot_ResolvesFastEditor | SandboxManagerFastBootTests | 2.42 s |

The two ~30 s `InitializeSubsystems` tests are slow because the
production boot path loads the full item/drop/skill catalogs from
StreamingAssets (no caching layer in EditMode). This is expected for
"does not throw on minimal setup" coverage; the test pass/fail signal is
binary.

## Diff summary

```
Assets/Scripts/Sandbox/SandboxManager.cs           | 118 ++++++++++++++++-----
Assets/Scripts/UI/GameHudController.cs             |  54 ++++++++--
Assets/Tests/EditMode/Sandbox/GameHudControllerTests.cs |  75 +++++++++++++
Assets/Tests/EditMode/Sandbox/SandboxManagerFastBootTests.cs |  72 +++++++++++++
4 files changed, 280 insertions(+), 39 deletions(-)
```

## Notes for integration lane

- This is a **defensive-only** change. No new service surface, no new
  public API, no schema change. Safe to merge without re-running
  FS-01 G1/G2 backend gates.
- The `InitializeSubsystems` re-ordering moves `IsInitialized = true`
  to right after the subsystem root creation block (was previously
  before `AssetRegistry = new AssetRegistry();`). Subsystem
  callers that previously polled `IsInitialized` to know whether
  `AssetRegistry` was ready will now see a one-tick delay. No
  in-tree caller is affected (verified via search of `IsInitialized`
  usages in `Assets/Scripts/`).
- The PORT_STATUS backlog entries for `PcIconBarButtons_OpenRuntimeBackedPanels`
  and `SandboxManagerFastBootTests` (lines 458–459) can be marked
  resolved once the integration lane confirms the merge.
