// -----------------------------------------------------------------------------
// VLTK Mobile — Seasonal Event (Sự Kiện Mùa) parser
// Source: settings/event/seasonal_events.txt.
//   EventId  Name  StartMonth  EndMonth  Type  RewardId  RewardCount
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox.ItemData;

namespace VLTK.Sandbox
{
    public static class PcSeasonalEventParser
    {
        public const int EventIdCol = 0;
        public const int NameCol = 1;
        public const int StartMonthCol = 2;
        public const int EndMonthCol = 3;
        public const int TypeCol = 4;
        public const int RewardIdCol = 5;
        public const int RewardCountCol = 6;

        public static List<PcSeasonalEventEntry> ParseFile(string path)
        {
            var rows = new List<PcSeasonalEventEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcSeasonalEventEntry
                {
                    eventId = PcItemCommon.Int(cols, EventIdCol),
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    startMonth = PcItemCommon.Int(cols, StartMonthCol),
                    endMonth = PcItemCommon.Int(cols, EndMonthCol),
                    type = cols.Length > TypeCol ? PcItemCommon.Int(cols, TypeCol) : 0,
                    rewardId = cols.Length > RewardIdCol ? PcItemCommon.Int(cols, RewardIdCol) : 0,
                    rewardCount = cols.Length > RewardCountCol ? PcItemCommon.Int(cols, RewardCountCol) : 0,
                });
            }
            return rows;
        }

        public static PcSeasonalEventRegistry BuildRegistry(string dir)
        {
            var reg = new PcSeasonalEventRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSeasonalEventEntry
    {
        public int eventId;
        public string nameRaw;
        public int startMonth;
        public int endMonth;
        public int type;
        public int rewardId;
        public int rewardCount;

        public bool IsActiveInMonth(int month)
        {
            if (startMonth <= endMonth)
                return month >= startMonth && month <= endMonth;
            // vòng quanh năm (vd: 11..2 = Tết)
            return month >= startMonth || month <= endMonth;
        }
    }

    public sealed class PcSeasonalEventRegistry
    {
        private readonly Dictionary<int, PcSeasonalEventEntry> _byId = new();
        public int Count => _byId.Count;
        public IEnumerable<PcSeasonalEventEntry> All => _byId.Values;
        public void Register(PcSeasonalEventEntry e)
        {
            if (e == null || e.eventId <= 0) return;
            _byId[e.eventId] = e;
        }
        public PcSeasonalEventEntry Get(int eventId)
            => _byId.TryGetValue(eventId, out var v) ? v : null;
        public IReadOnlyList<PcSeasonalEventEntry> GetActiveByMonth(int month)
        {
            var result = new List<PcSeasonalEventEntry>();
            foreach (var e in _byId.Values)
                if (e != null && e.IsActiveInMonth(month)) result.Add(e);
            return result;
        }
    }
}
