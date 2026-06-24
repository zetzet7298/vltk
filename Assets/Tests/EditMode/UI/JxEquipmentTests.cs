// -----------------------------------------------------------------------------
// VLTK Mobile — JX role/equipment panel tests (port KuiRoleStateVN/KuiRoleState)
// Verifies CtrlItemMap 15 slot mapping, ITEM_CELL_SIZE=35, equip/unequip
// validation, addpicBox background/sprite position math. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxEquipmentTests
    {
        private static JxEquippedItem EquipItem(int id = 1) => new()
        {
            ItemId = id,
            Genre = JxItemGenre.Equip,
            Name = "Kiếm",
            Quality = JxItemQuality.Normal,
        };

        [Test]
        public void Constants_MatchSource()
        {
            Assert.That(JxEquipmentState.CellSize, Is.EqualTo(35));
            Assert.That(JxEquipmentState.StartX, Is.EqualTo(24f));
            Assert.That(JxEquipmentState.StartY, Is.EqualTo(72f));
            Assert.That(JxEquipmentState.TempOffsetY, Is.EqualTo(55f));
        }

        [Test]
        public void SlotDefs_Has15Slots()
        {
            Assert.That(JxEquipmentState.SlotDefs.Length, Is.EqualTo(15));
        }

        [Test]
        public void SlotDef_Head_MatchesCtrlItemMap()
        {
            Assert.IsTrue(JxEquipmentState.TryGetSlotDef(JxEquipmentPanelSlot.Head, out var def));
            Assert.That(def.Key, Is.EqualTo("Cap"));
            Assert.That(def.CellX, Is.EqualTo(2));
            Assert.That(def.CellY, Is.EqualTo(2));
            Assert.That(def.OffsetX, Is.EqualTo(119f));
            Assert.That(def.OffsetY, Is.EqualTo(51f));
        }

        [Test]
        public void SlotDef_Weapon_MatchesCtrlItemMap()
        {
            Assert.IsTrue(JxEquipmentState.TryGetSlotDef(JxEquipmentPanelSlot.Weapon, out var def));
            Assert.That(def.Key, Is.EqualTo("Weapon"));
            Assert.That(def.CellX, Is.EqualTo(2));
            Assert.That(def.CellY, Is.EqualTo(4));
            Assert.That(def.OffsetX, Is.EqualTo(217f));
            Assert.That(def.OffsetY, Is.EqualTo(122f));
        }

        [Test]
        public void SlotDef_MaskAndShipin_MatchCtrlItemMap()
        {
            Assert.IsTrue(JxEquipmentState.TryGetSlotDef(JxEquipmentPanelSlot.Mask, out var mask));
            Assert.That(mask.Key, Is.EqualTo("Mask"));
            Assert.That(mask.CellX, Is.EqualTo(1));
            Assert.That(mask.CellY, Is.EqualTo(1));
            Assert.That(mask.OffsetX, Is.EqualTo(46f));
            Assert.That(mask.OffsetY, Is.EqualTo(51f));

            Assert.IsTrue(JxEquipmentState.TryGetSlotDef(JxEquipmentPanelSlot.Ornament, out var shipin));
            Assert.That(shipin.Key, Is.EqualTo("Shipin"));
            Assert.That(shipin.OffsetX, Is.EqualTo(247f));
            Assert.That(shipin.OffsetY, Is.EqualTo(297f));
        }

        [Test]
        public void Equip_RejectsInvalidId_NonEquip_AndOccupiedWhenNoReplace()
        {
            var s = new JxEquipmentState();
            Assert.IsFalse(s.Equip(JxEquipmentPanelSlot.Head, new JxEquippedItem { ItemId = 0, Genre = JxItemGenre.Equip }));
            Assert.IsFalse(s.Equip(JxEquipmentPanelSlot.Head, new JxEquippedItem { ItemId = 10, Genre = JxItemGenre.Medicine }));
            Assert.IsTrue(s.Equip(JxEquipmentPanelSlot.Head, EquipItem(1), replace: false));
            Assert.IsFalse(s.Equip(JxEquipmentPanelSlot.Head, EquipItem(2), replace: false));
        }

        [Test]
        public void Equip_ReplacesByDefault()
        {
            var s = new JxEquipmentState();
            Assert.IsTrue(s.Equip(JxEquipmentPanelSlot.Head, EquipItem(1)));
            Assert.IsTrue(s.Equip(JxEquipmentPanelSlot.Head, EquipItem(2)));
            Assert.IsTrue(s.TryGetItem(JxEquipmentPanelSlot.Head, out var item));
            Assert.That(item.ItemId, Is.EqualTo(2));
        }

        [Test]
        public void Unequip_RemovesItem()
        {
            var s = new JxEquipmentState();
            s.Equip(JxEquipmentPanelSlot.Weapon, EquipItem(100));
            Assert.IsTrue(s.Unequip(JxEquipmentPanelSlot.Weapon));
            Assert.IsFalse(s.TryGetItem(JxEquipmentPanelSlot.Weapon, out _));
            Assert.IsFalse(s.Unequip(JxEquipmentPanelSlot.Weapon));
        }

        [Test]
        public void SlotBackgroundPosition_MatchesAddpicBoxMath()
        {
            // Head: offset(119,51), size 2x2*35=70x70, tempOffsetY=55.
            // x = panelW/2 + 119 = 200+119=319
            // y = panelH - 51 - 70 - 55 = 600-176=424
            var (x, y) = JxEquipmentState.SlotBackgroundPosition(JxEquipmentPanelSlot.Head, 400, 600);
            Assert.That(x, Is.EqualTo(319f));
            Assert.That(y, Is.EqualTo(424f));
        }

        [Test]
        public void ItemSpritePosition_MatchesAddpicBoxMath()
        {
            // Weapon: offset(217,122), bg=70x140, tex=32x64.
            // x = 200 + 217 + 35 - 16 = 436
            // y = 600 - 122 - (140+64)/2 - 55 = 321
            var (x, y) = JxEquipmentState.ItemSpritePosition(JxEquipmentPanelSlot.Weapon, 400, 600, 32, 64);
            Assert.That(x, Is.EqualTo(436f));
            Assert.That(y, Is.EqualTo(321f));
        }
    }
}
