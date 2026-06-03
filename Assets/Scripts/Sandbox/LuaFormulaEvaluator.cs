// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.2 Lua Formula Evaluator
// Utility class to evaluate JX PC Lua formulas like SkillExpFunc.
// Sourced from gaibang.lua formulas.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Bộ tiện ích tính toán các công thức từ Lua script của PC JX1.
    /// Giúp tính toán kinh nghiệm và thuộc tính kỹ năng động theo cấp độ.
    /// </summary>
    public static class LuaFormulaEvaluator
    {
        private static readonly Dictionary<string, double> GlobalVariables = new();

        public static void SetVariable(string name, double value)
        {
            GlobalVariables[name.ToLowerInvariant()] = value;
        }

        /// <summary>
        /// Tính toán công thức SkillExpFunc từ gaibang.lua:
        /// SkillExpFunc(Exp0, a, Level, Time, Range) = floor(Exp0 * (a ^ (Level - 1)) * Time * Range / 2)
        /// </summary>
        public static int EvaluateSkillExp(double exp0, double a, int level, double time, double range)
        {
            try
            {
                double result = exp0 * Math.Pow(a, level - 1) * time * range / 2.0;
                return (int)Math.Floor(result);
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("LuaFormula", $"EvaluateSkillExp failed: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Tính toán các biểu thức toán học đơn giản dạng string từ Lua.
        /// </summary>
        public static double EvaluateExpression(string expr)
        {
            if (string.IsNullOrWhiteSpace(expr)) return 0;

            string clean = expr.Replace("floor", "").Replace("(", "").Replace(")", "").Trim();

            // Thay thế biến toàn cục
            foreach (var kvp in GlobalVariables)
            {
                clean = Regex.Replace(clean, @"\b" + kvp.Key + @"\b", kvp.Value.ToString(), RegexOptions.IgnoreCase);
            }

            try
            {
                return EvaluateSimpleMath(clean);
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn("LuaFormula", $"EvaluateExpression failed for '{expr}': {ex.Message}");
                return 0;
            }
        }

        private static double EvaluateSimpleMath(string expr)
        {
            expr = expr.Replace(" ", "");

            if (expr.Contains("^"))
            {
                string[] parts = expr.Split('^');
                if (parts.Length == 2 && double.TryParse(parts[0], out double baseVal) && double.TryParse(parts[1], out double expVal))
                {
                    return Math.Pow(baseVal, expVal);
                }
            }

            if (expr.Contains("*"))
            {
                string[] parts = expr.Split('*');
                double mult = 1.0;
                foreach (var p in parts)
                {
                    if (double.TryParse(p, out double v)) mult *= v;
                }
                return mult;
            }

            if (double.TryParse(expr, out double result))
                return result;

            return 0;
        }
    }
}
