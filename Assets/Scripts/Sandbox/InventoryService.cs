using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Equipment slot (subset of PC equip slots, sandbox scope).</summary>
    public enum EquipSlot
    {
        Weapon = 0,
        Helmet = 1,
        Armor = 2,
        Boots = 3,
        Necklace = 4,
        Ring = 5,
    }

    /// <summary>A stack of an item held in the test inventory.</summary>
    public class InventoryEntry
    {
        public ItemDefinition item;
        public int count;
    }

    /// <summary>
    /// M5.2 — Sandbox inventory/equipment tools. Pure C# (no MonoBehaviour) so it is
    /// fully EditMode-testable. Searches the item database (AC#1), adds items to a
    /// test inventory (AC#2), equips items and previews the resulting character stat
    /// totals (AC#3), and surfaces a missing-icon diagnostic (AC#4). A MonoBehaviour
    /// GM Items tab drives this and renders the results.
    /// </summary>
    public class InventoryService
    {
        private readonly ItemContractImporter _db;
        private readonly List<InventoryEntry> _inventory = new();
        private readonly Dictionary<EquipSlot, ItemDefinition> _equipped = new();
        private readonly PlayerEquipmentService _equipment;

        public event Action<PcWeaponType> OnWeaponTypeChanged;

        public InventoryService(ItemContractImporter db, PlayerEquipmentService equipment = null)
        {
            _db = db;
            _equipment = equipment;
        }

        public IReadOnlyList<InventoryEntry> Inventory => _inventory;
        public IReadOnlyDictionary<EquipSlot, ItemDefinition> Equipped => _equipped;

        /// <summary>AC#1 — search the item database by id or name substring.</summary>
        public List<ItemDefinition> Search(string query)
        {
            var results = new List<ItemDefinition>();
            if (_db == null) return results;

            bool empty = string.IsNullOrWhiteSpace(query);
            string q = empty ? "" : query.Trim();
            bool isId = int.TryParse(q, out var idQuery);

            foreach (var item in _db.Items)
            {
                if (empty
                    || (isId && item.itemId == idQuery)
                    || (!string.IsNullOrEmpty(item.DisplayName)
                        && item.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    results.Add(item);
                }
            }
            results.Sort((a, b) => a.itemId.CompareTo(b.itemId));
            return results;
        }

        /// <summary>AC#2 — add an item (by id) to the test inventory.</summary>
        public bool AddItem(int itemId, int count = 1)
        {
            var item = _db?.Resolve(itemId);
            if (item == null)
            {
                SubsystemLog.Warn("Inventory", $"AddItem: unknown item {itemId}");
                return false;
            }
            var existing = _inventory.Find(e => e.item.itemId == itemId);
            if (existing != null) existing.count += count;
            else _inventory.Add(new InventoryEntry { item = item, count = count });
            return true;
        }

        /// <summary>AC#3 — equip an item into a slot; returns the recomputed stat preview.</summary>
        public Dictionary<int, int> Equip(EquipSlot slot, int itemId)
        {
            var item = _db?.Resolve(itemId);
            if (item == null)
            {
                SubsystemLog.Warn("Inventory", $"Equip: unknown item {itemId}");
                return StatPreview();
            }
            _equipped[slot] = item;
            if (slot == EquipSlot.Weapon)
            {
                int variant = PlayerEquipmentService.ItemToWeaponVariant(itemId);
                _equipment?.Equip(PlayerEquipSlot.Weapon, variant, itemId);
                OnWeaponTypeChanged?.Invoke(PlayerEquipmentService.WeaponVariantToType(variant));
            }
            return StatPreview();
        }

        public Dictionary<int, int> Unequip(EquipSlot slot)
        {
            _equipped.Remove(slot);
            if (slot == EquipSlot.Weapon)
            {
                _equipment?.Unequip(PlayerEquipSlot.Weapon);
                OnWeaponTypeChanged?.Invoke(PcWeaponType.EmptyHand);
            }
            return StatPreview();
        }

        /// <summary>
        /// AC#3 — character stat preview: sum of all equipped items' stat deltas by
        /// attr code (base + refine + set-bonus stages applied).
        /// </summary>
        public Dictionary<int, int> StatPreview()
        {
            var totals = new Dictionary<int, int>();
            foreach (var item in _equipped.Values)
            {
                foreach (var d in item.statDeltas)
                {
                    totals.TryGetValue(d.attrCode, out var cur);
                    totals[d.attrCode] = cur + d.value;
                }
            }
            return totals;
        }

        /// <summary>AC#4 — items whose icon is missing, for the GM diagnostic.</summary>
        public List<ItemDefinition> MissingIconItems()
        {
            var missing = new List<ItemDefinition>();
            foreach (var entry in _inventory)
                if (!entry.item.iconResolved)
                    missing.Add(entry.item);
            return missing;
        }

        public bool HasMissingIcon(int itemId)
        {
            var item = _db?.Resolve(itemId);
            return item != null && !item.iconResolved;
        }
    }
}
