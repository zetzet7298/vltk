// -----------------------------------------------------------------------------
// VLTK Mobile — Flip Card runtime service (Lật Thẻ Nhận Thưởng)
// Quản lý thẻ lật: id, tên, phần thưởng, xác suất, tier (1-3).
// Wraps PcFlipCardRegistry.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class FlipCardService
    {
        public const string LogTag = "FlipCard";
        public const string DefaultStreamingDir = "Reference/PcFlipCard";

        private PcFlipCardRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcFlipCardEntry> All => _registry?.All;

        public FlipCardService() : this(null) { }

        public FlipCardService(PcFlipCardRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"FlipCardService loaded {_registry?.Count ?? 0} thẻ lật");
        }

        public void RegisterRegistry(PcFlipCardRegistry registry)
        {
            _registry = registry;
        }

        public PcFlipCardEntry GetCard(int cardId)
            => _registry != null ? _registry.Get(cardId) : null;

        public IReadOnlyList<PcFlipCardEntry> GetByTier(int tier)
            => _registry != null
                ? _registry.GetByTier(tier)
                : System.Array.Empty<PcFlipCardEntry>();

        public static FlipCardService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcFlipCardParser.BuildRegistry(dir);
            return new FlipCardService(reg);
        }
    }
}
