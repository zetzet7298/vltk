# US-001 Prune JX/Unity Skills

## Status

implemented

## Lane

normal

## Product Contract

Project-local skills must live in the real directory
`/var/www/vltk-mobile/harness/.agents/skills` and use the current repository,
canonical PC corpus, resolver toolchain, and live Coplay MCP surface. Obsolete
or duplicate context must not remain discoverable. Stable research, navigation,
JX toolchain, and Unity automation entry points must be registered in Harness.

## Relevant Product Docs

- `AGENTS.md`
- `/var/www/vltk-mobile/harness/.agents/skills/*`

## Acceptance Criteria

- Every retained JX/Unity skill points only to verified current roots and files.
- Retained MCP examples match the live resource-first workflow.
- Duplicate, obsolete, imaginary, or unused instructions and helpers are removed.
- The canonical skill root is a real directory and alternate project roots are absent.
- Each retained skill passes structural validation and stale-reference scans.
- Harness lists the stable inbound tools and records their current presence.

## Design Notes

- Keep `jx-pc-port-rule` as the source hierarchy authority.
- Keep `jx-pc-resource-resolver` as the only resource-resolution workflow.
- Keep `unity-mcp-orchestrator` as the only Unity MCP operating workflow.
- Specialized skills contain only domain-specific deltas.
- Do not mirror project skills for individual agent clients.

## Validation

| Layer | Expected proof |
| --- | --- |
| Unit | Skill frontmatter and folder validation |
| Integration | Verified paths, commands, references, and live MCP names |
| E2E | Not applicable to agent documentation |
| Platform | Linux workspace path validation |
| Release | Reviewed diff with no legacy root or fake tool references |

## Harness Delta

No Harness policy change. This story records maintenance of project-local skills.

## Evidence

- Canonical root is a real directory containing only the nine retained skills.
- Alternate project skill roots, stale workspaces, archives, evals, and caches are removed.
- Active player verifier scripts use the canonical `verify_player.cs`.
- Harness registers `srcwalk`, `ketch`, `semble`, `unity-mcp`, and `vltktool-unpak`.
- `scripts/verify-us-001.sh` provides the release proof for this story.
- Live Unity MCP verification observed one ready Unity `6000.4.7f1` editor on
  `Assets/Scenes/Sandbox.unity`, with the current test schemas available.
- Trace `#2` passed the normal-lane context requirements; human correction is
  recorded as intervention `#1`.
