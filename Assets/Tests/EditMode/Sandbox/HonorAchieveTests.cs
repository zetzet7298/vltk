// -----------------------------------------------------------------------------
// VLTK Mobile — HonorService EditMode tests.
// Kiểm tra vinh danh lifecycle: registry attach, CanAchieve, AddPoints with
// auto-achieve, AchieveHonor dispatch chain (title + aura + SFX + log +
// save), query APIs.
// PC source: settings/honor.txt + lua honor_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class HonorAchieveTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IHonorHost
        {
            public int TitleCalls;
            public int AuraCalls;
            public int NoticeCalls;
            public int AchievedCalls;
            public int SfxCalls;
            public int LogCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastHonorId;
            public int LastTitleId;
            public int LastAuraSkillId;
            public int LastRequiredPoints;
            public bool LastAchievedFlag;

            public void GrantTitle(int playerId, int honorId, int titleId)
            {
                TitleCalls++;
                LastTitleId = titleId;
            }
            public void ActivateAura(int playerId, int honorId, int auraSkillId)
            {
                AuraCalls++;
                LastAuraSkillId = auraSkillId;
            }
            public void ShowHonorNotice(int playerId, int honorId, string honorName) { NoticeCalls++; }
            public void OnHonorAchieved(int playerId, int honorId, string honorName, int points)
            {
                AchievedCalls++;
                LastRequiredPoints = points;
            }
            public void PlayHonorSFX(int playerId, int honorId) { SfxCalls++; }
            public void LogHonorEvent(int playerId, int honorId, string message) { LogCalls++; }
            public void SaveHonorProgress(int playerId, int honorId, int points, bool achieved)
            {
                SaveCalls++;
                LastAchievedFlag = achieved;
            }
        }

        private static PcHonorRegistry BuildRegistry(params (int id, string name, int points, int title, int aura)[] rows)
        {
            var reg = new PcHonorRegistry();
            foreach (var r in rows)
            {
                reg.Register(new PcHonorEntry
                {
                    honorId = r.id,
                    honorName = r.name,
                    requiredPoints = r.points,
                    titleReward = r.title,
                    auraSkillId = r.aura,
                });
            }
            return reg;
        }

        // ── Registry attach + count ────────────────────────────────────────

        [Test]
        public void Count_AfterRegistry_ReturnsEntryCount()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 10, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(1, svc.Count);
        }

        [Test]
        public void Count_EmptyService_ReturnsZero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void AttachRegistry_FiresOnHonorLoadedEvent()
        {
            var svc = new HonorService();
            int fired = 0;
            svc.OnHonorLoaded += () => fired++;
            svc.AttachRegistry(BuildRegistry((1, "X", 100, 0, 0)));
            Assert.AreEqual(1, fired);
        }

        // ── Lookup APIs ─────────────────────────────────────────────────────

        [Test]
        public void GetHonor_NotFound_ReturnsNull()
        {
            var svc = new HonorService();
            Assert.IsNull(svc.GetHonor(99));
        }

        [Test]
        public void GetHonor_Exists_ReturnsEntry()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 10, 0));
            var svc = new HonorService(reg);
            var h = svc.GetHonor(1);
            Assert.IsNotNull(h);
            Assert.AreEqual("Đồng", h.honorName);
        }

        [Test]
        public void GetByPoints_NotFound_ReturnsNull()
        {
            var svc = new HonorService();
            Assert.IsNull(svc.GetByPoints(999));
        }

        [Test]
        public void GetByPoints_Found_ReturnsEntry()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0), (2, "Bạc", 200, 0, 0));
            var svc = new HonorService(reg);
            var h = svc.GetByPoints(150);
            Assert.IsNotNull(h);
        }

        [Test]
        public void GetAll_NoRegistry_ReturnsEmpty()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, Count(svc.GetAll()));
        }

        [Test]
        public void GetAll_AfterRegistry()
        {
            var reg = BuildRegistry((1, "X", 100, 0, 0), (2, "Y", 200, 0, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(2, Count(svc.GetAll()));
        }

        // ── CanAchieve ──────────────────────────────────────────────────────

        [Test]
        public void CanAchieve_NotFound_ReturnsFalse()
        {
            var svc = new HonorService();
            Assert.IsFalse(svc.CanAchieve(99, 1000));
        }

        [Test]
        public void CanAchieve_InsufficientPoints_ReturnsFalse()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.IsFalse(svc.CanAchieve(1, 50));
        }

        [Test]
        public void CanAchieve_ExactPoints_ReturnsTrue()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.IsTrue(svc.CanAchieve(1, 100));
        }

        [Test]
        public void CanAchieve_ExceedPoints_ReturnsTrue()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.IsTrue(svc.CanAchieve(1, 500));
        }

        // ── AddPoints ───────────────────────────────────────────────────────

        [Test]
        public void AddPoints_NoRegistry_ReturnsZero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.AddPoints(1, 100));
        }

        [Test]
        public void AddPoints_BelowThreshold_NoAchieve()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(0, svc.AddPoints(1, 50));
            Assert.AreEqual(50, svc.GetPlayerPoints(1));
        }

        [Test]
        public void AddPoints_AtThreshold_Achieves()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(1, svc.AddPoints(1, 100));
            Assert.IsTrue(svc.HasAchieved(1, 1));
        }

        [Test]
        public void AddPoints_ExceedThreshold_Achieves()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(1, svc.AddPoints(1, 200));
        }

        [Test]
        public void AddPoints_AlreadyAchieved_NoReTrigger()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            svc.AddPoints(1, 100); // achieve first
            int secondCall = svc.AddPoints(1, 50); // +50 = 150, but already achieved
            Assert.AreEqual(0, secondCall);
        }

        [Test]
        public void AddPoints_MultipleTiers_AllAchieved()
        {
            var reg = BuildRegistry(
                (1, "Đồng", 100, 10, 100),
                (2, "Bạc", 200, 20, 200),
                (3, "Vàng", 500, 30, 300)
            );
            var svc = new HonorService(reg);
            int lastAchieved = svc.AddPoints(1, 500);
            Assert.AreEqual(3, lastAchieved); // highest tier
            Assert.IsTrue(svc.HasAchieved(1, 1));
            Assert.IsTrue(svc.HasAchieved(1, 2));
            Assert.IsTrue(svc.HasAchieved(1, 3));
        }

        [Test]
        public void AddPoints_MultiplePlayers_Independent()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            svc.AddPoints(1, 100);
            svc.AddPoints(2, 50);
            Assert.IsTrue(svc.HasAchieved(1, 1));
            Assert.IsFalse(svc.HasAchieved(2, 1));
            Assert.AreEqual(100, svc.GetPlayerPoints(1));
            Assert.AreEqual(50, svc.GetPlayerPoints(2));
        }

        [Test]
        public void AddPoints_AccumulatesAcross()
        {
            var reg = BuildRegistry((1, "Đồng", 100, 0, 0));
            var svc = new HonorService(reg);
            svc.AddPoints(1, 30);
            svc.AddPoints(1, 40);
            Assert.AreEqual(70, svc.GetPlayerPoints(1));
            Assert.IsFalse(svc.HasAchieved(1, 1));
            svc.AddPoints(1, 30); // 70+30=100
            Assert.IsTrue(svc.HasAchieved(1, 1));
        }

        // ── AchieveHonor ────────────────────────────────────────────────────

        [Test]
        public void AchieveHonor_NotFound_ReturnsFalse()
        {
            var svc = new HonorService();
            Assert.IsFalse(svc.AchieveHonor(1, 99));
        }

        [Test]
        public void AchieveHonor_AlreadyAchieved_ReturnsFalse()
        {
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            Assert.IsFalse(svc.AchieveHonor(1, 1)); // already
        }

        [Test]
        public void AchieveHonor_DispatchesTitleReward()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, "Đồng", 100, 50, 0));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, host.TitleCalls);
            Assert.AreEqual(50, host.LastTitleId);
        }

        [Test]
        public void AchieveHonor_DispatchesAuraSkill()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, "Đồng", 100, 0, 500));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, host.AuraCalls);
            Assert.AreEqual(500, host.LastAuraSkillId);
        }

        [Test]
        public void AchieveHonor_DispatchesAllCallbacks()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, "Đồng", 100, 50, 500));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, host.TitleCalls);
            Assert.AreEqual(1, host.AuraCalls);
            Assert.AreEqual(1, host.NoticeCalls);
            Assert.AreEqual(1, host.AchievedCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void AchieveHonor_NoTitleOrAura_OnlyMiscDispatch()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(0, host.TitleCalls);
            Assert.AreEqual(0, host.AuraCalls);
            Assert.AreEqual(1, host.NoticeCalls);
            Assert.AreEqual(1, host.AchievedCalls);
        }

        [Test]
        public void AchieveHonor_FiresOnPlayerHonorAchievedEvent()
        {
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg);
            int fired = 0;
            int lastH = 0;
            svc.OnPlayerHonorAchieved += (p, h) => { fired++; lastH = h; };
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(1, lastH);
        }

        [Test]
        public void AchieveHonor_WithoutHost_DoesNotThrow()
        {
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg);
            Assert.DoesNotThrow(() => svc.AchieveHonor(1, 1));
        }

        [Test]
        public void AchieveHonor_SaveWithAchievedTrue()
        {
            var host = new FakeHost();
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.IsTrue(host.LastAchievedFlag);
        }

        // ── Query APIs ──────────────────────────────────────────────────────

        [Test]
        public void HasAchieved_NoEntry_ReturnsFalse()
        {
            var svc = new HonorService();
            Assert.IsFalse(svc.HasAchieved(99, 1));
        }

        [Test]
        public void GetPlayerPoints_NoEntry_ReturnsZero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.GetPlayerPoints(99));
        }

        [Test]
        public void GetAchievedCount_NoEntry_ReturnsZero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.GetAchievedCount(99));
        }

        [Test]
        public void GetAchievedCount_AfterAchieves()
        {
            var reg = BuildRegistry((1, "X", 100, 0, 0), (2, "Y", 200, 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            svc.AchieveHonor(1, 2);
            Assert.AreEqual(2, svc.GetAchievedCount(1));
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_ReplacesHost()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var reg = BuildRegistry((1, "X", 100, 0, 0));
            var svc = new HonorService(reg, host1);
            svc.AttachHost(host2);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(0, host1.SfxCalls);
            Assert.AreEqual(1, host2.SfxCalls);
        }

        // ── Helper ──────────────────────────────────────────────────────────

        private static int Count<T>(System.Collections.Generic.IEnumerable<T> e)
        {
            int n = 0;
            foreach (var _ in e) n++;
            return n;
        }
    }
}
