// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2.6 Faction Map Runtime Service (Bản Đồ Môn Phái runtime)
// Bao bọc FactionMapService — thêm helper cho capture war, ownership check.
// Vietnamese: "Bản Đồ", "Môn Phái", "Tranh Chấp", "Chiếm Đóng", "Thủ Phủ".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime mở rộng cho bản đồ môn phái: capture, ownership, conflict.
    /// </summary>
    public class FactionMapRuntimeService
    {
        public const string LogTag = "FactionMapRuntime";

        private FactionMapService _inner;

        public int Count => _inner != null ? _inner.Count : 0;

        public FactionMapRuntimeService() : this(null) { }
        public FactionMapRuntimeService(FactionMapService inner) { _inner = inner; }

        public void AttachInner(FactionMapService inner)
        {
            _inner = inner ?? new FactionMapService();
            SubsystemLog.Info(LogTag, $"FactionMapRuntime attached: {Count} map");
        }

        public PcFactionMapEntry GetFactionMap(int mapId)
            => _inner != null ? _inner.GetMap(mapId) : null;

        public IReadOnlyList<PcFactionMapEntry> GetMapsForFaction(int factionId)
            => _inner != null
                ? _inner.GetByFaction(factionId)
                : (IReadOnlyList<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();

        /// <summary>
        /// Các map hiện không thuộc môn phái nào (factionId == 0 hoặc requiredLevel > 0)
        /// — coi như contested / có thể tranh chấp.
        /// </summary>
        public IReadOnlyList<PcFactionMapEntry> GetContestedMaps()
        {
            var list = new List<PcFactionMapEntry>();
            if (_inner == null) return list;
            foreach (var e in _inner.GetAllMaps())
            {
                if (e.factionId <= 0) list.Add(e);
            }
            return list;
        }

        /// <summary>
        /// Các map hiện đang thuộc sở hữu của factionId.
        /// </summary>
        public IReadOnlyList<PcFactionMapEntry> GetOwnedMaps(int factionId)
            => GetMapsForFaction(factionId);

        /// <summary>
        /// factionId có thể chiếm mapId không: kiểm tra tồn tại + chưa thuộc phe khác.
        /// </summary>
        public bool CanCapture(int mapId, int factionId)
        {
            var e = GetFactionMap(mapId);
            if (e == null) return false;
            if (factionId <= 0) return false;
            // Nếu đã thuộc phe khác thì không thể capture trực tiếp
            if (e.factionId > 0 && e.factionId != factionId) return false;
            return true;
        }

        public static FactionMapRuntimeService LoadFromStreamingAssets()
        {
            var inner = FactionMapService.LoadFromStreamingAssets();
            return new FactionMapRuntimeService(inner);
        }
    }
}
