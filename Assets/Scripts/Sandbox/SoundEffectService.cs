// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.23 Sound Effect runtime service
// Quản lý sound effects catalog (UI click, combat, NPC, ambient, music).
// PC source: settings/soundeffect.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SoundEffectService
    {
        public const string LogTag = "SoundEffect";

        private readonly PcSoundEffectRegistry _registry;
        private readonly Dictionary<int, AudioSource> _activeHandles = new();
        private int _nextHandleId = 1;

        public int Count => _registry?.Count ?? 0;

        public SoundEffectService() { }
        public SoundEffectService(PcSoundEffectRegistry registry) { _registry = registry ?? new PcSoundEffectRegistry(); }

        public static SoundEffectService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcSound");
            var reg = PcSoundEffectParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} sound effect");
            return new SoundEffectService(reg);
        }

        public PcSoundEffectEntry GetSound(int soundId) => _registry != null ? _registry.Get(soundId) : null;

        public IReadOnlyList<PcSoundEffectEntry> GetByCategory(int category)
            => _registry != null ? _registry.GetByCategory(category) : Array.Empty<PcSoundEffectEntry>();

        public string GetSoundPath(int soundId)
        {
            var e = GetSound(soundId);
            return e?.filePath ?? string.Empty;
        }

        public AudioClip TryLoadAudioClip(int soundId)
        {
            string p = GetSoundPath(soundId);
            if (string.IsNullOrEmpty(p)) return null;
            try
            {
                string resName = p;
                if (resName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    resName = resName.Substring(0, resName.Length - 4);
                else if (resName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    resName = resName.Substring(0, resName.Length - 4);
                else if (resName.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase))
                    resName = resName.Substring(0, resName.Length - 4);
                return Resources.Load<AudioClip>(resName);
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"TryLoadAudioClip({soundId}) failed: {ex.Message}");
                return null;
            }
        }

        public int Play(int soundId, Vector3 pos)
        {
            var clip = TryLoadAudioClip(soundId);
            if (clip == null) return 0;
            try
            {
                var go = new GameObject($"VLTK_SoundEffect_{soundId}");
                go.transform.position = pos;
                var src = go.AddComponent<AudioSource>();
                src.clip = clip;
                var entry = GetSound(soundId);
                if (entry != null && entry.volume > 0) src.volume = Mathf.Clamp01(entry.volume / 100f);
                src.Play();
                int handle = _nextHandleId++;
                _activeHandles[handle] = src;
                if (_activeHandles.Count > 256)
                {
                    SubsystemLog.Warn(LogTag, "Active sound handles > 256, có thể leak");
                }
                return handle;
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"Play({soundId}) failed: {ex.Message}");
                return 0;
            }
        }

        public bool Stop(int handle)
        {
            if (handle <= 0) return false;
            if (!_activeHandles.TryGetValue(handle, out var src)) return false;
            try
            {
                if (src != null)
                {
                    src.Stop();
                    if (src.gameObject != null) UnityEngine.Object.Destroy(src.gameObject);
                }
            }
            catch { }
            _activeHandles.Remove(handle);
            return true;
        }

        public string GetCategoryName(int category)
        {
            switch (category)
            {
                case 0: return "UI Click";
                case 1: return "UI Mở";
                case 2: return "UI Đóng";
                case 3: return "Đánh trúng";
                case 4: return "Kỹ năng";
                case 5: return "Chết";
                case 6: return "NPC Chào";
                case 7: return "Ambient";
                case 8: return "Nhạc nền";
                default: return $"Loại {category}";
            }
        }
    }
}
