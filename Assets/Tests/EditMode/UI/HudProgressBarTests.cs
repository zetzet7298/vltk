using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.UI;

namespace VLTK.Tests.UI
{
    [TestFixture]
    [Category("HUD")]
    public class HudProgressBarTests
    {
        [Test]
        public void Set_HalfFraction_SetsFillWidthTo50Percent()
        {
            var fill = new VisualElement { name = "Fill" };
            var bar = new HudProgressBar(fill, null);

            bar.Set(0.5f, 50, 100);

            Assert.AreEqual(new Length(50f, LengthUnit.Percent), fill.style.width.value);
        }

        [Test]
        public void Set_UpdatesLabelToCurrentSlashMax()
        {
            var label = new Label { name = "Text" };
            var bar = new HudProgressBar(null, label);

            bar.Set(0.75f, 6757, 8969);

            // vltkunity ProgressBar text format (recon §6b): "6757/8969"
            Assert.AreEqual("6757/8969", label.text);
        }

        [Test]
        public void Set_ClampsOverflowFractionTo100Percent()
        {
            var fill = new VisualElement { name = "Fill" };
            var bar = new HudProgressBar(fill, null);

            bar.Set(1.5f, 150, 100);

            Assert.AreEqual(new Length(100f, LengthUnit.Percent), fill.style.width.value);
        }

        [Test]
        public void Set_ClampsNegativeFractionTo0Percent()
        {
            var fill = new VisualElement { name = "Fill" };
            var bar = new HudProgressBar(fill, null);

            bar.Set(-0.5f, -10, 100);

            Assert.AreEqual(new Length(0f, LengthUnit.Percent), fill.style.width.value);
        }

        [Test]
        public void SetFraction_UpdatesWidthOnly()
        {
            var fill = new VisualElement { name = "Fill" };
            var label = new Label { name = "Text" };
            label.text = "unchanged";
            var bar = new HudProgressBar(fill, label);

            bar.SetFraction(0.25f);

            Assert.AreEqual(new Length(25f, LengthUnit.Percent), fill.style.width.value);
            Assert.AreEqual("unchanged", label.text);
        }

        [Test]
        public void Set_NullFill_DoesNotThrow()
        {
            var bar = new HudProgressBar(null, new Label());
            Assert.DoesNotThrow(() => bar.Set(0.5f, 50, 100));
        }
    }
}
