// -----------------------------------------------------------------------------
// VLTK Mobile — pure CityWar challenge-token turn-in model.
// PC source of truth (vl_update_27 only):
// - server1/script/missions/citywar_global/infocenter.lua:25-29,271-350
// - server1/script/missions/citywar_global/head.lua:24-26
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class CityWarChallengeTokenService
    {
        public const int TongTotalTaskId = 19;
        public const string ReasonEligible = "eligible";
        public const string ReasonEmptyGive = "empty_give";
        public const string ReasonWrongToken = "wrong_token";
        public const string ReasonDailyCapExceeded = "daily_cap_exceeded";

        public CityWarChallengeTokenPlan BuildTurnInPlan(CityWarChallengeTokenInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            int dailyBefore = input.StoredTaskDate == input.TodayTaskDate ? Math.Max(0, input.StoredDailyCount) : 0;
            int tokenCount = 0;
            for (int i = 0; i < input.GivenItems.Count; i++)
            {
                CityWarChallengeTokenUnit unit = input.GivenItems[i];
                if (unit == null || unit.StackCount <= 0)
                    return Reject(ReasonEmptyGive, dailyBefore, tokenCount);
                if (!unit.Item.Equals(CityWarPcConstants.ChallengeTokenItem))
                    return Reject(ReasonWrongToken, dailyBefore, tokenCount);
                tokenCount += unit.StackCount;
            }

            if (tokenCount <= 0)
                return Reject(ReasonEmptyGive, dailyBefore, tokenCount);

            if (dailyBefore + tokenCount > CityWarPcConstants.TiaoZhanLingDailyCap)
                return Reject(ReasonDailyCapExceeded, dailyBefore, tokenCount);

            var plan = CityWarChallengeTokenPlan.Accept(dailyBefore, tokenCount);
            if (input.StoredTaskDate != input.TodayTaskDate)
            {
                plan.Commands.Add(CityWarChallengeTokenOperation.Ints(
                    "SetTask", CityWarPcConstants.TiaoZhanLingTaskDate, input.TodayTaskDate));
                plan.Commands.Add(CityWarChallengeTokenOperation.Ints(
                    "SetTask", CityWarPcConstants.TiaoZhanLingTaskCount, 0));
            }

            for (int i = 0; i < input.GivenItems.Count; i++)
                plan.Commands.Add(CityWarChallengeTokenOperation.Ints("RemoveItemByIndex", input.GivenItems[i].ItemIndex));

            plan.Commands.Add(CityWarChallengeTokenOperation.Ints(
                "SetTask", CityWarPcConstants.TiaoZhanLingTaskCount, plan.DailyCountAfter));
            plan.Commands.Add(CityWarChallengeTokenOperation.Ints(
                "LG_ApplyAppendMemberTask",
                CityWarPcConstants.TiaoZhanLingLeagueType,
                CityWarPcConstants.TiaoZhanLingLeagueTaskCount,
                tokenCount));
            plan.Commands.Add(CityWarChallengeTokenOperation.Ints("AddOwnExp", plan.ExpReward));
            plan.Commands.Add(CityWarChallengeTokenOperation.Ints(
                "Ctc3tru_SetTask", TongTotalTaskId, input.CurrentTongTotal + tokenCount));
            plan.Events.Add(CityWarChallengeTokenOperation.Text("Msg2Player", "challenge_token_exp"));
            plan.Events.Add(CityWarChallengeTokenOperation.Text("Msg2Player", "tong_total_token_count"));
            plan.Events.Add(CityWarChallengeTokenOperation.Text("WriteLog", "citywar_give_tiaozhanling"));
            return plan;
        }

        private static CityWarChallengeTokenPlan Reject(string reason, int dailyBefore, int tokenCount)
        {
            var plan = CityWarChallengeTokenPlan.Reject(reason, dailyBefore, tokenCount);
            plan.Events.Add(CityWarChallengeTokenOperation.Text("Say", reason));
            return plan;
        }
    }

    public sealed class CityWarChallengeTokenInput
    {
        public int TodayTaskDate;
        public int StoredTaskDate;
        public int StoredDailyCount;
        public int CurrentTongTotal;
        public readonly List<CityWarChallengeTokenUnit> GivenItems = new List<CityWarChallengeTokenUnit>();
    }

    public sealed class CityWarChallengeTokenUnit
    {
        public readonly int ItemIndex;
        public readonly CityWarItemTuple Item;
        public readonly int StackCount;

        public CityWarChallengeTokenUnit(int itemIndex, CityWarItemTuple item, int stackCount)
        {
            ItemIndex = itemIndex;
            Item = item;
            StackCount = stackCount;
        }
    }

    public sealed class CityWarChallengeTokenPlan
    {
        public bool Accepted;
        public string ReasonCode;
        public int DailyCountBefore;
        public int TokenCount;
        public int DailyCountAfter;
        public int DailyRemaining;
        public int ExpReward;
        public readonly List<CityWarChallengeTokenOperation> Commands = new List<CityWarChallengeTokenOperation>();
        public readonly List<CityWarChallengeTokenOperation> Events = new List<CityWarChallengeTokenOperation>();

        public static CityWarChallengeTokenPlan Reject(string reason, int dailyBefore, int tokenCount)
        {
            return new CityWarChallengeTokenPlan
            {
                Accepted = false,
                ReasonCode = reason,
                DailyCountBefore = dailyBefore,
                TokenCount = tokenCount,
                DailyCountAfter = dailyBefore,
                DailyRemaining = Math.Max(0, CityWarPcConstants.TiaoZhanLingDailyCap - dailyBefore),
                ExpReward = 0,
            };
        }

        public static CityWarChallengeTokenPlan Accept(int dailyBefore, int tokenCount)
        {
            int dailyAfter = dailyBefore + tokenCount;
            return new CityWarChallengeTokenPlan
            {
                Accepted = true,
                ReasonCode = CityWarChallengeTokenService.ReasonEligible,
                DailyCountBefore = dailyBefore,
                TokenCount = tokenCount,
                DailyCountAfter = dailyAfter,
                DailyRemaining = CityWarPcConstants.TiaoZhanLingDailyCap - dailyAfter,
                ExpReward = tokenCount * CityWarPcConstants.TiaoZhanLingExpReward,
            };
        }
    }

    public sealed class CityWarChallengeTokenOperation
    {
        public readonly string Name;
        public readonly int[] IntArgs;
        public readonly string TextArg;

        private CityWarChallengeTokenOperation(string name, int[] intArgs, string textArg)
        {
            Name = name;
            IntArgs = intArgs ?? new int[0];
            TextArg = textArg;
        }

        public static CityWarChallengeTokenOperation Ints(string name, params int[] intArgs)
        {
            return new CityWarChallengeTokenOperation(name, intArgs, null);
        }

        public static CityWarChallengeTokenOperation Text(string name, string textArg)
        {
            return new CityWarChallengeTokenOperation(name, new int[0], textArg);
        }
    }
}
