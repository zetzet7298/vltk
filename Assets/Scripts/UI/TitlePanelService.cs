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
            // PC: danh hiệu xếp theo rarity xong theo ID
            return new int[]
            {
                1, 2, 3, 4, 5, 6, 7, 8, 9, 10,
                11, 12, 13, 14, 15, 16, 17, 18, 19, 20,
                21, 22, 23, 24, 25, 26, 27, 28, 29, 30,
                31, 32, 33, 34, 35, 36, 37, 38, 39, 40,
            };
        }

        public static TitlePanelSnapshot BuildSnapshot(TitleService svc, int playerId, int selectedTitleId = 0)
        {
            var snap = new TitlePanelSnapshot
            {
                playerLevel = 1,
                equippedTitleId = svc != null ? svc.ActivePlayerTitleId : 0,
                equippedTitleName = string.Empty,
                totalTitles = svc != null ? svc.PlayerTitleCount : 0,
                unlockedTitles = svc != null ? svc.UnlockedPlayerTitleCount : 0,
                rows = System.Array.Empty<TitlePanelRow>(),
            };
            var list = new List<TitlePanelRow>();
            if (svc != null)
            {
                foreach (var titleId in GetPcTitleOrder())
                {
                    // Best-effort: nếu service có registry thì lấy tên thật
                    string name = TitleVietnameseCatalog.GetVietnameseName(titleId) ?? ("Danh Hiệu " + titleId);
                    int rarity = (titleId / 10) % 6; // demo mapping
                    int reqLevel = (titleId % 9) + 1;
                    bool unlocked = svc.IsPlayerTitleUnlocked(titleId);
                    bool equipped = svc.ActivePlayerTitleId == titleId;
                    var row = new TitlePanelRow(titleId, name, rarity, reqLevel, equipped, unlocked, DescribeTitle(name, rarity, reqLevel, unlocked));
                    list.Add(row);
                    if (titleId == selectedTitleId) snap.selectedRow = row;
                }
                if (svc.ActivePlayerTitle != null) snap.equippedTitleName = svc.ActivePlayerTitle.displayName;
            }
            snap.rows = list;
            return snap;
        }

        public static bool TryEquip(TitleService svc, int playerId, int titleId)
        {
            if (svc == null || titleId <= 0) return false;
            if (!svc.IsPlayerTitleUnlocked(titleId)) return false;
            return svc.SetActivePlayerTitle(titleId);
        }

        public static bool TryUnEquip(TitleService svc, int playerId)
        {
            if (svc == null) return false;
            return svc.SetActivePlayerTitle(0);
        }

        public static string DescribeTitle(TitleEntry entry)
        {
            if (entry == null) return string.Empty;
            return "Danh Hiệu " + entry.name;
        }

        public static string DescribeTitle(string name, int rarity, int reqLevel, bool unlocked)
        {
            string r = RarityName(rarity);
            string status = unlocked ? "Đã mở khóa" : "Chưa mở khóa";
            return $"{name}\n{r} - Cấp yêu cầu {reqLevel}\n{status}";
        }

        public static string UpgradeStatus(TitleEntry entry, int playerLevel)
        {
            if (entry == null) return "Không tồn tại";
            if (playerLevel < entry.reqLevel) return $"Cần đạt cấp {entry.reqLevel}";
            return "Có thể trang bị";
        }

        public static string RarityName(int rarity) => rarity switch
        {
            RarityGreen => "Hiếm",
            RarityBlue => "Quý",
            RarityPurple => "Hiểm",
            RarityGold => "Tuyệt",
            RarityOrange => "Truyền Thuyết",
            _ => "Thường",
        };
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
