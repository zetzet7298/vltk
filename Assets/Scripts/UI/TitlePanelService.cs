// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Danh Hiệu (Player Title Panel)
// Reference: PC settings/playertitle.txt (363 danh hiệu) + TitleService runtime.
// PC playertitle.txt columns: TitleName, TitleId, SpeicalGraphic, FaceId,
//   AuraSkill, AuraSkillLevel, ExtSkill1..5(+Level), Memo, TitlePriority.
//   → PC KHÔNG có cột rarity/reqLevel nên panel không bịa các giá trị đó
//     (rarity mặc định = trắng, reqLevel = 0) — chỉ surface dữ liệu PC thật.
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
            this.name = name ?? string.Empty;
            this.rarity = rarity;
            this.reqLevel = reqLevel;
            this.isEquipped = isEquipped;
            this.isUnlocked = isUnlocked;
            this.summary = summary ?? string.Empty;
        }
    }

    /// <summary>
    /// Snapshot toàn bộ panel danh hiệu (cho controller render 1 frame).
    /// </summary>
    public sealed class TitlePanelSnapshot
    {
        public int playerLevel;
        public int equippedTitleId;
        public string equippedTitleName = string.Empty;
        public int totalTitles;
        public int unlockedTitles;
        public IReadOnlyList<TitlePanelRow> rows = System.Array.Empty<TitlePanelRow>();
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

        public const string LabelEquipped = "Đã trang bị";
        public const string LabelEquippable = "Có thể trang bị";
        public const string LabelLocked = "Chưa mở khóa";
        public const string LabelReqLevel = "Cấp yêu cầu";

        /// <summary>
        /// Thứ tự danh hiệu PC dùng để render mặc định khi chưa có TitleService.
        /// Lấy từ TitleVietnameseCatalog (các id danh hiệu PC đã ánh xạ tiếng Việt).
        /// </summary>
        public static IReadOnlyList<int> GetPcTitleOrder()
        {
            var ids = new List<int>();
            foreach (var kv in TitleVietnameseCatalog.GetAllMapped())
                ids.Add(kv.Key);
            ids.Sort();
            return ids;
        }

        /// <summary>
        /// Dựng snapshot panel từ TitleService thật. svc==null → snapshot rỗng (không throw).
        /// </summary>
        public static TitlePanelSnapshot BuildSnapshot(TitleService svc, int playerLevel, int selectedTitleId = 0)
        {
            var snap = new TitlePanelSnapshot
            {
                playerLevel = playerLevel,
                rows = System.Array.Empty<TitlePanelRow>(),
            };
            if (svc == null) return snap;

            int activeId = svc.ActivePlayerTitleId;
            snap.equippedTitleId = activeId;

            var rows = new List<TitlePanelRow>();
            foreach (var entry in svc.AllPlayerTitles)
            {
                if (entry == null) continue;
                bool equipped = activeId > 0 && entry.titleId == activeId;
                bool unlocked = svc.IsPlayerTitleUnlocked(entry.titleId);
                string name = ResolveName(entry);
                // PC playertitle.txt không có rarity/reqLevel → giữ trung thực: trắng, cấp 0.
                var row = new TitlePanelRow(
                    entry.titleId, name, RarityWhite, 0, equipped, unlocked,
                    DescribeTitle(name, RarityWhite, 0, unlocked));
                rows.Add(row);
                if (equipped) snap.equippedTitleName = name;
                if (selectedTitleId > 0 && entry.titleId == selectedTitleId) snap.selectedRow = row;
            }

            snap.rows = rows;
            snap.totalTitles = rows.Count;
            snap.unlockedTitles = svc.UnlockedPlayerTitleCount;
            return snap;
        }

        /// <summary>Trang bị danh hiệu nhân vật (yêu cầu đã mở khóa). Trả về true nếu đổi được.</summary>
        public static bool TryEquip(TitleService svc, int playerId, int titleId)
        {
            if (svc == null || titleId <= 0) return false;
            if (!svc.IsPlayerTitleUnlocked(titleId)) return false;
            return svc.SetActivePlayerTitle(titleId);
        }

        /// <summary>Gỡ danh hiệu nhân vật đang trang bị.</summary>
        public static bool TryUnEquip(TitleService svc, int playerId)
        {
            if (svc == null) return false;
            if (svc.ActivePlayerTitleId == 0) return false;
            return svc.SetActivePlayerTitle(0);
        }

        public static string DescribeTitle(TitleEntry entry)
        {
            if (entry == null) return string.Empty;
            return DescribeTitle(entry.name, RarityWhite, entry.reqLevel, false);
        }

        public static string DescribeTitle(string name, int rarity, int reqLevel, bool unlocked)
        {
            string display = string.IsNullOrEmpty(name) ? "Danh Hiệu" : name;
            string status = unlocked ? LabelEquippable : LabelLocked;
            string r = RarityName(rarity);
            if (reqLevel > 0)
                return $"{display} ({r}) — {LabelReqLevel} {reqLevel} — {status}";
            return $"{display} ({r}) — {status}";
        }

        public static string UpgradeStatus(TitleEntry entry, int playerLevel)
        {
            if (entry == null) return string.Empty;
            if (playerLevel < entry.reqLevel)
                return $"Cần đạt cấp {entry.reqLevel} (hiện tại {playerLevel})";
            return "Đủ điều kiện trang bị";
        }

        public static string RarityName(int rarity)
        {
            switch (rarity)
            {
                case RarityWhite: return "Trắng";
                case RarityGreen: return "Xanh lá";
                case RarityBlue: return "Xanh dương";
                case RarityPurple: return "Tím";
                case RarityGold: return "Vàng";
                case RarityOrange: return "Cam";
                default: return "Trắng";
            }
        }

        /// <summary>Ưu tiên tên tiếng Việt từ catalog, fallback về nameRaw (đã giải mã TCVN3).</summary>
        private static string ResolveName(PcPlayerTitleEntry entry)
        {
            if (entry == null) return string.Empty;
            string mapped = TitleVietnameseCatalog.GetVietnameseName(entry.titleId);
            if (!string.IsNullOrEmpty(mapped)) return mapped;
            return string.IsNullOrEmpty(entry.nameRaw) ? $"Danh Hiệu #{entry.titleId}" : entry.nameRaw;
        }
    }

    /// <summary>Entry nhẹ cho API DescribeTitle(TitleEntry)/UpgradeStatus — bind với TitleService.</summary>
    public class TitleEntry
    {
        public int titleId;
        public string name;
        public int reqLevel;
        public TitleEntry(int id, string n, int lvl) { titleId = id; name = n; reqLevel = lvl; }
    }
}
