// -----------------------------------------------------------------------------
// VLTK Mobile — PkCombatService EditMode tests.
// Kiểm tra chế độ PK (Peace/Free/Team/Faction/Bang), sát khí, red name,
// host dispatch chain.
// PC source: KNpc::IsEnemy, PK mode, RedName/Karma system.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PkCombatServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPkCombatHost
        {
            public int ModeChangedCalls;
            public int AttackCalls;
            public int KarmaCalls;
            public int RedNameCalls;
            public int ClearedRedCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public PkMode LastOldMode;
            public PkMode LastNewMode;
            public int LastAttackerId;
            public int LastTargetId;
            public bool LastCanAttack;
            public string LastReason;
            public PkPenaltyType LastPenalty;
            public int LastKarma;
            public int LastDelta;
            public bool LastIsRed;

            public void OnPkModeChanged(PkMode oldMode, PkMode newMode)
            {
                ModeChangedCalls++;
                LastOldMode = oldMode;
                LastNewMode = newMode;
            }
            public void OnAttackResolved(int attackerId, int targetId, bool canAttack, string reasonVi, PkPenaltyType penalty, int karmaChange)
            {
                AttackCalls++;
                LastAttackerId = attackerId;
                LastTargetId = targetId;
                LastCanAttack = canAttack;
                LastReason = reasonVi;
                LastPenalty = penalty;
            }
            public void OnKarmaChanged(int newKarma, int delta, bool isRedName)
            {
                KarmaCalls++;
                LastKarma = newKarma;
                LastDelta = delta;
                LastIsRed = isRedName;
            }
            public void OnBecameRedName(int actorId, int karma) { RedNameCalls++; }
            public void OnClearedRedName(int actorId) { ClearedRedCalls++; }
            public void LogPkEvent(int actorId, string message) { LogCalls++; }
            public void PlayPkSFX(int attackerId, int targetId, string combatType) { SfxCalls++; }
            public void SaveKarma(int actorId, int karma, PkMode mode) { SaveCalls++; }
        }

        private static CombatActorState MakeActor(int id, int factionId, int partyId = 0, int hp = 1000)
        {
            return new CombatActorState
            {
                actorId = id,
                faction = (CombatFaction)factionId,
                partyId = partyId,
                currentLife = hp,
                maxLife = hp,
            };
        }

        // ── Ctor / Mode / Karma ──────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new PkCombatService();
            Assert.AreEqual(PkMode.Peace, svc.Mode);
            Assert.AreEqual(0, svc.Karma);
            Assert.IsFalse(svc.IsRedName);
        }

        [Test]
        public void Constructor_WithFaction()
        {
            var svc = new PkCombatService(factionId: 5);
            Assert.AreEqual(PkMode.Peace, svc.Mode);
        }

        [Test]
        public void Constructor_WithBangAndFaction()
        {
            var svc = new PkCombatService(factionId: 5, bangId: 10);
            Assert.AreEqual(PkMode.Peace, svc.Mode);
        }

        [Test]
        public void Constructor_WithActorId()
        {
            var svc = new PkCombatService(5, 10, 42);
            Assert.AreEqual(42, svc.ActorId);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(5, 10, 42, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new PkCombatService();
            svc.AttachHost(host);
            svc.SetPkMode(PkMode.Free);
            Assert.AreEqual(1, host.ModeChangedCalls);
        }

        // ── SetPkMode ───────────────────────────────────────────────────────

        [Test]
        public void SetPkMode_Changes()
        {
            var svc = new PkCombatService();
            svc.SetPkMode(PkMode.Free);
            Assert.AreEqual(PkMode.Free, svc.Mode);
        }

        [Test]
        public void SetPkMode_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(5, 0, 42, host);
            svc.SetPkMode(PkMode.Free);
            Assert.AreEqual(1, host.ModeChangedCalls);
            Assert.AreEqual(PkMode.Peace, host.LastOldMode);
            Assert.AreEqual(PkMode.Free, host.LastNewMode);
            Assert.IsTrue(host.LogCalls >= 1);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void SetPkMode_FiresOnPkModeChangedEvent()
        {
            var svc = new PkCombatService();
            int fired = 0;
            PkMode? lastMode = null;
            svc.OnPkModeChanged += m => { fired++; lastMode = m; };
            svc.SetPkMode(PkMode.Faction);
            Assert.AreEqual(1, fired);
            Assert.AreEqual(PkMode.Faction, lastMode);
        }

        // ── CanAttack ────────────────────────────────────────────────────────

        [Test]
        public void CanAttack_Self_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Free);
            var a = MakeActor(1, 1);
            var r = svc.CanAttack(a, a);
            Assert.IsFalse(r.canAttack);
            Assert.That(r.reasonVi, Does.Contain("Không thể tự đánh mình"));
        }

        [Test]
        public void CanAttack_DeadTarget_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Free);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            t.currentLife = 0;
            var r = svc.CanAttack(a, t);
            Assert.IsFalse(r.canAttack);
            Assert.That(r.reasonVi, Does.Contain("Mục tiêu đã chết"));
        }

        [Test]
        public void CanAttack_PeaceMode_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            var r = svc.CanAttack(a, t);
            Assert.IsFalse(r.canAttack);
        }

        [Test]
        public void CanAttack_FreeMode_ReturnsTrue()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Free);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            var r = svc.CanAttack(a, t);
            Assert.IsTrue(r.canAttack);
            Assert.AreEqual(PkPenaltyType.KarmaIncrease, r.penalty);
            Assert.AreEqual(10, r.karmaChange);
        }

        [Test]
        public void CanAttack_FreeMode_OtherFaction_LowerKarma()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Free);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            var r = svc.CanAttack(a, t);
            Assert.AreEqual(5, r.karmaChange); // different faction
        }

        [Test]
        public void CanAttack_TeamMode_DifferentParty_ReturnsTrue()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Team);
            var a = MakeActor(1, 1, partyId: 100);
            var t = MakeActor(2, 2, partyId: 200);
            var r = svc.CanAttack(a, t);
            Assert.IsTrue(r.canAttack);
        }

        [Test]
        public void CanAttack_TeamMode_SameParty_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Team);
            var a = MakeActor(1, 1, partyId: 100);
            var t = MakeActor(2, 2, partyId: 100);
            var r = svc.CanAttack(a, t);
            Assert.IsFalse(r.canAttack);
        }

        [Test]
        public void CanAttack_FactionMode_DifferentFaction_ReturnsTrue()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Faction);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            var r = svc.CanAttack(a, t);
            Assert.IsTrue(r.canAttack);
        }

        [Test]
        public void CanAttack_FactionMode_SameFaction_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Faction);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 1);
            var r = svc.CanAttack(a, t);
            Assert.IsFalse(r.canAttack);
        }

        [Test]
        public void CanAttack_BangMode_DifferentFaction_ReturnsTrue()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Bang);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            var r = svc.CanAttack(a, t);
            Assert.IsTrue(r.canAttack);
        }

        [Test]
        public void CanAttack_BangMode_SameFaction_ReturnsFalse()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.SetPkMode(PkMode.Bang);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 1);
            var r = svc.CanAttack(a, t);
            Assert.IsFalse(r.canAttack);
        }

        [Test]
        public void CanAttack_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.SetPkMode(PkMode.Free);
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            svc.CanAttack(a, t);
            Assert.AreEqual(1, host.AttackCalls);
            Assert.IsTrue(host.LastCanAttack);
            Assert.AreEqual(1, host.SfxCalls);
        }

        [Test]
        public void CanAttack_NoAttack_NoSfx()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            // Peace mode
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            svc.CanAttack(a, t);
            Assert.AreEqual(1, host.AttackCalls);
            Assert.IsFalse(host.LastCanAttack);
            Assert.AreEqual(0, host.SfxCalls);
        }

        // ── ApplyKillPenalty ─────────────────────────────────────────────────

        [Test]
        public void ApplyKillPenalty_IncreasesKarma()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            Assert.AreEqual(50, svc.Karma);
            Assert.IsTrue(svc.IsRedName);
        }

        [Test]
        public void ApplyKillPenalty_ZeroKarma_NoChange()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 0 });
            Assert.AreEqual(0, svc.Karma);
        }

        [Test]
        public void ApplyKillPenalty_FiresOnKarmaChanged()
        {
            var svc = new PkCombatService(1, 0, 1);
            int fired = 0;
            svc.OnKarmaChanged += k => fired++;
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void ApplyKillPenalty_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            Assert.AreEqual(1, host.KarmaCalls);
            Assert.AreEqual(50, host.LastKarma);
            Assert.IsTrue(host.LastIsRed);
        }

        [Test]
        public void ApplyKillPenalty_BecomesRedName_Dispatches()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 1 });
            Assert.AreEqual(1, host.RedNameCalls);
        }

        [Test]
        public void ApplyKillPenalty_AlreadyRed_NoExtraRedCall()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 1 });
            svc.ApplyKillPenalty(new PkResult { karmaChange = 5 });
            Assert.AreEqual(1, host.RedNameCalls);
        }

        // ── ReduceKarma ──────────────────────────────────────────────────────

        [Test]
        public void ReduceKarma_Decreases()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 100 });
            svc.ReduceKarma(30);
            Assert.AreEqual(70, svc.Karma);
        }

        [Test]
        public void ReduceKarma_ClampsAtZero()
        {
            var svc = new PkCombatService(1, 0, 1);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            svc.ReduceKarma(100);
            Assert.AreEqual(0, svc.Karma);
            Assert.IsFalse(svc.IsRedName);
        }

        [Test]
        public void ReduceKarma_ClearsRedName_Dispatches()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            host.RedNameCalls = 0;
            host.ClearedRedCalls = 0;
            svc.ReduceKarma(100);
            Assert.AreEqual(1, host.ClearedRedCalls);
        }

        [Test]
        public void ReduceKarma_NotRed_NoClear()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ReduceKarma(10);
            Assert.AreEqual(0, host.ClearedRedCalls);
        }

        [Test]
        public void ReduceKarma_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PkCombatService(1, 0, 1, host);
            svc.ApplyKillPenalty(new PkResult { karmaChange = 50 });
            host.KarmaCalls = 0;
            svc.ReduceKarma(20);
            Assert.IsTrue(host.KarmaCalls >= 1);
        }

        [Test]
        public void PkCombatService_WithoutHost_DoesNotThrow()
        {
            var svc = new PkCombatService(1, 0, 1);
            Assert.DoesNotThrow(() => svc.SetPkMode(PkMode.Free));
            var a = MakeActor(1, 1);
            var t = MakeActor(2, 2);
            Assert.DoesNotThrow(() => svc.CanAttack(a, t));
            Assert.DoesNotThrow(() => svc.ApplyKillPenalty(new PkResult { karmaChange = 10 }));
            Assert.DoesNotThrow(() => svc.ReduceKarma(5));
        }
    }
}
