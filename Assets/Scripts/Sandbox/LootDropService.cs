// -----------------------------------------------------------------------------
// VLTK Mobile — Loot Drop Service
// Enemy drops from PC drop tables.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Result of a loot drop calculation.
    /// </summary>
    public struct LootDropResult
    {
        public int itemId;
        public int count;
        public int silverAmount;
    }

    /// <summary>
    /// Drop table entry from PC data.
    /// PC source: settings/dropitem.txt — maps monster template to loot.
    /// </summary>
    public class DropTableEntry
    {
        public int monsterTemplateId;
        public int itemId;
        public float dropRate;      // 0.0-1.0
        public int minCount;
        public int maxCount;
        public int minSilver;
        public int maxSilver;
    }

    /// <summary>
    /// Service that computes loot drops when enemies are killed.
    /// Uses PC drop tables with deterministic RNG (seeded by monster instance).
    /// </summary>
    public class LootDropService
    {
        private readonly ItemDatabase _itemDb;
        private readonly List<DropTableEntry> _dropTable = new();

        public LootDropService(ItemDatabase itemDb)
        {
            _itemDb = itemDb;
            LoadDefaultDropTable();
        }

        public List<LootDropResult> ComputeDrops(int monsterTemplateId, int monsterLevel, int instanceId)
        {
            var results = new List<LootDropResult>();
            var rng = new System.Random(instanceId * 31 + monsterTemplateId * 7 + 12345);

            // Silver drop (always)
            int silverBase = 5 + monsterLevel * 2;
            int silver = silverBase + rng.Next(0, silverBase / 2 + 1);
            results.Add(new LootDropResult { silverAmount = silver });

            // Item drops
            foreach (var entry in _dropTable)
            {
                if (entry.monsterTemplateId != monsterTemplateId) continue;

                float roll = (float)rng.NextDouble();
                if (roll > entry.dropRate) continue;

                int count = entry.minCount;
                if (entry.maxCount > entry.minCount)
                    count += rng.Next(0, entry.maxCount - entry.minCount + 1);

                if (entry.itemId > 0)
                {
                    results.Add(new LootDropResult
                    {
                        itemId = entry.itemId,
                        count = count,
                    });
                }

                if (entry.maxSilver > 0)
                {
                    int extraSilver = rng.Next(entry.minSilver, entry.maxSilver + 1);
                    results.Add(new LootDropResult { silverAmount = extraSilver });
                }
            }

            // Generic drops: small chance of potion based on level
            float potionChance = Mathf.Min(0.4f, 0.1f + monsterLevel * 0.01f);
            if ((float)rng.NextDouble() < potionChance)
            {
                int potionId = monsterLevel > 10 ? 7001 : 7002; // HP pot or MP pot
                results.Add(new LootDropResult { itemId = potionId, count = 1 });
            }

            // Rare equipment drops (level-scaled)
            float equipChance = Mathf.Min(0.15f, 0.02f + monsterLevel * 0.005f);
            if ((float)rng.NextDouble() < equipChance && monsterLevel > 5)
            {
                // Pick a random equipment appropriate for level
                var candidates = _itemDb.GetBySlot((EquipSlot)(rng.Next(0, 4)));
                if (candidates.Count > 0)
                {
                    var item = candidates[rng.Next(0, candidates.Count)];
                    results.Add(new LootDropResult { itemId = item.itemId, count = 1 });
                }
            }

            return results;
        }

        private void LoadDefaultDropTable()
        {
            // Default drop rates derived from PC dropitem.txt
            // Template: monsterTemplateId, itemId, dropRate, minCount, maxCount, minSilver, maxSilver

            // Ba Lăng enemies
            AddDrop(31, 7001, 0.15f, 1, 1, 0, 0); // Mèo vàng → HP pot
            AddDrop(42, 7002, 0.12f, 1, 1, 0, 0); // Hươu → MP pot
            AddDrop(43, 7001, 0.10f, 1, 2, 0, 0); // Heo → HP pot x2

            // Stronger enemies
            AddDrop(50, 2002, 0.05f, 1, 1, 0, 0); // Hổ → Giáp da
            AddDrop(51, 3002, 0.04f, 1, 1, 0, 0); // Gấu → Mũ sắt
            AddDrop(55, 1003, 0.03f, 1, 1, 0, 0); // Cướp núi → Kiếm xanh
            AddDrop(65, 1004, 0.02f, 1, 1, 0, 0); // Cướp đường → Đao lửa
        }

        private void AddDrop(int monsterId, int itemId, float rate, int min, int max, int minSilver, int maxSilver)
        {
            _dropTable.Add(new DropTableEntry
            {
                monsterTemplateId = monsterId,
                itemId = itemId,
                dropRate = rate,
                minCount = min,
                maxCount = max,
                minSilver = minSilver,
                maxSilver = maxSilver,
            });
        }
    }
}
