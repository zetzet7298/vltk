// -----------------------------------------------------------------------------
// VLTK Mobile — WaypointPriceService: runtime service cho bảng giá dịch chuyển
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class WaypointPriceService
    {
        public const string LogTag = "WaypointPrice";

        private readonly PcWaypointPriceRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public WaypointPriceService() { _reg = new PcWaypointPriceRegistry(); }
        public WaypointPriceService(PcWaypointPriceRegistry reg) { _reg = reg ?? new PcWaypointPriceRegistry(); }

        public void RegisterRegistry(PcWaypointPriceRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} bảng giá dịch chuyển");
        }

        public static WaypointPriceService LoadFromStreamingAssets(string subDir = "Reference/PcTravel")
        {
            var svc = new WaypointPriceService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcWaypointPriceParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public int GetPrice(string fromWaypoint, string toWaypoint)
        {
            if (string.IsNullOrEmpty(fromWaypoint) || string.IsNullOrEmpty(toWaypoint)) return -1;
            foreach (var e in _reg.All)
            {
                if (e.FromWaypoint == fromWaypoint && e.ToWaypoint == toWaypoint) return e.Price;
            }
            return -1;
        }

        public IEnumerable<PcWaypointPriceEntry> GetAll() => _reg.All;
    }
}
