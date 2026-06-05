// -----------------------------------------------------------------------------
// VLTK Mobile — PC Công Thành Chiến (Bang Chiến) parser (7 thành trên PC)
// Source: settings/battle/bangchien.txt (Reference/PcCity).
// File format (GB2312, tab-separated):
//   CityId  Name  MapId  CastleLevel  OwnerTongId  Income  Defenders
//   Attackers  Reward  OpenDay
// Trả về registry runtime tra cứu theo cityId / mapId / tongId.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBangChienParser
    {
        public const int NameCol = 1;
        public const int MapIdCol = 2;
        public const int CastleLevelCol = 3;
        public const int OwnerTongIdCol = 4;
        public const int IncomeCol = 5;
        public const int DefendersCol = 6;
        public const int AttackersCol = 7;
        public const int RewardCol = 8;
        public const int OpenDayCol = 9;

        public static List<PcBangChienEntry> ParseFile(string path)
        {
            var rows = new List<PcBangChienEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, 0);
                if (id <= 0) continue;
                rows.Add(new PcBangChienEntry
                {
                    cityId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    castleLevel = PcItemCommon.Int(cols, CastleLevelCol),
                    ownerTongId = PcItemCommon.Int(cols, OwnerTongIdCol),
                    income = PcItemCommon.Int(cols, IncomeCol),
                    defenders = PcItemCommon.Int(cols, DefendersCol),
                    attackers = PcItemCommon.Int(cols, AttackersCol),
                    reward = PcItemCommon.Int(cols, RewardCol),
                    openDay = PcItemCommon.Int(cols, OpenDayCol),
                });
            }
            return rows;
        }

        public static PcBangChienRegistry BuildRegistry(string dir)
        {
            var reg = new PcBangChienRegistry();
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
    public class PcBangChienEntry
    {
        public int cityId;
        public string nameRaw;
        public int mapId;
        public int castleLevel;
        public int ownerTongId;
        public int income;
        public int defenders;
        public int attackers;
        public int reward;
        public int openDay;
    }

    public sealed class PcBangChienRegistry
    {
        private readonly Dictionary<int, PcBangChienEntry> _byId = new();

        public int Count => _byId.Count;

        public void Register(PcBangChienEntry e)
        {
            if (e == null || e.cityId <= 0) return;
            _byId[e.cityId] = e;
        }

        public PcBangChienEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcBangChienEntry> GetByMap(int mapId)
        {
            var list = new List<PcBangChienEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBangChienEntry> GetByTong(int tongId)
        {
            var list = new List<PcBangChienEntry>();
            foreach (var e in _byId.Values)
                if (e.ownerTongId == tongId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBangChienEntry> All
            => new List<PcBangChienEntry>(_byId.Values);
    }
}
