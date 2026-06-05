// -----------------------------------------------------------------------------
// VLTK Mobile — UI Hoa Sơn Luận Kiếm Panel Service (Vòng đấu Hoa Sơn)
// Giả lập giao diện đăng ký, xem vòng, nhận thưởng.
// Vietnamese: "Hoa Sơn Luận Kiếm", "Vòng 1", "Bán Kết", "Chung Kết".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct HuaShanPanelRow
    {
        public readonly int roundIdx;
        public readonly string roundName;
        public readonly int mapId;
        public readonly string mapName;
        public readonly string rewardPreview;
        public readonly bool isFinal;
        public readonly bool isMyRound;
        public readonly bool isAvailable;
        public readonly int participants;
        public readonly int timeLeftSec;

        public HuaShanPanelRow(int roundIdx, string roundName, int mapId, string mapName, string rewardPreview,
            bool isFinal, bool isMyRound, bool isAvailable, int participants, int timeLeftSec)
        {
            this.roundIdx = roundIdx;
            this.roundName = roundName ?? string.Empty;
            this.mapId = mapId;
            this.mapName = mapName ?? string.Empty;
            this.rewardPreview = rewardPreview ?? string.Empty;
            this.isFinal = isFinal;
            this.isMyRound = isMyRound;
            this.isAvailable = isAvailable;
            this.participants = participants;
            this.timeLeftSec = timeLeftSec;
        }
    }

    public sealed class HuaShanPanelSnapshot
    {
        public int playerId;
        public int currentRound;
        public int maxRound;
        public bool isRegistered;
        public int myPosition;     // 1=Top 1, 2=Top 2, ...
        public IReadOnlyList<HuaShanPanelRow> rows;
    }

    /// <summary>
    /// Panel service Hoa Sơn Luận Kiếm — đăng ký, xem vòng, thưởng.
    /// </summary>
    public static class HuaShanPanelService
    {
        public const int MaxRounds = 5;
        public const int FinalRoundIdx = 4;

        public static string GetRoundName(int roundIdx)
        {
            if (roundIdx <= 0) return "Vòng loại";
            if (roundIdx == 1) return "Vòng 1";
            if (roundIdx == 2) return "Vòng 2";
            if (roundIdx == 3) return "Bán Kết";
            if (roundIdx >= 4) return "Chung Kết";
            return "Vòng " + (roundIdx + 1);
        }

        public static HuaShanPanelSnapshot BuildSnapshot(HuaShanLuanJianService svc, int playerId, int currentRound)
        {
            var snap = new HuaShanPanelSnapshot
            {
                playerId = playerId,
                currentRound = currentRound,
                maxRound = MaxRounds,
                isRegistered = false,
                myPosition = 0,
                rows = new List<HuaShanPanelRow>(),
            };
            if (svc == null) return snap;

            try
            {
                var list = new List<HuaShanPanelRow>(MaxRounds);
                for (int r = 0; r < MaxRounds; r++)
                {
                    var row = new HuaShanPanelRow(
                        roundIdx: r,
                        roundName: GetRoundName(r),
                        mapId: 100 + r,
                        mapName: "Đấu Trường Hoa Sơn " + (r + 1),
                        rewardPreview: "Phần thưởng: " + (r + 1) * 1000 + " vàng",
                        isFinal: r == FinalRoundIdx,
                        isMyRound: r == currentRound,
                        isAvailable: r <= currentRound,
                        participants: 32 - r * 6,
                        timeLeftSec: 600 - r * 60
                    );
                    list.Add(row);
                }
                snap.rows = list;
            }
            catch { }
            return snap;
        }

        public static HuaShanPanelRow? GetCurrentRound(HuaShanLuanJianService svc)
        {
            if (svc == null) return null;
            var snap = BuildSnapshot(svc, 0, 0);
            foreach (var r in snap.rows)
                if (r.isMyRound) return r;
            return null;
        }

        public static bool TryRegister(HuaShanLuanJianService svc, int playerId)
        {
            if (svc == null) return false;
            return true;
        }

        public static string GetFinalReward(HuaShanLuanJianService svc)
        {
            if (svc == null) return string.Empty;
            return "Danh Hiệu Hoa Sơn Đệ Nhất + 100,000 vàng + Trang bị tím";
        }
    }
}
