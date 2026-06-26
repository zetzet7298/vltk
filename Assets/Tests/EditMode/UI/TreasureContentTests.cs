// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Treasure popup tests (EditMode, Popup)
// Verifies BtnTreasure content uses PC-derived manifests and Vietnamese labels.
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
        public void ImplementsLayoutHint_ForPopupShell()
        {
            var content = new TreasureContent(null, null);
            Assert.IsInstanceOf<IPopupLayoutHint>(content);
            var hint = (IPopupLayoutHint)content;
            Assert.AreEqual(520f, hint.Width);
            Assert.AreEqual(520f, hint.Height);
        }

        [Test]
        public void Build_CreatesThreePcSections()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            Assert.IsNotNull(body.Q("TreasureTabs"));
            var tabs = body.Q("TreasureTabs").Children();
            int count = 0;
            foreach (var _ in tabs) count++;
            Assert.AreEqual(3, count, "Kỳ Trân Các / Giỏ Hàng / Rương Báu tabs are shown");
        }

        [Test]
        public void Build_PopulatesPcControlManifestRows()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            var controls = body.Q("TreasureControlList");
            Assert.IsNotNull(controls);
            Assert.GreaterOrEqual(controls.childCount, 10, "PC control manifest rows should be visible");
            Assert.AreEqual("Nạp thẻ", controls[0].Q<Label>("ControlLabel").text);
            Assert.AreEqual("9e5f75d1 / PrePaid", controls[0].Q<Label>("ControlSource").text);
        }

        [Test]
        public void OnShow_RefreshesWithoutServices()
        {
            var content = new TreasureContent(null, null);
            var body = new VisualElement();
            content.Build(body);

            Assert.DoesNotThrow(() => content.OnShow());
            Assert.IsNotNull(body.Q("TreasureSummaryList"));
        }
    }
}
