// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Special Skill Service (Kỹ Năng Đặc Biệt runtime)
// Wraps PcSpecialSkillRegistry. PC source: settings/specialskills.txt (58).
// Tra cứu skill đặc biệt theo id / môn phái.
// Vietnamese: "Kỹ Năng Đặc Biệt", "Tuyệt Kỹ", "Môn Phái".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý kỹ năng đặc biệt (Tuyệt Kỹ Môn Phái).
    /// PC source: settings/specialskills.txt.
    /// </summary>
    public class SpecialSkillService
    {
        public const string LogTag = "SpecialSkill";

        private PcSpecialSkillRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public SpecialSkillService() : this(null) { }

        public SpecialSkillService(PcSpecialSkillRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcSpecialSkillRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Kỹ Năng Đặc Biệt loaded: {Count} tuyệt kỹ");
        }

        public PcSpecialSkillEntry GetSpecialSkill(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IReadOnlyList<PcSpecialSkillEntry> GetByFaction(int factionId)
            => _registry != null
                ? _registry.GetByFaction(factionId)
                : (IReadOnlyList<PcSpecialSkillEntry>)Array.Empty<PcSpecialSkillEntry>();

        public IEnumerable<PcSpecialSkillEntry> GetAll()
            => _registry != null
                ? (IEnumerable<PcSpecialSkillEntry>)IterateAll()
                : (IEnumerable<PcSpecialSkillEntry>)Array.Empty<PcSpecialSkillEntry>();

        private IEnumerable<PcSpecialSkillEntry> IterateAll()
        {
            // Registry không expose "All"; quét qua các faction 1..10
            for (int f = 1; f <= 10; f++)
            {
                foreach (var e in _registry.GetByFaction(f))
                    yield return e;
            }
        }

        public static SpecialSkillService LoadFromStreamingAssets(string subdir = "Reference/PcSkill")
        {
            var svc = new SpecialSkillService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcSpecialSkillParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"SpecialSkillService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
