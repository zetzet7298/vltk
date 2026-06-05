// -----------------------------------------------------------------------------
// VLTK Mobile — Compensation runtime service (Bồi Thường)
// Quản lý gói bồi thường: phát item + bạc cho người chơi bị ảnh hưởng, có expireDate.
// Wraps PcCompensationRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class CompensationService
    {
        public const string LogTag = "Compensation";
        public const string DefaultStreamingDir = "Reference/PcCompensation";

        private PcCompensationRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcCompensationEntry> All => _registry?.All;

        public CompensationService() : this(null) { }

        public CompensationService(PcCompensationRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"CompensationService loaded {_registry?.Count ?? 0} gói bồi thường");
        }

        public void RegisterRegistry(PcCompensationRegistry registry)
        {
            _registry = registry;
        }

        public PcCompensationEntry GetCompensation(int compId)
            => _registry != null ? _registry.Get(compId) : null;

        public IReadOnlyList<PcCompensationEntry> GetActive(int currentDate)
            => _registry != null
                ? _registry.GetActive(currentDate)
                : System.Array.Empty<PcCompensationEntry>();

        public static CompensationService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcCompensationParser.BuildRegistry(dir);
            return new CompensationService(reg);
        }
    }
}
