// -----------------------------------------------------------------------------
// VLTK Mobile — ST-9.10 Guild City War Log parser
// Source: guildcitywarlog.txt (Reference/PcTong or PcBattlefield).
// Cols: LogId  WarId  CityId  EventType  TongId  PlayerId  EventTimeUnix  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildCityWarLogParser
    {
        public const int LogIdCol = 0;
        public const int WarIdCol = 1;
        public const int CityIdCol = 2;
        public const int EventTypeCol = 3;
        public const int TongIdCol = 4;
        public const int PlayerIdCol = 5;
        public const int EventTimeUnixCol = 6;
        public const int DescriptionCol = 7;

        public static List<PcGuildCityWarLogEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildCityWarLogEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int id = PcItemCommon.Int(cols, LogIdCol);
                if (id <= 0) continue;
                rows.Add(new PcGuildCityWarLogEntry
                {
                    logId = id,
                    warId = PcItemCommon.Int(cols, WarIdCol),
                    cityId = PcItemCommon.Int(cols, CityIdCol),
                    eventType = PcItemCommon.Int(cols, EventTypeCol),
                    tongId = PcItemCommon.Int(cols, TongIdCol),
                    playerId = PcItemCommon.Int(cols, PlayerIdCol),
                    eventTimeUnix = PcItemCommon.Int(cols, EventTimeUnixCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcGuildCityWarLogRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildCityWarLogRegistry();
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
    public class PcGuildCityWarLogEntry
    {
        public int logId;
        public int warId;
        public int cityId;
        public int eventType;
        public int tongId;
        public int playerId;
        public int eventTimeUnix;
        public string description;
    }

    public sealed class PcGuildCityWarLogRegistry
    {
        private readonly Dictionary<int, PcGuildCityWarLogEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcGuildCityWarLogEntry e)
        {
            if (e == null || e.logId <= 0) return;
            _byId[e.logId] = e;
        }

        public PcGuildCityWarLogEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcGuildCityWarLogEntry> GetByWar(int warId)
        {
            var list = new List<PcGuildCityWarLogEntry>();
            foreach (var e in _byId.Values) if (e.warId == warId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGuildCityWarLogEntry> GetByCity(int cityId)
        {
            var list = new List<PcGuildCityWarLogEntry>();
            foreach (var e in _byId.Values) if (e.cityId == cityId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGuildCityWarLogEntry> GetByTong(int tongId)
        {
            var list = new List<PcGuildCityWarLogEntry>();
            foreach (var e in _byId.Values) if (e.tongId == tongId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGuildCityWarLogEntry> All => new List<PcGuildCityWarLogEntry>(_byId.Values);
    }
}
