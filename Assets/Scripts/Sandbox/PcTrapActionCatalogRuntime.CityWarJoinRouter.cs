// -----------------------------------------------------------------------------
// VLTK Mobile — city-war transfer-map join router catalog fields/properties.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionCatalogEntry
    {
        public bool IsCityWarJoinRouter => string.Equals(actionKind, "CityWarJoinRouter", StringComparison.OrdinalIgnoreCase);
    }
}
