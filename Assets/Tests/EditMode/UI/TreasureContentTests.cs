// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Treasure popup tests (EditMode, Popup)
// Verifies BtnTreasure content uses PC SPR sheet/hit zones, not a mobile mockup.
// -----------------------------------------------------------------------------
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI.Treasure;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class TreasureContentTests
    {
        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new TreasureContent(null, null);
            Assert.AreEqual("Bảo Vật", content.TitleVi);
        }

        [Test]
        public void ImplementsPcSheetHints_ForPopupShell()
        {
            var content = new TreasureContent(null, null);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            Assert.AreEqual(PopupChromeKind.PcTreasure, content.Chrome);
            Assert.AreEqual(563f, content.Width);
            Assert.AreEqual(476f, content.Height);
        }

        [Test]
        public void Build_CreatesPcMallSheetAndButtons()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("TreasurePanel"));
            Assert.IsNotNull(body.Q("MarketGoodsLayer"));
            Assert.IsNotNull(body.Q<Button>("SellType"));
            Assert.IsNotNull(body.Q<Button>("LeftBtn"));
            Assert.IsNotNull(body.Q<Button>("RightBtn"));
            Assert.IsNotNull(body.Q<Button>("ShoppingCart"));
            Assert.IsNotNull(body.Q<Button>("Close"));
            Assert.IsNull(body.Q("TreasureTabs"), "PC sheet must not show the old mobile/fake tab strip");
        }

        [Test]
        public void Build_BackendButtonsDisabledButCloseEnabled()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsFalse(body.Q<Button>("SellType").enabledSelf);
            Assert.IsFalse(body.Q<Button>("LeftBtn").enabledSelf);
            Assert.IsFalse(body.Q<Button>("RightBtn").enabledSelf);
            Assert.IsFalse(body.Q<Button>("ShoppingCart").enabledSelf);
            Assert.IsTrue(body.Q<Button>("Close").enabledSelf);
        }

        [Test]
        public void Build_NullServicesStillShowsPcGoodsFrame()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            var controls = body.Q("MarketGoodsLayer");
            Assert.IsNotNull(controls);
            Assert.GreaterOrEqual(controls.childCount, 1, "Null services should still render inside the PC MarketGoods frame");
        }

        [Test]
        public void OnShow_RefreshesWithoutServices()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            Assert.DoesNotThrow(() => content.OnShow());
            Assert.IsNotNull(body.Q("TreasureHuntStatus"));
        }
    }
}
