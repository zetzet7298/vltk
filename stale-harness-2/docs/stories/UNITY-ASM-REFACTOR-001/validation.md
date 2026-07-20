# Validation

## Proof Strategy

Each structural slice is accepted only when it preserves GUIDs, compiles normally
in Unity, adds no Console errors, passes focused tests, and passes any relevant
PlayMode smoke. Comparable before/after timing is an initiative-level gate for a
performance claim, not a blocker for accepting a behavior-preserving structural
slice. Hot reload is not proof.

## Test Plan

| Layer | Cases |
| --- | --- |
| Unit | Focused EditMode tests for every moved pure parser/service/catalog. |
| Integration | Consumers compile and relevant cross-assembly test group passes. |
| E2E | Required only if the slice crosses scene/UI/runtime behavior. |
| Platform | Relevant PlayMode/mobile smoke only when an engine/platform boundary changes. |
| Performance | Before claiming an initiative-level improvement, compare assembly source size and Unity/Bee timings under equivalent invalidation, cache, optimization, and dirty-worktree conditions. |
| Logs/Audit | Unity Console contains zero new compile/runtime errors. |

Fast Script Reload compatibility must be tested separately for supported method
bodies, async/lambda usage, generic code, serialized fields, and public API/type
changes. Unsupported cases must fall back cleanly to normal Unity compilation.

## Fixtures

- Existing deterministic EditMode fixtures for the selected slice.
- Existing Unity scene or PlayMode fixture only if demanded by the selected boundary.

## Commands

```text
Full Unity compile and zero new Console errors.
Focused EditMode batch:
- ItemData parser/batch/importer/equipment-binding tests that consume moved types.
- PcDropRateParser and DropRateRegistry tests.
- Direct PcItemCommon/PcText consumer tests identified by the dependency map.
Relevant Sandbox boot/integration smoke after the full compile.
```

## Acceptance Evidence

- Herdr scout, peer, and proof-auditor reports.
- Selected slice: 20-file VLTK.PortData item-ingestion seam, with PortData ->
  Core/Model and Sandbox -> PortData only.
- Twenty moved source/meta pairs are byte-identical to their `HEAD` originals;
  all original script GUIDs are preserved. The slice contains 2,015 moved LOC.
- Reviewer found no confirmed P0-P3 issue, no assembly cycle, no behavior/body or
  namespace change, and no missing moved asset/meta pair.
- The writer also created the required sibling `Assets/Scripts/PortData.meta`,
  which sat just outside its declared directory write-scope. The coordinator
  treated this as an orchestration scope violation, inspected the file, verified
  its unique GUID, and explicitly adopted it into the integrated slice.
- The first compile request used the stale pre-move source graph and reported
  CS2001 missing-source errors. After a forced full AssetDatabase refresh, Unity
  imported the new asmdef and completed compilation successfully.
- Successful compile produced `Library/ScriptAssemblies/VLTK.PortData.dll` and
  the final MCP Console error query returned zero entries. A later no-change full
  refresh also returned the Editor to ready state with zero Console error entries.
- Focused EditMode job `6ac836a15d05400683690cdcd992ef20` passed 102/102,
  failed 0, skipped 0, duration 11.6382788 seconds. Snapshot:
  `/home/zet/.local/state/herdr-orchestrator/runs/orch-12a6325c56fb064c/editmode-TestResults.xml`,
  SHA-256 `71bd180937ddb1fc1febe7633022b41dfb2fb6d4d6e780dc36262a9895237e0d`.
- PlayMode job `e9bf7da36d9b42af878ad7a79da5923e` passed the
  `SandboxBootE2ETests` fixture 10/10, failed 0, skipped 0, duration 110.128341
  seconds.
- The successful changed-graph Bee observation recorded PortData Csc 1.320209 s,
  PortData ILPP 0.014301 s, Sandbox Csc 5.583566 s, EditMode Csc 2.391153 s,
  and backend wait 9.971927 s. These are after observations only.
- No compile-time improvement percentage is claimed: a comparable before trace
  with the same invalidation, cache, optimization mode, and dirty-worktree state
  does not exist yet.
- PortData wave 2 moved another 24 pure parser source/meta pairs (2,522 LOC) with
  24/24 source bytes, meta bytes, and GUIDs preserved. `VLTK.PortData` retained
  only `VLTK.Core` and `VLTK.Model` references; `VLTK.UI` gained a direct
  `VLTK.PortData` reference because `MallPanelService` consumes `PcMallEntry`.
- The new `VLTK.Tests.PortData.EditMode` assembly passed 35/35 tests in job
  `8c14a18322b84641aa112aadb5bc48cb` (1.9440382 seconds). The wave-2 consumer
  group passed 55/55 in job `6f77eea138114d008dc306a53226856d`
  (105.1006481 seconds), and the earlier boundary PlayMode smoke passed 10/10 in
  job `45544324f8c14d7faddaa8e22e14b5af` (167.0811705 seconds).
- World wave 1 moved 26 pure map/travel parser source/meta pairs (2,755 LOC) into
  `VLTK.World`. All 26 sources and metas remain byte-identical to `HEAD`, all
  GUIDs are preserved, namespaces and bodies are unchanged, and the assembly has
  `noEngineReferences: true` with dependencies only on Core, Model, and PortData.
- The World consumer direction is `World -> Core/Model/PortData`,
  `Sandbox -> World`, and `VLTK.Tests.EditMode -> World`; no World reference to
  Sandbox or UI was added. Runtime loaders, map services, rendering, and scene
  orchestration remain in Sandbox.
- After one required full AssetDatabase import for the external `git mv`, Unity
  completed normal Debug compilation and the Console compile-error query returned
  zero entries. No redundant refresh followed.
- World focused EditMode job `743abd404a354c0c831b7c15f13947ba`
  passed 108/108, failed 0, skipped 0, duration 1.5908729 seconds. A preceding
  short-name selection returned 0 tests and is explicitly not counted as proof.
- World PlayMode job `fd4541aa4d92496cbfba04b60262ae1e` passed
  `SandboxBootE2ETests` 10/10, failed 0, skipped 0, duration 110.9669679 seconds.
  Afterward Unity logged one exception-classified entry whose complete message was
  only the Test Runner saving `TestResults.xml`; it had no stack trace and was not
  a compile or test failure.
- The initial Fast Script Reload attempt remained 0/5: the cold session had no
  live probe, no `[FSR-PILOT]` evidence, and no qualifying post-start method-body
  save. This was an intermediate result superseded by the successful retry below;
  normal Debug compilation remains the fallback for unsupported/structural edits.
- Herdr recovery recorded two orchestration defects honestly: the coordinator
  created the measurement document while an earlier writer was active, producing
  a `proven_violation` even though paths did not overlap; and raw Unity Console SSE
  observation appeared hung. The latter was inspected once, found to have exited,
  and both read-only attempts were collected through guarded controller recovery.
- The independent World reviewer found no confirmed P0-P3 issue. It independently
  observed 52 `R100` moves, 26/26 source and meta hashes/GUIDs preserved, zero
  insertion/deletion in moved bodies, no World-reachable asmdef cycle, and no
  direct UnityEngine or UI dependency in World.
- Reviewer residual gap: direct EditMode symbol coverage was found for 11 of the
  26 moved types. Remaining parsers may be exercised indirectly, but a per-type
  execution map does not yet exist; keep this as follow-up proof rather than
  weakening the accepted 108-test boundary batch.
- Herdr could not formal-collect the completed reviewer because the accumulated
  run state exceeded the controller's 524,288-byte write limit. The controller
  reported `mutation_certainty=none`; the final reviewer transcript was inspected
  directly and the orchestration run remains active instead of being falsely
  marked finished.
- Combat wave 1 moved 18 clean skill/combat parser and catalog source/meta pairs
  (2,549 LOC) into `VLTK.Combat`. `PcModSkillParser` was explicitly excluded
  because it is dirty parity work. All 18 source and meta pairs remain byte-identical
  to `HEAD`; script GUIDs are preserved and namespaces/bodies are unchanged.
- `VLTK.Combat` has `noEngineReferences: true` and references only Model and
  PortData. Source scan found no UnityEngine, UnityEditor, or UI dependency.
  Sandbox, the main EditMode assembly, and PortFactorySmoke reference Combat;
  Combat does not reference any of them.
- The first Combat compile exposed 13 test-topology errors. Five of six proposed
  parser fixtures also exercised Sandbox services, so the proposed dedicated
  Combat test assembly would have depended back on Sandbox. The fixtures were
  returned intact to the main EditMode assembly and the micro test asmdef was
  deleted; no test was weakened, excluded, or removed.
- After adding the missing `PortFactorySmoke -> VLTK.Combat` edge, the required
  second structural import/compile completed with zero Console compile errors.
  No redundant refresh followed either compile request.
- Combat focused EditMode job `4604eee603ce4cf1951cbe25b2490dad`
  passed 76/76, failed 0, skipped 0, duration 33.6478032 seconds. It covered six
  parser fixtures plus direct Sandbox services, catalog parity, weapon/thief source
  parsing, faction skill tree, and the PortFactory NPC combat plan.
- Combat PlayMode job `f528d651662e489d854ceb555c4937d8` passed
  `SandboxBootE2ETests` 10/10, failed 0, skipped 0, duration 117.2259051 seconds.
  The only exception-classified post-test entry was the Test Runner saving
  `TestResults.xml`, with no stack trace.
- After Combat wave 1, Sandbox contains 89,257 C# LOC. The generated assemblies
  measured 49,664 bytes for `VLTK.Combat.dll` and 2,002,432 bytes for
  `VLTK.Sandbox.dll`. These are size observations, not a compile-speed claim.
- Fast Script Reload retry passed five consecutive method-body saves in one Play
  session: baseline revision 2 followed by revisions 3, 4, 5, 6, and 7, all on
  instance `-10579422`. No normal compile or domain reload occurred during the
  saves and Unity's internal Console counter reported zero errors afterward.
- The post-FSR normal compile completed with zero Console compile errors. Focused
  EditMode job `2878989fd6c4497388e38f2822599f15` passed 4/4 in 1.068024 seconds;
  final PlayMode job `cbd18e9416a84f7d84f4ca4eb6b48662` passed 10/10 in
  202.4614341 seconds. Debug and Auto Refresh mode 2 remained active.
- Gameplay.Domain wave 1 moved six production source/meta pairs into
  `VLTK.Gameplay.Domain`: `AssetRegistry`, `FactionVietnameseCatalog`,
  `PcMaxManaFormula`, `SkillCatalog`, `SkillLevelUpScriptCatalog`, and
  `SkillSectCatalog` (791 LOC total). Five source bodies and all six meta files are
  byte-identical to their originals; every script GUID is preserved. The only
  source edit was removal of an unused `using UnityEngine` from `AssetRegistry`,
  allowing the assembly to retain `noEngineReferences: true`.
- The final production direction is `Gameplay.Domain -> Core/Model`, with Sandbox
  and UI consuming Domain. The pure in-memory registry/catalog stays in Domain;
  `VLTK.Resources` continues to own actual resource providers. A graph generated
  from all production asmdefs passed `tsort` with exit 0; Domain has no edge to
  Sandbox, UI, Resources, Editor, or tests.
- The first Domain compile correctly rejected the proposed boundary because
  `IAssetRegistry` still lived in Sandbox. A temporary placement in
  `VLTK.Resources` then exposed 21 `Resources.Load/GetBuiltinResource` namespace
  collisions when Sandbox referenced the assembly. The coordinator rejected the
  call-site workaround, moved the pure registry into Domain, removed the incorrect
  Resources edges, and added the one missing direct PlayMode-to-Domain reference.
  The final Console compile-error query returned zero entries.
- `VLTK.Tests.Gameplay.Domain.EditMode` owns the two fully isolated Domain fixtures;
  the mixed `SkillSectCatalogTests` fixture remains in the main EditMode assembly
  because it also exercises Sandbox services. Focused Domain job
  `5e3a1e7705c54230b9cd26d746feb687` passed 13/13 in 0.6880175 seconds. The selected
  catalog/progression/localization/UI/mana consumer job
  `ca54c01d95364564a327cc2485cd7c0d` passed 92/92 in 4.0780952 seconds.
- The full PlayMode assembly job `7a8ab530b01642ff9cd4023b24e6580c`
  completed 31 tests and reported one failure in the unrelated backend combat-demo
  fixture. A focused rerun `ac7b72929af9440ab5ba9d27454cc637`
  reproduced it: setup sets `runCombatDemoOnComplete=false`, while the test calls
  `StartFullFlowAsync` and waits for `IsCombatDemoCompleted=true`; production code
  only runs the demo when that flag is true. This pre-existing contradictory test
  was not changed as part of the assembly refactor.
- The relevant boundary PlayMode job `5e5da3b2e892442caaa18a6eebe30651`
  passed `E2EGameplayLoopTests` plus `SandboxBootE2ETests` 18/18, failed 0, skipped
  0, in 182.4734609 seconds. It directly covers `SkillCatalog`, `AssetRegistry`,
  gameplay registration, Sandbox lifecycle, map load/switch/unload, and registry
  wiring. After clearing the Test Runner's result-save bookkeeping entry, the final
  Console error query returned zero entries.
- After this wave Sandbox contains 88,465 C# LOC. Generated assembly sizes are
  31,232 bytes for `VLTK.Gameplay.Domain.dll`, 2,119,680 bytes for
  `VLTK.Sandbox.dll`, and 10,240 bytes for the Domain EditMode test DLL. These are
  size observations only; no compile-speed percentage is claimed.
- The required guarded Herdr resume used one event-driven await. It failed before
  mutation because the durable run state still exceeds 524,288 bytes
  (`mutation_certainty=none`); no status polling or false finish followed.
- World wave 2 moved 15 additional clean weather/music/map/object/spawn/NPC-resource
  parser source/meta pairs (1,632 LOC) into the existing `VLTK.World` assembly.
  All 30 source/meta hashes are identical before and after the move, all script
  GUIDs are preserved, and no new production or test asmdef was created.
- The moved sources contain no UnityEngine, UnityEditor, or UI reference. Their only
  project-level dependencies are Core, Model, PortData helpers, and World parsers;
  UI, PlayMode, and PortFactorySmoke had no direct source consumers. The production
  asmdef graph again passed `tsort` with exit 0.
- One required full AssetDatabase import/normal compile completed with zero Console
  errors; no second Refresh followed. Focused World wave-2 EditMode job
  `9e840d70d97c49689489eaba0ade9898` passed 35/35 in 1.9419977 seconds. Boundary
  PlayMode job `b818dc490cc544ccaedb155877d4aee3` passed
  `SandboxBootE2ETests` 10/10 in 186.1549656 seconds.
- After World wave 2, Sandbox contains 86,833 C# LOC and World contains 4,387 C#
  LOC. Generated assembly sizes are 110,080 bytes for `VLTK.World.dll` and
  2,084,864 bytes for `VLTK.Sandbox.dll`. These remain size observations, not a
  compile-speed percentage claim.
- Combat wave 2 moved 12 clean missile/magic-attribute and battle/arena parser
  source/meta pairs (1,320 LOC) into the existing `VLTK.Combat` assembly. All 24
  source/meta hashes are identical before and after the move, all script GUIDs are
  preserved, and no asmdef edge changed: Combat still references only Model and
  PortData with `noEngineReferences: true`.
- Candidates that required World helpers or a Sandbox-owned catalog were excluded
  instead of creating Combat-to-World/Sandbox edges. Dirty combat runtime, tuning,
  mod-skill, and parity sources remained untouched. Source scans found no
  UnityEngine, UnityEditor, or UI reference in the moved slice; the production
  asmdef graph again passed `tsort` with exit 0.
- One required full AssetDatabase import/normal compile completed with zero Console
  errors and no second Refresh. Focused Combat wave-2 EditMode job
  `95424c604c524ce8ba481792006f151e` passed 42/42 in 3.7019307 seconds. Boundary
  PlayMode job `155a3565ebc1418c95676e01ff378c1f` passed
  `SandboxBootE2ETests` 10/10 in 180.0128401 seconds.
- Existing tests directly or through services cover missile, attrib-constant,
  battlefield, award, reward, honor, Tống-Kim, SJ-battle, and arena behavior.
  `PcMagicAttribParser`, `PcHuaShanLuanJianParser`, and some registry-only paths do
  not yet have a dedicated direct parser fixture; this remains an explicit coverage
  gap rather than an inferred pass.
- After Combat wave 2, Sandbox contains 85,513 C# LOC and Combat contains 3,869 C#
  LOC. Generated assembly sizes are 77,824 bytes for `VLTK.Combat.dll` and
  2,061,312 bytes for `VLTK.Sandbox.dll`. These are size observations only.
- PortData wave 3 moved 21 clean parser source/meta pairs (1,998 LOC) from Sandbox
  into the existing `VLTK.PortData` assembly: achievement, activity, adventure,
  auction config, online/daily rewards, fashion, friend, mail, mount, partner, pet,
  player title, ranking, reputation, title effect, VIP, world rank, faction bonus,
  faction relation, and faction title. `PcHonorParser` was explicitly excluded
  because it resolves `Application.streamingAssetsPath` through UnityEngine.
- All 42 moved source/meta hashes are identical before and after the move and every
  script GUID is preserved. The slice contains no UnityEngine, World, Combat, UI,
  or runtime-service dependency. PortData remains `noEngineReferences: true` and
  references only Core and Model.
- The structural AssetDatabase import wrapper reached its 60-second client timeout,
  but Unity subsequently reported a new completed domain reload, idle/ready state,
  and no compilation error. No redundant Refresh followed. Focused PortData
  EditMode job `d2f09390ba1348ffa5969e1b1bbd653c` passed 116/116, failed 0,
  skipped 0, in 2.1096638 seconds. Boundary PlayMode job
  `eff508f64776423fb1b4dfb61d918d9c` passed `SandboxBootE2ETests` 10/10 in
  211.9898604 seconds.
- The post-PlayMode Console contained only expected runtime warnings and the Test
  Runner result-save bookkeeping entry; the error-only query returned no compile or
  test failure. The Console was then cleared once. The production asmdef graph
  passed `tsort` with exit 0, and `srcwalk review --scope Assets/Scripts/PortData`
  reported only the expected moved/add evidence for this dirty-tree slice.
- After PortData wave 3, Sandbox contains 600 C# files / 83,515 LOC and PortData
  contains 66 C# files / 6,540 LOC. Generated assembly sizes are 2,023,936 bytes
  for `VLTK.Sandbox.dll` and 124,416 bytes for `VLTK.PortData.dll`. These are size
  observations only and are not a compile-speed claim.
- Runtime identity alignment renamed the existing assembly from `VLTK.Sandbox` to
  `VLTK.Sandbox.Runtime`. The source folder, `VLTK.Sandbox` namespace, asmdef asset
  path/meta, script files, and script GUIDs remain unchanged. UI, main EditMode,
  PlayMode, and PortFactorySmoke references plus PortData's `InternalsVisibleTo`
  friend name were updated. Five PortFactory reflection helpers now target the new
  assembly name while continuing to resolve the unchanged namespace.
- The first compile request reported `refresh_triggered=false`: Unity rebuilt
  PortData's new friend declaration while Bee still targeted cached
  `VLTK.Sandbox.dll`, producing 48 expected intermediate `PcText is inaccessible`
  errors. One subsequent forced all-asset import reported `refresh_triggered=true`,
  created a new domain reload, and compiled the renamed graph cleanly. This was a
  cache/import transition, not a dependency-direction change; no additional refresh
  followed.
- Runtime inspection proved `typeof(SandboxManager).Assembly.GetName().Name` is
  `VLTK.Sandbox.Runtime`, no `VLTK.Sandbox` assembly is loaded, the new Runtime DLL
  exists at 2,023,936 bytes, and the old ScriptAssemblies DLL is absent. The
  production asmdef graph passed `tsort` with exit 0. The active Sandbox scene,
  Player prefab, and NpcDialogue prefab loaded with zero missing MonoBehaviours.
  Enemy prefab retained its two pre-existing missing-script entries; no new missing
  binding was attributed to the rename.
- Focused EditMode job `8b753a2cfa71441e8b68c3f6f894dcca` passed
  `SandboxManagerFastBootTests` plus `SkillContentTests` 23/23 in 4.0141253 seconds.
  Focused PortFactory PlayMode job `0ecf0845ebe2465db1a48ef2c0663c00`
  passed five representative named/reflection consumer tests 5/5 in 0.4909619
  seconds. Boundary PlayMode job `f70e05b053db491d81bf9721388dd621`
  passed `SandboxBootE2ETests` 10/10 in 183.1653377 seconds.
- A broader PortFactory run `7225dce5ed4b40a9a96c16757f94faee` executed 46
  tests and exposed five existing Compensation/Dialog API or data-expectation
  failures while 41 completed without a recorded failure. The failures concern
  missing/stale `IsValid`, `HasScript`, option/source surfaces, and an expected
  7-versus-9 count; they are retained as gaps and are not presented as rename
  regressions or a green full assembly.
- After the test runs, the Console error-only query contained only the Test Runner
  `TestResults.xml` save bookkeeping entry. The earlier `SandboxPlayerController`
  OnValidate exception produced during import did not recur after clearing and
  exercising the focused suites. The final Console was cleared once.
- PortData wave 4 moved 11 clean Guild/Tong parser source/meta pairs (1,022 LOC)
  into the existing PortData assembly: Guild rank, workshop level, task, city-war
  log, task definition, stunt, workshop, Tong level, NPC position, settings, and
  stunt. All 22 before/after SHA-256 values are identical and every script GUID is
  preserved. `PcTongListParser` was deliberately excluded because its map-entry
  ownership and sole batch-loader consumer place it in World rather than PortData.
- The wave adds no asmdef and no edge. The moved sources contain no UnityEngine,
  UnityEditor, World, Combat, UI, MonoBehaviour, or Application dependency; they use
  only System, Core/Model, and PortData-owned helpers. One forced all-asset import
  completed a normal compilation/domain reload with zero Console errors; no second
  refresh followed. Focused EditMode job `5cba66e86e1649139ed4665128173e6e`
  passed the selected Guild/Tong parser and service fixtures 80/80 in 19.2495616
  seconds. Boundary PlayMode job `9f2fd299e2db4a9ab81c7380e79fd01c`
  passed `SandboxBootE2ETests` 10/10 in 281.6676353 seconds.
- A separate dead-code audit required zero callers/dependents, zero exact reflection
  or string lookup, zero serialized GUID references, no Unity lifecycle root, and a
  clean source/meta pair. It approved and removed only `MapPortValidator`,
  `SpawnBatchManager`, and `ThiefSkillService` with their meta files (256 LOC). The
  cleanup did not remove the still-used region parser, spawn runtime, thief parser,
  DTO, registry, or any test.
- The dead-code import wrapper timed out after 60 seconds, but Unity reconnected with
  a new domain reload and reported idle/ready with zero Console errors; no redundant
  refresh followed. Focused `CombatSystemTests`, `CoverageSmokeTests`, and
  `ServiceSelfCheckTests` job `1ad1d92289fb4096bcc8b6d197c210ad` passed 15/15 in
  1.6931506 seconds. Boundary PlayMode job `0e478d2ac5f74c15b352b37903bbb352`
  passed `SandboxBootE2ETests` 10/10 in 253.8525402 seconds despite one transient MCP
  transport reconnect. Final Console contained only Test Runner result-save
  bookkeeping and was cleared once.
- After PortData wave 4 and the bounded dead-code cleanup, Sandbox.Runtime contains
  586 C# files / 82,237 LOC and PortData contains 77 C# files / 7,562 LOC. Generated
  DLL sizes are 1,989,120 bytes for `VLTK.Sandbox.Runtime.dll` and 154,624 bytes for
  `VLTK.PortData.dll`. These are size observations, not a compile-speed claim.
- World wave 3 moved six clean boss/siege parser source/meta pairs (631 LOC) into
  the existing `VLTK.World` assembly: world boss, gold boss, Hoàng Kim boss, city
  defence, city war, and Bang Chien. All 12 source/meta hashes are identical
  before/after and every script GUID is preserved. Sandbox consumers continue to
  reference these types through the existing one-way `Sandbox.Runtime -> World`
  edge; no World-to-Sandbox or Combat edge was added.
- The moved sources use only System, Model, Core, and PortData helpers. They contain
  no UnityEngine, UnityEditor, UI, Combat, MonoBehaviour, or Application dependency.
  The normal full import/domain reload completed with zero Console errors and no
  redundant refresh. Focused World/boss/siege EditMode job
  `b14b812638024ca1a98041f66c5a22b0` executed 161 tests: 157 passed, 0 failed,
  and 4 pre-existing TODO tests were ignored. Boundary PlayMode job
  `5d1841f29e4542ac9547e46343214c40` passed `SandboxBootE2ETests` 10/10 in
  172.0004658 seconds.
  - After World wave 3, Sandbox.Runtime contains 580 C# files / 81,606 LOC and World
  contains 47 C# files / 5,018 LOC. Generated DLL sizes are 1,978,880 bytes for
    `VLTK.Sandbox.Runtime.dll`, 121,344 bytes for `VLTK.World.dll`, and 154,624 bytes
    for `VLTK.PortData.dll`. These remain size observations, not a compile-speed claim.
  - World wave 4 moved four clean mission-placement parser source/meta pairs (387 LOC)
    into `VLTK.World`: mission arena, maze, Qianchonglou, and mission battle matrices.
    All eight source/meta hashes are identical before/after and GUIDs are preserved.
    Arena/Qianchong/Battle use the World-owned map-list reader; Maze uses the
    PortData-owned text reader. No Combat or Sandbox dependency was introduced.
  - The normal full import/domain reload completed with zero Console errors and no
    redundant refresh. Focused mission EditMode job
    `dd8bfe497d824edfb4697ad5a98d2451` passed 11/11 in 0.9740155 seconds. Boundary
    PlayMode job `c4bfa9849c514a66b48c5cc493b45a82` passed
    `SandboxBootE2ETests` 10/10 in 169.3766714 seconds. The final Console contained
    only Test Runner result-save bookkeeping and was cleared once.
  - After World wave 4, Sandbox.Runtime contains 576 C# files / 81,219 LOC and World
    contains 51 C# files / 5,405 LOC. Generated DLL sizes are 1,967,104 bytes for
    `VLTK.Sandbox.Runtime.dll`, 132,608 bytes for `VLTK.World.dll`, and 154,624 bytes
    for `VLTK.PortData.dll`. These remain size observations, not a compile-speed claim.
  - The current test topology has five assemblies, with no one-fixture micro-asmdef:
    broad `VLTK.Tests.EditMode` owns 313 source files; `VLTK.Tests.PlayMode` owns 6;
    `PortFactorySmoke` owns 13; dedicated `VLTK.Tests.PortData.EditMode` owns 14;
    and dedicated `VLTK.Tests.Gameplay.Domain.EditMode` owns 2. The two dedicated
    assemblies reference only their production layer plus Model (and test runner);
    the broad assembly remains the integration bucket for mixed Sandbox/UI/World/
    Combat fixtures. This is an ownership matrix, not a claim that every fixture has
    been independently audited.
  - A comparable normal-compile speed percentage is still intentionally unresolved.
    Current Bee/DLL observations are after-slice snapshots only. Completion requires
    isolated before/after project copies with the same Unity `6000.4.7f1`, package
    lock, ProjectSettings, Debug optimization mode, cache protocol, source invalidation
    fixture, and repeated wall-time/Bee critical-path samples. No folder layout or DLL
    size delta is being presented as speed improvement.
  - Current after-only measurement: `/usr/bin/time python3 scripts/compile_scripts.py`
    on Unity `6000.4.7f1` LinuxEditor reported `WALL_SECONDS=46.28`, requested a
    full Debug compile, and returned the editor to idle/ready with zero Console
    errors. This sample is retained as a reproducible post-slice observation only;
    it is not comparable-before/after evidence.
  - A candidate seven-file source-index move was deliberately rejected and reverted
    after normal compilation exposed that its DTO/registry types still live inside
    Sandbox service files (`PcDialogSysSourceIndexEntry`, `PcLibScriptSourceCatalog`,
    `PcMissionScriptSourceCatalog`, `PcShopScriptIndexRegistry`,
    `PcFlipCardProtocolRegistry`, and related types). The parser/meta bytes were
    restored exactly; no dependency edge or behavior change was retained. This is a
    proof that the boundary rules reject parser-only moves when ownership is not yet
    cohesive, rather than hiding the coupling with a reverse reference.
  - PortData wave 5 moved 16 clean reward/economy/event parser source/meta pairs
    (1,555 LOC) from Sandbox into the existing `VLTK.PortData` assembly. All 32
    files are `R100`, source/meta bytes match their `HEAD` originals, all 16 script
    GUIDs are preserved, namespaces and bodies are unchanged, and no production
    asmdef edge was added. The slice excludes every dirty combat/parity file.
  - The first compile request reused the stale pre-move source graph and reported
    CS2001 missing-source errors. The required full AssetDatabase import then exposed
    a real test topology gap: `HongbaoRuntimeBehaviorTests` directly consumes moved
    PortData types. Adding the one-way `PortFactorySmoke -> VLTK.PortData` reference
    completed normal compilation with zero C# errors. No production reverse edge was
    introduced.
  - Focused EditMode job `9ef0c5fd5d7e40b6bc83f9708272daa6` passed 44/44 in
    16.9538036 seconds. Backend PlayMode job
    `bc1786f39cee49ec9877e7aa4f4e22aa` passed the complete
    `CombatFeedbackPlayModeTests` fixture 5/5 after correcting its contradictory
    setup from `runCombatDemoOnComplete=false` to `true`; production code and all
    assertions remain unchanged. Boundary PlayMode job
    `81941b1362a64a0cbef6112db0808b85` passed `SandboxBootE2ETests` 10/10 in
    173.0096966 seconds.
  - The full refresh also surfaced two out-of-slice GUID conflicts between immutable
    Addressables package test assets and `PcMap/wharf_sample.txt` / `scroll_sample.txt`.
    They are recorded as pre-existing external proof noise; they are not C# compile or
    test failures and were not modified by this refactor.
  - `scripts/check_unity_assembly_boundaries.py` now enforces the declared production
    layer direction, explicit VLTK assembly classification, named and GUID-resolved
    cycles/forbidden edges, malformed GUID references, asmdef schema validity, and
    script/meta GUID presence/uniqueness. `scripts/measure_unity_debug_compile.py`
    records Bee/wall trials and rejects empty, under-sampled, cross-series-mismatched,
    or internally heterogeneous metadata before a performance verdict. Their combined
    coordinator suite passed 55/55 and the live project boundary guard returned clean.
  - A comparable before/after compile-speed percentage remains unresolved by design:
    no isolated pre-wave series exists under the same Debug/AutoRefresh/cache/
    invalidation protocol. The measurement tool requires at least five homogeneous
    trials per side and will not manufacture a claim from the current after-only Bee
    artifacts.
    - Fresh guarded Herdr run `orch-cdb518188956a884` used independent scout, writer,
      reviewer, and proof-auditor lanes. The proof audit re-derived the 16-pair integrity,
      required test edge, fixture root cause, and Python guard behavior. It also separated
      four older source-index metas reserialized by Unity from the exact 16-pair wave-5
      acceptance claim rather than overstating all working-tree moves as byte-identical.
    - Test-topology slice 2026-07-18 moved nine cohesive pure World parser fixtures and
      their metas from the broad Sandbox EditMode bucket into the dedicated
      `VLTK.Tests.World.EditMode` assembly. Git reports all 18 moves as `R100`; direct
      `cmp` against each `HEAD` source/meta succeeded, so fixture bodies and Unity GUIDs
      are preserved. The assembly references only Unity test runner, NUnit, Core, Model,
      and World. Combat remains in the broad EditMode assembly because its two clean
      fixtures do not justify a one-feature micro-assembly yet.
    - World EditMode job `59b7f404233640dbaf26766ae63118d4` executed 99 tests in
      1.5053357 seconds: 95 passed, 0 failed, and 4 existing TODO cases were ignored.
      Daily focused PlayMode job `fb610da8837b4889a759dd106f20943a` ran only
      `E2E_DefaultMap_IsBaLangHuyen` and passed 1/1 in 16.420665 seconds. Boundary job
      `25237a5a964e4b55a735dccae7635de6` retained the complete
      `SandboxBootE2ETests` proof and passed 10/10 in 238.9363186 seconds. The final
      Console contained only Unity Test Runner's `Saving results to .../TestResults.xml`
      bookkeeping entry, classified by the MCP surface as `Exception`; there was no C#
      compile or test failure.
    - `scripts/run_unity_test_profile.py` now selects `fast`, `focused`, or `boundary`
      from changed paths. Added/deleted/renamed files, asmdef/asmref/meta, scenes,
      prefabs/assets, Packages, ProjectSettings, and unknown Assets/scripts paths fail
      closed to boundary. An explicit profile cannot weaken the automatically required
      proof. Boundary renders a full Debug compile plus the complete SandboxBoot fixture;
      focused renders the owning EditMode assembly plus the one-test PlayMode smoke.
    - The runner's execution path now accepts the actual paged `read_console` response
      shape and rejects failed/cancelled/timed-out/inconclusive/zero-test Unity jobs. Its
      focused end-to-end execution completed successfully for a World parser path. The
      combined Python policy/measurement/boundary suite passes 102/102, the live assembly
      guard is clean, `git diff --check` passes, and srcwalk review reports only the
      intended runner and World-test topology changes in those scopes.
    - Adversarial Herdr review found and the coordinator closed four additional
      false-fast routes: verification-policy self-edits, `--changed-from` excluding dirty
      work, non-C# assets inside an otherwise mapped code prefix, and boundary plans that
      dropped the owning EditMode assembly. Plain `--path` values without a status now
      fail closed; callers use `M:path` for a known method-body edit. Unity terminal
      summaries require coherent counters and at least one pass, while accepting the real
      `Skipped:Ignored` aggregate only when passed tests coexist with ignored cases.
      Compound porcelain statuses inspect both index/worktree columns; cross-owner renames
      retain both source and destination owners; MCP operations require explicit success;
      Console deltas compare message multiplicity and exempt only an exact TestResults.xml
      save entry. Post-fix runner execution passed World EditMode 95/99 (4 ignored) in
      1.444 seconds and the focused PlayMode smoke 1/1 in 10.265 seconds, including
      post-test Console verification.
    - These measurements prove a smaller daily verification path, not a comparable
      normal compile-speed percentage. Structural changes still require normal full
      compilation and the 10-test boundary smoke; the earlier five-trial, metadata-matched
      Bee protocol remains the gate for any compile-speed claim.
