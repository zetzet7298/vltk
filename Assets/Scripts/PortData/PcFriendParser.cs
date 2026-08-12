// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/friendlist.txt parser
// Source: friendlist.txt (Bạn bè).
// Cols: FriendId, PlayerId, FriendPlayerId, Intimacy, AddedTimeUnix
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFriendParser
    {
        public const int FriendIdCol = 0;
        public const int PlayerIdCol = 1;
        public const int FriendPlayerIdCol = 2;
        public const int IntimacyCol = 3;
        public const int AddedTimeUnixCol = 4;

        public static List<PcFriendEntry> ParseFile(string path)
        {
            var rows = new List<PcFriendEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, FriendIdCol);
                if (id <= 0) continue;
                rows.Add(new PcFriendEntry
                {
                    friendId = id,
                    playerId = PcItemCommon.Int(cols, PlayerIdCol),
                    friendPlayerId = PcItemCommon.Int(cols, FriendPlayerIdCol),
                    intimacy = PcItemCommon.Int(cols, IntimacyCol),
                    addedTimeUnix = PcItemCommon.Int(cols, AddedTimeUnixCol),
                });
            }
            return rows;
        }

        public static PcFriendRegistry BuildRegistry(string dir)
        {
            var reg = new PcFriendRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcFriendEntry
    {
        public int friendId;
        public int playerId;
        public int friendPlayerId;
        public int intimacy;
        public int addedTimeUnix;
    }

    public sealed class PcFriendRegistry
    {
        private readonly Dictionary<int, PcFriendEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcFriendEntry e) { if (e == null || e.friendId <= 0) return; _byId[e.friendId] = e; }
        public PcFriendEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcFriendEntry> GetByPlayer(int playerId)
        {
            var list = new List<PcFriendEntry>();
            foreach (var e in _byId.Values)
                if (e.playerId == playerId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcFriendEntry> All => new List<PcFriendEntry>(_byId.Values);
    }
}
