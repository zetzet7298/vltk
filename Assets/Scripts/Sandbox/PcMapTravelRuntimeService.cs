// -----------------------------------------------------------------------------
// VLTK Mobile — PC map travel runtime service
// Source: Client 6.0/settings/{waypoint.txt, wharf.txt, revivepos.ini, scroll.txt}
// Purpose: one service-level facade for exact PC travel/revive lookup proof.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public sealed class PcMapTravelRuntimeService
    {
        private readonly WaypointService _waypoints;
        private readonly WharfService _wharves;
        private readonly RevivePosService _revives;
        private readonly ScrollService _scrolls;

        public PcMapRuntimeDataRegistry Registry { get; }
        public int WaypointCount => _waypoints.Count;
        public int WharfCount => _wharves.Count;
        public int ReviveCount => _revives.Count;
        public int ScrollValueCount => _scrolls.Count;

        public PcMapTravelRuntimeService(
            PcMapRuntimeDataRegistry registry,
            WaypointService waypoints,
            WharfService wharves,
            RevivePosService revives,
            ScrollService scrolls)
        {
            Registry = registry ?? new PcMapRuntimeDataRegistry();
            _waypoints = waypoints ?? new WaypointService(null);
            _wharves = wharves ?? new WharfService(null);
            _revives = revives ?? new RevivePosService(null);
            _scrolls = scrolls ?? new ScrollService();
        }

        public static PcMapTravelRuntimeService LoadFromStreamingAssets(string relativeDir = "Reference/PcMap")
        {
            return LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, relativeDir));
        }

        public static PcMapTravelRuntimeService LoadFromDirectory(string pcMapDir)
        {
            if (string.IsNullOrEmpty(pcMapDir) || !Directory.Exists(pcMapDir))
                return Empty();

            var batch = PcMapDataBatchLoader.Load(pcMapDir, pcMapDir);
            var scrollService = new ScrollService();
            scrollService.AttachRegistry(BuildExactScrollRegistry(pcMapDir));

            return new PcMapTravelRuntimeService(
                PcMapRuntimeDataRegistry.FromBatch(batch),
                new WaypointService(PcWaypointParser.BuildRegistry(pcMapDir)),
                new WharfService(PcWharfParser.BuildRegistry(pcMapDir)),
                new RevivePosService(PcRevivePosParser.BuildRegistry(pcMapDir)),
                scrollService);
        }

        private static PcScrollRegistry BuildExactScrollRegistry(string pcMapDir)
        {
            var registry = new PcScrollRegistry();
            var scrollPath = Path.Combine(pcMapDir, PcMapDataBatchLoader.ScrollFileName);
            foreach (var scroll in PcScrollParser.ParseFile(scrollPath))
                registry.Register(scroll);
            return registry;
        }

        public static PcMapTravelRuntimeService Empty()
        {
            var scrollService = new ScrollService();
            scrollService.AttachRegistry(new PcScrollRegistry());
            return new PcMapTravelRuntimeService(
                new PcMapRuntimeDataRegistry(),
                new WaypointService(new PcWaypointRegistry()),
                new WharfService(new PcWharfRegistry()),
                new RevivePosService(new PcRevivePosRegistry()),
                scrollService);
        }

        public PcWaypointEntry GetWaypoint(int waypointId) => _waypoints.GetWaypoint(waypointId);
        public IEnumerable<PcWaypointEntry> GetWaypointServiceRowsForMap(int mapId) => _waypoints.GetByMap(mapId);
        public IReadOnlyList<WaypointEntry> GetWaypointsForMap(int mapId) => Registry.GetWaypointsForMap(mapId);

        public PcWharfEntry GetWharf(int wharfId) => _wharves.GetWharf(wharfId);
        public IEnumerable<PcWharfEntry> GetWharfServiceRowsForMap(int mapId) => _wharves.GetByFromMap(mapId);
        public IReadOnlyList<WharfEntry> GetWharvesForMap(int mapId) => Registry.GetWharvesForMap(mapId);

        public PcRevivePosEntry GetRevive(int reviveId) => _revives.GetRevive(reviveId);
        public IEnumerable<PcRevivePosEntry> GetReviveServiceRowsForMap(int mapId) => _revives.GetByMap(mapId);
        public IReadOnlyList<RevivePos> GetRevivePositionsForMap(int mapId) => Registry.GetRevivePositionsForMap(mapId);
        public RevivePos GetDefaultRevivePosition(int mapId) => Registry.GetDefaultRevivePosition(mapId);

        public PcScrollEntry GetScrollValue(int scrollId) => _scrolls.GetScroll(scrollId);
        public List<PcScrollEntry> GetScrollValuesByFromMap(int fromMapId) => _scrolls.GetByFromMap(fromMapId);
        public List<PcScrollEntry> GetScrollValuesByToMap(int toMapId) => _scrolls.GetByToMap(toMapId);
        public IReadOnlyList<ScrollEntry> GetScrollMapRowsForMap(int mapId) => Registry.GetScrollsForMap(mapId);
    }
}
