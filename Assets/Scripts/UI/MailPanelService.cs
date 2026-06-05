// -----------------------------------------------------------------------------
// VLTK Mobile — Mail Panel Service (Hòm Thư)
// Dựng snapshot cho UI hòm thư. Kết hợp MailService + lọc theo playerId.
// Vietnamese: "Hòm Thư", "Chưa đọc", "Đã đọc", "Nhận thưởng", "Gửi thư".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct MailPanelRow
    {
        public readonly int mailId;
        public readonly string senderName;
        public readonly string title;
        public readonly string bodyPreview;
        public readonly bool hasItem;
        public readonly bool hasGold;
        public readonly string sentTimeAgo;
        public readonly bool isRead;
        public readonly bool isClaimed;
        public readonly long timeLeftSec;

        public MailPanelRow(int mailId, string senderName, string title, string bodyPreview, bool hasItem, bool hasGold, string sentTimeAgo, bool isRead, bool isClaimed, long timeLeftSec)
        {
            this.mailId = mailId;
            this.senderName = senderName;
            this.title = title;
            this.bodyPreview = bodyPreview;
            this.hasItem = hasItem;
            this.hasGold = hasGold;
            this.sentTimeAgo = sentTimeAgo;
            this.isRead = isRead;
            this.isClaimed = isClaimed;
            this.timeLeftSec = timeLeftSec;
        }
    }

    public sealed class MailPanelSnapshot
    {
        public int playerId;
        public int unreadCount;
        public int totalCount;
        public int totalGold;
        public IReadOnlyList<MailPanelRow> rows;
    }

    public static class MailPanelService
    {
        public const string LabelInbox = "Hòm Thư";
        public const string LabelUnread = "Chưa đọc";
        public const string LabelRead = "Đã đọc";
        public const string LabelClaim = "Nhận thưởng";
        public const string LabelSend = "Gửi thư";
        public const string LabelTitle = "Tiêu đề";
        public const string LabelBody = "Nội dung";

        public static MailPanelSnapshot BuildSnapshot(MailService mail, int playerId)
        {
            return new MailPanelSnapshot { rows = System.Array.Empty<MailPanelRow>() };
        }

        public static bool TryClaim(MailService mail, int playerId, int mailId)
        {
            return false;
        }

        public static bool MarkRead(MailService mail, int playerId, int mailId)
        {
            return false;
        }

        public static int GetUnreadCount(MailService mail, int playerId)
        {
            return 0;
        }

        public static IReadOnlyList<MailPanelRow> GetRecentMails(MailService mail, int playerId, int count)
        {
            return System.Array.Empty<MailPanelRow>();
        }

        public static MailEntry ComposeNewMail(int playerId, int receiverId, string title, string body)
        {
            return default;
        }

    }
}
