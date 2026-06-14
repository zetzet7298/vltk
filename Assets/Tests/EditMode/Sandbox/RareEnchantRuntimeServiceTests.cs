// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for rare-enchant runtime (tier resolution + level roll
// + host dispatch). PC source: settings/rare.txt 29 cols. Each MAGIC_ID has
// 1+ tier rows. A tier specifies [MAG_P1_MIN, MAG_P1_MAX] + weapon/slot/
// elemental weights. The runtime resolves a (magicId, level) to a tier,
// validates weights, then dispatches via IRareEnchantHost.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class RareEnchantRuntimeServiceTests
    {
        // --- Test fixtures ----------------------------------------------------

        private static PcRareEnchantEntry MakeEntry(
            int magicId, int min, int max,
            int wSword = 0, int wBlade = 0, int wWand = 0, int wSpear = 0,
            int wHammer = 0, int wDualBlades = 0, int wDarts = 0, int wKnife = 0,
            int wCrossbow = 0,
            int wArmor = 0, int wRing = 0, int wNecklace = 0, int wAmulet = 0,
            int wBoot = 0, int wBelt = 0, int wHelm = 0, int wCuff = 0,
            int wSachet = 0, int wPendant = 0,
            int wMetal = 0, int wWood = 0, int wWater = 0, int wFire = 0,
            int wEarth = 0)
        {
            return new PcRareEnchantEntry
            {
                nameRaw = $"Magic{magicId}",
                magicId = magicId,
                magP1Min = min,
                magP1Max = max,
                wSword = wSword, wBlade = wBlade, wWand = wWand, wSpear = wSpear,
                wHammer = wHammer, wDualBlades = wDualBlades, wDarts = wDarts,
                wKnife = wKnife, wCrossbow = wCrossbow,
                wArmor = wArmor, wRing = wRing, wNecklace = wNecklace, wAmulet = wAmulet,
                wBoot = wBoot, wBelt = wBelt, wHelm = wHelm, wCuff = wCuff,
                wSachet = wSachet, wPendant = wPendant,
                wMetal = wMetal, wWood = wWood, wWater = wWater, wFire = wFire, wEarth = wEarth,
            };
        }

        private static RareEnchantService BuildService(params PcRareEnchantEntry[] entries)
        {
            var table = new PcRareEnchantTable();
            foreach (var e in entries) table.Add(e);
            var svc = new RareEnchantService();
            svc.AttachTable(table);
            return svc;
        }

        private sealed class FakeHost : IRareEnchantHost
        {
            public int GetMagicIdFor = 0;
            public bool SetMagicOk = true;
            public int SetCalls;
            public int PoolBumpCalls;
            public int LastWrittenMagic;
            public int LastWrittenLevel;
            public string LastPlayer;
            public int LastItemIndex;

            public int GetWeaponMagicId(string player, int itemIndex)
            {
                LastPlayer = player; LastItemIndex = itemIndex;
                return GetMagicIdFor;
            }

            public bool SetWeaponMagic(string player, int itemIndex, int magicId, int level)
            {
                SetCalls++;
                LastPlayer = player; LastItemIndex = itemIndex;
                LastWrittenMagic = magicId; LastWrittenLevel = level;
                return SetMagicOk;
            }

            public void IncrementMagicPool(string player, int magicId)
            {
                PoolBumpCalls++;
                LastPlayer = player;
            }
        }

        // --- ResolveTier ------------------------------------------------------

        [Test]
        public void ResolveTier_UnknownMagicId_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(999, (int)RareWeaponType.Sword, (int)RareSlotType.Armor, (int)RareElemental.Metal);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("UnknownMagicId", r.ReasonVi);
        }

        [Test]
        public void ResolveTier_NoService_Denies()
        {
            var rt = new RareEnchantRuntimeService(null, null);
            var r = rt.ResolveTier(1, 0, 0, 0);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("NoService", r.ReasonVi);
        }

        [Test]
        public void ResolveTier_SingleTierContainsLevel_Resolves()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 5);
            Assert.IsTrue(r.Resolved);
            Assert.AreEqual(5, r.Level);
            Assert.AreEqual(5, r.WeaponTypeWeight);
            Assert.AreEqual(3, r.SlotTypeWeight);
            Assert.AreEqual(7, r.ElementalWeight);
        }

        [Test]
        public void ResolveTier_LevelOutOfAllTiers_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 100);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("LevelOutOfAllTiers", r.ReasonVi);
        }

        [Test]
        public void ResolveTier_MultipleTiers_PrefersHighestMinContainingLevel()
        {
            var low = MakeEntry(42, 1, 5, wSword: 5, wArmor: 3, wMetal: 7);
            var high = MakeEntry(42, 6, 20, wSword: 8, wArmor: 4, wMetal: 9);
            var svc = BuildService(low, high);
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 10);
            Assert.IsTrue(r.Resolved);
            Assert.AreSame(high, r.Tier);
            Assert.AreEqual(8, r.WeaponTypeWeight);
        }

        [Test]
        public void ResolveTier_WeaponTypeWeightZero_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wBlade: 5, wArmor: 3, wMetal: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 5);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("WeaponTypeWeightZero", r.ReasonVi);
        }

        [Test]
        public void ResolveTier_SlotTypeWeightZero_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wHelm: 3, wMetal: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 5);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("SlotTypeWeightZero", r.ReasonVi);
        }

        [Test]
        public void ResolveTier_ElementalWeightZero_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wFire: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var r = rt.ResolveTier(42, (int)RareWeaponType.Sword, (int)RareSlotType.Armor,
                (int)RareElemental.Metal, requestedLevel: 5);
            Assert.IsFalse(r.Resolved);
            StringAssert.Contains("ElementalWeightZero", r.ReasonVi);
        }

        [Test]
        public void RollLevelInTiers_NoTiers_ReturnsZero()
        {
            var rt = new RareEnchantRuntimeService(null, null);
            Assert.AreEqual(0, rt.RollLevelInTiers(new List<PcRareEnchantEntry>()));
        }

        [Test]
        public void RollLevelInTiers_UnionRange_DeterministicMidpoint()
        {
            var tiers = new List<PcRareEnchantEntry>
            {
                MakeEntry(42, 1, 5, wSword: 1),
                MakeEntry(42, 4, 12, wSword: 1),
            };
            var rt = new RareEnchantRuntimeService(null, null); // null seed = deterministic midpoint path
            // union: [1, 12], size=12, midpoint formula = min + size/2 = 7
            Assert.AreEqual(7, rt.RollLevelInTiers(tiers));
        }

        // --- ApplyEnchant (host dispatch) ------------------------------------

        [Test]
        public void ApplyEnchant_NoHost_Denies()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var rt = new RareEnchantRuntimeService(svc, null);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsFalse(outcome.Applied);
            StringAssert.Contains("NoHost", outcome.ReasonVi);
        }

        [Test]
        public void ApplyEnchant_ResolvesAndWritesMagic()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost { GetMagicIdFor = 0, SetMagicOk = true };
            var rt = new RareEnchantRuntimeService(svc, host, seed: 7);
            var outcome = rt.ApplyEnchant("alice", 3, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsTrue(outcome.Applied, outcome.ReasonVi);
            Assert.IsTrue(outcome.MagicWritten);
            Assert.IsTrue(outcome.PoolBumped);
            Assert.AreEqual(1, host.SetCalls);
            Assert.AreEqual(1, host.PoolBumpCalls);
            Assert.AreEqual(42, host.LastWrittenMagic);
            Assert.AreEqual(5, host.LastWrittenLevel);
        }

        [Test]
        public void ApplyEnchant_ReplacesZeroMagic_Allowed()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost { GetMagicIdFor = 0 };
            var rt = new RareEnchantRuntimeService(svc, host);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsTrue(outcome.Applied);
        }

        [Test]
        public void ApplyEnchant_RejectsDifferentMagic()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost { GetMagicIdFor = 99 };
            var rt = new RareEnchantRuntimeService(svc, host);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsFalse(outcome.Applied);
            StringAssert.Contains("WeaponHasDifferentMagic", outcome.ReasonVi);
            Assert.AreEqual(0, host.SetCalls);
            Assert.AreEqual(0, host.PoolBumpCalls);
        }

        [Test]
        public void ApplyEnchant_AllowsReEnchantSameMagic()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost { GetMagicIdFor = 42 };
            var rt = new RareEnchantRuntimeService(svc, host);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 7);
            Assert.IsTrue(outcome.Applied);
        }

        [Test]
        public void ApplyEnchant_HostSetFails_RollsBack()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wSword: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost { GetMagicIdFor = 0, SetMagicOk = false };
            var rt = new RareEnchantRuntimeService(svc, host);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsFalse(outcome.Applied);
            StringAssert.Contains("HostSetFailed", outcome.ReasonVi);
            Assert.AreEqual(0, host.PoolBumpCalls);
        }

        [Test]
        public void ApplyEnchant_WeightZero_DeniesBeforeHostCall()
        {
            var svc = BuildService(MakeEntry(42, 1, 10, wBlade: 5, wArmor: 3, wMetal: 7));
            var host = new FakeHost();
            var rt = new RareEnchantRuntimeService(svc, host);
            var outcome = rt.ApplyEnchant("alice", 0, 42,
                RareWeaponType.Sword, RareSlotType.Armor, RareElemental.Metal, 5);
            Assert.IsFalse(outcome.Applied);
            Assert.AreEqual(0, host.SetCalls);
        }

        // --- Weight helpers ---------------------------------------------------

        [Test]
        public void GetWeaponTypeWeight_AllNineTypes()
        {
            var e = MakeEntry(1, 1, 1, wSword: 1, wBlade: 2, wWand: 0, wSpear: 4, wHammer: 0,
                wDualBlades: 6, wDarts: 0, wKnife: 8, wCrossbow: 9);
            Assert.AreEqual(1, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 0));
            Assert.AreEqual(2, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 1));
            Assert.AreEqual(0, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 2));
            Assert.AreEqual(4, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 3));
            Assert.AreEqual(0, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 4));
            Assert.AreEqual(6, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 5));
            Assert.AreEqual(0, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 6));
            Assert.AreEqual(8, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 7));
            Assert.AreEqual(9, RareEnchantRuntimeService.GetWeaponTypeWeight(e, 8));
        }

        [Test]
        public void GetSlotTypeWeight_AllTenSlots()
        {
            var e = MakeEntry(1, 1, 1, wArmor: 1);
            e.wRing = 2; e.wNecklace = 3; e.wAmulet = 4; e.wBoot = 5;
            e.wBelt = 6; e.wHelm = 7; e.wCuff = 8; e.wSachet = 9; e.wPendant = 10;
            for (int i = 0; i < 10; i++)
                Assert.AreEqual(i + 1, RareEnchantRuntimeService.GetSlotTypeWeight(e, i));
        }

        [Test]
        public void GetElementalWeight_AllFiveElementals()
        {
            var e = MakeEntry(1, 1, 1, wMetal: 11, wFire: 33);
            e.wWood = 22; e.wWater = 33; e.wEarth = 55;
            Assert.AreEqual(11, RareEnchantRuntimeService.GetElementalWeight(e, 0));
            Assert.AreEqual(22, RareEnchantRuntimeService.GetElementalWeight(e, 1));
            Assert.AreEqual(33, RareEnchantRuntimeService.GetElementalWeight(e, 2));
            Assert.AreEqual(33, RareEnchantRuntimeService.GetElementalWeight(e, 3));
            Assert.AreEqual(55, RareEnchantRuntimeService.GetElementalWeight(e, 4));
        }
    }
}
