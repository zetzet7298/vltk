// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/maps/newcitydefence/*.txt city defence (thủ thành) parser
// Source: maps/newcitydefence/guai###.txt + battlechange#.txt (PC wave scripts).
//   Each file: MapId \t WaveIndex \t NpcTemplateId \t Count \t IntervalSec
//   + reward line: RewardId \t RewardCount \t MinLevel
// Mobile aggregates into PcCityDefenceEntry per (mapId, waveIndex).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcCityDefenceParser
    {
        public static List<PcCityDefenceEntry> ParseFile(string path)
        {
            var rows = new List<PcCityDefenceEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcCityDefenceEntry
                {
                    mapId = PcItemCommon.Int(cols, 0),
                    waveIndex = cols.Length > 1 ? PcItemCommon.Int(cols, 1) : 0,
                    defenderNpcId = cols.Length > 2 ? PcItemCommon.Int(cols, 2) : 0,
                    npcCount = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    waveIntervalSec = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 60,
                    rewardId = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    rewardCount = cols.Length > 6 ? PcItemCommon.Int(cols, 6) : 0,
                    minLevel = cols.Length > 7 ? PcItemCommon.Int(cols, 7) : 0,
                });
            }
            return rows;
        }

        public static PcCityDefenceRegistry BuildRegistry(string dir)
        {
            var reg = new PcCityDefenceRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Scan all .txt under dir, including subdirs.
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcCityDefenceEntry
    {
        public int mapId;
        public int waveIndex;
        public int defenderNpcId;
        public int npcCount;
        public int waveIntervalSec;
        public int rewardId;
        public int rewardCount;
        public int minLevel;
    }

    public sealed class PcCityDefenceRegistry
    {
        private readonly Dictionary<int, List<PcCityDefenceEntry>> _byMap = new();
        private readonly List<PcCityDefenceEntry> _all = new();
        public int Count => _all.Count;
        public IEnumerable<PcCityDefenceEntry> All => _all;

        public void Register(PcCityDefenceEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _all.Add(e);
            if (!_byMap.TryGetValue(e.mapId, out var list))
            {
                list = new List<PcCityDefenceEntry>();
                _byMap[e.mapId] = list;
            }
            list.Add(e);
        }

        public IReadOnlyList<PcCityDefenceEntry> Get(int mapId)
            => _byMap.TryGetValue(mapId, out var v)
                ? (IReadOnlyList<PcCityDefenceEntry>)v
                : System.Array.Empty<PcCityDefenceEntry>();
    }
}
