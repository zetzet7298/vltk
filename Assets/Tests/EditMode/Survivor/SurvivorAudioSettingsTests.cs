// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorAudioSettingsTests
// Ticket 40 gate (audio part): settings panel pure-logic controller.
// Default → save → load roundtrip, clamp 0..1, apply volume qua IAudioVolumeSink
// (fake mixer), lang toggle persist + switch runtime, fail-closed thiếu dep.
// Pure-logic: MemoryStorage + FakeSink — KHÔNG PlayerPrefs thật, KHÔNG scene
// (spec Testing Decisions: seam duy nhất EditMode).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorAudioSettingsTests
    {
        /// <summary>In-memory ISaveStorage (pattern SurvivorSaveTests — tránh PlayerPrefs dirty).</summary>
        private sealed class MemoryStorage : ISaveStorage
        {
            private readonly Dictionary<string, string> _m = new();

            public string GetString(string key, string defaultValue)
                => _m.TryGetValue(key, out var v) ? v : defaultValue;

            public void SetString(string key, string value) => _m[key] = value;

            public void DeleteKey(string key) => _m.Remove(key);
        }

        /// <summary>Fake sink ghi nhận volume → assert mixer receive (external behavior).</summary>
        private sealed class FakeSink : IAudioVolumeSink
        {
            public readonly Dictionary<SurvivorAudioBus, float> Volumes = new();

            public float GetVolume(SurvivorAudioBus bus)
                => Volumes.TryGetValue(bus, out var v) ? v : 1f;

            public void SetVolume(SurvivorAudioBus bus, float volume) => Volumes[bus] = volume;
        }

        /// <summary>Controller với storage + sink mới (cùng MemoryStorage = persist layer chung).</summary>
        private static SurvivorAudioSettingsController MakeController(MemoryStorage storage,
            FakeSink sink, SurvivorText text = null)
        {
            return new SurvivorAudioSettingsController(
                new SurvivorSaveService(storage), sink, storage, text);
        }

        // --- gate: load defaults + apply ---

        [Test]
        public void Load_NoSaveYet_AppliesDefaults_AndLangVi()
        {
            var storage = new MemoryStorage();
            var sink = new FakeSink();
            var c = MakeController(storage, sink);
            c.Load();

            Assert.AreEqual(1f, c.MasterVolume, 1e-6f);
            Assert.AreEqual(0.6f, c.BgmVolume, 1e-6f);
            Assert.AreEqual(0.8f, c.SfxVolume, 1e-6f);
            Assert.AreEqual("vi", c.Language);
            Assert.AreEqual(1f, sink.GetVolume(SurvivorAudioBus.Master), 1e-6f, "apply master 1");
            Assert.AreEqual(0.6f, sink.GetVolume(SurvivorAudioBus.Bgm), 1e-6f, "apply bgm 0.6");
            Assert.AreEqual(0.8f, sink.GetVolume(SurvivorAudioBus.Sfx), 1e-6f, "apply sfx 0.8");
        }

        // --- gate: save → load roundtrip ---

        [Test]
        public void SetSaveReload_KeepsAllValues_AndReappliesToSink()
        {
            var storage = new MemoryStorage();
            var sink = new FakeSink();
            var c = MakeController(storage, sink);
            c.Load();
            c.SetMasterVolume(0.5f);
            c.SetBgmVolume(0.35f);
            c.SetSfxVolume(0.9f);
            c.SetLanguage("en");
            c.Save();

            // Reload bằng service mới, cùng storage (tựa app restart).
            var sink2 = new FakeSink();
            var c2 = MakeController(storage, sink2);
            c2.Load();

            Assert.AreEqual(0.5f, c2.MasterVolume, 1e-6f, "master roundtrip");
            Assert.AreEqual(0.35f, c2.BgmVolume, 1e-6f, "bgm roundtrip");
            Assert.AreEqual(0.9f, c2.SfxVolume, 1e-6f, "sfx roundtrip");
            Assert.AreEqual("en", c2.Language, "lang roundtrip");
            Assert.AreEqual(0.5f, sink2.GetVolume(SurvivorAudioBus.Master), 1e-6f, "reload áp master");
            Assert.AreEqual(0.35f, sink2.GetVolume(SurvivorAudioBus.Bgm), 1e-6f, "reload áp bgm");
            Assert.AreEqual(0.9f, sink2.GetVolume(SurvivorAudioBus.Sfx), 1e-6f, "reload áp sfx");
        }

        // --- gate: clamp 0..1 ---

        [Test]
        public void SetVolume_ClampsTo01()
        {
            var storage = new MemoryStorage();
            var sink = new FakeSink();
            var c = MakeController(storage, sink);
            c.Load();

            c.SetBgmVolume(1.7f);
            c.SetSfxVolume(-0.3f);
            c.SetMasterVolume(2f);

            Assert.AreEqual(1f, c.BgmVolume, 1e-6f);
            Assert.AreEqual(0f, c.SfxVolume, 1e-6f);
            Assert.AreEqual(1f, c.MasterVolume, 1e-6f);
            Assert.AreEqual(1f, sink.GetVolume(SurvivorAudioBus.Bgm), 1e-6f, "sink nhận giá trị clamp");
            Assert.AreEqual(0f, sink.GetVolume(SurvivorAudioBus.Sfx), 1e-6f);
        }

        // --- gate: apply ngay (không chờ Save) ---

        [Test]
        public void SetVolume_AppliesToSinkImmediately()
        {
            var storage = new MemoryStorage();
            var sink = new FakeSink();
            var c = MakeController(storage, sink);
            c.Load();

            c.SetBgmVolume(0.4f);
            Assert.AreEqual(0.4f, sink.GetVolume(SurvivorAudioBus.Bgm), 1e-6f, "apply bgm ngay khi set");

            c.SetSfxVolume(0.25f);
            Assert.AreEqual(0.25f, sink.GetVolume(SurvivorAudioBus.Sfx), 1e-6f, "apply sfx ngay khi set");
        }

        // --- gate: lang toggle persist + switch runtime ---

        [Test]
        public void LangToggle_Persists_AndSwitchesTextRuntime()
        {
            var storage = new MemoryStorage();
            var text = new SurvivorText();
            var c = MakeController(storage, new FakeSink(), text);
            c.Load();

            c.SetLanguage("en");
            Assert.AreEqual("en", c.Language);
            Assert.AreEqual("en", text.Language, "SetLanguage switch SurvivorText ngay");
            c.Save();

            // Reload: text mới phải nhận lang đã persist.
            var text2 = new SurvivorText();
            var c2 = MakeController(storage, new FakeSink(), text2);
            c2.Load();

            Assert.AreEqual("en", c2.Language, "lang persist sau reload");
            Assert.AreEqual("en", text2.Language, "reload áp lang vào text mới");
        }

        [Test]
        public void SetLanguage_InvalidIgnored()
        {
            var c = MakeController(new MemoryStorage(), new FakeSink());
            c.Load();
            c.SetLanguage("xx");
            Assert.AreEqual("vi", c.Language, "ngôn ngữ ngoài 'vi'/'en' bị bỏ qua");
        }

        // --- gate: fail-closed ---

        [Test]
        public void MissingDeps_FailClosed_NoThrow()
        {
            var c = new SurvivorAudioSettingsController(null, null, null);
            c.Load();      // thiếu service + sink + storage
            c.SetMasterVolume(0.4f);
            c.SetBgmVolume(0.3f);
            c.SetSfxVolume(0.2f);
            c.SetLanguage("en");
            c.Save();
            Assert.Pass("fail-closed: mọi op no-op, không exception");
        }

        [Test]
        public void MissingSaveService_MasterStillPersists()
        {
            var storage = new MemoryStorage();
            var c = new SurvivorAudioSettingsController(null, null, storage);
            c.Load();
            c.SetMasterVolume(0.42f);
            c.Save();

            Assert.AreEqual("0.42", storage.GetString(SurvivorAudioSettingsController.MasterVolumeKey, null),
                "master lưu key riêng dù thiếu save service");
        }
    }
}