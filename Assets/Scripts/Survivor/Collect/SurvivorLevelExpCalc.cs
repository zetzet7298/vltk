using System;

namespace VLTK.Survivor
{
    /// <summary>
    /// LevelExpCalc parity-shape (dhcd BattleCore.LevelExpCalc.AddExp):
    /// cộng XP, loop level-up, XP dư carryover. Pure static — test được EditMode.
    /// </summary>
    public static class SurvivorLevelExpCalc
    {
        /// <summary>
        /// AddExp(ref xp, ref level, amount, xpToNext) — parity shape dhcd AddExp.
        /// Trả số level tăng (0 = không lên). xpToNext(level) = curve config.
        /// </summary>
        public static int AddExp(ref int xp, ref int level, int amount, Func<int, int> xpToNext)
        {
            if (amount <= 0) return 0;
            int ups = 0;
            xp += amount;
            while (xp >= xpToNext(level))
            {
                xp -= xpToNext(level);
                level++;
                ups++;
            }
            return ups;
        }

        /// <summary>Overload lấy curve trực tiếp từ LevelCurveConfig.</summary>
        public static int AddExp(ref int xp, ref int level, int amount, LevelCurveConfig curve)
        {
            return AddExp(ref xp, ref level, amount, curve.XpToNext);
        }
    }
}
