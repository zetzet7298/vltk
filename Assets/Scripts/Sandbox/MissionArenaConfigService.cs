// -----------------------------------------------------------------------------
// VLTK Mobile — Mission Arena Config Service
// Quản lý vị trí battle/ready cho arena.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý cấu hình arena (vị trí battle + ready).
    /// </summary>
    public class MissionArenaConfigService
    {
        private readonly PcMissionArenaRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MissionArenaConfigService() { _reg = new PcMissionArenaRegistry(); }
        public MissionArenaConfigService(PcMissionArenaRegistry reg) { _reg = reg ?? new PcMissionArenaRegistry(); }

        public static MissionArenaConfigService LoadFromStreamingAssets()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Reference/PcMission/arena");
            return new MissionArenaConfigService(PcMissionArenaParser.BuildRegistry(path));
        }

        public PcMissionArenaEntry Get(int arenaId) => _reg.Get(arenaId);
        public IEnumerable<PcMissionArenaEntry> All => _reg.All;
        public IReadOnlyList<ArenaPos> GetBattlePositions() => _reg.Get(1)?.BattlePositions ?? new List<ArenaPos>();
        public IReadOnlyList<ArenaPos> GetReadyPositions() => _reg.Get(1)?.ReadyPositions ?? new List<ArenaPos>();
    }
}
