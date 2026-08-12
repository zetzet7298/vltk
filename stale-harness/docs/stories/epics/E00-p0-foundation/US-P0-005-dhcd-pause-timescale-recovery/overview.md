# Overview

## Current Behavior

The hashed Android controller proves a role-keyed pending-event path for random-skill requests. Hash-locked metadata/pointer-table mapping now connects the normal card UI's `OnVisible`/`OnHidden` callers to `BattleSys.set_IsPause`, but runtime scope and timer/input effects remain unresolved.

## Target Behavior

Produce a reviewer-auditable evidence boundary for global pause, local input lock, and timer behavior. Preserve `unresolved` and block parity implementation until the remaining caller scope and runtime behavior are corroborated.

## Affected Users

- Gameplay and multiplayer owners relying on correct card-choice timing.
- Reviewers checking DHCD parity claims.

## Affected Product Docs

- `specs/dhcd-jx-port/03-gameplay/deck-timeline.md`
- `specs/dhcd-jx-port/10-research/dhcd-reverse-queue.md`
- `specs/dhcd-jx-port/10-research/unresolved-rules.md`
- `/home/zet/Projects/dhcd/docs/evidence/r-dhcd-003-pause-timescale.md`

## Non-Goals

- No Unity `Time.timeScale` or input-lock implementation.
- No claim based on malformed C# or type names.
- No global pause product decision.
