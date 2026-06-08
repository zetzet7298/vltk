// -----------------------------------------------------------------------------
// VLTK Mobile — Tong map entrance catalog fields/properties.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionCatalogEntry
    {
        public bool IsTongMapEntrance => string.Equals(actionKind, "TongMapEntrance", StringComparison.OrdinalIgnoreCase);
    }
}
