// -----------------------------------------------------------------------------
// VLTK Mobile — Chuyển Sinh level bonus service
// Wraps PC settings/task/metempsychosis/translife.txt (level table 160..200).
// Deliberately separate from TranslifeSkillService/translifeskill.txt schema.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TranslifeLevelService
    {
        public const string LogTag = "TranslifeLevel";
        public const string DefaultStreamingDir = "Reference/PcTask";

        private PcTranslifeLevelRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;

        public TranslifeLevelService() : this(null) { }

        public TranslifeLevelService(PcTranslifeLevelRegistry registry)
        {
            RegisterRegistry(registry);
        }

        public void RegisterRegistry(PcTranslifeLevelRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"Chuyển Sinh level table loaded: {Count} cấp");
        }

        public PcTranslifeLevelEntry GetLevel(int level)
            => _registry != null ? _registry.Get(level) : null;

        public static TranslifeLevelService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            var svc = new TranslifeLevelService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcTranslifeLevelParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"TranslifeLevelService: directory không tồn tại {dir}");
            }
            return svc;
        }
    }
}
