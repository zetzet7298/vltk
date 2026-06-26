// -----------------------------------------------------------------------------
// VLTK Mobile — HUD-003 Inventory grid builder (slice 2)
// 6×10 grid from PC INI [ItemBox] geometry (verified 94a9b42e/b49267df ini).
// Cell = icon + count badge; empty cells render faint. Category filter via
// EquipmentSlotMappingService.ItemTypeToCategory(itemGenre).
// ADR-I2 (PC geometry), ADR-I3 (mobile filter), ADR-I5 (icon resolver injection).
// -----------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine.UIElements;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.UI.Inventory
{
    /// <summary>Resolves an item's icon into a VisualElement (sprite/texture).</summary>
    public interface IItemIconResolver
    {
        /// <summary>True when the item has a usable icon (else fallback to label).</summary>
        bool TryResolve(ItemDefinition item, out UnityEngine.Sprite sprite);
    }

    /// <summary>Builds the 6×10 inventory grid from PC geometry. Pure-presentational.</summary>
    public static class InventoryGridBuilder
    {
        public const int Columns = 6;
        public const int Rows = 10;
        public const int TotalCells = Columns * Rows;

        /// <summary>
        /// Build <paramref name="container"/> as the grid. Up to <c>TotalCells</c> cells;
        /// filled cells show icon/count, the rest render empty.
        /// </summary>
        public static void Build(
            VisualElement container,
            IReadOnlyList<InventoryEntry> entries,
            PcItemCategory? filter,
            IItemIconResolver iconResolver = null)
        {
            container.Clear();
            container.AddToClassList("inv-grid");

            var filtered = FilterEntries(entries, filter);

            for (int i = 0; i < TotalCells; i++)
            {
                var cell = new VisualElement { name = "InvCell_" + i };
                cell.AddToClassList("inv-cell");

                if (i < filtered.Count)
                {
                    var entry = filtered[i];
                    cell.AddToClassList("filled");
                    PopulateCell(cell, entry, iconResolver);
                }
                else
                {
                    cell.AddToClassList("empty");
                }
                container.Add(cell);
            }
        }

        /// <summary>Filter inventory entries by category (null/None = show all).</summary>
        public static List<InventoryEntry> FilterEntries(IReadOnlyList<InventoryEntry> entries, PcItemCategory? filter)
        {
            var result = new List<InventoryEntry>();
            if (entries == null) return result;
            foreach (var e in entries)
            {
                if (e?.item == null) continue;
                if (!filter.HasValue) { result.Add(e); continue; }   // null = all
                if (EquipmentSlotMappingService.ItemTypeToCategory(e.item.itemGenre) == filter.Value)
                    result.Add(e);
            }
            return result;
        }

        private static void PopulateCell(VisualElement cell, InventoryEntry entry, IItemIconResolver iconResolver)
        {
            var item = entry.item;

            // Icon (sprite) if resolvable, else a VI category-label fallback (no fabricated art).
            UnityEngine.Sprite sprite = null;
            if (iconResolver != null && iconResolver.TryResolve(item, out sprite) && sprite != null)
            {
                var icon = new VisualElement { name = "CellIcon" };
                icon.AddToClassList("inv-cell-icon");
                icon.style.backgroundImage = new StyleBackground(sprite);
                cell.Add(icon);
            }
            else
            {
                var cat = EquipmentSlotMappingService.GetMapping(
                    EquipmentSlotMappingService.ItemTypeToCategory(item.itemGenre));
                var label = new Label(cat?.slotNameVi ?? "Đồ") { name = "CellFallback" };
                label.AddToClassList("inv-cell-fallback");
                cell.Add(label);
            }

            // Count badge (only when stack > 1).
            if (entry.count > 1)
            {
                var badge = new Label(entry.count.ToString(
                    System.Globalization.CultureInfo.InvariantCulture)) { name = "CellCount" };
                badge.AddToClassList("inv-cell-count");
                cell.Add(badge);
            }
        }
    }
}
