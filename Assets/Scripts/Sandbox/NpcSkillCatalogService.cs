// -----------------------------------------------------------------------------
// VLTK Mobile — NPC/Boss skill catalog service.
// Wraps the PC skills1.txt-derived npcskills.txt subset; data only, no AI executor.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class NpcSkillCatalogService
    {
        public const string LogTag = "NpcSkillCatalog";

        private readonly PcNpcSkillRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public int NpcScriptCount => _registry != null ? _registry.NpcScriptCount : 0;
        public int BossNameCount => _registry != null ? _registry.BossNameCount : 0;
        public int BossNameOnlyCount => _registry != null ? _registry.BossNameOnlyCount : 0;
        public IReadOnlyList<PcNpcSkillEntry> All
            => _registry != null ? _registry.All : (IReadOnlyList<PcNpcSkillEntry>)System.Array.Empty<PcNpcSkillEntry>();

        public NpcSkillCatalogService() : this(null) { }

        public NpcSkillCatalogService(PcNpcSkillRegistry registry)
        {
            _registry = registry ?? new PcNpcSkillRegistry();
        }

        public PcNpcSkillEntry Get(int skillId) => _registry.Get(skillId);

        public static NpcSkillCatalogService LoadFromStreamingAssets(string subdir = "Reference/PcSkill")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var reg = PcNpcSkillParser.BuildRegistry(dir);
            if (reg.Count > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC NPC/Boss skill catalog: {reg.Count} rows");
            else
                SubsystemLog.Warn(LogTag, $"PC NPC/Boss skill catalog missing at {dir}");
            return new NpcSkillCatalogService(reg);
        }
    }
}
