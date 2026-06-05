// -----------------------------------------------------------------------------
// VLTK Mobile — UI Extensive Panel Service Tests
// Tests cho 8 panel services: Inventory, Map, Bag, NpcDialog, Character,
// SkillTree, Stall, Compound.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI;

namespace VLTK.Tests.EditMode
{
    [TestFixture]
    public class InventoryPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_NullDb()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = InventoryPanelService.BuildSnapshot(null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(30, snap.totalSlots);
                Assert.AreEqual(0, snap.usedSlots);
            });
        }

        [Test]
        public void GetPcInventoryOrder_NonEmpty()
        {
            var order = InventoryPanelService.GetPcInventoryOrder();
            Assert.IsNotNull(order);
            Assert.Greater(order.Count, 0);
            Assert.AreEqual(30, order.Count);
        }

        [Test]
        public void GetItemName_ReturnsString()
        {
            string name = InventoryPanelService.GetItemName(null, 0);
            Assert.IsNotNull(name);
            Assert.AreEqual(string.Empty, name);
        }
    }

    [TestFixture]
    public class MapPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_NullSvc()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = MapPanelService.BuildSnapshot(null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(0, snap.totalMaps);
            });
        }

        [Test]
        public void GetMapsByType_FiltersCorrectly()
        {
            var src = new[]
            {
                new MapPanelRow(1, "A", 0, 1, true, false, 0),
                new MapPanelRow(2, "B", 1, 1, true, false, 0),
                new MapPanelRow(3, "C", 0, 1, true, false, 0),
            };
            var filtered = MapPanelService.GetMapsByType(src, 0);
            Assert.AreEqual(2, filtered.Count);
        }

        [Test]
        public void GetMapIconPath_ReturnsString()
        {
            string path = MapPanelService.GetMapIconPath(123);
            Assert.IsNotNull(path);
            Assert.That(path, Does.Contain("icon_0123"));
        }
    }

    [TestFixture]
    public class BagPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = BagPanelService.BuildSnapshot(0);
                Assert.IsNotNull(snap);
                Assert.AreEqual(4, snap.totalBags);
            });
        }

        [Test]
        public void GetBag_ReturnsNullForInvalid()
        {
            Assert.IsNull(BagPanelService.GetBag(0));
            Assert.IsNull(BagPanelService.GetBag(99));
        }

        [Test]
        public void GetRemainingSlots_NonNegative()
        {
            int rem = BagPanelService.GetRemainingSlots(1);
            Assert.GreaterOrEqual(rem, 0);
            Assert.AreEqual(80, rem);
        }
    }

    [TestFixture]
    public class NpcDialogPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = NpcDialogPanelService.BuildSnapshot(null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(1, snap.npcId);
            });
        }

        [Test]
        public void GetNext_ReturnsNullForInvalid()
        {
            Assert.IsNull(NpcDialogPanelService.GetNext(0));
            Assert.IsNull(NpcDialogPanelService.GetNext(-1));
        }
    }

    [TestFixture]
    public class CharacterPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = CharacterPanelService.BuildSnapshot(null, null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(1, snap.level);
            });
        }

        [Test]
        public void GetPcStatOrder_NonEmpty()
        {
            var order = CharacterPanelService.GetPcStatOrder();
            Assert.IsNotNull(order);
            Assert.Greater(order.Count, 0);
        }

        [Test]
        public void ComputePowerLevel_ZeroForZero()
        {
            int power = CharacterPanelService.ComputePowerLevel(null);
            Assert.AreEqual(0, power);
        }
    }

    [TestFixture]
    public class SkillTreePanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = SkillTreePanelService.BuildSnapshot(null, null);
                Assert.IsNotNull(snap);
                Assert.GreaterOrEqual(snap.totalSkills, 0);
            });
        }

        [Test]
        public void GetPcSkillTreeOrder_NonEmpty()
        {
            var order = SkillTreePanelService.GetPcSkillTreeOrder(VLTK.Model.CombatFaction.CaiBang);
            Assert.IsNotNull(order);
            Assert.Greater(order.Count, 0);
        }

        [Test]
        public void CanLearn_RejectsNoPrereq()
        {
            var row = new SkillTreeRow(
                skillId: 1, skillName: "Test", parentId: 0, tier: 0, column: 0,
                isUnlocked: false, isLearned: false, isActive: false,
                prereqMet: false, reqLevel: 10, reqSkillId: 0, iconPath: "");
            Assert.IsFalse(SkillTreePanelService.CanLearn(row, null));
        }
    }

    [TestFixture]
    public class StallPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = StallPanelService.BuildSnapshot(null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(20, snap.totalSlots);
            });
        }

        [Test]
        public void TryAddItem_RejectsInvalidItem()
        {
            Assert.IsFalse(StallPanelService.TryAddItem(0, 0, 1, 1, 1));
            Assert.IsFalse(StallPanelService.TryAddItem(1, -1, 1, 1, 1));
            Assert.IsFalse(StallPanelService.TryAddItem(1, 0, 0, 1, 1));
            Assert.IsFalse(StallPanelService.TryAddItem(1, 0, 1, 0, 1));
            Assert.IsFalse(StallPanelService.TryAddItem(1, 0, 1, 1, 0));
        }

        [Test]
        public void GetTotalValue_ZeroForEmpty()
        {
            int val = StallPanelService.GetTotalValue(0);
            Assert.AreEqual(0, val);
        }
    }

    [TestFixture]
    public class CompoundPanelServiceTests
    {
        [Test]
        public void BuildSnapshot_DoesNotThrow_Null()
        {
            Assert.DoesNotThrow(() =>
            {
                var snap = CompoundPanelService.BuildSnapshot(null, 1);
                Assert.IsNotNull(snap);
                Assert.AreEqual(0, snap.totalRecipes);
            });
        }

        [Test]
        public void CanCompound_RejectsInvalidRecipe()
        {
            Assert.IsFalse(CompoundPanelService.CanCompound(0, 1, 5));
            Assert.IsFalse(CompoundPanelService.CanCompound(1, 0, 5));
            Assert.IsFalse(CompoundPanelService.CanCompound(1, 1, 0));
            Assert.IsFalse(CompoundPanelService.CanCompound(1, 1, 99999));
        }

        [Test]
        public void ComputeSuccessRate_ReturnsBetween_0_1()
        {
            float r1 = CompoundPanelService.ComputeSuccessRate(1, 50, 10);
            float r2 = CompoundPanelService.ComputeSuccessRate(1, 100, 30);
            float r3 = CompoundPanelService.ComputeSuccessRate(1, 1, 0);
            Assert.GreaterOrEqual(r1, 0f);
            Assert.LessOrEqual(r1, 1f);
            Assert.Greater(r2, r1);
            Assert.LessOrEqual(r2, 0.95f);
            Assert.GreaterOrEqual(r3, 0.5f);
        }
    }
}
