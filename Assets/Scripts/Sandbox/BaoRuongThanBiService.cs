// -----------------------------------------------------------------------------
// VLTK Mobile — Bảo Rương Thần Bí runtime service (Rương Thần Bí)
// Quản lý rương thần bí theo tier: id, tên, tier, cấp yêu cầu, phần thưởng, xác suất.
// Wraps PcBaoRuongThanBiRegistry. Loads from Reference/PcEvent.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class BaoRuongThanBiService
    {
        public const string LogTag = "BaoRuongThanBi";
        public const string DefaultStreamingDir = "Reference/PcEvent";

        private PcBaoRuongThanBiRegistry _registry;

        public int Count => _registry != null ? _registry.Count : 0;
        public IEnumerable<PcBaoRuongThanBiEntry> All => _registry?.All;

        public BaoRuongThanBiService() : this(null) { }

        public BaoRuongThanBiService(PcBaoRuongThanBiRegistry registry)
        {
            _registry = registry;
            SubsystemLog.Info(LogTag, $"BaoRuongThanBiService loaded {_registry?.Count ?? 0} rương thần bí");
        }

        public void RegisterRegistry(PcBaoRuongThanBiRegistry registry)
        {
            _registry = registry;
        }

        public PcBaoRuongThanBiEntry GetBox(int boxId)
            => _registry != null ? _registry.Get(boxId) : null;

        public IReadOnlyList<PcBaoRuongThanBiEntry> GetByTier(int tier)
            => _registry != null
                ? _registry.GetByTier(tier)
                : System.Array.Empty<PcBaoRuongThanBiEntry>();

        public static BaoRuongThanBiService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcBaoRuongThanBiParser.BuildRegistry(dir);
            return new BaoRuongThanBiService(reg);
        }
    }
}
