using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>
    /// Dạng curve level (own-design; dhcd không expose LevelExpCalc số).
    /// </summary>
    public enum XpCurveKind
    {
        Linear,      // base + (L-1)*growth — P1 default
        Step,        // base + floor((L-1)/5)*growth*5 — nhảy mỗi 5 level
        Exponential, // base * exp^(L-1) — leo nhanh, dùng P2+ endless
    }

    /// <summary>
    /// Level curve config. Default giữ P1: 5 + (L-1)*3 (spec D6).
    /// ScriptableObject để orchestrator author asset; code default = Default().
    /// </summary>
    [CreateAssetMenu(menuName = "VLTK/Survivor/Level Curve", fileName = "LevelCurve")]
    public sealed class LevelCurveConfig : ScriptableObject
    {
        public XpCurveKind Kind = XpCurveKind.Linear;
        public int BaseXp = 5;         // XP cần lên L2
        public int GrowthPerLevel = 3; // Linear/Step increment
        public float Exponent = 1.35f; // Exponential growth factor
        public int MaxLevel = 99;      // cap — từ đây XpToNext = MaxValue (không lên nữa)

        public int XpToNext(int level)
        {
            if (level < 1) level = 1;
            if (level >= MaxLevel) return int.MaxValue;
            switch (Kind)
            {
                case XpCurveKind.Linear:
                    return BaseXp + (level - 1) * GrowthPerLevel;
                case XpCurveKind.Step:
                    return BaseXp + ((level - 1) / 5) * GrowthPerLevel * 5;
                case XpCurveKind.Exponential:
                    return Mathf.Max(1, Mathf.RoundToInt(BaseXp * Mathf.Pow(Exponent, level - 1)));
                default:
                    return BaseXp;
            }
        }

        /// <summary>Default P1 curve — 5+(L-1)*3, parity SurvivorPlayer.XpToNext.</summary>
        public static LevelCurveConfig Default()
        {
            var c = CreateInstance<LevelCurveConfig>();
            c.Kind = XpCurveKind.Linear;
            c.BaseXp = 5;
            c.GrowthPerLevel = 3;
            c.MaxLevel = 99;
            return c;
        }
    }
}
