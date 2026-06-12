// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Kinh Mạch (Meridian Panel)
// PC source:
//   - settings/PcMeridian/meridian.txt      → 8 mạch (经脉ID 1-8) theo thứ tự.
//   - settings/PcMeridian/meridian_level.txt → 128 huyệt (8 mạch × 16 cấp).
// Panel chỉ surface dữ liệu PcMeridianRegistry đã load (MeridianService); KHÔNG
// bịa bonusType/bonusValue/reqExp vì meridian_level.txt không có các cột đó —
// cấu trúc dữ liệu thật chỉ có (tên, mạch, cấp huyệt, fallback, successRate, mô tả).
// Vietnamese: "Kinh Mạch", "Huyệt Đạo", "Cấp hiện tại", "Cấp yêu cầu".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    /// <summary>Một hàng trong panel kinh mạch (một huyệt đạo).</summary>
    public readonly struct MeridianPanelRow
    {
        public readonly int meridianId;
        public readonly int levelIdx;        // 穴位ID (1-16) — cũng là cấp nhân vật yêu cầu.
        public readonly string levelName;    // Tên huyệt (đã giải mã TCVN3).
        public readonly int reqPlayerLevel;  // = levelIdx (PC: huyệt cấp N mở ở cấp nhân vật N).
        public readonly int successRate;     // /10000 (10000 = 100%).
        public readonly int fallbackLevel;   // Cấp tụt khi đột phá thất bại.
        public readonly int playerTier;      // Cấp tu luyện hiện tại của người chơi (0-9).
        public readonly bool isUnlocked;     // playerTier > 0.
        public readonly string summary;

        public MeridianPanelRow(int meridianId, int levelIdx, string levelName, int reqPlayerLevel, int successRate, int fallbackLevel, int playerTier, bool isUnlocked, string summary)
        {
            this.meridianId = meridianId;
            this.levelIdx = levelIdx;
            this.levelName = levelName ?? string.Empty;
            this.reqPlayerLevel = reqPlayerLevel;
            this.successRate = successRate;
            this.fallbackLevel = fallbackLevel;
            this.playerTier = playerTier;
            this.isUnlocked = isUnlocked;
            this.summary = summary ?? string.Empty;
        }
    }

    public sealed class MeridianPanelSnapshot
    {
        public int playerLevel;
        public int currentLevel;
        public int exp;
        public int totalLevels;
        public int unlockedLevels;
        public IReadOnlyList<int> meridianOrder = System.Array.Empty<int>();
        public IReadOnlyList<MeridianPanelRow> rows = System.Array.Empty<MeridianPanelRow>();
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

        public const string LabelMeridian = "Kinh Mạch";
        public const string LabelAcupoint = "Huyệt Đạo";
        public const string LabelCurrentLevel = "Cấp hiện tại";
        public const string LabelReqLevel = "Cấp yêu cầu";
        public const string LabelUnlocked = "Đã khai mở";
        public const string LabelLocked = "Chưa khai mở";

        /// <summary>
        /// Thứ tự 8 mạch theo PC meridian.txt (经脉ID 1-8). Khi đã có MeridianService,
        /// dùng <see cref="GetPcMeridianOrder(MeridianService)"/> để lấy thứ tự thật từ registry.
        /// </summary>
        private static readonly int[] PcMeridianOrder = { 1, 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>Thứ tự mạch mặc định (PC meridian.txt: 8 mạch, ID 1-8).</summary>
        public static IReadOnlyList<int> GetPcMeridianOrder() => PcMeridianOrder;

        /// <summary>Thứ tự mạch thật từ registry đã load (first-seen order). Fallback về thứ tự PC tĩnh.</summary>
        public static IReadOnlyList<int> GetPcMeridianOrder(MeridianService svc)
        {
            if (svc == null) return PcMeridianOrder;
            var ids = new List<int>();
            foreach (var m in svc.GetMeridianIds()) ids.Add(m);
            return ids.Count > 0 ? ids : (IReadOnlyList<int>)PcMeridianOrder;
        }

        /// <summary>
        /// Dựng snapshot panel kinh mạch từ MeridianService thật. svc==null → snapshot rỗng (không throw).
        /// Liệt kê toàn bộ huyệt đạo (8 mạch × 16 cấp) theo thứ tự mạch rồi theo cấp huyệt.
        /// </summary>
        public static MeridianPanelSnapshot BuildSnapshot(MeridianService svc, int playerLevel, int selectedMeridianId = 0, int selectedLevel = 0)
        {
            var snap = new MeridianPanelSnapshot { playerLevel = playerLevel };
            if (svc == null) return snap;

            var order = new List<int>();
            var rows = new List<MeridianPanelRow>();
            int unlocked = 0;

            foreach (var meridianId in svc.GetMeridianIds())
            {
                order.Add(meridianId);
                foreach (var p in svc.GetMeridianPoints(meridianId))
                {
                    if (p == null) continue;
                    int tier = svc.GetPlayerAcupointLevel(p.meridianId, p.acupointId);
                    bool isUnlocked = tier > 0;
                    if (isUnlocked) unlocked++;
                    var row = new MeridianPanelRow(
                        p.meridianId,
                        p.acupointId,
                        p.nameRaw,
                        p.acupointId, // PC: huyệt cấp N mở ở cấp nhân vật N.
                        p.successRate,
                        p.fallbackLevel,
                        tier,
                        isUnlocked,
                        BuildSummary(p, isUnlocked));
                    rows.Add(row);
                    if (selectedMeridianId > 0 && p.meridianId == selectedMeridianId
                        && selectedLevel > 0 && p.acupointId == selectedLevel)
                        snap.selectedRow = row;
                }
            }

            snap.meridianOrder = order.Count > 0 ? order : (IReadOnlyList<int>)PcMeridianOrder;
            snap.rows = rows;
            snap.totalLevels = rows.Count;
            snap.unlockedLevels = unlocked;
            return snap;
        }

        /// <summary>Thử đột phá một huyệt đạo (mạch, cấp). Trả về true nếu đột phá thành công.</summary>
        public static bool TryUpgrade(MeridianService svc, int playerLevel, int meridianId, int levelIdx)
        {
            if (svc == null || meridianId <= 0 || levelIdx <= 0) return false;
            return svc.TryUpgrade(meridianId, levelIdx, playerLevel) == UpgradeResult.Success;
        }

        /// <summary>Tổng số huyệt đạo người chơi đã khai mở (playerTier > 0).</summary>
        public static int GetProgress(MeridianService svc, int playerId)
        {
            if (svc == null) return 0;
            int n = 0;
            foreach (var meridianId in svc.GetMeridianIds())
                foreach (var p in svc.GetMeridianPoints(meridianId))
                    if (p != null && svc.GetPlayerAcupointLevel(p.meridianId, p.acupointId) > 0) n++;
            return n;
        }

        /// <summary>Mô tả huyệt đạo: tên + hiệu quả PC + trạng thái khai mở.</summary>
        public static string BuildSummary(PcMeridianEntry p, bool unlocked)
        {
            if (p == null) return string.Empty;
            string name = string.IsNullOrEmpty(p.nameRaw) ? LabelAcupoint : p.nameRaw;
            string status = unlocked ? LabelUnlocked : LabelLocked;
            string effect = string.IsNullOrEmpty(p.description) ? string.Empty : $" — {p.description}";
            // successRate /10000 → phần trăm hiển thị.
            int pct = p.successRate / 100;
            return $"{name} ({LabelReqLevel} {p.acupointId}, tỉ lệ {pct}%){effect} — {status}";
        }

        public static string BonusTypeName(int t)
        {
            switch (t)
            {
                case BonusHp: return "Sinh lực";
                case BonusMp: return "Nội lực";
                case BonusAtk: return "Công kích";
                case BonusDef: return "Phòng thủ";
                case BonusCrit: return "Bạo kích";
                case BonusBlock: return "Đỡ đòn";
                default: return $"Loại {t}";
            }
        }
    }
}
