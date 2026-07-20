# Overview

## Current Behavior

`Assets/Scripts/Sandbox/VLTK.Sandbox.asmdef` owns hundreds of scripts across
unrelated gameplay, PC-data porting, scene orchestration, and support services.
Changes inside the assembly incur a large compile unit and can affect UI and test
assemblies that reference it.

## Target Behavior

Sandbox is reduced through small, cohesive runtime assemblies with explicit
one-way dependencies. Each extraction preserves runtime behavior and Unity GUIDs,
returns the Editor to a clean compiling state, and carries comparable compile and
test evidence.

## Affected Users

- Developers and agents iterating in the Unity Editor.
- Players indirectly protected by preserved gameplay behavior.

## Affected Product Docs

- `AGENTS.md`
- `docs/decisions/0008-unity-refactor-fast-iteration-guardrails.md`
- `docs/decisions/0009-unity-sandbox-decomposition-and-iteration-loop.md`

## Non-Goals

- Rewriting gameplay systems.
- Porting additional PC behavior during structural extraction.
- Claiming compile improvement without measured before/after evidence.
