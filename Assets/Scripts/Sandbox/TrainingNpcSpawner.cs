// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Spawns 5 training NPCs (2 bao cát, 2 cọc gỗ, 1 mộc nhân) in a pentagon formation
    /// around a center point with maximum HP.
    /// PC source: template IDs 415 (Cọc gỗ), 416 (Mộc nhân), 417 (Bao cát)
    /// from NpcS.txt and Region_S.dat.
    /// </summary>
    public sealed class TrainingNpcSpawner : MonoBehaviour
    {
        [Header("Center position (world coordinates)")]
        public float centerX = 53246f;
        public float centerY = -52041f;

        [Header("Pentagon radius in world units")]
        public float radius = 300f;

        [Header("Override center with player position on spawn")]
        public bool usePlayerPosition = false;

        private const int TEMPLATE_COC_GOC = 415;
        private const int TEMPLATE_MOC_NHAN = 416;
        private const int TEMPLATE_BAO_CAT = 417;

        private const int MAX_HP = 9999;

        private Transform _npcRoot;

        public void Spawn()
        {
            if (_npcRoot != null)
                Destroy(_npcRoot.gameObject);

            _npcRoot = new GameObject("TrainingNpcs").transform;
            _npcRoot.SetParent(transform, false);

            Vector2 center = usePlayerPosition ? GetPlayerCenter() : new Vector2(centerX, centerY);

            // Pentagon: 5 vertices, start from top (angle = 90°), evenly spaced 72° apart
            // Order: Bao cát, Cọc gỗ, Mộc nhân, Cọc gỗ, Bao cát
            int[] templateIds = { TEMPLATE_BAO_CAT, TEMPLATE_COC_GOC, TEMPLATE_MOC_NHAN, TEMPLATE_COC_GOC, TEMPLATE_BAO_CAT };
            string[] vietnameseNames = { "Bao cát", "Cọc gỗ", "Mộc nhân", "Cọc gỗ", "Bao cát" };
            Color[] colors = {
                new Color(0.50f, 0.45f, 0.35f, 1f), // Bao cát - sandbag
                new Color(0.55f, 0.40f, 0.25f, 1f), // Cọc gỗ - wood
                new Color(0.60f, 0.45f, 0.30f, 1f), // Mộc nhân - wood dummy
                new Color(0.55f, 0.40f, 0.25f, 1f), // Cọc gỗ - wood
                new Color(0.50f, 0.45f, 0.35f, 1f), // Bao cát - sandbag
            };

            for (int i = 0; i < 5; i++)
            {
                float angleDeg = 90f + i * 72f; // Start from top, clockwise
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float x = center.x + radius * Mathf.Cos(angleRad);
                float y = center.y + radius * Mathf.Sin(angleRad);

                SpawnSingleNpc(i, templateIds[i], vietnameseNames[i], colors[i], new Vector2(x, y));
            }

            SubsystemLog.Info("TrainingNpcSpawner",
                $"Spawned 5 training NPCs in pentagon at center={center}, radius={radius}");
        }

        private Vector2 GetPlayerCenter()
        {
            var mgr = SandboxManager.Instance;
            if (mgr != null && mgr.PlayerController != null)
                return mgr.PlayerController.transform.position;
            return new Vector2(centerX, centerY);
        }

        private void SpawnSingleNpc(int index, int templateId, string vietnameseName, Color bodyColor, Vector2 worldPos)
        {
            var go = new GameObject($"TrainingNPC_{index}_{vietnameseName}");
            go.transform.SetParent(_npcRoot, false);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            // Body visual — PC training objects use corpse SPRs: spr/obj/corpse/enemy178..180_corpse.spr
            string sprResType = templateId switch
            {
                TEMPLATE_COC_GOC => "enemy178",
                TEMPLATE_MOC_NHAN => "enemy179",
                TEMPLATE_BAO_CAT => "enemy180",
                _ => null
            };
            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(go.transform, false);
            var sr = bodyGo.AddComponent<SpriteRenderer>();
            sr.sprite = LoadTrainingSprite(templateId);
            sr.sortingOrder = MapRenderer.PlayerSortingOrder - 10;

            // Shadow below body
            var shadowGo = new GameObject("Shadow");
            shadowGo.transform.SetParent(go.transform, false);
            var shadowSr = shadowGo.AddComponent<SpriteRenderer>();
            shadowSr.sprite = MakeColoredSprite(96, new Color(0f, 0f, 0f, 0.3f));
            shadowSr.transform.localPosition = new Vector3(0f, -4f, 0.1f);
            shadowSr.sortingOrder = MapRenderer.PlayerSortingOrder - 20;

            // Nameplate with HP bar
            var plate = CreateNameplate(go.transform, vietnameseName, MAX_HP);
            var anchor = go.AddComponent<EnemyNameplateAnchor>();
            anchor.spriteRenderer = sr;
            anchor.nameplateRoot = plate != null ? plate.transform : null;
            anchor.worldOffset = 18f;
            anchor.Apply();

            // Enemy AI (static, no movement for training objects)
            var template = new NpcTemplate
            {
                templateId = templateId,
                nameRaw = vietnameseName,
                nameNormalized = vietnameseName,
                level = 1,
                maxLife = MAX_HP,
                kind = 0,
                series = 0,
                walkSpeed = 0,
                runSpeed = 0,
                visionRadius = 0,
                activeRadius = 0,
                aiMode = 0,
                aiParams = new int[0],
                spriteClipRef = "",
                spriteResolved = false,
            };
            var npcInstance = new NpcInstance
            {
                instanceId = index + 1000,
                template = template,
                worldPosition = worldPos,
                spawn = new NpcSpawn
                {
                    templateId = templateId,
                    posX = worldPos.x,
                    posY = worldPos.y,
                    regionX = 0,
                    regionY = 0,
                    scriptRef = "",
                }
            };
            var ai = go.AddComponent<BaLangEnemyAi>();
            ai.Initialize(npcInstance, plate);
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
            bar.barFill = CreateBar(root.transform, "HpBarFill", new Vector3(-27f, 0f, -0.1f), new Color(0.0f, 0.8f, 0.1f, 1f), 54f, 6f);
            bar.barFill.transform.localScale = Vector3.one;
            bar.barFill.transform.localPosition = new Vector3(-27f, 0f, -0.1f);
            bar.Initialize(displayName, maxLife);
            // Hide world renderers; screen-space overlay handles display
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
            tm.fontSize = fontSize;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = color;
            tm.text = text;
            return tm;
        }

        private static SpriteRenderer CreateBar(Transform parent, string name, Vector3 localPos, Color color, float width, float height)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = MakeColoredSprite(1, Color.white);
            sr.color = color;
            sr.sortingOrder = -5;
            go.transform.localScale = new Vector3(width, height, 1f);
            return sr;
        }

        private static readonly Dictionary<int, Sprite> SpriteCache = new Dictionary<int, Sprite>();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticCache()
        {
            SpriteCache.Clear();
        }

        private Sprite LoadRealTrainingSprite(string sprResType)
        {
            if (string.IsNullOrEmpty(sprResType))
                return MakeColoredSprite(128, new Color(0.5f, 0.45f, 0.35f, 1f));

            string sprPath = MapEnemyDatabase.BuildNpcSprPath(sprResType, "st");
            var svc = new VLTK.Sprites.SprRuntimeService();
            var sprite = svc.ResolveSprite(sprPath);
            if (sprite != null)
                return sprite;

            // Fallback to colored square
            return MakeColoredSprite(128, new Color(0.5f, 0.45f, 0.35f, 1f));
        }

        private static Sprite LoadTrainingSprite(int templateId)
        {
            if (SpriteCache.TryGetValue(templateId, out var cached))
                return cached;

            // Use PC corpse SPR for training objects (source: canonical Client 6.0/data/spr/spr/obj/corpse)
            string sprResType = templateId switch
            {
                TEMPLATE_COC_GOC => "enemy178",
                TEMPLATE_MOC_NHAN => "enemy179",
                TEMPLATE_BAO_CAT => "enemy180",
                _ => null
            };

            if (sprResType != null)
            {
                var svc = new VLTK.Sprites.SprRuntimeService();
                string sprPath = MapEnemyDatabase.BuildNpcSprPath(sprResType, "st");
                var sprite = svc.ResolveSprite(sprPath);
                if (sprite != null)
                {
                    SpriteCache[templateId] = sprite;
                    return sprite;
                }
            }

            // Fallback to colored square
            return MakeColoredSprite(128, new Color(0.5f, 0.45f, 0.35f, 1f));
        }
        private static Sprite MakeColoredSprite(int size, Color color)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }
    }
}
