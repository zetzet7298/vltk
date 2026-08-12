// -----------------------------------------------------------------------------
// VLTK Mobile — GM Test Server token full-map teleport catalog.
// PC sources:
//   script/item/ib/shenxingfu.lua (Thần Hành Phù menus + NewWorld coords)
//   script/item/ib/headshenxingfu.lua (other-map THP targets)
//   script/global/gm/lenhbaiadmintestserver.lua (GM token travel actions)
//   settings/maplist.ini + Reference/PcMap/revivepos.ini + waypoint.txt
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class GmTeleportDestination
    {
        public int mapId;
        public string nameVi;
        public string mapType;
        public string sourcePath;
        public Vector2 worldPosition;
        public string coordinateSource;
        public int fightState;

        public string TypeLabel => GmTeleportCatalogService.TypeLabel(mapType);
        public string DisplayLabel => $"{mapId} - {nameVi} [{TypeLabel}]";
    }

    internal readonly struct GmTeleportPoint
    {
        public readonly Vector2 world;
        public readonly string source;
        public readonly int fightState;

        public GmTeleportPoint(Vector2 world, string source, int fightState = 0)
        {
            this.world = world;
            this.source = source;
            this.fightState = fightState;
        }
    }

    public sealed class GmTeleportCatalogService
    {
        public const string FilterAll = "All";
        public const string FilterCity = "City";
        public const string FilterField = "Field";
        public const string FilterCave = "Cave";
        public const string FilterBattlefield = "Battlefield";
        public const string FilterTong = "Tong";
        public const string FilterOthers = "Others";
        public const int DefaultPageSize = 80;

        private readonly MapManager _mapManager;
        private readonly string _pcMapDir;
        private List<GmTeleportDestination> _cache;

        public GmTeleportCatalogService(MapManager mapManager = null, string pcMapDir = null)
        {
            _mapManager = mapManager;
            _pcMapDir = string.IsNullOrEmpty(pcMapDir)
                ? Path.Combine(Application.streamingAssetsPath, "Reference/PcMap")
                : pcMapDir;
        }

        public IReadOnlyList<GmTeleportDestination> GetAllDestinations()
        {
            if (_cache != null) return _cache;
            _cache = BuildDestinations();
            return _cache;
        }

        public GmTeleportDestination FindByMapId(int mapId)
        {
            foreach (var d in GetAllDestinations())
                if (d.mapId == mapId)
                    return d;
            return null;
        }

        private List<GmTeleportDestination> BuildDestinations()
        {
            var manager = ResolveMapManager();
            var revive = LoadRevivePoints(Path.Combine(_pcMapDir, "revivepos.ini"));
            var waypoint = LoadWaypointPoints(Path.Combine(_pcMapDir, "waypoint.txt"));
            var geometry = LoadBulkGeometryBounds(Application.streamingAssetsPath);
            var result = new List<GmTeleportDestination>();
            var seen = new HashSet<int>();

            if (manager == null) return result;
            foreach (var entry in manager.GetAllEntries())
            {
                if (entry == null || entry.mapId <= 0 || !seen.Add(entry.mapId)) continue;
                result.Add(BuildDestination(entry, revive, waypoint, geometry));
            }
            result.Sort((a, b) => a.mapId.CompareTo(b.mapId));
            return result;
        }

        private MapManager ResolveMapManager()
        {
            var manager = _mapManager ?? new MapManager();
            if (manager.Catalog.Count == 0)
                manager.LoadCatalog();
            return manager;
        }

        private static GmTeleportDestination BuildDestination(
            MapCatalogEntry entry,
            IReadOnlyDictionary<int, GmTeleportPoint> revive,
            IReadOnlyDictionary<int, GmTeleportPoint> waypoint,
            IReadOnlyDictionary<int, RectDef> geometry)
        {
            var point = ResolveDefaultPoint(entry, revive, waypoint, geometry);
            return new GmTeleportDestination
            {
                mapId = entry.mapId,
                nameVi = string.IsNullOrEmpty(entry.displayNameNormalized) ? $"Map {entry.mapId}" : entry.displayNameNormalized,
                mapType = entry.worldSetMembership,
                sourcePath = entry.sourceMapPath,
                worldPosition = point.world,
                coordinateSource = point.source,
                fightState = point.fightState,
            };
        }

        private static GmTeleportPoint ResolveDefaultPoint(
            MapCatalogEntry entry,
            IReadOnlyDictionary<int, GmTeleportPoint> revive,
            IReadOnlyDictionary<int, GmTeleportPoint> waypoint,
            IReadOnlyDictionary<int, RectDef> geometry)
        {
            if (revive.TryGetValue(entry.mapId, out var rev)) return rev;
            if (waypoint.TryGetValue(entry.mapId, out var wp)) return wp;
            if (entry.geometryBounds != null && entry.geometryBounds.width > 0f && entry.geometryBounds.height > 0f)
                return GeometryCenter(entry.geometryBounds, "MapManager.geometryBounds");
            if (geometry.TryGetValue(entry.mapId, out var g))
                return GeometryCenter(g, "MapGeometryCatalog.bounds");
            return new GmTeleportPoint(MapEnemyDatabase.GetDefaultSpawnPoint(entry.mapId), "fallback_default_spawn");
        }

        private static GmTeleportPoint GeometryCenter(RectDef rect, string source)
            => new(new Vector2(rect.x + rect.width * 0.5f, rect.y + rect.height * 0.5f), source);

        public static bool TryGetScriptDestination(string actionId, MapManager mapManager, out GmTeleportDestination destination)
        {
            destination = null;
            if (string.IsNullOrEmpty(actionId) || !ScriptTeleports.TryGetValue(actionId, out var spec))
                return false;

            MapCatalogEntry entry = null;
            if (mapManager != null)
                mapManager.Catalog.TryGetValue(spec.mapId, out entry);

            destination = new GmTeleportDestination
            {
                mapId = spec.mapId,
                nameVi = !string.IsNullOrEmpty(spec.name)
                    ? spec.name
                    : (entry?.displayNameNormalized ?? $"Map {spec.mapId}"),
                mapType = entry?.worldSetMembership,
                sourcePath = entry?.sourceMapPath,
                worldPosition = CellToWorld(spec.cellX, spec.cellY),
                coordinateSource = spec.pcFunction,
                fightState = spec.fightState,
            };
            return true;
        }

        private static Vector2 CellToWorld(int cellX, int cellY)
            => MapEnemyDatabase.MpsToWorld(cellX * 32, cellY * 32);

        private static Dictionary<int, GmTeleportPoint> LoadRevivePoints(string path)
        {
            var result = new Dictionary<int, GmTeleportPoint>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

            int currentMap = 0;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = (rawLine ?? string.Empty).Trim();
                if (line.Length == 0 || line.StartsWith(";")) continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    int.TryParse(line.Substring(1, line.Length - 2), out currentMap);
                    continue;
                }
                if (currentMap <= 0 || result.ContainsKey(currentMap)) continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                if (!int.TryParse(line.Substring(0, eq).Trim(), out _)) continue;
                if (!TryParsePair(line.Substring(eq + 1), out int mpsX, out int mpsY)) continue;
                result[currentMap] = new GmTeleportPoint(
                    MapEnemyDatabase.MpsToWorld(mpsX, mpsY),
                    "Reference/PcMap/revivepos.ini");
            }
            return result;
        }

        private static Dictionary<int, GmTeleportPoint> LoadWaypointPoints(string path)
        {
            var result = new Dictionary<int, GmTeleportPoint>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;

            bool headerSkipped = false;
            foreach (var rawLine in File.ReadAllLines(path))
            {
                if (!headerSkipped) { headerSkipped = true; continue; }
                if (string.IsNullOrWhiteSpace(rawLine)) continue;
                var cols = rawLine.Split('\t');
                if (cols.Length < 3) continue;
                if (!TryParseTriple(cols[2], out int mapId, out int cellX, out int cellY)) continue;
                if (mapId <= 0 || result.ContainsKey(mapId)) continue;
                int fightState = cols.Length > 3 && int.TryParse(cols[3].Trim(), out var fs) ? fs : 0;
                result[mapId] = new GmTeleportPoint(
                    CellToWorld(cellX, cellY),
                    "Reference/PcMap/waypoint.txt",
                    fightState);
            }
            return result;
        }

        private static bool TryParsePair(string csv, out int a, out int b)
        {
            a = b = 0;
            var parts = (csv ?? string.Empty).Split(',');
            return parts.Length >= 2 && int.TryParse(parts[0].Trim(), out a) && int.TryParse(parts[1].Trim(), out b);
        }

        private static bool TryParseTriple(string csv, out int a, out int b, out int c)
        {
            a = b = c = 0;
            var parts = (csv ?? string.Empty).Split(',');
            return parts.Length >= 3 && int.TryParse(parts[0].Trim(), out a) &&
                   int.TryParse(parts[1].Trim(), out b) && int.TryParse(parts[2].Trim(), out c);
        }

        private static Dictionary<int, RectDef> LoadBulkGeometryBounds(string streamingRoot)
        {
            var result = new Dictionary<int, RectDef>();
            string aliasPath = Path.Combine(streamingRoot ?? string.Empty, "MapAliasCatalog.json");
            string geometryPath = Path.Combine(streamingRoot ?? string.Empty, "MapGeometryCatalog.json");
            if (!File.Exists(aliasPath) || !File.Exists(geometryPath)) return result;

            var aliases = LoadJson<AliasCatalogJson>(aliasPath);
            var geometries = LoadJson<GeometryCatalogJson>(geometryPath);
            if (aliases?.aliases == null || geometries?.geometries == null) return result;

            var geometryByKey = new Dictionary<string, RectDef>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in geometries.geometries)
            {
                if (g == null || string.IsNullOrEmpty(g.geometryKey) || g.bounds == null) continue;
                if (g.bounds.width <= 0f || g.bounds.height <= 0f) continue;
                geometryByKey[g.geometryKey] = g.bounds;
            }

            foreach (var a in aliases.aliases)
            {
                if (a == null || a.mapId <= 0 || string.IsNullOrEmpty(a.geometryKey)) continue;
                if (geometryByKey.TryGetValue(a.geometryKey, out var bounds))
                    result[a.mapId] = bounds;
            }
            return result;
        }

        private static T LoadJson<T>(string path) where T : class
        {
            try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
            catch { return null; }
        }

        public static List<GmTeleportDestination> Filter(IEnumerable<GmTeleportDestination> source, string query, string typeFilter)
        {
            var result = new List<GmTeleportDestination>();
            string q = NormalizeSearch(query);
            foreach (var d in source ?? Array.Empty<GmTeleportDestination>())
            {
                if (!MatchesType(d, typeFilter)) continue;
                if (q.Length > 0 && !MatchesQuery(d, q)) continue;
                result.Add(d);
            }
            result.Sort((a, b) => a.mapId.CompareTo(b.mapId));
            return result;
        }

        private static bool MatchesQuery(GmTeleportDestination d, string normalizedQuery)
        {
            if (d == null) return false;
            if (d.mapId.ToString().Contains(normalizedQuery)) return true;
            return NormalizeSearch(d.nameVi).Contains(normalizedQuery) ||
                   NormalizeSearch(d.mapType).Contains(normalizedQuery) ||
                   NormalizeSearch(d.sourcePath).Contains(normalizedQuery);
        }

        private static bool MatchesType(GmTeleportDestination d, string filter)
        {
            if (d == null) return false;
            if (string.IsNullOrEmpty(filter) || filter == FilterAll) return true;
            string t = d.mapType ?? string.Empty;
            if (filter == FilterOthers)
                return t.Length == 0 || (t != FilterCity && t != FilterField && t != FilterCave && t != FilterBattlefield && t != FilterTong);
            return string.Equals(t, filter, StringComparison.OrdinalIgnoreCase);
        }

        public static string TypeLabel(string mapType)
        {
            return mapType switch
            {
                "City" => "Thành thị",
                "Capital" => "Thủ đô",
                "Country" => "Vùng",
                "Field" => "Luyện công",
                "Cave" => "Hang động",
                "Battlefield" => "Chiến trường",
                "Tong" => "Bang phái",
                "Others" => "Khác",
                _ => "Khác",
            };
        }

        public static string FilterLabel(string filter)
        {
            return filter switch
            {
                FilterCity => "Thành",
                FilterField => "Luyện",
                FilterCave => "Hang",
                FilterBattlefield => "Chiến",
                FilterTong => "Bang",
                FilterOthers => "Khác",
                _ => "Tất cả",
            };
        }

        private static string NormalizeSearch(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var normalized = s.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(normalized.Length);
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private sealed class ScriptTeleportSpec
        {
            public readonly int mapId;
            public readonly int cellX;
            public readonly int cellY;
            public readonly string name;
            public readonly int fightState;
            public readonly string pcFunction;

            public ScriptTeleportSpec(int mapId, int cellX, int cellY, string name, int fightState, string pcFunction)
            {
                this.mapId = mapId;
                this.cellX = cellX;
                this.cellY = cellY;
                this.name = name;
                this.fightState = fightState;
                this.pcFunction = pcFunction;
            }
        }

        private static readonly Dictionary<string, ScriptTeleportSpec> ScriptTeleports = new()
        {
            { "goto_satthu", new(78, 1509, 3209, "Vượt ải", 0, "lenhbaiadmintestserver.lua:goto_satthu") },
            { "goto_thientri", new(934, 1598, 3240, "Thiên Trì Mật Cảnh", 1, "lenhbaiadmintestserver.lua:goto_thientri") },
            { "goto_chaucoc", new(176, 1574, 2955, "Loạn Chiến Cửu Châu", 0, "lenhbaiadmintestserver.lua:goto_chaucoc") },
            { "goto_vantieu", new(1, 1559, 2768, "Vận Tiêu", 1, "lenhbaiadmintestserver.lua:goto_vantieu") },
            { "goto_tinsu", new(11, 3024, 5086, "Tín Sứ", 0, "lenhbaiadmintestserver.lua:goto_tinsu") },
            { "goto_thiluyenduong", new(176, 1588, 2941, "Thí Luyện Đường", 0, "lenhbaiadmintestserver.lua:goto_thiluyenduong") },
            { "goto_kiemgia", new(949, 1580, 3158, "Kiếm Gia Mê Cung", 0, "lenhbaiadmintestserver.lua:goto_kiemgia") },
            { "goto_viemde", new(37, 1711, 3179, "Viêm Đế Bảo Tàng", 0, "lenhbaiadmintestserver.lua:goto_viemde") },
            { "goto_phonglangdo", new(336, 1124, 3187, "Phong Lăng Độ", 1, "lenhbaiadmintestserver.lua:goto_phonglangdo") },
            { "gopos_9x", new(93, 1640, 3264, "Boss Sát thủ 9x", 1, "lenhbaiadmintestserver.lua:gopos_9x") },
            { "gopos_2x", new(73, 1544, 2944, "Boss Sát thủ 2x", 1, "lenhbaiadmintestserver.lua:gopos_2x") },
            { "gopos_3x", new(4, 1576, 2992, "Boss Sát thủ 3x", 1, "lenhbaiadmintestserver.lua:gopos_3x") },
            { "gopos_4x", new(5, 1616, 3472, "Boss Sát thủ 4x", 1, "lenhbaiadmintestserver.lua:gopos_4x") },
            { "gopos_5x", new(12, 1792, 3168, "Boss Sát thủ 5x", 1, "lenhbaiadmintestserver.lua:gopos_5x") },
            { "gopos_6x", new(164, 1784, 3120, "Boss Sát thủ 6x", 1, "lenhbaiadmintestserver.lua:gopos_6x") },
            { "gopos_7x", new(123, 1600, 3200, "Boss Sát thủ 7x", 1, "lenhbaiadmintestserver.lua:gopos_7x") },
            { "gopos_8x", new(201, 1768, 3200, "Boss Sát thủ 8x", 1, "lenhbaiadmintestserver.lua:gopos_8x") },
        };

        [Serializable] private sealed class AliasCatalogJson { public AliasEntry[] aliases; }
        [Serializable] private sealed class AliasEntry { public int mapId; public string geometryKey; }
        [Serializable] private sealed class GeometryCatalogJson { public GeometryEntry[] geometries; }
        [Serializable] private sealed class GeometryEntry { public string geometryKey; public RectDef bounds; }
    }
}
