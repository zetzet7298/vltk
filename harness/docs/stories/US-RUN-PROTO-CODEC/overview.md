# US-RUN-PROTO-CODEC Overview

## Status

implemented

## Lane

high-risk

## Current Behavior

Before this story, the backend had no generated Python bindings, code-generation
command or binary framing for canonical `game.v1`. The current worktree now has
the bounded repo-local implementation described below.

## Target Behavior

Canonical `game.proto` reproducibly generates committed Python bindings and one
binary WebSocket message round-trips exactly one length-delimited envelope.

Current Harness completion accepts the implementation as a bounded repo-local
codec delivery. Staging/committing the generated artifacts remains an explicit
release-owner action because the shared backend worktree contains unrelated
changes.

## Affected Users

- Backend and Unity realtime implementers.

## Affected Product Docs

- `docs/decisions/0008-game-v1-runtime-authority.md`
- `/var/www/vltk-mobile/contracts/proto/game/v1/game.proto`

## Non-Goals

- Editing the canonical public proto.
- Owning runtime state or opening a production WSS endpoint.
