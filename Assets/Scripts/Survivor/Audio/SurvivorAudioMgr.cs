// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorAudioMgr
// Own audio pipeline (D10, ticket 36): BGM source + SFX pool riêng, clip loader
// tự tải StreamingAssets (không phụ thuộc SandboxManager), AudioMixer
// (master/bgm/sfx) nếu asset được gán, volume API cho settings (ticket 40).
// Skill cast → AudioService.PlaySkillCast qua SandboxManager, fail-closed.
// Fail-closed toàn cục: thiếu clip/mixer/Sandbox → im lặng, không crash.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using VLTK.Sandbox;

namespace VLTK.Survivor
{
    public sealed class SurvivorAudioMgr : MonoBehaviour
    {
        public static SurvivorAudioMgr Instance { get; private set; }

        /// <summary>Gán AudioMixer asset (Assets/Survivor/Audio/Survivor.mixer) trong Inspector.</summary>
        [SerializeField] private AudioMixer _mixer;

        private const int SfxPoolSize = 8;
        private const float ClipDefaultVolume = 0.9f;

        private readonly List<AudioSource> _sfxPool = new();
        private readonly Dictionary<string, AudioClip> _clipCache = new();
        private readonly HashSet<string> _missingWarned = new();
        private readonly Dictionary<SurvivorAudioBus, float> _volumes = new()
        {
            [SurvivorAudioBus.Master] = 1f,
            [SurvivorAudioBus.Bgm] = 0.6f,   // parity AudioService default
            [SurvivorAudioBus.Sfx] = 0.8f,
        };

        private AudioSource _bgm;
        private int _sfxIndex;
        private SurvivorAudioContext _context;
        private string _currentBgmPath;
        private bool _bgmGrouped;
        private bool _sfxGrouped;
        private bool _wiredPlayerEvents;

        public SurvivorAudioContext Context => _context;

        // ── Boot ───────────────────────────────────────────────────────

        /// <summary>Tạo singleton nếu chưa có (orchestrator gọi ở Director.OnInit).</summary>
        public static SurvivorAudioMgr EnsureInstance()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("SurvivorAudioMgr");
            return go.AddComponent<SurvivorAudioMgr>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgm = MakeSource("BGM", loop: true);
            for (int i = 0; i < SfxPoolSize; i++) _sfxPool.Add(MakeSource($"SFX_{i}", loop: false));

            // Mixer routing (fail-closed: thiếu mixer/group → volume tính tay).
            if (_mixer != null)
            {
                var bgmGroups = _mixer.FindMatchingGroups("BGM");
                var sfxGroups = _mixer.FindMatchingGroups("SFX");
                var bgmGroup = bgmGroups.Length > 0 ? bgmGroups[0] : null;
                var sfxGroup = sfxGroups.Length > 0 ? sfxGroups[0] : null;
                _bgm.outputAudioMixerGroup = bgmGroup;
                _bgmGrouped = bgmGroup != null;
                _sfxGrouped = sfxGroup != null;
                for (int i = 0; i < _sfxPool.Count; i++) _sfxPool[i].outputAudioMixerGroup = sfxGroup;
            }

            // Settings bootstrap (ticket 40): volume persist → áp mixer ngay.
            // Additive — chưa có save thì default trong _volumes giữ nguyên (1/0.6/0.8).
            ApplyPersistedVolumes();
        }

        private void Update()
        {
            // Tự wire event player (LevelUp/Died) — không sửa SurvivorPlayer/Director.
            if (_wiredPlayerEvents) return;
            var player = SurvivorGameDirector.Instance != null ? SurvivorGameDirector.Instance.Player : null;
            if (player == null) return;
            player.LevelUp += _ => PlayEvent(SurvivorAudioEvent.LevelUp);
            player.Died += _ => PlayEvent(SurvivorAudioEvent.Die);
            _wiredPlayerEvents = true;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private AudioSource MakeSource(string name, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var src = go.AddComponent<AudioSource>();
            src.loop = loop;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            return src;
        }

        // ── BGM context switch ─────────────────────────────────────────

        public void SetContext(SurvivorAudioContext context)
        {
            if (_context == context) return;
            _context = context;
            PlayBgmTrack(context);
        }

        private async void PlayBgmTrack(SurvivorAudioContext context)
        {
            var path = SurvivorAudioBank.BgmTrackRelativePath(context);
            if (path == null || !SurvivorAudioBank.IsStaged(path)) { WarnMissingOnce(path); return; }
            if (path == _currentBgmPath && _bgm != null && _bgm.isPlaying) return;

            var clip = await LoadClipAsync(path);
            if (clip == null || _context != context || _bgm == null) return;

            _currentBgmPath = path;
            _bgm.clip = clip;
            if (_bgmGrouped) _bgm.volume = ClipDefaultVolume;
            else _bgm.volume = ClipDefaultVolume * _volumes[SurvivorAudioBus.Bgm] * _volumes[SurvivorAudioBus.Master];
            _bgm.Play();
        }

        // ── SFX ────────────────────────────────────────────────────────

        public void PlayEvent(SurvivorAudioEvent e)
        {
            var path = SurvivorAudioBank.EventSfxRelativePath(e);
            if (path == null || !SurvivorAudioBank.IsStaged(path)) { WarnMissingOnce(path); return; }
            PlayClip(path);
        }

        /// <summary>Skill cast qua AudioService (Sandbox) — fail-closed: thiếu Sandbox → im lặng.</summary>
        public void PlaySkillCast(int skillId)
        {
            if (!SurvivorAudioBank.IsSkillCastStaged(skillId)) { WarnMissingOnce(SurvivorAudioBank.SkillCastRelativePath(skillId)); return; }
            PlaySkillCastPath(SurvivorAudioBank.SkillCastRelativePath(skillId));
        }

        public void PlaySkillCastPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return;
            var audio = SandboxManager.Instance != null ? SandboxManager.Instance.AudioService : null;
            audio?.PlaySkillCast(relativePath); // AudioService tự warn missing; volume theo category Combat.
        }

        private async void PlayClip(string relativePath)
        {
            var clip = await LoadClipAsync(relativePath);
            if (clip == null || _sfxPool.Count == 0) return;

            var src = _sfxPool[_sfxIndex % _sfxPool.Count];
            _sfxIndex++;
            float vol = _sfxGrouped ? ClipDefaultVolume
                : ClipDefaultVolume * _volumes[SurvivorAudioBus.Sfx] * _volumes[SurvivorAudioBus.Master];
            src.PlayOneShot(clip, vol);
        }

        // ── Volume API (settings ticket 40) ────────────────────────────

        public float GetVolume(SurvivorAudioBus bus) => _volumes.TryGetValue(bus, out var v) ? v : 1f;

        public void SetVolume(SurvivorAudioBus bus, float volume)
        {
            _volumes[bus] = Mathf.Clamp01(volume);
            if (_mixer == null) return;
            // Mixer xử lý gain; SetFloat false = param chưa exposed → fallback volume tay.
            _mixer.SetFloat(ParamName(bus), SurvivorAudioBank.ToDb(_volumes[bus]));
        }

        /// <summary>
        /// Ticket 40 hook: đọc volume từ settings đã persist (SurvivorSaveService 39)
        /// + master key riêng (panel), áp vào mixer. Fail-closed: settings corrupt
        /// → LoadSettings reset defaults; thiếu PlayerPrefs key → fallback 1.
        /// </summary>
        private void ApplyPersistedVolumes()
        {
            var settings = new SurvivorSaveService().LoadSettings(out _);
            float master = PlayerPrefs.GetFloat(SurvivorAudioSettingsController.MasterVolumeKey, 1f);
            SetVolume(SurvivorAudioBus.Master, master);
            SetVolume(SurvivorAudioBus.Bgm, settings.audioBgm);
            SetVolume(SurvivorAudioBus.Sfx, settings.audioSfx);
        }

        private static string ParamName(SurvivorAudioBus bus)
        {
            return bus switch
            {
                SurvivorAudioBus.Master => SurvivorAudioBank.MixerParamMaster,
                SurvivorAudioBus.Bgm => SurvivorAudioBank.MixerParamBgm,
                SurvivorAudioBus.Sfx => SurvivorAudioBank.MixerParamSfx,
                _ => null,
            };
        }

        // ── Clip loading (own pipeline, mirror AudioService.LoadClipAsync) ──

        private async Task<AudioClip> LoadClipAsync(string relativePath)
        {
            var key = relativePath.Replace('\\', '/').TrimStart('/');
            if (_clipCache.TryGetValue(key, out var cached)) return cached;

            var fullPath = Path.Combine(Application.streamingAssetsPath, key);
            if (!fullPath.Contains("://")) fullPath = "file://" + fullPath;
            if (fullPath.StartsWith("file://", System.StringComparison.OrdinalIgnoreCase))
            {
                var local = fullPath.Substring("file://".Length);
                if (!File.Exists(local)) { WarnMissingOnce(key); return null; }
            }

            using var request = UnityWebRequestMultimedia.GetAudioClip(fullPath, InferAudioType(key));
            var op = request.SendWebRequest();
            while (!op.isDone) await Task.Yield();
            if (request.result != UnityWebRequest.Result.Success) { WarnMissingOnce(key); return null; }

            var clip = DownloadHandlerAudioClip.GetContent(request);
            if (clip != null) _clipCache[key] = clip;
            return clip;
        }

        private static AudioType InferAudioType(string relativePath)
        {
            var ext = Path.GetExtension(relativePath)?.ToLowerInvariant();
            return ext switch
            {
                ".ogg" => AudioType.OGGVORBIS,
                ".mp3" => AudioType.MPEG,
                ".wav" or ".wave" => AudioType.WAV,
                _ => AudioType.UNKNOWN,
            };
        }

        private void WarnMissingOnce(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath) || _missingWarned.Contains(relativePath)) return;
            _missingWarned.Add(relativePath);
            Debug.LogWarning($"[SurvivorAudio] chưa staged (im lặng fail-closed): StreamingAssets/{relativePath}");
        }
    }
}
