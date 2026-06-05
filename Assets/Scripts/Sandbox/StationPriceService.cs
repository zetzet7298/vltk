// -----------------------------------------------------------------------------
// VLTK Mobile — StationPriceService: runtime service cho bảng giá trạm xe
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class StationPriceService
    {
        public const string LogTag = "StationPrice";

        private readonly PcStationPriceRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public StationPriceService() { _reg = new PcStationPriceRegistry(); }
        public StationPriceService(PcStationPriceRegistry reg) { _reg = reg ?? new PcStationPriceRegistry(); }

        public void RegisterRegistry(PcStationPriceRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} bảng giá trạm xe");
        }

        public static StationPriceService LoadFromStreamingAssets(string subDir = "Reference/PcTravel")
        {
            var svc = new StationPriceService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcStationPriceParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public int GetPrice(string fromStation, string toStation)
        {
            if (string.IsNullOrEmpty(fromStation) || string.IsNullOrEmpty(toStation)) return -1;
            foreach (var e in _reg.All)
            {
                if (e.FromStation == fromStation && e.ToStation == toStation) return e.Price;
            }
            return -1;
        }

        public IEnumerable<PcStationPriceEntry> GetAll() => _reg.All;
    }
}
