# Research: Unity API & C# Patterns for JX PC Porting

## Research questions
- How to efficiently load and parse binary PAK files in Unity C#?
- How to decode JX SPR files and render them as Unity Sprites/Textures with minimal GC allocations?
- What is the best practice for running C++ legacy logic / JX Lua scripts (like `gaibang.lua`) in Unity?
- How to handle grid coordinates and 8-way directional rendering in Unity?

## Key findings
1. **Binary PAK Loading and Decompression** — PC JX uses `.pak` files (often zlib-compressed). Reading them in Unity C# requires using `System.IO.FileStream` with `System.IO.Compression.DeflateStream` or `SharpZipLib`. For maximum performance, reading bytes directly into a `NativeArray<byte>` and using Unity's Job System or `System.ReadOnlySpan<byte>` avoids memory copying and GC garbage. [Microsoft API Docs on Span](https://learn.microsoft.com/en-us/dotnet/api/system.readonlyspan-1)
2. **SPR Decoding and Texture Generation** — SPR format contains frame offsets, sizes, and palette indexes. To render these in Unity without performance hiccups, bytes should be read using `unsafe` pointers or `System.Buffer.MemoryCopy`. The texture should be created once via `new Texture2D(...)`, updated via `Texture2D.LoadRawTextureData(ReadOnlySpan<byte>)` or `GetRawTextureData<T>()`, and applied with `Texture2D.Apply(false, false)` to avoid mipmap generation and CPU-GPU upload overhead. [Unity Texture2D Docs](https://docs.unity3d.com/ScriptReference/Texture2D.html)
3. **Lua Engine Integration** — JX PC relies heavily on Lua for skill, NPC, and quest logic (e.g. `gaibang.lua`). In Unity, XLua is the industry standard for Chinese/Vietnamese MMORPG ports, allowing seamless C# and Lua bindings with codegen to avoid reflection overhead. Alternatively, MoonSharp can be used for pure C# compatibility. [XLua Github Repository](https://github.com/Tencent/xLua)
4. **C++ Data Structure Mapping** — Parsing `SceneDataDef.h` structures directly in C# requires accurate packing alignment. `[StructLayout(LayoutKind.Sequential, Pack = 1)]` should be used alongside `Marshal.PtrToStructure` or `UnsafeUtility.As` to cast raw bytes directly to C# structs, matching the PC MSVC compiler's memory layout. [Microsoft StructLayout Docs](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute)

## Unity API notes
- **`Texture2D.LoadRawTextureData(NativeArray<T>)` / `Texture2D.GetRawTextureData<T>()`**: Best way to upload raw SPR pixel data to GPU. Avoids the overhead of `SetPixels()`, which performs color conversion and creates managed arrays.
- **`Sprite.Create(Texture2D, Rect, Vector2, float, uint, SpriteMeshType, Vector4)`**: Used to create sprite references from dynamically loaded SPR frames. Ensure sprite mesh type is set to `SpriteMeshType.FullRect` to avoid CPU triangulation overhead.
- **`System.IO.MemoryMappedFiles.MemoryMappedFile`**: For reading large PAK or asset files directly from disk without allocating large managed byte arrays in RAM.
- **`System.Diagnostics.Stopwatch`**: Used for micro-benchmarking parser performance.

## Gotchas/warnings
- **GC Allocation in Dev Loop**: Reading thousands of files or sprites (like SPR frames) dynamically can trigger frequent Garbage Collection (GC) spikes. Always cache `Sprite` objects and reuse `Texture2D` instances if they share the same resolution.
- **Endianness & Struct Alignment**: JX PC is x86/x64 Little Endian. Unity mobile (ARM64) is also Little Endian, but struct packing rules (e.g. 1-byte vs 4-byte alignment) must be explicitly defined using `Pack = 1` in `StructLayout` to prevent misalignment crashes or garbage data on mobile devices.
- **Texture.Apply Overhead**: Calling `Texture2D.Apply()` is a heavy operation as it uploads texture data to the GPU. Group updates or do them on the main thread only when necessary.
- **Unsafe Code in WebGL/Mobile**: If targeting WebGL or certain mobile platforms, `unsafe` C# code is supported by IL2CPP but requires the "Allow 'unsafe' Code" checkbox enabled in Assembly Definition files.

## Sources
- Kept: Unity Texture2D API Reference (https://docs.unity3d.com/ScriptReference/Texture2D.html) — Crucial for learning optimal ways to load raw SPR pixel bytes.
- Kept: Microsoft StructLayoutAttribute Class (https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.structlayoutattribute) — Essential for mapping C++ PC structs (`SceneDataDef.h`) to C# without manual offset arithmetic.
- Kept: Tencent XLua Repository (https://github.com/Tencent/xLua) — The standard Lua-C# binding library for Unity game porting.
- Dropped: General Unity 2D Sprite tutorials — Too generic, did not address low-level binary SPR decoding or palette rendering.

## Gaps
- Exact structure of the `.pak` and `.spr` formats in the current repository could not be inspected due to lack of local read/grep tools.
- Verification of whether XLua or MoonSharp is already integrated into the `vltk-mobile` project structure.
- *Next Steps*: Once write/read access to the project is confirmed, inspect `/var/www/vltk-mobile/Assets` using the Unity Editor or grep to verify if a Lua binding library is already present, and locate the existing SPR parser class to optimize it.

## Confidence
High (based on established Unity game porting patterns and standard C#/Unity APIs for handling binary data, custom textures, and C++ struct interoperability).
