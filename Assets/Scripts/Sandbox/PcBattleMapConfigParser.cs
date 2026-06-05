// -----------------------------------------------------------------------------
// VLTK Mobile — PC battle map config parser (Battle Map Config - 80 battlefields)
// Source: battlemapconfig.txt (Reference/PcBattlefield).
// Columns: BattleMapId  MapId  BattleType  MaxPlayers  MinLevel  MaxLevel
//          ScoreWin  ScoreLoss  DurationSec  RespawnSec
// Vietnamese: "Cấu Hình Chiến Trường", "Tối Đa", "Thời Gian", "Hồi Sinh".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBattleMapConfigParser
    {
        public const int BattleMapIdCol = 0;
        public const int MapIdCol = 1;
        public const int BattleTypeCol = 2;
        public const int MaxPlayersCol = 3;
        public const int MinLevelCol = 4;
        public const int MaxLevelCol = 5;
        public const int ScoreWinCol = 6;
        public const int ScoreLossCol = 7;
        public const int DurationSecCol = 8;
        public const int RespawnSecCol = 9;

        public static List<PcBattleMapConfigEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleMapConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, BattleMapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcBattleMapConfigEntry
                {
                    battleMapId = id,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    battleType = PcItemCommon.Int(cols, BattleTypeCol),
                    maxPlayers = PcItemCommon.Int(cols, MaxPlayersCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    scoreWin = PcItemCommon.Int(cols, ScoreWinCol),
                    scoreLoss = PcItemCommon.Int(cols, ScoreLossCol),
                    durationSec = PcItemCommon.Int(cols, DurationSecCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnSecCol),
                });
            }
            return rows;
        }

        public static PcBattleMapConfigRegistry BuildRegistry(string dir)
        {
            var reg = new PcBattleMapConfigRegistry();
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
    public class PcBattleMapConfigEntry
    {
        public int battleMapId;
        public int mapId;
        public int battleType; // 0=Tống Kim, 1=Quốc Chiến, 2=Công Thành, 3=Boss, 4=PvP
        public int maxPlayers;
        public int minLevel;
        public int maxLevel;
        public int scoreWin;
        public int scoreLoss;
        public int durationSec;
        public int respawnSec;
    }

    public sealed class PcBattleMapConfigRegistry
    {
        private readonly Dictionary<int, PcBattleMapConfigEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcBattleMapConfigEntry e)
        {
            if (e == null || e.battleMapId <= 0) return;
            _byId[e.battleMapId] = e;
        }

        public PcBattleMapConfigEntry Get(int battleMapId)
            => _byId.TryGetValue(battleMapId, out var v) ? v : null;

        public IReadOnlyList<PcBattleMapConfigEntry> GetByBattleType(int battleType)
        {
            var list = new List<PcBattleMapConfigEntry>();
            foreach (var e in _byId.Values)
                if (e.battleType == battleType) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBattleMapConfigEntry> GetByMap(int mapId)
        {
            var list = new List<PcBattleMapConfigEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBattleMapConfigEntry> All => new List<PcBattleMapConfigEntry>(_byId.Values);
    }
}
