// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info content tests (EditMode, Category "Popup")
// Spec REQ-5 (paperdoll bind), REQ-6 (stats bind), REQ-7 (Đánh giá placeholder),
// REQ-4 (tabs), REQ-9 (action buttons), REQ-10 (EditMode coverage).
// -----------------------------------------------------------------------------
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Backend.Dto;
using VLTK.Sandbox;
using VLTK.UI.CharacterInfo;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class CharacterInfoContentTests
    {
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

        [Test]
        public void Paperdoll_BindsRealEquipmentSlots_EquippedVsEmpty()
        {
            var equipment = new PlayerEquipmentService();
            // Equip weapon (variant != default → equipped)
            equipment.Equip(PlayerEquipSlot.Weapon, variant: 5, itemId: 100001);

            var content = new CharacterInfoContent(equipment, () => null);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();   // re-read equipment

            var weaponCell = body.Q("Slot_weapon");
            Assert.IsNotNull(weaponCell, "weapon paperdoll slot");
            Assert.IsTrue(weaponCell.ClassListContains("equipped"), "weapon should be marked equipped");

            var helmetCell = body.Q("Slot_helmet");
            Assert.IsNotNull(helmetCell);
            Assert.IsTrue(helmetCell.ClassListContains("empty"), "unequipped helmet should be empty");

            // Framework slots (Ring/Necklace/Belt/Boots) carry 'framework' class.
            var ringCell = body.Q("Slot_ring");
            Assert.IsNotNull(ringCell);
            Assert.IsTrue(ringCell.ClassListContains("framework"), "ring is framework slot");
        }

        [Test]
        public void Paperdoll_HasReferenceSlotCount()
        {
            var content = new CharacterInfoContent(null, () => null);
            var body = new VisualElement();
            content.Build(body);

            // Reference shows ~12 slots; all defined slots present.
            Assert.IsNotNull(body.Q("Slot_weapon"));
            Assert.IsNotNull(body.Q("Slot_armor"));
            Assert.IsNotNull(body.Q("Slot_helmet"));
            Assert.IsNotNull(body.Q("Slot_mount"));
            Assert.IsNotNull(body.Q("Slot_mask"));
            Assert.IsNotNull(body.Q("Slot_amulet"));
            Assert.IsNotNull(body.Q("Slot_charm"));
            Assert.IsNotNull(body.Q("Slot_trinket"));
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
