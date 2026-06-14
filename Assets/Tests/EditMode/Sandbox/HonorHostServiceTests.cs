// -----------------------------------------------------------------------------
// VLTK Mobile — HonorService EditMode tests.
// Kiểm tra vinh danh runtime: add points, achieve honor, host dispatch chain.
// PC source: settings/honor.txt + lua honor_event.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class HonorHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IHonorHost
        {
            public int GrantTitleCalls;
            public int ActivateAuraCalls;
            public int ShowNoticeCalls;
            public int AchievedCalls;
            public int SfxCalls;
            public int LogCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastHonorId;
            public int LastTitleId;
            public int LastAuraSkillId;
            public string LastHonorName;
            public int LastPoints;
            public string LastMessage;
            public bool LastAchieved;

            public void GrantTitle(int playerId, int honorId, int titleId)
            {
                GrantTitleCalls++;
                LastPlayerId = playerId;
                LastHonorId = honorId;
                LastTitleId = titleId;
            }
            public void ActivateAura(int playerId, int honorId, int auraSkillId)
            {
                ActivateAuraCalls++;
                LastAuraSkillId = auraSkillId;
            }
            public void ShowHonorNotice(int playerId, int honorId, string honorName)
            {
                ShowNoticeCalls++;
                LastHonorName = honorName;
            }
            public void OnHonorAchieved(int playerId, int honorId, string honorName, int points)
            {
                AchievedCalls++;
                LastPoints = points;
            }
            public void PlayHonorSFX(int playerId, int honorId) { SfxCalls++; }
            public void LogHonorEvent(int playerId, int honorId, string message)
            {
                LogCalls++;
                LastMessage = message;
            }
            public void SaveHonorProgress(int playerId, int honorId, int points, bool achieved)
            {
                SaveCalls++;
                LastAchieved = achieved;
            }
        }

        private static PcHonorRegistry MakeRegistry(params (int id, int required, string name, int title, int aura)[] entries)
        {
            var reg = new PcHonorRegistry();
            foreach (var e in entries)
            {
                reg.Register(new PcHonorEntry
                {
                    honorId = e.id,
                    requiredPoints = e.required,
                    honorName = e.name,
                    titleReward = e.title,
                    auraSkillId = e.aura,
                });
            }
            return reg;
        }

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Constructor_WithRegistry()
        {
            var reg = MakeRegistry((1, 100, "Test", 0, 0));
            var svc = new HonorService(reg);
            Assert.AreEqual(1, svc.Count);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var reg = MakeRegistry((1, 100, "Test", 0, 0));
            var svc = new HonorService(reg, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var reg = MakeRegistry((1, 100, "Test", 11, 0)); // titleReward=11 so GrantTitle fires
            var svc = new HonorService(reg);
            svc.AttachHost(host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, host.GrantTitleCalls);
        }

        [Test]
        public void AttachRegistry_Updates()
        {
            var svc = new HonorService();
            var reg = MakeRegistry((1, 100, "T1", 0, 0), (2, 200, "T2", 0, 0));
            svc.AttachRegistry(reg);
            Assert.AreEqual(2, svc.Count);
        }

        [Test]
        public void AttachRegistry_NullFallsBackToEmpty()
        {
            var svc = new HonorService(MakeRegistry((1, 100, "T1", 0, 0)));
            svc.AttachRegistry(null);
            Assert.AreEqual(0, svc.Count);
        }

        // ── GetHonor / GetByPoints / CanAchieve ─────────────────────────────

        [Test]
        public void GetHonor_Exists()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsNotNull(svc.GetHonor(1));
        }

        [Test]
        public void GetHonor_NotFound_ReturnsNull()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsNull(svc.GetHonor(999));
        }

        [Test]
        public void GetByPoints_Exists()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsNotNull(svc.GetByPoints(100));
        }

        [Test]
        public void GetByPoints_NotFound_ReturnsNull()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            // points=0 → no honor has requiredPoints <= 0 → null
            Assert.IsNull(svc.GetByPoints(0));
        }

        [Test]
        public void CanAchieve_True()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsTrue(svc.CanAchieve(1, 150));
        }

        [Test]
        public void CanAchieve_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsFalse(svc.CanAchieve(1, 50));
        }

        [Test]
        public void CanAchieve_NotFound_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsFalse(svc.CanAchieve(999, 1000));
        }

        // ── AddPoints ────────────────────────────────────────────────────────

        [Test]
        public void AddPoints_Accumulates()
        {
            var svc = new HonorService(MakeRegistry((1, 100, "T1", 0, 0)));
            svc.AddPoints(1, 30);
            svc.AddPoints(1, 20);
            Assert.AreEqual(50, svc.GetPlayerPoints(1));
        }

        [Test]
        public void AddPoints_AchievesHonorWhenReached()
        {
            var svc = new HonorService(MakeRegistry((1, 100, "T1", 0, 0)));
            int achieved = svc.AddPoints(1, 150);
            Assert.AreEqual(1, achieved);
        }

        [Test]
        public void AddPoints_NoAchieveWhenBelowThreshold()
        {
            var svc = new HonorService(MakeRegistry((1, 100, "T1", 0, 0)));
            int achieved = svc.AddPoints(1, 50);
            Assert.AreEqual(0, achieved);
        }

        [Test]
        public void AddPoints_NoAchieveWhenAlreadyAchieved()
        {
            var svc = new HonorService(MakeRegistry((1, 100, "T1", 0, 0)));
            svc.AddPoints(1, 150);
            int achieved = svc.AddPoints(1, 10);
            Assert.AreEqual(0, achieved);
        }

        [Test]
        public void AddPoints_MultipleHonors()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0), (2, 200, "T2", 0, 0), (3, 300, "T3", 0, 0));
            var svc = new HonorService(reg);
            int achieved = svc.AddPoints(1, 250);
            Assert.AreEqual(2, achieved); // 1st and 2nd honors achieved
        }

        // ── AchieveHonor ────────────────────────────────────────────────────

        [Test]
        public void AchieveHonor_Success()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsTrue(svc.AchieveHonor(1, 1));
        }

        [Test]
        public void AchieveHonor_NotFound_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsFalse(svc.AchieveHonor(1, 999));
        }

        [Test]
        public void AchieveHonor_AlreadyAchieved_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            Assert.IsFalse(svc.AchieveHonor(1, 1));
        }

        [Test]
        public void AchieveHonor_FiresOnPlayerHonorAchieved()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            int fired = 0;
            svc.OnPlayerHonorAchieved += (p, h) => fired++;
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void AchieveHonor_DispatchesHost()
        {
            var host = new FakeHost();
            var reg = MakeRegistry((1, 100, "T1", 11, 22));
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(1, host.GrantTitleCalls);
            Assert.AreEqual(11, host.LastTitleId);
            Assert.AreEqual(1, host.ActivateAuraCalls);
            Assert.AreEqual(22, host.LastAuraSkillId);
            Assert.AreEqual(1, host.ShowNoticeCalls);
            Assert.AreEqual(1, host.AchievedCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
            Assert.IsTrue(host.LastAchieved);
        }

        [Test]
        public void AchieveHonor_NoTitleReward_Skipped()
        {
            var host = new FakeHost();
            var reg = MakeRegistry((1, 100, "T1", 0, 0)); // no title
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(0, host.GrantTitleCalls);
        }

        [Test]
        public void AchieveHonor_NoAuraSkill_Skipped()
        {
            var host = new FakeHost();
            var reg = MakeRegistry((1, 100, "T1", 0, 0)); // no aura
            var svc = new HonorService(reg, host);
            svc.AchieveHonor(1, 1);
            Assert.AreEqual(0, host.ActivateAuraCalls);
        }

        // ── HasAchieved / GetPlayerPoints / GetAchievedCount ────────────────

        [Test]
        public void HasAchieved_True()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            Assert.IsTrue(svc.HasAchieved(1, 1));
        }

        [Test]
        public void HasAchieved_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.IsFalse(svc.HasAchieved(1, 1));
        }

        [Test]
        public void HasAchieved_DifferentPlayer_False()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            Assert.IsFalse(svc.HasAchieved(2, 1));
        }

        [Test]
        public void GetPlayerPoints_DefaultZero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.GetPlayerPoints(999));
        }

        [Test]
        public void GetAchievedCount_AfterAchieve()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0), (2, 200, "T2", 0, 0));
            var svc = new HonorService(reg);
            svc.AchieveHonor(1, 1);
            svc.AchieveHonor(1, 2);
            Assert.AreEqual(2, svc.GetAchievedCount(1));
        }

        [Test]
        public void GetAchievedCount_NoPlayer_Zero()
        {
            var svc = new HonorService();
            Assert.AreEqual(0, svc.GetAchievedCount(999));
        }

        // ── GetAll ──────────────────────────────────────────────────────────

        [Test]
        public void GetAll_Empty()
        {
            var svc = new HonorService();
            int n = 0;
            foreach (var _ in svc.GetAll()) n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void GetAll_Populated()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0), (2, 200, "T2", 0, 0));
            var svc = new HonorService(reg);
            int n = 0;
            foreach (var _ in svc.GetAll()) n++;
            Assert.AreEqual(2, n);
        }

        // ── OnHonorLoaded event ─────────────────────────────────────────────

        [Test]
        public void AttachRegistry_FiresOnHonorLoaded()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService();
            int fired = 0;
            svc.OnHonorLoaded += () => fired++;
            svc.AttachRegistry(reg);
            Assert.AreEqual(1, fired);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void HonorService_WithoutHost_DoesNotThrow()
        {
            var reg = MakeRegistry((1, 100, "T1", 0, 0));
            var svc = new HonorService(reg);
            Assert.DoesNotThrow(() => svc.AchieveHonor(1, 1));
            Assert.DoesNotThrow(() => svc.AddPoints(1, 50));
        }
    }
}
