// -----------------------------------------------------------------------------
// VLTK Mobile — Audio Service
// Sound effects and background music manager.
// PC source: settings/sound.txt, music/*.mp3, sfx/*.wav
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
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

        public void PlayBGM(string id)
        {
            if (!BgmEnabled || _bgmSource == null) return;

            if (!_defs.TryGetValue(id, out var def) || def.category != AudioCategory.BGM)
            {
                SubsystemLog.Warn("Audio", $"BGM '{id}' not found");
                return;
            }

            // Same track already playing — skip
            if (_bgmSource.isPlaying && _bgmSource.clip != null && _bgmSource.clip.name == id)
                return;

            _bgmSource.volume = def.volume * GetCategoryVolume(AudioCategory.BGM);
            _bgmSource.loop = def.loop;

            // Try to load from StreamingAssets
            var clip = LoadClip(def.resourcePath);
            if (clip != null)
            {
                _bgmSource.clip = clip;
                _bgmSource.Play();
                SubsystemLog.Info("Audio", $"BGM: {id}");
            }
            else
            {
                SubsystemLog.Warn("Audio", $"BGM clip not found: {def.resourcePath}");
            }
        }

        public void StopBGM()
        {
            if (_bgmSource != null && _bgmSource.isPlaying)
                _bgmSource.Stop();
        }

        public void PlaySFX(string id, float volumeScale = 1f)
        {
            if (!SfxEnabled) return;

            if (!_defs.TryGetValue(id, out var def))
            {
                SubsystemLog.Warn("Audio", $"SFX '{id}' not found");
                return;
            }

            var src = GetNextSfxSource();
            if (src == null) return;

            src.volume = def.volume * GetCategoryVolume(def.category) * volumeScale;
            src.loop = false;

            var clip = LoadClip(def.resourcePath);
            if (clip != null)
            {
                src.PlayOneShot(clip, src.volume);
            }
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

        private AudioClip LoadClip(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            // Try StreamingAssets first
            var fullPath = System.IO.Path.Combine(Application.streamingAssetsPath, path);
            if (!System.IO.File.Exists(fullPath)) return null;

            // Use UnityWebRequest for audio loading
            // For now return null — actual audio loading requires async
            return null;
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
