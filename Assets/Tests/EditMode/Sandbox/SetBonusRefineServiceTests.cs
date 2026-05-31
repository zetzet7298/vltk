using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M5.3 — Set Bonus and Refine Rules tests. Golden replay matching (AC#1),
    /// stub-status quality gate (AC#2), and recalculation when equipment changes
    /// (AC#3).
    /// </summary>
    public class SetBonusRefineServiceTests
    {
        private ItemDefinition Item(int id, int setId, int refine, int attr, int value)
        {
            var item = new ItemDefinition { itemId = id, setId = setId, refineLevel = refine };
            item.statDeltas.Add(new ItemStatDelta { stage = ItemStatStage.Base, attrCode = attr, value = value });
            return item;
        }

        // --- AC#3: recompute totals (base + refine + set bonus) ---

        [Test]
        public void ComputeTotals_BaseDeltasOnly()
        {
            var svc = new SetBonusRefineService();
            var items = new List<ItemDefinition> { Item(1, 0, 0, 28, 10), Item(2, 0, 0, 28, 5) };
            var totals = svc.ComputeTotals(items);
            Assert.AreEqual(15, totals[28]);
        }

        [Test]
        public void ComputeTotals_AppliesRefinePerLevel()
        {
            var svc = new SetBonusRefineService();
            svc.AddRefineRule(new RefineRule { attrCode = 28, perLevelBonus = 3 });
            // item with refine level 4 → +12 on attr 28.
            var totals = svc.ComputeTotals(new List<ItemDefinition> { Item(1, 0, 4, 28, 10) });
            Assert.AreEqual(22, totals[28]); // 10 base + 4*3 refine
        }

        [Test]
        public void ComputeTotals_ActivatesSetBonus_AtThreshold()
        {
            var svc = new SetBonusRefineService();
            svc.AddSetRule(new SetBonusRule
            {
                setId = 100, requiredPieces = 2,
                bonusByAttr = new Dictionary<int, int> { { 29, 50 } },
            });
            // Two pieces of set 100 → bonus applies.
            var items = new List<ItemDefinition> { Item(1, 100, 0, 28, 10), Item(2, 100, 0, 28, 10) };
            var totals = svc.ComputeTotals(items);
            Assert.AreEqual(20, totals[28]);
            Assert.AreEqual(50, totals[29]); // set bonus
        }

        [Test]
        public void ComputeTotals_SetBonus_NotActiveBelowThreshold()
        {
            var svc = new SetBonusRefineService();
            svc.AddSetRule(new SetBonusRule
            {
                setId = 100, requiredPieces = 3,
                bonusByAttr = new Dictionary<int, int> { { 29, 50 } },
            });
            // Only 2 pieces, needs 3 → no bonus.
            var items = new List<ItemDefinition> { Item(1, 100, 0, 28, 10), Item(2, 100, 0, 28, 10) };
            var totals = svc.ComputeTotals(items);
            Assert.IsFalse(totals.ContainsKey(29));
        }

        [Test]
        public void ComputeTotals_ChangingEquipment_Recalculates()
        {
            var svc = new SetBonusRefineService();
            svc.AddSetRule(new SetBonusRule
            {
                setId = 100, requiredPieces = 2,
                bonusByAttr = new Dictionary<int, int> { { 29, 50 } },
            });
            var oneItem = new List<ItemDefinition> { Item(1, 100, 0, 28, 10) };
            Assert.IsFalse(svc.ComputeTotals(oneItem).ContainsKey(29)); // 1 piece, no bonus

            var twoItems = new List<ItemDefinition> { Item(1, 100, 0, 28, 10), Item(2, 100, 0, 28, 10) };
            Assert.AreEqual(50, svc.ComputeTotals(twoItems)[29]); // 2 pieces → bonus
        }

        // --- AC#1: golden replay ---

        [Test]
        public void ReplayGolden_MatchingCase_Passes()
        {
            var svc = new SetBonusRefineService();
            svc.AddRefineRule(new RefineRule { attrCode = 28, perLevelBonus = 2 });
            var test = new SetRefineGoldenCase
            {
                caseId = "G1",
                equipped = new List<ItemDefinition> { Item(1, 0, 3, 28, 10) }, // 10 + 3*2 = 16
                expectedTotals = new Dictionary<int, int> { { 28, 16 } },
            };
            var result = svc.ReplayGolden(test);
            Assert.IsTrue(result.matched);
            Assert.IsEmpty(result.mismatches);
        }

        [Test]
        public void ReplayGolden_Mismatch_ReportsDifference()
        {
            var svc = new SetBonusRefineService();
            var test = new SetRefineGoldenCase
            {
                caseId = "G2",
                equipped = new List<ItemDefinition> { Item(1, 0, 0, 28, 10) },
                expectedTotals = new Dictionary<int, int> { { 28, 99 } }, // wrong expectation
            };
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Golden case 'G2' mismatch"));
            var result = svc.ReplayGolden(test);
            Assert.IsFalse(result.matched);
            Assert.IsNotEmpty(result.mismatches);
        }

        // --- AC#2: quality gate reports stubbed rules ---

        [Test]
        public void QualityGate_ReportsStubRules()
        {
            var svc = new SetBonusRefineService();
            svc.AddSetRule(new SetBonusRule { setId = 1, requiredPieces = 2, status = ContractRuleStatus.Implemented });
            svc.AddRefineRule(new RefineRule { attrCode = 28, status = ContractRuleStatus.Stub });

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("stubbed set/refine rule"));
            var report = svc.QualityGate();
            Assert.AreEqual(1, report.stubRules);
            Assert.AreEqual(1, report.implementedRules);
            Assert.IsFalse(report.passed);
        }

        [Test]
        public void QualityGate_NoStubs_Passes()
        {
            var svc = new SetBonusRefineService();
            svc.AddSetRule(new SetBonusRule { setId = 1, requiredPieces = 2, status = ContractRuleStatus.Implemented });
            var report = svc.QualityGate();
            Assert.IsTrue(report.passed);
        }
    }
}
