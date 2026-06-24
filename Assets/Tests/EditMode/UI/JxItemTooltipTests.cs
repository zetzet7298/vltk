// -----------------------------------------------------------------------------
// VLTK Mobile — JX item tooltip EditMode tests (port KuiItemdescVN.cpp)
// Verifies: durability label resolution (Forever/Broken/NeedFix/Life/Count
// boundary), mask exception, durability format string, price format, use/discard/
// shortcut action visibility. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxItemTooltipTests
    {
        // ---- Durability label resolution (port lines 644-665) ----

        [Test]
        public void Durability_Forever_WhenMinusOne()
        {
            // Nguồn: m_nCurrentDur == -1 → FOREVER.
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(-1, true, -1),
                Is.EqualTo(JxDurabilityLabel.Forever));
        }

        [Test]
        public void Durability_Broken_WhenZeroOrOne()
        {
            // Nguồn: 0 hoặc 1 → BROKEN (trang bi hong).
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(0, true, 0),
                Is.EqualTo(JxDurabilityLabel.Broken));
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(1, true, 0),
                Is.EqualTo(JxDurabilityLabel.Broken));
        }

        [Test]
        public void Durability_NeedFix_WhenLowNonMaskEquip()
        {
            // Nguồn: durability <=5 && >0, non-mask equip → NEEDFIX.
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(2, true, 0),
                Is.EqualTo(JxDurabilityLabel.NeedFix));
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(5, true, 2),
                Is.EqualTo(JxDurabilityLabel.NeedFix));
        }

        [Test]
        public void Durability_Count_ForMaskEvenWhenLow()
        {
            // Nguồn: equip + equip_mask(11) → COUNT (luôn, kể cả <=5).
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(2, true, 11),
                Is.EqualTo(JxDurabilityLabel.Count));
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(50, true, 11),
                Is.EqualTo(JxDurabilityLabel.Count));
        }

        [Test]
        public void Durability_Life_WhenAboveThresholdNonMask()
        {
            // Nguồn: >5, non-mask equip → LIFE.
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(6, true, 0),
                Is.EqualTo(JxDurabilityLabel.Life));
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(100, true, 5),
                Is.EqualTo(JxDurabilityLabel.Life));
        }

        [Test]
        public void Durability_NeedFix_NotForNonEquip()
        {
            // Non-equip: mask exception không áp dụng → vẫn NeedFix ở low dur.
            // isEquip=false → isMask=false → NeedFix.
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(3, false, 11),
                Is.EqualTo(JxDurabilityLabel.NeedFix));
            Assert.That(JxItemTooltipState.ResolveDurabilityLabel(10, false, 11),
                Is.EqualTo(JxDurabilityLabel.Life));
        }

        // ---- Durability format string (port szDurInfo "%s%d/%d") ----

        [Test]
        public void FormatDurability_Forever_JustLabel()
        {
            var data = new JxItemTooltipData { Durability = -1 };
            Assert.That(JxItemTooltipState.FormatDurability(data), Is.EqualTo("Vĩnh viễn"));
        }

        [Test]
        public void FormatDurability_Broken_JustLabel()
        {
            var data = new JxItemTooltipData { Durability = 0, MaxDurability = 50, Genre = JxItemGenre.Equip };
            Assert.That(JxItemTooltipState.FormatDurability(data), Is.EqualTo("Hỏng"));
        }

        [Test]
        public void FormatDurability_NeedFix_WithCurMax()
        {
            var data = new JxItemTooltipData { Durability = 3, MaxDurability = 50, Genre = JxItemGenre.Equip };
            // "Cần sửa: 3/50"
            Assert.That(JxItemTooltipState.FormatDurability(data), Is.EqualTo("Cần sửa: 3/50"));
        }

        [Test]
        public void FormatDurability_Life_WithCurMax()
        {
            var data = new JxItemTooltipData { Durability = 30, MaxDurability = 50, Genre = JxItemGenre.Equip };
            Assert.That(JxItemTooltipState.FormatDurability(data), Is.EqualTo("Tuổi thọ: 30/50"));
        }

        [Test]
        public void FormatDurability_Count_ForMask_WithCurMax()
        {
            var data = new JxItemTooltipData
            { Durability = 30, MaxDurability = 50, Genre = JxItemGenre.Equip, EquipDetailType = 11 };
            Assert.That(JxItemTooltipState.FormatDurability(data), Is.EqualTo("Số lượng: 30/50"));
        }

        [Test]
        public void FormatDurability_Threshold_Boundary()
        {
            // 5 → NeedFix, 6 → Life (boundary at <=5).
            var d5 = new JxItemTooltipData { Durability = 5, MaxDurability = 50, Genre = JxItemGenre.Equip };
            var d6 = new JxItemTooltipData { Durability = 6, MaxDurability = 50, Genre = JxItemGenre.Equip };
            Assert.That(JxItemTooltipState.FormatDurability(d5), Is.EqualTo("Cần sửa: 5/50"));
            Assert.That(JxItemTooltipState.FormatDurability(d6), Is.EqualTo("Tuổi thọ: 6/50"));
        }

        // ---- Price format ----

        [Test]
        public void FormatPrice_ThousandsSeparator()
        {
            Assert.That(JxItemTooltipState.FormatPrice(1234567), Is.EqualTo("1,234,567"));
            Assert.That(JxItemTooltipState.FormatPrice(0), Is.EqualTo("0"));
        }

        // ---- Action visibility (use/discard/shortcut) ----

        [Test]
        public void CanUse_True_ForConsumables()
        {
            Assert.IsTrue(JxItemTooltipState.CanUse(JxItemGenre.Medicine));
            Assert.IsTrue(JxItemTooltipState.CanUse(JxItemGenre.Task));
            Assert.IsTrue(JxItemTooltipState.CanUse(JxItemGenre.TownPortal));
            Assert.IsTrue(JxItemTooltipState.CanUse(JxItemGenre.Fusion));
        }

        [Test]
        public void CanUse_False_ForEquipMineMaterials()
        {
            Assert.IsFalse(JxItemTooltipState.CanUse(JxItemGenre.Equip));
            Assert.IsFalse(JxItemTooltipState.CanUse(JxItemGenre.Mine));
            Assert.IsFalse(JxItemTooltipState.CanUse(JxItemGenre.Materials));
        }

        [Test]
        public void CanDiscard_True_ForAllGenres()
        {
            // Nguồn: mọi genre đều vứt được.
            foreach (var g in System.Enum.GetValues(typeof(JxItemGenre)))
                Assert.IsTrue(JxItemTooltipState.CanDiscard((JxItemGenre)g));
        }

        [Test]
        public void CanShortcut_True_ForStackableNonEquip()
        {
            Assert.IsTrue(JxItemTooltipState.CanShortcut(JxItemGenre.Medicine, stackable: true));
            Assert.IsTrue(JxItemTooltipState.CanShortcut(JxItemGenre.Medicine, stackable: false)); // dùng được
        }

        [Test]
        public void CanShortcut_False_ForEquip()
        {
            Assert.IsFalse(JxItemTooltipState.CanShortcut(JxItemGenre.Equip, stackable: true));
            Assert.IsFalse(JxItemTooltipState.CanShortcut(JxItemGenre.Equip, stackable: false));
        }

        [Test]
        public void CanShortcut_False_ForMineMaterialsNotStackable()
        {
            // Mine/Materials: không dùng được + không stackable → không phím tắt.
            Assert.IsFalse(JxItemTooltipState.CanShortcut(JxItemGenre.Mine, stackable: false));
            Assert.IsFalse(JxItemTooltipState.CanShortcut(JxItemGenre.Materials, stackable: false));
            // Nhưng stackable thì được.
            Assert.IsTrue(JxItemTooltipState.CanShortcut(JxItemGenre.Mine, stackable: true));
        }
    }
}
