// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/activitysys/activity.txt activity (hoạt động) parser
// Source: activity.txt (GB2312, tab-separated, 21 activities).
//   Id \t Name \t StartDate \t EndDate \t Description \t TaskGroup \t TaskVersion \t TaskIdSet
// Mobile synthesizes Type (0=daily, 1=weekly, 2=monthly) from date range
//   + OpenHour/CloseHour from StartDate/EndDate if they are HHMM.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcActivityParser
    {
        public const string MainFile = "activity.txt";

        public static List<PcActivityEntry> ParseFile(string path)
        {
            var rows = new List<PcActivityEntry>();
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
                string startDate = cols.Length > 2 ? PcItemCommon.Str(cols, 2) : string.Empty;
                string endDate = cols.Length > 3 ? PcItemCommon.Str(cols, 3) : string.Empty;
                string taskGroup = cols.Length > 5 ? PcItemCommon.Str(cols, 5) : string.Empty;
                rows.Add(new PcActivityEntry
                {
                    activityId = id,
                    nameRaw = PcItemCommon.Str(cols, 1),
                    type = InferType(startDate, endDate),
                    openHour = ExtractHour(startDate),
                    closeHour = ExtractHour(endDate),
                    mapId = 0,
                    minLevel = 0,
                    maxLevel = 0,
                    rewardTable = taskGroup,
                    description = cols.Length > 4 ? PcItemCommon.Str(cols, 4) : string.Empty,
                });
            }
            return rows;
        }

        // 0=daily, 1=weekly, 2=monthly based on start/end date span.
        private static int InferType(string startDate, string endDate)
        {
            // PC startDate/endDate dạng "0" hoặc "200909250000" (yyyyMMddHHmm)
            if (string.IsNullOrEmpty(startDate) || startDate == "0") return 0;
            if (string.IsNullOrEmpty(endDate) || endDate == "0") return 0;
            if (startDate.Length >= 8 && endDate.Length >= 8)
            {
                int.TryParse(startDate.Substring(4, 2), out int sm);
                int.TryParse(endDate.Substring(4, 2), out int em);
                if (em - sm >= 1) return 2; // span months
                if (endDate.Length >= 8 && startDate.Length >= 8
                    && endDate.Substring(0, 8) != startDate.Substring(0, 8))
                    return 1; // different day → weekly-ish
            }
            return 0;
        }

        private static int ExtractHour(string s)
        {
            if (string.IsNullOrEmpty(s) || s.Length < 10) return 0;
            if (s == "0") return 0;
            int.TryParse(s.Substring(8, 2), out int h);
            return h;
        }

        public static PcActivityRegistry BuildRegistry(string dir)
        {
            var reg = new PcActivityRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, MainFile);
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcActivityEntry
    {
        public int activityId;
        public string nameRaw;
        public int type;              // 0=daily, 1=weekly, 2=monthly
        public int openHour;          // 0..23
        public int closeHour;         // 0..23
        public int mapId;
        public int minLevel;
        public int maxLevel;
        public string rewardTable;
        public string description;
    }

    public sealed class PcActivityRegistry
    {
        private readonly Dictionary<int, PcActivityEntry> _byId = new();
        private readonly Dictionary<int, List<PcActivityEntry>> _byType = new();
        public int Count => _byId.Count;
        public IEnumerable<PcActivityEntry> All => _byId.Values;

        public void Register(PcActivityEntry e)
        {
            if (e == null || e.activityId <= 0) return;
            _byId[e.activityId] = e;
            if (!_byType.TryGetValue(e.type, out var list))
            {
                list = new List<PcActivityEntry>();
                _byType[e.type] = list;
            }
            list.Add(e);
        }

        public PcActivityEntry Get(int activityId)
            => _byId.TryGetValue(activityId, out var v) ? v : null;

        public IReadOnlyList<PcActivityEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcActivityEntry>)v
                : System.Array.Empty<PcActivityEntry>();

        public IReadOnlyList<PcActivityEntry> GetActiveByHour(int hour)
        {
            var list = new List<PcActivityEntry>();
            foreach (var e in _byId.Values)
            {
                if (e.openHour == e.closeHour) { list.Add(e); continue; }
                if (e.openHour < e.closeHour)
                {
                    if (hour >= e.openHour && hour < e.closeHour) list.Add(e);
                }
                else
                {
                    // wraps midnight: e.g. 22..6
                    if (hour >= e.openHour || hour < e.closeHour) list.Add(e);
                }
            }
            return list;
        }
    }
}
