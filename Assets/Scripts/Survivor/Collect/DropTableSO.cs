using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Loại item rơi. Own-design (dhcd không expose): XP = gem level-up,
    /// Gold = tiền (ticket 13 shop), Heal/Magnet/Bomb = supply (D2, chưa cần P1).
    /// </summary>
    public enum DropOutputType
    {
        Xp,
        Gold,
        Heal,
        Magnet,
        Bomb,
    }

    /// <summary>
    /// Một dòng drop. Schema shape ý tưởng từ CollectItemPoolConfig
    /// (PoolID/ItemID/OutputType/Param1/Param2/BronID — field names giữ parity),
    /// giá trị own. DropRate/CountMin/CountMax/Weight = own (dhcd dùng
    /// DropItemPoolID/DropItemCount/DropItemRatio per monster — shape tương đương
    /// qua research/wave-system.md §2b/2c).
    /// </summary>
    [System.Serializable]
    public sealed class DropEntry
    {
        public int PoolID;                 // parity CollectItemPoolConfig.PoolID — nhóm drop
        public int ItemID;                 // parity ItemID — id riêng của dòng
        public DropOutputType OutputType;  // parity OutputType (own enum)
        public int Param1;                 // parity Param1 — amount (xp/gold/heal...)
        public int Param2;                 // parity Param2 — reserved (0); dùng sau nếu cần duration
        public int BronID;                 // parity BronID — id visual/icon item (0 = proxy màu)
        [Range(0f, 1f)] public float DropRate = 0.5f; // own: xác suất dòng này rơi khi die
        public int CountMin = 1;           // own: số item tối thiểu
        public int CountMax = 1;           // own: số item tối đa (inclusive)

        public int RollCount(System.Random rng)
        {
            if (CountMax <= CountMin) return CountMin;
            return CountMin + rng.Next(CountMax - CountMin + 1);
        }
    }

    /// <summary>
    /// Drop table ScriptableObject. Một asset = nhiều pool (PoolID), roll theo pool.
    /// </summary>
    [CreateAssetMenu(menuName = "VLTK/Survivor/Drop Table", fileName = "DropTable")]
    public sealed class DropTableSO : ScriptableObject
    {
        public List<DropEntry> Entries = new List<DropEntry>();

        public List<DropEntry> ForPool(int poolId)
        {
            var list = new List<DropEntry>();
            for (int i = 0; i < Entries.Count; i++)
                if (Entries[i].PoolID == poolId)
                    list.Add(Entries[i]);
            return list;
        }
    }
}
