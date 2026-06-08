// -----------------------------------------------------------------------------
// VLTK Mobile — ClearSkill TeamEnterHole catalog fields/properties.
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionCatalogEntry
    {
        public bool IsClearSkillTeamEnterHole => string.Equals(actionKind, "ClearSkillTeamEnterHole", StringComparison.OrdinalIgnoreCase);
    }
}
