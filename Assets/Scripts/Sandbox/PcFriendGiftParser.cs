// -----------------------------------------------------------------------------
// VLTK Mobile — PC friendgift.txt parser
// Source: settings/friend/friendgift.txt (Quà Bạn Bè).
// Columns: GiftId Name ItemId ItemCount FriendshipRequired DailyLimit Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFriendGiftParser
    {
        public const int GiftIdCol = 0;
        public const int NameCol = 1;
        public const int ItemIdCol = 2;
        public const int ItemCountCol = 3;
        public const int FriendshipRequiredCol = 4;
        public const int DailyLimitCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcFriendGiftEntry> ParseFile(string path)
        {
            var rows = new List<PcFriendGiftEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, GiftIdCol);
                if (id <= 0) continue;
                rows.Add(new PcFriendGiftEntry
                {
                    giftId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    itemId = PcItemCommon.Int(cols, ItemIdCol),
                    itemCount = PcItemCommon.Int(cols, ItemCountCol),
                    friendshipRequired = PcItemCommon.Int(cols, FriendshipRequiredCol),
                    dailyLimit = PcItemCommon.Int(cols, DailyLimitCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcFriendGiftRegistry BuildRegistry(string dir)
        {
            var reg = new PcFriendGiftRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("friendgift"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFriendGiftEntry
    {
        public int giftId;
        public string nameRaw;
        public int itemId;
        public int itemCount;
        public int friendshipRequired;
        public int dailyLimit;
        public string description;
    }

    public sealed class PcFriendGiftRegistry
    {
        private readonly Dictionary<int, PcFriendGiftEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcFriendGiftEntry e) { if (e == null || e.giftId <= 0) return; _byId[e.giftId] = e; }
        public PcFriendGiftEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcFriendGiftEntry> GetByFriendship(int friendshipLevel)
        {
            var list = new List<PcFriendGiftEntry>();
            foreach (var e in _byId.Values)
                if (e.friendshipRequired <= friendshipLevel) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcFriendGiftEntry> All => new List<PcFriendGiftEntry>(_byId.Values);
    }
}
