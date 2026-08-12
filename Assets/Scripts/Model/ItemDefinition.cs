using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M5.1 — Item definition imported from the vltktool item contract bundle
    /// (generate_item_contract_bundle.py / item_stat_contract.json). Mirrors the
    /// contract shape: stat deltas keyed by attr_code and stage, set/refine refs,
    /// and an icon reference resolved through the asset registry.
    /// </summary>
    [Serializable]
    public enum ItemStatStage
    {
        Base = 0,        // "base"
        MagicIndex = 1,  // "magic_index" (gold magic option slots)
        Refine = 2,      // refine-applied delta
        SetBonus = 3,    // set-bonus delta
    }

    [Serializable]
    public class ItemStatDelta
    {
        public string ruleId;     // e.g. "STAT_BASE_28"
        public ItemStatStage stage;
        public int attrCode;      // PC attribute code
        public int value;
    }

    [Serializable]
    public class ItemDefinition
    {
        public int itemId;
        public int resId;
        public string nameRaw;
        public string nameNormalized;
        public int setId;             // 0 = not part of a set
        public int refineLevel;       // Enchance/Point derived

        // PC item tuple. Script/magic items (e.g. GM token 6/1/4890) are
        // addressed by genre/detail/particular in Lua rather than synthetic id.
        public int itemGenre;
        public int detailType;
        public int particularType;
        public string description;
        public string scriptPath;

        public SourceAssetId iconSourceId;
        public bool iconResolved;
        public List<ItemStatDelta> statDeltas = new();
        public List<string> warnings = new();

        public string DisplayName =>
            !string.IsNullOrEmpty(nameNormalized) ? nameNormalized :
            !string.IsNullOrEmpty(nameRaw) ? nameRaw : $"Item_{itemId}";

        /// <summary>Sum of stat deltas for an attr code across the given stages.</summary>
        public int SumAttr(int attrCode, params ItemStatStage[] stages)
        {
            int sum = 0;
            foreach (var d in statDeltas)
            {
                if (d.attrCode != attrCode) continue;
                if (stages != null && stages.Length > 0)
                {
                    bool match = false;
                    foreach (var s in stages) if (s == d.stage) { match = true; break; }
                    if (!match) continue;
                }
                sum += d.value;
            }
            return sum;
        }
    }

    /// <summary>Status of a contract rule (mirrors vltktool rule status strings).</summary>
    [Serializable]
    public enum ContractRuleStatus
    {
        Unknown = 0,
        Implemented = 1,        // "implemented"
        ImplementedApprox = 2,  // "implemented_approx"
        Stub = 3,               // "stub"
    }

    [Serializable]
    public class ContractRule
    {
        public string ruleId;
        public ContractRuleStatus status;
    }

    /// <summary>Quality gate report surfaced in the GM Tools tab (M5.1 AC#2).</summary>
    [Serializable]
    public class ItemQualityGateReport
    {
        public int totalItems;
        public int created;
        public int updated;
        public int stubRules;
        public int approxRules;
        public int implementedRules;
        public bool passed;
        public List<string> messages = new();
    }
}
