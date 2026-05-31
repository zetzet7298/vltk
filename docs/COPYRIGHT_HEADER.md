# Copyright Header Template

Every new source file in this proprietary project must start with a copyright
header. Copy the block for the matching language to the very top of the file.

## C# / C++ / shader (`.cs`, `.cpp`, `.h`, `.shader`)

```csharp
// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------
```

## Python / shell / YAML (`.py`, `.sh`, `.yml`)

```python
# -----------------------------------------------------------------------------
# VLTK Mobile
# Copyright (c) 2026 vltk. All rights reserved.
# Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
# -----------------------------------------------------------------------------
```

## Rules

- Keep the year as the file's creation year; do not churn it on every edit.
- Do not add headers to generated files, third-party code, or `jxwin-kinnox/`
  reference material (that source belongs to the original rights holders — see
  `NOTICE.md`).
- Place the header above `using` / `#include` / `import` statements.
- When you add a notable change, also update `CHANGELOG.md` under `[Unreleased]`.

## Releasing

1. Move `[Unreleased]` entries under a new `## [x.y.z] - YYYY-MM-DD` section.
2. Bump `bundleVersion` in `ProjectSettings/ProjectSettings.asset` to match.
3. Tag the release: `git tag vX.Y.Z`.
