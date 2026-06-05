// -----------------------------------------------------------------------------
// VLTK Mobile — MapNpcRespawn runtime service
// Wraps PcMapNpcRespawnRegistry. PC source: settings/npcrespawn.txt.
// Vietnamese: "NPC Sinh Sản", "Hồi Sinh", "Nhóm Quái", "Số Lượng Tối Đa".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapNpcRespawnService
    {
        private readonly PcMapNpcRespawnRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapNpcRespawnService() { _reg = new PcMapNpcRespawnRegistry(); }
        public MapNpcRespawnService(PcMapNpcRespawnRegistry reg) { _reg = reg ?? new PcMapNpcRespawnRegistry(); }

        public static MapNpcRespawnService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapNpcRespawnService(PcMapNpcRespawnParser.BuildRegistry(path));
        }

        public IReadOnlyList<PcMapNpcRespawnEntry> GetRespawns(int mapId) => _reg.GetByMap(mapId);
        public IReadOnlyList<PcMapNpcRespawnEntry> GetByTemplate(int templateId) => _reg.GetByTemplate(templateId);
        public IReadOnlyList<PcMapNpcRespawnEntry> GetAll() => _reg.All;

        public IReadOnlyList<PcMapNpcRespawnEntry> GetGroupRespawns(int mapId, int groupId)
        {
            var list = new List<PcMapNpcRespawnEntry>();
            foreach (var e in _reg.GetByMap(mapId)) if (e.groupId == groupId) list.Add(e);
            return list;
        }

        /// <summary>Tính thời điểm NPC hồi sinh tiếp theo (Unix seconds). Trả 0 nếu không tìm thấy.</summary>
        public long ComputeRespawnTime(int mapId, int npcId, int lastDeathUnix)
        {
            foreach (var e in _reg.GetByMap(mapId))
            {
                if (e.npcId == npcId) return lastDeathUnix + e.respawnSec;
            }
            return 0;
        }
    }
}
