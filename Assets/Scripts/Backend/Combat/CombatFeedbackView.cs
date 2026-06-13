// -----------------------------------------------------------------------------
// VLTK.Backend.Combat — CombatFeedbackView
// MonoBehaviour subscribe CombatFeedbackBus.OnFeedback để render damage text
// (color: white=normal, yellow=crit, red=miss, green=heal) với float-up
// animation, despawn sau <see cref="textLifetimeSeconds"/>.
//
// Design:
//   - Subscribe trong OnEnable, unsubscribe trong OnDisable (an toàn cho
//     scene reload + GameObject.Destroy).
//   - Spawn text bằng GameObject.CreatePrimitive(PrimitiveType.Quad) với
//     unlit material + TextMesh (built-in, không cần TextMeshPro). Hoặc dùng
//     prefab tùy Inspector (combatTextPrefab).
//   - Mỗi feedback instance là một GameObject con của <transform>, di
//     chuyển + mờ dần theo lifetime, rồi Destroy.
//   - Nếu canvasRoot được set thì spawn dưới Canvas (Screen Space - Overlay)
//     — phù hợp UI HUD overlay.
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
    /// Text component + float-up animation. Mỗi feedback sống
    /// <see cref="textLifetimeSeconds"/> rồi tự Destroy.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CombatFeedbackView : MonoBehaviour
    {
        // ----- Inspector -----

        [Tooltip("Canvas cha để spawn text dưới (Screen Space Overlay). " +
                 "Nếu null, sẽ spawn world-space quad tại event.Position.")]
        public Canvas canvasRoot;

        [Tooltip("Prefab Text tùy chỉnh (optional). Nếu null sẽ tạo Text runtime " +
                 "với font mặc định Arial.")]
        public Text textPrefab;

        [Tooltip("Kích thước font mặc định (chỉ dùng khi textPrefab=null).")]
        [Min(8)]
        public int defaultFontSize = 24;

        [Tooltip("Thời gian sống của mỗi damage text (giây). Sau đó auto-Destroy.")]
        [Min(0.1f)]
        public float textLifetimeSeconds = 1.0f;

        [Tooltip("Khoảng cách float-up (pixel trong UI space, hoặc world unit).")]
        public float floatDistance = 60f;

        [Tooltip("Offset Y cho world-space spawn (tránh che player).")]
        public float worldYOffset = 1.5f;

        [Tooltip("Màu cho mỗi loại feedback. Có thể override trong Inspector.")]
        public Color normalColor = Color.white;
        public Color critColor = Color.yellow;
        public Color missColor = Color.red;
        public Color healColor = Color.green;

        // ----- Runtime state -----

        private readonly List<ActiveFeedback> _active = new();
        private static readonly System.Random _rng = new();

        private struct ActiveFeedback
        {
            public GameObject Go;
            public Text Text;
            public Vector3 StartPos;
            public float StartTime;
            public float Lifetime;
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
                // Float-up
                if (canvasRoot != null)
                {
                    var rt = f.Go.transform as RectTransform;
                    if (rt != null)
                    {
                        rt.anchoredPosition = (Vector2)f.StartPos + Vector2.up * (floatDistance * t);
                    }
                }
                else
                {
                    f.Go.transform.position = f.StartPos + Vector3.up * (floatDistance * t * 0.05f);
                }
                // Fade out in last 30%
                if (f.Text != null)
                {
                    Color c = f.Text.color;
                    c.a = t < 0.7f ? 1f : (1f - (t - 0.7f) / 0.3f);
                    f.Text.color = c;
                }
            }
        }

        // ----- Event handler -----

        private void HandleFeedback(CombatFeedbackEvent evt)
        {
            string label = FormatLabel(evt);
            Color color = ColorFor(evt.Kind);
            SpawnFeedback(label, color, evt.Position);
        }

        private string FormatLabel(CombatFeedbackEvent evt)
        {
            switch (evt.Kind)
            {
                case CombatFeedbackKind.Miss: return "Miss";
                case CombatFeedbackKind.Heal: return $"+{evt.Value}";
                case CombatFeedbackKind.Crit: return $"CRIT! {evt.Value}";
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

        private void SpawnFeedback(string label, Color color, Vector3 worldPos)
        {
            GameObject go;
            Text text;

            if (canvasRoot != null)
            {
                // UI mode: spawn under canvas
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
                    text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                    text.fontSize = defaultFontSize;
                    text.alignment = TextAnchor.MiddleCenter;
                }
                if (text != null)
                {
                    text.text = label;
                    text.color = color;
                }
                // Place at a random offset near the screen center for demo
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
                    StartPos = rt != null ? (Vector3)rt.anchoredPosition : Vector3.zero,
                    StartTime = Time.time,
                    Lifetime = textLifetimeSeconds,
                });
            }
            else
            {
                // World-space fallback: use TextMesh (3D)
                go = new GameObject("CombatFeedbackText3D", typeof(TextMesh));
                go.transform.SetParent(transform, false);
                go.transform.position = worldPos + Vector3.up * worldYOffset;
                var tm = go.GetComponent<TextMesh>();
                tm.text = label;
                tm.color = color;
                tm.fontSize = 32;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.alignment = TextAlignment.Center;
                tm.characterSize = 0.1f;
                _active.Add(new ActiveFeedback
                {
                    Go = go,
                    Text = null, // TextMesh fade handled via material color
                    StartPos = go.transform.position,
                    StartTime = Time.time,
                    Lifetime = textLifetimeSeconds,
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
