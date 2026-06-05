// -----------------------------------------------------------------------------
// VLTK Mobile — PC SongJin (Tống Kim) battle tier parser (Sơ/Trung/Cao)
// Source: songjin_tier.txt (Reference/PcBattlefield).
// Columns: TierId  Name  Tier  MapId  MinLevel  MaxLevel  MaxPlayers  ScoreWin
// Tier: 0=Sơ Cấp, 1=Trung Cấp, 2=Cao Cấp
// Vietnamese: "Tống Kim", "Cấp Bậc", "Sơ Cấp", "Trung Cấp", "Cao Cấp".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSjBattleParser
    {
        public const int TierIdCol = 0;
        public const int NameCol = 1;
        public const int TierCol = 2;
        public const int MapIdCol = 3;
        public const int MinLevelCol = 4;
        public const int MaxLevelCol = 5;
        public const int MaxPlayersCol = 6;
        public const int ScoreWinCol = 7;

        public static List<PcSjBattleEntry> ParseFile(string path)
        {
            var rows = new List<PcSjBattleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                int id = PcItemCommon.Int(cols, TierIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSjBattleEntry
                {
                    tierId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    tier = PcItemCommon.Int(cols, TierCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    maxPlayers = PcItemCommon.Int(cols, MaxPlayersCol),
                    scoreWin = PcItemCommon.Int(cols, ScoreWinCol),
                });
            }
            return rows;
        }

        public static PcSjBattleRegistry BuildRegistry(string dir)
        {
            var reg = new PcSjBattleRegistry();
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
    public class PcSjBattleEntry
    {
        public int tierId;
        public string name;
        public int tier; // 0=Sơ, 1=Trung, 2=Cao
        public int mapId;
        public int minLevel;
        public int maxLevel;
        public int maxPlayers;
        public int scoreWin;
    }

    public sealed class PcSjBattleRegistry
    {
        private readonly Dictionary<int, PcSjBattleEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcSjBattleEntry e)
        {
            if (e == null || e.tierId <= 0) return;
            _byId[e.tierId] = e;
        }

        public PcSjBattleEntry Get(int tierId)
            => _byId.TryGetValue(tierId, out var v) ? v : null;

        public IReadOnlyList<PcSjBattleEntry> GetByTier(int tier)
        {
            var list = new List<PcSjBattleEntry>();
            foreach (var e in _byId.Values)
                if (e.tier == tier) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcSjBattleEntry> GetByMap(int mapId)
        {
            var list = new List<PcSjBattleEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcSjBattleEntry> All => new List<PcSjBattleEntry>(_byId.Values);
    }
}
