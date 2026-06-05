// -----------------------------------------------------------------------------
// VLTK Mobile — PC map data batch loader
// Orchestrates maplist / cavelist / tong / waypoint / scroll / wharf / revivepos
// parsers and merges parsed entries into a MapCatalog-shaped payload consumable
// by MapCatalogLoader. Source files are GB2312 by default; falls back to UTF-8
// when the GB2312 decoder rejects bytes (covers nativeplacelist variants).
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public class PcMapDataBatchResult
    {
        public List<MapEntry> maps = new();
        public List<CaveEntry> caves = new();
        public List<TongMapEntry> tongs = new();
        public List<WaypointEntry> waypoints = new();
        public List<ScrollEntry> scrolls = new();
        public List<WharfEntry> wharves = new();
        public List<RevivePos> revivePositions = new();

        public int TotalParsed =>
            maps.Count + caves.Count + tongs.Count + waypoints.Count +
            scrolls.Count + wharves.Count + revivePositions.Count;
    }

    public static class PcMapDataBatchLoader
    {
        public const string MapListFileName = "maplist.ini";
        public const string CaveListFileName = "cavelist.ini";
        public const string WaypointFileName = "waypoint.txt";
        public const string ScrollFileName = "scroll.txt";
        public const string WharfFileName = "wharf.txt";
        public const string RevivePosFileName = "revivepos.ini";
        public const string TongDirectoryName = "tong";

        public static PcMapDataBatchResult Load(string serverSettingsPath, string serverMapsPath = null)
        {
            var result = new PcMapDataBatchResult();
            if (string.IsNullOrEmpty(serverSettingsPath) || !Directory.Exists(serverSettingsPath))
            {
                SubsystemLog.Warn("PcMapBatch", $"Settings dir not found: {serverSettingsPath}");
                return result;
            }

            var mapListPath = FindFile(serverSettingsPath, "maplist", ".ini");
            var caveListPath = FindFile(serverSettingsPath, "cavelist", ".ini");
            var waypointPath = FindFile(serverSettingsPath, "waypoint", ".txt");
            var scrollPath = FindFile(serverSettingsPath, "scroll", ".txt");
            var wharfPath = FindFile(serverSettingsPath, "wharf", ".txt");
            var revivePosPath = FindFile(serverSettingsPath, "revivepos", ".ini");

            if (mapListPath != null)
                result.maps = PcMapListParser.ParseFile(mapListPath);
            if (caveListPath != null)
                result.caves = PcCaveListParser.ParseFile(caveListPath, result.maps);
            result.tongs = PcTongListParser.ParseFile(serverMapsPath, result.maps);
            if (waypointPath != null)
                result.waypoints = PcWaypointParser.ParseFile(waypointPath);
            if (scrollPath != null)
                result.scrolls = PcScrollParser.ParseFile(scrollPath);
            if (wharfPath != null)
                result.wharves = PcWharfParser.ParseFile(wharfPath);
            if (revivePosPath != null)
                result.revivePositions = PcRevivePosParser.ParseFile(revivePosPath, result.maps);

            SubsystemLog.Info("PcMapBatch",
                $"Total {result.TotalParsed} entries: " +
                $"maps={result.maps.Count} caves={result.caves.Count} tongs={result.tongs.Count} " +
                $"waypoints={result.waypoints.Count} scrolls={result.scrolls.Count} " +
                $"wharves={result.wharves.Count} revive={result.revivePositions.Count}");
            return result;
        }

        private static string FindFile(string dir, string stem, string ext)
        {
            string direct = Path.Combine(dir, stem + ext);
            if (File.Exists(direct)) return direct;
            var matches = Directory.GetFiles(dir, stem + "*" + ext, SearchOption.TopDirectoryOnly);
            return matches.Length > 0 ? matches[0] : null;
        }

        public static List<MapEntry> BuildMapCatalog(PcMapDataBatchResult batch)
        {
            return batch?.maps ?? new List<MapEntry>();
        }
    }
}
