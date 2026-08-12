// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSaveTests
// Ticket 39 gate: EditMode self-check xanh cho round-trip, migration, corrupt-recovery.
// Pure-logic qua ISaveStorage in-memory — KHÔNG đụng PlayerPrefs thật,
// KHÔNG scene, KHÔNG PlayMode (spec Testing Decisions).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSaveTests
    {
        /// <summary>In-memory ISaveStorage cho test (tránh PlayerPrefs dirty).</summary>
        private sealed class MemoryStorage : ISaveStorage
        {
            private readonly Dictionary<string, string> _m = new();

            public string GetString(string key, string defaultValue)
            {
                return _m.TryGetValue(key, out var v) ? v : defaultValue;
            }

            public void SetString(string key, string value) => _m[key] = value;

            public void DeleteKey(string key) => _m.Remove(key);

            public bool Has(string key) => _m.ContainsKey(key);
        }

        // --- gate: round-trip ---

        [Test]
        public void RoundTrip_Progress_KeepsAllFields()
        {
            var storage = new MemoryStorage();
            var save = new SurvivorSaveService(storage);
            var p = SurvivorProgressData.CreateDefault();
            p.bestFloor = 3;
            p.bestScore = 12345L;
            p.totalKills = 999L;
            p.runCount = 7;
            p.unlockedStageIds = new List<int> { 1, 2, 3 };
            p.metaUpgrades = new List<SurvivorMetaUpgrade> { new SurvivorMetaUpgrade { id = "damage", level = 2 } };

            save.SaveProgress(p);

            var load = new SurvivorSaveService(storage); // service mới, cùng storage
            var r = load.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.Loaded, outcome);
            Assert.AreEqual(3, r.bestFloor);
            Assert.AreEqual(12345L, r.bestScore);
            Assert.AreEqual(999L, r.totalKills);
            Assert.AreEqual(7, r.runCount);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, r.unlockedStageIds);
            Assert.AreEqual(1, r.metaUpgrades.Count);
            Assert.AreEqual("damage", r.metaUpgrades[0].id);
            Assert.AreEqual(2, r.metaUpgrades[0].level);
            Assert.AreEqual(SurvivorSaveService.ProgressVersion, r.version);
        }

        [Test]
        public void RoundTrip_Settings_KeepsAllFields()
        {
            var storage = new MemoryStorage();
            var save = new SurvivorSaveService(storage);
            var s = SurvivorSettingsData.CreateDefault();
            s.audioBgm = 0.35f;
            s.audioSfx = 0.9f;
            s.lang = "en";
            s.quality = 2;

            save.SaveSettings(s);

            var load = new SurvivorSaveService(storage);
            var r = load.LoadSettings(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.Loaded, outcome);
            Assert.AreEqual(0.35f, r.audioBgm, 1e-6f);
            Assert.AreEqual(0.9f, r.audioSfx, 1e-6f);
            Assert.AreEqual("en", r.lang);
            Assert.AreEqual(2, r.quality);
            Assert.AreEqual(SurvivorSaveService.SettingsVersion, r.version);
        }

        [Test]
        public void Load_NoSaveYet_ReturnsDefaults_Fresh()
        {
            var service = new SurvivorSaveService(new MemoryStorage());
            var p = service.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.Fresh, outcome);
            Assert.AreEqual(1, p.unlockedStageIds.Count, "default unlock stage 1");
            Assert.AreEqual(SurvivorSaveService.ProgressVersion, p.version);
        }

        // --- gate: version migration ---

        [Test]
        public void Migration_OldVersionData_RunsMigratorAndBumpsVersion()
        {
            var storage = new MemoryStorage();
            // Giả lập save cũ: version 0, bestScore dạng cũ, thiếu field mới.
            storage.SetString(SurvivorSaveService.ProgressKey, "{\"version\":0,\"bestScore\":50,\"runCount\":2}");

            int seenFrom = -1;
            var service = new SurvivorSaveService(storage);
            service.ProgressMigrator = (d, fromVersion) =>
            {
                seenFrom = fromVersion;
                d.bestScore = d.bestScore * 1000; // vd: v1 đổi đơn vị bestScore
                return d;
            };

            var r = service.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.Loaded, outcome);
            Assert.AreEqual(0, seenFrom, "migrator nhận fromVersion=0");
            Assert.AreEqual(50000L, r.bestScore, "field được migrate");
            Assert.AreEqual(2, r.runCount, "field cũ giữ nguyên");
            Assert.AreEqual(SurvivorSaveService.ProgressVersion, r.version, "bump lên version hiện tại");
        }

        [Test]
        public void Migration_NoopMigrator_DoesNotInfiniteLoop()
        {
            var storage = new MemoryStorage();
            storage.SetString(SurvivorSaveService.ProgressKey, "{\"version\":0}");

            var service = new SurvivorSaveService(storage); // migrator default no-op
            var r = service.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.Loaded, outcome);
            Assert.AreEqual(SurvivorSaveService.ProgressVersion, r.version, "guard chống stall vẫn đưa về version hiện tại");
        }

        // --- gate: corrupt-recovery ---

        [Test]
        public void Corrupt_MainKey_BadBackup_ResetsDefaults_AndKeepsBackupBytes()
        {
            var storage = new MemoryStorage();
            storage.SetString(SurvivorSaveService.ProgressKey, "{{{not-json");
            storage.SetString(SurvivorSaveService.ProgressKey + ".bak", "{{also-bad");

            var service = new SurvivorSaveService(storage);
            var r = service.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.ResetDefaults, outcome);
            Assert.AreEqual(0, r.bestScore, "reset về defaults");
            Assert.IsFalse(storage.Has(SurvivorSaveService.ProgressKey), "main key hỏng bị xóa");
            Assert.AreEqual("{{also-bad", storage.GetString(SurvivorSaveService.ProgressKey + ".bak", null),
                "backup bytes được GIỮ lại (không tự xóa)");
        }

        [Test]
        public void Corrupt_MainKey_GoodBackup_RecoversFromBackup()
        {
            var storage = new MemoryStorage();
            // Save thành công trước → backup last-known-good có data.
            var save = new SurvivorSaveService(storage);
            var p = SurvivorProgressData.CreateDefault();
            p.bestScore = 777L;
            p.totalKills = 42L;
            save.SaveProgress(p);

            // Crash giữa write → main key hỏng một nửa, backup còn nguyên.
            storage.SetString(SurvivorSaveService.ProgressKey, "{\"version\":1,\"bestScore\":7");

            var load = new SurvivorSaveService(storage);
            var r = load.LoadProgress(out var outcome);
            Assert.AreEqual(SurvivorLoadOutcome.RecoveredFromBackup, outcome);
            Assert.AreEqual(777L, r.bestScore, "phục hồi từ backup, không mất progress");
            Assert.AreEqual(42L, r.totalKills);
            Assert.AreEqual("{\"version\":1,\"bestScore\":7", storage.GetString(SurvivorSaveService.ProgressKey, null),
                "main key hỏng giữ nguyên làm evidence");
            Assert.AreEqual(SurvivorSaveService.ProgressVersion, r.version);
        }

        [Test]
        public void Save_WritesBackupKey_LastKnownGood()
        {
            var storage = new MemoryStorage();
            var service = new SurvivorSaveService(storage);
            service.SaveProgress(SurvivorProgressData.CreateDefault());

            Assert.IsTrue(storage.Has(SurvivorSaveService.ProgressKey), "main key");
            Assert.IsTrue(storage.Has(SurvivorSaveService.ProgressKey + ".bak"), "backup key");
            Assert.AreEqual(storage.GetString(SurvivorSaveService.ProgressKey, null),
                storage.GetString(SurvivorSaveService.ProgressKey + ".bak", null), "backup = last good JSON");
        }
    }
}
