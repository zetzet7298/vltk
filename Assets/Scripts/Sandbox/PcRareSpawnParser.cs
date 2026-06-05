// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/rare.txt (Quái vật hiếm spawn) parser
// Source: rare.txt (rare NPC spawn data, GB2312).
//   Same format as normal.txt but with Probability column appended.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcRareSpawnEntry
    {
        public int npcId;
        public int npcTemplateId;
        public int mapId;
        public int posX;
        public int posY;
        public int respawnSec;
        public float probability;  // Xác suất spawn (0.0 - 1.0)
    }

    public sealed class PcRareSpawnRegistry
    {
        private readonly Dictionary<int, PcRareSpawnEntry> _byId = new();
        private readonly Dictionary<int, List<PcRareSpawnEntry>> _byMap = new();
        public int Count => _byId.Count;

        public void Register(PcRareSpawnEntry e)
        {
            if (e == null) return;
            _byId[e.npcId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var ml)) { ml = new(); _byMap[e.mapId] = ml; }
            ml.Add(e);
        }

        public PcRareSpawnEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public List<PcRareSpawnEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v) ? v : new List<PcRareSpawnEntry>();

        public IEnumerable<PcRareSpawnEntry> All => _byId.Values;
    }

    public static class PcRareSpawnParser
    {
        public const int NpcTemplateIdCol = 1;
        public const int MapIdCol = 2;
        public const int PosXCol = 3;
        public const int PosYCol = 4;
        public const int RespawnCol = 5;
        public const int ProbabilityCol = 6;

        public static List<PcRareSpawnEntry> ParseFile(string path)
        {
            var rows = new List<PcRareSpawnEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 7) continue;
                int prob = PcItemCommon.Int(cols, ProbabilityCol);
                rows.Add(new PcRareSpawnEntry
                {
                    npcId = PcItemCommon.Int(cols, 0),
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnCol),
                    probability = prob > 100 ? prob / 10000f : prob / 100f,
                });
            }
            return rows;
        }

        public static PcRareSpawnRegistry BuildRegistry(string dir)
        {
            var reg = new PcRareSpawnRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "rare*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }
}
