using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// A set-bonus rule: when at least <see cref="requiredPieces"/> items of the set
    /// are equipped, the listed stat deltas (by attr code) activate. Mirrors the
    /// vltktool set_bonus contract (SET_COUNT_EQUIPPED_PIECES /
    /// SET_HIDDEN_MAGIC_ACTIVATION).
    /// </summary>
    public class SetBonusRule
    {
        public int setId;
        public int requiredPieces;
        public ContractRuleStatus status = ContractRuleStatus.Implemented;
        public Dictionary<int, int> bonusByAttr = new();
    }

    /// <summary>
    /// A refine rule: a refine level grants a per-level multiplier on a base stat
    /// delta. Mirrors the vltktool refine contract (REFINE_GEN_NORMAL: Point/Enchance
    /// derived deltas).
    /// </summary>
    public class RefineRule
    {
        public int attrCode;
        public int perLevelBonus;        // flat bonus per refine level
        public ContractRuleStatus status = ContractRuleStatus.Implemented;
    }

    /// <summary>A golden replay case: equipped items + refine, expected stat totals.</summary>
    public class SetRefineGoldenCase
    {
        public string caseId;
        public List<ItemDefinition> equipped = new();
        public Dictionary<int, int> expectedTotals = new();
    }

    /// <summary>Result of replaying a golden case.</summary>
    public class GoldenReplayResult
    {
        public string caseId;
        public bool matched;
        public Dictionary<int, int> actual = new();
        public List<string> mismatches = new();
    }

    /// <summary>
    /// M5.3 — Set bonus and refine rule evaluation, validated against golden replay
    /// cases. Pure C# (no MonoBehaviour) so it is fully EditMode-testable. Computes
    /// totals from equipped items + set bonuses + refine (AC#3 recalculation),
    /// matches them against golden expected outcomes (AC#1), and reports stubbed
    /// rules through the quality gate (AC#2).
    /// </summary>
    public class SetBonusRefineService
    {
        private readonly Dictionary<int, SetBonusRule> _setRules = new();
        private readonly Dictionary<int, RefineRule> _refineRules = new();

        public void AddSetRule(SetBonusRule rule)
        {
            if (rule != null) _setRules[rule.setId] = rule;
        }

        public void AddRefineRule(RefineRule rule)
        {
            if (rule != null) _refineRules[rule.attrCode] = rule;
        }

        /// <summary>
        /// AC#3 — recalculate the full stat totals for a set of equipped items:
        /// base item deltas + refine (level * perLevelBonus) + active set bonuses.
        /// </summary>
        public Dictionary<int, int> ComputeTotals(IReadOnlyCollection<ItemDefinition> equipped)
        {
            var totals = new Dictionary<int, int>();
            if (equipped == null) return totals;

            var setCounts = new Dictionary<int, int>();

            foreach (var item in equipped)
            {
                if (item == null) continue;

                // Base item stat deltas.
                foreach (var d in item.statDeltas)
                    Add(totals, d.attrCode, d.value);

                // Refine: per-level flat bonus on each item's base attrs.
                if (item.refineLevel > 0)
                {
                    foreach (var d in item.statDeltas)
                    {
                        if (d.stage != ItemStatStage.Base) continue;
                        if (_refineRules.TryGetValue(d.attrCode, out var rr))
                            Add(totals, d.attrCode, rr.perLevelBonus * item.refineLevel);
                    }
                }

                // Count set membership.
                if (item.setId != 0)
                {
                    setCounts.TryGetValue(item.setId, out var c);
                    setCounts[item.setId] = c + 1;
                }
            }

            // Active set bonuses.
            foreach (var kv in setCounts)
            {
                if (_setRules.TryGetValue(kv.Key, out var rule) && kv.Value >= rule.requiredPieces)
                {
                    foreach (var b in rule.bonusByAttr)
                        Add(totals, b.Key, b.Value);
                }
            }

            return totals;
        }

        /// <summary>AC#1 — replay a golden case and compare to expected totals.</summary>
        public GoldenReplayResult ReplayGolden(SetRefineGoldenCase test)
        {
            var result = new GoldenReplayResult { caseId = test?.caseId };
            if (test == null)
            {
                result.matched = false;
                result.mismatches.Add("null case");
                return result;
            }

            result.actual = ComputeTotals(test.equipped);
            result.matched = true;

            foreach (var exp in test.expectedTotals)
            {
                result.actual.TryGetValue(exp.Key, out var actual);
                if (actual != exp.Value)
                {
                    result.matched = false;
                    result.mismatches.Add($"attr {exp.Key}: expected {exp.Value}, got {actual}");
                }
            }

            if (!result.matched)
                SubsystemLog.Warn("SetRefine", $"Golden case '{test.caseId}' mismatch: {string.Join("; ", result.mismatches)}");
            return result;
        }

        /// <summary>AC#2 — quality gate: report any stubbed set/refine rules.</summary>
        public ItemQualityGateReport QualityGate()
        {
            var report = new ItemQualityGateReport();
            foreach (var r in _setRules.Values)
                TallyStatus(report, r.status);
            foreach (var r in _refineRules.Values)
                TallyStatus(report, r.status);

            report.passed = report.stubRules == 0;
            report.messages.Add($"Set/refine rules: {report.implementedRules} impl / " +
                                $"{report.approxRules} approx / {report.stubRules} stub");
            if (report.stubRules > 0)
                SubsystemLog.Warn("SetRefine", $"{report.stubRules} stubbed set/refine rule(s)");
            return report;
        }

        private static void TallyStatus(ItemQualityGateReport report, ContractRuleStatus status)
        {
            switch (status)
            {
                case ContractRuleStatus.Implemented: report.implementedRules++; break;
                case ContractRuleStatus.ImplementedApprox: report.approxRules++; break;
                case ContractRuleStatus.Stub: report.stubRules++; break;
            }
        }

        private static void Add(Dictionary<int, int> map, int key, int value)
        {
            map.TryGetValue(key, out var cur);
            map[key] = cur + value;
        }
    }
}
