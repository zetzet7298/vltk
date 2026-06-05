// -----------------------------------------------------------------------------
// VLTK Mobile — Loot Drop Service
// Enemy drops from PC drop tables.
// Source: settings/droprate/npcdroprate*.ini (parsed by PcDropRateParser +
// DropRateRegistry). Fallback to the original hard-coded drop table when no
// PC table matches the NPC.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

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
        public float dropRate;
        public int minCount;
        public int maxCount;
        public int minSilver;
        public int maxSilver;
    }

    /// <summary>
    /// Service that computes loot drops when enemies are killed.
    /// Uses PC drop tables (via DropRateRegistry) with deterministic RNG.
    /// </summary>
    public class LootDropService
    {
        private readonly ItemDatabase _itemDb;
        private readonly List<DropTableEntry> _dropTable = new();
        private DropRateRegistry _registry;

        public LootDropService(ItemDatabase itemDb)
        {
            _itemDb = itemDb;
            LoadDefaultDropTable();
        }

        public void AttachRegistry(DropRateRegistry registry)
        {
            _registry = registry;
        }

        public DropRateRegistry Registry => _registry;

        public List<LootDropResult> ComputeDrops(int monsterTemplateId, int monsterLevel, int instanceId)
        {
            var results = new List<LootDropResult>();
            var rng = new System.Random(instanceId * 31 + monsterTemplateId * 7 + 12345);

            int silverBase = 5 + monsterLevel * 2;
            int silver = silverBase + rng.Next(0, silverBase / 2 + 1);
            results.Add(new LootDropResult { silverAmount = silver });

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

            if (_registry != null)
            {
                foreach (var drop in ResolveDrops(monsterTemplateId, monsterLevel, instanceId))
                {
                    results.Add(drop);
                }
            }

            float potionChance = Mathf.Min(0.4f, 0.1f + monsterLevel * 0.01f);
            if ((float)rng.NextDouble() < potionChance)
            {
                int potionId = monsterLevel > 10 ? 7001 : 7002;
                results.Add(new LootDropResult { itemId = potionId, count = 1 });
            }

            float equipChance = Mathf.Min(0.15f, 0.02f + monsterLevel * 0.005f);
            if ((float)rng.NextDouble() < equipChance && monsterLevel > 5)
            {
                var candidates = _itemDb.GetBySlot((EquipSlot)(rng.Next(0, 4)));
                if (candidates.Count > 0)
                {
                    var item = candidates[rng.Next(0, candidates.Count)];
                    results.Add(new LootDropResult { itemId = item.itemId, count = 1 });
                }
            }

            return results;
        }

        public List<LootDropResult> ResolveDrops(int npcTemplateId, int npcLevel, int instanceSeed)
        {
            var results = new List<LootDropResult>();
            if (_registry == null) return results;
            var rng = new System.Random(instanceSeed * 17 + npcTemplateId + npcLevel * 3 + 7);
            var tables = _registry.GetTablesForLevel(npcLevel);
            foreach (var table in tables)
            {
                int range = Mathf.Max(1, table.randRange);
                foreach (var entry in table.entries)
                {
                    if (entry.randRate <= 0) continue;
                    int roll = rng.Next(0, range);
                    if (roll >= entry.randRate) continue;
                    int count = 1;
                    int minL = Mathf.Max(1, entry.minItemLevel);
                    int maxL = Mathf.Max(minL, entry.maxItemLevel);
                    if (maxL > minL)
                        count = rng.Next(minL, maxL + 1);
                    if (entry.itemId <= 0) continue;
                    results.Add(new LootDropResult
                    {
                        itemId = entry.itemId,
                        count = Mathf.Max(1, count),
                    });
                }
            }
            return results;
        }

        public List<int> ResolveDrop(int npcTemplateId, int npcLevel)
        {
            var results = new List<int>();
            foreach (var r in ResolveDrops(npcTemplateId, npcLevel, npcTemplateId * 13 + 1))
                results.Add(r.itemId);
            return results;
        }

        private void LoadDefaultDropTable()
        {
            AddDrop(31, 7001, 0.15f, 1, 1, 0, 0);
            AddDrop(42, 7002, 0.12f, 1, 1, 0, 0);
            AddDrop(43, 7001, 0.10f, 1, 1, 0, 2);
            AddDrop(50, 2002, 0.05f, 1, 1, 0, 0);
            AddDrop(51, 3002, 0.04f, 1, 1, 0, 0);
            AddDrop(55, 1003, 0.03f, 1, 1, 0, 0);
            AddDrop(65, 1004, 0.02f, 1, 1, 0, 0);
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
