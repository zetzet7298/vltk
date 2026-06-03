// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.2 Combat Reflection Service
// Calculates melee/range damage return from reflection skills (e.g., La Hán Trận).
// Source: PC KNpc::CalcDamage reflection rules.
// -----------------------------------------------------------------------------

using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý phản đòn sát thương trong chiến đấu (La Hán Trận, Dịch Cân Kinh).
    /// PC source: KNpc::CalcDamage phản đòn sát thương cận chiến / tầm xa.
    /// </summary>
    public static class CombatReflectionService
    {
        /// <summary>
        /// Tính toán sát thương phản đòn phản lại attacker.
        /// Áp dụng sau khi defender đã nhận sát thương thực tế (finalDamage).
        /// </summary>
        public static int ApplyReflection(int finalDamage, int reflectPercent, int attackerCurrentHp, bool isMelee)
        {
            if (finalDamage <= 0 || reflectPercent <= 0)
                return 0;

            // 1) Sát thương phản đòn cơ bản = sát thương nhận vào * % phản đòn
            int reflected = Mathf.RoundToInt(finalDamage * (reflectPercent / 100f));

            // 2) PC JX1 reflection cap: Phản đòn không thể giết chết attacker ngay lập tức
            // từ mức HP đầy, và không thể vượt quá HP hiện tại của attacker
            if (reflected >= attackerCurrentHp)
            {
                reflected = Mathf.Max(0, attackerCurrentHp - 1);
            }

            return reflected;
        }

        /// <summary>
        /// Lấy % phản đòn cận chiến của defender dựa trên các trạng thái buff.
        /// Source: MagicAttributeKind.MeleeDamageReturnP.
        /// </summary>
        public static int GetMeleeReflectPercent(CombatActorState defender)
        {
            if (defender == null) return 0;

            int sum = 0;
            foreach (var state in defender.states.Values)
            {
                if (state.kind == MagicAttributeKind.MeleeDamageReturnP)
                {
                    sum += state.value1;
                }
            }
            return sum;
        }

        /// <summary>
        /// Lấy % phản đòn tầm xa của defender dựa trên các trạng thái buff.
        /// Source: MagicAttributeKind.RangeDamageReturnP.
        /// </summary>
        public static int GetRangeReflectPercent(CombatActorState defender)
        {
            if (defender == null) return 0;

            int sum = 0;
            foreach (var state in defender.states.Values)
            {
                if (state.kind == MagicAttributeKind.RangeDamageReturnP)
                {
                    sum += state.value1;
                }
            }
            return sum;
        }
    }
}
