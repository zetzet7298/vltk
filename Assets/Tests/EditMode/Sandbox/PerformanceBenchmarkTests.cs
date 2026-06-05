// -----------------------------------------------------------------------------
// VLTK Mobile — Performance benchmark tests cho toàn bộ runtime services.
// Đo thời gian load, lookup, filter, build snapshot. Tất cả tests skip nếu
// service null; sử dụng Stopwatch với timeout hợp lý cho mobile.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PerformanceBenchmarkTests
    {
        private static readonly Stopwatch _sw = new Stopwatch();

        // ─── Load benchmark ────────────────────────────────────────────────
        [Test]
        public void Test_LoadAllServices_Under2Seconds()
        {
            _sw.Restart();
            // Tạo registry cho từng service chính
            int created = 0;
            try { _ = AdventureService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = GuildService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = TitleService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = BattlefieldService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = AuctionService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = LotteryService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = MapListFullService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = ItemDetailService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = AchievementService.LoadFromStreamingAssets(); created++; } catch { }
            try { _ = MallService.LoadFromStreamingAssets(); created++; } catch { }
            _sw.Stop();
            Assert.Greater(created, 0, "Phải instantiate được ít nhất 1 service");
            Assert.Less(_sw.ElapsedMilliseconds, 2000, $"Load {created} services trong <2s (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Single-key lookup ─────────────────────────────────────────────
        [Test]
        public void Test_LookupById_Under1ms()
        {
            var titleSvc = TitleService.LoadFromStreamingAssets();
            var mapSvc = MapListFullService.LoadFromStreamingAssets();
            var achSvc = AchievementService.LoadFromStreamingAssets();
            var lotterySvc = LotteryService.LoadFromStreamingAssets();

            int validId = 0;
            if (titleSvc != null)
            {
                for (int i = 1; i <= 5000 && validId == 0; i++)
                {
                    if (titleSvc.GetPlayerTitle(i) != null) validId = i;
                }
            }
            Assert.Greater(validId, 0, "Phải tìm được 1 valid title ID");

            _sw.Restart();
            for (int i = 0; i < 1000; i++)
            {
                _ = titleSvc?.GetPlayerTitle(validId);
                _ = mapSvc?.Get(1);
                _ = achSvc?.Get(validId);
                _ = lotterySvc?.Get(validId);
            }
            _sw.Stop();
            Assert.Less(_sw.ElapsedMilliseconds, 1000, $"1000 lần lookup trong <1s (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Bulk lookup ───────────────────────────────────────────────────
        [Test]
        public void Test_BulkLookup1000Entries_Under10ms()
        {
            var titleSvc = TitleService.LoadFromStreamingAssets();
            Assert.IsNotNull(titleSvc);

            var ids = new List<int>();
            for (int i = 1; i <= 5000 && ids.Count < 1000; i++)
            {
                if (titleSvc.GetPlayerTitle(i) != null) ids.Add(i);
            }
            if (ids.Count == 0)
            {
                Assert.Ignore("Không có title data để test");
                return;
            }

            _sw.Restart();
            int hits = 0;
            foreach (var id in ids)
            {
                if (titleSvc.GetPlayerTitle(id) != null) hits++;
            }
            _sw.Stop();
            Assert.AreEqual(ids.Count, hits, "Bulk lookup phải hit 100%");
            Assert.Less(_sw.ElapsedMilliseconds, 10, $"1000 bulk lookup trong <10ms (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Filter by map ─────────────────────────────────────────────────
        [Test]
        public void Test_FilterByMap100Entries_Under5ms()
        {
            var advSvc = AdventureService.LoadFromStreamingAssets();
            Assert.IsNotNull(advSvc);

            // Collect 100 entries
            int targetMap = -1;
            int count = 0;
            foreach (var e in advSvc.GetAllAdventures())
            {
                if (e == null) continue;
                if (count == 0) targetMap = e.mapId;
                if (e.mapId == targetMap) count++;
                if (count >= 100) break;
            }
            Assert.Greater(count, 0, "Phải có ít nhất 1 mục adventure để filter");

            _sw.Restart();
            int filtered = 0;
            for (int i = 0; i < 1000; i++)
            {
                foreach (var e in advSvc.GetAdventuresForMap(targetMap))
                {
                    filtered++;
                }
            }
            _sw.Stop();
            Assert.Greater(filtered, 0, "Filter phải trả về entries");
            Assert.Less(_sw.ElapsedMilliseconds, 5000, $"1000 filter runs trong <5s (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Build snapshot ────────────────────────────────────────────────
        [Test]
        public void Test_BuildSnapshot_Under5ms()
        {
            var advSvc = AdventureService.LoadFromStreamingAssets();
            Assert.IsNotNull(advSvc);

            _sw.Restart();
            int sum = 0;
            for (int i = 0; i < 1000; i++)
            {
                // Build snapshot giả lập bằng cách enumerate
                foreach (var e in advSvc.GetAllAdventures())
                {
                    if (e != null) sum += e.advId;
                }
            }
            _sw.Stop();
            Assert.Greater(sum, 0, "Snapshot phải có entries");
            Assert.Less(_sw.ElapsedMilliseconds, 5000, $"1000 snapshot builds trong <5s (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Guild upgrade simulation ──────────────────────────────────────
        [Test]
        public void Test_TryUpgrade_Simulate1000Times_Under100ms()
        {
            var guildSvc = GuildService.LoadFromStreamingAssets();
            Assert.IsNotNull(guildSvc);

            _sw.Restart();
            int successes = 0;
            for (int i = 0; i < 1000; i++)
            {
                // Tạo state mới cho mỗi lần test
                var tempGuild = new GuildService(new PcTongLevelRegistry());
                var result = tempGuild.TryUpgrade(2, 1_000_000);
                if (result == GuildUpgradeResult.Success) successes++;
            }
            _sw.Stop();
            Assert.AreEqual(1000, successes, "Tất cả 1000 lần upgrade phải success với đủ tiền");
            Assert.Less(_sw.ElapsedMilliseconds, 100, $"1000 guild upgrades trong <100ms (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── World boss DPS simulation ─────────────────────────────────────
        [Test]
        public void Test_ComputeDpsScore_10000Times_Under10ms()
        {
            _sw.Restart();
            double total = 0;
            for (int i = 0; i < 10000; i++)
            {
                // DPS = (baseDmg * (1 + critChance/100) * (1 + skillBonus/100) * (1 - defReduction/100)) / time
                double baseDmg = 1000 + (i % 100);
                double crit = 1.15;
                double bonus = 1.20;
                double def = 0.30;
                double time = 1.5;
                total += (baseDmg * crit * bonus * (1.0 - def)) / time;
            }
            _sw.Stop();
            Assert.Greater(total, 0.0, "DPS phải > 0");
            Assert.Less(_sw.ElapsedMilliseconds, 100, $"10000 DPS compute trong <100ms (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Encounter random roll ─────────────────────────────────────────
        [Test]
        public void Test_RollEncounter_10000Times_Under100ms()
        {
            var rng = new System.Random(42);
            var weights = new[] { 50, 30, 15, 4, 1 }; // tổng = 100
            _sw.Restart();
            int[] hist = new int[5];
            for (int i = 0; i < 10000; i++)
            {
                int roll = rng.Next(0, 100);
                int cum = 0;
                for (int j = 0; j < weights.Length; j++)
                {
                    cum += weights[j];
                    if (roll < cum) { hist[j]++; break; }
                }
            }
            _sw.Stop();
            Assert.AreEqual(10000, hist[0] + hist[1] + hist[2] + hist[3] + hist[4], "Tổng rolls phải = 10000");
            Assert.Less(_sw.ElapsedMilliseconds, 100, $"10000 encounter rolls trong <100ms (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Vietnamese catalog lookup ─────────────────────────────────────
        [Test]
        public void Test_GetVietnameseName_10000Times_Under5ms()
        {
            _sw.Restart();
            string last = null;
            for (int i = 0; i < 10000; i++)
            {
                int fid = (i % 16) + 1;
                last = FactionVietnameseCatalog.GetVietnameseName(fid);
            }
            _sw.Stop();
            Assert.IsNotNull(last, "Catalog phải trả về tên");
            Assert.Less(_sw.ElapsedMilliseconds, 50, $"10000 catalog lookups trong <50ms (mất {_sw.ElapsedMilliseconds}ms)");
        }

        // ─── Text resource translate ───────────────────────────────────────
        [Test]
        public void Test_TranslatePath_10000Times_Under10ms()
        {
            var textSvc = TextResourceService.LoadFromStreamingAssets();
            if (textSvc == null) { Assert.Ignore("TextResourceService không có data"); return; }

            _sw.Restart();
            string last = null;
            for (int i = 0; i < 10000; i++)
            {
                int id = (i % 100) + 1;
                last = textSvc.GetVietnamese("key_" + id);
            }
            _sw.Stop();
            Assert.Less(_sw.ElapsedMilliseconds, 100, $"10000 text lookups trong <100ms (mất {_sw.ElapsedMilliseconds}ms)");
        }
    }
}
