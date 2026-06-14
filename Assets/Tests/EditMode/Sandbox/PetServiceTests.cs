// -----------------------------------------------------------------------------
// VLTK Mobile — PetService EditMode tests.
// Kiểm tra pet lifecycle: create, feed (hunger), train (skill + level up),
// intimacy, host dispatch chain.
// PC source: Pet NPC, PetDetailEntry, lua pet_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PetServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPetHost
        {
            public int CreatedCalls;
            public int FedCalls;
            public int TrainedCalls;
            public int HungryCalls;
            public int IntimacyCalls;
            public int SfxCalls;
            public int LogCalls;
            public int SaveCalls;
            public int LastPlayerId;
            public int LastPetId;
            public int LastFoodId;
            public int LastSkillId;
            public int LastHunger;
            public int LastNewLevel;
            public int LastNewExp;
            public int LastNewIntimacy;
            public string LastName;
            public string LastSfxAction;

            public void OnPetCreated(int playerId, int petId, string name, int hunger, int intimacy)
            {
                CreatedCalls++;
                LastName = name;
                LastHunger = hunger;
            }
            public void OnPetFed(int playerId, int petId, int foodId, int newHunger)
            {
                FedCalls++;
                LastFoodId = foodId;
                LastHunger = newHunger;
            }
            public void OnPetTrained(int playerId, int petId, int skillId, int newLevel, int newExp)
            {
                TrainedCalls++;
                LastSkillId = skillId;
                LastNewLevel = newLevel;
                LastNewExp = newExp;
            }
            public void OnPetHungry(int playerId, int petId, int currentHunger, int threshold) { HungryCalls++; }
            public void OnPetIntimacyChanged(int playerId, int petId, int newIntimacy)
            {
                IntimacyCalls++;
                LastNewIntimacy = newIntimacy;
            }
            public void PlayPetSFX(int playerId, int petId, string action)
            {
                SfxCalls++;
                LastSfxAction = action;
            }
            public void LogPetEvent(int playerId, int petId, string message) { LogCalls++; }
            public void SavePetState(int playerId, int petId, int level, int exp, int hunger, int intimacy)
            {
                SaveCalls++;
            }
        }

        // ── Ctor / CreatePet ────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new PetService();
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void CreatePet_AddsPet()
        {
            var svc = new PetService();
            var p = svc.CreatePet(1, 100, "PetA", "pet_a.spr");
            Assert.IsNotNull(p);
            Assert.AreEqual(1, svc.Count);
        }

        [Test]
        public void CreatePet_InitializesDefaults()
        {
            var svc = new PetService();
            var p = svc.CreatePet(1, 100, "PetA", "pet_a.spr");
            Assert.AreEqual(1, p.level);
            Assert.AreEqual(0, p.exp);
            Assert.AreEqual(PetService.MaxHunger, p.hunger);
            Assert.AreEqual(0, p.intimacy);
        }

        [Test]
        public void CreatePet_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            svc.CreatePet(1, 100, "PetA", "pet_a.spr");
            Assert.AreEqual(1, host.CreatedCalls);
            Assert.AreEqual("PetA", host.LastName);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void CreatePet_NullName_UsesEmpty()
        {
            var svc = new PetService();
            var p = svc.CreatePet(1, 100, null, "pet_a.spr");
            Assert.AreEqual(string.Empty, p.name);
        }

        [Test]
        public void CreatePet_FiresOnPetCreatedEvent()
        {
            var svc = new PetService();
            int fired = 0;
            svc.OnPetCreated += (pl, pe) => fired++;
            svc.CreatePet(1, 100, "X", "x");
            Assert.AreEqual(1, fired);
        }

        // ── GetPet ───────────────────────────────────────────────────────────

        [Test]
        public void GetPet_NotFound_ReturnsNull()
        {
            var svc = new PetService();
            Assert.IsNull(svc.GetPet(99));
        }

        [Test]
        public void GetPet_Exists_ReturnsPet()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            Assert.IsNotNull(svc.GetPet(1));
        }

        // ── TryFeed ──────────────────────────────────────────────────────────

        [Test]
        public void TryFeed_Success()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.GetPet(1).hunger = 50;
            Assert.IsTrue(svc.TryFeed(1, 50));
            Assert.AreEqual(75, svc.GetHunger(1));
        }

        [Test]
        public void TryFeed_ClampsAtMax()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            // hunger is already MaxHunger
            Assert.IsTrue(svc.TryFeed(1, 50));
            Assert.AreEqual(PetService.MaxHunger, svc.GetHunger(1));
        }

        [Test]
        public void TryFeed_NoPet_ReturnsFalse()
        {
            var svc = new PetService();
            Assert.IsFalse(svc.TryFeed(99, 1));
        }

        [Test]
        public void TryFeed_ZeroFoodId_ReturnsFalse()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            Assert.IsFalse(svc.TryFeed(1, 0));
        }

        [Test]
        public void TryFeed_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            svc.CreatePet(1, 100, "X", "x");
            svc.TryFeed(1, 50);
            Assert.AreEqual(1, host.FedCalls);
            Assert.AreEqual(50, host.LastFoodId);
        }

        [Test]
        public void TryFeed_FiresOnPetFedEvent()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            int fired = 0;
            svc.OnPetFed += pl => fired++;
            svc.TryFeed(1, 50);
            Assert.AreEqual(1, fired);
        }

        // ── TryTrain ─────────────────────────────────────────────────────────

        [Test]
        public void TryTrain_Success_AddsSkill()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            Assert.IsTrue(svc.TryTrain(1, 1));
            Assert.That(svc.GetPet(1).skills, Does.Contain("1"));
        }

        [Test]
        public void TryTrain_DuplicateSkill_NoDuplicate()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.TryTrain(1, 1);
            svc.TryTrain(1, 1);
            // skills should have only "1" once
            var skills = svc.GetPet(1).skills;
            int count = 0;
            int idx = 0;
            while ((idx = skills.IndexOf("1", idx)) != -1) { count++; idx++; }
            Assert.AreEqual(1, count);
        }

        [Test]
        public void TryTrain_Hungry_ReturnsFalse()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.GetPet(1).hunger = 10; // below HungryThreshold
            Assert.IsFalse(svc.TryTrain(1, 1));
        }

        [Test]
        public void TryTrain_Hungry_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            svc.CreatePet(1, 100, "X", "x");
            svc.GetPet(1).hunger = 10;
            svc.TryTrain(1, 1);
            Assert.AreEqual(1, host.HungryCalls);
        }

        [Test]
        public void TryTrain_NoPet_ReturnsFalse()
        {
            var svc = new PetService();
            Assert.IsFalse(svc.TryTrain(99, 1));
        }

        [Test]
        public void TryTrain_LevelUp_AfterEnoughExp()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            // level 1, exp += 10 per train, need 100 to level up
            for (int i = 0; i < 10; i++) svc.TryTrain(1, 100 + i);
            Assert.AreEqual(2, svc.GetPet(1).level);
        }

        [Test]
        public void TryTrain_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            svc.CreatePet(1, 100, "X", "x");
            svc.TryTrain(1, 1);
            Assert.AreEqual(1, host.TrainedCalls);
            Assert.AreEqual(1, host.LastSkillId);
        }

        [Test]
        public void TryTrain_WithoutHost_DoesNotThrow()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            Assert.DoesNotThrow(() => svc.TryTrain(1, 1));
        }

        // ── AddIntimacy ──────────────────────────────────────────────────────

        [Test]
        public void AddIntimacy_Increases()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.AddIntimacy(1, 100);
            Assert.AreEqual(100, svc.GetIntimacy(1));
        }

        [Test]
        public void AddIntimacy_ClampsAtMax()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.AddIntimacy(1, 2000);
            Assert.AreEqual(PetService.MaxIntimacy, svc.GetIntimacy(1));
        }

        [Test]
        public void AddIntimacy_ClampsAtZero()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.GetPet(1).intimacy = 50;
            svc.AddIntimacy(1, -100);
            Assert.AreEqual(0, svc.GetIntimacy(1));
        }

        [Test]
        public void AddIntimacy_NoPet_DoesNotThrow()
        {
            var svc = new PetService();
            Assert.DoesNotThrow(() => svc.AddIntimacy(99, 10));
        }

        [Test]
        public void AddIntimacy_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PetService(host);
            svc.CreatePet(1, 100, "X", "x");
            svc.AddIntimacy(1, 100);
            Assert.AreEqual(1, host.IntimacyCalls);
            Assert.AreEqual(100, host.LastNewIntimacy);
        }

        // ── IsHungry ─────────────────────────────────────────────────────────

        [Test]
        public void IsHungry_NoPet_ReturnsFalse()
        {
            var svc = new PetService();
            Assert.IsFalse(svc.IsHungry(99));
        }

        [Test]
        public void IsHungry_BelowThreshold_True()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            svc.GetPet(1).hunger = 10;
            Assert.IsTrue(svc.IsHungry(1));
        }

        [Test]
        public void IsHungry_AboveThreshold_False()
        {
            var svc = new PetService();
            svc.CreatePet(1, 100, "X", "x");
            Assert.IsFalse(svc.IsHungry(1));
        }

        // ── AttachHost ───────────────────────────────────────────────────────

        [Test]
        public void AttachHost_Replaces()
        {
            var host1 = new FakeHost();
            var host2 = new FakeHost();
            var svc = new PetService(host1);
            svc.AttachHost(host2);
            svc.CreatePet(1, 100, "X", "x");
            Assert.AreEqual(0, host1.CreatedCalls);
            Assert.AreEqual(1, host2.CreatedCalls);
        }
    }
}
