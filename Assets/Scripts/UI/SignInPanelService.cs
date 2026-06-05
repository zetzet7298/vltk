// -----------------------------------------------------------------------------
// VLTK Mobile — Sign-In Panel Service (Điểm Danh)
// Dựng snapshot cho UI điểm danh hằng ngày. Kết hợp SignInService + streak.
// Vietnamese: "Điểm Danh", "Hôm nay", "Đã nhận", "Thưởng", "Liên tục", "X2".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct SignInPanelRow
    {
        public readonly int dayIdx;
        public readonly bool isClaimed;
        public readonly bool isToday;
        public readonly bool isFuture;
        public readonly string rewardItemName;
        public readonly int rewardCount;
        public readonly int rewardGold;
        public readonly bool isDouble;

        public SignInPanelRow(int dayIdx, bool isClaimed, bool isToday, bool isFuture, string rewardItemName, int rewardCount, int rewardGold, bool isDouble)
        {
            this.dayIdx = dayIdx;
            this.isClaimed = isClaimed;
            this.isToday = isToday;
            this.isFuture = isFuture;
            this.rewardItemName = rewardItemName;
            this.rewardCount = rewardCount;
            this.rewardGold = rewardGold;
            this.isDouble = isDouble;
        }
    }

    public sealed class SignInPanelSnapshot
    {
        public int playerId;
        public int totalDays;
        public int currentStreak;
        public int lastSignInDay;
        public bool canSignInToday;
        public IReadOnlyList<SignInPanelRow> rows;
    }

    public static class SignInPanelService
    {
        public const string LabelSignIn = "Điểm Danh";
        public const string LabelToday = "Hôm nay";
        public const string LabelClaimed = "Đã nhận";
        public const string LabelReward = "Thưởng";
        public const string LabelDay = "Ngày";
        public const string LabelStreak = "Liên tục";
        public const string LabelDouble = "X2";

        public static SignInPanelSnapshot BuildSnapshot(SignInService svc, int playerId, int currentDay)
        {
            return new SignInPanelSnapshot { rows = System.Array.Empty<SignInPanelRow>() };
        }

        public static bool TrySignIn(SignInService svc, int playerId, int day)
        {
            return false;
        }

        public static int GetStreak(SignInService svc, int playerId)
        {
            return 0;
        }

        public static SignInPanelRow GetTodayReward(SignInService svc, int currentDay)
        {
            return default;
        }

    }
}
