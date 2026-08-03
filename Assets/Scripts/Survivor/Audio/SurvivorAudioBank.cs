// -----------------------------------------------------------------------------
// VLTK.Survivor — SurvivorAudioBank
// Pure-logic audio mapping (D10): event → sfx clip path, context → bgm track,
// skill-cast staged check, volume math. Không MonoBehaviour — EditMode test seam.
// Fail-closed: chưa staged audio → path không tồn tại → runtime im lặng, không crash.
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>SFX game events (ticket 36: hit/cast/pickup/levelup/die).</summary>
    public enum SurvivorAudioEvent
    {
        Hit,      // projectile trúng monster
        Cast,     // player bắn (basic attack)
        Pickup,   // nhặt XpGem
        LevelUp,  // lên level
        Die,      // player chết (game over)
    }

    /// <summary>BGM context — 3 track menu/battle/boss.</summary>
    public enum SurvivorAudioContext
    {
        Menu,
        Battle,
        Boss,
    }

    /// <summary>Volume bus — settings (ticket 40) set qua đây.</summary>
    public enum SurvivorAudioBus
    {
        Master,
        Bgm,
        Sfx,
    }

    /// <summary>
    /// Static mapping + fail-closed staged checks. Chưa staged file → method trả
    /// null/false → caller im lặng. Root có thể override trong test.
    /// </summary>
    public static class SurvivorAudioBank
    {
        // ── Mixer (asset sinh bởi editor script, gán vào SurvivorAudioMgr._mixer) ──
        public const string MixerAssetPath = "Assets/Survivor/Audio/Survivor.mixer";
        public const string MixerParamMaster = "masterVol";
        public const string MixerParamBgm = "bgmVol";
        public const string MixerParamSfx = "sfxVol";

        // ── Own SFX staging dir (tự author, KHÔNG đụng Audio/SFX của Sandbox) ──
        private const string SfxDir = "survivor/audio/sfx";
        private const string BgmDir = "survivor/audio/bgm";
        // Skill SFX = JX staged sẵn (StreamingAssets/sound/skill/, 28 wav).
        private const string SkillDir = "sound/skill";

        /// <summary>Overridable trong EditMode test; default = StreamingAssets thật.</summary>
        public static string StreamingAssetsRoot { get; set; } = Application.streamingAssetsPath;

        // ── Mapping ────────────────────────────────────────────────────

        /// <summary>SFX clip relative path cho event; null nếu event chưa có mapping.</summary>
        public static string EventSfxRelativePath(SurvivorAudioEvent e)
        {
            return e switch
            {
                SurvivorAudioEvent.Hit => $"{SfxDir}/hit.wav",
                SurvivorAudioEvent.Cast => $"{SfxDir}/cast.wav",
                SurvivorAudioEvent.Pickup => $"{SfxDir}/pickup.wav",
                SurvivorAudioEvent.LevelUp => $"{SfxDir}/levelup.wav",
                SurvivorAudioEvent.Die => $"{SfxDir}/die.wav",
                _ => null,
            };
        }

        /// <summary>BGM track relative path cho context; null nếu chưa có.</summary>
        public static string BgmTrackRelativePath(SurvivorAudioContext c)
        {
            return c switch
            {
                SurvivorAudioContext.Menu => $"{BgmDir}/menu.ogg",
                SurvivorAudioContext.Battle => $"{BgmDir}/battle.ogg",
                SurvivorAudioContext.Boss => $"{BgmDir}/boss.ogg",
                _ => null,
            };
        }

        /// <summary>Skill cast wav theo skillId: sound_k{id:000}.wav (tên file JX staged).</summary>
        public static string SkillCastRelativePath(int skillId)
        {
            return $"{SkillDir}/sound_k{skillId:D3}.wav";
        }

        // ── Fail-closed staged checks ──────────────────────────────────

        public static bool IsStaged(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath)) return false;
            return File.Exists(Path.Combine(StreamingAssetsRoot, relativePath));
        }

        public static bool IsEventSfxStaged(SurvivorAudioEvent e)
        {
            var p = EventSfxRelativePath(e);
            return p != null && IsStaged(p);
        }

        public static bool IsBgmTrackStaged(SurvivorAudioContext c)
        {
            var p = BgmTrackRelativePath(c);
            return p != null && IsStaged(p);
        }

        public static bool IsSkillCastStaged(int skillId)
        {
            return IsStaged(SkillCastRelativePath(skillId));
        }

        // ── Volume math (mixer SetFloat nhận dB) ───────────────────────

        /// <summary>Linear 0..1 → dB. 0 → -80 (audible floor, mixer convention).</summary>
        public static float ToDb(float linear)
        {
            return linear <= 0.0001f ? -80f : 20f * Mathf.Log10(linear);
        }
    }
}
