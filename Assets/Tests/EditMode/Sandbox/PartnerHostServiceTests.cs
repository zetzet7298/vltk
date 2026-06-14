// -----------------------------------------------------------------------------
// VLTK Mobile — PartnerService EditMode tests.
// Kiểm tra partner (thú cưng đồng hành) lifecycle: spawn/despawn/level-up/
// hunger-decay/feed, host dispatch chain.
// PC source: settings/partner/* + lua partner_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PartnerHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPartnerServiceHost
        {
            public int SpawnCalls;
            public int DespawnCalls;
            public int LevelUpCalls;
            public int DecayCalls;
            public int StarvingCalls;
            public int FedCalls;
            public int SfxCalls;
            public int SaveCalls;
            public int LastPetId;
            public int LastTemplateId;
            public int LastNewLevel;
            public int LastNewHunger;
            public int LastPrevHunger;
            public string LastNameVi;
            public string LastReason;
            public string LastSfxAction;

            public void OnPetSpawned(int petId, int templateId, int level, string nameVi, int maxHp, int currentHp)
            {
                SpawnCalls++;
                LastPetId = petId;
                LastTemplateId = templateId;
                LastNameVi = nameVi;
            }
            public void OnPetDespawned(int petId, int templateId, string reason)
            {
                DespawnCalls++;
                LastPetId = petId;
                LastTemplateId = templateId;
                LastReason = reason;
            }
            public void OnPetLevelledUp(int petId, int newLevel, int maxHp, int currentHp, int overflowExp)
            {
                LevelUpCalls++;
                LastNewLevel = newLevel;
            }
            public void OnPetHungerDecayed(int petId, int currentHunger) { DecayCalls++; }
            public void OnPetStarving(int petId, int templateId, int currentHunger) { StarvingCalls++; }
            public void OnPetFed(int petId, int newHunger, int previousHunger)
            {
                FedCalls++;
                LastNewHunger = newHunger;
                LastPrevHunger = previousHunger;
            }
            public void PlayPetSFX(int petId, string action) { SfxCalls++; LastSfxAction = action; }
            public void SavePetState(int petId, int templateId, int level, int exp, int hunger, int currentHp, int maxHp) { SaveCalls++; }
        }

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default_NoPets()
        {
            var svc = new PartnerService();
            Assert.AreEqual(0, svc.ActivePetCount);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new PartnerService();
            svc.AttachHost(host);
            var pet = svc.SpawnPet(101);
            Assert.IsNotNull(pet);
            Assert.AreEqual(1, host.SpawnCalls);
        }

        // ── SpawnPet ────────────────────────────────────────────────────────

        [Test]
        public void SpawnPet_Success()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            Assert.IsNotNull(pet);
            Assert.AreEqual(1, svc.ActivePetCount);
        }

        [Test]
        public void SpawnPet_AssignsUniqueId()
        {
            var svc = new PartnerService();
            var p1 = svc.SpawnPet(101);
            var p2 = svc.SpawnPet(102);
            Assert.AreNotEqual(p1.petId, p2.petId);
        }

        [Test]
        public void SpawnPet_ClampsLevel()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101, level: 999);
            Assert.IsNotNull(pet);
            Assert.AreEqual(PartnerService.MaxLevel, pet.level);
        }

        [Test]
        public void SpawnPet_NullName_DefaultName()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101, nameVi: null);
            Assert.AreEqual("Đồng Hành", pet.nameVi);
        }

        [Test]
        public void SpawnPet_AtMaxSlots_ReturnsNull()
        {
            var svc = new PartnerService();
            for (int i = 0; i < PartnerService.MaxPetSlots; i++)
                svc.SpawnPet(100 + i);
            Assert.IsNull(svc.SpawnPet(999));
        }

        [Test]
        public void SpawnPet_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            svc.SpawnPet(101);
            Assert.AreEqual(1, host.SpawnCalls);
            Assert.AreEqual(101, host.LastTemplateId);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void SpawnPet_FiresPetSpawnedEvent()
        {
            var svc = new PartnerService();
            int fired = 0;
            svc.PetSpawned += id => fired++;
            svc.SpawnPet(101);
            Assert.AreEqual(1, fired);
        }

        // ── DespawnPet ──────────────────────────────────────────────────────

        [Test]
        public void DespawnPet_Exists()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            Assert.IsTrue(svc.DespawnPet(pet.petId));
            Assert.AreEqual(0, svc.ActivePetCount);
        }

        [Test]
        public void DespawnPet_NotFound_ReturnsFalse()
        {
            var svc = new PartnerService();
            Assert.IsFalse(svc.DespawnPet(999));
        }

        [Test]
        public void DespawnPet_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            svc.DespawnPet(pet.petId);
            Assert.AreEqual(1, host.DespawnCalls);
            Assert.AreEqual(101, host.LastTemplateId);
        }

        [Test]
        public void DespawnPet_WithReason_DispatchesReason()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            svc.DespawnPet(pet.petId, "released");
            Assert.AreEqual("released", host.LastReason);
        }

        [Test]
        public void DespawnPet_FiresPetDespawnedEvent()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            int fired = 0;
            svc.PetDespawned += id => fired++;
            svc.DespawnPet(pet.petId);
            Assert.AreEqual(1, fired);
        }

        // ── GetPet / GetExpForLevel / GetPartnerCharacteristic ──────────────

        [Test]
        public void GetPet_NotFound_ReturnsNull()
        {
            var svc = new PartnerService();
            Assert.IsNull(svc.GetPet(999));
        }

        [Test]
        public void GetPet_Exists_ReturnsPet()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            Assert.IsNotNull(svc.GetPet(pet.petId));
        }

        [Test]
        public void GetExpForLevel_DefaultFormula()
        {
            var svc = new PartnerService();
            Assert.AreEqual(100, svc.GetExpForLevel(1));
            Assert.AreEqual(500, svc.GetExpForLevel(5));
        }

        [Test]
        public void GetExpForLevel_OverMax_ReturnsDefault()
        {
            var svc = new PartnerService();
            // MaxLevel+1 falls back to level*100
            int exp = svc.GetExpForLevel(PartnerService.MaxLevel + 1);
            Assert.AreEqual((PartnerService.MaxLevel + 1) * 100, exp);
        }

        [Test]
        public void GetPartnerCharacteristic_NullRegistry_ReturnsNull()
        {
            var svc = new PartnerService();
            Assert.IsNull(svc.GetPartnerCharacteristic(101));
        }

        // ── AwardExp ────────────────────────────────────────────────────────

        [Test]
        public void AwardExp_NotFound_NoEffect()
        {
            var svc = new PartnerService();
            Assert.DoesNotThrow(() => svc.AwardExp(999, 100));
        }

        [Test]
        public void AwardExp_ZeroOrNegative_NoEffect()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            svc.AwardExp(pet.petId, 0);
            Assert.AreEqual(0, pet.exp);
        }

        [Test]
        public void AwardExp_BelowThreshold_NoLevelUp()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            svc.AwardExp(pet.petId, 50); // need 100 to level up
            Assert.AreEqual(1, pet.level);
            Assert.AreEqual(50, pet.exp);
        }

        [Test]
        public void AwardExp_AtThreshold_LevelsUp()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            svc.AwardExp(pet.petId, 100);
            Assert.AreEqual(2, pet.level);
            Assert.AreEqual(0, pet.exp);
        }

        [Test]
        public void AwardExp_MultiLevel_LevelsMultiple()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            // level 1 needs 100, level 2 needs 200
            svc.AwardExp(pet.petId, 350);
            Assert.AreEqual(3, pet.level);
            Assert.AreEqual(50, pet.exp); // 350 - 100 - 200 = 50
        }

        [Test]
        public void AwardExp_AtMaxLevel_NoMoreUp()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101, level: PartnerService.MaxLevel);
            int prevLevel = pet.level;
            svc.AwardExp(pet.petId, 9999);
            Assert.AreEqual(prevLevel, pet.level);
        }

        [Test]
        public void AwardExp_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            svc.AwardExp(pet.petId, 100);
            Assert.AreEqual(1, host.LevelUpCalls);
            Assert.AreEqual(1, host.SfxCalls);
            Assert.IsTrue(host.SaveCalls >= 1);
        }

        [Test]
        public void AwardExp_FiresPetLevelledUpEvent()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            int fired = 0;
            svc.PetLevelledUp += id => fired++;
            svc.AwardExp(pet.petId, 100);
            Assert.AreEqual(1, fired);
        }

        // ── DecayHunger ─────────────────────────────────────────────────────

        [Test]
        public void DecayHunger_Decreases()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            pet.hunger = 50;
            svc.DecayHunger(pet.petId, 10);
            Assert.AreEqual(40, pet.hunger);
        }

        [Test]
        public void DecayHunger_ClampsAtZero()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            pet.hunger = 5;
            svc.DecayHunger(pet.petId, 100);
            Assert.AreEqual(0, pet.hunger);
        }

        [Test]
        public void DecayHunger_NotFound_NoEffect()
        {
            var svc = new PartnerService();
            Assert.DoesNotThrow(() => svc.DecayHunger(999, 10));
        }

        [Test]
        public void DecayHunger_ReachZero_TriggersStarving()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            pet.hunger = 5;
            svc.DecayHunger(pet.petId, 10);
            Assert.AreEqual(PetState.Starving, pet.state);
            Assert.AreEqual(1, host.StarvingCalls);
        }

        [Test]
        public void DecayHunger_AlreadyStarving_NoMoreCall()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            pet.hunger = 0;
            pet.state = PetState.Starving;
            svc.DecayHunger(pet.petId, 5);
            Assert.AreEqual(0, host.StarvingCalls);
        }

        [Test]
        public void DecayHunger_DispatchesDecay()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            pet.hunger = 50;
            svc.DecayHunger(pet.petId, 5);
            Assert.AreEqual(1, host.DecayCalls);
        }

        [Test]
        public void DecayHunger_FiresHungerDecayedEvent()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            int fired = 0;
            svc.HungerDecayed += id => fired++;
            svc.DecayHunger(pet.petId, 5);
            Assert.AreEqual(1, fired);
        }

        // ── FeedPet ─────────────────────────────────────────────────────────

        [Test]
        public void FeedPet_Increases()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            pet.hunger = 20;
            svc.FeedPet(pet.petId, 30);
            Assert.AreEqual(50, pet.hunger);
        }

        [Test]
        public void FeedPet_ClampsAtMax()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            pet.hunger = 90;
            svc.FeedPet(pet.petId, 30);
            Assert.AreEqual(100, pet.hunger);
        }

        [Test]
        public void FeedPet_FromStarving_ResetsState()
        {
            var svc = new PartnerService();
            var pet = svc.SpawnPet(101);
            pet.hunger = 0;
            pet.state = PetState.Starving;
            svc.FeedPet(pet.petId, 30);
            Assert.AreEqual(PetState.Following, pet.state);
        }

        [Test]
        public void FeedPet_NotFound_NoEffect()
        {
            var svc = new PartnerService();
            Assert.DoesNotThrow(() => svc.FeedPet(999, 30));
        }

        [Test]
        public void FeedPet_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new PartnerService(null, host);
            var pet = svc.SpawnPet(101);
            pet.hunger = 20;
            svc.FeedPet(pet.petId, 30);
            Assert.AreEqual(1, host.FedCalls);
            Assert.AreEqual(50, host.LastNewHunger);
            Assert.AreEqual(20, host.LastPrevHunger);
        }

        // ── ClearAll ────────────────────────────────────────────────────────

        [Test]
        public void ClearAll_Empties()
        {
            var svc = new PartnerService();
            svc.SpawnPet(101);
            svc.SpawnPet(102);
            svc.SpawnPet(103);
            svc.ClearAll();
            Assert.AreEqual(0, svc.ActivePetCount);
        }

        [Test]
        public void ClearAll_FiresDespawnForEach()
        {
            var svc = new PartnerService();
            svc.SpawnPet(101);
            svc.SpawnPet(102);
            int fired = 0;
            svc.PetDespawned += id => fired++;
            svc.ClearAll();
            Assert.AreEqual(2, fired);
        }

        // ── AllActivePets ───────────────────────────────────────────────────

        [Test]
        public void AllActivePets_Empty()
        {
            var svc = new PartnerService();
            int n = 0;
            foreach (var _ in svc.AllActivePets) n++;
            Assert.AreEqual(0, n);
        }

        [Test]
        public void AllActivePets_AfterSpawns()
        {
            var svc = new PartnerService();
            svc.SpawnPet(101);
            svc.SpawnPet(102);
            int n = 0;
            foreach (var _ in svc.AllActivePets) n++;
            Assert.AreEqual(2, n);
        }
    }
}
