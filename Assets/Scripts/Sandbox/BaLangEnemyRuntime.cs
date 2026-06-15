// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Sandbox
{
    public sealed class EnemyHealthBar : MonoBehaviour
    {
        public TextMesh nameText;
        public TextMesh hpText;
        public SpriteRenderer barBack;
        public SpriteRenderer barFill;

        public int CurrentLife { get; private set; }
        public int MaxLife { get; private set; }
        public string DisplayName { get; private set; }
        public bool HasThreeLayers => nameText != null && hpText != null && barBack != null && barFill != null;

        public void Initialize(string displayName, int maxLife)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Vô hệ Kẻ địch" : displayName;
            MaxLife = Mathf.Max(1, maxLife);
            SetLife(MaxLife);
            if (nameText != null) nameText.text = DisplayName;
        }

        public void SetLife(int currentLife)
        {
            CurrentLife = Mathf.Clamp(currentLife, 0, Mathf.Max(1, MaxLife));
            if (hpText != null)
                hpText.text = $"{CurrentLife}/{MaxLife}";
            if (barFill != null)
            {
                float ratio = MaxLife > 0 ? Mathf.Clamp01((float)CurrentLife / MaxLife) : 0f;
                var s = barFill.transform.localScale;
                barFill.transform.localScale = new Vector3(ratio, s.y, s.z);
            }
        }
    }

    public sealed class EnemyNameplateAnchor : MonoBehaviour
    {
        public SpriteRenderer spriteRenderer;
        public Transform nameplateRoot;
        public float worldOffset = 18f;

        public Vector3 ScreenAnchorWorldPosition
        {
            get
            {
                if (spriteRenderer != null && spriteRenderer.sprite != null)
                    return new Vector3(transform.position.x, spriteRenderer.bounds.max.y + worldOffset, transform.position.z);
                return transform.position + new Vector3(0f, 88f, 0f);
            }
        }

        private void LateUpdate()
        {
            Apply();
        }

        public void Apply()
        {
            if (nameplateRoot == null) return;
            var world = ScreenAnchorWorldPosition;
            nameplateRoot.position = new Vector3(world.x, world.y, -10f);
        }
    }

    public sealed class PcDamageNumber : MonoBehaviour
    {
        public const float DefaultLifetimeSeconds = 0.85f;

        public int Damage { get; private set; }

        private TextMesh _text;
        private Color _color;
        private Vector3 _startPosition;
        private float _age;

        public static PcDamageNumber Spawn(Vector3 worldPosition, int damage, Transform parent)
        {
            if (damage <= 0) return null;

            var go = new GameObject($"PcDamageNumber_{damage}");
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;

            var popup = go.AddComponent<PcDamageNumber>();
            popup.Initialize(damage);
            return popup;
        }

        private void Initialize(int damage)
        {
            Damage = damage;
            _startPosition = transform.position;
            _color = new Color(1f, 0.08f, 0.02f, 1f);

            _text = gameObject.AddComponent<TextMesh>();
            _text.text = damage.ToString();
            _text.fontSize = 48;
            _text.characterSize = 0.42f;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = _color;

            var mr = gameObject.GetComponent<MeshRenderer>();
            if (mr != null)
                mr.sortingOrder = MapRenderer.PlayerSortingOrder + 3600;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            _age += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(_age / DefaultLifetimeSeconds);
            transform.position = _startPosition + new Vector3(0f, 58f * t, 0f);

            if (_text != null)
            {
                var c = _color;
                c.a = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.35f) / 0.65f));
                _text.color = c;
            }

            if (_age >= DefaultLifetimeSeconds)
            {
                if (Application.isPlaying)
                    Destroy(gameObject);
                else
                    DestroyImmediate(gameObject);
            }
        }
    }

    public sealed class BaLangEnemyAi : MonoBehaviour
    {
        public NpcInstance instance;
        public Vector2 origin;
        public Vector2 target;
        public float arriveDistance = 12f;
        public int CurrentLife { get; private set; }
        public int MaxLife { get; private set; }
        public bool HasMoveTarget { get; private set; }
        public bool MovedSinceSpawn => Vector2.Distance(transform.position, origin) > 1f;

        private float _nextThink;
        private float _phase;
        private EnemyHealthBar _healthBar;
        private PcNpcVisual _visual;

        public void Initialize(NpcInstance npc, EnemyHealthBar healthBar)
        {
            instance = npc;
            _healthBar = healthBar;
            _visual = GetComponent<PcNpcVisual>();
            origin = transform.position;
            target = origin;
            MaxLife = Mathf.Max(1, npc?.template?.maxLife ?? 50);
            CurrentLife = MaxLife;
            _phase = (npc?.instanceId ?? 1) * 0.73f;
            _nextThink = 0.1f + ((npc?.instanceId ?? 1) % 7) * 0.15f;
            _healthBar?.SetLife(CurrentLife);
        }

        // [SECT-ALL] Bug fix (companion to SetLife death hide): respawn timer.
        // Khi CurrentLife=0, _deathTimestamp được set; sau 5s respawn → show lại + reset HP.
        private float _deathTimestamp = -1f;
        public float deathRespawnDelay = 5f;

        public void SetLife(int currentLife)
        {
            SetLife(currentLife, false);
        }

        public void SetLife(int currentLife, bool showDamage)
        {
            int previousLife = CurrentLife;
            CurrentLife = Mathf.Clamp(currentLife, 0, Mathf.Max(1, MaxLife));
            _healthBar?.SetLife(CurrentLife);
            if (showDamage && CurrentLife < previousLife)
                PcDamageNumber.Spawn(DamagePopupPosition(), previousLife - CurrentLife, transform.parent);

            // [SECT-ALL] Bug fix (user report 2026-06-15): NPC die nhưng sprite vẫn hiển thị.
            // Root cause thật: SetLife chỉ update CurrentLife + health bar, KHÔNG hide visual.
            // Khi player cast skill 357 / chém NPC, damage đi qua CombatRuntimeService → BaLangEnemyAi.SetLife()
            // → CurrentLife=0 nhưng sprite (NpcSprite/NpcShadow/Nameplate) vẫn active. User thấy xác chết đứng.
            // Fix: ẩn children (sprite/shadow/nameplate/health bar) nhưng GIỮ parent activeSelf=true.
            // QUAN TRỌNG: KHÔNG SetActive(false) parent — nếu không, MonoBehaviour.Update() không chạy
            // trên inactive GameObject → respawn timer (Tick) không bao giờ trigger.
            // PC JX1: corpse tồn tại vài giây rồi despawn (handled bằng corpseIdx sprite).
            // Mobile MVP: hide children thẳng (Phase 5 follow-up sẽ thay bằng death anim sprite).
            if (CurrentLife <= 0 && previousLife > 0)
            {
                _deathTimestamp = Time.time;
                foreach (Transform child in transform)
                    if (child.gameObject.activeSelf)
                        child.gameObject.SetActive(false);
            }
        }

        private Vector3 DamagePopupPosition()
        {
            var sr = _visual != null ? _visual.GetComponentInChildren<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return new Vector3(transform.position.x, sr.bounds.max.y + 10f, -12f);
            return transform.position + new Vector3(0f, 78f, -12f);
        }

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float now)
        {
            // [SECT-ALL] Respawn check (companion to death hide in SetLife).
            // PHẢI đặt TRƯỚC early return template check, nếu không respawn không bao giờ
            // trigger cho NPC có template.aiMode=0 (trainer) hoặc walkSpeed=0 (statue/object).
            if (_deathTimestamp > 0f && now - _deathTimestamp >= deathRespawnDelay)
            {
                _deathTimestamp = -1f;
                CurrentLife = MaxLife;
                if (!gameObject.activeSelf) gameObject.SetActive(true);
                foreach (Transform child in transform)
                    if (!child.gameObject.activeSelf) child.gameObject.SetActive(true);
                _healthBar?.SetLife(CurrentLife);
                return; // skip this frame after respawn to avoid double-tick
            }
            // Skip AI tick khi đang chết
            if (_deathTimestamp > 0f) return;

            var template = instance?.template;
            if (template == null || template.aiMode <= 0 || template.walkSpeed <= 0)
                return;
            if (_deathTimestamp > 0f)
            {
                if (Time.frameCount % 60 == 0) UnityEngine.Debug.Log($"[DEAD-TICK] {name} ts={_deathTimestamp:F1} now={now:F1} diff={now - _deathTimestamp:F1}");
                return;
            }

            if (!HasMoveTarget && now >= _nextThink)
                ChooseNextTarget(now);

            if (!HasMoveTarget)
                return;

            var pos = (Vector2)transform.position;
            var toTarget = target - pos;
            if (toTarget.magnitude <= arriveDistance)
            {
                HasMoveTarget = false;
                return;
            }

            float speed = Mathf.Max(8f, template.walkSpeed * 18f);
            var moveDir = toTarget.normalized;
            _visual?.SetMoveInput(moveDir);
            var next = pos + moveDir * speed * Mathf.Max(0f, deltaTime);
            transform.position = new Vector3(next.x, next.y, transform.position.z);
        }

        private void ChooseNextTarget(float now)
        {
            var template = instance?.template;
            var p = template?.aiParams;
            if (template == null || p == null || p.Length < 4)
                return;

            int baseDistance = Mathf.Max(1, p[0]);
            int distanceVariance = Mathf.Max(0, p[1]);
            int angleBase = p[2];
            int angleVariance = Mathf.Max(0, p[3]);

            float waveA = Mathf.Abs(Mathf.Sin(_phase + now * 1.37f));
            float waveB = Mathf.Sin(_phase * 2.1f + now * 0.73f);
            float distance = Mathf.Max(12f, baseDistance - distanceVariance * waveA);

            float offsetUnits = angleVariance > 0 ? Mathf.Lerp(angleVariance, angleBase, waveA) : angleBase;
            if (waveB < 0f) offsetUnits = -offsetUnits;
            float angleRad = offsetUnits / 64f * Mathf.PI * 2f;
            var dir = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            if (dir.sqrMagnitude < 0.001f) dir = Vector2.right;

            var desired = (Vector2)transform.position + dir.normalized * distance;
            float radius = template.activeRadius > 0 ? template.activeRadius : baseDistance;
            var fromOrigin = desired - origin;
            if (fromOrigin.magnitude > radius)
                desired = origin + fromOrigin.normalized * radius;

            target = desired;
            HasMoveTarget = true;
            float durationTicks = template.walkSpeed > 0 ? distance / template.walkSpeed : 5f;
            _nextThink = now + Mathf.Clamp(durationTicks * 0.15f, 0.8f, 6f);
        }
    }

    /// <summary>
    /// Lightweight NPC instance for Region_S spawns.
    /// Holds template + world position + series (ngũ hành element per spawn, not per template).
    /// </summary>
    public sealed class BaLangNpcEntry
    {
        public NpcTemplate template;
        public Vector2 worldPosition;
        public int series;   // per-spawn ngũ hành override
        public int level;    // per-spawn level from Region_S
        public int facing;   // curFrame from Region_S
        public int instanceId;
        /// <summary>Live scene AI component — set after spawn for runtime position sync.</summary>
        public BaLangEnemyAi enemyBehaviour;
    }

    public sealed class BaLangEnemySpawnRuntime : MonoBehaviour
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

        public IReadOnlyList<BaLangNpcEntry> Entries => _entries;

        public void SpawnFromRegionS(string regionSFolder)
        {
            Clear();

            _registry = new NpcTemplateRegistry();
            BaLangEnemyDatabase.RegisterAll(_registry);

            var spawns = BaLangEnemyRegionScanner.ScanRegionS(regionSFolder);
            int id = 1;
            SpawnTrainerMarkers(spawns);

            foreach (var sp in spawns)
            {
                // Only spawn supported outside-town enemy animals with staged PC walk SPRs.
                // Skip town NPCs/training objects until their exact PC visuals are staged.
                if (sp.kind != 0) continue;

                var template = _registry.Resolve(sp.templateId);
                if (template == null) continue;

                var worldPos = BaLangEnemyDatabase.MpsToWorld(sp.mpsX, sp.mpsY);
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
            SubsystemLog.Info("BaLangEnemy",
                $"Spawned {liveEnemyCount} enemies from {spawns.Count} Region_S entries (kind=0 only)");
        }

        /// <summary>
        /// Collect active enemies for combat auto-targeting.
        /// Returns enemy runtime info including position, HP, alive status.
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
            enemyRoot = new GameObject("BaLangEnemies").transform;
            enemyRoot.SetParent(transform, false);
            pcObjectRoot = new GameObject("BaLangPcObjects").transform;
            pcObjectRoot.SetParent(transform, false);
            liveEnemyCount = vietnameseNameCount = threeLayerUiCount = movingAiCount = 0;
            trainerMarkerCount = 0;
            _entries.Clear();
            _registry = null;
            // Drop static sprite cache so a re-Build sau domain reload/Play stop tạo texture mới
            // thay vì trỏ vào native object đã destroy.
            if (_whiteSprite != null)
            {
                var cachedTex = _whiteSprite.texture;
                _whiteSprite = null;
                if (cachedTex != null) Destroy(cachedTex);
            }
        }

        private void SpawnTrainerMarkers(List<RegionSSpawnEntry> spawns)
        {
            if (pcObjectRoot == null) return;
            foreach (var sp in spawns)
            {
                if (!BaLangEnemyDatabase.IsTrainerSpawn(sp.templateId)) continue;
                var pos = BaLangEnemyDatabase.MpsToWorld(sp.mpsX, sp.mpsY);
                var vi = BaLangEnemyDatabase.VietnameseTrainerName(sp.templateId, sp.nameRaw);
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
                marker.missingVisual = sp.templateId == 311 ? "passerby097 SPR not staged" : "enemy178/179/180 SPR not staged";
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
                string walkPath = BaLangEnemyDatabase.BuildNpcSprPath(template.spriteClipRef, "wlk");
                visual.Configure(walkPath, walkPath, ReferencePixelForTemplate(template));

                // Use per-spawn series for the display name
                string displayName = $"{BaLangEnemyDatabase.VietnameseSeriesName(entry.series)} {template?.DisplayName ?? "Kẻ địch"}";
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
                        posX = 0,
                        posY = 0,
                        regionX = 0,
                        regionY = 0,
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

        public static EnemyHealthBar CreateNameplate(Transform parent, string displayName, int maxLife)
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
            // PC-style readable layer is the screen-space overlay. Keep this data model for HP state,
            // but hide world TextMesh/SpriteRenderer to avoid duplicate/cluttered labels.
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

        private static Vector2 ReferencePixelForTemplate(NpcTemplate template)
        {
            return template?.templateId switch
            {
                31 => new Vector2(160f, 192f), // ani049
                42 => new Vector2(187f, 191f), // ani061
                43 => new Vector2(160f, 192f), // ani063
                _ => new Vector2(160f, 192f),
            };
        }

        private static Sprite CreateBodySprite(NpcTemplate template)
        {
            int size = 24;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var color = ColorForTemplate(template);
            var outline = new Color(0f, 0f, 0f, 0.65f);
            float radius = (size - 1) * 0.45f;
            var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, d <= radius ? (d > radius - 2f ? outline : color) : Color.clear);
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        }

        private static Sprite _whiteSprite;
        private static Sprite WhiteSprite()
        {
            // Unity có thể destroy underlying texture sau domain reload nhưng managed
            // reference vẫn non-null; check cả sprite lẫn texture để tránh
            // MissingReferenceException giữa các play session.
            if (_whiteSprite != null && _whiteSprite.texture != null) return _whiteSprite;
            _whiteSprite = null;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return _whiteSprite;
        }

        private static Color ColorForTemplate(NpcTemplate template)
        {
            int id = template?.templateId ?? 0;
            return id switch
            {
                31 => new Color(0.95f, 0.72f, 0.15f, 1f),  // 金猫 - golden
                42 => new Color(0.65f, 0.45f, 0.25f, 1f),  // 梅花鹿 - brown deer
                43 => new Color(0.95f, 0.92f, 0.90f, 1f),  // 白猪 - white pig
                413 => new Color(0.55f, 0.40f, 0.25f, 1f), // 木桩 - wood
                414 => new Color(0.60f, 0.45f, 0.30f, 1f), // 木人 - wood dummy
                415 => new Color(0.50f, 0.45f, 0.35f, 1f), // 沙袋 - sandbag
                _ => Color.gray,
            };
        }

        private static bool ContainsChinese(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;
            foreach (char c in text)
                if (c >= 0x4e00 && c <= 0x9fff) return true;
            return false;
        }
    }
}
