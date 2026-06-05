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
            var snap = new SignInPanelSnapshot
            {
                playerId = playerId,
                totalDays = svc?.Count ?? 0,
                rows = Array.Empty<SignInPanelRow>(),
            };
            if (svc == null) return snap;
            var rows = new List<SignInPanelRow>();
            for (int d = 1; d <= svc.Count; d++)
            {
                bool isToday = d == currentDay;
                bool isFuture = d > currentDay;
                bool canClaim = svc.CanSignIn(d, currentDay - 1, svc.Count);
                bool isDouble = svc.IsDouble(d);
                rows.Add(new SignInPanelRow(d, !canClaim && d < currentDay, isToday, isFuture, GetItemName(d), 1, GetGold(d), isDouble));
            }
            snap.currentStreak = GetStreak(svc, playerId);
            snap.lastSignInDay = currentDay > 0 ? currentDay - 1 : 0;
            snap.canSignInToday = rows.Count > 0 && currentDay <= svc.Count && svc.CanSignIn(currentDay, currentDay - 1, svc.Count);
            snap.rows = rows;
            return snap;
        }

        public static bool TrySignIn(SignInService svc, int playerId, int day)
        {
            if (svc == null || day <= 0) return false;
            return svc.TryClaimDay(playerId, day);
        }

        public static int GetStreak(SignInService svc, int playerId)
        {
            if (svc == null) return 0;
            return svc.GetPlayerStreak(playerId);
        }

        public static SignInPanelRow GetTodayReward(SignInService svc, int currentDay)
        {
            if (svc == null || currentDay <= 0) return default;
            foreach (var row in BuildSnapshot(svc, 0, currentDay).rows)
            {
                if (row.isToday) return row;
            }
            return default;
        }

        private static string GetItemName(int day)
        {
            switch ((day - 1) % 7)
            {
                case 0: return "Túi Tiểu Cường";
                case 1: return "Đan Dược";
                case 2: return "Bảo Rương";
                case 3: return "Đá Cường Hóa";
                case 4: return "Nguyệt Lệ";
                case 5: return "Bí Tịch";
                case 6: return "Đan Công";
                default: return "Phần Thưởng";
            }
        }

        private static int GetGold(int day)
        {
            return 10000 * (1 + (day - 1) / 7);
        }
    }
}
