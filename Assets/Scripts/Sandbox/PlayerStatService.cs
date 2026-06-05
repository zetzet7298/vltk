// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.2 Player Stat Service
// Calculates player stats (HP, MP, Damage, Defense, Resists) based on level,
// faction stats, and attributes from PC source config.
// Source: PC KNpc::CalcCurLifeMax, CalcCurManaMax, CalcDamage.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Bảng thuộc tính nhân vật sau khi tính toán.
    /// </summary>
    public struct PlayerStats
    {
        public int level;
        public int hpMax;
        public int mpMax;
        public int minDamage;
        public int maxDamage;
        public int defense;
        public int attackRating;
        public int fireResist;
        public int coldResist;
        public int lightResist;
        public int poisonResist;
        public int physicsResist;
    }

    /// <summary>
    /// Chỉ số cộng thêm từ trang bị.
    /// </summary>
    public struct EquipmentBonus
    {
        public int hp;
        public int mp;
        public int damageMin;
        public int damageMax;
        public int defense;
        public int attackRating;
        public int fireResist;
        public int coldResist;
        public int lightResist;
        public int poisonResist;
        public int physicsResist;
    }

    /// <summary>
    /// Service tính toán thuộc tính nhân vật theo môn phái, tiềm năng và trang bị.
    /// PC source: KNpc::CalcCurLifeMax / KNpc::CalcCurManaMax.
    /// </summary>
    public static class PlayerStatService
    {
        /// <summary>
        /// Tính toán toàn bộ chỉ số của người chơi.
        /// </summary>
        public static PlayerStats CalculateStats(int level, int factionId, int strength, int dexterity, int vitality, int innerStrength, EquipmentBonus equip)
        {
            int lv = Mathf.Max(1, level);
            var stats = new PlayerStats { level = lv };

            // 1) HP Max: Base (vit * factor) + level * factor
            float hpFactor = GetHpFactor(factionId);
            stats.hpMax = Mathf.RoundToInt(50 + vitality * hpFactor + lv * 1.5f) + equip.hp;

            // 2) MP Max: Sử dụng PcMaxManaFormula
            var combatFaction = (CombatFaction)factionId;
            stats.mpMax = PcMaxManaFormula.Compute(lv, innerStrength, combatFaction) + equip.mp;

            // 3) Sát thương ngoại công: Dựa trên Sức mạnh (Strength)
            float dmgFactor = GetDamageFactor(factionId);
            stats.minDamage = Mathf.RoundToInt(strength * dmgFactor) + equip.damageMin;
            stats.maxDamage = Mathf.RoundToInt(strength * dmgFactor * 1.2f) + equip.damageMax;

            // 4) Phòng ngự: Thân pháp (Dexterity) + Trang bị
            stats.defense = Mathf.RoundToInt(dexterity * 0.25f) + equip.defense;

            // 5) Tỷ lệ chính xác (Attack Rating): Thân pháp * 4
            stats.attackRating = (dexterity * 4) + equip.attackRating;

            // 6) Kháng tính: Capped at MaxResist (95%)
            const int MaxRes = DamageFormulaService.MaxResist;
            stats.fireResist = Mathf.Clamp(GetBaseResist(factionId, DamageType.Fire, lv) + equip.fireResist, 0, MaxRes);
            stats.coldResist = Mathf.Clamp(GetBaseResist(factionId, DamageType.Cold, lv) + equip.coldResist, 0, MaxRes);
            stats.lightResist = Mathf.Clamp(GetBaseResist(factionId, DamageType.Light, lv) + equip.lightResist, 0, MaxRes);
            stats.poisonResist = Mathf.Clamp(GetBaseResist(factionId, DamageType.Poison, lv) + equip.poisonResist, 0, MaxRes);
            stats.physicsResist = Mathf.Clamp(equip.physicsResist, 0, MaxRes); // Vật lý chủ yếu từ trang bị/skill

            return stats;
        }

        /// <summary>
        /// Yêu cầu EXP để lên cấp tiếp theo.
        /// Công thức PC: Exp(L) = 100 * (1.15 ^ (L - 1)) * L * L.
        /// </summary>
        public static long GetExpRequired(int level)
        {
            if (level <= 0) return 0;
            if (level >= 99) return long.MaxValue; // Capped at level 99
            double baseExp = 100 * Math.Pow(1.15, level - 1) * level * level;
            return (long)Math.Max(100, Math.Round(baseExp));
        }

        // ── Helpers ────────────────────────────────────────────────────────

        private static float GetHpFactor(int factionId)
        {
            return factionId switch
            {
                CombatFactionExt.ShaolinId or CombatFactionExt.TianWangId => 4f, // Trâu bò
                CombatFactionExt.TangMenId or CombatFactionExt.CaiBangId or CombatFactionExt.TianRenId => 3f, // Trung bình
                CombatFactionExt.EMeiId or CombatFactionExt.CuiYanId => 2.5f,
                CombatFactionExt.WuDangId or CombatFactionExt.KunLunId => 2f,
                _ => 3f,
            };
        }

        private static float GetDamageFactor(int factionId)
        {
            return factionId switch
            {
                CombatFactionExt.TianWangId or CombatFactionExt.ShaolinId => 1.2f,
                CombatFactionExt.TangMenId or CombatFactionExt.CaiBangId => 1.0f,
                _ => 0.8f,
            };
        }

        private static int GetBaseResist(int factionId, DamageType type, int level)
        {
            // Môn phái có kháng tính tự nhiên theo ngũ hành
            int element = CombatFactionExt.ToCharClass(factionId);
            bool isMatchingResist = false;

            if (type == DamageType.Fire && element == 4) isMatchingResist = true;   // Cái Bang, Thiên Nhẫn (Hỏa)
            if (type == DamageType.Cold && element == 2) isMatchingResist = true;   // Nga My, Thúy Yên (Thủy)
            if (type == DamageType.Light && element == 5) isMatchingResist = true;  // Võ Đang, Côn Lôn (Thổ -> Lôi)
            if (type == DamageType.Poison && element == 3) isMatchingResist = true; // Đường Môn, Ngũ Độc (Mộc -> Độc)

            // Mỗi 5 cấp tăng 1% kháng tự nhiên môn phái
            return isMatchingResist ? level / 5 : 0;
        }
    }
}
