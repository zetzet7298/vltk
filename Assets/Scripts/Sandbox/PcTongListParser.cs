// -----------------------------------------------------------------------------
// VLTK Mobile — PC tong list parser
// PC stores tong (bang hội) bases as MapType=Tong entries in maplist.ini and
// per-map config under settings/maps/tong*/ directories. We derive tong list
// from parsed maplist (MapType=Tong), preserving the PC id, name and level
// range, then enrich with the first .ini/.txt config path under settings/maps/
// matching the same name fragment if present.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcTongListParser
    {
        public static List<TongMapEntry> ParseFromMapList(IReadOnlyList<MapEntry> mapList, string mapsRoot = null)
        {
            var rows = new List<TongMapEntry>();
            if (mapList == null) return rows;

            var candidates = new List<MapEntry>();
            foreach (var m in mapList)
            {
                if (m == null) continue;
                if (string.Equals(m.mapType, "Tong", StringComparison.OrdinalIgnoreCase))
                    candidates.Add(m);
            }
            candidates.Sort((a, b) => a.mapId.CompareTo(b.mapId));

            int tongId = 0;
            foreach (var m in candidates)
            {
                tongId++;
                rows.Add(new TongMapEntry
                {
                    tongId = tongId,
                    mapId = m.mapId,
                    nameRaw = m.nameRaw,
                    nameNormalized = m.nameNormalized,
                    levelMin = m.levelMin,
                    levelMax = m.levelMax,
                    tongWarConfig = FindTongWarConfig(mapsRoot, m.nameRaw),
                    sourceMapPath = m.sourceMapPath ?? string.Empty,
                });
            }

            SubsystemLog.Info("PcTongList", $"Parsed {rows.Count} tong map rows");
            return rows;
        }

        public static List<TongMapEntry> ParseFile(string mapsRoot, IReadOnlyList<MapEntry> mapList)
        {
            return ParseFromMapList(mapList, mapsRoot);
        }

        private static string FindTongWarConfig(string mapsRoot, string nameFragment)
        {
            if (string.IsNullOrEmpty(mapsRoot) || !Directory.Exists(mapsRoot))
                return string.Empty;
            if (string.IsNullOrEmpty(nameFragment)) return string.Empty;
            string[] dirs;
            try
            {
                dirs = Directory.GetDirectories(mapsRoot);
            }
            catch
            {
                return string.Empty;
            }
            foreach (var d in dirs)
            {
                var leaf = SafeLeaf(d);
                if (string.IsNullOrEmpty(leaf)) continue;
                if (leaf.IndexOf("tong", StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (leaf.IndexOf(nameFragment, StringComparison.OrdinalIgnoreCase) >= 0)
                    return d;
            }
            return string.Empty;
        }

        private static string SafeLeaf(string dir)
        {
            try
            {
                var info = new DirectoryInfo(dir);
                return info.Name;
            }
            catch
            {
                try { return Path.GetFileName(dir); } catch { return string.Empty; }
            }
        }
    }
}
