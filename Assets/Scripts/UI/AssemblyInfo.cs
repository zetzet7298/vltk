// -----------------------------------------------------------------------------
// VLTK Mobile — VLTK.UI assembly internals.
// Exposes internal seams (e.g. SkillContent.SelectSkill / TryUpgrade) to the
// EditMode test assembly so tests can drive interactive parity without relying
// on UI pointer-event flakiness. Mirrors the standard Unity test pattern.
// -----------------------------------------------------------------------------
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("VLTK.Tests.EditMode")]
