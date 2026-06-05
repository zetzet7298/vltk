// -----------------------------------------------------------------------------
// VLTK Mobile — Friend Service (Bạn bè runtime)
// Quản lý danh sách bạn bè, thân mật, tin nhắn.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Thông tin bạn bè (snapshot cho UI).
    /// </summary>
    public class FriendEntry
    {
        public int friendId;
        public int friendPlayerId;
        public string friendName;
        public int level;
        public int faction;
        public bool isOnline;
        public long lastLoginSec;
        public int intimacy;
    }

    /// <summary>
    /// Service quản lý bạn bè.
    /// </summary>
    public class FriendService
    {
        public const string LogTag = "Friend";
        public const int MaxFriends = 100;

        private PcFriendRegistry _registry = new();
        private readonly Dictionary<int, Dictionary<int, FriendEntry>> _byPlayer = new();
        private readonly Dictionary<int, List<string>> _messages = new();
        private int _nextFriendId = 1;

        public int Count => _registry?.Count ?? 0;

        public FriendService() { }
        public FriendService(PcFriendRegistry reg) { _registry = reg ?? new PcFriendRegistry(); }
        public void AttachRegistry(PcFriendRegistry reg)
        {
            _registry = reg ?? new PcFriendRegistry();
            // Build cache từ registry
            foreach (var f in _registry.All)
            {
                if (!_byPlayer.TryGetValue(f.playerId, out var dict))
                {
                    dict = new Dictionary<int, FriendEntry>();
                    _byPlayer[f.playerId] = dict;
                }
                dict[f.friendId] = new FriendEntry
                {
                    friendId = f.friendId,
                    friendPlayerId = f.friendPlayerId,
                    friendName = $"Player_{f.friendPlayerId}",
                    level = 1,
                    faction = 0,
                    isOnline = false,
                    lastLoginSec = f.addedTimeUnix,
                    intimacy = f.intimacy,
                };
            }
        }

        /// <summary>Thêm bạn. Trả true nếu thêm mới.</summary>
        public bool AddFriend(int playerId, int friendId)
        {
            if (playerId <= 0 || friendId <= 0) return false;
            if (playerId == friendId) return false;
            if (!_byPlayer.TryGetValue(playerId, out var dict))
            {
                dict = new Dictionary<int, FriendEntry>();
                _byPlayer[playerId] = dict;
            }
            // Check duplicate
            foreach (var f in dict.Values)
                if (f.friendPlayerId == friendId) return false;
            int id = _nextFriendId++;
            dict[id] = new FriendEntry
            {
                friendId = id,
                friendPlayerId = friendId,
                friendName = $"Player_{friendId}",
                level = 1,
                faction = 0,
                isOnline = false,
                lastLoginSec = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                intimacy = 0,
            };
            return true;
        }

        /// <summary>Xóa bạn.</summary>
        public bool RemoveFriend(int playerId, int friendId)
        {
            if (!_byPlayer.TryGetValue(playerId, out var dict)) return false;
            int? keyToRemove = null;
            foreach (var kv in dict)
                if (kv.Value.friendPlayerId == friendId) { keyToRemove = kv.Key; break; }
            if (keyToRemove.HasValue) return dict.Remove(keyToRemove.Value);
            return false;
        }

        /// <summary>Danh sách bạn bè của player.</summary>
        public IReadOnlyList<FriendEntry> GetFriends(int playerId)
        {
            if (!_byPlayer.TryGetValue(playerId, out var dict))
                return System.Array.Empty<FriendEntry>();
            return new List<FriendEntry>(dict.Values);
        }

        /// <summary>Danh sách bạn đang online.</summary>
        public IReadOnlyList<FriendEntry> GetOnlineFriends(int playerId)
        {
            var list = new List<FriendEntry>();
            foreach (var f in GetFriends(playerId))
                if (f.isOnline) list.Add(f);
            return list;
        }

        /// <summary>Tăng thân mật. Trả về thân mật mới.</summary>
        public int AddIntimacy(int playerId, int friendId, int amount)
        {
            if (!_byPlayer.TryGetValue(playerId, out var dict)) return 0;
            foreach (var f in dict.Values)
            {
                if (f.friendPlayerId == friendId)
                {
                    f.intimacy = System.Math.Max(0, f.intimacy + amount);
                    return f.intimacy;
                }
            }
            return 0;
        }

        /// <summary>Top N bạn thân theo intimacy giảm dần.</summary>
        public IReadOnlyList<FriendEntry> GetBestFriends(int playerId, int n)
        {
            var list = new List<FriendEntry>(GetFriends(playerId));
            list.Sort((a, b) => b.intimacy.CompareTo(a.intimacy));
            if (n > 0 && list.Count > n) list.RemoveRange(n, list.Count - n);
            return list;
        }

        /// <summary>Gửi tin nhắn cho bạn.</summary>
        public bool SendMessage(int playerId, int friendId, string msg)
        {
            if (string.IsNullOrEmpty(msg)) return false;
            if (!_messages.TryGetValue(friendId, out var list))
            {
                list = new List<string>();
                _messages[friendId] = list;
            }
            list.Add($"{playerId}:{msg}");
            return true;
        }

        public IReadOnlyList<string> GetMessages(int playerId)
        {
            if (_messages.TryGetValue(playerId, out var list))
                return new List<string>(list);
            return System.Array.Empty<string>();
        }

        public static FriendService LoadFromStreamingAssets()
        {
            var svc = new FriendService();
            try
            {
                string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcFriend");
                var reg = PcFriendParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            catch (System.Exception)
            {
                // empty registry fallback
            }
            return svc;
        }
    }
}
