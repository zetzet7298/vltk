// -----------------------------------------------------------------------------
// VLTK Mobile — map 907 pure C# runtime smoke proof.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Pure C# proof for default map 907 readiness. It intentionally avoids scene,
    /// UI, screenshot, and Unity-MCP dependencies: only runtime catalogs/services that
    /// are available to EditMode tests are exercised.
    /// </summary>
    public sealed class Map907RuntimeSmokeService
    {
        public const int MapId = MapPortManifest.VuotAiNhiepThiTranId;
        public const string ExpectedNameVi = "Vượt ải Nhiếp Thí Trần";
        public const string ExpectedGeometryKey = "g_a7649e666581b845";

        public static readonly RectDef ExpectedBounds = new RectDef
        {
            x = 39424f,
            y = -56320f,
            width = 14848f,
            height = 7168f,
        };

        public static readonly Vector2 RepresentativeSpawn = new Vector2(47232f, -52544f);
        public static readonly Vector2 OutOfBoundsClickTarget = new Vector2(999999f, -999999f);
        public static readonly Vector2 ExpectedClampedTarget = new Vector2(54272f, -56320f);
        public static readonly Vector2 ProbeMinimapSize = new Vector2(256f, 256f);

        public Map907RuntimeSmokeResult Run(
            MapManager mapManager = null,
            MinimapService minimapService = null,
            MapInteractiveCatalogFile interactiveCatalog = null,
            SandboxPlayerController playerController = null)
        {
            var result = new Map907RuntimeSmokeResult();
            var registry = new AssetRegistry();
            mapManager ??= new MapManager(registry);
            minimapService ??= new MinimapService(registry);

            mapManager.LoadCatalog();
            result.CatalogCount = mapManager.Catalog.Count;
            result.MapExists = mapManager.Catalog.TryGetValue(MapId, out var entry);
            if (!result.MapExists)
            {
                result.MissingRuntimeDependency = "MapManager.Catalog thiếu map 907 sau LoadCatalog().";
                result.Notes.Add(result.MissingRuntimeDependency);
                return result;
            }

            result.MapName = entry.displayNameNormalized;
            result.NameMatchesExpected = string.Equals(result.MapName, ExpectedNameVi, StringComparison.Ordinal);
            result.GeometryKey = entry.geometryKey;
            result.GeometryKeyMatchesExpected = string.Equals(result.GeometryKey, ExpectedGeometryKey, StringComparison.OrdinalIgnoreCase);

            mapManager.LoadMap(MapId);
            result.ActiveMapId = mapManager.ActiveMapId;
            var map = mapManager.ActiveMap;
            result.ActiveMapLoaded = map != null && result.ActiveMapId == MapId;
            result.Bounds = map?.sourceBoundsRect;
            result.BoundsUsable = IsUsable(result.Bounds);
            result.BoundsMatchCommittedData = RectNearlyEquals(result.Bounds, ExpectedBounds);

            if (!result.ActiveMapLoaded || !result.BoundsUsable)
            {
                result.MissingRuntimeDependency = "MapManager không expose ActiveMap.sourceBoundsRect usable cho map 907.";
                result.Notes.Add(result.MissingRuntimeDependency);
                return result;
            }

            result.RepresentativeWorld = RepresentativeSpawn;
            result.RepresentativeNormalized = minimapService.WorldToMinimapNormalized(map, RepresentativeSpawn);
            result.RepresentativePixel = minimapService.WorldToMinimapPixel(map, RepresentativeSpawn, ProbeMinimapSize);
            result.RoundTripWorld = minimapService.MinimapPixelToWorld(map, result.RepresentativePixel, ProbeMinimapSize);
            result.RoundTripMatchesRepresentative = NearlyEquals(result.RoundTripWorld, RepresentativeSpawn, 0.05f);

            result.ClampedOutOfBoundsTarget = ClampToBounds(result.Bounds, OutOfBoundsClickTarget);
            result.ClampedTargetMatchesCommittedData = NearlyEquals(result.ClampedOutOfBoundsTarget, ExpectedClampedTarget, 0.01f);
            result.MinimapBottomRightClickWorld = minimapService.MinimapNormalizedToWorld(map, new Vector2(1f, 1f));
            result.MinimapClickClampMatchesTarget = NearlyEquals(result.MinimapBottomRightClickWorld, ExpectedClampedTarget, 0.01f);

            if (playerController != null)
            {
                playerController.allowKeyboardFallback = false;
                playerController.followCameraEnabled = false;
                playerController.SetMapBounds(result.Bounds);
                playerController.MoveTo(OutOfBoundsClickTarget);
                result.PlayerControllerClampProbeRan = true;
                result.PlayerControllerMoveTarget = playerController.MoveTarget;
                result.PlayerControllerClampMatchesCommittedData = NearlyEquals(result.PlayerControllerMoveTarget, ExpectedClampedTarget, 0.01f);
            }
            else
            {
                result.Notes.Add("SandboxPlayerController clamp probe not supplied; pure C# minimap/bounds clamp proof still ran.");
            }

            interactiveCatalog ??= MapInteractiveCatalogRuntime.LoadFromStreamingAssets();
            result.TrapCatalogLoaded = interactiveCatalog?.geometries != null && interactiveCatalog.geometries.Length > 0;
            var geometry = interactiveCatalog?.FindForMap(map);
            result.TrapGeometryFound = geometry != null;
            if (geometry != null)
            {
                result.TrapCount = geometry.trapCount;
                result.ObjectCount = geometry.objectCount;
                result.TrapCountsMatchCommittedData = geometry.trapCount == 16 && geometry.objectCount == 0;
                result.StaticTrapClearForMap = Contains(geometry.staticTrapClearMapIds, MapId);
                result.AllTrapScriptsResolved = geometry.traps != null && geometry.traps.Length == geometry.trapCount && AllResolved(geometry.traps);
            }

            result.MissingRuntimeDependency = result.Success
                ? string.Empty
                : "Một hoặc nhiều dependency pure C# cho map 907 chưa sẵn sàng; xem flags trong result.";
            result.Notes.Add("Scene/player feel smoke vẫn cần chạy riêng; service này chỉ proof catalog/bounds/minimap/trap surfaces pure C#.");
            return result;
        }

        private static bool IsUsable(RectDef rect)
            => rect != null && rect.width > 0f && rect.height > 0f;

        private static Vector2 ClampToBounds(RectDef rect, Vector2 point)
            => new Vector2(
                Mathf.Clamp(point.x, rect.x, rect.x + rect.width),
                Mathf.Clamp(point.y, rect.y, rect.y + rect.height));

        private static bool RectNearlyEquals(RectDef actual, RectDef expected)
            => actual != null &&
               Mathf.Approximately(actual.x, expected.x) &&
               Mathf.Approximately(actual.y, expected.y) &&
               Mathf.Approximately(actual.width, expected.width) &&
               Mathf.Approximately(actual.height, expected.height);

        private static bool NearlyEquals(Vector2 a, Vector2 b, float epsilon)
            => Mathf.Abs(a.x - b.x) <= epsilon && Mathf.Abs(a.y - b.y) <= epsilon;

        private static bool Contains(int[] values, int target)
        {
            if (values == null) return false;
            for (int i = 0; i < values.Length; i++)
                if (values[i] == target) return true;
            return false;
        }

        private static bool AllResolved(MapInteractiveTrap[] traps)
        {
            if (traps == null || traps.Length == 0) return false;
            for (int i = 0; i < traps.Length; i++)
                if (traps[i] == null || !traps[i].scriptResolved || string.IsNullOrEmpty(traps[i].scriptPath))
                    return false;
            return true;
        }
    }

    public sealed class Map907RuntimeSmokeResult
    {
        public int CatalogCount;
        public bool MapExists;
        public string MapName;
        public bool NameMatchesExpected;
        public string GeometryKey;
        public bool GeometryKeyMatchesExpected;
        public int ActiveMapId;
        public bool ActiveMapLoaded;
        public RectDef Bounds;
        public bool BoundsUsable;
        public bool BoundsMatchCommittedData;
        public Vector2 RepresentativeWorld;
        public Vector2 RepresentativeNormalized;
        public Vector2 RepresentativePixel;
        public Vector2 RoundTripWorld;
        public bool RoundTripMatchesRepresentative;
        public Vector2 ClampedOutOfBoundsTarget;
        public bool ClampedTargetMatchesCommittedData;
        public Vector2 MinimapBottomRightClickWorld;
        public bool MinimapClickClampMatchesTarget;
        public bool PlayerControllerClampProbeRan;
        public Vector2 PlayerControllerMoveTarget;
        public bool PlayerControllerClampMatchesCommittedData;
        public bool TrapCatalogLoaded;
        public bool TrapGeometryFound;
        public int TrapCount;
        public int ObjectCount;
        public bool TrapCountsMatchCommittedData;
        public bool StaticTrapClearForMap;
        public bool AllTrapScriptsResolved;
        public string MissingRuntimeDependency = string.Empty;
        public List<string> Notes = new List<string>();

        public bool Success =>
            MapExists &&
            NameMatchesExpected &&
            GeometryKeyMatchesExpected &&
            ActiveMapLoaded &&
            BoundsUsable &&
            BoundsMatchCommittedData &&
            RoundTripMatchesRepresentative &&
            ClampedTargetMatchesCommittedData &&
            MinimapClickClampMatchesTarget &&
            (!PlayerControllerClampProbeRan || PlayerControllerClampMatchesCommittedData) &&
            TrapCatalogLoaded &&
            TrapGeometryFound &&
            TrapCountsMatchCommittedData &&
            StaticTrapClearForMap &&
            AllTrapScriptsResolved;
    }
}
