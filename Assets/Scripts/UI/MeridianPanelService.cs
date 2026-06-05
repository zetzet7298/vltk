// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Kinh Mạch (Meridian Panel)
// Reference: PC meridian/kinh mạch system + PcMeridianRegistry + MeridianService.
// Vietnamese: "Kinh Mạch", "Cấp hiện tại", "Kinh nghiệm", "Cấp yêu cầu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một hàng trong panel kinh mạch.</summary>
    public readonly struct MeridianPanelRow
    {
        public readonly int levelIdx;
        public readonly string levelName;
        public readonly int reqPlayerLevel;
        public readonly int reqExp;
        public readonly int bonusType; // 0=hp, 1=mp, 2=atk, 3=def, 4=crit, 5=block
        public readonly int bonusValue;
        public readonly bool isUnlocked;
        public readonly string summary;

        public MeridianPanelRow(int levelIdx, string levelName, int reqPlayerLevel, int reqExp, int bonusType, int bonusValue, bool isUnlocked, string summary)
        {
            this.levelIdx = levelIdx;
            this.levelName = levelName;
            this.reqPlayerLevel = reqPlayerLevel;
            this.reqExp = reqExp;
            this.bonusType = bonusType;
            this.bonusValue = bonusValue;
            this.isUnlocked = isUnlocked;
            this.summary = summary;
        }
    }

    public sealed class MeridianPanelSnapshot
    {
        public int playerLevel;
        public int currentLevel;
        public int exp;
        public int totalLevels;
        public int unlockedLevels;
        public IReadOnlyList<MeridianPanelRow> rows;
        public MeridianPanelRow? selectedRow;
    }

    public static class MeridianPanelService
    {
        public const int BonusHp = 0;
        public const int BonusMp = 1;
        public const int BonusAtk = 2;
        public const int BonusDef = 3;
        public const int BonusCrit = 4;
        public const int BonusBlock = 5;

        public static IReadOnlyList<int> GetPcMeridianOrder()
        {
            // 128 cấp kinh mạch (PC) — chia 16 meridian × 8 level
            return new int[]
            {
                1, 2, 3, 4, 5, 6, 7, 8,
                9, 10, 11, 12, 13, 14, 15, 16,
                17, 18, 19, 20, 21, 22, 23, 24,
                25, 26, 27, 28, 29, 30, 31, 32,
            };
        }

        public static MeridianPanelSnapshot BuildSnapshot(MeridianService svc, int playerId, int selectedLevel = 0)
        {
            int totalLevels = svc != null ? svc.Count : 0;
            var snap = new MeridianPanelSnapshot
            {
                playerLevel = 1,
                currentLevel = 0,
                exp = 0,
                totalLevels = totalLevels,
                unlockedLevels = 0,
                rows = System.Array.Empty<MeridianPanelRow>(),
            };
            var list = new List<MeridianPanelRow>();
            if (svc != null)
            {
                int idx = 0;
                foreach (var mid in svc.GetMeridianIds())
                {
                    foreach (var p in svc.GetMeridianPoints(mid))
                    {
                        idx++;
                        if (selectedLevel == 0) selectedLevel = idx;
                        int bonus = (idx * 13) % 5; // demo mapping
                        int val = 50 + idx * 7;
                        bool unlocked = svc.GetPlayerAcupointLevel(p.acupointId) > 0;
                        if (unlocked) snap.unlockedLevels++;
                        if (unlocked && snap.currentLevel < p.level) snap.currentLevel = p.level;
                        var row = new MeridianPanelRow(
                            idx,
                            string.IsNullOrEmpty(p.nameRaw) ? ("Huyệt vị " + p.acupointId) : p.nameRaw,
                            p.reqLevel,
                            p.reqExp,
                            bonus,
                            val,
                            unlocked,
                            BuildSummary(p, unlocked));
                        list.Add(row);
                        if (idx == selectedLevel) snap.selectedRow = row;
                    }
                }
            }
            snap.rows = list;
            return snap;
        }

        public static bool TryUpgrade(MeridianService svc, int playerId, int levelIdx)
        {
            if (svc == null || levelIdx <= 0) return false;
            // Map level idx -> acupoint id (linear scan)
            int idx = 0;
            foreach (var mid in svc.GetMeridianIds())
            {
                foreach (var p in svc.GetMeridianPoints(mid))
                {
                    idx++;
                    if (idx == levelIdx)
                    {
                        var r = svc.TryUpgrade(p.acupointId, 50);
                        return r == UpgradeResult.Success;
                    }
                }
            }
            return false;
        }

        public static int GetProgress(MeridianService svc, int playerId)
        {
            if (svc == null) return 0;
            int total = 0, unlocked = 0;
            foreach (var mid in svc.GetMeridianIds())
                foreach (var p in svc.GetMeridianPoints(mid))
                {
                    total++;
                    if (svc.GetPlayerAcupointLevel(p.acupointId) > 0) unlocked++;
                }
            if (total == 0) return 0;
            return (int)((unlocked * 100.0f) / total);
        }

        public static string BuildSummary(PcMeridianEntry p, bool unlocked)
        {
            string status = unlocked ? "Đã mở" : "Chưa mở";
            return $"{(string.IsNullOrEmpty(p.nameRaw) ? "Huyệt vị" : p.nameRaw)}\nCấp yêu cầu {p.reqLevel}\n{status}";
        }

        public static string BonusTypeName(int t) => t switch
        {
            BonusHp => "Tăng máu",
            BonusMp => "Tăng nội lực",
            BonusAtk => "Tăng công kích",
            BonusDef => "Tăng phòng thủ",
            BonusCrit => "Tăng bạo kích",
            BonusBlock => "Tăng đỡ đòn",
            _ => "Khác",
        };
    }
}
