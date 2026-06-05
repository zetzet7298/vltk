// -----------------------------------------------------------------------------
// VLTK Mobile — StationService: runtime service cho trạm xe (station.txt)
// Quản lý vị trí trạm xe (Station) cho phép dịch chuyển giữa các thành phố.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class StationService
    {
        public const string LogTag = "Station";

        private readonly PcStationRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public StationService() { _reg = new PcStationRegistry(); }
        public StationService(PcStationRegistry reg) { _reg = reg ?? new PcStationRegistry(); }

        public void RegisterRegistry(PcStationRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} trạm xe");
        }

        public static StationService LoadFromStreamingAssets(string subDir = "Reference/PcTravel")
        {
            var svc = new StationService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcStationParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcStationEntry GetStation(int id) => _reg.Get(id);
        public IEnumerable<PcStationEntry> GetAll() => _reg.All;

        public IEnumerable<PcStationEntry> GetByMap(int mapId)
        {
            foreach (var e in _reg.All)
            {
                foreach (var s in e.Sects)
                {
                    if (s.MapId == mapId) { yield return e; break; }
                }
            }
        }
    }
}
