using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class ItemExchangeHostCommand
    {
        public string ApiName;
        public readonly List<int> IntArgs = new List<int>();
        public string TextArg;

        public static ItemExchangeHostCommand Create(string apiName, string textArg = null, params int[] intArgs)
        {
            var command = new ItemExchangeHostCommand { ApiName = apiName ?? string.Empty, TextArg = textArg };
            if (intArgs != null) command.IntArgs.AddRange(intArgs);
            return command;
        }
    }

    [Serializable]
    public sealed class ItemExchangePlanInput
    {
        public int GivenItemCount;
        public int ItemIndex;
        public int Genre;
        public int Detail;
        public int Particular;
        public int BindState;
        public int UseTime;
        public int ExpireTime;
        public int ExchangeValue;
        public int MagicLevel;
        public int Energy;
        public int ConsumeCount;
        public int FreeBagCells;
        public string ItemName;
        public int ItemQuality;
    }

    [Serializable]
    public sealed class ItemExchangePlan
    {
        public bool Success;
        public string PlanKind;
        public string FailureReason;
        public string SourceFunction;
        public string AwardName;
        public int RequiredMagicLevel;
        public int OverflowMagicLevel;
        public readonly List<ItemExchangeHostCommand> Commands = new List<ItemExchangeHostCommand>();

        public static ItemExchangePlan Fail(string kind, string sourceFunction, string reason)
        {
            return new ItemExchangePlan
            {
                Success = false,
                PlanKind = kind ?? string.Empty,
                SourceFunction = sourceFunction ?? string.Empty,
                FailureReason = reason ?? string.Empty
            };
        }

        public static ItemExchangePlan Ok(string kind, string sourceFunction)
        {
            return new ItemExchangePlan
            {
                Success = true,
                PlanKind = kind ?? string.Empty,
                SourceFunction = sourceFunction ?? string.Empty,
                FailureReason = string.Empty
            };
        }
    }

    public sealed class ItemExchangeLingpaiDefinition
    {
        public string NameVi;
        public string PcFunction;
        public int RequiredMagicLevel;
        public int Genre;
        public int Detail;
        public int Particular;
    }
}
