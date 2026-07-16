# Overview

## Current Behavior

The Android controller has role-keyed pending-event branches and named enqueue/dequeue targets, but player-data callee bodies, FIFO semantics, modal input lock, pause, and cross-player serialization are unresolved.

## Target Behavior

Recover request/response and modal lock semantics with native/runtime proof, or retain a fail-closed evidence boundary.

## Affected Users

- Players interacting with level-up/random-skill modals.
- Gameplay and multiplayer owners.

## Affected Product Docs

- `specs/dhcd-jx-port/03-gameplay/deck-timeline.md`
- `specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md`
- `/home/zet/Projects/dhcd/docs/evidence/r-dhcd-002-modal-queue.md`

## Non-Goals

- No assumed FIFO, input lock, global pause, or Unity UI implementation.
