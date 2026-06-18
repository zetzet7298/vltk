using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M5.2 — Inventory and Equipment Sandbox tests. Search items (AC#1), add to
    /// inventory (AC#2), equip + stat preview (AC#3), missing-icon diagnostic (AC#4).
    /// </summary>
    public class InventoryServiceTests
    {
        private string _stagingRoot;

        [SetUp]
        public void SetUp()
        {
            _stagingRoot = MalePlayerSprStaging.StageForTests();
        }

        [TearDown]
        public void TearDown()
        {
            MalePlayerSprStaging.CleanupTempDir(_stagingRoot);
        }

        private ItemDefinition Item(int id, string name, int attr, int value, bool iconResolved = true)
        {
            var item = new ItemDefinition
            {
                itemId = id,
                nameNormalized = name,
                iconResolved = iconResolved,
            };
            item.statDeltas.Add(new ItemStatDelta { stage = ItemStatStage.Base, attrCode = attr, value = value });
            return item;
        }

        private ItemContractImporter DbWith(params ItemDefinition[] items)
        {
            var imp = new ItemContractImporter();
            var bundle = new ItemContractBundle { items = new List<ItemDefinition>(items) };
            imp.Import(bundle);
            return imp;
        }

        // --- AC#1: search ---

        [Test]
        public void Search_ByName_ReturnsMatches()
        {
            var db = DbWith(Item(1, "Sword of Power", 28, 10), Item(2, "Wooden Shield", 29, 5));
            var svc = new InventoryService(db);
            var results = svc.Search("sword");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(1, results[0].itemId);
        }

        [Test]
        public void Search_ById_ReturnsItem()
        {
            var db = DbWith(Item(1, "A", 28, 10), Item(42, "B", 29, 5));
            var svc = new InventoryService(db);
            var results = svc.Search("42");
            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(42, results[0].itemId);
        }

        [Test]
        public void Search_Empty_ReturnsAllSortedById()
        {
            var db = DbWith(Item(3, "C", 1, 1), Item(1, "A", 1, 1), Item(2, "B", 1, 1));
            var svc = new InventoryService(db);
            var results = svc.Search("");
            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(1, results[0].itemId);
            Assert.AreEqual(3, results[2].itemId);
        }

        // --- AC#2: add to inventory ---

        [Test]
        public void AddItem_AppearsInInventory()
        {
            var db = DbWith(Item(1, "A", 28, 10));
            var svc = new InventoryService(db);
            Assert.IsTrue(svc.AddItem(1));
            Assert.AreEqual(1, svc.Inventory.Count);
            Assert.AreEqual(1, svc.Inventory[0].count);
        }

        [Test]
        public void AddItem_Stacks()
        {
            var db = DbWith(Item(1, "A", 28, 10));
            var svc = new InventoryService(db);
            svc.AddItem(1, 2);
            svc.AddItem(1, 3);
            Assert.AreEqual(1, svc.Inventory.Count);
            Assert.AreEqual(5, svc.Inventory[0].count);
        }

        [Test]
        public void AddItem_Unknown_Fails()
        {
            var db = DbWith(Item(1, "A", 28, 10));
            var svc = new InventoryService(db);
            Assert.IsFalse(svc.AddItem(999));
        }

        [Test]
        public void AddItem_RejectsNewStackWhenMobileBagFull()
        {
            var items = new List<ItemDefinition>();
            for (int i = 1; i <= InventoryService.MaxInventorySlots + 1; i++)
                items.Add(Item(i, $"Item {i}", 28, i));

            var svc = new InventoryService(DbWith(items.ToArray()));
            for (int i = 1; i <= InventoryService.MaxInventorySlots; i++)
                Assert.IsTrue(svc.AddItem(i), $"slot {i} should fit");

            Assert.AreEqual(InventoryService.MaxInventorySlots, svc.Inventory.Count);
            Assert.IsFalse(svc.AddItem(InventoryService.MaxInventorySlots + 1));
            Assert.AreEqual(InventoryService.MaxInventorySlots, svc.Inventory.Count);
        }

        [Test]
        public void AddItem_StacksExistingWhenMobileBagFull()
        {
            var items = new List<ItemDefinition>();
            for (int i = 1; i <= InventoryService.MaxInventorySlots; i++)
                items.Add(Item(i, $"Item {i}", 28, i));

            var svc = new InventoryService(DbWith(items.ToArray()));
            for (int i = 1; i <= InventoryService.MaxInventorySlots; i++)
                Assert.IsTrue(svc.AddItem(i));

            Assert.IsTrue(svc.AddItem(1, 3));
            Assert.AreEqual(InventoryService.MaxInventorySlots, svc.Inventory.Count);
            Assert.AreEqual(4, svc.Inventory[0].count);
        }

        // --- AC#3: equip + stat preview ---

        [Test]
        public void Equip_UpdatesStatPreview()
        {
            var db = DbWith(Item(1, "Sword", 28, 10), Item(2, "Helm", 28, 5));
            var svc = new InventoryService(db);

            var preview1 = svc.Equip(EquipSlot.Weapon, 1);
            Assert.AreEqual(10, preview1[28]);

            var preview2 = svc.Equip(EquipSlot.Helmet, 2);
            Assert.AreEqual(15, preview2[28]); // 10 + 5 same attr code
        }

        [Test]
        public void Equip_SeparateAttrs_AccumulateIndependently()
        {
            var db = DbWith(Item(1, "Sword", 28, 10), Item(2, "Ring", 29, 7));
            var svc = new InventoryService(db);
            svc.Equip(EquipSlot.Weapon, 1);
            var preview = svc.Equip(EquipSlot.Ring, 2);
            Assert.AreEqual(10, preview[28]);
            Assert.AreEqual(7, preview[29]);
        }

        [Test]
        public void Unequip_RecomputesPreview()
        {
            var db = DbWith(Item(1, "Sword", 28, 10), Item(2, "Helm", 28, 5));
            var svc = new InventoryService(db);
            svc.Equip(EquipSlot.Weapon, 1);
            svc.Equip(EquipSlot.Helmet, 2);
            var preview = svc.Unequip(EquipSlot.Weapon);
            Assert.AreEqual(5, preview[28]);
        }

        [Test]
        public void Equip_ReplacesSlot()
        {
            var db = DbWith(Item(1, "Sword A", 28, 10), Item(2, "Sword B", 28, 25));
            var svc = new InventoryService(db);
            svc.Equip(EquipSlot.Weapon, 1);
            var preview = svc.Equip(EquipSlot.Weapon, 2); // same slot
            Assert.AreEqual(25, preview[28]); // replaced, not summed
        }

        [Test]
        public void PlayerEquipmentService_ItemToWeaponVariant_MapsPcMeleeRows()
        {
            Assert.AreEqual(MalePlayerSpriteCatalog.ShortWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(1));
            Assert.AreEqual(MalePlayerSpriteCatalog.ShortWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(21));
            Assert.AreEqual(MalePlayerSpriteCatalog.StaffWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(22));
            Assert.AreEqual(MalePlayerSpriteCatalog.StaffWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(41));
            Assert.AreEqual(MalePlayerSpriteCatalog.DualWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(42));
            Assert.AreEqual(MalePlayerSpriteCatalog.DualWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(71));
            Assert.AreEqual(MalePlayerSpriteCatalog.EmptyWeaponVariant, PlayerEquipmentService.ItemToWeaponVariant(999));
        }

        [Test]
        public void InventoryEquipWeapon_UpdatesEquipmentServiceAndControllerVisual()
        {
            var db = DbWith(Item(1, "Kiếm ngắn", 28, 10), Item(22, "Côn dài", 28, 20), Item(42, "Song kiếm", 28, 30));
            var equipment = new PlayerEquipmentService();
            var svc = new InventoryService(db, equipment);
            var go = new GameObject("InventoryVisualBridgeTest");
            try
            {
                var controller = go.AddComponent<SandboxPlayerController>();
                controller.followCameraEnabled = false;
                controller.allowKeyboardFallback = false;
                if (controller.visual is MalePlayerVisual maleVisual)
                {
                    maleVisual.spritesRootOverride = _stagingRoot;
                    maleVisual.RefreshActionParts(force: true);
                }
                svc.OnWeaponTypeChanged += controller.EquipWeapon;

                svc.Equip(EquipSlot.Weapon, 1);
                Assert.AreEqual(PcWeaponType.ShortWeapon, equipment.GetCurrentWeaponType());
                Assert.AreEqual(PcWeaponType.ShortWeapon, controller.EquippedWeapon);
                Assert.AreEqual(PcWeaponType.ShortWeapon, controller.visual.currentWeapon);
                Assert.IsTrue(controller.visual.HasAllRequiredParts, string.Join("\n", controller.visual.LastMissingRequiredParts));

                svc.Equip(EquipSlot.Weapon, 22);
                Assert.AreEqual(PcWeaponType.LongWeapon, equipment.GetCurrentWeaponType());
                Assert.AreEqual(PcWeaponType.LongWeapon, controller.EquippedWeapon);
                Assert.AreEqual(PcWeaponType.LongWeapon, controller.visual.currentWeapon);
                Assert.IsTrue(controller.visual.HasAllRequiredParts, string.Join("\n", controller.visual.LastMissingRequiredParts));

                svc.Equip(EquipSlot.Weapon, 42);
                Assert.AreEqual(PcWeaponType.DualWeapon, equipment.GetCurrentWeaponType());
                Assert.AreEqual(PcWeaponType.DualWeapon, controller.EquippedWeapon);
                Assert.AreEqual(PcWeaponType.DualWeapon, controller.visual.currentWeapon);
                Assert.IsTrue(controller.visual.HasAllRequiredParts, string.Join("\n", controller.visual.LastMissingRequiredParts));

                svc.Unequip(EquipSlot.Weapon);
                Assert.AreEqual(PcWeaponType.EmptyHand, equipment.GetCurrentWeaponType());
                Assert.AreEqual(PcWeaponType.EmptyHand, controller.EquippedWeapon);
                Assert.AreEqual(PcWeaponType.EmptyHand, controller.visual.currentWeapon);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // --- AC#4: missing icon diagnostic ---

        [Test]
        public void MissingIconItems_ListsUnresolvedIcons()
        {
            var db = DbWith(
                Item(1, "Good", 28, 10, iconResolved: true),
                Item(2, "NoIcon", 29, 5, iconResolved: false));
            var svc = new InventoryService(db);
            svc.AddItem(1);
            svc.AddItem(2);

            var missing = svc.MissingIconItems();
            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual(2, missing[0].itemId);
            Assert.IsTrue(svc.HasMissingIcon(2));
            Assert.IsFalse(svc.HasMissingIcon(1));
        }
    }
}
