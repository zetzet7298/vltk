// -----------------------------------------------------------------------------
// VLTK Mobile — Mail Service (Hệ thống thư trong game)
// Quản lý gửi/nhận thư giữa các nhân vật, kèm vật phẩm/vàng đính kèm.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Một lá thư trong hệ thống.
    /// </summary>
    [Serializable]
    public class MailEntry
    {
        public int mailId;
        public int senderId;
        public string senderName;
        public int receiverId;
        public string title;
        public string body;
        public int itemId;
        public int itemCount;
        public int goldAmount;
        public long sentTimeUnix;
        public bool isRead;
        public bool isClaimed;
    }

    /// <summary>
    /// Service quản lý thư.
    /// </summary>
    public class MailService
    {
        public const string LogTag = "Mail";

        private readonly Dictionary<int, MailEntry> _allMails = new();
        private int _nextId = 1;

        public int Count => _allMails.Count;

        public MailService() { }

        /// <summary>Gửi thư cho người nhận. Trả về mailId (>0 nếu thành công).</summary>
        public int SendMail(int senderId, int receiverId, string title, string body, int itemId = 0, int itemCount = 0, int gold = 0)
        {
            if (receiverId <= 0) return 0;
            if (string.IsNullOrEmpty(title)) return 0;
            int id = _nextId++;
            _allMails[id] = new MailEntry
            {
                mailId = id,
                senderId = senderId,
                senderName = senderId > 0 ? $"Player_{senderId}" : "Hệ Thống",
                receiverId = receiverId,
                title = title,
                body = body ?? string.Empty,
                itemId = itemId,
                itemCount = itemCount,
                goldAmount = gold,
                sentTimeUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                isRead = false,
                isClaimed = false,
            };
            return id;
        }

        /// <summary>Danh sách thư của 1 người chơi (sắp xếp mới nhất trước).</summary>
        public IReadOnlyList<MailEntry> GetMails(int playerId)
        {
            var list = new List<MailEntry>();
            foreach (var m in _allMails.Values)
                if (m.receiverId == playerId) list.Add(m);
            list.Sort((a, b) => b.sentTimeUnix.CompareTo(a.sentTimeUnix));
            return list;
        }

        /// <summary>Đánh dấu đã đọc.</summary>
        public bool MarkRead(int mailId)
        {
            if (!_allMails.TryGetValue(mailId, out var m)) return false;
            if (m.isRead) return true;
            m.isRead = true;
            return true;
        }

        /// <summary>Nhận vật phẩm/vàng đính kèm.</summary>
        public bool ClaimMail(int mailId)
        {
            if (!_allMails.TryGetValue(mailId, out var m)) return false;
            if (m.isClaimed) return false;
            m.isClaimed = true;
            return true;
        }

        /// <summary>Xóa thư.</summary>
        public bool DeleteMail(int mailId) => _allMails.Remove(mailId);

        /// <summary>Số thư chưa đọc của 1 người chơi.</summary>
        public int GetUnreadCount(int playerId)
        {
            int n = 0;
            foreach (var m in _allMails.Values)
                if (m.receiverId == playerId && !m.isRead) n++;
            return n;
        }

        /// <summary>Nhận tất cả thư có thể. Trả về số thư đã nhận.</summary>
        public int ClaimAll(int playerId)
        {
            int n = 0;
            foreach (var m in _allMails.Values)
            {
                if (m.receiverId == playerId && !m.isClaimed)
                {
                    m.isClaimed = true;
                    n++;
                }
            }
            return n;
        }

        public static MailService LoadFromStreamingAssets() => new MailService();
    }
}
