// -----------------------------------------------------------------------------
// VLTK Mobile — ST-1.7 Map Connection runtime service
// Quản lý kết nối giữa các bản đồ (cổng, dịch chuyển, bí mật, nhiệm vụ).
// PC source: settings/mapconnection.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class MapConnectionService
    {
        public const string LogTag = "MapConn";

        private readonly PcMapConnectionRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public MapConnectionService() { }
        public MapConnectionService(PcMapConnectionRegistry registry) { _registry = registry ?? new PcMapConnectionRegistry(); }

        public static MapConnectionService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcMap");
            var reg = PcMapConnectionParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} kết nối bản đồ");
            return new MapConnectionService(reg);
        }

        public PcMapConnectionEntry GetConnection(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcMapConnectionEntry> GetByFromMap(int mapId)
            => _registry != null ? _registry.GetByFromMap(mapId) : Array.Empty<PcMapConnectionEntry>();
        public IReadOnlyList<PcMapConnectionEntry> GetByToMap(int mapId)
            => _registry != null ? _registry.GetByToMap(mapId) : Array.Empty<PcMapConnectionEntry>();

        public IReadOnlyList<PcMapConnectionEntry> GetAdjacentMaps(int mapId)
            => GetByFromMap(mapId);

        public bool CanUseConnection(int connectionId, int playerLevel)
        {
            var c = GetConnection(connectionId);
            if (c == null) return false;
            return playerLevel >= c.requiredLevel;
        }

        public float ComputeDistance(int connectionId)
        {
            var c = GetConnection(connectionId);
            if (c == null) return 0f;
            int dx = c.toX - c.fromX;
            int dy = c.toY - c.fromY;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public string GetConnectionTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Bình thường";
                case 1: return "Truyền tống";
                case 2: return "Cổng dịch chuyển";
                case 3: return "Bí mật";
                case 4: return "Nhiệm vụ";
                default: return $"Loại {type}";
            }
        }
    }
}
