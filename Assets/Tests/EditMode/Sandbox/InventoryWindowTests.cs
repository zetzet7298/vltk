using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using VLTK.UI;
using VLTK.Sandbox;
using VLTK.Model;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Port verification for the inventory window ("Hành Trang" / PC 物品 Open([[items]])).
    /// Verifies the spec values trace to PC INI, the mobile 4×7 override binds
    /// InventoryService data, tier colors match 7bfc9072.ini, and the bag button toggles the window.
    /// </summary>
    public class InventoryWindowTests
    {
        // ── Spec parity (PC 05ea8560 / dc11ac12 / 7bfc9072) ──────────────────

        [Test]
        public void Spec_PcGridIs6x10_60Slots()
        {
            Assert.AreEqual(6, InventoryWindowPcSpec.PcGridColumns);
            Assert.AreEqual(10, InventoryWindowPcSpec.PcGridRows);
            Assert.AreEqual(60, InventoryWindowPcSpec.PcSlotCount);
            Assert.AreEqual(60, InventoryPanelService.PcGridSlotCount);
            Assert.AreEqual(1, InventoryWindowPcSpec.PcUnitBorder);
        }

        [Test]
        public void Spec_MobileOverrideIs4x7_28Slots()
        {
            Assert.AreEqual(4, InventoryWindowPcSpec.GridColumns);
            Assert.AreEqual(7, InventoryWindowPcSpec.GridRows);
            Assert.AreEqual(28, InventoryWindowPcSpec.SlotCount);
            Assert.AreEqual(28, InventoryPanelService.GridSlotCount);
            Assert.AreEqual(112, InventoryWindowPcSpec.GridWidth);
            Assert.AreEqual(196, InventoryWindowPcSpec.GridHeight);
            Assert.AreEqual(1, InventoryWindowPcSpec.UnitBorder);
        }

        [Test]
        public void Spec_OpenCommandMatchesPc()
        {
            Assert.AreEqual("Open([[items]])", InventoryWindowPcSpec.PcOpenCommand);
            Assert.AreEqual("Player_Items", InventoryWindowPcSpec.PcButtonClassType);
            Assert.AreEqual("dc11ac12", InventoryWindowPcSpec.PcToolbarUid);
            Assert.AreEqual("05ea8560", InventoryWindowPcSpec.PcWindowUid);
            Assert.AreEqual(@"\spr\Ui3\道具\daojumianban.spr", InventoryWindowPcSpec.PcBackgroundSpr);
            Assert.AreEqual("16503a96", InventoryWindowPcSpec.PcBackgroundSpriteUid);
        }

        [Test]
        public void Spec_WindowGeometryMatchesPcIni()
        {
            Assert.AreEqual(214, InventoryWindowPcSpec.WindowWidth);
            Assert.AreEqual(474, InventoryWindowPcSpec.WindowHeight);
            Assert.AreEqual(24, InventoryWindowPcSpec.PcGridLeft);
            Assert.AreEqual(72, InventoryWindowPcSpec.PcGridTop);
            Assert.AreEqual(168, InventoryWindowPcSpec.PcGridWidth);
            Assert.AreEqual(280, InventoryWindowPcSpec.PcGridHeight);
            Assert.AreEqual(53, InventoryWindowPcSpec.MoneyLeft);
            Assert.AreEqual(353, InventoryWindowPcSpec.MoneyTop);
            Assert.AreEqual(142, InventoryWindowPcSpec.CloseLeft);
            Assert.AreEqual(414, InventoryWindowPcSpec.CloseTop);
        }

        [Test]
        public void Spec_TierColorsMatchPcIni()
        {
            // 7bfc9072.ini exact RGB values.
            AssertRgb(InventoryWindowPcSpec.TierWhite, 255, 255, 255);
            AssertRgb(InventoryWindowPcSpec.TierBlue, 51, 102, 250);
            AssertRgb(InventoryWindowPcSpec.TierPurple, 188, 64, 255);
            AssertRgb(InventoryWindowPcSpec.TierGold, 243, 194, 90);
            AssertRgb(InventoryWindowPcSpec.TierRed, 255, 51, 51);
        }

        [Test]
        public void Spec_FrameAndMoneyColorsMatchPcIni()
        {
            // 05ea8560 [Settings] + [Money].
            AssertRgb(InventoryWindowPcSpec.FrameBorderColor, 100, 80, 30);
            AssertRgb(InventoryWindowPcSpec.FrameBgColor, 243, 194, 70);
            AssertRgb(InventoryWindowPcSpec.MoneyColor, 255, 217, 78);
        }

        [Test]
        public void Spec_TierColorMapping()
        {
            AssertRgb(InventoryWindowPcSpec.TierColor(0), 255, 255, 255);
            AssertRgb(InventoryWindowPcSpec.TierColor(1), 51, 102, 250);
            AssertRgb(InventoryWindowPcSpec.TierColor(2), 188, 64, 255);
            AssertRgb(InventoryWindowPcSpec.TierColor(3), 243, 194, 90);
            AssertRgb(InventoryWindowPcSpec.TierColor(4), 255, 51, 51);
        }

        // ── Snapshot bound to runtime InventoryService ───────────────────────

        [Test]
        public void BuildSnapshot_AlwaysHasMobile28Slots()
        {
            var snap = InventoryPanelService.BuildGridSnapshot((InventoryService)null, 1);
            Assert.AreEqual(28, snap.totalSlots);
            Assert.AreEqual(28, snap.rows.Count);
            Assert.AreEqual(0, snap.usedSlots);
        }

        [Test]
        public void BuildSnapshot_BindsHeldItems()
        {
            var db = DbWith(
                Item(10, "Đao Gỗ", refine: 0, setId: 0),
                Item(20, "Giáp Tinh Luyện", refine: 3, setId: 0),
                Item(30, "Nhẫn Bộ Trang", refine: 0, setId: 5));
            var inv = new InventoryService(db);
            inv.AddItem(10, 1);
            inv.AddItem(20, 1);
            inv.AddItem(30, 2);

            var snap = InventoryPanelService.BuildGridSnapshot(inv, 1);
            Assert.AreEqual(3, snap.usedSlots);
            Assert.AreEqual(10, snap.rows[0].itemId);
            Assert.AreEqual("Đao Gỗ", snap.rows[0].itemName);
            Assert.AreEqual(0, snap.rows[0].itemQuality, "no refine/set -> white");
            Assert.AreEqual(1, snap.rows[1].itemQuality, "refine 3 -> blue");
            Assert.AreEqual(3, snap.rows[2].itemQuality, "set piece -> gold tier");
            Assert.AreEqual(2, snap.rows[2].count);
            // Trailing slots empty.
            Assert.AreEqual(0, snap.rows[27].itemId);
        }

        [Test]
        public void ResolveQuality_Tiers()
        {
            Assert.AreEqual(0, InventoryPanelService.ResolveQuality(Item(1, "a", 0, 0)));
            Assert.AreEqual(1, InventoryPanelService.ResolveQuality(Item(1, "a", 3, 0)));
            Assert.AreEqual(2, InventoryPanelService.ResolveQuality(Item(1, "a", 7, 0)));
            Assert.AreEqual(3, InventoryPanelService.ResolveQuality(Item(1, "a", 0, 9)));
            Assert.AreEqual(0, InventoryPanelService.ResolveQuality(null));
        }

        // ── HUD controller toggle + populate ─────────────────────────────────

        [Test]
        public void Controller_ToggleInventory_ShowsAndHides()
        {
            var go = new GameObject("InvHudTest");
            try
            {
                var hud = go.AddComponent<GameHudController>();
                var invWindow = new VisualElement { name = "InventoryWindow" };
                invWindow.AddToClassList("hidden");
                var invFrame = new VisualElement { name = "InventoryFrame" };
                var invGrid = new ScrollView { name = "InventoryGrid" };
                var invMoney = new Label { name = "InventoryMoney" };
                invWindow.Add(invFrame);
                invWindow.Add(invGrid);
                invWindow.Add(invMoney);

                SetField(hud, "_invWindow", invWindow);
                SetField(hud, "_invFrame", invFrame);
                SetField(hud, "_invGrid", invGrid);
                SetField(hud, "_invMoney", invMoney);

                Assert.IsFalse(hud.IsInventoryVisible);

                hud.ToggleInventory();
                Assert.IsTrue(hud.IsInventoryVisible);
                Assert.AreEqual(28, hud.InventorySlotCount, "grid populated with 28 mobile slots");

                hud.ToggleInventory();
                Assert.IsFalse(hud.IsInventoryVisible);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Controller_ItemsPcHitProxy_OpensInventoryAtBakedPcButton()
        {
            var go = new GameObject("InvHudHitProxyTest");
            try
            {
                var hud = go.AddComponent<GameHudController>();
                var root = new VisualElement { name = "GameHud" };
                var bottom = new VisualElement { name = "BottomPanel" };
                root.Add(bottom);

                var invWindow = new VisualElement { name = "InventoryWindow" };
                invWindow.AddToClassList("hidden");
                var invGrid = new ScrollView { name = "InventoryGrid" };
                var invMoney = new Label { name = "InventoryMoney" };
                SetField(hud, "_invWindow", invWindow);
                SetField(hud, "_invGrid", invGrid);
                SetField(hud, "_invMoney", invMoney);

                InvokePrivate(hud, "RegisterInventoryPcHitProxy", root);
                var proxy = bottom.Q("BtnItemsPcHitProxy");
                Assert.IsNotNull(proxy, "PC Túi đồ hit proxy must exist over the baked bottom-bar icon");
                Assert.AreEqual(PickingMode.Position, proxy.pickingMode);
                Assert.AreEqual(611f * 1280f / 1024f, proxy.style.left.value.value, 0.01f);
                Assert.AreEqual((728f - 680f) * 82f / 89f, proxy.style.top.value.value, 0.01f);

                Assert.IsFalse(hud.IsInventoryVisible);
                var evt = PointerDownEvent.GetPooled();
                evt.target = proxy;
                proxy.SendEvent(evt);
                Assert.IsTrue(hud.IsInventoryVisible);
                Assert.AreEqual(28, hud.InventorySlotCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void Controller_PopulateInventory_RendersSlotsAndMoney()
        {
            var go = new GameObject("InvHudTest2");
            try
            {
                var hud = go.AddComponent<GameHudController>();
                var invGrid = new ScrollView { name = "InventoryGrid" };
                var invMoney = new Label { name = "InventoryMoney" };
                SetField(hud, "_invGrid", invGrid);
                SetField(hud, "_invMoney", invMoney);

                var db = DbWith(Item(10, "Đao Gỗ", 0, 0));
                var inv = new InventoryService(db);
                inv.AddItem(10, 5);
                var snap = InventoryPanelService.BuildGridSnapshot(inv, 1, gold: 0, silver: 1234);

                hud.PopulateInventory(snap);
                Assert.AreEqual(28, hud.InventorySlotCount);
                Assert.AreEqual("Bạc: 1234", invMoney.text);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ── helpers ──────────────────────────────────────────────────────────

        private static void AssertRgb(InventoryWindowPcSpec.Rgb c, int r, int g, int b)
        {
            Assert.AreEqual(r, c.r);
            Assert.AreEqual(g, c.g);
            Assert.AreEqual(b, c.b);
        }

        private static void SetField(GameHudController hud, string name, object value)
        {
            var f = typeof(GameHudController).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"Field {name} not found");
            f.SetValue(hud, value);
        }

        private static void InvokePrivate(GameHudController hud, string name, params object[] args)
        {
            var method = typeof(GameHudController).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"Method {name} not found");
            method.Invoke(hud, args);
        }

        private static ItemDefinition Item(int id, string name, int refine, int setId)
        {
            return new ItemDefinition
            {
                itemId = id,
                nameNormalized = name,
                refineLevel = refine,
                setId = setId,
                iconResolved = true,
            };
        }

        private static ItemContractImporter DbWith(params ItemDefinition[] items)
        {
            var imp = new ItemContractImporter();
            imp.Import(new ItemContractBundle { items = new List<ItemDefinition>(items) });
            return imp;
        }
    }
}
