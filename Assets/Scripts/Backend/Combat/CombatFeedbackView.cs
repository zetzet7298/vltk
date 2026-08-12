// -----------------------------------------------------------------------------
// VLTK.Backend.Combat — CombatFeedbackView
// MonoBehaviour subscribe CombatFeedbackBus.OnFeedback để render damage text
// (color: trắng=normal, vàng=crit, xám=miss, xanh=heal) với float-up + pop
// animation, despawn sau <see cref="textLifetimeSeconds"/>.
//
// Design:
//   - Subscribe trong OnEnable, unsubscribe trong OnDisable (an toàn cho
//     scene reload + GameObject.Destroy).
//   - World-space mode (canvasRoot=null): spawn TextMesh (3D) TẠI evt.Position
//     → số damage ĐỎ nhảy ngay tại mục tiêu như JX PC (KNpc::DoHurt client render).
//   - UI mode (canvasRoot!=null): spawn Text dưới Canvas (Screen Space Overlay).
//   - Mỗi feedback: pop-scale (0.6→1.2→1.0) + float-up + fade-out cuối 30%.
//
// Note: VLTK.Backend.asmdef có tham chiếu UnityEngine.UI (autoReferenced +
// UGUI package) — nên CombatFeedbackView dùng được Text/Canvas. KHÔNG cần
// thêm reference trong asmdef.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VLTK.Backend.Combat
{
    /// <summary>
    /// Render damage text feedback (normal/crit/miss/heal) bằng cách spawn
    /// Text/TextMesh + float-up + pop animation. Mỗi feedback sống
    /// <see cref="textLifetimeSeconds"/> rồi tự Destroy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackView : MonoBehaviour
    {
        // ----- Inspector -----

        [Tooltip("Canvas cha để spawn text dưới (Screen Space Overlay). " +
                 "Nếu null, sẽ spawn world-space TextMesh tại event.Position.")]
        public Canvas canvasRoot;

        [Tooltip("Prefab Text tùy chỉnh (optional). Nếu null sẽ tạo Text runtime " +
                 "với font mặc định Arial.")]
        public Text textPrefab;

        [Tooltip("Kích thước font mặc định (chỉ dùng khi textPrefab=null, UI mode).")]
        [Min(8)]
        public int defaultFontSize = 24;

        [Tooltip("Kích thước font world-space (TextMesh). PC JX dùng số to ~80 world unit.")]
        [Min(8)]
        public int worldFontSize = 80;

        [Tooltip("Kích thước ký tự world-space (world unit). PC JX ~1.4 world unit/digit.")]
        [Min(0.01f)]
        public float worldCharacterSize = 1.4f;

        [Tooltip("Thời gian sống của mỗi damage text (giây). PC JX ~0.9s.")]
        [Min(0.1f)]
        public float textLifetimeSeconds = 0.9f;

        [Tooltip("Khoảng cách float-up (pixel trong UI space, hoặc world unit).")]
        public float floatDistance = 60f;

        [Tooltip("Tốc độ float-up world-space (world unit/giây).")]
        public float worldFloatSpeed = 1.8f;

        [Tooltip("Offset Y cho world-space spawn (tránh che player).")]
        public float worldYOffset = 2.0f;

        [Tooltip("Random jitter X cho world-space spawn (tránh chồng số khi multi-hit).")]
        public float worldJitterX = 0.8f;

        [Tooltip("Màu cho mỗi loại feedback. PC JX damage red/yellow/gray convention.")]
        // PC JX palette (observable gameplay convention):
        //   Normal = đỏ cờ chói (#FF3D26-ish), Crit = vàng chói (#FFD900-ish),
        //   Miss = xám trung tính, Heal = xanh lá tươi.
        public Color normalColor = new Color(1f, 0.24f, 0.10f, 1f);   // đỏ damage (PC)
        public Color critColor = new Color(1f, 0.85f, 0.10f, 1f);      // vàng chí mạng (PC)
        public Color missColor = new Color(0.78f, 0.78f, 0.78f, 1f);  // xám trượt (PC)
        public Color healColor = new Color(0.30f, 1f, 0.40f, 1f);      // xanh hồi máu (PC)

        // ----- Runtime state -----

        private readonly List<ActiveFeedback> _active = new();
        private static readonly System.Random _rng = new();

        private struct ActiveFeedback
        {
            public GameObject Go;
            public Text Text;        // UI mode
            public TextMesh Mesh;    // world-space mode
            public Vector3 StartPos;
            public float StartTime;
            public float Lifetime;
            public bool WorldSpace;
        }

        // ----- Lifecycle -----

        private void OnEnable()
        {
            CombatFeedbackBus.OnFeedback += HandleFeedback;
        }

        private void OnDisable()
        {
            CombatFeedbackBus.OnFeedback -= HandleFeedback;
        }

        private void Update()
        {
            // Animate + cleanup active feedbacks
            float now = Time.time;
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                var f = _active[i];
                if (f.Go == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                float t = (now - f.StartTime) / Mathf.Max(0.001f, f.Lifetime);
                if (t >= 1f)
                {
                    Destroy(f.Go);
                    _active.RemoveAt(i);
                    continue;
                }

                // Alpha: full đến 0.6, fade trong 40% cuối.
                float alpha = t < 0.6f ? 1f : Mathf.Clamp01(1f - (t - 0.6f) / 0.4f);

                if (f.WorldSpace)
                {
                    // [DMG-100PC] Float-up + pop scale (PC JX damage number animation).
                    // Pop: 0→0.10 phồng to 1.3x, 0.10→1.0 thu về 1.0x. PC JX pop rất nhanh.
                    float pop = t < 0.10f
                        ? Mathf.Lerp(0.4f, 1.30f, t / 0.10f)
                        : Mathf.Lerp(1.30f, 1.0f, (t - 0.10f) / 0.90f);
                    float up = worldFloatSpeed * t * (1f + t * 0.4f);  // ease-out-ish float up
                    f.Go.transform.position = f.StartPos + Vector3.up * up;
                    f.Go.transform.localScale = Vector3.one * pop;

                    if (f.Mesh != null)
                    {
                        Color c = f.Mesh.color;
                        c.a = alpha;
                        f.Mesh.color = c;
                    }
                    // [DMG-OUTLINE] Alpha shadow outline cùng main text (children inherit scale/pos).
                    ApplyShadowAlpha(f.Go.transform, alpha);
                }
                else
                {
                    // UI mode float-up.
                    var rt = f.Go.transform as RectTransform;
                    if (rt != null)
                    {
                        rt.anchoredPosition = (Vector2)f.StartPos + Vector2.up * (floatDistance * t);
                    }
                    if (f.Text != null)
                    {
                        Color c = f.Text.color;
                        c.a = alpha;
                        f.Text.color = c;
                    }
                }
            }
        }

        // ----- Event handler -----

        private void HandleFeedback(CombatFeedbackEvent evt)
        {
            string label = FormatLabel(evt);
            Color color = ColorFor(evt.Kind);
            SpawnFeedback(label, color, evt.Position, evt.IsCritical);
        }

        private string FormatLabel(CombatFeedbackEvent evt)
        {
            switch (evt.Kind)
            {
                case CombatFeedbackKind.Miss: return "Trượt";
                case CombatFeedbackKind.Heal: return $"+{evt.Value}";
                case CombatFeedbackKind.Crit: return $"[{evt.Value}]";
                case CombatFeedbackKind.Normal: default: return evt.Value.ToString();
            }
        }

        private Color ColorFor(CombatFeedbackKind kind)
        {
            switch (kind)
            {
                case CombatFeedbackKind.Crit: return critColor;
                case CombatFeedbackKind.Miss: return missColor;
                case CombatFeedbackKind.Heal: return healColor;
                case CombatFeedbackKind.Normal: default: return normalColor;
            }
        }

        private void SpawnFeedback(string label, Color color, Vector3 worldPos, bool isCrit)
        {
            GameObject go;

            if (canvasRoot != null)
            {
                // UI mode: spawn under canvas
                Text text;
                if (textPrefab != null)
                {
                    go = Instantiate(textPrefab.gameObject, canvasRoot.transform);
                    text = go.GetComponent<Text>();
                }
                else
                {
                    go = new GameObject("CombatFeedbackText", typeof(RectTransform), typeof(Text));
                    go.transform.SetParent(canvasRoot.transform, false);
                    text = go.GetComponent<Text>();
                    text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                    text.fontSize = defaultFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                }
                if (text != null)
                {
                    text.text = label;
                    text.color = color;
                    text.fontStyle = isCrit ? FontStyle.Bold : FontStyle.Normal;
                }
                var rt = go.transform as RectTransform;
                if (rt != null)
                {
                    rt.anchoredPosition = new Vector2(
                        (float)(_rng.NextDouble() - 0.5) * 200f,
                        0f);
                }
                _active.Add(new ActiveFeedback
                {
                    Go = go,
                    Text = text,
                    Mesh = null,
                    StartPos = rt != null ? (Vector3)rt.anchoredPosition : Vector3.zero,
                    StartTime = Time.time,
                    Lifetime = textLifetimeSeconds,
                    WorldSpace = false,
                });
            }
            else
            {
                // [DMG-100PC] World-space TextMesh TẠI vị trí target (PC damage popup).
                // PC source: KNpc::DoHurt (KNpc.cpp:1427) trigger animation frames;
                // damage number rendering là CLIENT-SIDE, tính từ HP delta giữa sync update.
                // Client-side rendering code KHÔNG có trong source tree hiện có (chỉ server Core
                // được ship). Visual dưới đây theo JX-PC observable convention:
                //   - Bold font (NotoSans-Bold fallback LegacyRuntime)
                //   - Black outline (4 shadow copy offset)
                //   - Red cờ cho damage, vàng chói cho crit, xám cho miss
                //   - Pop scale 0.4→1.3→1.0 + float-up + fade last 30%
                go = new GameObject("DamageNumber", typeof(TextMesh), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                // Jitter X nhẹ để multi-hit không chồng số.
                float jitterX = (float)(_rng.NextDouble() - 0.5) * worldJitterX;
                go.transform.position = worldPos + Vector3.up * worldYOffset + Vector3.right * jitterX;
                var tm = go.GetComponent<TextMesh>();
                tm.text = label;
                tm.color = color;
                tm.fontSize = isCrit ? Mathf.RoundToInt(worldFontSize * 1.80f) : worldFontSize;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.characterSize = worldCharacterSize;
                tm.fontStyle = FontStyle.Bold;  // PC JX: bold only, no italic.
                // Font: ưu tiên NotoSans-Bold (sắc nét, đậm — PC look) → fallback LegacyRuntime.
                var font = LoadDamageFont();
                if (font != null)
                {
                    tm.font = font;
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = font.material;
                }
                // [DMG-OUTLINE] Black outline PC-style: 4 TextMesh shadow copy ở 4 hướng,
                // offset nhỏ theo characterSize. Render TRƯỚC main text (sortingOrder thấp hơn).
                SpawnOutlineShadows(go.transform, label, worldCharacterSize, isCrit);
                // Scale nhỏ ban đầu → Update pop phồng lên (cảm giác "nhảy số").
                go.transform.localScale = Vector3.one * 0.4f;
                _active.Add(new ActiveFeedback
                {
                    Go = go,
                    Text = null,
                    Mesh = tm,
                    StartPos = go.transform.position,
                    StartTime = Time.time,
                    Lifetime = textLifetimeSeconds,
                    WorldSpace = true,
                });
            }
        }

        // [DMG-OUTLINE] 4 TextMesh shadow copies làm outline đen cho damage number.
        // PC JX damage number có viền đen rõ ràng quanh chữ số (visible readability cue).
        private void SpawnOutlineShadows(Transform parent, string label, float charSize, bool isCrit)
        {
            float offset = charSize * 0.12f;  // ~0.17 world unit, đủ rõ mà không che main text.
            int baseFontSize = isCrit ? Mathf.RoundToInt(worldFontSize * 1.80f) : worldFontSize;
            int baseSorting = MapRendererSortingBelow(parent);

            for (int i = 0; i < 4; i++)
            {
                Vector3 localOffset = i switch
                {
                    0 => new Vector3(-offset, 0f, 0f),   // left
                    1 => new Vector3(offset, 0f, 0f),    // right
                    2 => new Vector3(0f, offset, 0f),    // up
                    _ => new Vector3(0f, -offset, 0f),   // down
                };
                var sh = new GameObject("DamageNumberShadow", typeof(TextMesh), typeof(MeshRenderer));
                sh.transform.SetParent(parent, false);
                sh.transform.localPosition = localOffset;
                sh.transform.localRotation = Quaternion.identity;
                sh.transform.localScale = Vector3.one;
                var shtm = sh.GetComponent<TextMesh>();
                shtm.text = label;
                shtm.color = new Color(0f, 0f, 0f, 0.85f);  // black 85% alpha
                shtm.fontSize = baseFontSize;
                shtm.anchor = TextAnchor.MiddleCenter;
                shtm.alignment = TextAlignment.Center;
                shtm.characterSize = worldCharacterSize;
                shtm.fontStyle = FontStyle.Bold;
                var shFont = LoadDamageFont();
                if (shFont != null)
                {
                    shtm.font = shFont;
                    var shMr = sh.GetComponent<MeshRenderer>();
                    if (shMr != null)
                    {
                        shMr.sharedMaterial = shFont.material;
                        shMr.sortingOrder = baseSorting;  // behind main text
                    }
                }
            }
        }

        private static Font _cachedDamageFont;
        private static Font LoadDamageFont()
        {
            if (_cachedDamageFont != null) return _cachedDamageFont;
            // PC JX look = bold + sharp. NotoSans-Bold có sẵn trong Assets/UI/Fonts.
            // Fallback LegacyRuntime nếu Resources path khác.
            _cachedDamageFont = Resources.Load<Font>("UI/Fonts/NotoSans-Bold");
            if (_cachedDamageFont == null)
                _cachedDamageFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return _cachedDamageFont;
        }

        private static int MapRendererSortingBelow(Transform mainTransform)
        {
            // Lấy sortingOrder của main TextMesh renderer, trừ 1 để outline render behind.
            var mr = mainTransform.GetComponent<MeshRenderer>();
            return mr != null ? mr.sortingOrder - 1 : 0;
        }

        // [DMG-OUTLINE] Fade alpha cho tất cả shadow TextMesh con (outline) cùng main text.
        private static void ApplyShadowAlpha(Transform parent, float alpha)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var sh = parent.GetChild(i).GetComponent<TextMesh>();
                if (sh != null)
                {
                    var c = sh.color;
                    c.a = alpha * 0.85f;  // base alpha 0.85 trong SpawnOutlineShadows.
                    sh.color = c;
                }
            }
        }

        // ----- Test helpers -----

        /// <summary>
        /// Số feedback đang active (test inspector — không nên gọi ngoài test).
        /// </summary>
        public int ActiveCount => _active.Count;
    }
}
