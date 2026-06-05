// -----------------------------------------------------------------------------
// VLTK Mobile — PC maplist.ini full parser
// Source: settings/maplist.ini (1,005 maps).
// Cols: MapId  Name  Type  RequiredLevel  MaxLevel  RequiredFame  IsBattlefield  IsInstance
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapListFullParser
    {
        public const int MapIdCol = 0;
        public const int NameCol = 1;
        public const int TypeCol = 2;
        public const int RequiredLevelCol = 3;
        public const int MaxLevelCol = 4;
        public const int RequiredFameCol = 5;
        public const int IsBattlefieldCol = 6;
        public const int IsInstanceCol = 7;

        public const int TypeCity = 0;
        public const int TypeCapital = 1;
        public const int TypeCountry = 2;
        public const int TypeField = 3;
        public const int TypeCave = 4;
        public const int TypeTong = 5;
        public const int TypeBattlefield = 6;
        public const int TypeInstance = 7;

        public static List<PcMapListFullEntry> ParseFile(string path)
        {
            var rows = new List<PcMapListFullEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapListFullEntry
                {
                    mapId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    requiredFame = PcItemCommon.Int(cols, RequiredFameCol),
                    isBattlefield = PcItemCommon.Int(cols, IsBattlefieldCol) != 0,
                    isInstance = PcItemCommon.Int(cols, IsInstanceCol) != 0,
                });
            }
            return rows;
        }

        public static PcMapListFullRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapListFullRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree.
            foreach (var f in Directory.GetFiles(dir, "maplist*.ini"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapListFullEntry
    {
        public int mapId;
        public string nameRaw;
        public int type;
        public int requiredLevel;
        public int maxLevel;
        public int requiredFame;
        public bool isBattlefield;
        public bool isInstance;
    }

    public sealed class PcMapListFullRegistry
    {
        private readonly Dictionary<int, PcMapListFullEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMapListFullEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _byId[e.mapId] = e;
        }
        public PcMapListFullEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMapListFullEntry> GetByType(int type)
        {
            var list = new List<PcMapListFullEntry>();
            foreach (var e in _byId.Values) if (e.type == type) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapListFullEntry> GetByLevel(int level)
        {
            var list = new List<PcMapListFullEntry>();
            foreach (var e in _byId.Values)
                if (level >= e.requiredLevel && (e.maxLevel == 0 || level <= e.maxLevel))
                    list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapListFullEntry> GetBattlefieldMaps() => GetByType(PcMapListFullParser.TypeBattlefield);
        public IReadOnlyList<PcMapListFullEntry> GetInstanceMaps() => GetByType(PcMapListFullParser.TypeInstance);
        public IReadOnlyList<PcMapListFullEntry> GetCityMaps() => GetByType(PcMapListFullParser.TypeCity);
        public IReadOnlyList<PcMapListFullEntry> GetCaveMaps() => GetByType(PcMapListFullParser.TypeCave);
        public IReadOnlyList<PcMapListFullEntry> GetTongMaps() => GetByType(PcMapListFullParser.TypeTong);
        public IReadOnlyList<PcMapListFullEntry> GetFieldMaps() => GetByType(PcMapListFullParser.TypeField);
        public IReadOnlyList<PcMapListFullEntry> GetCountryMaps() => GetByType(PcMapListFullParser.TypeCountry);
        public IReadOnlyList<PcMapListFullEntry> GetCapitalMaps() => GetByType(PcMapListFullParser.TypeCapital);
        public IReadOnlyList<PcMapListFullEntry> All => new List<PcMapListFullEntry>(_byId.Values);
    }
}
