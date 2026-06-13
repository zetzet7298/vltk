// -----------------------------------------------------------------------------
// VLTK.Backend.Combat — HitEffectSpawner
// MonoBehaviour subscribe CombatFeedbackBus.OnFeedback để spawn SPR hit
// effect tại vị trí event. Theo SprRuntimeService pattern từ CTS-06
// (fullstack/cts06-male-weapon-spr-staging): load SPR từ UID-hashed file
// trong StreamingAssets/Sprites/{uid}.spr, cache kết quả.
//
// Design:
//   - Resolve SPR bằng <see cref="SprRuntimeService"/> (đã có sẵn trong
//     VLTK.Sprites) — fallback nếu SPR không tồn tại: tạo colored quad.
//   - Mỗi hit effect là GameObject con của <transform>, tự Destroy sau
//     <see cref="effectLifetimeSeconds"/>.
//   - Miss feedback KHÔNG spawn hit effect (chỉ text "Miss").
//   - Heal feedback cũng KHÔNG spawn hit effect (chỉ text "+N" xanh).
//   - Có thể override hitEffectUid trong Inspector; default = "0" (PC
//     UID hash của hit effect tiêu chuẩn, fallback nếu không tìm thấy).
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Backend.Combat
{
    /// <summary>
    /// Spawn hit effect (SPR sprite) tại vị trí CombatFeedbackEvent.Position
    /// khi nhận event Normal/Crit. Miss/Heal thì bỏ qua.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HitEffectSpawner : MonoBehaviour
    {
        // ----- Inspector -----

        [Tooltip("UID hex hash (signed) cho hit effect SPR. PC engine VLTK dùng " +
                 "FileNameHash. Nếu SPR không tồn tại, fallback về colored quad.")]
        public string hitEffectUid = "00000000";

        [Tooltip("Số sprite frame cho hit effect (loop animation). Nếu = 1 thì static.")]
        [Min(1)]
        public int frameCount = 4;

        [Tooltip("Thời gian mỗi frame (giây).")]
        [Min(0.05f)]
        public float frameIntervalSeconds = 0.1f;

        [Tooltip("Thời gian sống tối đa của effect (giây).")]
        [Min(0.1f)]
        public float effectLifetimeSeconds = 0.5f;

        [Tooltip("Kích thước world-space (sprite hiển thị 1 quad bao nhiêu unit).")]
        public Vector3 effectScale = new(1f, 1f, 1f);

        [Tooltip("Sorting order cho SpriteRenderer (cao = render trước).")]
        [Min(-1000)]
        public int sortingOrder = 100;

        [Tooltip("Tint màu cho hit effect (1,1,1 = giữ nguyên SPR).")]
        public Color tint = Color.white;

        [Tooltip("Màu fallback nếu SPR không tìm thấy. Crit dùng màu này + tint vàng.")]
        public Color fallbackColor = new(1f, 0.4f, 0.4f, 0.9f);

        // ----- Runtime -----

        private SprRuntimeService _sprService;

        private void OnEnable()
        {
            // SprRuntimeService không tốn kém khi khởi tạo (chỉ set root path).
            _sprService = _sprService ?? new SprRuntimeService();
            CombatFeedbackBus.OnFeedback += HandleFeedback;
        }

        private void OnDisable()
        {
            CombatFeedbackBus.OnFeedback -= HandleFeedback;
        }

        // ----- Event handler -----

        private void HandleFeedback(CombatFeedbackEvent evt)
        {
            // Miss và Heal KHÔNG spawn hit effect
            if (evt.IsMiss || evt.IsHeal) return;

            // Resolve sprite
            var sprite = ResolveHitSprite();
            if (sprite == null)
            {
                Debug.LogWarning($"[HitEffectSpawner] SPR uid='{hitEffectUid}' " +
                                 "không tìm thấy; dùng fallback quad.");
            }
            SpawnEffect(evt.Position, sprite, evt.IsCritical);
        }

        private Sprite ResolveHitSprite()
        {
            if (string.IsNullOrEmpty(hitEffectUid) || _sprService == null) return null;
            try
            {
                // ComputePathUidHex từ SprRuntimeService đã bao gồm signing rule.
                // Truyền thẳng UID string → loader tìm file {uid}.spr trong
                // StreamingAssets/Sprites/ (và sub-folders nếu có).
                return _sprService.ResolveSprite(hitEffectUid);
            }
            catch
            {
                return null;
            }
        }

        private void SpawnEffect(Vector3 worldPos, Sprite sprite, bool isCrit)
        {
            GameObject go = new("HitEffect")
            {
                transform = { parent = transform, position = worldPos, localScale = effectScale },
            };
            // Sprite renderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            sr.color = isCrit ? (tint * new Color(1.2f, 1.2f, 0.6f, 1f)) : tint;
            if (sprite == null)
            {
                // Fallback: quad colored
                var mf = go.AddComponent<MeshFilter>();
                mf.sharedMesh = GetQuadMesh();
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = GetUnlitMaterial(isCrit ? Color.yellow : fallbackColor);
            }
            // Auto-destroy
            Destroy(go, effectLifetimeSeconds);
        }

        // ----- Helpers (cached) -----

        private static Mesh _quadMesh;
        private static Mesh GetQuadMesh()
        {
            if (_quadMesh != null) return _quadMesh;
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Quad);
            _quadMesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            // Don't actually create the primitive in scene; destroy it.
            // (CreatePrimitive returns a GameObject — destroy it now to keep scene clean.)
            if (Application.isPlaying) Destroy(primitive);
            else DestroyImmediate(primitive);
            return _quadMesh;
        }

        private static Material _unlitMaterialCache;
        private Material GetUnlitMaterial(Color color)
        {
            if (_unlitMaterialCache != null)
            {
                _unlitMaterialCache.color = color;
                return _unlitMaterialCache;
            }
            // Use built-in unlit shader (URP may not have Sprites/Default; use
            // legacy Unlit/Color which is always available).
            var shader = Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default");
            _unlitMaterialCache = new Material(shader) { color = color };
            return _unlitMaterialCache;
        }
    }
}
