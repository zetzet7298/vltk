// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/faction_map.txt Faction Map parser
// Source: faction_map.txt (33 entries) hoặc extract từ PcTong/tong_setting.ini.
//   FactionId  MapId  MapName  RequiredLevel  OwnerBonusPercent
// Phân vùng bản đồ theo môn phái: Tống Kim, Tổng Tiêu Cục, đại lý phủ, ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionMapParser
    {
        public const int FactionIdCol = 0;
        public const int MapIdCol = 1;
        public const int MapNameCol = 2;
        public const int RequiredLevelCol = 3;
        public const int OwnerBonusPercentCol = 4;

        public static List<PcFactionMapEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionMapEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                rows.Add(new PcFactionMapEntry
                {
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    mapNameRaw = PcItemCommon.Str(cols, MapNameCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    ownerBonusPercent = PcItemCommon.Int(cols, OwnerBonusPercentCol),
                });
            }
            return rows;
        }

        public static PcFactionMapRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionMapRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "faction_map.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcFactionMapEntry
    {
        public int factionId;
        public int mapId;
        public string mapNameRaw;
        public int requiredLevel;
        public int ownerBonusPercent;
    }

    public sealed class PcFactionMapRegistry
    {
        private readonly Dictionary<int, PcFactionMapEntry> _byMapId = new();
        private readonly Dictionary<int, List<PcFactionMapEntry>> _byFaction = new();
        public int Count => _byMapId.Count;
        public IEnumerable<PcFactionMapEntry> All => _byMapId.Values;
        public void Register(PcFactionMapEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _byMapId[e.mapId] = e;
            if (!_byFaction.TryGetValue(e.factionId, out var list))
            {
                list = new List<PcFactionMapEntry>();
                _byFaction[e.factionId] = list;
            }
            list.Add(e);
        }
        public PcFactionMapEntry Get(int mapId)
            => _byMapId.TryGetValue(mapId, out var v) ? v : null;
        public IReadOnlyList<PcFactionMapEntry> GetByFaction(int factionId)
            => _byFaction.TryGetValue(factionId, out var v)
                ? (IReadOnlyList<PcFactionMapEntry>)v
                : (IReadOnlyList<PcFactionMapEntry>)System.Array.Empty<PcFactionMapEntry>();
    }
}
