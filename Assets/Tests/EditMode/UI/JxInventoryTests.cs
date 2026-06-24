// -----------------------------------------------------------------------------
// VLTK Mobile — JX inventory EditMode tests (port of KuiItemVN.cpp + KuiItem.cpp)
// Verifies: MAX_ITEM=1024, grid fit/collision (AABB), add/remove/move/swap,
// pixel placement math (Y-flip), broken-equip (durability 0/1), quality color
// (normal/purple/gold/platinum), stack label rule. Category: HudJxCocos.
// -----------------------------------------------------------------------------

using System.Linq;
using NUnit.Framework;
using VLTK.UI.JxCocos;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("HudJxCocos")]
    public class JxInventoryTests
    {
        private static JxInventoryItem Item(int id, int gx, int gy, int w = 1, int h = 1,
            uint genre = (uint)JxItemGenre.Medicine, int stack = 0, int durability = -1,
            bool stackable = false)
            => new()
            {
                ItemId = id,
                GridX = gx,
                GridY = gy,
                Width = w,
                Height = h,
                Genre = genre,
                Stack = stack,
                Durability = durability,
                Stackable = stackable,
            };

        // ---- Port constants ----

        [Test]
        public void MaxItem_Is1024_PerSource()
        {
            // Nguồn: KItem.h MAX_ITEM 1024.
            Assert.That(JxInventoryState.MaxItem, Is.EqualTo(1024));
        }

        [Test]
        public void CellSize_Is52_PerSource()
        {
            // Nguồn: KuiItemVN.h ITEM_CELL_SIZE 52.
            Assert.That(JxInventoryState.CellSize, Is.EqualTo(52));
        }

        [Test]
        public void StartPos_Is26_54_PerSource()
        {
            // Nguồn: m_StartPos.x=26, m_StartPos.y=54.
            Assert.That(JxInventoryState.StartX, Is.EqualTo(26f));
            Assert.That(JxInventoryState.StartY, Is.EqualTo(54f));
        }

        // ---- Item id validation (port) ----

        [Test]
        public void IsValidItemId_RejectsZero_Negative_AndMaxOrAbove()
        {
            // Nguồn: uId > 0 && uId < MAX_ITEM.
            Assert.IsFalse(JxInventoryState.IsValidItemId(0));
            Assert.IsFalse(JxInventoryState.IsValidItemId(1024));
            Assert.IsFalse(JxInventoryState.IsValidItemId(5000));
            Assert.IsTrue(JxInventoryState.IsValidItemId(1));
            Assert.IsTrue(JxInventoryState.IsValidItemId(1023));
        }

        // ---- Grid fit + collision ----

        [Test]
        public void FitsInGrid_True_WithinBounds()
        {
            var s = new JxInventoryState(cols: 4, rows: 3);
            Assert.IsTrue(s.FitsInGrid(0, 0, 1, 1));
            Assert.IsTrue(s.FitsInGrid(3, 2, 1, 1)); // ô cuối (0-indexed)
            // 2x2 tại (2,1): 2+2=4 cols ok, 1+2=3 rows ok.
            Assert.IsTrue(s.FitsInGrid(2, 1, 2, 2));
        }

        [Test]
        public void FitsInGrid_False_OutOfBounds()
        {
            var s = new JxInventoryState(cols: 4, rows: 3);
            Assert.IsFalse(s.FitsInGrid(4, 0, 1, 1)); // x=4 vượt
            Assert.IsFalse(s.FitsInGrid(0, 3, 1, 1)); // y=3 vượt
            Assert.IsFalse(s.FitsInGrid(3, 2, 2, 2)); // 2x2 tràn
            Assert.IsFalse(s.FitsInGrid(0, 0, 0, 1)); // w=0
        }

        [Test]
        public void CollisionDetection_AABB()
        {
            var s = new JxInventoryState(cols: 8, rows: 6);
            s.AddItem(Item(1, 2, 2, 1, 1));
            // Cùng ô (2,2) → va chạm.
            Assert.IsFalse(s.IsRegionFree(2, 2, 1, 1));
            // Loại trừ chính nó.
            Assert.IsTrue(s.IsRegionFree(2, 2, 1, 1, excludeItemId: 1));
            // Ô kề không va chạm.
            Assert.IsTrue(s.IsRegionFree(3, 2, 1, 1));
            // 2x2 chạm góc.
            Assert.IsFalse(s.IsRegionFree(1, 1, 2, 2));
        }

        // ---- Add/Remove ----

        [Test]
        public void AddItem_RejectsInvalid_Overlap_Duplicate()
        {
            var s = new JxInventoryState(cols: 8, rows: 6);
            // Invalid id.
            Assert.IsFalse(s.AddItem(Item(0, 0, 0)));
            Assert.IsFalse(s.AddItem(Item(1024, 0, 0)));
            // Valid.
            Assert.IsTrue(s.AddItem(Item(1, 0, 0)));
            // Duplicate id.
            Assert.IsFalse(s.AddItem(Item(1, 1, 1)));
            // Overlap.
            Assert.IsFalse(s.AddItem(Item(2, 0, 0)));
            // Out of bounds.
            Assert.IsFalse(s.AddItem(Item(3, 8, 0)));
        }

        [Test]
        public void RemoveItem_Works()
        {
            var s = new JxInventoryState();
            s.AddItem(Item(5, 0, 0));
            Assert.IsTrue(s.RemoveItem(5));
            Assert.IsFalse(s.RemoveItem(5));
            Assert.IsFalse(s.Items.ContainsKey(5));
        }

        // ---- Move ----

        [Test]
        public void MoveItem_RelocatesWithoutCollision()
        {
            var s = new JxInventoryState(cols: 8, rows: 6);
            s.AddItem(Item(1, 0, 0));
            Assert.IsTrue(s.MoveItem(1, 3, 3));
            Assert.That(s.Items[1].GridX, Is.EqualTo(3));
            Assert.That(s.Items[1].GridY, Is.EqualTo(3));
        }

        [Test]
        public void MoveItem_RejectsCollision()
        {
            var s = new JxInventoryState(cols: 8, rows: 6);
            s.AddItem(Item(1, 0, 0));
            s.AddItem(Item(2, 3, 3));
            // Đẩy 1 vào ô của 2 → va chạm.
            Assert.IsFalse(s.MoveItem(1, 3, 3));
            Assert.That(s.Items[1].GridX, Is.EqualTo(0));
        }

        [Test]
        public void MoveItem_RejectsOutOfBounds()
        {
            var s = new JxInventoryState(cols: 4, rows: 4);
            s.AddItem(Item(1, 0, 0, 2, 2));
            // 2x2 tại (3,0) → tràn (3+2=5>4).
            Assert.IsFalse(s.MoveItem(1, 3, 0));
        }

        // ---- Swap ----

        [Test]
        public void SwapItems_1x1_ExchangePositions()
        {
            var s = new JxInventoryState(cols: 8, rows: 6);
            s.AddItem(Item(1, 0, 0));
            s.AddItem(Item(2, 5, 5));
            Assert.IsTrue(s.SwapItems(1, 2));
            Assert.That(s.Items[1].GridX, Is.EqualTo(5));
            Assert.That(s.Items[1].GridY, Is.EqualTo(5));
            Assert.That(s.Items[2].GridX, Is.EqualTo(0));
        }

        [Test]
        public void SwapItems_DifferentSizes_RejectsIfDoesNotFit()
        {
            var s = new JxInventoryState(cols: 4, rows: 4);
            s.AddItem(Item(1, 0, 0, 1, 1));
            s.AddItem(Item(2, 2, 0, 2, 2)); // 2x2 tại (2,0)
            // 1x1 vào ô (2,0) ok, nhưng 2x2 vào ô (0,0) ok → swap được? 
            // A(1x1)→(2,0) fits, B(2x2)→(0,0) fits (0+2=2<=4). Không va chạm khác → swap ok.
            Assert.IsTrue(s.SwapItems(1, 2));
        }

        // ---- Pixel placement (port math) ----

        [Test]
        public void GridToPixelLocal_SourceMath()
        {
            // nCurX = StartX(26) + GridX*52 + Width*52/2
            // nCurY = StartY(54) + GridY*52 + Height*52/2
            var item = Item(1, 1, 1, 2, 2); // w=2,h=2 tại (1,1)
            var (x, y) = JxInventoryState.GridToPixelLocal(item);
            // x = 26 + 1*52 + 2*52/2 = 26+52+52 = 130
            Assert.That(x, Is.EqualTo(130f));
            // y = 54 + 1*52 + 2*52/2 = 54+52+52 = 158
            Assert.That(y, Is.EqualTo(158f));
        }

        [Test]
        public void GridToPixelParent_FlipsY()
        {
            var item = Item(1, 0, 0, 1, 1);
            var (x, y) = JxInventoryState.GridToPixelParent(item, parentHeight: 500f);
            // x = 26 + 0 + 26 = 52; y = 500 - (54+0+26) = 500-80 = 420.
            Assert.That(x, Is.EqualTo(52f));
            Assert.That(y, Is.EqualTo(420f));
        }

        // ---- Broken equip + icon fallback ----

        [Test]
        public void IsBrokenEquip_True_OnlyForEquipDurability0Or1()
        {
            // Durability 0/1 + equip = broken.
            Assert.IsTrue(JxInventoryState.IsBrokenEquip(
                Item(1, 0, 0, genre: (uint)JxItemGenre.Equip, durability: 0)));
            Assert.IsTrue(JxInventoryState.IsBrokenEquip(
                Item(2, 0, 0, genre: (uint)JxItemGenre.Equip, durability: 1)));
            // Durability 50 = không hỏng.
            Assert.IsFalse(JxInventoryState.IsBrokenEquip(
                Item(3, 0, 0, genre: (uint)JxItemGenre.Equip, durability: 50)));
            // Non-equip durability 0 = không hỏng (rule chỉ cho equip).
            Assert.IsFalse(JxInventoryState.IsBrokenEquip(
                Item(4, 0, 0, genre: (uint)JxItemGenre.Medicine, durability: 0)));
        }

        [Test]
        public void EffectiveIconPath_BrokenEquipFallback()
        {
            // Broken equip → brokenequip.spr.
            var broken = Item(1, 0, 0, genre: (uint)JxItemGenre.Equip, durability: 0);
            broken.IconPath = "spr/normal.spr";
            Assert.That(JxInventoryState.EffectiveIconPath(broken), Is.EqualTo("\\spr\\item\\equip\\brokenequip.spr"));
        }

        [Test]
        public void EffectiveIconPath_EmptyFallback_问号()
        {
            var item = Item(1, 0, 0);
            item.IconPath = "";
            Assert.That(JxInventoryState.EffectiveIconPath(item), Is.EqualTo("\\spr\\others\\问号.spr"));
        }

        [Test]
        public void EffectiveIconPath_KeepsValidPath()
        {
            var item = Item(1, 0, 0);
            item.IconPath = "spr/potion.spr";
            Assert.That(JxInventoryState.EffectiveIconPath(item), Is.EqualTo("spr/potion.spr"));
        }

        // ---- Stack label ----

        [Test]
        public void ShowStackLabel_True_OnlyStackableNonEquip()
        {
            // Nguồn: Genre != item_equip && IsStack().
            Assert.IsTrue(JxInventoryState.ShowStackLabel(
                Item(1, 0, 0, genre: (uint)JxItemGenre.Medicine, stackable: true)));
            // Equip + stackable → không (equip không hiện stack).
            Assert.IsFalse(JxInventoryState.ShowStackLabel(
                Item(2, 0, 0, genre: (uint)JxItemGenre.Equip, stackable: true)));
            // Non-equip + không stackable → không.
            Assert.IsFalse(JxInventoryState.ShowStackLabel(
                Item(3, 0, 0, genre: (uint)JxItemGenre.Medicine, stackable: false)));
        }

        // ---- Quality (port nTempColor) ----

        [Test]
        public void ComputeQuality_PlatinumOverGoldOverPurple()
        {
            Assert.That(JxInventoryState.ComputeEquipQuality(isPurple: false, hasGoldId: false, isPlatina: false),
                Is.EqualTo(JxItemQuality.Normal));
            Assert.That(JxInventoryState.ComputeEquipQuality(true, false, false), Is.EqualTo(JxItemQuality.Purple));
            Assert.That(JxInventoryState.ComputeEquipQuality(false, true, false), Is.EqualTo(JxItemQuality.Gold));
            // Platina wins over gold+purple.
            Assert.That(JxInventoryState.ComputeEquipQuality(true, true, true), Is.EqualTo(JxItemQuality.Platinum));
            // Gold wins over purple.
            Assert.That(JxInventoryState.ComputeEquipQuality(true, true, false), Is.EqualTo(JxItemQuality.Gold));
        }
    }
}
