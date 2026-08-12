// -----------------------------------------------------------------------------
// VLTK Mobile — Chuyển Sinh skill source service.
// Wraps the PC skills.txt-derived 9-row translifeskill.txt subset; this is not
// the PcTask/translife.txt level bonus table and has no runtime unlock executor.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class TranslifeSkillSourceService
    {
        public const string LogTag = "TranslifeSkillSource";
        public const string DefaultStreamingDir = "Reference/PcSkill";
        public const string SourceFileName = PcTranslifeSkillSourceParser.SourceFileName;
        public const string PcSourceRelativePath = PcTranslifeSkillSourceParser.PcSourceRelativePath;

        private readonly PcTranslifeSkillSourceRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IReadOnlyList<PcTranslifeSkillSourceEntry> All
            => _registry != null ? _registry.All : (IReadOnlyList<PcTranslifeSkillSourceEntry>)System.Array.Empty<PcTranslifeSkillSourceEntry>();

        public TranslifeSkillSourceService() : this(null) { }

        public TranslifeSkillSourceService(PcTranslifeSkillSourceRegistry registry)
        {
            _registry = registry ?? new PcTranslifeSkillSourceRegistry();
        }

        public PcTranslifeSkillSourceEntry Get(int skillId) => _registry.Get(skillId);

        public static TranslifeSkillSourceService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var reg = PcTranslifeSkillSourceParser.BuildRegistry(dir);
            if (reg.Count > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC Chuyển Sinh skill source catalog: {reg.Count} rows");
            else
                SubsystemLog.Warn(LogTag, $"PC Chuyển Sinh skill source catalog missing at {dir}");
            return new TranslifeSkillSourceService(reg);
        }
    }
}
