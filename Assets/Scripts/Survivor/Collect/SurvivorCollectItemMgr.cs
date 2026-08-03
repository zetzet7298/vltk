using System.Collections.Generic;

namespace VLTK.Survivor
{
    /// <summary>Kết quả một item rơi (mgr trả về; runtime spawn gem/visual theo Amount/BronID).</summary>
    public struct DropResult
    {
        public int ItemID;
        public DropOutputType OutputType;
        public int Amount; // từ DropEntry.Param1
        public int BronID;

        public DropResult(int itemId, DropOutputType outputType, int amount, int bronId)
        {
            ItemID = itemId;
            OutputType = outputType;
            Amount = amount;
            BronID = bronId;
        }
    }

    /// <summary>
    /// SurvivorCollectItemMgr — drop khi die + bonus đợt. Class thuần (không scene),
    /// nhận DropTableSO + System.Random (seed cố định để test deterministic).
    /// parity-shape dhcd:
    ///  - drop khi die = WaveRefresh.SpawnMonsterNormal → MonsterCreateParam
    ///    (DropItemPoolID/DropItemCount/DropItemRatio, research/wave-system.md §2e-4)
    ///  - TriggerWave = bonus drop khi wave trigger (wave clear reward, rate ép = 1)
    ///  - TestRate = roll xác suất theo rate (RateTest shape)
    /// Runtime (orchestrator) hook: monster die → RollActorDrop(poolId) → spawn item per DropResult;
    /// wave trigger → RollWaveBonus(poolId).
    ///
    /// ponytail: MERGE GEM ĐƯỢC BỎ (ticket 32 cho phép bỏ + rationale).
    /// Rationale: magnet kéo gem về trong vài giây (radius 1.6/speed 8) + GemLifetime 10s
    /// tự hủy → gem không tích tụ ngoài tầm nhặt; merge cần quét O(n²) mỗi frame trong khi
    /// cap quái P1 nhỏ. Khi nào cần lại: gem đồng thời &gt; 200 → pooling + merge bucket
    /// 1 lần/frame, không phải per-gem O(n²).
    /// </summary>
    public sealed class SurvivorCollectItemMgr
    {
        public readonly DropTableSO Table;

        public SurvivorCollectItemMgr(DropTableSO table)
        {
            Table = table;
        }

        /// <summary>Drop khi quái chết: mỗi entry trong pool roll DropRate độc lập.</summary>
        public List<DropResult> RollActorDrop(int poolId, System.Random rng)
        {
            return RollPool(poolId, rng, forceRate: false);
        }

        /// <summary>Bonus đợt (parity TriggerWave): mọi entry trong pool rơi (rate ép = 1).</summary>
        public List<DropResult> RollWaveBonus(int poolId, System.Random rng)
        {
            return RollPool(poolId, rng, forceRate: true);
        }

        /// <summary>Roll xác suất — parity TestRate (rate 0..1, rng deterministic theo seed).</summary>
        public static bool TestRate(float rate, System.Random rng)
        {
            return rng.NextDouble() < rate;
        }

        private List<DropResult> RollPool(int poolId, System.Random rng, bool forceRate)
        {
            var result = new List<DropResult>();
            if (Table == null) return result;
            var entries = Table.ForPool(poolId);
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (!forceRate && !TestRate(e.DropRate, rng)) continue;
                int count = e.RollCount(rng);
                for (int k = 0; k < count; k++)
                    result.Add(new DropResult(e.ItemID, e.OutputType, e.Param1, e.BronID));
            }
            return result;
        }
    }
}
