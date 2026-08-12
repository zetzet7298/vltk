// -----------------------------------------------------------------------------
// VLTK.Survivor — MonsterCapConfig (ticket 42, 60fps monster cap)
// Cap số monster đồng thời: giữ frame budget 16.7ms trên mobile trung bình.
// Nguồn đếm = SurvivorGameDirector.Monsters (public API).
//
//  - MonsterCapPolicy = logic thuần (test EditMode): EffectiveCap/CanSpawn/Excess/
//    PickTrimIndices. CanSpawn là spawn-gate thật — SurvivorMonsterCap đăng ký
//    vào SurvivorGameDirector.MonsterSpawnGate (ticket 42: boss exempt).
//  - SurvivorMonsterCap (Mono) ở file riêng — 1 MonoBehaviour/file (Unity 6
//    resolve scene reference fail với multi-class file).
//  - Fail-closed: cap cấu hình ≤ 0 → DefaultCap; > MaxCap → trần.
//  - Cap table (own, xem docs/survivor-profiling-plan.md): default 80 — mid
//    Android ~80 SpriteRenderer ≈ 80 draw call; low-end 50, high-end 120.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Cấu hình cap — scene author gán trên SurvivorMonsterCap.</summary>
    [Serializable]
    public sealed class MonsterCapConfig
    {
        [Tooltip("Cap monster đồng thời; ≤0 → default. Cap table: profiling plan.")]
        public int MaxMonsters = MonsterCapPolicy.DefaultCap;

        public int Effective => MonsterCapPolicy.EffectiveCap(MaxMonsters);
    }

    /// <summary>Policy cap thuần — test EditMode không scene.</summary>
    public static class MonsterCapPolicy
    {
        /// <summary>Own balance: 80 SPR ≈ 80 DC ≈ mid Android 60fps (đo lại = profiling plan).</summary>
        public const int DefaultCap = 80;

        /// <summary>Trần an toàn — không cho config giết perf.</summary>
        public const int MaxCap = 200;

        /// <summary>Fail-closed: ≤0 (chưa cấu hình/hỏng) → default; > MaxCap → cắt trần.</summary>
        public static int EffectiveCap(int configured)
        {
            if (configured <= 0) return DefaultCap;
            return Math.Min(configured, MaxCap);
        }

        /// <summary>Spawn-gate hook: còn chỗ thì spawn. active = Monsters.Count.</summary>
        public static bool CanSpawn(int activeCount, int configuredCap)
        {
            return activeCount < EffectiveCap(configuredCap);
        }

        /// <summary>Số cần trim để về cap; ≤ 0 → 0.</summary>
        public static int Excess(int activeCount, int configuredCap)
        {
            return Math.Max(0, activeCount - EffectiveCap(configuredCap));
        }

        /// <summary>
        /// Index cần trim (front-first = kẻ sống lâu nhất trước), dừng khi đủ
        /// excess. exempt(i) = monster không được trim (boss). Fail-closed:
        /// không đủ non-exempt → trả ít hơn excess.
        /// </summary>
        public static List<int> PickTrimIndices(int activeCount, int configuredCap, Func<int, bool> exempt = null)
        {
            var res = new List<int>();
            int excess = Excess(activeCount, configuredCap);
            if (excess <= 0) return res;
            for (int i = 0; i < activeCount && res.Count < excess; i++)
                if (exempt == null || !exempt(i)) res.Add(i);
            return res;
        }
    }
}
