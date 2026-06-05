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

            // PC SkillExpFunc uses only +, *, ^. Precedence: ^ binds tightest,
            // then *, then +. We support any combination of those, with no
            // parentheses. Subtraction / division fall through to the warning
            // at the bottom so misparses are surfaced instead of silently
            // returning 0.

            // Sum of additive terms: a + b + c
            if (expr.Contains("+"))
            {
                double sum = 0;
                foreach (var addPart in expr.Split('+'))
                {
                    if (addPart.Length == 0) continue;
                    if (!TryParseTerm(addPart, out double term)) continue;
                    sum += term;
                }
                return sum;
            }

            // Single additive term: a*b*c or a^b or a^b*c or a*b^c.
            if (TryParseTerm(expr, out double single))
                return single;

            // Fallback: log so the caller can spot a misparse.
            SubsystemLog.Warn("LuaFormula", $"EvaluateSimpleMath: unsupported expression '{expr}'");
            return 0;
        }

        /// <summary>
        /// Parse a single additive term: optional product of factors, where
        /// any single factor may itself be a^b.
        /// </summary>
        private static bool TryParseTerm(string term, out double value)
        {
            value = 0;
            if (term.Length == 0) return false;

            // Single literal?
            if (double.TryParse(term, out value)) return true;

            // Product of factors separated by *. Each factor can be a literal
            // or a^b. We allow any number of factors; the first ^ found in
            // the term determines the exponent pair.
            if (term.Contains("*"))
            {
                double product = 1;
                bool any = false;
                foreach (var factor in term.Split('*'))
                {
                    if (factor.Length == 0) return false;
                    if (!TryParseFactor(factor, out double v)) return false;
                    product *= v;
                    any = true;
                }
                if (!any) return false;
                value = product;
                return true;
            }

            // Single factor with ^ (handled by TryParseFactor).
            return TryParseFactor(term, out value);
        }

        private static bool TryParseFactor(string factor, out double value)
        {
            value = 0;
            if (factor.Length == 0) return false;

            if (factor.Contains("^"))
            {
                int powIdx = factor.IndexOf('^');
                string left = factor.Substring(0, powIdx);
                string right = factor.Substring(powIdx + 1);
                if (double.TryParse(left, out double baseVal) && double.TryParse(right, out double expVal))
                {
                    value = Math.Pow(baseVal, expVal);
                    return true;
                }
                return false;
            }

            return double.TryParse(factor, out value);
        }
    }
}
