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
            return System.Array.Empty<int>();
        }

        public static MeridianPanelSnapshot BuildSnapshot(MeridianService svc, int playerId, int selectedLevel = 0)
        {
            return new MeridianPanelSnapshot { rows = System.Array.Empty<MeridianPanelRow>() };
        }

        public static bool TryUpgrade(MeridianService svc, int playerId, int levelIdx)
        {
            return false;
        }

        public static int GetProgress(MeridianService svc, int playerId)
        {
            return 0;
        }

        public static string BuildSummary(PcMeridianEntry p, bool unlocked)
        {
            return string.Empty;
        }

        public static string BonusTypeName(int t)
        {
            return string.Empty;
        }

    }
}
