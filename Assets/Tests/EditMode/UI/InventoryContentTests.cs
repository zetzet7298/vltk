// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Inventory grid builder + content tests (EditMode, Popup)
// Spec analog REQ-5 (grid bind), REQ-8 (BtnItems), REQ-10 (EditMode coverage).
// Uses the same ItemContractImporter seeding pattern as InventoryServiceTests.
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI.Inventory;
using VLTK.UI.Popup;

namespace VLTK.Tests.UI
{
    [TestFixture, Category("Popup")]
    public class InventoryGridBuilderTests
    {
        [Test]
        public void Build_CreatesSixByTenGrid()
        {
            var grid = new VisualElement();
            InventoryGridBuilder.Build(grid, new List<InventoryEntry>(), filter: null);

            Assert.AreEqual(InventoryGridBuilder.TotalCells, grid.childCount,
                "grid must always render the full 6x10 cell count");
        }

        [Test]
        public void Build_EmptyCellsCarryEmptyClass()
        {
            var grid = new VisualElement();
            InventoryGridBuilder.Build(grid, new List<InventoryEntry>(), filter: null);

            Assert.IsTrue(grid[0].ClassListContains("empty"), "first cell empty when no items");
            Assert.AreEqual(InventoryGridBuilder.TotalCells,
                grid.Query(className: "empty").ToList().Count, "all cells empty");
        }

        [Test]
        public void Build_FilledCellsShowCountBadge_WhenStacked()
        {
            var entries = new List<InventoryEntry>
            {
                new() { item = Item(1, "Thuốc Hồi Sinh", genre: 8), count = 5 },
            };
            var grid = new VisualElement();
            InventoryGridBuilder.Build(grid, entries, filter: null);

            var first = grid[0];
            Assert.IsTrue(first.ClassListContains("filled"));
            Assert.IsNotNull(first.Q<Label>("CellCount"), "count badge present for stack>1");
            Assert.AreEqual("5", first.Q<Label>("CellCount").text);
        }

        [Test]
        public void Build_SingleItemHasNoCountBadge()
        {
            var entries = new List<InventoryEntry>
            {
                new() { item = Item(2, "Kiếm", genre: 1), count = 1 },
            };
            var grid = new VisualElement();
            InventoryGridBuilder.Build(grid, entries, filter: null);

            Assert.IsNull(grid[0].Q("CellCount"), "no count badge for count=1");
        }

        [Test]
        public void Filter_ReturnsOnlyMatchingCategory()
        {
            var entries = new List<InventoryEntry>
            {
                new() { item = Item(1, "Kiếm",   genre: 1) },   // Weapon
                new() { item = Item(2, "Thuốc",  genre: 8) },   // Medicament
                new() { item = Item(3, "Quặng",  genre: 9) },   // Material
            };
            var meds = InventoryGridBuilder.FilterEntries(entries, PcItemCategory.Medicament);
            Assert.AreEqual(1, meds.Count);
            Assert.AreEqual(2, meds[0].item.itemId);

            var all = InventoryGridBuilder.FilterEntries(entries, null);
            Assert.AreEqual(3, all.Count);
        }

        [Test]
        public void Build_WithCategoryFilter_OnlyShowsMatching()
        {
            var entries = new List<InventoryEntry>
            {
                new() { item = Item(1, "Kiếm",  genre: 1), count = 1 },
                new() { item = Item(2, "Thuốc", genre: 8), count = 1 },
                new() { item = Item(3, "Quặng", genre: 9), count = 1 },
            };
            var grid = new VisualElement();
            InventoryGridBuilder.Build(grid, entries, filter: PcItemCategory.Medicament);

            int filled = grid.Query(className: "filled").ToList().Count;
            Assert.AreEqual(1, filled, "only the Medicament cell filled");
        }

        private static ItemDefinition Item(int id, string name, int genre)
        {
            var item = new ItemDefinition
            {
                itemId = id,
                nameNormalized = name,
                itemGenre = genre,
                iconResolved = false,   // force fallback label path (no fabricated art)
            };
            return item;
        }
    }

    [TestFixture, Category("Popup")]
    public class InventoryContentTests
    {
        private ItemContractImporter DbWith(params ItemDefinition[] items)
        {
            var imp = new ItemContractImporter();
            var bundle = new ItemContractBundle { items = new List<ItemDefinition>(items) };
            imp.Import(bundle);
            return imp;
        }

        private static ItemDefinition Item(int id, string name, int genre)
        {
            var item = new ItemDefinition
            {
                itemId = id,
                nameNormalized = name,
                itemGenre = genre,
                iconResolved = false,
            };
            return item;
        }

        [Test]
        public void TitleVi_IsVietnamese()
        {
            var content = new InventoryContent(null);
            Assert.AreEqual("Hành Trang", content.TitleVi);
        }

        [Test]
        public void Build_CreatesPcSheetGridAndButtons()
        {
            var content = new InventoryContent(null);
            var body = new VisualElement();
            content.Build(body);

            var grid = body.Q("InvGrid");
            Assert.IsNotNull(body.Q("InventoryPanel"));
            Assert.IsNotNull(grid);
            Assert.AreEqual(InventoryGridBuilder.TotalCells, grid.childCount);
            Assert.IsNotNull(body.Q<Button>("MakeAdvBtn"));
            Assert.IsNotNull(body.Q<Button>("MarkPriceBtn"));
            Assert.IsNotNull(body.Q<Button>("MakeStallBtn"));
            Assert.IsNotNull(body.Q<Button>("OpenStatus"));
            Assert.IsNotNull(body.Q<Button>("Close"));
        }

        [Test]
        public void OnShow_PopulatesGridAndFooterFromInventory()
        {
            var db = DbWith(Item(1, "Kiếm", genre: 1), Item(2, "Thuốc", genre: 8));
            var svc = new InventoryService(db);
            svc.AddItem(1);
            svc.AddItem(2);

            var content = new InventoryContent(svc);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();

            var grid = body.Q("InvGrid");
            int filled = grid.Query(className: "filled").ToList().Count;
            Assert.AreEqual(2, filled, "both items shown in default Tất Cả tab");

            Assert.AreEqual("2/28", body.Q<Label>("SlotCount").text);
        }

        [Test]
        public void ChromeHint_UsesPcInventorySheetSize()
        {
            var content = new InventoryContent(null);

            Assert.AreEqual(PopupChromeKind.PcInventory, content.Chrome);
            Assert.AreEqual(214f, content.Width);
            Assert.AreEqual(454f, content.Height);
        }

        [Test]
        public void OnShow_RefreshesAfterAddItem()
        {
            var db = DbWith(Item(1, "Kiếm", genre: 1));
            var svc = new InventoryService(db);

            var content = new InventoryContent(svc);
            var body = new VisualElement();
            content.Build(body);
            content.OnShow();
            Assert.AreEqual("0/28", body.Q<Label>("SlotCount").text);

            svc.AddItem(1);
            content.OnShow();
            Assert.AreEqual("1/28", body.Q<Label>("SlotCount").text);
        }

    }
}
