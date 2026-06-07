using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sprites;

namespace VLTK.Sandbox
{
    public class MapRenderer : MonoBehaviour
    {
        private ObstacleOverlayRenderer _obstacleOverlay;
        private Transform _mapRoot;
        private int _loadedMapId = -1;
        private SprRuntimeService _sprService;

        public int LoadedMapId => _loadedMapId;
        public ObstacleOverlayRenderer ObstacleOverlay => _obstacleOverlay;
        public SprRuntimeService SprService => _sprService;

        /// <summary>World-space bounds of the rendered map content (valid when <see cref="HasContent"/>).</summary>
        public Bounds ContentBounds { get; private set; }
        public bool HasContent { get; private set; }

        private void Awake()
        {
            _obstacleOverlay = gameObject.AddComponent<ObstacleOverlayRenderer>();
            _mapRoot = new GameObject("MapContent").transform;
            _mapRoot.SetParent(transform, false);
            _sprService = new SprRuntimeService();
        }

        public void LoadMapRegions(MapDefinition mapDef)
        {
            if (mapDef == null) return;

            Clear();
            _loadedMapId = mapDef.catalogEntry.mapId;

            var catalogEntry = mapDef.catalogEntry;
            SubsystemLog.Info("MapRenderer",
                $"Loading map {catalogEntry.displayNameNormalized} " +
                $"({mapDef.regionCountX}x{mapDef.regionCountY} regions)");

            // SPR textures are decoded on demand per ground/builtin name.
            LoadSampleRegions(mapDef);

            // Log SPR resolution stats
            SubsystemLog.Info("MapRenderer",
                $"SPR stats: {_sprService.CacheCount} resolved, {_sprService.MissCount} missing");
        }

        public void Clear()
        {
            _obstacleOverlay.Clear();
            for (int i = _mapRoot.childCount - 1; i >= 0; i--)
                Destroy(_mapRoot.GetChild(i).gameObject);
            _sprService?.ClearCache();
            foreach (var t in _proceduralTex)
                if (t != null) Destroy(t);
            _proceduralTex.Clear();
            _spriteCache.Clear();
            _builtinSortCounter = 0;
            _loadedMapId = -1;
            ContentBounds = new Bounds();
            HasContent = false;
        }

        // JX region scene constants (KScenePlaceRegionC): scene 512x1024, ground cell 32.
        private const int RegionSceneWidth = 512;
        private const int GroundCell = 32;

        // sortingOrder is a 16-bit field (-32768..32767). The map spans screen-Y up to
        // ~100000, so the old "screenY*2 clamped to ±32000" scheme overflowed AND
        // saturated thousands of objects at the same ceiling value, leaving their relative
        // draw order undefined (gate/house pieces occluded each other). Depth is now driven
        // by the camera's CustomAxis transparency sort on world-Y (see SandboxManager.
        // FrameCameraOnMap); these constants only separate the coarse layers.
        public const int GroundSortingOrder = -1000;  // terrain, always beneath objects
        public const int CoverSortingOrder = 0;       // flat ground decals (grass/road), Y-sorted
        public const int BuiltinSortingOrder = 1000;  // base for structures/trees (above cover)
        public const int PlayerSortingOrder = 5000;   // actors above static map art

        private static string ResolveRegionFolder(string regionFolder)
        {
            if (string.IsNullOrEmpty(regionFolder)) return null;
            if (Path.IsPathRooted(regionFolder)) return regionFolder;
            return Path.Combine(Application.streamingAssetsPath, regionFolder);
        }

        private void LoadSampleRegions(MapDefinition mapDef)
        {
            // Prefer generated bulk-port regions, then legacy client-projected test regions.
            var generatedDir = ResolveRegionFolder(mapDef.catalogEntry?.regionFolder);
            var clientDir = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{mapDef.catalogEntry.mapId}_C");
            var legacyDir = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions", $"Map_{mapDef.catalogEntry.mapId}");
            var regionsDir = Directory.Exists(generatedDir) ? generatedDir : clientDir;
            if (!Directory.Exists(regionsDir))
                regionsDir = legacyDir;
            if (!Directory.Exists(regionsDir))
                regionsDir = Path.Combine(Application.streamingAssetsPath, "TestData", "Regions");
            if (!Directory.Exists(regionsDir))
            {
                SubsystemLog.Warn("MapRenderer", "No test region data available");
                ApplyFullMapBounds(mapDef);
                return;
            }

            var files = Directory.GetFiles(regionsDir, "*.dat");
            SubsystemLog.Info("MapRenderer", $"Found {files.Length} region files in {Path.GetFileName(regionsDir)}");

            // Region grid coordinates come from the filename: COL_ROW_Region(_C).dat.
            var entries = new List<(string path, int col, int row)>();
            foreach (var f in files)
            {
                var name = Path.GetFileNameWithoutExtension(f);
                var parts = name.Split('_');
                if (parts.Length >= 2 && int.TryParse(parts[0], out int c) && int.TryParse(parts[1], out int r))
                    entries.Add((f, c, r));
            }
            if (entries.Count == 0)
            {
                SubsystemLog.Warn("MapRenderer", "No coordinate-named region files found");
                ApplyFullMapBounds(mapDef);
                return;
            }

            bool fullInit = false;
            var fullBounds = new Bounds();
            // Per-cell content weight (cover + structures) for town-core framing.
            var cellWeight = new Dictionary<(int, int), int>();
            int rendered = 0;

            foreach (var e in entries)
            {
                var data = File.ReadAllBytes(e.path);
                var region = RegionParser.Parse(data);
                if (!region.success) continue;
                rendered++;
                int weight = 0;

                // Region screen origin (scene Y already halved): (col*512, row*512).
                float regionScreenX = e.col * RegionSceneWidth;
                float regionScreenY = e.row * RegionSceneWidth;

                // Debug obstacle overlay aligned to the same screen-space origin.
                if (region.HasObstacle)
                {
                    var grid = RegionParser.ExtractObstacle(region, _loadedMapId, e.col, e.row);
                    if (grid != null)
                        _obstacleOverlay.RenderRegion(grid, regionScreenX, -(regionScreenY + 16 * GroundCell));
                }

                if (region.HasGround)
                {
                    var ground = GroundLayerParser.ExtractFromRegion(region);
                    if (ground != null)
                    {
                        RenderGroundLayer(ground, e.col, e.row, regionScreenX, regionScreenY, ref fullBounds, ref fullInit);
                        RenderGroundCover(ground);
                        weight += ground.objects.Count;
                    }
                }

                if (region.HasBuiltin)
                {
                    var builtin = BuildinObjParser.ExtractFromRegion(region);
                    if (builtin != null)
                    {
                        RenderBuiltinObjects(builtin);
                        // Buildings (houses/bridges/walls) define the town core far more
                        // than scattered trees; weight named structures heavily so the
                        // camera frames the settlement rather than a forest patch.
                        foreach (var obj in builtin.objects)
                        {
                            string n = obj.imageName ?? "";
                            bool structure = n.Contains("房屋") || n.Contains("house") || n.Contains("House")
                                || n.Contains("桥") || n.Contains("building") || n.Contains("墙")
                                || n.Contains("牌坊") || n.Contains("井");
                            weight += structure ? 40 : 1;
                        }
                    }
                }

                cellWeight[(e.col, e.row)] = weight + 1; // +1 so any rendered cell counts
            }

            // Hide the obstacle overlay by default; toggle remains for debugging.
            _obstacleOverlay.Hide();

            // Frame the WxW region window with the most content (the town core)
            // rather than the sparse full extent — only a subset of regions matched.
            if (cellWeight.Count > 0)
            {
                const int W = 5;
                int minC = int.MaxValue, maxC = int.MinValue, minR = int.MaxValue, maxR = int.MinValue;
                foreach (var kv in cellWeight)
                {
                    minC = Mathf.Min(minC, kv.Key.Item1); maxC = Mathf.Max(maxC, kv.Key.Item1);
                    minR = Mathf.Min(minR, kv.Key.Item2); maxR = Mathf.Max(maxR, kv.Key.Item2);
                }

                int bestScore = -1, bestC = minC, bestR = minR;
                for (int cc = minC; cc <= maxC; cc++)
                for (int rr = minR; rr <= maxR; rr++)
                {
                    int score = 0;
                    for (int c = cc; c < cc + W; c++)
                    for (int r = rr; r < rr + W; r++)
                        if (cellWeight.TryGetValue((c, r), out int w)) score += w;
                    if (score > bestScore) { bestScore = score; bestC = cc; bestR = rr; }
                }

                float centerScreenX = (bestC + W * 0.5f) * RegionSceneWidth;
                float centerScreenY = (bestR + W * 0.5f) * RegionSceneWidth;
                float span = W * RegionSceneWidth;
                ContentBounds = new Bounds(
                    new Vector3(centerScreenX, -centerScreenY, 0f),
                    new Vector3(span, span, 1f));
                HasContent = true;

                SubsystemLog.Info("MapRenderer",
                    $"Town focus: cols {bestC}-{bestC + W - 1} rows {bestR}-{bestR + W - 1} (score {bestScore})");
            }
            else
            {
                ContentBounds = fullBounds;
                HasContent = fullInit;
            }

            ApplyFullMapBounds(mapDef);

            SubsystemLog.Info("MapRenderer",
                $"Rendered {rendered} regions; focus center={ContentBounds.center} size={ContentBounds.size}");
        }

        private void ApplyFullMapBounds(MapDefinition mapDef)
        {
            var r = mapDef?.sourceBoundsRect;
            if (r == null || r.width <= 0f || r.height <= 0f)
                return;

            ContentBounds = new Bounds(
                new Vector3(r.x + r.width * 0.5f, r.y + r.height * 0.5f, 0f),
                new Vector3(r.width, r.height, 1f));
            HasContent = true;
        }

        // Ground tiles tile a 512x512 screen block: sprite 64x64 placed at (h*32, v*32),
        // h/v stepping 0,2,4..14 (8x8). Top-left pivot, world = (screenX, -screenY).
        private void RenderGroundLayer(GroundLayerData ground, int col, int row,
            float regionScreenX, float regionScreenY, ref Bounds fullBounds, ref bool fullInit)
        {
            var groundRoot = new GameObject($"Ground_{col}_{row}").transform;
            groundRoot.SetParent(_mapRoot, false);

            foreach (var tile in ground.tiles)
            {
                float sx = regionScreenX + tile.h * GroundCell;
                float sy = regionScreenY + tile.v * GroundCell;
                float worldX = sx;
                float worldY = -sy;

                var sprite = GetGroundSprite(tile.spriteName, tile.frame);
                if (sprite == null) continue;

                var go = new GameObject($"Tile_{tile.h}_{tile.v}");
                go.transform.SetParent(groundRoot, false);
                go.transform.position = new Vector3(worldX, worldY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = GroundSortingOrder; // ground always beneath objects

                var tb = new Bounds(new Vector3(worldX + 32f, worldY - 32f, 0f), new Vector3(64f, 64f, 1f));
                if (!fullInit) { fullBounds = tb; fullInit = true; }
                else fullBounds.Encapsulate(tb);
            }
        }

        // Ground cover objects use absolute scene coords: screen = (posX, posY/2).
        private void RenderGroundCover(GroundLayerData ground)
        {
            if (ground.objects.Count == 0) return;
            var coverRoot = new GameObject("Cover").transform;
            coverRoot.SetParent(_mapRoot, false);

            foreach (var obj in ground.objects)
            {
                float screenX = obj.positionX;
                float screenY = obj.positionY * 0.5f;
                var sprite = GetObjectSprite(obj.imageName, obj.frame);
                if (sprite == null) continue;

                var go = new GameObject($"Cover_{obj.imageName}");
                go.transform.SetParent(coverRoot, false);
                go.transform.position = new Vector3(screenX, -screenY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                // Flat ground decals sit below structures (BuiltinSortingOrder); within
                // this layer the camera's CustomAxis world-Y sort orders them by feet.
                sr.sortingOrder = CoverSortingOrder;
            }
        }

        // Builtin objects use the JX isometric projection:
        //   screenX = sceneX,  screenY = sceneY/2 - sceneZ*(887/1024)
        // (from KRepresentShell3::CoordinateTransform). Ignoring Z caused gate beams
        // and tall structures to render at wrong heights, leaving dark gaps.
        private const float ZScreenScale = 887f / 1024f; // ≈0.866
        private int _builtinSortCounter;

        private void RenderBuiltinObjects(BuildinObjData builtin)
        {
            if (builtin.objects.Count == 0) return;
            var builtinRoot = new GameObject("Builtin").transform;
            builtinRoot.SetParent(_mapRoot, false);

            foreach (var obj in builtin.objects)
            {
                float screenX = obj.imgX1;
                float screenY = obj.imgY1 * 0.5f - obj.imgZ1 * ZScreenScale;
                var sprite = GetBuiltinSprite(obj.imageName, obj.frame);
                if (sprite == null) continue;

                var go = new GameObject($"Builtin_{obj.imageName}");
                go.transform.SetParent(builtinRoot, false);
                go.transform.position = new Vector3(screenX, -screenY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = BuiltinSortingOrder + (_builtinSortCounter++);
            }
        }

        // --- Sprite helpers (1px = 1 world unit) ---

        private readonly Dictionary<string, Sprite> _spriteCache = new();
        private readonly List<Texture2D> _proceduralTex = new();

        // Ground tiles: top-left pivot, fall back to a flat terrain-colored tile.
        private Sprite GetGroundSprite(string name, int frame)
        {
            string key = $"g|{name}|{frame}";
            if (_spriteCache.TryGetValue(key, out var cached)) return cached;

            var tex = _sprService.ResolveTexture(name, frame);
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect)
                : CreateFlatTile(TerrainColor(name));
            _spriteCache[key] = sprite;
            return sprite;
        }

        // Cover/builtin objects: bottom-center pivot, skip when art is missing.
        private Sprite GetObjectSprite(string name, int frame)
        {
            string key = $"o|{name}|{frame}";
            if (_spriteCache.TryGetValue(key, out var cached)) return cached;

            var tex = _sprService.ResolveTexture(name, frame);
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0f), 1f, 0, SpriteMeshType.FullRect)
                : null;
            _spriteCache[key] = sprite;
            return sprite;
        }

        // Builtin objects use top-left pivot: ImgPos1 is the quad top-left anchor,
        // the sprite extends right and downward to match ImgPos3.
        private Sprite GetBuiltinSprite(string name, int frame)
        {
            string key = $"b|{name}|{frame}";
            if (_spriteCache.TryGetValue(key, out var cached)) return cached;

            var tex = _sprService.ResolveTexture(name, frame);
            Sprite sprite = tex != null
                ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0f, 1f), 1f, 0, SpriteMeshType.FullRect)
                : null;
            _spriteCache[key] = sprite;
            return sprite;
        }

        private static Color TerrainColor(string name)
        {
            string n = name ?? "";
            if (n.Contains("水") || n.Contains("波") || n.Contains("沼")) return new Color(0.30f, 0.45f, 0.70f);
            if (n.Contains("草") || n.Contains("绿")) return new Color(0.42f, 0.55f, 0.30f);
            if (n.Contains("砂") || n.Contains("土") || n.Contains("红") || n.Contains("路") || n.Contains("卵"))
                return new Color(0.74f, 0.66f, 0.48f);
            return new Color(0.60f, 0.58f, 0.50f);
        }

        private Sprite CreateFlatTile(Color color)
        {
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, color);
            tex.Apply();
            _proceduralTex.Add(tex);
            // 64x64 screen footprint at 1px=1unit -> ppu = 1/64.
            return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0f, 1f), 1f / 64f, 0, SpriteMeshType.FullRect);
        }
    }
}
