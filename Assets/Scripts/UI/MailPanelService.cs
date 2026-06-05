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
            var snap = new MailPanelSnapshot { playerId = playerId, rows = Array.Empty<MailPanelRow>() };
            if (mail == null || playerId <= 0) return snap;
            var rows = new List<MailPanelRow>();
            int unread = 0;
            int totalGold = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            foreach (var kv in mail.GetType().GetField("_allMails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) != null ? null : null) { }
            // Use public API
            foreach (var entry in EnumerateAll(mail))
            {
                if (entry.receiverId != playerId) continue;
                if (!entry.isRead) unread++;
                totalGold += entry.goldAmount;
                string preview = entry.body != null && entry.body.Length > 32 ? entry.body.Substring(0, 32) + "..." : entry.body;
                string ago = FormatTimeAgo(now - entry.sentTimeUnix);
                long left = Math.Max(0, 7L * 24 * 3600 - (now - entry.sentTimeUnix));
                rows.Add(new MailPanelRow(entry.mailId, entry.senderName, entry.title, preview, entry.itemId > 0, entry.goldAmount > 0, ago, entry.isRead, entry.isClaimed, left));
            }
            snap.unreadCount = unread;
            snap.totalCount = rows.Count;
            snap.totalGold = totalGold;
            snap.rows = rows;
            return snap;
        }

        public static bool TryClaim(MailService mail, int playerId, int mailId)
        {
            if (mail == null || mailId <= 0) return false;
            return mail.MarkClaimed(mailId);
        }

        public static bool MarkRead(MailService mail, int playerId, int mailId)
        {
            if (mail == null || mailId <= 0) return false;
            return mail.MarkRead(mailId);
        }

        public static int GetUnreadCount(MailService mail, int playerId)
        {
            if (mail == null || playerId <= 0) return 0;
            int count = 0;
            foreach (var entry in EnumerateAll(mail))
            {
                if (entry.receiverId == playerId && !entry.isRead) count++;
            }
            return count;
        }

        public static IReadOnlyList<MailPanelRow> GetRecentMails(MailService mail, int playerId, int count)
        {
            if (mail == null || playerId <= 0 || count <= 0) return Array.Empty<MailPanelRow>();
            var snap = BuildSnapshot(mail, playerId);
            var list = new List<MailPanelRow>(snap.rows);
            list.Sort((a, b) => b.mailId.CompareTo(a.mailId));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
            return list;
        }

        public static MailEntry ComposeNewMail(int playerId, int receiverId, string title, string body)
        {
            return new MailEntry
            {
                mailId = 0,
                senderId = playerId,
                receiverId = receiverId,
                title = title,
                body = body,
                sentTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            };
        }

        private static IEnumerable<MailEntry> EnumerateAll(MailService mail)
        {
            var field = typeof(MailService).GetField("_allMails", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field?.GetValue(mail) is Dictionary<int, MailEntry> dict) return dict.Values;
            return Array.Empty<MailEntry>();
        }

        private static string FormatTimeAgo(long sec)
        {
            if (sec < 60) return sec + " giây trước";
            if (sec < 3600) return (sec / 60) + " phút trước";
            if (sec < 86400) return (sec / 3600) + " giờ trước";
            return (sec / 86400) + " ngày trước";
        }
    }
}
