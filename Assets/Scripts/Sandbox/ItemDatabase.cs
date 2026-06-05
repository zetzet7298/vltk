// -----------------------------------------------------------------------------
// VLTK Mobile — Complete Item Database
// PC items from item_stat_contract.json with full stat mapping.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Complete item database loaded from PC item contracts.
    /// Provides lookup, search, and stat computation for all items.
    /// Falls back to a built-in catalog of common items when contract data is missing.
    /// </summary>
    public class ItemDatabase
    {
        private readonly Dictionary<int, ItemDefinition> _items = new();
        private readonly Dictionary<int, List<ItemDefinition>> _bySet = new();
        private readonly Dictionary<EquipSlot, List<ItemDefinition>> _bySlot = new();

        public IReadOnlyCollection<ItemDefinition> AllItems => _items.Values;
        public int Count => _items.Count;

        public ItemDatabase()
        {
            // Initialize slot caches
            foreach (EquipSlot slot in Enum.GetValues(typeof(EquipSlot)))
                _bySlot[slot] = new List<ItemDefinition>();

            LoadBuiltInCatalog();
        }

        /// <summary>Create from contract importer (when available).</summary>
        public ItemDatabase(ItemContractImporter importer) : this()
        {
            if (importer == null) return;
            foreach (var item in importer.Items)
                AddOrUpdate(item);
        }

        public ItemDefinition Resolve(int itemId)
        {
            _items.TryGetValue(itemId, out var def);
            return def;
        }

        public List<ItemDefinition> Search(string query)
        {
            var results = new List<ItemDefinition>();
            if (string.IsNullOrWhiteSpace(query))
            {
                results.AddRange(_items.Values);
                results.Sort((a, b) => a.itemId.CompareTo(b.itemId));
                return results;
            }

            string q = query.Trim();
            bool isId = int.TryParse(q, out var idQuery);

            foreach (var item in _items.Values)
            {
                if ((isId && item.itemId == idQuery) ||
                    (!string.IsNullOrEmpty(item.DisplayName) &&
                     item.DisplayName.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    results.Add(item);
                }
            }
            results.Sort((a, b) => a.itemId.CompareTo(b.itemId));
            return results;
        }

        public List<ItemDefinition> GetBySlot(EquipSlot slot)
        {
            return _bySlot.TryGetValue(slot, out var list) ? list : new List<ItemDefinition>();
        }

        public List<ItemDefinition> GetBySet(int setId)
        {
            return _bySet.TryGetValue(setId, out var list) ? list : new List<ItemDefinition>();
        }

        public void AddOrUpdate(ItemDefinition item)
        {
            if (item == null) return;
            _items[item.itemId] = item;

            if (item.setId > 0)
            {
                if (!_bySet.ContainsKey(item.setId))
                    _bySet[item.setId] = new List<ItemDefinition>();
                var setList = _bySet[item.setId];
                if (!setList.Exists(i => i.itemId == item.itemId))
                    setList.Add(item);
            }
        }

        // PC-derived stat attribute codes
        public const int ATTR_LIFE = 1;
        public const int ATTR_MANA = 2;
        public const int ATTR_STRENGTH = 10;
        public const int ATTR_DEXTERITY = 11;
        public const int ATTR_VITALITY = 12;
        public const int ATTR_ENERGY = 13;
        public const int ATTR_ATTACK = 20;
        public const int ATTR_DEFENSE = 21;
        public const int ATTR_ATTACK_SPEED = 22;
        public const int ATTR_MOVE_SPEED = 23;
        public const int ATTR_HIT_RATE = 30;
        public const int ATTR_DODGE = 31;
        public const int ATTR_CRITICAL = 32;
        public const int ATTR_FIRE_RES = 40;
        public const int ATTR_ICE_RES = 41;
        public const int ATTR_LIGHT_RES = 42;
        public const int ATTR_POISON_RES = 43;

        private void LoadBuiltInCatalog()
        {
            // Weapons - Short (swords/blades)
            AddBuiltin(1001, "Thanh Kiếm Sắt", "Kiếm sắt cơ bản", EquipSlot.Weapon, setId: 0,
                (ATTR_ATTACK, 15), (ATTR_HIT_RATE, 5));
            AddBuiltin(1002, "Đao Luyện", "Đao sắt", EquipSlot.Weapon, setId: 0,
                (ATTR_ATTACK, 18), (ATTR_ATTACK_SPEED, 3));
            AddBuiltin(1003, "Kiếm Thanh Phong", "Kiếm xanh", EquipSlot.Weapon, setId: 1,
                (ATTR_ATTACK, 28), (ATTR_HIT_RATE, 8), (ATTR_STRENGTH, 3));
            AddBuiltin(1004, "Đao Xích Thố", "Đao lửa", EquipSlot.Weapon, setId: 1,
                (ATTR_ATTACK, 32), (ATTR_ATTACK_SPEED, 5), (ATTR_FIRE_RES, 5));

            // Weapons - Long (staves/spears)
            AddBuiltin(1021, "Trượng Thiết", "Trượng sắt", EquipSlot.Weapon, setId: 0,
                (ATTR_ATTACK, 20), (ATTR_MANA, 30));
            AddBuiltin(1022, "Thương Băng", "Thương băng", EquipSlot.Weapon, setId: 2,
                (ATTR_ATTACK, 30), (ATTR_ICE_RES, 8), (ATTR_ENERGY, 5));

            // Weapons - Dual (paired weapons)
            AddBuiltin(1041, "Song Kiếm", "Song kiếm", EquipSlot.Weapon, setId: 0,
                (ATTR_ATTACK, 24), (ATTR_ATTACK_SPEED, 8), (ATTR_CRITICAL, 3));
            AddBuiltin(1042, "Song Đao", "Song đao", EquipSlot.Weapon, setId: 3,
                (ATTR_ATTACK, 26), (ATTR_ATTACK_SPEED, 10), (ATTR_DEXTERITY, 4));

            // Armor
            AddBuiltin(2001, "Áo Vải", "Giáp vải cơ bản", EquipSlot.Armor, setId: 0,
                (ATTR_DEFENSE, 10), (ATTR_VITALITY, 2));
            AddBuiltin(2002, "Áo Da", "Giáp da", EquipSlot.Armor, setId: 0,
                (ATTR_DEFENSE, 18), (ATTR_VITALITY, 4), (ATTR_DODGE, 3));
            AddBuiltin(2003, "Giáp Sắt", "Giáp sắt", EquipSlot.Armor, setId: 4,
                (ATTR_DEFENSE, 30), (ATTR_VITALITY, 8), (ATTR_STRENGTH, 3));
            AddBuiltin(2004, "Giáp Bạc", "Giáp bạc", EquipSlot.Armor, setId: 5,
                (ATTR_DEFENSE, 45), (ATTR_VITALITY, 12), (ATTR_STRENGTH, 5), (ATTR_DEFENSE, 5));

            // Helmets
            AddBuiltin(3001, "Mũ Vải", "Mũ vải", EquipSlot.Helmet, setId: 0,
                (ATTR_DEFENSE, 5), (ATTR_ENERGY, 2));
            AddBuiltin(3002, "Mũ Sắt", "Mũ sắt", EquipSlot.Helmet, setId: 4,
                (ATTR_DEFENSE, 12), (ATTR_VITALITY, 3));
            AddBuiltin(3003, "Mũ Bạc", "Mũ bạc", EquipSlot.Helmet, setId: 5,
                (ATTR_DEFENSE, 18), (ATTR_VITALITY, 5), (ATTR_HIT_RATE, 3));

            // Boots
            AddBuiltin(4001, "Giày Vải", "Giày vải", EquipSlot.Boots, setId: 0,
                (ATTR_DEFENSE, 3), (ATTR_MOVE_SPEED, 5));
            AddBuiltin(4002, "Giày Da", "Giày da", EquipSlot.Boots, setId: 0,
                (ATTR_DEFENSE, 8), (ATTR_MOVE_SPEED, 8), (ATTR_DODGE, 3));
            AddBuiltin(4003, "Giày Sắt", "Giày sắt", EquipSlot.Boots, setId: 4,
                (ATTR_DEFENSE, 15), (ATTR_MOVE_SPEED, 6), (ATTR_VITALITY, 3));

            // Necklaces
            AddBuiltin(5001, "Dây Chuyền Đồng", "Dây chuyền đồng", EquipSlot.Necklace, setId: 0,
                (ATTR_MANA, 20), (ATTR_ENERGY, 3));
            AddBuiltin(5002, "Dây Chuyền Bạc", "Dây chuyền bạc", EquipSlot.Necklace, setId: 5,
                (ATTR_MANA, 40), (ATTR_ENERGY, 5), (ATTR_LIFE, 30));

            // Rings
            AddBuiltin(6001, "Nhẫn Đồng", "Nhẫn đồng", EquipSlot.Ring, setId: 0,
                (ATTR_HIT_RATE, 5), (ATTR_CRITICAL, 2));
            AddBuiltin(6002, "Nhẫn Bạc", "Nhẫn bạc", EquipSlot.Ring, setId: 5,
                (ATTR_HIT_RATE, 8), (ATTR_CRITICAL, 4), (ATTR_ATTACK, 5));

            // Consumables
            AddBuiltin(7001, "Thuốc Hồi Máu", "Hồi 100 HP", 0,
                (ATTR_LIFE, 100));
            AddBuiltin(7002, "Thuốc Hồi Khí", "Hồi 80 MP", 0,
                (ATTR_MANA, 80));
            AddBuiltin(7003, "Thuốc Hồi Sinh", "Hồi toàn bộ HP/MP", 0,
                (ATTR_LIFE, 9999), (ATTR_MANA, 9999));
            AddBuiltin(7004, "Thuốc Tốc Độ", "Tăng tốc độ di chuyển", 0,
                (ATTR_MOVE_SPEED, 30));
        }

        private void AddBuiltin(int id, string name, string desc, EquipSlot slot, int setId, params (int attr, int val)[] stats)
        {
            var item = new ItemDefinition
            {
                itemId = id,
                nameRaw = name,
                nameNormalized = desc,
                setId = setId,
                refineLevel = 0,
                iconResolved = false,
            };

            foreach (var (attr, val) in stats)
            {
                item.statDeltas.Add(new ItemStatDelta
                {
                    ruleId = $"STAT_BASE_{attr}",
                    stage = ItemStatStage.Base,
                    attrCode = attr,
                    value = val,
                });
            }

            AddOrUpdate(item);

            // Cache by equip slot
            if (_bySlot.TryGetValue(slot, out var list))
            {
                list.Add(item);
            }
        }

        private void AddBuiltin(int id, string name, string desc, int setId, params (int attr, int val)[] stats)
        {
            AddBuiltin(id, name, desc, (EquipSlot)(-1), setId, stats);
        }
    }
}
