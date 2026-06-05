// -----------------------------------------------------------------------------
// VLTK Mobile — New Player Guide runtime service
// Quản lý các bước hướng dẫn tân thủ (mở bản đồ, gặp NPC, nhận thưởng).
// Wraps PcNewPlayerGuideRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public class NewPlayerGuideService
    {
        public const string LogTag = "NewPlayerGuide";
        public const string DefaultStreamingDir = "Reference/PcNewPlayerGuide";

        private PcNewPlayerGuideRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcNewPlayerGuideEntry> All => _registry?.All;

        public NewPlayerGuideService() : this(null) { }

        public NewPlayerGuideService(PcNewPlayerGuideRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"NewPlayerGuideService loaded {_registry?.Count ?? 0} bước tân thủ");
        }

        public void RegisterRegistry(PcNewPlayerGuideRegistry registry)
        {
            _registry = registry;
        }

        public PcNewPlayerGuideEntry GetGuide(int guideId)
            => _registry != null ? _registry.Get(guideId) : null;

        public IReadOnlyList<PcNewPlayerGuideEntry> GetForLevel(int level)
        {
            var result = new List<PcNewPlayerGuideEntry>();
            if (_registry == null) return result;
            foreach (var e in _registry.GetForLevel(level)) result.Add(e);
            return result;
        }

        public static NewPlayerGuideService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcNewPlayerGuideParser.BuildRegistry(dir);
            return new NewPlayerGuideService(reg);
        }
    }
}
