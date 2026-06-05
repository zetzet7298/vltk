// -----------------------------------------------------------------------------
// VLTK Mobile — ST-7.x Treasure Hunt runtime service
// Wraps PcTreasureHuntRegistry. PC source: settings/activity/treasurehunt.txt.
// Quản lý săn kho báu: lookup, khoảng cách, đào.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Săn Kho Báu: tìm kho báu gần, kiểm tra cấp độ đào.
    /// </summary>
    public class TreasureHuntService
    {
        public const string LogTag = "TreasureHunt";
        public const string DefaultStreamingDir = "Reference/PcActivity";

        private PcTreasureHuntRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public TreasureHuntService() { }
        public TreasureHuntService(PcTreasureHuntRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcTreasureHuntRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "TreasureHunt registry rỗng");
        }

        public PcTreasureHuntEntry GetTreasure(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcTreasureHuntEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : Array.Empty<PcTreasureHuntEntry>();

        public IReadOnlyList<PcTreasureHuntEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcTreasureHuntEntry>();

        public IReadOnlyList<PcTreasureHuntEntry> GetNearbyTreasures(int mapId, float x, float y, int range)
        {
            var result = new List<PcTreasureHuntEntry>();
            if (_reg == null) return result;
            int r2 = range * range;
            foreach (var e in _reg.GetByMap(mapId))
            {
                float dx = (float)e.posX - x;
                float dy = (float)e.posY - y;
                int d2 = (int)(dx * dx + dy * dy);
                if (d2 <= r2) result.Add(e);
            }
            return result;
        }

        public bool CanDig(int treasureId, int playerLevel)
        {
            var entry = GetTreasure(treasureId);
            if (entry == null) return false;
            return playerLevel >= entry.requiredLevel;
        }

        public static TreasureHuntService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcTreasureHuntParser.BuildRegistry(dir);
            return new TreasureHuntService(reg);
        }
    }
}
