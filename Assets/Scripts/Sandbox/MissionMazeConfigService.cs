// -----------------------------------------------------------------------------
// VLTK Mobile — Mission Maze Config Service
// Quản lý thông tin nhiệm vụ mê cung (Ngọc Long Sơn Trang).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý cấu hình maze (19 nhiệm vụ mê cung).
    /// </summary>
    public class MissionMazeConfigService
    {
        private readonly PcMissionMazeRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MissionMazeConfigService() { _reg = new PcMissionMazeRegistry(); }
        public MissionMazeConfigService(PcMissionMazeRegistry reg) { _reg = reg ?? new PcMissionMazeRegistry(); }

        public static MissionMazeConfigService LoadFromStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Reference/PcMission/maze");
            return new MissionMazeConfigService(PcMissionMazeParser.BuildRegistry(path));
        }

        public PcMissionMazeEntry Get(int taskId) => _reg.Get(taskId);
        public IEnumerable<PcMissionMazeEntry> All => _reg.All;
    }
}
