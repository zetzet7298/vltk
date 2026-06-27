// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info content tests (EditMode, Category "Popup")
// Spec REQ-5 (paperdoll bind), REQ-6 (stats bind), REQ-7 (Đánh giá placeholder),
// REQ-4 (tabs), REQ-9 (action buttons), REQ-10 (EditMode coverage).
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Backend.Dto;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI.CharacterInfo;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class CharacterInfoContentTests
    {
        private static ItemDefinition TestItem(int id, string name = "Test Item")
            => new ItemDefinition { itemId = id, nameNormalized = name };

        private static InventoryService InventoryWith(params ItemDefinition[] items)
        {
            var importer = new ItemContractImporter();
            importer.Import(new ItemContractBundle { items = new List<ItemDefinition>(items) });
            return new InventoryService(importer);
        }

        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new CharacterInfoContent(null, () => null);
            Assert.AreEqual("Thông Tin Nhân Vật", content.TitleVi);
        }

        [Test]
        public void Build_CreatesThreeTabs_AndDefaultIsTrangBi()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();

            content.Build(body);

            Assert.IsNotNull(body.Q("tab_thuoctinh"), "Thuộc tính tab button");
            Assert.IsNotNull(body.Q("tab_trangbi"), "Trang bị tab button");
            Assert.IsNotNull(body.Q("tab_danhgia"), "Đánh giá tab button");

            // REQ-4: Trang bị is the default tab → visible, others hidden.
            Assert.AreEqual(DisplayStyle.Flex, body.Q("TabBody_trangbi").style.display.value);
            Assert.AreEqual(DisplayStyle.None, body.Q("TabBody_thuoctinh").style.display.value);
            Assert.AreEqual(DisplayStyle.None, body.Q("TabBody_danhgia").style.display.value);
        }

        [Test]
        public void SwitchTab_TogglesVisibleBody()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            // tap Thuộc tính tab button
            body.Q<Button>("tab_thuoctinh").SimulateClick();

            Assert.AreEqual(DisplayStyle.Flex, body.Q("TabBody_thuoctinh").style.display.value);
            Assert.AreEqual(DisplayStyle.None, body.Q("TabBody_trangbi").style.display.value);
        }

        [Test, Category("Equipment")]
        public void Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty()
        {
            var equipment = new PlayerEquipmentService();
            // Equip visual slots (variant != default → equipped). Visual path must remain first.
            equipment.Equip(PlayerEquipSlot.Head, variant: 2, itemId: 100010);
            equipment.Equip(PlayerEquipSlot.Weapon, variant: 5, itemId: 100001);
            equipment.Equip(PlayerEquipSlot.Body, variant: 3, itemId: 100062);
            equipment.Equip(PlayerEquipSlot.Mount, variant: 16, itemId: 100200);

            var content = new CharacterInfoContent(equipment, () => null);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();   // re-read equipment

            Assert.IsTrue(body.Q("Slot_helmet").ClassListContains("equipped"), "helmet visual slot should remain equipped");
            Assert.IsTrue(body.Q("Slot_weapon").ClassListContains("equipped"), "weapon visual slot should remain equipped");
            Assert.IsTrue(body.Q("Slot_armor").ClassListContains("equipped"), "armor visual slot should remain equipped");
            Assert.IsTrue(body.Q("Slot_mount").ClassListContains("equipped"), "mount visual slot should remain equipped");

            // Gameplay-bound slots without equipped items are now empty, not framework.
            Assert.IsTrue(body.Q("Slot_ring").ClassListContains("empty"), "ring is bound gameplay slot and should be empty when unequipped");
            Assert.IsTrue(body.Q("Slot_pendant").ClassListContains("empty"), "pendant should be empty when unequipped");
            Assert.IsTrue(body.Q("Slot_trinket").ClassListContains("empty"), "trinket should be empty when unequipped");
            Assert.IsTrue(body.Q("Slot_mask").ClassListContains("empty"), "mask should be empty when unequipped");
        }

        [Test, Category("Equipment")]
        public void Paperdoll_HasReferenceSlotCount()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            // Final PR-2 paperdoll has 13 slots (12 original + ring2); all defined slots present.
            Assert.AreEqual(13, CharacterInfoPaperdoll.Slots.Count);
            Assert.IsNotNull(body.Q("Slot_weapon"));
            Assert.IsNotNull(body.Q("Slot_armor"));
            Assert.IsNotNull(body.Q("Slot_helmet"));
            Assert.IsNotNull(body.Q("Slot_mount"));
            Assert.IsNotNull(body.Q("Slot_mask"));
            Assert.IsNotNull(body.Q("Slot_pendant"));
            Assert.IsNotNull(body.Q("Slot_trinket2"));
            Assert.IsNotNull(body.Q("Slot_trinket"));
            Assert.IsNotNull(body.Q("Slot_ring2"));
        }

        [Test, Category("Equipment")]
        public void Paperdoll_TwoRings_BothPresent()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("Slot_ring"), "primary ring slot");
            Assert.IsNotNull(body.Q("Slot_ring2"), "PC-parity second ring slot");
        }

        [Test, Category("Equipment")]
        public void Paperdoll_SlotIdentifiers_FollowPcSemantics()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNull(body.Q("Slot_amulet"), "old sachet key must be renamed to pendant");
            Assert.IsNull(body.Q("Slot_charm"), "old charm key must be renamed to trinket2");
            Assert.IsNotNull(body.Q("Slot_pendant"), "pendant/sachet key");
            Assert.IsNotNull(body.Q("Slot_trinket2"), "second ornament key");
        }

        [Test, Category("Equipment")]
        public void Paperdoll_GameplaySlot_Equipped_ShowsEquippedClass()
        {
            var mask = TestItem(900001, "Mặt Nạ Test");
            var inventory = InventoryWith(mask);
            inventory.Equip(EquipSlot.Mask, mask.itemId);

            var content = new CharacterInfoContent(null, () => null, inventory);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            var maskCell = body.Q("Slot_mask");
            Assert.IsNotNull(maskCell);
            Assert.IsTrue(maskCell.ClassListContains("equipped"), "mask equipped in InventoryService should render equipped");
        }

        [Test, Category("Equipment")]
        public void Paperdoll_GameplaySlot_EquippedViaDict()
        {
            var container = new VisualElement();
            var item = TestItem(900002, "Mặt Nạ Dict");
            var equipped = new Dictionary<EquipSlot, ItemDefinition>
            {
                { EquipSlot.Mask, item }
            };

            CharacterInfoPaperdoll.Build(container, equipment: null, equippedItems: equipped);
            Assert.IsTrue(container.Q("Slot_mask").ClassListContains("equipped"));
            Assert.IsTrue(container.Q("Slot_pendant").ClassListContains("empty"));

            Assert.DoesNotThrow(() => CharacterInfoPaperdoll.Build(container, equipment: null, equippedItems: null));
            Assert.IsTrue(container.Q("Slot_mask").ClassListContains("empty"), "null dict should be safe empty state");
        }

        [Test]
        public void Stats_BindFromPlayerStateResponse()
        {
            var stats = new PlayerStateResponse
            {
                level = 42, strength = 35, dexterity = 25, vitality = 25, spirit = 15,
                money = 12345, transLife = 1, repute = 999,
            };
            var content = new CharacterInfoContent(null, () => stats);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.AreEqual("42", body.Q<Label>("Stat_level_Value").text);
            Assert.AreEqual("35", body.Q<Label>("Stat_strength_Value").text);
            Assert.AreEqual("12345", body.Q<Label>("Stat_money_Value").text);
            Assert.AreEqual("1", body.Q<Label>("Stat_transLife_Value").text);
        }

        [Test]
        public void Stats_WithNullProvider_ShowPlaceholders()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.AreEqual("--", body.Q<Label>("Stat_strength_Value").text);
        }

        [Test]
        public void DanhGiaTab_HasPlaceholderMessage()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            var placeholder = body.Q<Label>("Placeholder");
            Assert.IsNotNull(placeholder);
            Assert.IsTrue(placeholder.text.Contains("sắp ra mắt"),
                "Đánh giá tab must show a 'coming soon' placeholder (REQ-7)");
        }

        [Test]
        public void ActionButtons_AllPresent_NonDestructive()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            // REQ-9: Khóa / Đính / Tháo present and clickable without throwing.
            Assert.IsNotNull(body.Q("btn_lock"), "Khóa button");
            Assert.IsNotNull(body.Q("btn_embed"), "Đính button");
            Assert.IsNotNull(body.Q("btn_unequip"), "Tháo button");

            Assert.DoesNotThrow(() =>
            {
                body.Q<Button>("btn_lock").SimulateClick();
                body.Q<Button>("btn_embed").SimulateClick();
                body.Q<Button>("btn_unequip").SimulateClick();
            }, "action buttons must be non-destructive in slice 1");
        }

        [Test]
        public void OnShow_RefreshesPaperdollAfterEquip()
        {
            var equipment = new PlayerEquipmentService();
            var content = new CharacterInfoContent(equipment, () => null);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();
            Assert.IsTrue(body.Q("Slot_armor").ClassListContains("empty"));

            equipment.Equip(PlayerEquipSlot.Body, variant: 2, itemId: 100062);
            content.OnShow();   // re-read

            Assert.IsTrue(body.Q("Slot_armor").ClassListContains("equipped"),
                "OnShow must refresh paperdoll to reflect new equip");
        }
    }

    /// <summary>Simulates a button click without needing a real event loop.</summary>
    internal static class ButtonTestExt
    {
        // UIElements Button.clickable.Invoke is protected; invoke via reflection on the
        // Clickable instance so tests don't need a live input event loop.
        public static void SimulateClick(this Button btn)
        {
            var clickable = btn?.clickable;
            if (clickable == null) return;
            var invoke = clickable.GetType().GetMethod("Invoke",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            // Invoke(EventBase) needs an event; build a minimal ClickEvent.
            var evt = ClickEvent.GetPooled();
            evt.target = btn;
            invoke?.Invoke(clickable, new object[] { evt });
        }
    }
}
