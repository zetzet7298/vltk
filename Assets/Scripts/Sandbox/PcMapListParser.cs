// -----------------------------------------------------------------------------
// VLTK Mobile — PC maplist.ini parser
// Source: settings/maplist.ini (1,005 maps, GB2312 encoded)
// Each entry is grouped by id: `1=path`, `1_name=Vietnamese`, `1_MapType=City`,
// `1_MapPos=169,287`, `1_AutoGoldenNpc=2000`, `1_GoldenType=13`,
// `1_GoldenDropRate=...`, `1_NormalDropRate=...`, `1_NpcAutoLevelMin=25`,
// `1_NpcAutoLevelMax=25`. Returns List<MapEntry> for MapCatalog merge.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcMapListParser
    {
        public const string DefaultMapType = "Field";

        public static List<MapEntry> ParseFile(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<MapEntry>();
            return ParseLines(ReadLines(absolutePath));
        }

        public static List<MapEntry> ParseLines(IEnumerable<string> lines)
        {
            var rows = new List<MapEntry>();
            if (lines == null) return rows;

            var groups = new Dictionary<int, Dictionary<string, string>>();

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue;
                if (line[0] == '/' && line.Length > 1 && line[1] == '/') continue;
                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (string.IsNullOrEmpty(key)) continue;

                int underscore = key.IndexOf('_');
                if (underscore <= 0) continue;
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int mapId))
                    continue;
                var subKey = key.Substring(underscore + 1);
                if (string.IsNullOrEmpty(subKey)) continue;

                if (!groups.TryGetValue(mapId, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[mapId] = bag;
                }
                bag[subKey] = value;
            }

            foreach (var kv in groups)
            {
                int mapId = kv.Key;
                var bag = kv.Value;
                if (!bag.TryGetValue("name", out var rawName) || string.IsNullOrEmpty(rawName))
                    continue;
                bag.TryGetValue("maptype", out var mapType);
                if (string.IsNullOrEmpty(mapType)) mapType = DefaultMapType;
                bag.TryGetValue("mappath", out var sourceMapPath);
                if (string.IsNullOrEmpty(sourceMapPath))
                    sourceMapPath = bag.TryGetValue("path", out var pathAlt) ? pathAlt : string.Empty;
                int posX = 0, posY = 0;
                if (bag.TryGetValue("mappos", out var posStr) && !string.IsNullOrEmpty(posStr))
                {
                    var parts = posStr.Split(',');
                    if (parts.Length >= 1) int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posX);
                    if (parts.Length >= 2) int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posY);
                }
                int levelMin = 0, levelMax = 0;
                int.TryParse(BagValue(bag, "npcautolevelmin"), NumberStyles.Integer, CultureInfo.InvariantCulture, out levelMin);
                int.TryParse(BagValue(bag, "npcautolevelmax"), NumberStyles.Integer, CultureInfo.InvariantCulture, out levelMax);
                int autoGoldenNpc = 0, goldenType = 0;
                int.TryParse(BagValue(bag, "autogoldennpc"), NumberStyles.Integer, CultureInfo.InvariantCulture, out autoGoldenNpc);
                int.TryParse(BagValue(bag, "goldentype"), NumberStyles.Integer, CultureInfo.InvariantCulture, out goldenType);

                rows.Add(new MapEntry
                {
                    mapId = mapId,
                    nameRaw = rawName,
                    nameNormalized = rawName.Trim(),
                    mapType = mapType,
                    mapPosX = posX,
                    mapPosY = posY,
                    levelMin = levelMin,
                    levelMax = levelMax,
                    autoGoldenNpc = autoGoldenNpc,
                    goldenType = goldenType,
                    goldenDropRate = BagValue(bag, "goldendroprate"),
                    normalDropRate = BagValue(bag, "normaldroprate"),
                    sourceMapPath = sourceMapPath ?? string.Empty,
                });
            }

            rows.Sort((a, b) => a.mapId.CompareTo(b.mapId));
            SubsystemLog.Info("PcMapList", $"Parsed {rows.Count} map rows");
            return rows;
        }

        private static string BagValue(Dictionary<string, string> bag, string key)
        {
            if (bag == null) return string.Empty;
            return bag.TryGetValue(key, out var v) ? v : string.Empty;
        }

        public static List<MapEntry> BuildMapCatalog(IReadOnlyList<MapEntry> parsed)
        {
            var result = new List<MapEntry>();
            if (parsed == null) return result;
            foreach (var m in parsed)
            {
                if (m == null || m.mapId <= 0) continue;
                if (string.IsNullOrEmpty(m.nameRaw)) continue;
                result.Add(m);
            }
            return result;
        }

        public static string[] ReadLines(string absolutePath)
        {
            var bytes = File.ReadAllBytes(absolutePath);
            string text = TryDecodeText(bytes);
            if (text.Length > 0 && text[0] == '\ufeff') text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }

        private static string TryDecodeText(byte[] bytes)
        {
            var utf8Strict = new UTF8Encoding(false, true);
            try
            {
                return utf8Strict.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
            }
            try
            {
                return Encoding.GetEncoding("GB2312").GetString(bytes);
            }
            catch
            {
                return Encoding.UTF8.GetString(bytes);
            }
        }
    }
}
