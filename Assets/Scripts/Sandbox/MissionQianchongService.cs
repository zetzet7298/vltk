// -----------------------------------------------------------------------------
// VLTK Mobile — Mission Qianchonglou Service
// Quản lý đường đi Vạn Trọng Lâu (6 tracks).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý cấu hình qianchonglou (6 đường đi).
    /// </summary>
    public class MissionQianchongService
    {
        private readonly PcMissionQianchongRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MissionQianchongService() { _reg = new PcMissionQianchongRegistry(); }
        public MissionQianchongService(PcMissionQianchongRegistry reg) { _reg = reg ?? new PcMissionQianchongRegistry(); }

        public static MissionQianchongService LoadFromStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Reference/PcMission/qianchonglou");
            return new MissionQianchongService(PcMissionQianchongParser.BuildRegistry(path));
        }

        public PcMissionQianchongEntry Get(int trackId) => _reg.Get(trackId);
        public IEnumerable<PcMissionQianchongEntry> All => _reg.All;
    }
}
