// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tống Kim battle parser (80 battles trên PC)
// Source: settings/battle/tongjinbattle.txt (Reference/PcBattlefield).
// File format (GB2312, tab-separated):
//   BattleId  Name  MapId  SongCampId  JinCampId  MinLevel  MaxLevel
//   RequiredCount  ScoreWin  ScoreLoss  RewardItem
// Trả về registry runtime tra cứu theo battleId / mapId / level.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTongJinBattleParser
    {
        public const int NameCol = 1;
        public const int MapIdCol = 2;
        public const int SongCampIdCol = 3;
        public const int JinCampIdCol = 4;
        public const int MinLevelCol = 5;
        public const int MaxLevelCol = 6;
        public const int RequiredCountCol = 7;
        public const int ScoreWinCol = 8;
        public const int ScoreLossCol = 9;
        public const int RewardItemCol = 10;

        /// <summary>Parse 1 file .txt Tống Kim battle. Trả về danh sách entries (rỗng nếu lỗi).</summary>
        public static List<PcTongJinBattleEntry> ParseFile(string path)
        {
            var rows = new List<PcTongJinBattleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, 0);
                if (id <= 0) continue;
                rows.Add(new PcTongJinBattleEntry
                {
                    battleId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    songCampId = PcItemCommon.Int(cols, SongCampIdCol),
                    jinCampId = PcItemCommon.Int(cols, JinCampIdCol),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    requiredCount = PcItemCommon.Int(cols, RequiredCountCol),
                    scoreWin = PcItemCommon.Int(cols, ScoreWinCol),
                    scoreLoss = PcItemCommon.Int(cols, ScoreLossCol),
                    rewardItem = PcItemCommon.Int(cols, RewardItemCol),
                });
            }
            return rows;
        }

        /// <summary>Build full registry từ thư mục (đệ quy). Bỏ qua file rỗng/lỗi.</summary>
        public static PcTongJinBattleRegistry BuildRegistry(string dir)
        {
            var reg = new PcTongJinBattleRegistry();
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
    public class PcTongJinBattleEntry
    {
        public int battleId;
        public string nameRaw;
        public int mapId;
        public int songCampId;
        public int jinCampId;
        public int minLevel;
        public int maxLevel;
        public int requiredCount;
        public int scoreWin;
        public int scoreLoss;
        public int rewardItem;
    }

    public sealed class PcTongJinBattleRegistry
    {
        private readonly Dictionary<int, PcTongJinBattleEntry> _byId = new();

        public int Count => _byId.Count;

        public void Register(PcTongJinBattleEntry e)
        {
            if (e == null || e.battleId <= 0) return;
            _byId[e.battleId] = e;
        }

        public PcTongJinBattleEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcTongJinBattleEntry> GetByMap(int mapId)
        {
            var list = new List<PcTongJinBattleEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTongJinBattleEntry> GetByLevel(int level)
        {
            var list = new List<PcTongJinBattleEntry>();
            foreach (var e in _byId.Values)
                if (level >= e.minLevel && level <= e.maxLevel) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTongJinBattleEntry> All
            => new List<PcTongJinBattleEntry>(_byId.Values);
    }
}
