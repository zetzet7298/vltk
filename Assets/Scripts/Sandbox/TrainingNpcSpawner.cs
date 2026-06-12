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
    /// PC source: template IDs 413 (Cọc gỗ), 414 (Mộc nhân), 415 (Bao cát)
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

        private const int TEMPLATE_COC_GOC = 413;
        private const int TEMPLATE_MOC_NHAN = 414;
        private const int TEMPLATE_BAO_CAT = 415;

        private const int MAX_HP = 999999;

        private Transform _npcRoot;

        public void Spawn()
        {
            DestroyExistingChild(_npcRoot, "TrainingNpcs");

            _npcRoot = new GameObject("TrainingNpcs").transform;
            _npcRoot.SetParent(transform, false);

            Vector2 center = usePlayerPosition ? GetPlayerCenter() : new Vector2(centerX, centerY);

            // Pentagon: 5 vertices, start from top (angle = 90°), evenly spaced 72° apart
            // Order: Bao cát, Cọc gỗ, Mộc nhân, Cọc gỗ, Bao cát
            int[] templateIds = { TEMPLATE_BAO_CAT, TEMPLATE_COC_GOC, TEMPLATE_MOC_NHAN, TEMPLATE_COC_GOC, TEMPLATE_BAO_CAT };
            string[] vietnameseNames = { "Bao cát", "Cọc gỗ", "Mộc nhân", "Cọc gỗ", "Bao cát" };

            for (int i = 0; i < 5; i++)
            {
                float angleDeg = 90f + i * 72f; // Start from top, clockwise
                float angleRad = angleDeg * Mathf.Deg2Rad;
                float x = center.x + radius * Mathf.Cos(angleRad);
                float y = center.y + radius * Mathf.Sin(angleRad);

                SpawnSingleNpc(i, templateIds[i], vietnameseNames[i], new Vector2(x, y));
            }

            SubsystemLog.Info("TrainingNpcSpawner",
                $"Spawned 5 training NPCs in pentagon at center={center}, radius={radius}");
        }

        public List<EnemyRuntimeInfo> GetActiveEnemies()
        {
            var result = new List<EnemyRuntimeInfo>();
            if (_npcRoot == null) return result;

            foreach (var ai in _npcRoot.GetComponentsInChildren<BaLangEnemyAi>())
            {
                if (ai == null || ai.instance?.template == null) continue;
                result.Add(new EnemyRuntimeInfo
                {
                    enemyId = ai.instance.instanceId,
                    displayName = ai.instance.template.DisplayName,
                    position = (Vector2)ai.transform.position,
                    alive = ai.CurrentLife > 0,
                    currentLife = ai.CurrentLife,
                    maxLife = ai.MaxLife,
                    enemyBehaviour = ai,
                });
            }

            return result;
        }


        private void DestroyExistingChild(Transform cachedRoot, string childName)
        {
            var root = cachedRoot != null ? cachedRoot : transform.Find(childName);
            while (root != null)
            {
                var go = root.gameObject;
                if (Application.isPlaying)
                    Destroy(go);
                else
                    DestroyImmediate(go);
                root = transform.Find(childName);
            }
        }

        private Vector2 GetPlayerCenter()
        {
            var mgr = SandboxManager.Instance;
            if (mgr != null && mgr.PlayerController != null)
                return mgr.PlayerController.transform.position;
            return new Vector2(centerX, centerY);
        }

        private void SpawnSingleNpc(int index, int templateId, string vietnameseName, Vector2 worldPos)
        {
            var go = new GameObject($"TrainingNPC_{index}_{vietnameseName}");
            go.transform.SetParent(_npcRoot, false);
            go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

            string standPath = TrainingStandPath(templateId);
            var visual = go.AddComponent<PcNpcVisual>();
            visual.Configure(standPath, standPath, new Vector2(160f, 192f));
            var sr = go.transform.Find("NpcSprite")?.GetComponent<SpriteRenderer>();

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

        private static string TrainingStandPath(int templateId)
        {
            string resType = templateId switch
            {
                TEMPLATE_COC_GOC => "enemy178",
                TEMPLATE_MOC_NHAN => "enemy179",
                TEMPLATE_BAO_CAT => "enemy180",
                _ => null
            };
            return string.IsNullOrEmpty(resType) ? null : $@"spr\npcres\enemy\{resType}\{resType}_st.spr";
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
