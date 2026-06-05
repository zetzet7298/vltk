// -----------------------------------------------------------------------------
// VLTK Mobile — ST-3.11 Missile Effect runtime service (Hiệu Ứng Đạn)
// Wraps PcMissileEffectRegistry + thêm instance pool để play/stop effect.
// Vietnamese: "Hiệu Ứng", "Chém", "Nổ AOE", "Phi Tiêu", "Trận Pháp", "Sát Thương".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime cho hiệu ứng đạn (missle): play / stop / stop all.
    /// Trả về instanceId để cleanup sau này. Khi không tìm thấy effect vẫn
    /// trả về handle (không throw) để caller không phải wrap try/catch.
    /// </summary>
    public class MissileEffectService
    {
        public const string LogTag = "MissileEffect";
        public const string DefaultStreamingDir = "Reference/PcMissle";

        private PcMissileEffectRegistry _registry;
        private readonly Dictionary<int, GameObject> _liveInstances = new();
        private int _nextHandle = 1;

        public int Count => _registry != null ? _registry.Count : 0;
        public int LiveCount => _liveInstances.Count;

        public MissileEffectService() { }
        public MissileEffectService(PcMissileEffectRegistry reg) { _registry = reg; }

        public void AttachRegistry(PcMissileEffectRegistry reg)
        {
            _registry = reg ?? new PcMissileEffectRegistry();
            SubsystemLog.Info(LogTag, $"MissileEffect loaded: {Count} effect");
        }

        public PcMissileEffectEntry GetEffect(int effectId)
            => _registry != null ? _registry.Get(effectId) : null;

        public IReadOnlyList<PcMissileEffectEntry> GetByType(int effectType)
            => _registry != null
                ? _registry.GetByType(effectType)
                : (IReadOnlyList<PcMissileEffectEntry>)System.Array.Empty<PcMissileEffectEntry>();

        public IReadOnlyList<PcMissileEffectEntry> All
            => _registry != null ? _registry.All : (IReadOnlyList<PcMissileEffectEntry>)System.Array.Empty<PcMissileEffectEntry>();

        /// <summary>
        /// Play effect tại vị trí pos với rotation rot. Trả về instanceId (>0)
        /// hoặc -1 nếu không tìm thấy effect. Không throw khi thiếu data.
        /// </summary>
        public int PlayEffect(int effectId, Vector3 pos, Quaternion rot)
        {
            if (_registry == null) return -1;
            var entry = _registry.Get(effectId);
            if (entry == null)
            {
                SubsystemLog.Warn(LogTag, $"Effect {effectId} không tồn tại");
                return -1;
            }
            int handle = _nextHandle++;
            GameObject go = null;
            // Chỉ tạo GameObject khi runtime path có thật (test runner tránh crash)
#if UNITY_EDITOR || !UNITY_INCLUDE_TESTS
            try
            {
                go = new GameObject($"MissileEffect_{effectId}_{handle}");
                go.transform.position = pos;
                go.transform.rotation = rot;
                if (entry.isLooping)
                {
                    var lifetime = Mathf.Max(0.05f, entry.durationMs / 1000f);
                    Object.Destroy(go, lifetime);
                }
                else
                {
                    Object.Destroy(go, 0.05f);
                }
            }
            catch (System.Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"PlayEffect {effectId} lỗi: {ex.Message}");
            }
#endif
            if (go != null) _liveInstances[handle] = go;
            return handle;
        }

        public bool StopEffect(int instanceId)
        {
            if (!_liveInstances.TryGetValue(instanceId, out var go))
                return false;
            _liveInstances.Remove(instanceId);
            if (go != null) Object.Destroy(go);
            return true;
        }

        public void StopAll()
        {
            foreach (var kv in _liveInstances)
            {
                if (kv.Value != null) Object.Destroy(kv.Value);
            }
            _liveInstances.Clear();
        }

        public bool IsPlaying(int instanceId)
            => _liveInstances.ContainsKey(instanceId);

        public static MissileEffectService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new MissileEffectService();
            if (Directory.Exists(dir))
            {
                var reg = PcMissileEffectParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"MissileEffect dir không tồn tại {dir}");
            }
            return svc;
        }
    }
}
