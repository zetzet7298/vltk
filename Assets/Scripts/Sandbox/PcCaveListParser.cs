// -----------------------------------------------------------------------------
// VLTK Mobile — PC cavelist.ini parser
// Source: settings/cavelist.ini (subset of maplist with cave-scoped entries,
// GB2312 encoded). Each entry shares `id_name`, `id_MapPos`. Level range and
// bossTemplateId are joined from the matching maplist entry when available.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcCaveListParser
    {
        public static List<CaveEntry> ParseFile(string absolutePath, IReadOnlyList<MapEntry> mapList = null)
        {
            if (string.IsNullOrEmpty(absolutePath) || !File.Exists(absolutePath))
                return new List<CaveEntry>();
            return ParseLines(PcMapListParser.ReadLines(absolutePath), mapList);
        }

        public static List<CaveEntry> ParseLines(IEnumerable<string> lines, IReadOnlyList<MapEntry> mapList = null)
        {
            var rows = new List<CaveEntry>();
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
                if (!int.TryParse(key.Substring(0, underscore), NumberStyles.Integer, CultureInfo.InvariantCulture, out int caveId))
                    continue;
                var subKey = key.Substring(underscore + 1);
                if (string.IsNullOrEmpty(subKey)) continue;
                if (!groups.TryGetValue(caveId, out var bag))
                {
                    bag = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    groups[caveId] = bag;
                }
                bag[subKey] = value;
            }

            var mapById = new Dictionary<int, MapEntry>();
            if (mapList != null)
            {
                foreach (var m in mapList)
                {
                    if (m == null) continue;
                    mapById[m.mapId] = m;
                }
            }

            foreach (var kv in groups)
            {
                int caveId = kv.Key;
                var bag = kv.Value;
                if (!bag.TryGetValue("name", out var rawName) || string.IsNullOrEmpty(rawName))
                    continue;
                int posX = 0, posY = 0;
                string sourceMapPath = string.Empty;
                if (bag.TryGetValue("mappath", out var mappathVal) && !string.IsNullOrEmpty(mappathVal))
                    sourceMapPath = mappathVal;
                else if (bag.TryGetValue("path", out var pathVal) && !string.IsNullOrEmpty(pathVal))
                    sourceMapPath = pathVal;
                if (bag.TryGetValue("mappos", out var posStr) && !string.IsNullOrEmpty(posStr))
                {
                    var parts = posStr.Split(',');
                    if (parts.Length >= 1) int.TryParse(parts[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posX);
                    if (parts.Length >= 2) int.TryParse(parts[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out posY);
                }
                int levelMin = 0, levelMax = 0, bossTemplateId = 0;
                if (mapById.TryGetValue(caveId, out var mEntry))
                {
                    levelMin = mEntry.levelMin;
                    levelMax = mEntry.levelMax;
                    bossTemplateId = mEntry.autoGoldenNpc;
                    if (string.IsNullOrEmpty(sourceMapPath))
                        sourceMapPath = mEntry.sourceMapPath;
                }

                rows.Add(new CaveEntry
                {
                    caveId = caveId,
                    mapId = caveId,
                    nameRaw = rawName,
                    nameNormalized = rawName.Trim(),
                    mapPosX = posX,
                    mapPosY = posY,
                    levelMin = levelMin,
                    levelMax = levelMax,
                    bossTemplateId = bossTemplateId,
                    sourceMapPath = sourceMapPath ?? string.Empty,
                });
            }

            rows.Sort((a, b) => a.caveId.CompareTo(b.caveId));
            SubsystemLog.Info("PcCaveList", $"Parsed {rows.Count} cave rows");
            return rows;
        }
    }
}
