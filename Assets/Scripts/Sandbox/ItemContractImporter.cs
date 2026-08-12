using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>An item contract bundle to import (parsed from vltktool output).</summary>
    public class ItemContractBundle
    {
        public string version;
        public List<ItemDefinition> items = new();
        public List<ContractRule> rules = new();
    }

    /// <summary>
    /// M5.1 — Imports item contract bundles produced by vltktool
    /// (generate_item_contract_bundle.py) into the Unity item database. Pure C# (no
    /// MonoBehaviour) so it is fully EditMode-testable. Creates/updates item
    /// definitions (AC#1), produces a quality-gate report for the GM Tools tab
    /// (AC#2), and respects strict mode for stubbed rules (AC#3).
    /// </summary>
    public class ItemContractImporter
    {
        private readonly Dictionary<int, ItemDefinition> _db = new();
        private readonly Dictionary<string, ItemDefinition> _byPcTuple = new();

        /// <summary>AC#3 — when true, stubbed contract rules fail the import.</summary>
        public bool StrictMode { get; set; }

        public int Count => _db.Count;
        public IReadOnlyCollection<ItemDefinition> Items => _db.Values;

        public ItemDefinition Resolve(int itemId)
        {
            _db.TryGetValue(itemId, out var i);
            return i;
        }

        public ItemDefinition ResolvePcItem(int itemGenre, int detailType, int particularType)
        {
            _byPcTuple.TryGetValue(PcTupleKey(itemGenre, detailType, particularType), out var item);
            return item;
        }

        private static string PcTupleKey(int itemGenre, int detailType, int particularType)
            => itemGenre + ":" + detailType + ":" + particularType;

        /// <summary>
        /// Import a bundle: upsert items (AC#1) and compute the quality-gate report
        /// (AC#2). In strict mode the presence of stub rules marks the gate failed
        /// (AC#3); otherwise stubs are recorded as warnings.
        /// </summary>
        public ItemQualityGateReport Import(ItemContractBundle bundle)
        {
            var report = new ItemQualityGateReport();
            if (bundle == null)
            {
                report.passed = false;
                report.messages.Add("Null contract bundle");
                SubsystemLog.Error("ItemImport", "Null contract bundle");
                return report;
            }

            // AC#1 — create or update item definitions.
            foreach (var item in bundle.items)
            {
                if (item == null) continue;
                if (_db.ContainsKey(item.itemId))
                {
                    _db[item.itemId] = item;
                    report.updated++;
                }
                else
                {
                    _db[item.itemId] = item;
                    report.created++;
                }

                if (item.itemGenre != 0 || item.detailType != 0 || item.particularType != 0)
                    _byPcTuple[PcTupleKey(item.itemGenre, item.detailType, item.particularType)] = item;
            }
            report.totalItems = _db.Count;

            // AC#2 — tally rule statuses for the gate report.
            foreach (var rule in bundle.rules)
            {
                switch (rule.status)
                {
                    case ContractRuleStatus.Implemented: report.implementedRules++; break;
                    case ContractRuleStatus.ImplementedApprox: report.approxRules++; break;
                    case ContractRuleStatus.Stub: report.stubRules++; break;
                }
            }

            // AC#3 — strict mode fails on any stub.
            if (report.stubRules > 0)
            {
                if (StrictMode)
                {
                    report.passed = false;
                    report.messages.Add($"Strict mode: {report.stubRules} stub rule(s) present → import failed");
                    SubsystemLog.Error("ItemImport", $"Strict gate failed: {report.stubRules} stub rule(s)");
                }
                else
                {
                    report.passed = true;
                    report.messages.Add($"{report.stubRules} stub rule(s) present (warning, non-strict)");
                    SubsystemLog.Warn("ItemImport", $"{report.stubRules} stub rule(s) imported as warnings");
                }
            }
            else
            {
                report.passed = true;
            }

            report.messages.Add($"Imported {report.created} new, {report.updated} updated; " +
                                $"rules: {report.implementedRules} impl / {report.approxRules} approx / {report.stubRules} stub");
            SubsystemLog.Info("ItemImport",
                $"Gate {(report.passed ? "PASS" : "FAIL")}: {report.totalItems} items, {report.stubRules} stubs");
            return report;
        }

        /// <summary>AC#2 — resolve item icon references through the asset registry.</summary>
        public void ResolveIcons(IAssetRegistry assets)
        {
            if (assets == null) return;
            foreach (var item in _db.Values)
            {
                if (item.iconSourceId == null) { item.iconResolved = false; continue; }
                var entry = assets.Resolve(item.iconSourceId);
                item.iconResolved = entry != null && entry.status == AssetStatus.Available;
            }
        }
    }
}
