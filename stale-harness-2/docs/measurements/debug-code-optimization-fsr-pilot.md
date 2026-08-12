# Debug Code Optimization And Fast Script Reload Pilot

## Purpose

Measure and operationalize the fast Unity development loop without weakening the
normal compilation and test gates. Hot reload is an inner-loop accelerator, not
proof that an assembly, serialized type, scene, or build is valid.

## Environment

| Field | Observed value |
| --- | --- |
| Project | `/var/www/vltk-mobile` |
| Unity | `6000.4.7f1` |
| Editor instance | `vltk-mobile@244c0d539f780309` |
| Platform | Linux Editor; active build target `StandaloneLinux64` |
| Initial code optimization | `Release` |
| Current code optimization | `Debug` |
| Fast Script Reload before pilot | Not installed in `Packages` or `Assets` |
| FSR current pin | Git tag `1.8`; package metadata reports `1.6.1`, while source `VersionId` reports 1.8 |
| FSR upstream commit trial | `51140b71d9e5df1de231b33ec20ee089b18bebec`; package metadata 1.8.0 |
| FSR license | MIT upstream |

The mode was read and changed through
`UnityEditor.Compilation.CompilationPipeline.codeOptimization`. The transition
`Release -> Debug` triggered Unity's normal compilation/domain reload. No explicit
AssetDatabase refresh was requested. After reload the API still reported `Debug`
and the MCP Console error query returned zero entries.

## Supported Daily Loop

```text
enter Play once
  -> change only a supported method body
  -> let Fast Script Reload compile and patch it
  -> prove the same live instance observes the new revision
  -> run the smallest related EditMode assembly or fixture group
  -> at the feature/refactor boundary: normal full compile + relevant PlayMode smoke
```

Immediately fall back to normal Unity compilation for script moves, `.asmdef`
changes, fields or serialized state, inheritance, public API/type shape, generic
methods/types, or any failed/ambiguous hot reload.

## FSR Method-Body Pilot

Use a dedicated PlayMode tooling probe rather than a dirty gameplay file. The probe
must contain one private non-generic method returning an integer revision and an
`OnScriptHotReload` callback that logs the revision.

1. Pin and install FSR 1.8; retain the exact manifest and lock diff.
2. Record Auto Refresh before changing it. FSR requires Disabled or Enabled Outside
   Play Mode so Unity does not perform a normal domain reload for the method-body save.
3. Complete one normal full compile and confirm zero Console errors.
4. Enter Play once with the probe instance present.
5. Change only the private method body from revision 1 to 2, then 3 through 6.
6. For every save record save time, FSR reload-start time, proof-log time, errors,
   whether a normal domain reload occurred, and whether the original instance survived.
7. Exit Play, allow a normal full compile, run the focused EditMode group and relevant
   PlayMode smoke, and restore the prior Auto Refresh setting if the pilot fails.

Success requires all of the following for five consecutive edits:

- No normal Unity assembly/domain reload caused by the save.
- The existing Play session and probe instance survive.
- The exact new revision is observed by runtime behavior, not only an FSR success log.
- No compiler or Console error is introduced.

## Measurement Table

| Run | Mode | Tool | Revision | Save UTC | Reload start ms | Proof ms | Full domain reload | Instance retained | Errors | Result |
| --- | --- | --- | --- | --- | ---: | ---: | --- | --- | ---: | --- |
| 0 | Release | Unity | n/a | n/a | n/a | n/a | Expected on mode switch | n/a | 0 | Baseline mode observed |
| 0 | Debug | Unity | n/a | n/a | n/a | n/a | Expected on mode switch | n/a | 0 | Mode applied and retained |
| 1 | Debug | FSR tag `1.8` | 1 -> 2 | not retained | n/a | n/a | Not proven | Not proven | 1 compiler failure | Failed: generated response referenced a missing/stale `Microsoft.CSharp.dll` path |
| 2.1 | Debug | FSR tag `1.8` | 2 -> 3 | not captured | not captured | observed within MCP round trip | No | Yes, `-10579422` | 0 | Pass |
| 2.2 | Debug | FSR tag `1.8` | 3 -> 4 | not captured | not captured | observed within MCP round trip | No | Yes, `-10579422` | 0 | Pass |
| 2.3 | Debug | FSR tag `1.8` | 4 -> 5 | not captured | not captured | runtime state confirmed | No | Yes, `-10579422` | 0 | Pass |
| 2.4 | Debug | FSR tag `1.8` | 5 -> 6 | not captured | not captured | runtime state confirmed | No | Yes, `-10579422` | 0 | Pass |
| 2.5 | Debug | FSR tag `1.8` | 6 -> 7 | not captured | not captured | runtime state confirmed | No | Yes, `-10579422` | 0 | Pass |

## Current Pilot Status

The method-body pilot is **accepted for the narrow supported inner loop** on
2026-07-18. It is not proof for structural edits and does not replace normal
compilation or tests.

- Package remained pinned to Git tag `1.8`; the active cache fingerprint was
  `76a63e346ee198ddb2c45c49e78b370e699e1fea`, and its Roslyn directory contained
  `Plugins/Roslyn/Microsoft.CSharp.dll`.
- Code Optimization was set to Debug. Auto Refresh was set to mode 2 (Enabled
  Outside Play Mode) with `kAutoRefresh=true`; both survived domain reload and the
  final test runs.
- A runtime-only probe GameObject was created after entering Play. Baseline log:
  `[FSR-PILOT] instance=-10579422 revision=2`.
- Five separate filesystem saves changed only the private method-body literal:
  `2 -> 3 -> 4 -> 5 -> 6 -> 7`. Every revision was observed by live runtime state
  on instance `-10579422`.
- Throughout the five saves, `is_compiling=false`, domain reload was not pending,
  and `last_domain_reload_after_unix_ms` remained `1784340673775`. Unity's internal
  Console counters reported zero errors after the fifth edit.
- `read_console` timed out while scanning 9,435 accumulated log entries for the
  later revisions. Revisions 5-7 were therefore confirmed through the probe's
  runtime static state via reflection; this is stronger behavior evidence than an
  FSR success message alone.
- After exiting Play, one explicit normal script compile advanced the domain reload
  timestamp to `1784341078517` and completed with zero Console compile errors.
- Focused EditMode job `2878989fd6c4497388e38f2822599f15` passed 4/4 in
  1.068024 seconds. Final PlayMode job `cbd18e9416a84f7d84f4ca4eb6b48662`
  passed `SandboxBootE2ETests` 10/10 in 202.4614341 seconds.

Operational constraint: make hot-reload method-body saves through the IDE or plain
filesystem watcher path. A Unity MCP script-edit tool automatically imports and
requests compilation, so it belongs to the normal compile path; never follow it
with another Refresh. Fields, serialized state, inheritance, generic/API changes,
script moves, and asmdef edits always use normal compilation and boundary tests.

Do not publish a percentage improvement until equivalent-condition before/after
samples exist. Report median, p95, minimum, maximum, cache/invalidation state,
optimization mode, affected assemblies, and the raw evidence paths.

## Failure And Fallback

| Failure | Required action |
| --- | --- |
| FSR package does not compile on Unity 6000.4 | Remove the pinned package, restore manifest/lock, and use normal Debug compilation. |
| Unity performs a full reload after a method-body save | Stop the pilot, restore Auto Refresh, and diagnose watcher/reload interception. |
| New revision is not observed | Treat the pilot as failed even if FSR reports success. |
| Unsupported source pattern | Use normal Unity compilation; do not contort production code or add broad rewrite overrides. |
| Repeated edits become unstable or memory-heavy | Bound Play sessions and restart; reject FSR as the default loop if repeatability fails. |
| Structural change | Full Unity compile, focused tests, and relevant PlayMode smoke are mandatory. |

## Sources

- Unity live reflection: `CompilationPipeline.codeOptimization` is a writable static
  `CodeOptimization` property with `Debug`, `Release`, and `None` enum values.
- Fast Script Reload Asset Store listing: version 1.8, free, Unity 6 compatibility
  shown for `6000.0.23f1`.
- Upstream release 1.8: Unity 6 support added.
- Upstream package metadata: `com.handzlikchris.fastscriptreload` 1.8.0.
- Upstream documentation: generic methods/types are unsupported, new fields are
  experimental, and Auto Refresh must not trigger Unity compilation during Play.
