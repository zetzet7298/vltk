// -----------------------------------------------------------------------------
// VLTK Mobile — Generalized multi-map enemy spawn runtime.
// Replaces BaLangEnemySpawnRuntime for all maps.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Multi-map enemy spawn runtime. Loads PC Region_S data for any map,
    /// spawns enemies with proper templates, AI, nameplates.
    /// Does not fabricate NPC spawns when PC Region_S data is unavailable.
    /// </summary>
    public sealed class MapEnemySpawnRuntime : MonoBehaviour
    {
        public Transform enemyRoot;
        public Transform pcObjectRoot;
        public int liveEnemyCount;
        public int trainerMarkerCount;
        public int vietnameseNameCount;
        public int threeLayerUiCount;
        public int movingAiCount;

        private NpcTemplateRegistry _registry;
        private readonly List<BaLangNpcEntry> _entries = new();
        private int _currentMapId = -1;

        public IReadOnlyList<BaLangNpcEntry> Entries => _entries;
        public int CurrentMapId => _currentMapId;

        public void SpawnForMap(int mapId, string regionSFolder)
        {
            Clear();
            _currentMapId = mapId;

            _registry = new NpcTemplateRegistry();
            MapEnemyDatabase.RegisterAllForMap(mapId, _registry);

            if (VuotAiKillBossMatchSpawns.IsMissionMap(mapId))
            {
                int missionBosses = VuotAiKillBossMatchSpawns.AddMissionBossEntries(mapId, _registry, _entries);
                BuildSceneObjects();
                SubsystemLog.Info("MapEnemy",
                    $"Map {mapId}: PC killbossmatch ClearMapNpc/Obj/Trap active; spawned {missionBosses} mission bosses instead of static Region_S enemies");
                return;
            }

            var spawns = BaLangEnemyRegionScanner.ScanRegionS(regionSFolder);
            if (spawns.Count == 0)
            {
                SubsystemLog.Info("MapEnemy",
                    $"Map {mapId}: no PC Region_S entries to spawn; leaving enemy layer empty");
            }

            int id = 1;
            SpawnTrainerMarkers(spawns);

            foreach (var sp in spawns)
            {
                if (sp.kind != 0) continue;
                var template = _registry.Resolve(sp.templateId);
                if (template == null) continue;

                var worldPos = MapEnemyDatabase.MpsToWorld(sp.mpsX, sp.mpsY);
                _entries.Add(new BaLangNpcEntry
                {
                    template = template,
                    worldPosition = worldPos,
                    series = sp.series,
                    level = sp.level,
                    facing = sp.curFrame,
                    instanceId = id++,
                });
            }

            BuildSceneObjects();
            SubsystemLog.Info("MapEnemy",
                $"Map {mapId}: spawned {liveEnemyCount} enemies from {spawns.Count} PC Region_S entries");
        }

        /// <summary>
        /// Collect active enemies for combat auto-targeting.
        /// </summary>
        public List<EnemyRuntimeInfo> GetActiveEnemies()
        {
            var result = new List<EnemyRuntimeInfo>();
            if (enemyRoot == null) return result;

            foreach (var ai in enemyRoot.GetComponentsInChildren<BaLangEnemyAi>())
            {
                if (ai == null || ai.instance?.template == null) continue;
                result.Add(new EnemyRuntimeInfo
                {
                    enemyId = ai.instance.instanceId,
                    displayName = ai.instance.template?.DisplayName ?? "Kẻ địch",
                    position = (Vector2)ai.transform.position,
                    alive = ai.CurrentLife > 0,
                    currentLife = ai.CurrentLife,
                    maxLife = ai.MaxLife,
                    enemyBehaviour = ai,
                });
            }
            return result;
        }

        public void Clear()
        {
            if (enemyRoot != null)
                Destroy(enemyRoot.gameObject);
            if (pcObjectRoot != null)
                Destroy(pcObjectRoot.gameObject);
            enemyRoot = new GameObject("MapEnemies").transform;
            enemyRoot.SetParent(transform, false);
            pcObjectRoot = new GameObject("MapPcObjects").transform;
            pcObjectRoot.SetParent(transform, false);
            liveEnemyCount = vietnameseNameCount = threeLayerUiCount = movingAiCount = 0;
            trainerMarkerCount = 0;
            _entries.Clear();
            _registry = null;
        }

        private void SpawnTrainerMarkers(List<RegionSSpawnEntry> spawns)
        {
            if (pcObjectRoot == null) return;
            foreach (var sp in spawns)
            {
                if (!MapEnemyDatabase.IsTrainerSpawn(sp.templateId)) continue;
                var pos = MapEnemyDatabase.MpsToWorld(sp.mpsX, sp.mpsY);
                var vi = MapEnemyDatabase.VietnameseTrainerName(sp.templateId, sp.nameRaw);
                var go = new GameObject($"PC_{vi}_{sp.templateId}");
                go.transform.SetParent(pcObjectRoot, false);
                go.transform.position = new Vector3(pos.x, pos.y, 0f);
                var marker = go.AddComponent<BaLangPcSpawnMarker>();
                marker.templateId = sp.templateId;
                marker.rawName = sp.nameRaw;
                marker.vietnameseName = vi;
                marker.mpsX = sp.mpsX;
                marker.mpsY = sp.mpsY;
                marker.script = sp.script;
                
                // Render training objects (cọc gỗ/mộc nhân/bao cát) with corpse SPR
                var template = MapEnemyDatabase.GetTemplate(sp.templateId);
                if (template != null && (sp.templateId == 413 || sp.templateId == 414 || sp.templateId == 415))
                {
                    string sprPath = MapEnemyDatabase.BuildNpcSprPath(template.spriteClipRef, "st");
                    var visual = go.AddComponent<PcNpcVisual>();
                    visual.Configure(sprPath, sprPath);
                    marker.missingVisual = null;
                }
                else
                {
                    marker.missingVisual = "SPR not staged";
                }
                
                trainerMarkerCount++;
            }
        }

        private void BuildSceneObjects()
        {
            foreach (var entry in _entries)
            {
                var template = entry.template;
                var go = new GameObject($"Enemy_{entry.instanceId}_{template?.DisplayName ?? "unknown"}");
                go.transform.SetParent(enemyRoot, false);
                go.transform.position = new Vector3(entry.worldPosition.x, entry.worldPosition.y, 0f);

                var visual = go.AddComponent<PcNpcVisual>();
                string standPath = MapEnemyDatabase.BuildNpcSprPath(template.spriteClipRef, "st");
                string walkPath = MapEnemyDatabase.BuildNpcSprPath(template.spriteClipRef, "wlk");
                visual.Configure(standPath, walkPath, ReferencePixelForTemplate(template));

                string displayName = $"{MapEnemyDatabase.VietnameseSeriesName(entry.series)} {template?.DisplayName ?? "Kẻ địch"}";
                int maxLife = Mathf.Max(1, template?.maxLife ?? 50);

                var plate = CreateNameplate(go.transform, displayName, maxLife);
                var anchor = go.AddComponent<EnemyNameplateAnchor>();
                anchor.spriteRenderer = visual.GetComponentInChildren<SpriteRenderer>();
                anchor.nameplateRoot = plate != null ? plate.transform : null;
                anchor.worldOffset = 18f;
                anchor.Apply();

                var npcInstance = new NpcInstance
                {
                    instanceId = entry.instanceId,
                    template = template,
                    worldPosition = entry.worldPosition,
                    spawn = new NpcSpawn
                    {
                        templateId = template?.templateId ?? 0,
                        posX = 0, posY = 0, regionX = 0, regionY = 0,
                    }
                };
                var ai = go.AddComponent<BaLangEnemyAi>();
                ai.Initialize(npcInstance, plate);

                liveEnemyCount++;
                if (!ContainsChinese(displayName))
                    vietnameseNameCount++;
                if (plate != null && plate.HasThreeLayers)
                    threeLayerUiCount++;
                if (template != null && template.aiMode > 0 && template.walkSpeed > 0)
                    movingAiCount++;
            }
        }

        private static Vector2 ReferencePixelForTemplate(NpcTemplate template)
        {
            return template?.templateId switch
            {
                31 => new Vector2(160f, 192f),
                42 => new Vector2(187f, 191f),
                43 => new Vector2(160f, 192f),
                _ => new Vector2(160f, 192f),
            };
        }

        private static bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
                if (c >= 0x4e00 && c <= 0x9fff) return true;
            return false;
        }

        private static EnemyHealthBar CreateNameplate(Transform parent, string displayName, int maxLife)
        {
            var root = new GameObject("Nameplate");
            root.transform.SetParent(parent, false);
            root.transform.localPosition = new Vector3(0f, 88f, 0f);
            var bar = root.AddComponent<EnemyHealthBar>();

            bar.nameText = CreateText(root.transform, "Name", new Vector3(0f, 28f, 0f), 34, Color.white, displayName);
            bar.hpText = CreateText(root.transform, "HP", new Vector3(0f, 13f, 0f), 28, new Color(1f, 0.93f, 0.78f), "0/0");
            bar.barBack = CreateBar(root.transform, "HpBarBack", new Vector3(0f, 0f, 0f), new Color(0.18f, 0.03f, 0.03f, 0.92f), 54f, 6f);
            bar.barFill = CreateBar(root.transform, "HpBarFill", new Vector3(-27f, 0f, -0.1f), new Color(0.9f, 0.04f, 0.02f, 1f), 54f, 6f);
            bar.barFill.transform.localScale = Vector3.one;
            bar.barFill.transform.localPosition = new Vector3(-27f, 0f, -0.1f);
            bar.Initialize(displayName, maxLife);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>())
                renderer.enabled = false;
            return bar;
        }

        private static TextMesh CreateText(Transform parent, string name, Vector3 localPos, int fontSize, Color color, string text)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.fontSize = fontSize;
            tm.characterSize = 0.45f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            var mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = MapRenderer.PlayerSortingOrder + 3000;
            return tm;
        }

        private static SpriteRenderer CreateBar(Transform parent, string name, Vector3 localPos, Color color, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = new Vector3(width, height, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.color = color;
            sr.drawMode = SpriteDrawMode.Simple;
            sr.sortingOrder = MapRenderer.PlayerSortingOrder + 2990;
            return sr;
        }

        private static Sprite _whiteSprite;
        private static Texture2D _whiteTexture;
        private static Sprite WhiteSprite()
        {
            // Validity check protects against the static surviving a destroyed
            // underlying Texture2D across editor play sessions (Domain Reload
            // disabled) — Unity overrides == for UnityEngine.Object.
            if (_whiteSprite != null && _whiteTexture != null) return _whiteSprite;
            _whiteSprite = null;
            _whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _whiteTexture.SetPixel(0, 0, Color.white);
            _whiteTexture.Apply();
            _whiteSprite = Sprite.Create(_whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }

        /// <summary>
        /// Release the shared white sprite + texture. Safe to call multiple
        /// times. Call this when the runtime is torn down to avoid leaking the
        /// static Texture2D across editor play sessions.
        /// </summary>
        public static void ReleaseWhiteSprite()
        {
            if (_whiteSprite != null)
            {
                if (Application.isPlaying) Destroy(_whiteSprite); else DestroyImmediate(_whiteSprite);
                _whiteSprite = null;
            }
            if (_whiteTexture != null)
            {
                if (Application.isPlaying) Destroy(_whiteTexture); else DestroyImmediate(_whiteTexture);
                _whiteTexture = null;
            }
        }

        private void OnDestroy()
        {
            // Don't release the shared sprite here — other MapEnemySpawnRuntime
            // instances may still be alive in the same scene. Use the explicit
            // ReleaseWhiteSprite() entry point when fully tearing down.
        }
    }
}
