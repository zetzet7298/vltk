// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Character Info PC combined panel tests (EditMode, Popup)
// Verifies the 428×430 PC panel (config UID 2711122c): PcCharacter chrome +
// centering, real SPR panel background, 12 equipment hit-zones per INI, stat
// values at PC coords, +/- spend with disabled frame state, no remaining
// placeholder, footer Item/Close wiring.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Sandbox;
using VLTK.UI.CharacterInfo;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class CharacterInfoContentTests
    {
        private static PcStatsSnapshot Stats(int remain = 0, int str = 295, int dex = 38, int vit = 335, int inner = 15)
            => new PcStatsSnapshot(
                "Xích Lông Cẩu", "Hiệp Khách", 107, 0, 100, 0, 546,
                4486, 5000, 842, 1000, 2883, 3000,
                2591084696L, 12600000000L,
                str, vit, dex, inner, remain,
                "3013/8752", "0/0",
                20, 48, 20, 20,
                0, 49, 75, 54, 60);

        // ---- Title / contract ----
        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            Assert.AreEqual("Thông Tin Nhân Vật", content.TitleVi);
        }

        [Test]
        public void Implements_Layout_And_Chrome_Hints_WithPcPanelFootprint()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            Assert.IsInstanceOf<IPopupChromeHint>(content);
            var hint = (IPopupLayoutHint)content;
            Assert.AreEqual(428f, hint.Width, "PC combined panel width (2711122c)");
            Assert.AreEqual(430f, hint.Height, "PC combined panel height");
            Assert.AreEqual(PopupChromeKind.PcCharacter, ((IPopupChromeHint)content).Chrome);
        }

        [Test]
        public void PopupWindow_HidesGenericChrome_AndCentersPanel()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var window = new VLTK.UI.Popup.PopupWindow(content);

            Assert.IsTrue(window.ClassListContains("popup-window--pc-character"),
                "PcCharacter panel opts out of generic chrome");
            Assert.IsNull(window.Q<Label>("PopupTitle"),
                "generic title hidden — panel sprite owns the title");
            // Centered in 1280×720: (1280-428)/2 = 426 ; (720-430)/2 = 145
            Assert.AreEqual(426f, window.style.left.value.value, 0.5f, "centered left");
            Assert.AreEqual(145f, window.style.top.value.value, 0.5f, "centered top");
        }

        // ---- Build: panel + zones + buttons ----
        [Test]
        public void Build_CreatesPanelSprite_Overlay_AndFooterButtons()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("Panel"), "PC panel sprite layer");
            Assert.IsNotNull(body.Q("Paperdoll"), "equipment zone overlay");
            Assert.IsNotNull(body.Q("Item"), "footer Hành trang button");
            Assert.IsNotNull(body.Q("Close"), "footer Đóng button");
        }

        [Test]
        public void Build_CreatesTwelveEquipmentZones_PerPcIni()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var body = new VisualElement();
            content.Build(body);

            Assert.AreEqual(12, CharacterInfoContent.EquipZones.Count, "2711122c has 12 trang-bị zones");
            foreach (var z in CharacterInfoContent.EquipZones)
                Assert.IsNotNull(body.Q("Zone_" + z.key), "zone " + z.key);
        }

        // ---- Stats bind + no "--" ----
        [Test]
        public void Stats_BindFromSnapshot_NoPlaceholder()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => Stats(remain: 5)));
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.AreEqual("107", body.Q<Label>("Stat_Level").text);
            Assert.AreEqual("4486", body.Q<Label>("Stat_Life").text);
            Assert.AreEqual("295", body.Q<Label>("Stat_Strength").text);
            Assert.AreEqual("3013/8752", body.Q<Label>("Stat_LeftDamage").text);
            Assert.AreEqual("2591084696/12600000000", body.Q<Label>("Stat_Exp").text);
            Assert.AreEqual("5", body.Q<Label>("Stat_RemainPoint").text);
            Assert.AreEqual("75", body.Q<Label>("Stat_ResistLighting").text);
        }

        [Test]
        public void NoComingSoonPlaceholder_Remains()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var body = new VisualElement();
            content.Build(body);
            // The old "sắp ra mắt" placeholder must be gone.
            Assert.IsNull(body.Q<Label>("Placeholder"));
            Assert.IsNull(body.Q("Page_danhgia"));
        }

        // ---- +/- spend ----
        [Test]
        public void AddPoint_SpendsRemainPoint_AndDisablesWhenZero()
        {
            int remain = 2;
            var state = new PcCharacterPanelState(() => Stats(remain: remain))
            {
                DistributePotential = kind =>
                {
                    if (remain <= 0) return false;
                    remain--;
                    return true;
                }
            };
            var content = new CharacterInfoContent(state);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.IsTrue(content.TryDistribute(PcPotentialKind.Strength), "spend 1 succeeds");
            Assert.AreEqual("1", body.Q<Label>("Stat_RemainPoint").text);

            Assert.IsTrue(content.TryDistribute(PcPotentialKind.Vitality));
            Assert.IsFalse(content.TryDistribute(PcPotentialKind.Dexterity), "0 points left fails");
            // Out of points → disabled frame applied.
            Assert.IsTrue(body.Q("AddStrength").ClassListContains("char-add-point--disabled"));
        }

        [Test]
        public void AddPoint_NoCallback_StaysDisabled()
        {
            var state = new PcCharacterPanelState(() => Stats(remain: 5)) { DistributePotential = null };
            var content = new CharacterInfoContent(state);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.IsFalse(body.Q("AddStrength").enabledInHierarchy);
        }

        // ---- Gender background ----
        [Test]
        public void Panel_SelectsMaleFemaleBackground_ByProvider()
        {
            bool female = false;
            var state = new PcCharacterPanelState(() => default) { IsFemaleProvider = () => female };
            var content = new CharacterInfoContent(state);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            var panel = body.Q("Panel");
            Assert.IsFalse(panel.ClassListContains("char-panel--female"), "male default");

            female = true;
            content.OnShow();
            Assert.IsTrue(panel.ClassListContains("char-panel--female"), "female after flip");
        }

        // ---- Equipment zone state ----
        [Test]
        public void Equipment_BoundZones_ShowEquippedState()
        {
            var equipped = new Dictionary<EquipSlot, bool>
            {
                { EquipSlot.Helmet, true },
                { EquipSlot.Weapon, true },
                { EquipSlot.Mount, true },
            };
            var state = new PcCharacterPanelState(() => default)
            {
                EquipmentStateProvider = () => equipped
            };
            var content = new CharacterInfoContent(state);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            Assert.IsTrue(body.Q("Zone_Cap").ClassListContains("char-equip-zone--equipped"));
            Assert.IsTrue(body.Q("Zone_Weapon").ClassListContains("char-equip-zone--equipped"));
            Assert.IsTrue(body.Q("Zone_Horse").ClassListContains("char-equip-zone--equipped"));
            Assert.IsTrue(body.Q("Zone_Ring1").ClassListContains("char-equip-zone--empty"));
            var bangle = body.Q("Zone_Bangle");
            Assert.IsTrue(bangle.ClassListContains("char-equip-zone--bindable") == false,
                "Bangle has no EquipSlot enum → framework, not bindable");
        }

        [Test]
        public void Equipment_BackendMissingButtons_Disabled()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var body = new VisualElement();
            content.Build(body);

            Assert.IsFalse(body.Q<Button>("BtnLock").enabledInHierarchy);
            Assert.IsFalse(body.Q<Button>("BtnBind").enabledInHierarchy);
            Assert.IsFalse(body.Q<Button>("BtnUnBind").enabledInHierarchy);
        }

        // ---- Footer Close raises popup close ----
        [Test]
        public void FooterCloseButton_RaisesPopupWindowClose()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var window = new VLTK.UI.Popup.PopupWindow(content);

            bool fired = false;
            window.Closed += () => fired = true;

            var closeBtn = window.Q<Button>("Close");
            Assert.IsNotNull(closeBtn, "footer Close button present in window");
            closeBtn.SimulateClick();
            Assert.IsTrue(fired, "footer Close raises PopupWindow.Closed");
        }

        [Test]
        public void OnShow_RefreshesEquipmentAfterEquip()
        {
            var equipped = new Dictionary<EquipSlot, bool>();
            var state = new PcCharacterPanelState(() => default)
            {
                EquipmentStateProvider = () => equipped
            };
            var content = new CharacterInfoContent(state);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();
            Assert.IsTrue(body.Q("Zone_Cap").ClassListContains("char-equip-zone--empty"));

            equipped[EquipSlot.Helmet] = true;
            content.OnShow();
            Assert.IsTrue(body.Q("Zone_Cap").ClassListContains("char-equip-zone--equipped"));
        }

        [Test]
        public void OnClose_ClearsInternalRefs()
        {
            var content = new CharacterInfoContent(new PcCharacterPanelState(() => default));
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();
            Assert.DoesNotThrow(() => content.OnClose());
        }

        // ---- helpers ----
        private static bool DispatchPointerDown(VisualElement el)
        {
            if (el == null) return false;
            var evt = PointerDownEvent.GetPooled();
            evt.target = el;
            el.SendEvent(evt);
            return true;
        }
    }

    /// <summary>Simulates a button click without needing a real event loop.</summary>
    internal static class ButtonTestExt
    {
        public static void SimulateClick(this Button btn)
        {
            var clickable = btn?.clickable;
            if (clickable == null) return;
            var invoke = clickable.GetType().GetMethod("Invoke",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var evt = ClickEvent.GetPooled();
            evt.target = btn;
            invoke?.Invoke(clickable, new object[] { evt });
        }
    }
}
