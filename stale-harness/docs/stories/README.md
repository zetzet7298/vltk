# Stories

Stories are work packets. They turn product intent into bounded implementation
and validation work.

## Admitted Story Index

`E00-p0-foundation` is admitted. Its high-risk packets are indexed here; their
packet files remain the source for scope, evidence, and validation details.

| Story | Packet | Lifecycle/state |
| --- | --- | --- |
| US-P0-001 | Provenance evidence gate | implemented (packet contract) |
| US-P0-002 | Arena candidate provenance | in_progress |
| US-P0-003 | Reuse inventory | implemented (inventory contract) |
| US-P0-004 | DHCD card economy recovery | in_progress |
| US-P0-005 | DHCD pause/timeScale recovery | in_progress |
| US-P0-006 | DHCD mode-selection recovery | in_progress |
| US-P0-007 | DHCD modal-queue recovery | in_progress |
| US-P0-008 | DHCD drop/XP recovery | in_progress |

Implemented packet contracts do not imply a Unity/runtime implementation or
closure of the unresolved evidence recorded by their packets.

## Normal Story

Use `docs/templates/story.md` for normal feature work.

Suggested path:

```text
docs/stories/epics/E01-domain-name/US-001-short-story-title.md
```

## High-Risk Story

Use `docs/templates/high-risk-story/` when the feature intake classifies work as
high-risk.

Suggested path:

```text
docs/stories/epics/E02-risky-domain/US-012-risky-story-title/
  execplan.md
  overview.md
  design.md
  validation.md
```

## Status Flow

```text
planned -> in_progress -> implemented
                  |
                  v
               changed
                  |
                  v
               retired
```
