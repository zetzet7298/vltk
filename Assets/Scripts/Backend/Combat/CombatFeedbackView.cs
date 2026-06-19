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

        [Tooltip("Kích thước font world-space (TextMesh). Camera ortho 240 → ~64.")]
        [Min(8)]
        public int worldFontSize = 64;

        [Tooltip("Kích thước ký tự world-space (world unit). Càng lớn càng to.")]
        [Min(0.01f)]
        public float worldCharacterSize = 16f;

        [Tooltip("Thời gian sống của mỗi damage text (giây). Sau đó auto-Destroy.")]
        [Min(0.1f)]
        public float textLifetimeSeconds = 1.0f;

        [Tooltip("Khoảng cách float-up (pixel trong UI space, hoặc world unit).")]
        public float floatDistance = 60f;

        [Tooltip("Tốc độ float-up world-space (world unit/giây).")]
        public float worldFloatSpeed = 1.6f;

        [Tooltip("Offset Y cho world-space spawn (tránh che player).")]
        public float worldYOffset = 1.5f;

        [Tooltip("Random jitter X cho world-space spawn (tránh chồng số khi multi-hit).")]
        public float worldJitterX = 0.6f;

        [Tooltip("Màu cho mỗi loại feedback. Có thể override trong Inspector.")]
        public Color normalColor = new Color(1f, 0.35f, 0.15f, 1f);   // cam-đỏ đậm (PC damage number)
        public Color critColor = new Color(1f, 0.85f, 0.1f, 1f);      // vàng chí mạng
        public Color missColor = new Color(0.85f, 0.85f, 0.85f, 1f);  // xám trượt
        public Color healColor = new Color(0.3f, 1f, 0.4f, 1f);       // xanh hồi máu

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
                    // Float-up + pop scale.
                    float up = worldFloatSpeed * t;
                    // Pop: 0→0.15 phồng lên (1.35x), 0.15→1 thu về 1.0x.
                    float pop = t < 0.15f
                        ? Mathf.Lerp(0.6f, 1.35f, t / 0.15f)
                        : Mathf.Lerp(1.35f, 1.0f, (t - 0.15f) / 0.85f);
                    f.Go.transform.position = f.StartPos + Vector3.up * up;
                    f.Go.transform.localScale = Vector3.one * pop;

                    if (f.Mesh != null)
                    {
                        Color c = f.Mesh.color;
                        c.a = alpha;
                        f.Mesh.color = c;
                    }
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
                // World-space mode: TextMesh TẠI vị trí target (PC damage popup).
                go = new GameObject("DamageNumber", typeof(TextMesh), typeof(MeshRenderer));
                go.transform.SetParent(transform, false);
                // Jitter X nhẹ để multi-hit không chồng số.
                float jitterX = (float)(_rng.NextDouble() - 0.5) * worldJitterX;
                go.transform.position = worldPos + Vector3.up * worldYOffset + Vector3.right * jitterX;
                var tm = go.GetComponent<TextMesh>();
                tm.text = label;
                tm.color = color;
                tm.fontSize = isCrit ? Mathf.RoundToInt(worldFontSize * 1.25f) : worldFontSize;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.characterSize = worldCharacterSize;
                tm.fontStyle = isCrit ? FontStyle.BoldAndItalic : FontStyle.Bold;
                // Đặt font + material chuẩn để TextMesh render được (Arial built-in).
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null)
                {
                    tm.font = font;
                    var mr = go.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = font.material;
                }
                // Scale nhỏ ban đầu → Update pop phồng lên (cảm giác "nhảy số").
                go.transform.localScale = Vector3.one * 0.6f;
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

        // ----- Test helpers -----

        /// <summary>
        /// Số feedback đang active (test inspector — không nên gọi ngoài test).
        /// </summary>
        public int ActiveCount => _active.Count;
    }
}
