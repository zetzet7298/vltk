// -----------------------------------------------------------------------------
// VLTK Mobile — ST-7.x Encounter runtime service
// Wraps PcEncounterRegistry. PC source: settings/encounter/encounter.txt.
// Quản lý kỳ ngộ: kích hoạt sự kiện ngẫu nhiên khi di chuyển trong bản đồ.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Kỳ Ngộ: cuộn ngẫu nhiên một sự kiện cho map, kiểm tra theo xác suất.
    /// </summary>
    public class EncounterService
    {
        public const string LogTag = "Encounter";
        public const string DefaultStreamingDir = "Reference/PcEncounter";

        private PcEncounterRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public EncounterService() { }
        public EncounterService(PcEncounterRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcEncounterRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Encounter registry rỗng");
        }

        public PcEncounterEntry GetEncounter(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcEncounterEntry> GetByType(int type)
            => _reg != null ? _reg.GetByType(type) : Array.Empty<PcEncounterEntry>();

        public IReadOnlyList<PcEncounterEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : Array.Empty<PcEncounterEntry>();

        public IReadOnlyList<PcEncounterEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcEncounterEntry>();

        /// <summary>
        /// Cuộn ngẫu nhiên một kỳ ngộ cho map với seed cho trước. Trả về null nếu không match.
        /// </summary>
        public PcEncounterEntry RollEncounter(int mapId, int randomSeed)
        {
            if (_reg == null) return null;
            var list = _reg.GetByMap(mapId);
            if (list == null || list.Count == 0) return null;
            var rng = new System.Random(randomSeed);
            // Duyệt qua từng kỳ ngộ, cuộn xác suất
            foreach (var e in list)
            {
                if (e.probability <= 0) continue;
                int roll = rng.Next(0, 10000);
                if (roll < e.probability) return e;
            }
            return null;
        }

        public string GetEncounterTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Vật phẩm";
                case 1: return "NPC";
                case 2: return "Bẫy";
                case 3: return "Cổng";
                case 4: return "Sự kiện";
                default: return "Khác";
            }
        }

        public static EncounterService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcEncounterParser.BuildRegistry(dir);
            return new EncounterService(reg);
        }
    }
}
