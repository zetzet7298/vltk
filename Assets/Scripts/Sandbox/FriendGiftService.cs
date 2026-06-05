// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.x Friend Gift runtime service
// Wraps PcFriendGiftRegistry. PC source: settings/friend/friendgift.txt.
// Quản lý quà tặng bạn bè: theo cấp độ thân thiết, giới hạn hằng ngày.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Quà Bạn Bè: kiểm tra cấp độ thân thiết, giới hạn hằng ngày.
    /// </summary>
    public class FriendGiftService
    {
        public const string LogTag = "FriendGift";
        public const string DefaultStreamingDir = "Reference/PcFriend";

        private PcFriendGiftRegistry _reg;

        public int Count => _reg?.Count ?? 0;

        public FriendGiftService() { }
        public FriendGiftService(PcFriendGiftRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcFriendGiftRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "FriendGift registry rỗng");
        }

        public PcFriendGiftEntry GetGift(int id) => _reg != null ? _reg.Get(id) : null;

        public IReadOnlyList<PcFriendGiftEntry> GetByFriendship(int friendshipLevel)
            => _reg != null ? _reg.GetByFriendship(friendshipLevel) : Array.Empty<PcFriendGiftEntry>();

        public IReadOnlyList<PcFriendGiftEntry> All
            => _reg != null ? _reg.All : Array.Empty<PcFriendGiftEntry>();

        public bool CanSendGift(int giftId, int friendshipLevel, int alreadySentToday)
        {
            var entry = GetGift(giftId);
            if (entry == null) return false;
            if (friendshipLevel < entry.friendshipRequired) return false;
            if (entry.dailyLimit > 0 && alreadySentToday >= entry.dailyLimit) return false;
            return true;
        }

        public IReadOnlyList<PcFriendGiftEntry> GetAvailableGifts(int friendshipLevel)
        {
            return GetByFriendship(friendshipLevel);
        }

        public static FriendGiftService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcFriendGiftParser.BuildRegistry(dir);
            return new FriendGiftService(reg);
        }
    }
}
