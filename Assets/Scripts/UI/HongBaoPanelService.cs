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
            return new HongBaoPanelSnapshot { rows = System.Array.Empty<HongBaoPanelRow>() };
        }

        public static bool TrySend(HongbaoService svc, int playerId, int amount, string message)
        {
            return false;
        }

        public static bool TryClaim(HongbaoService svc, int playerId, int hongbaoId)
        {
            return false;
        }

        public static int GetClaimedAmount(HongbaoService svc, int playerId, int hongbaoId)
        {
            return 0;
        }

    }
}
