// -----------------------------------------------------------------------------
// VLTK Mobile — Atlas compound craft-plan result + operations
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class CompoundPlanOperation
    {
        public string name;
        public int intArg;
        public string textArg;

        public static CompoundPlanOperation Int(string name, int arg)
            => new CompoundPlanOperation { name = name, intArg = arg };

        public static CompoundPlanOperation Text(string name, string arg)
            => new CompoundPlanOperation { name = name, textArg = arg ?? string.Empty };
    }

    public sealed class CompoundCraftPlan
    {
        public CompoundPlanStatus status;
        public int costSilver;
        public int sourceItemValueSum;
        public int destinationItemValue;
        public float successProbability;
        public PcAtlasCompoundRecipe recipe;
        public IReadOnlyList<PcAtlasCompoundSourceItem> necessaryItems = Array.Empty<PcAtlasCompoundSourceItem>();
        public IReadOnlyList<PcAtlasCompoundSourceItem> alternativeItems = Array.Empty<PcAtlasCompoundSourceItem>();
        public IReadOnlyList<CompoundPlanOperation> operations = Array.Empty<CompoundPlanOperation>();

        public static CompoundCraftPlan Reject(CompoundPlanStatus status)
            => new CompoundCraftPlan { status = status };
    }

    public sealed class CompoundCraftExecutionResult
    {
        public CompoundPlanStatus status;
        public int resultItemIndex;
        public IReadOnlyList<string> executedOperationNames = Array.Empty<string>();
    }
}
