// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Danh Hiệu (Player Title Panel)
// Reference: PC ranking/title system + PcPlayerTitleRegistry + TitleService.
// Vietnamese: "Danh Hiệu", "Đã trang bị", "Có thể trang bị", "Cấp yêu cầu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>
    /// Một hàng trong panel danh hiệu (read-only struct để bind UI nhanh).
    /// </summary>
    public readonly struct TitlePanelRow
    {
        public readonly int titleId;
        public readonly string name;
        public readonly int rarity; // 0=white, 1=green, 2=blue, 3=purple, 4=gold, 5=orange
        public readonly int reqLevel;
        public readonly bool isEquipped;
        public readonly bool isUnlocked;
        public readonly string summary;

        public TitlePanelRow(int titleId, string name, int rarity, int reqLevel, bool isEquipped, bool isUnlocked, string summary)
        {
            this.titleId = titleId;
            this.name = name;
            this.rarity = rarity;
            this.reqLevel = reqLevel;
            this.isEquipped = isEquipped;
            this.isUnlocked = isUnlocked;
            this.summary = summary;
        }
    }

    /// <summary>
    /// Snapshot toàn bộ panel danh hiệu (cho controller render 1 frame).
    /// </summary>
    public sealed class TitlePanelSnapshot
    {
        public int playerLevel;
        public int equippedTitleId;
        public string equippedTitleName;
        public int totalTitles;
        public int unlockedTitles;
        public IReadOnlyList<TitlePanelRow> rows;
        public TitlePanelRow? selectedRow;
    }

    /// <summary>
    /// Service cung cấp dữ liệu cho UI panel Danh Hiệu. Không tự thay đổi TitleService ngoài TryEquip/TryUnEquip.
    /// </summary>
    public static class TitlePanelService
    {
        public const int RarityWhite = 0;
        public const int RarityGreen = 1;
        public const int RarityBlue = 2;
        public const int RarityPurple = 3;
        public const int RarityGold = 4;
        public const int RarityOrange = 5;

        public static IReadOnlyList<int> GetPcTitleOrder()
        {
            return System.Array.Empty<int>();
        }

        public static TitlePanelSnapshot BuildSnapshot(TitleService svc, int playerId, int selectedTitleId = 0)
        {
            return new TitlePanelSnapshot { rows = System.Array.Empty<TitlePanelRow>() };
        }

        public static bool TryEquip(TitleService svc, int playerId, int titleId)
        {
            return false;
        }

        public static bool TryUnEquip(TitleService svc, int playerId)
        {
            return false;
        }

        public static string DescribeTitle(TitleEntry entry)
        {
            return string.Empty;
        }

        public static string DescribeTitle(string name, int rarity, int reqLevel, bool unlocked)
        {
            return string.Empty;
        }

        public static string UpgradeStatus(TitleEntry entry, int playerLevel)
        {
            return string.Empty;
        }

        public static string RarityName(int rarity)
        {
            return string.Empty;
        }

    }

    /// <summary>Entry stub cho API DescribeTitle(TitleEntry) — bind với TitleService.</summary>
    public class TitleEntry
    {
        public int titleId;
        public string name;
        public int reqLevel;
        public TitleEntry(int id, string n, int lvl) { titleId = id; name = n; reqLevel = lvl; }
    }
}
