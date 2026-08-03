// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorAudioBankTests
// Ticket 36 gate: event→clip mapping, BGM context switch, volume routing math,
// fail-closed staged checks (chưa staged → im lặng, không crash).
// Pure logic — SurvivorAudioBank không MonoBehaviour, không PlayMode.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorAudioBankTests
    {
        // --- gate 1: event → sfx mapping (own dir, không đụng Sandbox Audio/SFX) ---
        [Test]
        public void EventSfxRelativePath_MapsAllEvents()
        {
            Assert.AreEqual("survivor/audio/sfx/hit.wav", SurvivorAudioBank.EventSfxRelativePath(SurvivorAudioEvent.Hit));
            Assert.AreEqual("survivor/audio/sfx/cast.wav", SurvivorAudioBank.EventSfxRelativePath(SurvivorAudioEvent.Cast));
            Assert.AreEqual("survivor/audio/sfx/pickup.wav", SurvivorAudioBank.EventSfxRelativePath(SurvivorAudioEvent.Pickup));
            Assert.AreEqual("survivor/audio/sfx/levelup.wav", SurvivorAudioBank.EventSfxRelativePath(SurvivorAudioEvent.LevelUp));
            Assert.AreEqual("survivor/audio/sfx/die.wav", SurvivorAudioBank.EventSfxRelativePath(SurvivorAudioEvent.Die));
        }

        // --- gate 2: bgm track mapping (own pipeline, menu/battle/boss) ---
        [Test]
        public void BgmTrackRelativePath_MapsAllContexts()
        {
            Assert.AreEqual("survivor/audio/bgm/menu.ogg", SurvivorAudioBank.BgmTrackRelativePath(SurvivorAudioContext.Menu));
            Assert.AreEqual("survivor/audio/bgm/battle.ogg", SurvivorAudioBank.BgmTrackRelativePath(SurvivorAudioContext.Battle));
            Assert.AreEqual("survivor/audio/bgm/boss.ogg", SurvivorAudioBank.BgmTrackRelativePath(SurvivorAudioContext.Boss));
        }

        // --- gate 3: fail-closed staged checks (disk thật) ---
        [Test]
        public void IsEventSfxStaged_OwnSfxChuaStaged_ImLang()
        {
            // Own SFX chưa author → false → runtime im lặng, không crash.
            // Khi stage file đầu tiên, update test này theo.
            Assert.IsFalse(SurvivorAudioBank.IsEventSfxStaged(SurvivorAudioEvent.Hit), "hit.wav chưa staged");
            Assert.IsFalse(SurvivorAudioBank.IsEventSfxStaged(SurvivorAudioEvent.LevelUp), "levelup.wav chưa staged");
        }

        [Test]
        public void IsSkillCastStaged_StagedWav_Found()
        {
            // sound_k001.wav staged sẵn (StreamingAssets/sound/skill/ 28 wav).
            Assert.IsTrue(SurvivorAudioBank.IsSkillCastStaged(1), "k001 staged");
            Assert.IsFalse(SurvivorAudioBank.IsSkillCastStaged(999), "k999 không tồn tại");
        }

        [Test]
        public void SkillCastRelativePath_ZeroPadded3()
        {
            Assert.AreEqual("sound/skill/sound_k001.wav", SurvivorAudioBank.SkillCastRelativePath(1));
            Assert.AreEqual("sound/skill/sound_k045.wav", SurvivorAudioBank.SkillCastRelativePath(45));
        }

        [Test]
        public void IsBgmTrackStaged_ChuaStaged_ImLang()
        {
            // BGM own chưa có file → false → không crash.
            Assert.IsFalse(SurvivorAudioBank.IsBgmTrackStaged(SurvivorAudioContext.Battle));
            Assert.IsFalse(SurvivorAudioBank.IsBgmTrackStaged(SurvivorAudioContext.Boss));
        }

        // --- gate 4: volume routing math (linear → dB cho mixer SetFloat) ---
        [Test]
        public void ToDb_FullVolume_ZeroDb()
        {
            Assert.AreEqual(0f, SurvivorAudioBank.ToDb(1f), 1e-4f);
        }

        [Test]
        public void ToDb_HalfVolume_Minus6Db()
        {
            Assert.AreEqual(-6.0206f, SurvivorAudioBank.ToDb(0.5f), 1e-3f);
        }

        [Test]
        public void ToDb_Zero_Minus80Floor()
        {
            Assert.AreEqual(-80f, SurvivorAudioBank.ToDb(0f), 1e-4f);
            Assert.AreEqual(-80f, SurvivorAudioBank.ToDb(0.00001f), 1e-4f);
        }

        // --- gate 5: mixer param names (SurvivorAudioMgr.SetVolume ↔ generator) ---
        [Test]
        public void MixerParams_ExposedNames_KhongTrung()
        {
            Assert.AreEqual("masterVol", SurvivorAudioBank.MixerParamMaster);
            Assert.AreEqual("bgmVol", SurvivorAudioBank.MixerParamBgm);
            Assert.AreEqual("sfxVol", SurvivorAudioBank.MixerParamSfx);
            Assert.AreNotEqual(SurvivorAudioBank.MixerParamMaster, SurvivorAudioBank.MixerParamBgm);
            Assert.AreNotEqual(SurvivorAudioBank.MixerParamBgm, SurvivorAudioBank.MixerParamSfx);
        }
    }
}
