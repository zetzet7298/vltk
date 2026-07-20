# 0008 Unity Refactor And Fast-Iteration Guardrails

Date: 2026-07-18

## Status

Accepted

## Context

The Unity project already uses assembly definitions, but `VLTK.Sandbox` contains
hundreds of scripts and dominates compilation time. Future refactor work will be
performed by multiple agents across multiple sessions, so conversational guidance
is insufficient. Assembly moves can also break Unity GUID references, introduce
dependency cycles, or appear faster while silently weakening test coverage.

## Decision

Make `AGENTS.md` the durable entrypoint for Unity refactor and iteration rules.
All agents and subagents must:

- shrink compile blast radius through cohesive, one-way assembly boundaries;
- inspect symbol and dependency evidence before moving scripts or editing asmdefs;
- preserve `.meta` files and separate boundary moves from behavior changes;
- use focused EditMode proof for pure logic and full Unity proof for structural or
  engine-boundary changes;
- treat hot reload only as an optional inner-loop tool;
- measure compile-performance work before and after; and
- retain the full test path instead of weakening tests for speed.

Global Editor settings, scripting defines, and package installation remain outside
an agent's authority unless the current user request explicitly includes them.

## Alternatives Considered

1. Keep these rules only in the current conversation. Rejected because future
   agents and subagents would not receive durable guidance.
2. Put the rules only in a refactor plan. Rejected because `AGENTS.md` is the
   bounded authority entrypoint read before task-specific planning.
3. Mandate a particular hot-reload package. Rejected because package installation
   is a separate project mutation and hot reload cannot prove structural changes.

## Consequences

Positive:

- Future refactor slices start from consistent assembly and validation rules.
- Compile-time improvements require evidence rather than folder-only claims.
- Unity GUIDs, engine-boundary proof, and test coverage receive explicit protection.

Tradeoffs:

- Assembly refactors require more discovery and incremental validation.
- Agents cannot silently optimize their local loop by changing global project or
  Editor settings.

## Follow-Up

- Use these rules to plan and execute the first bounded `VLTK.Sandbox` extraction.
- Revisit assembly granularity after measured compile traces from the first slices.
