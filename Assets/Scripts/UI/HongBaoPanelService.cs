// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel Service cho Hồng Bao (Red Envelope Panel)
// Reference: PC hongbao system + HongbaoService.
// Vietnamese: "Hồng Bao", "Đã gửi", "Đã nhận", "Còn lại", "Tin nhắn".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct HongBaoPanelRow
    {
        public readonly int hongbaoId;
        public readonly int senderId;
        public readonly string senderName;
        public readonly int totalAmount;
        public readonly int claimedAmount;
        public readonly int claimerCount;
        public readonly int maxClaimer;
        public readonly string message;
        public readonly int timeLeftSec;
        public readonly bool isClaimed;

        public HongBaoPanelRow(int hongbaoId, int senderId, string senderName, int totalAmount, int claimedAmount, int claimerCount, int maxClaimer, string message, int timeLeftSec, bool isClaimed)
        {
            this.hongbaoId = hongbaoId;
            this.senderId = senderId;
            this.senderName = senderName;
            this.totalAmount = totalAmount;
            this.claimedAmount = claimedAmount;
            this.claimerCount = claimerCount;
            this.maxClaimer = maxClaimer;
            this.message = message;
            this.timeLeftSec = timeLeftSec;
            this.isClaimed = isClaimed;
        }
    }

    public sealed class HongBaoPanelSnapshot
    {
        public int playerId;
        public int totalSent;
        public int totalClaimed;
        public IReadOnlyList<HongBaoPanelRow> rows;
    }

    public static class HongBaoPanelService
    {
        public static HongBaoPanelSnapshot BuildSnapshot(HongbaoService svc, int playerId)
        {
            var snap = new HongBaoPanelSnapshot
            {
                playerId = playerId,
                totalSent = 0,
                totalClaimed = 0,
                rows = System.Array.Empty<HongBaoPanelRow>(),
            };
            if (svc == null) return snap;
            int nowSec = (int)(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var list = new List<HongBaoPanelRow>();
            foreach (var h in svc.GetAllHongbaos())
            {
                int timeLeft = h.expireTime > 0 ? Math.Max(0, h.expireTime - nowSec) : 0;
                int total = h.totalAmount;
                int claimed = h.claimedAmount;
                int claimer = h.claimerCount;
                bool isClaimed = svc.HasClaimed(playerId, h.id);
                if (h.senderId == playerId) snap.totalSent += total;
                if (isClaimed) snap.totalClaimed += svc.GetClaimedAmount(playerId, h.id);
                list.Add(new HongBaoPanelRow(
                    h.id,
                    h.senderId,
                    h.senderName ?? "Ẩn danh",
                    total,
                    claimed,
                    claimer,
                    h.maxClaimer,
                    h.message ?? string.Empty,
                    timeLeft,
                    isClaimed));
            }
            snap.rows = list;
            return snap;
        }

        public static bool TrySend(HongbaoService svc, int playerId, int amount, string message)
        {
            if (svc == null || amount <= 0) return false;
            return svc.Send(playerId, amount, message ?? string.Empty);
        }

        public static bool TryClaim(HongbaoService svc, int playerId, int hongbaoId)
        {
            if (svc == null || hongbaoId <= 0) return false;
            if (!svc.CanClaim(hongbaoId, 0)) return false;
            return svc.Claim(hongbaoId, 0) > 0;
        }

        public static int GetClaimedAmount(HongbaoService svc, int playerId, int hongbaoId)
        {
            if (svc == null || hongbaoId <= 0) return 0;
            return svc.GetClaimedAmount(playerId, hongbaoId);
        }
    }
}
