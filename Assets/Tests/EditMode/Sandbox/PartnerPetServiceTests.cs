using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PartnerServiceTests
    {
        private static string PartnerDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcPartner");
        private static string PetDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcPet");

        [Test]
        public void LoadFromStreamingAssets_LoadsCharacteristics()
        {
            var reg = PcPartnerParser.BuildRegistry(PartnerDir);
            Assert.GreaterOrEqual(reg.Count, 1, "PC character.txt có ít nhất 1 characteristic");
        }

        [Test]
        public void SpawnPet_AddsInstance()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            int before = svc.ActivePetCount;
            var pet = svc.SpawnPet(templateId: 1, level: 5, nameVi: "Hắc Miêu");
            Assert.IsNotNull(pet);
            Assert.AreEqual(before + 1, svc.ActivePetCount);
            Assert.AreEqual("Hắc Miêu", pet.nameVi);
            Assert.AreEqual(5, pet.level);
            Assert.AreEqual(100, pet.hunger);
        }

        [Test]
        public void DespawnPet_RemovesInstance()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            var pet = svc.SpawnPet(1, 1, "A");
            int before = svc.ActivePetCount;
            bool ok = svc.DespawnPet(pet.petId);
            Assert.IsTrue(ok);
            Assert.AreEqual(before - 1, svc.ActivePetCount);
            Assert.IsNull(svc.GetPet(pet.petId));
        }

        [Test]
        public void AwardExp_LevelsUp()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            var pet = svc.SpawnPet(1, 1, "A");
            int startLevel = pet.level;
            // level 1 needs 100 exp to level up
            bool fired = false;
            svc.PetLevelledUp += _ => fired = true;
            svc.AwardExp(pet.petId, 250);
            Assert.AreEqual(startLevel + 1, pet.level, "Pet phải lên 1 level khi đạt exp yêu cầu");
            Assert.IsTrue(fired, "Event PetLevelledUp phải fire");
        }

        [Test]
        public void DecayHunger_ReducesHunger()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            var pet = svc.SpawnPet(1, 1, "A");
            int start = pet.hunger;
            svc.DecayHunger(pet.petId, 20);
            Assert.AreEqual(start - 20, pet.hunger);
        }

        [Test]
        public void DecayHunger_TriggersStarvingAtZero()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            var pet = svc.SpawnPet(1, 1, "A");
            bool starvingFired = false;
            svc.PetStarving += _ => starvingFired = true;
            svc.DecayHunger(pet.petId, 200);
            Assert.AreEqual(0, pet.hunger);
            Assert.IsTrue(pet.IsStarving);
            Assert.IsTrue(starvingFired);
        }

        [Test]
        public void FeedPet_RestoresHunger()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            var pet = svc.SpawnPet(1, 1, "A");
            svc.DecayHunger(pet.petId, 50);
            svc.FeedPet(pet.petId, 30);
            Assert.AreEqual(80, pet.hunger);
        }

        [Test]
        public void GetPartnerCharacteristic_ReturnsEntry()
        {
            var svc = new PartnerService(PcPartnerParser.BuildRegistry(PartnerDir));
            // Build a fake registry to guarantee lookup
            var reg = new PcPartnerRegistry();
            reg.Register(new PcPartnerEntry { characteristic = 99, visionRadius = 10, activeRadius = 5 });
            var svc2 = new PartnerService(reg);
            var entry = svc2.GetPartnerCharacteristic(99);
            Assert.IsNotNull(entry);
            Assert.AreEqual(10, entry.visionRadius);
        }
    }

    public class PetSkillServiceTests
    {
        private static string PetDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcPet");

        [Test]
        public void LoadFromStreamingAssets_LoadsSkills()
        {
            var svc = PetSkillService.LoadFromStreamingAssets(PetDir);
            Assert.GreaterOrEqual(svc.TotalSkills, 1, "PC pet_skill_def.txt có ~21 rows");
        }

        [Test]
        public void GetMaxLevel_IsPositive()
        {
            var svc = PetSkillService.LoadFromStreamingAssets(PetDir);
            Assert.Greater(svc.GetMaxLevel(), 0);
        }

        [Test]
        public void GetSkillForLevel_NotNullForLowLevel()
        {
            var svc = PetSkillService.LoadFromStreamingAssets(PetDir);
            var entry = svc.GetSkillForLevel(1);
            Assert.IsNotNull(entry, "Level 1 phải có entry (PC sparse table falls back)");
            Assert.AreEqual(1, entry.level);
        }

        [Test]
        public void GetSkillBonus_ReturnsValidValue()
        {
            var svc = PetSkillService.LoadFromStreamingAssets(PetDir);
            int primary = svc.GetSkillBonus(1, true);
            int secondary = svc.GetSkillBonus(1, false);
            // PC uses -1 for "no skill" — both should be parseable
            Assert.IsTrue(primary >= -1);
            Assert.IsTrue(secondary >= -1);
        }

        [Test]
        public void GetSkillForLevel_FallsBackToLowerLevel()
        {
            var reg = new PcPetSkillRegistry();
            reg.Register(new PcPetSkillEntry { level = 3, magAttr1 = 42, param1 = 10, magAttr2 = 7 });
            var svc = new PetSkillService(reg);
            var entry = svc.GetSkillForLevel(50);
            Assert.IsNotNull(entry);
            Assert.AreEqual(3, entry.level);
            Assert.AreEqual(42, entry.magAttr1);
        }
    }
}
