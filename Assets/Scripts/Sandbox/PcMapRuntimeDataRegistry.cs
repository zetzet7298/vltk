// -----------------------------------------------------------------------------
// VLTK Mobile — runtime registry for PC map travel data
// Source: settings/waypoint.txt, scroll.txt, wharf.txt, revivepos.ini
// Purpose: expose parsed PC travel/revive data to mobile runtime services.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class PcMapRuntimeDataRegistry
    {
        private readonly Dictionary<int, List<WaypointEntry>> _waypointsByMap = new();
        private readonly Dictionary<int, List<ScrollEntry>> _scrollsByMap = new();
        private readonly Dictionary<int, List<WharfEntry>> _wharvesByMap = new();
        private readonly Dictionary<int, List<RevivePos>> _reviveByMap = new();

        public int WaypointCount { get; private set; }
        public int ScrollCount { get; private set; }
        public int WharfCount { get; private set; }
        public int ReviveCount { get; private set; }

        public static PcMapRuntimeDataRegistry FromBatch(PcMapDataBatchResult batch)
        {
            var registry = new PcMapRuntimeDataRegistry();
            registry.Load(batch);
            return registry;
        }

        public static PcMapRuntimeDataRegistry LoadFromStreamingAssets()
        {
            var pcMapDir = Path.Combine(Application.streamingAssetsPath, "Reference/PcMap");
            if (!Directory.Exists(pcMapDir)) return new PcMapRuntimeDataRegistry();
            return FromBatch(PcMapDataBatchLoader.Load(pcMapDir, pcMapDir));
        }

        public IReadOnlyList<WaypointEntry> GetWaypointsForMap(int mapId)
            => _waypointsByMap.TryGetValue(mapId, out var rows) ? rows : System.Array.Empty<WaypointEntry>();

        public IReadOnlyList<ScrollEntry> GetScrollsForMap(int mapId)
            => _scrollsByMap.TryGetValue(mapId, out var rows) ? rows : System.Array.Empty<ScrollEntry>();

        public IReadOnlyList<WharfEntry> GetWharvesForMap(int mapId)
            => _wharvesByMap.TryGetValue(mapId, out var rows) ? rows : System.Array.Empty<WharfEntry>();

        public IReadOnlyList<RevivePos> GetRevivePositionsForMap(int mapId)
            => _reviveByMap.TryGetValue(mapId, out var rows) ? rows : System.Array.Empty<RevivePos>();

        public RevivePos GetDefaultRevivePosition(int mapId)
        {
            var rows = GetRevivePositionsForMap(mapId);
            return rows.Count > 0 ? rows[0] : null;
        }

        private void Load(PcMapDataBatchResult batch)
        {
            if (batch == null) return;

            foreach (var w in batch.waypoints)
            {
                if (w == null || w.mapId <= 0) continue;
                Add(_waypointsByMap, w.mapId, w);
                WaypointCount++;
            }

            foreach (var s in batch.scrolls)
            {
                if (s == null) continue;
                ScrollCount++;
                // PC scroll.txt is a scroll value table in some versions (no map target).
                // Only index by map when the parser can resolve a positive mapId.
                if (s.mapId > 0)
                    Add(_scrollsByMap, s.mapId, s);
            }

            foreach (var wharf in batch.wharves)
            {
                if (wharf == null || wharf.mapId <= 0) continue;
                Add(_wharvesByMap, wharf.mapId, wharf);
                WharfCount++;
            }

            foreach (var revive in batch.revivePositions)
            {
                if (revive == null || revive.mapId <= 0) continue;
                Add(_reviveByMap, revive.mapId, revive);
                ReviveCount++;
            }
        }

        private static void Add<T>(Dictionary<int, List<T>> byMap, int mapId, T entry)
        {
            if (!byMap.TryGetValue(mapId, out var rows))
            {
                rows = new List<T>();
                byMap[mapId] = rows;
            }
            rows.Add(entry);
        }
    }
}
