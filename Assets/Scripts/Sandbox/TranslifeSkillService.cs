// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Translife Skill Service (Skill Chuyển Sinh runtime)
// Wraps PcTranslifeSkillRegistry. PC source: settings/translifeskill.txt (9).
// 4 cấp chuyển sinh × skill đặc biệt. Mỗi lần chuyển sinh mở khoá 1-2 skill mới.
// Vietnamese: "Chuyển Sinh", "Skill Chuyển Sinh", "Cảnh Giới".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý skill chuyển sinh (theo cảnh giới 1-4).
    /// PC source: settings/translifeskill.txt.
    /// </summary>
    public class TranslifeSkillService
    {
        public const string LogTag = "TranslifeSkill";

        public const int MinTranslifeLevel = 1;
        public const int MaxTranslifeLevel = 4;

        private PcTranslifeSkillRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public TranslifeSkillService() : this(null) { }

        public TranslifeSkillService(PcTranslifeSkillRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcTranslifeSkillRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Skill Chuyển Sinh loaded: {Count} skill theo cảnh giới");
        }

        public PcTranslifeSkillEntry GetTranslifeSkill(int id)
            => _registry != null ? _registry.Get(id) : null;

        public IReadOnlyList<PcTranslifeSkillEntry> GetByTranslifeLevel(int level)
            => _registry != null
                ? _registry.GetByTranslifeLevel(level)
                : (IReadOnlyList<PcTranslifeSkillEntry>)Array.Empty<PcTranslifeSkillEntry>();

        public static TranslifeSkillService LoadFromStreamingAssets(string subdir = "Reference/PcSkill")
        {
            var svc = new TranslifeSkillService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcTranslifeSkillParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"TranslifeSkillService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
