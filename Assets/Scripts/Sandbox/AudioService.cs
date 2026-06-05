// -----------------------------------------------------------------------------
// VLTK Mobile — Audio Service
// Sound effects and background music manager.
// PC source: settings/sound.txt, music/*.mp3, sfx/*.wav
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Audio clip category.</summary>
    public enum AudioCategory
    {
        BGM,         // Background music
        SFX,         // Sound effects
        Ambient,     // Ambient/environment sounds
        Combat,      // Combat sounds
        UI,          // UI interaction sounds
    }

    /// <summary>Audio clip reference.</summary>
    [Serializable]
    public class AudioDef
    {
        public string id;
        public AudioCategory category;
        public string resourcePath;
        public float volume = 1f;
        public bool loop;
        public float fadeIn = 0f;
        public float fadeOut = 0f;
    }

    /// <summary>
    /// Audio service for the VLTK mobile client.
    /// Manages BGM playback, SFX pools, and ambient sounds.
    /// Pure C# with AudioSource pooling — no scene setup required.
    /// </summary>
    public class AudioService
    {
        private readonly Dictionary<string, AudioDef> _defs = new();
        private readonly Dictionary<string, AudioClip> _clipCache = new();
        private readonly Dictionary<string, Task<AudioClip>> _clipLoadTasks = new();
        private readonly HashSet<string> _missingClipWarnings = new();
        private readonly Dictionary<AudioCategory, float> _categoryVolume = new()
        {
            [AudioCategory.BGM] = 0.6f,
            [AudioCategory.SFX] = 0.8f,
            [AudioCategory.Ambient] = 0.4f,
            [AudioCategory.Combat] = 0.7f,
            [AudioCategory.UI] = 0.5f,
        };

        private AudioSource _bgmSource;
        private readonly List<AudioSource> _sfxPool = new();
        private int _sfxPoolIndex;
        private const int SFX_POOL_SIZE = 8;
        private Transform _audioRoot;

        public bool BgmEnabled { get; set; } = true;
        public bool SfxEnabled { get; set; } = true;

        public AudioService()
        {
            LoadDefaultAudioDefs();
        }

        public void Initialize(Transform root)
        {
            _audioRoot = root;

            // Create BGM source
            var bgmGo = new GameObject("BGM_Source");
            bgmGo.transform.SetParent(root, false);
            _bgmSource = bgmGo.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.playOnAwake = false;

            // Create SFX pool
            for (int i = 0; i < SFX_POOL_SIZE; i++)
            {
                var sfxGo = new GameObject($"SFX_{i}");
                sfxGo.transform.SetParent(root, false);
                var src = sfxGo.AddComponent<AudioSource>();
                src.playOnAwake = false;
                _sfxPool.Add(src);
            }
        }

        // ── Playback ────────────────────────────────────────────────────

        private string _requestedBgmId;
        private string _loadingBgmId;

        public void PlayBGM(string id)
        {
            _ = PlayBGMAsync(id);
        }

        public async Task PlayBGMAsync(string id)
        {
            if (!BgmEnabled || _bgmSource == null) return;

            if (!_defs.TryGetValue(id, out var def) || def.category != AudioCategory.BGM)
            {
                SubsystemLog.Warn("Audio", $"BGM '{id}' not found");
                return;
            }

            // Only the currently playing track can be skipped. A previous request
            // may have failed while an older clip is still assigned to the source.
            if (_bgmSource.isPlaying && _bgmSource.clip != null && _bgmSource.clip.name == id)
                return;

            if (_loadingBgmId == id)
                return;

            _requestedBgmId = id;
            _loadingBgmId = id;
            var clip = await LoadClipAsync(def.resourcePath);
            if (_loadingBgmId == id)
                _loadingBgmId = null;

            if (clip == null)
            {
                if (_requestedBgmId == id)
                    _requestedBgmId = null;
                return;
            }

            // A newer PlayBGM request won while this load was in flight.
            if (_requestedBgmId != id || !BgmEnabled || _bgmSource == null)
                return;

            clip.name = id;
            _bgmSource.volume = def.volume * GetCategoryVolume(AudioCategory.BGM);
            _bgmSource.loop = def.loop;
            _bgmSource.clip = clip;
            _bgmSource.Play();
            SubsystemLog.Info("Audio", $"BGM: {id}");
        }

        public void StopBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
                _bgmSource.Stop();
        }

        public void PlaySFX(string id, float volumeScale = 1f)
        {
            _ = PlaySFXAsync(id, volumeScale);
        }

        public async Task PlaySFXAsync(string id, float volumeScale = 1f)
        {
            if (!SfxEnabled) return;

            if (!_defs.TryGetValue(id, out var def))
            {
                SubsystemLog.Warn("Audio", $"SFX '{id}' not found");
                return;
            }

            var clip = await LoadClipAsync(def.resourcePath);
            if (clip == null || !SfxEnabled) return;

            var src = GetNextSfxSource();
            if (src == null) return;

            src.volume = def.volume * GetCategoryVolume(def.category) * volumeScale;
            src.loop = false;
            src.PlayOneShot(clip, src.volume);
        }

        public void PlayCombatSFX(string action)
        {
            PlaySFX($"combat_{action}");
        }

        public void PlayUISFX(string action)
        {
            PlaySFX($"ui_{action}");
        }

        // ── Volume Control ──────────────────────────────────────────────

        public float GetCategoryVolume(AudioCategory category)
        {
            return _categoryVolume.TryGetValue(category, out var v) ? v : 1f;
        }

        public void SetCategoryVolume(AudioCategory category, float volume)
        {
            _categoryVolume[category] = Mathf.Clamp01(volume);
            if (category == AudioCategory.BGM && _bgmSource != null)
                _bgmSource.volume = volume;
        }

        // ── Internal ────────────────────────────────────────────────────

        private AudioSource GetNextSfxSource()
        {
            if (_sfxPool.Count == 0) return null;
            var src = _sfxPool[_sfxPoolIndex % _sfxPool.Count];
            _sfxPoolIndex++;
            return src;
        }

        public bool TryGetCachedClip(string resourcePath, out AudioClip clip)
        {
            return _clipCache.TryGetValue(NormalizeCacheKey(resourcePath), out clip);
        }

        public bool TryResolveResourcePath(string resourcePath, out string resourcesPath)
        {
            resourcesPath = ToResourcesPath(resourcePath);
            return !string.IsNullOrEmpty(resourcesPath);
        }

        public string ResolveStreamingAssetsUri(string resourcePath)
        {
            if (string.IsNullOrWhiteSpace(resourcePath)) return null;
            if (resourcePath.Contains("://")) return resourcePath;

            var fullPath = Path.Combine(Application.streamingAssetsPath, resourcePath);
            return fullPath.Contains("://") ? fullPath : $"file://{fullPath}";
        }

        public Task<AudioClip> LoadClip(string resourcePath)
        {
            return LoadClipAsync(resourcePath);
        }

        public Task<AudioClip> LoadClipAsync(string resourcePath)
        {
            var key = NormalizeCacheKey(resourcePath);
            if (string.IsNullOrEmpty(key)) return Task.FromResult<AudioClip>(null);
            if (_clipCache.TryGetValue(key, out var cached)) return Task.FromResult(cached);
            if (_clipLoadTasks.TryGetValue(key, out var pending)) return pending;

            var task = LoadClipInternalAsync(resourcePath, key);
            _clipLoadTasks[key] = task;
            return task;
        }

        private async Task<AudioClip> LoadClipInternalAsync(string resourcePath, string key)
        {
            try
            {
                var resourcesPath = ToResourcesPath(resourcePath);
                var resourcesClip = Resources.Load<AudioClip>(resourcesPath);
                if (resourcesClip != null)
                {
                    _clipCache[key] = resourcesClip;
                    return resourcesClip;
                }

                var uri = ResolveStreamingAssetsUri(resourcePath);
                if (uri == null)
                {
                    WarnMissingClipOnce(key, resourcePath, "empty path");
                    return null;
                }

                if (uri.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
                {
                    var localPath = uri.Substring("file://".Length);
                    if (!File.Exists(localPath))
                    {
                        WarnMissingClipOnce(key, resourcePath, $"not found in Resources/{resourcesPath} or StreamingAssets ({localPath})");
                        return null;
                    }
                }

                using var request = UnityWebRequestMultimedia.GetAudioClip(uri, InferAudioType(resourcePath));
                var op = request.SendWebRequest();
                while (!op.isDone) await Task.Yield();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    WarnMissingClipOnce(key, resourcePath, $"not found/readable at {uri}: {request.error}");
                    return null;
                }

                var clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip == null)
                {
                    WarnMissingClipOnce(key, resourcePath, $"decoded clip is null at {uri}");
                    return null;
                }

                _clipCache[key] = clip;
                return clip;
            }
            finally
            {
                _clipLoadTasks.Remove(key);
            }
        }

        private void WarnMissingClipOnce(string key, string resourcePath, string reason)
        {
            if (_missingClipWarnings.Add(key))
                SubsystemLog.Warn("Audio", $"Audio clip missing: '{resourcePath}' ({reason})");
        }

        private static string NormalizeCacheKey(string resourcePath)
        {
            return string.IsNullOrWhiteSpace(resourcePath)
                ? string.Empty
                : resourcePath.Replace('\\', '/').Trim().TrimStart('/');
        }

        private static string ToResourcesPath(string resourcePath)
        {
            var normalized = NormalizeCacheKey(resourcePath);
            if (string.IsNullOrEmpty(normalized)) return string.Empty;

            const string resourcesMarker = "/Resources/";
            var markerIndex = normalized.IndexOf(resourcesMarker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
                normalized = normalized.Substring(markerIndex + resourcesMarker.Length);
            else if (normalized.StartsWith("Resources/", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring("Resources/".Length);

            return Path.ChangeExtension(normalized, null);
        }

        private static AudioType InferAudioType(string resourcePath)
        {
            var ext = Path.GetExtension(resourcePath)?.ToLowerInvariant();
            return ext switch
            {
                ".aif" or ".aiff" => AudioType.AIFF,
                ".it" => AudioType.IT,
                ".mod" => AudioType.MOD,
                ".mp3" => AudioType.MPEG,
                ".ogg" => AudioType.OGGVORBIS,
                ".s3m" => AudioType.S3M,
                ".wav" or ".wave" => AudioType.WAV,
                ".xm" => AudioType.XM,
                _ => AudioType.UNKNOWN,
            };
        }

        private void LoadDefaultAudioDefs()
        {
            // BGM per map (PC source: music/ directory)
            AddDef("bgm_balang", AudioCategory.BGM, "Audio/BGM/balang.ogg", 0.6f, true);
            AddDef("bgm_giangtan", AudioCategory.BGM, "Audio/BGM/giangtan.ogg", 0.6f, true);
            AddDef("bgm_tuongduong", AudioCategory.BGM, "Audio/BGM/tuongduong.ogg", 0.6f, true);
            AddDef("bgm_thanhdo", AudioCategory.BGM, "Audio/BGM/thanhdo.ogg", 0.6f, true);
            AddDef("bgm_daily", AudioCategory.BGM, "Audio/BGM/daily.ogg", 0.6f, true);
            AddDef("bgm_bienkinh", AudioCategory.BGM, "Audio/BGM/bienkinh.ogg", 0.6f, true);

            // Combat SFX
            AddDef("combat_hit", AudioCategory.Combat, "Audio/SFX/combat_hit.wav", 0.8f);
            AddDef("combat_crit", AudioCategory.Combat, "Audio/SFX/combat_crit.wav", 0.9f);
            AddDef("combat_block", AudioCategory.Combat, "Audio/SFX/combat_block.wav", 0.7f);
            AddDef("combat_miss", AudioCategory.Combat, "Audio/SFX/combat_miss.wav", 0.5f);
            AddDef("combat_kill", AudioCategory.Combat, "Audio/SFX/combat_kill.wav", 0.9f);
            AddDef("combat_skill_cast", AudioCategory.Combat, "Audio/SFX/skill_cast.wav", 0.8f);
            AddDef("combat_skill_hit", AudioCategory.Combat, "Audio/SFX/skill_hit.wav", 0.8f);

            // UI SFX
            AddDef("ui_click", AudioCategory.UI, "Audio/SFX/ui_click.wav", 0.5f);
            AddDef("ui_quest_accept", AudioCategory.UI, "Audio/SFX/quest_accept.wav", 0.6f);
            AddDef("ui_quest_complete", AudioCategory.UI, "Audio/SFX/quest_complete.wav", 0.7f);
            AddDef("ui_levelup", AudioCategory.UI, "Audio/SFX/levelup.wav", 0.8f);
            AddDef("ui_item_pickup", AudioCategory.UI, "Audio/SFX/item_pickup.wav", 0.6f);
            AddDef("ui_equip", AudioCategory.UI, "Audio/SFX/equip.wav", 0.6f);

            // Ambient
            AddDef("ambient_forest", AudioCategory.Ambient, "Audio/Ambient/forest.ogg", 0.3f, true);
            AddDef("ambient_town", AudioCategory.Ambient, "Audio/Ambient/town.ogg", 0.3f, true);
            AddDef("ambient_river", AudioCategory.Ambient, "Audio/Ambient/river.ogg", 0.25f, true);
        }

        private void AddDef(string id, AudioCategory category, string path, float volume, bool loop = false)
        {
            _defs[id] = new AudioDef
            {
                id = id,
                category = category,
                resourcePath = path,
                volume = volume,
                loop = loop,
            };
        }
    }
}
