// -----------------------------------------------------------------------------
// VLTK Mobile — PC maplist.ini full parser
// PC source: Client 6.0/settings/maplist.ini (also server settings/maplist.ini),
// 1,005 maps. Committed copy: Assets/StreamingAssets/Reference/PcMap/maplist.ini.
//
// FORMAT (verified against PC source): this file is an INI section, NOT a TSV.
// A single [List] section holds, per map id N:
//     N=<world\path>                  (e.g. 1=西北南区\凤翔)
//     N_name=<localized display name> (e.g. 1_name= Phượng Tường)
//     N_MapPos=<x>,<y>                (world-map minimap coordinate)
//     N_MapType=City|Capital|Country|Field|Cave|Tong|Battlefield|Others
//     N_<other>=...                   (NpcSeriesAuto, GoldenType, drop rates …)
// Lines starting with ';' are comments. Only 203/1005 maps carry an explicit
// MapType key; maps without one are categorized TypeOther (faithful to PC, which
// has an explicit "Others" bucket and leaves the rest unspecified).
//
// The previous implementation assumed an 8-column tab-separated layout, so every
// line failed the tab split and the registry loaded 0 maps. See PORT_STATUS
// backlog #16.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapListFullParser
    {
        // MapType enum values. The first eight match the historical numbering so
        // existing services/tests keep compiling. TypeInstance has no source in
        // maplist.ini (instances are defined elsewhere) and is retained only for
        // API compatibility. TypeOther covers the PC "Others" bucket plus maps
        // that omit the MapType key.
        public const int TypeCity = 0;
        public const int TypeCapital = 1;
        public const int TypeCountry = 2;
        public const int TypeField = 3;
        public const int TypeCave = 4;
        public const int TypeTong = 5;
        public const int TypeBattlefield = 6;
        public const int TypeInstance = 7;
        public const int TypeOther = 8;

        /// <summary>
        /// Map a PC MapType string (City/Field/…) to the enum value. Matching is
        /// case-insensitive so the lone "Maptype" key typo in the PC file and any
        /// casing variant resolve correctly. Unknown/empty strings fall back to
        /// TypeOther.
        /// </summary>
        public static int MapTypeFromString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return TypeOther;
            switch (raw.Trim().ToLowerInvariant())
            {
                case "city": return TypeCity;
                case "capital": return TypeCapital;
                case "country": return TypeCountry;
                case "field": return TypeField;
                case "cave": return TypeCave;
                case "tong": return TypeTong;
                case "battlefield": return TypeBattlefield;
                case "instance": return TypeInstance;
                default: return TypeOther; // includes "Others"
            }
        }

        public static List<PcMapListFullEntry> ParseFile(string path)
        {
            var rows = new List<PcMapListFullEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            var lines = PcItemCommon.ReadServerLines(path);
            // Preserve first-seen order so callers get maps in id order as they
            // appear in the file.
            var byId = new Dictionary<int, PcMapListFullEntry>();
            var order = new List<int>();

            foreach (var rawLine in lines)
            {
                if (rawLine == null) continue;
                var line = rawLine.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue; // comment
                if (line[0] == '[') continue;                    // section header

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();

                int underscore = key.IndexOf('_');
                string idPart = underscore < 0 ? key : key.Substring(0, underscore);
                if (!int.TryParse(idPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int id) || id <= 0)
                    continue;

                if (!byId.TryGetValue(id, out var entry))
                {
                    entry = new PcMapListFullEntry { mapId = id, type = TypeOther };
                    byId[id] = entry;
                    order.Add(id);
                }

                if (underscore < 0)
                {
                    // "N=<world\path>" — the map's path is the bare id key value.
                    entry.pathRaw = val;
                    entry.hasPath = true;
                    continue;
                }

                string sub = key.Substring(underscore + 1).Trim().ToLowerInvariant();
                switch (sub)
                {
                    case "name":
                        entry.nameRaw = val;
                        break;
                    case "mappos":
                        ParseMapPos(val, entry);
                        break;
                    case "maptype":
                        entry.type = MapTypeFromString(val);
                        entry.isBattlefield = entry.type == TypeBattlefield;
                        break;
                    // Other keys (NpcSeries*, GoldenType, drop rates, NewWorld*…)
                    // are not part of this parser's scope.
                }
            }

            foreach (var id in order)
            {
                var e = byId[id];
                // A real map is one that declared a path line ("N=..."). Stray
                // subkeys without a path are ignored.
                if (e.hasPath) rows.Add(e);
            }
            return rows;
        }

        private static void ParseMapPos(string val, PcMapListFullEntry entry)
        {
            if (string.IsNullOrEmpty(val)) return;
            int comma = val.IndexOf(',');
            if (comma < 0) return;
            string xs = val.Substring(0, comma).Trim();
            string ys = val.Substring(comma + 1).Trim();
            if (int.TryParse(xs, NumberStyles.Integer, CultureInfo.InvariantCulture, out int x))
                entry.mapPosX = x;
            if (int.TryParse(ys, NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                entry.mapPosY = y;
        }

        public static PcMapListFullRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapListFullRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Use the explicit file-name family to avoid sweeping unrelated
            // .txt/.ini files in the directory tree. Skip *_sample.ini so the
            // small UTF-8 preview copies do not clobber real entries.
            foreach (var f in Directory.GetFiles(dir, "maplist*.ini", SearchOption.AllDirectories))
            {
                var name = Path.GetFileNameWithoutExtension(f);
                if (name != null && name.EndsWith("_sample", System.StringComparison.OrdinalIgnoreCase))
                    continue;
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapListFullEntry
    {
        public int mapId;
        public string nameRaw;
        public string pathRaw;     // PC "N=<world\path>" value
        public int type;           // PcMapListFullParser.Type*
        public int mapPosX;        // PC MapPos x (world-map minimap coord)
        public int mapPosY;        // PC MapPos y
        public bool isBattlefield; // derived from MapType == Battlefield

        // No source in maplist.ini; retained for API/back-compat. maplist.ini
        // carries no per-map level/fame gate or instance flag — those live in
        // other PC tables (cavelist, instance configs, etc.).
        public int requiredLevel;
        public int maxLevel;
        public int requiredFame;
        public bool isInstance;

        [System.NonSerialized] internal bool hasPath;
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
        public IReadOnlyList<PcMapListFullEntry> GetOtherMaps() => GetByType(PcMapListFullParser.TypeOther);
        public IReadOnlyList<PcMapListFullEntry> All => new List<PcMapListFullEntry>(_byId.Values);
    }
}
