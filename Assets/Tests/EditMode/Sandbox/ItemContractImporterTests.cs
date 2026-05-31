using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M5.1 — Item Contract Import tests. Create/update item definitions (AC#1),
    /// quality-gate report (AC#2), and strict-mode stub handling (AC#3).
    /// </summary>
    public class ItemContractImporterTests
    {
        private ItemDefinition Item(int id, string name = null)
            => new ItemDefinition
            {
                itemId = id,
                nameNormalized = name ?? $"Item{id}",
                statDeltas =
                {
                    new ItemStatDelta { ruleId = "STAT_BASE_28", stage = ItemStatStage.Base, attrCode = 28, value = 46 },
                },
            };

        private ItemContractBundle Bundle(List<ItemDefinition> items, params ContractRuleStatus[] ruleStatuses)
        {
            var b = new ItemContractBundle { version = "phase1h", items = items };
            int i = 0;
            foreach (var s in ruleStatuses)
                b.rules.Add(new ContractRule { ruleId = $"R{i++}", status = s });
            return b;
        }

        // --- AC#1: create / update item definitions ---

        [Test]
        public void Import_CreatesItems()
        {
            var imp = new ItemContractImporter();
            var report = imp.Import(Bundle(new List<ItemDefinition> { Item(1), Item(2) }));
            Assert.AreEqual(2, report.created);
            Assert.AreEqual(0, report.updated);
            Assert.AreEqual(2, imp.Count);
            Assert.IsNotNull(imp.Resolve(1));
        }

        [Test]
        public void Import_SecondTime_UpdatesExisting()
        {
            var imp = new ItemContractImporter();
            imp.Import(Bundle(new List<ItemDefinition> { Item(1, "Old") }));
            var report = imp.Import(Bundle(new List<ItemDefinition> { Item(1, "New") }));
            Assert.AreEqual(0, report.created);
            Assert.AreEqual(1, report.updated);
            Assert.AreEqual("New", imp.Resolve(1).DisplayName);
        }

        [Test]
        public void Import_NullBundle_FailsGate()
        {
            var imp = new ItemContractImporter();
            LogAssert.Expect(LogType.Error, "[ItemImport] Null contract bundle");
            var report = imp.Import(null);
            Assert.IsFalse(report.passed);
        }

        [Test]
        public void ItemDefinition_SumAttr_AggregatesByStage()
        {
            var item = Item(1);
            item.statDeltas.Add(new ItemStatDelta { stage = ItemStatStage.Refine, attrCode = 28, value = 10 });
            Assert.AreEqual(56, item.SumAttr(28)); // 46 base + 10 refine
            Assert.AreEqual(46, item.SumAttr(28, ItemStatStage.Base));
            Assert.AreEqual(10, item.SumAttr(28, ItemStatStage.Refine));
        }

        // --- AC#2: quality gate report ---

        [Test]
        public void Import_TalliesRuleStatuses()
        {
            var imp = new ItemContractImporter();
            var report = imp.Import(Bundle(
                new List<ItemDefinition> { Item(1) },
                ContractRuleStatus.Implemented, ContractRuleStatus.ImplementedApprox, ContractRuleStatus.ImplementedApprox));
            Assert.AreEqual(1, report.implementedRules);
            Assert.AreEqual(2, report.approxRules);
            Assert.AreEqual(0, report.stubRules);
            Assert.IsTrue(report.passed);
        }

        // --- AC#3: strict mode for stubbed rules ---

        [Test]
        public void Import_StrictMode_StubFailsGate()
        {
            var imp = new ItemContractImporter { StrictMode = true };
            LogAssert.Expect(LogType.Error, "[ItemImport] Strict gate failed: 1 stub rule(s)");
            var report = imp.Import(Bundle(
                new List<ItemDefinition> { Item(1) },
                ContractRuleStatus.Implemented, ContractRuleStatus.Stub));
            Assert.AreEqual(1, report.stubRules);
            Assert.IsFalse(report.passed);
        }

        [Test]
        public void Import_NonStrict_StubWarnsButPasses()
        {
            var imp = new ItemContractImporter { StrictMode = false };
            var report = imp.Import(Bundle(
                new List<ItemDefinition> { Item(1) },
                ContractRuleStatus.Stub));
            Assert.AreEqual(1, report.stubRules);
            Assert.IsTrue(report.passed);
        }

        // --- icon resolution ---

        [Test]
        public void ResolveIcons_MarksAvailableIcons()
        {
            var assets = new AssetRegistry();
            var icon = new SourceAssetId { sourcePath = "item/icon1.spr", uid = 1 };
            assets.Register(new AssetRegistryEntry { sourceId = icon, status = AssetStatus.Available });

            var imp = new ItemContractImporter();
            var item = Item(1);
            item.iconSourceId = icon;
            imp.Import(Bundle(new List<ItemDefinition> { item }));
            imp.ResolveIcons(assets);

            Assert.IsTrue(imp.Resolve(1).iconResolved);
        }
    }
}
