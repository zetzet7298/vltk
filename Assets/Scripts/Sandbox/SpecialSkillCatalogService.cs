// -----------------------------------------------------------------------------
// VLTK Mobile — PC skills1.txt special-skill script catalog service.
// Source: Server 6.0/server/home_jxser/server1/settings/skills1.txt filtered by
// LvlSetScript prefix "\\script\\skill\\special".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class SpecialSkillCatalogService
    {
        private readonly PcSpecialSkillRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public int UniqueSkillIdCount => _registry != null ? _registry.UniqueSkillIdCount : 0;
        public int UniqueScriptCount => _registry != null ? _registry.UniqueScriptCount : 0;
        public IReadOnlyList<PcSpecialSkillEntry> All
            => _registry != null ? _registry.All : (IReadOnlyList<PcSpecialSkillEntry>)System.Array.Empty<PcSpecialSkillEntry>();

        public SpecialSkillCatalogService() : this(null) { }

        public SpecialSkillCatalogService(PcSpecialSkillRegistry registry)
        {
            _registry = registry ?? new PcSpecialSkillRegistry();
        }

        public PcSpecialSkillEntry Get(int skillId) => _registry.Get(skillId);

        public IReadOnlyList<PcSpecialSkillEntry> GetByScript(string script)
            => _registry.GetByScript(script);

        public static SpecialSkillCatalogService LoadFromStreamingAssets(string subdir = "Reference/PcSkill")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            return new SpecialSkillCatalogService(PcSpecialSkillParser.BuildRegistry(dir));
        }
    }
}
