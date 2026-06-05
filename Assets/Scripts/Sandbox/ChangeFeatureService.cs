// -----------------------------------------------------------------------------
// VLTK Mobile — Change Feature runtime service (Đổi Ngoại Hình)
// Quản lý đổi ngoại hình: chi phí vật phẩm + bạc → sprite mới.
// Wraps PcChangeFeatureRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ChangeFeatureService
    {
        public const string LogTag = "ChangeFeature";
        public const string DefaultStreamingDir = "Reference/PcChangeFeature";

        private PcChangeFeatureRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcChangeFeatureEntry> All => _registry?.All;

        public ChangeFeatureService() : this(null) { }

        public ChangeFeatureService(PcChangeFeatureRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"ChangeFeatureService loaded {_registry?.Count ?? 0} mẫu đổi ngoại hình");
        }

        public void RegisterRegistry(PcChangeFeatureRegistry registry)
        {
            _registry = registry;
        }

        public PcChangeFeatureEntry GetFeature(int featureId)
            => _registry != null ? _registry.Get(featureId) : null;

        public IEnumerable<PcChangeFeatureEntry> GetAllFeatures()
            => _registry != null ? _registry.All : System.Array.Empty<PcChangeFeatureEntry>();

        public static ChangeFeatureService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcChangeFeatureParser.BuildRegistry(dir);
            return new ChangeFeatureService(reg);
        }
    }
}
