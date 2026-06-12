// -----------------------------------------------------------------------------
// VLTK Mobile — Mail Panel Service (Hòm Thư)
// Dựng snapshot cho UI hòm thư từ MailService runtime (lọc theo playerId).
// mail==null → snapshot rỗng (null-safe, không throw). ComposeNewMail tạo một
// MailEntry mới chưa gửi để UI soạn thư (senderId/receiverId/title/body).
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
            this.senderName = senderName ?? string.Empty;
            this.title = title ?? string.Empty;
            this.bodyPreview = bodyPreview ?? string.Empty;
            this.hasItem = hasItem;
            this.hasGold = hasGold;
            this.sentTimeAgo = sentTimeAgo ?? string.Empty;
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
        public IReadOnlyList<MailPanelRow> rows = System.Array.Empty<MailPanelRow>();
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

        /// <summary>Giới hạn ký tự xem trước nội dung thư trong danh sách.</summary>
        public const int BodyPreviewLength = 40;

        public static MailPanelSnapshot BuildSnapshot(MailService mail, int playerId)
        {
            if (mail == null)
                return new MailPanelSnapshot { playerId = playerId };

            var mails = mail.GetMails(playerId);
            var rows = new List<MailPanelRow>(mails.Count);
            int unread = 0;
            int totalGold = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            foreach (var m in mails)
            {
                if (m == null) continue;
                if (!m.isRead) unread++;
                totalGold += m.goldAmount;
                rows.Add(new MailPanelRow(
                    m.mailId,
                    string.IsNullOrEmpty(m.senderName) ? (m.senderId > 0 ? $"Player_{m.senderId}" : "Hệ Thống") : m.senderName,
                    m.title,
                    Preview(m.body),
                    m.itemId > 0,
                    m.goldAmount > 0,
                    FormatTimeAgo(now - m.sentTimeUnix),
                    m.isRead,
                    m.isClaimed,
                    0));
            }

            return new MailPanelSnapshot
            {
                playerId = playerId,
                unreadCount = unread,
                totalCount = rows.Count,
                totalGold = totalGold,
                rows = rows,
            };
        }

        public static bool TryClaim(MailService mail, int playerId, int mailId)
        {
            if (mail == null || mailId <= 0) return false;
            return mail.ClaimMail(mailId);
        }

        public static bool MarkRead(MailService mail, int playerId, int mailId)
        {
            if (mail == null || mailId <= 0) return false;
            return mail.MarkRead(mailId);
        }

        public static int GetUnreadCount(MailService mail, int playerId)
        {
            if (mail == null) return 0;
            return mail.GetUnreadCount(playerId);
        }

        public static IReadOnlyList<MailPanelRow> GetRecentMails(MailService mail, int playerId, int count)
        {
            if (mail == null || count <= 0) return System.Array.Empty<MailPanelRow>();
            var snap = BuildSnapshot(mail, playerId);
            if (snap.rows.Count <= count) return snap.rows;
            var top = new List<MailPanelRow>(count);
            for (int i = 0; i < count; i++) top.Add(snap.rows[i]);
            return top;
        }

        /// <summary>
        /// Soạn một lá thư mới (chưa gửi) cho UI. Trả về MailEntry không null để UI
        /// gắn item/vàng trước khi gọi MailService.SendMail.
        /// </summary>
        public static MailEntry ComposeNewMail(int playerId, int receiverId, string title, string body)
        {
            return new MailEntry
            {
                mailId = 0,
                senderId = playerId,
                senderName = playerId > 0 ? $"Player_{playerId}" : "Hệ Thống",
                receiverId = receiverId,
                title = title ?? string.Empty,
                body = body ?? string.Empty,
                itemId = 0,
                itemCount = 0,
                goldAmount = 0,
                sentTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                isRead = false,
                isClaimed = false,
            };
        }

        private static string Preview(string body)
        {
            if (string.IsNullOrEmpty(body)) return string.Empty;
            if (body.Length <= BodyPreviewLength) return body;
            return body.Substring(0, BodyPreviewLength) + "…";
        }

        private static string FormatTimeAgo(long secondsAgo)
        {
            if (secondsAgo < 0) secondsAgo = 0;
            if (secondsAgo < 60) return "Vừa xong";
            if (secondsAgo < 3600) return $"{secondsAgo / 60} phút trước";
            if (secondsAgo < 86400) return $"{secondsAgo / 3600} giờ trước";
            return $"{secondsAgo / 86400} ngày trước";
        }
    }
}
