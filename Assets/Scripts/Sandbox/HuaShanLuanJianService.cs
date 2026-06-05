// -----------------------------------------------------------------------------
// VLTK Mobile — ST-8.11 Hoa Sơn Luận Kiếm runtime service (PvP tournament)
// PC source: settings/event/huashan.txt.
// Vietnamese: "Hoa Sơn Luận Kiếm", "Vòng 1", "Tứ Kết", "Bán Kết", "Chung Kết".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class HuaShanLuanJianService
    {
        public const string LogTag = "HuaShan";

        private readonly PcHuaShanLuanJianRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public HuaShanLuanJianService() { }
        public HuaShanLuanJianService(PcHuaShanLuanJianRegistry registry) { _registry = registry ?? new PcHuaShanLuanJianRegistry(); }

        public static HuaShanLuanJianService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcEvent");
            var reg = PcHuaShanLuanJianParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} vòng Hoa Sơn Luận Kiếm");
            return new HuaShanLuanJianService(reg);
        }

        public PcHuaShanLuanJianEntry GetRound(int roundIdx)
        {
            if (_registry == null) return null;
            foreach (var e in _registry.GetByRound(roundIdx))
                return e;
            return null;
        }

        public IReadOnlyList<PcHuaShanLuanJianEntry> GetByMap(int mapId)
            => _registry != null ? _registry.GetByMap(mapId) : Array.Empty<PcHuaShanLuanJianEntry>();

        public bool CanJoinRound(int round, int playerLevel)
        {
            var entry = GetRound(round);
            if (entry == null) return false;
            return playerLevel >= entry.requiredLevel;
        }

        public PcHuaShanLuanJianEntry GetFinalRound() => _registry != null ? _registry.GetFinalRound() : null;
        public int GetTotalRounds() => _registry != null ? _registry.GetTotalRounds() : 0;

        public string GetRoundName(int round)
        {
            if (round <= 0) return string.Empty;
            if (_registry == null) return $"Vòng {round}";
            var entry = GetRound(round);
            if (entry != null && entry.isFinalRound) return "Chung Kết";
            int total = GetTotalRounds();
            if (total > 0)
            {
                if (round == total) return "Chung Kết";
                if (round == total - 1) return "Bán Kết";
                if (round == total - 2) return "Tứ Kết";
            }
            return $"Vòng {round}";
        }
    }
}
