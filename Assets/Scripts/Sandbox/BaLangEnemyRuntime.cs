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

        private int previousLife;

        public void Initialize(string displayName, int maxLife)
        {
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? "Vô hệ Kẻ địch" : displayName;
            MaxLife = Mathf.Max(1, maxLife);
            SetLife(MaxLife, showDamage: false);
            previousLife = MaxLife;
            if (nameText != null) nameText.text = DisplayName;
        }

        public void SetLife(int currentLife, bool showDamage = false, bool isCrit = false)
        {
            previousLife = CurrentLife;
            CurrentLife = Mathf.Clamp(currentLife, 0, Mathf.Max(1, MaxLife));
            if (hpText != null)
                hpText.text = $"{CurrentLife}/{MaxLife}";
            if (barFill != null)
            {
                float ratio = MaxLife > 0 ? Mathf.Clamp01((float)CurrentLife / MaxLife) : 0f;
                Vector3 scale = barFill.transform.localScale;
                barFill.transform.localScale = new Vector3(ratio, scale.y, scale.z);
            }
            if (showDamage && CurrentLife < previousLife)
            {
                // Spawn damage number at target position with crit visual
                var sr = GetComponentInChildren<SpriteRenderer>();
                Vector3 spawnPos = sr != null && sr.sprite != null
                    ? new Vector3(transform.position.x, sr.bounds.max.y + 10f, -12f)
                    : transform.position + new Vector3(0f, 78f, -12f);
                PcDamageNumber.Spawn(spawnPos, previousLife - CurrentLife, transform.parent, isCrit);
            }
        }

        public bool LastDamageWasCrit { get; private set; }
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

        public static PcDamageNumber Spawn(Vector3 worldPosition, int damage, Transform parent, bool isCrit = false)
        {
            if (damage <= 0) return null;

            var go = new GameObject($"PcDamageNumber_{damage}");
            if (parent != null)
                go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;

            var popup = go.AddComponent<PcDamageNumber>();
            popup.Initialize(damage, isCrit);
            return popup;
        }

        private void Initialize(int damage, bool isCrit)
        {
            Damage = damage;
            _startPosition = transform.position;
            // [DMG-100PC] PC JX damage number palette (observable):
            //   Normal = đỏ cờ chói (1.0, 0.24, 0.10), Crit = vàng chói (1.0, 0.85, 0.10).
            //   Font: NotoSans-Bold (sắc nét, đậm — fallback LegacyRuntime).
            //   Outline: 4 TextMesh shadow copy offset nhỏ.
            _color = isCrit ? new Color(1f, 0.85f, 0.10f, 1f) : new Color(1f, 0.24f, 0.10f, 1f);
            int fontSize = isCrit ? 100 : 80;
            float charSize = isCrit ? 0.6f : 0.5f;

            _text = gameObject.AddComponent<TextMesh>();
            _text.text = damage.ToString();
            _text.fontSize = fontSize;
            _text.characterSize = charSize;
            _text.anchor = TextAnchor.MiddleCenter;
            _text.alignment = TextAlignment.Center;
            _text.color = _color;
            _text.fontStyle = FontStyle.Bold;

            var font = LoadDamageFont();
            if (font != null)
            {
                _text.font = font;
                var mr = gameObject.GetComponent<MeshRenderer>();
                if (mr != null) mr.sharedMaterial = font.material;
            }

            var mrComp = gameObject.GetComponent<MeshRenderer>();
            if (mrComp != null)
                mrComp.sortingOrder = MapRenderer.PlayerSortingOrder + 3600;

            // [DMG-OUTLINE] 4 shadow copies cho outline đen PC-style.
            SpawnOutlineShadows(fontSize, charSize);
        }

        private void SpawnOutlineShadows(int fontSize, float charSize)
        {
            float offset = charSize * 0.12f;
            for (int i = 0; i < 4; i++)
            {
                Vector3 localOffset = i switch
                {
                    0 => new Vector3(-offset, 0f, 0f),
                    1 => new Vector3(offset, 0f, 0f),
                    2 => new Vector3(0f, offset, 0f),
                    _ => new Vector3(0f, -offset, 0f),
                };
                var sh = new GameObject("PcDamageNumberShadow", typeof(TextMesh), typeof(MeshRenderer));
                sh.transform.SetParent(transform, false);
                sh.transform.localPosition = localOffset;
                sh.transform.localRotation = Quaternion.identity;
                sh.transform.localScale = Vector3.one;
                var shtm = sh.AddComponent<TextMesh>();
                shtm.text = Damage.ToString();
                shtm.color = new Color(0f, 0f, 0f, 0.85f);
                shtm.fontSize = fontSize;
                shtm.characterSize = charSize;
                shtm.anchor = TextAnchor.MiddleCenter;
                shtm.alignment = TextAlignment.Center;
                shtm.fontStyle = FontStyle.Bold;
                var font = LoadDamageFont();
                if (font != null)
                {
                    shtm.font = font;
                    var mr = sh.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        mr.sharedMaterial = font.material;
                        mr.sortingOrder = MapRenderer.PlayerSortingOrder + 3599;  // behind main
                    }
                }
            }
        }

        private static Font _cachedDamageFont;
        private static Font LoadDamageFont()
        {
            if (_cachedDamageFont != null) return _cachedDamageFont;
            _cachedDamageFont = Resources.Load<Font>("UI/Fonts/NotoSans-Bold");
            if (_cachedDamageFont == null)
                _cachedDamageFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cachedDamageFont;
        }

        private void Update()
        {
            Tick(Time.deltaTime);
        }

        public void Tick(float deltaTime)
        {
            _age += Mathf.Max(0f, deltaTime);
            float t = Mathf.Clamp01(_age / DefaultLifetimeSeconds);
            // [DMG-100PC] Float-up PC JX: 58 world unit/giây, ease-out (^1.3) → dốc lên nhanh đầu.
            float up = 58f * t * (1f + t * 0.3f);
            transform.position = _startPosition + new Vector3(0f, up, 0f);

            float alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01((t - 0.35f) / 0.65f));
            if (_text != null)
            {
                var c = _color;
                c.a = alpha;
                _text.color = c;
            }
            // [DMG-OUTLINE] Fade alpha outline cùng main text.
            for (int i = 0; i < transform.childCount; i++)
            {
                var sh = transform.GetChild(i).GetComponent<TextMesh>();
                if (sh != null)
                {
                    var c = sh.color;
                    c.a = alpha * 0.85f;
                    sh.color = c;
                }
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
            _healthBar?.SetLife(CurrentLife, showDamage: false, isCrit: false);
        }

        public void SetLife(int currentLife)
        {
            SetLife(currentLife, false, false);
        }

        public void SetLife(int currentLife, bool showDamage)
        {
            SetLife(currentLife, showDamage, false);
        }

        public void SetLife(int currentLife, bool showDamage, bool isCrit)
        {
            int previousLife = CurrentLife;
            CurrentLife = Mathf.Clamp(currentLife, 0, Mathf.Max(1, MaxLife));
            _healthBar?.SetLife(CurrentLife, showDamage, isCrit);

            // [SECT-ALL] Death state machine (PC source: KNpc::DoDeath @ 0x0809def0).
            // PC behavior khi chết:
            //   1. Set m_214 = 0xa (DEATH_STATE) — line 0x0809df84
            //   2. Call KNpc::ClearProcessAI() (0x08090cf0) — stop AI loop
            //   3. Swap sprite sang CorpseIdx (no SPR data in available source — TODO)
            //   4. Play m_DeathFrame animation rồi despawn sau N frame
            //   5. Sau delay (PC source không có explicit delay — TODO), Revive() tại vị trí gốc
            // Mobile port MVP (chỉ những gì có PC source):
            //   ✓ Set IsDead = true
            //   ✓ nextAttackTime = infinity (tương đương ClearProcessAI: AI tick bỏ qua)
            //   ✗ Corpse sprite swap — TODO, CorpseIdx field có nhưng SPR mapping không accessible
            //   ✗ Despawn timing — TODO, m_DeathFrame count không accessible
            //   ✗ Respawn delay + position — TODO, KNpc::Revive flow tồn tại nhưng delay constant không có
            if (CurrentLife <= 0 && previousLife > 0)
            {
                _isDead = true;
                // ClearProcessAI equivalent: Tick() sẽ skip AI khi _isDead = true.
                // (nextAttackTime ở GameplayActor layer, không access được từ visual layer)
            }
        }

        // [SECT-ALL] Death state flag (PC source: KNpc m_214 = 0xa / DEATH_STATE).
        // Public cho Combat layer / GameplayLoop đọc.
        private bool _isDead;
        public bool IsDead => _isDead;

        private void Update()
        {
            Tick(Time.deltaTime, Time.time);
        }

        public void Tick(float deltaTime, float now)
        {
            // [SECT-ALL] PC source: ClearProcessAI (0x08090cf0) semantics.
            // Khi KNpc die, AI loop bị stop. Mobile port tương đương: skip toàn bộ AI logic
            // khi _isDead = true. KHÔNG hide visual — PC swap sang CorpseIdx sprite (TODO,
            // CorpseIdx field có trong binary nhưng SPR mapping không accessible trong source tree).
            if (_isDead) return;

            var template = instance?.template;
            if (template == null || template.aiMode <= 0 || template.walkSpeed <= 0)
                return;

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
