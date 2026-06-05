// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/normal.txt (Quái vật thường spawn) parser
// Source: normal.txt (NPC spawn data, GB2312).
//   Col 0:  NpcId
//   Col 1..4:  NpcTemplateId, MapId, PosX, PosY
//   Col 5:  Count
//   Col 6:  RespawnSec
//   Col 7..67:  Magic attribs (5 slots * 12 cols)
// We keep spawn + map + position for runtime monster spawn lookup.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcNormalSpawnEntry
    {
        public int npcId;             // Mã NPC
        public int npcTemplateId;     // Mã template NPC
        public int mapId;             // Bản đồ spawn
        public int posX;              // Tọa độ X
        public int posY;              // Tọa độ Y
        public int count;             // Số lượng spawn
        public int respawnSec;        // Thời gian tái sinh (giây)
    }

    public sealed class PcNormalSpawnRegistry
    {
        private readonly Dictionary<int, PcNormalSpawnEntry> _byId = new();
        private readonly Dictionary<int, List<PcNormalSpawnEntry>> _byMap = new();
        public int Count => _byId.Count;

        public void Register(PcNormalSpawnEntry e)
        {
            if (e == null) return;
            _byId[e.npcId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var ml)) { ml = new(); _byMap[e.mapId] = ml; }
            ml.Add(e);
        }

        public PcNormalSpawnEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcNormalSpawnEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v) ? v : new List<PcNormalSpawnEntry>();

        public IEnumerable<PcNormalSpawnEntry> All => _byId.Values;
    }

    public static class PcNormalSpawnParser
    {
        public const int NpcTemplateIdCol = 1;
        public const int MapIdCol = 2;
        public const int PosXCol = 3;
        public const int PosYCol = 4;
        public const int CountCol = 5;
        public const int RespawnCol = 6;

        public static List<PcNormalSpawnEntry> ParseFile(string path)
        {
            var rows = new List<PcNormalSpawnEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 7) continue;
                rows.Add(new PcNormalSpawnEntry
                {
                    npcId = PcItemCommon.Int(cols, 0),
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    count = PcItemCommon.Int(cols, CountCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnCol),
                });
            }
            return rows;
        }

        public static PcNormalSpawnRegistry BuildRegistry(string dir)
        {
            var reg = new PcNormalSpawnRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "normal*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
