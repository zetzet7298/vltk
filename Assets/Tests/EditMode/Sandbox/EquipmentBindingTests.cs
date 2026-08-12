// -----------------------------------------------------------------------------
// VLTK Mobile — PR-1 Equipment binding tests (bind-accessory-equipment-slots)
// Category "Equipment" — run via category_names=["Equipment"] filter.
// Covers: PcItemCategory accessory members, DetailTypeToCategory mapping,
// EquipSlot enum extension, InventoryService read helpers, pendant loader fix.
// Strict TDD: written RED-first against non-existent API surface.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.Sandbox.ItemData;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("Equipment")]
    public class EquipmentBindingTests
    {
        // ── Test helpers ──────────────────────────────────────────────────

        private ItemDefinition Item(int id, string name = "Test", int attr = 1, int value = 1)
        {
            var item = new ItemDefinition { itemId = id, nameNormalized = name };
            item.statDeltas.Add(new ItemStatDelta
            {
                stage = ItemStatStage.Base,
                attrCode = attr,
                value = value
            });
            return item;
        }

        private ItemContractImporter DbWith(params ItemDefinition[] items)
        {
            var imp = new ItemContractImporter();
            var bundle = new ItemContractBundle { items = new List<ItemDefinition>(items) };
            imp.Import(bundle);
            return imp;
        }

        // ── T1/T2: PcItemCategory accessory members are equippable ────────

        [Test]
        public void PcItemCategory_Mask_IsEquippable()
        {
            Assert.IsTrue(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Mask));
            var m = EquipmentSlotMappingService.GetMapping(PcItemCategory.Mask);
            Assert.IsNotNull(m);
            Assert.AreEqual("Mặt Nạ", m.slotNameVi);
            Assert.IsTrue(m.isEquippable);
            Assert.AreEqual(1, m.maxStackSize);
        }

        [Test]
        public void PcItemCategory_Pendant_IsEquippable()
        {
            Assert.IsTrue(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Pendant));
            var m = EquipmentSlotMappingService.GetMapping(PcItemCategory.Pendant);
            Assert.IsNotNull(m);
            Assert.AreEqual("Hộ Thân Phù", m.slotNameVi);
            Assert.IsTrue(m.isEquippable);
            Assert.AreEqual(1, m.maxStackSize);
        }

        [Test]
        public void PcItemCategory_Trinket_IsEquippable()
        {
            Assert.IsTrue(EquipmentSlotMappingService.IsEquippable(PcItemCategory.Trinket));
            var m = EquipmentSlotMappingService.GetMapping(PcItemCategory.Trinket);
            Assert.IsNotNull(m);
            Assert.AreEqual("Bội Kiện", m.slotNameVi);
            Assert.IsTrue(m.isEquippable);
            Assert.AreEqual(1, m.maxStackSize);
        }

        // ── T3/T4: DetailTypeToCategory — PC EQUIPDETAILTYPE → PcItemCategory ──

        [Test]
        public void DetailTypeToCategory_Ring_ReturnsRing()
        {
            Assert.AreEqual(PcItemCategory.Ring, EquipmentSlotMappingService.DetailTypeToCategory(3));
        }

        [Test]
        public void DetailTypeToCategory_Necklace_ReturnsNecklace()
        {
            // PC amulet.txt DetailType 4 (equip_amulet) = Necklace ("Liên")
            Assert.AreEqual(PcItemCategory.Necklace, EquipmentSlotMappingService.DetailTypeToCategory(4));
        }

        [Test]
        public void DetailTypeToCategory_Pendant_ReturnsPendant()
        {
            Assert.AreEqual(PcItemCategory.Pendant, EquipmentSlotMappingService.DetailTypeToCategory(9));
        }

        [Test]
        public void DetailTypeToCategory_Mask_ReturnsMask()
        {
            Assert.AreEqual(PcItemCategory.Mask, EquipmentSlotMappingService.DetailTypeToCategory(11));
        }

        [Test]
        public void DetailTypeToCategory_Trinket_ReturnsTrinket()
        {
            Assert.AreEqual(PcItemCategory.Trinket, EquipmentSlotMappingService.DetailTypeToCategory(14));
        }

        [Test]
        public void DetailTypeToCategory_Unknown_ReturnsMaterial()
        {
            Assert.AreEqual(PcItemCategory.Material, EquipmentSlotMappingService.DetailTypeToCategory(99));
            Assert.AreEqual(PcItemCategory.Material, EquipmentSlotMappingService.DetailTypeToCategory(0));
        }

        // ── T5 (TRIANGULATE): ItemTypeToCategory axis independence ─────────

        [Test]
        public void ItemTypeToCategory_Int15_StillMaterial()
        {
            // PcItemCategory.Trinket=15 must NOT collide with ItemTypeToCategory's default fallback.
            // ItemTypeToCategory switches on ItemType integers (1–12), not PcItemCategory enum values.
            Assert.AreEqual(PcItemCategory.Material, EquipmentSlotMappingService.ItemTypeToCategory(15));
        }

        // ── T6/T7: EquipSlot enum extension (append-only after Mount=6) ───

        [Test]
        public void EquipSlot_ExistingValues_Unchanged()
        {
            Assert.AreEqual(0, (int)EquipSlot.Weapon);
            Assert.AreEqual(1, (int)EquipSlot.Helmet);
            Assert.AreEqual(2, (int)EquipSlot.Armor);
            Assert.AreEqual(3, (int)EquipSlot.Boots);
            Assert.AreEqual(4, (int)EquipSlot.Necklace);
            Assert.AreEqual(5, (int)EquipSlot.Ring);
            Assert.AreEqual(6, (int)EquipSlot.Mount);
        }

        [Test]
        public void EquipSlot_NewMembers_Appended_AfterMount()
        {
            Assert.AreEqual(7, (int)EquipSlot.Ring2);
            Assert.AreEqual(8, (int)EquipSlot.Mask);
            Assert.AreEqual(9, (int)EquipSlot.Pendant);
            Assert.AreEqual(10, (int)EquipSlot.Belt);
            Assert.AreEqual(11, (int)EquipSlot.Trinket);
            Assert.AreEqual(12, (int)EquipSlot.Trinket2);
        }

        // ── T8/T9: InventoryService read helpers (safe empty state) ────────

        [Test]
        public void InventoryService_GetEquipped_Empty_ReturnsNull()
        {
            var svc = new InventoryService(DbWith());
            Assert.IsNull(svc.GetEquipped(EquipSlot.Mask));
            Assert.IsNull(svc.GetEquipped(EquipSlot.Ring2));
            Assert.IsFalse(svc.IsSlotEquipped(EquipSlot.Mask));
            Assert.IsFalse(svc.IsSlotEquipped(EquipSlot.Trinket2));
        }

        [Test]
        public void InventoryService_Equipped_AccessorySlot_Readable()
        {
            var svc = new InventoryService(DbWith(Item(101, "Test Mask")));
            svc.Equip(EquipSlot.Mask, 101);
            var equipped = svc.GetEquipped(EquipSlot.Mask);
            Assert.IsNotNull(equipped);
            Assert.AreEqual(101, equipped.itemId);
            Assert.IsTrue(svc.IsSlotEquipped(EquipSlot.Mask));
        }

        [Test]
        public void InventoryService_Equip_Ring2_IndependentFromRing()
        {
            var svc = new InventoryService(DbWith(Item(1, "Ring A"), Item(2, "Ring B")));
            svc.Equip(EquipSlot.Ring2, 2);
            Assert.IsTrue(svc.IsSlotEquipped(EquipSlot.Ring2));
            Assert.IsFalse(svc.IsSlotEquipped(EquipSlot.Ring));
            Assert.AreEqual(2, svc.GetEquipped(EquipSlot.Ring2).itemId);
            Assert.IsNull(svc.GetEquipped(EquipSlot.Ring));
        }

        // ── T10/T11: Pendant loader fallback fix (7→9) ────────────────────

        [Test]
        public void PcItemBatchLoader_PendantFallback_IsNine()
        {
            var items = new List<ItemDefinition>
            {
                new ItemDefinition { detailType = 0, itemGenre = 0 }
            };
            PcItemBatchLoader.ApplyCategoryIds(items, 800000, "pendant");
            Assert.AreEqual(9, items[0].detailType,
                "pendant fallback must be 9 (equip_pendant), not 7 (equip_helm)");
        }

        [Test]
        public void PcItemBatchLoader_Pendant_NotClassifiedAsHelm()
        {
            var items = new List<ItemDefinition>
            {
                new ItemDefinition { detailType = 0, itemGenre = 0 }
            };
            PcItemBatchLoader.ApplyCategoryIds(items, 800000, "pendant");
            Assert.AreEqual(9, items[0].detailType);
            var category = EquipmentSlotMappingService.DetailTypeToCategory(items[0].detailType);
            Assert.AreEqual(PcItemCategory.Pendant, category);
        }

        // ── add-pc-mask-and-shipin-parsers: parser + particularType guards ──

        [Test]
        public void PcMaskItemParser_ParseRow_ReadsDetailTypeAndPreservesZeroParticularType()
        {
            var cols = new string[PcMaskItemParser.MinColumns];
            cols[0] = "Mặt nạ thử";
            cols[1] = "0";
            cols[2] = "11";
            cols[3] = "0";
            cols[4] = @"\spr\item\equip\mask\yrmj_01.spr";
            var item = PcMaskItemParser.ParseRow(cols, 1);
            Assert.IsNotNull(item);
            Assert.AreEqual(0, item.itemGenre);
            Assert.AreEqual(11, item.detailType);
            Assert.AreEqual(0, item.particularType);
            Assert.IsNotNull(item.iconSourceId);
        }

        [Test]
        public void PcShipinItemParser_ParseRow_ReadsTrinketDetailType()
        {
            var cols = new string[PcShipinItemParser.MinColumns];
            cols[0] = "Trang sức thử";
            cols[1] = "0";
            cols[2] = "14";
            cols[3] = "0";
            cols[4] = @"\spr\mel\trangsuctanthu.spr";
            var item = PcShipinItemParser.ParseRow(cols, 1);
            Assert.IsNotNull(item);
            Assert.AreEqual(0, item.itemGenre);
            Assert.AreEqual(14, item.detailType);
            Assert.AreEqual(PcItemCategory.Trinket, EquipmentSlotMappingService.DetailTypeToCategory(item.detailType));
            Assert.IsNotNull(item.iconSourceId);
        }

        [Test]
        public void PcItemBatchLoader_MaskParticularTypeZero_IsPreserved()
        {
            var items = new List<ItemDefinition>
            {
                new ItemDefinition { itemGenre = 0, detailType = 11, particularType = 0 }
            };
            PcItemBatchLoader.ApplyCategoryIds(items, 1500000, "mask");
            Assert.AreEqual(0, items[0].particularType,
                "mask.txt uses ParticularType=0 as a valid PC key and must not be row-indexed");
        }

        [Test]
        public void PcItemBatchLoader_ShipinParticularTypeZero_FallsBackToUniqueRows()
        {
            var items = new List<ItemDefinition>
            {
                new ItemDefinition { itemGenre = 0, detailType = 14, particularType = 0 },
                new ItemDefinition { itemGenre = 0, detailType = 14, particularType = 0 },
            };
            PcItemBatchLoader.ApplyCategoryIds(items, 1600000, "shipin");
            Assert.AreEqual(1, items[0].particularType);
            Assert.AreEqual(2, items[1].particularType);
        }
    }
}
