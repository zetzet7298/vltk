# 0006 Unity 6 + C# Mobile Stack

Date: 2026-05-30

## Status

Accepted

## Context

The VLTK Mobile project ports Võ Lâm Truyền Kỳ (JX Online 3) from its
original PC stack (C++/DirectX/Lua) to mobile platforms (Android and iOS).

The mobile runtime must handle:

- **2D sprite rendering** — the game uses a custom SPR sprite format with
  palette-indexed frames, directions, and animation sequences.
- **Binary format parsing** — PAK archives, SPR sprites, MAP tiles, and other
  proprietary binary formats must be decoded at runtime or build time.
- **Lua scripting** — the original game logic is heavily driven by Lua scripts;
  preserving compatibility reduces porting effort.
- **Mobile deployment** — the final product must ship on iOS and Android with
  acceptable performance on mid-range devices.

## Decision

Use **Unity 6 LTS (6000.4.7f1)** with **C#** as the primary language.

Key technology choices:

| Concern | Choice |
| --- | --- |
| Engine | Unity 6 LTS (6000.4.7f1) |
| Language | C# (.NET Standard 2.1) |
| UI framework | uGUI (Canvas + RectTransform) |
| Lua runtime | MoonSharp (pure-C# Lua 5.2 interpreter) |
| 2D rendering | SpriteRenderer-based pipeline (Built-in RP) |
| Android backend | IL2CPP (AOT compilation) |
| iOS backend | IL2CPP |

## Alternatives Considered

1. **Godot 4** — lighter engine with lower overhead, but mobile tooling is less
   mature and C# support (via .NET module) is still stabilizing. Smaller
   ecosystem for debugging and profiling tools.

2. **Custom engine** — maximum control over rendering and binary format
   handling, but requires building an entire mobile pipeline (input, audio,
   rendering, platform abstraction) from scratch. Too much infrastructure work
   for a port project.

3. **Cocos2d-x** — C++ native which aligns with the original codebase, but the
   ecosystem is significantly smaller, iteration speed is slower, and tooling
   for asset inspection and debugging is limited compared to Unity.

## Consequences

Positive:

- Mature mobile build pipeline with well-documented iOS and Android deployment.
- C# is productive for porting C++ game logic — similar OOP model, strong
  typing, and garbage collection reduce manual memory management.
- uGUI works reliably on mobile with touch input and screen adaptation.
- Large Asset Store ecosystem provides debugging, profiling, and utility tools.
- IL2CPP produces near-native performance on both Android and iOS.

Tradeoffs:

- Unity license cost applies when revenue exceeds the threshold (Unity
  Personal is free below the revenue cap).
- Unity's 2D rendering is heavier than specialized 2D engines — SpriteRenderer
  batching and draw call management require attention.
- SPR/PAK/MAP binary format parsing requires custom C# tooling since these are
  proprietary formats not supported by any Unity package.

## Follow-Up

- Evaluate URP (Universal Render Pipeline) for sprite rendering performance if
  the Built-in RP shows bottlenecks with large sprite counts.
- Evaluate Addressables vs Resources API for asset loading strategy — PAK
  archives may map better to one approach over the other.
