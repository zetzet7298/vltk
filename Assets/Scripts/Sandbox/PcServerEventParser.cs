// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/events.txt server event (Sự Kiện Máy Chủ) parser
// Source: settings/events.txt (455 entries, GB2312, tab-separated).
//   Cols: EventId  EventName  ScriptFile  MapId  StartDate  EndDate  Type
//   Type: 0 = open (luôn mở), 1 = limited (theo ngày).
//   StartDate / EndDate là số nguyên yyyymmdd (ví dụ: 20251231).
// Lua scripts trong script/event/* là nguồn đầy đủ nhưng quá phức tạp để parse
// runtime, nên ta đọc file index events.txt để liệt kê các sự kiện.
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcServerEventParser
    {
        public const int EventIdCol = 0;
        public const int NameCol = 1;
        public const int ScriptFileCol = 2;
        public const int MapIdCol = 3;
        public const int StartDateCol = 4;
        public const int EndDateCol = 5;
        public const int TypeCol = 6;

        public static List<PcServerEventEntry> ParseFile(string path)
        {
            var rows = new List<PcServerEventEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path); }
            catch { lines = File.ReadAllLines(path); }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcServerEventEntry
                {
                    eventId = PcItemCommon.Int(cols, EventIdCol),
                    nameVi = PcItemCommon.Str(cols, NameCol),
                    scriptFile = cols.Length > ScriptFileCol ? PcItemCommon.Str(cols, ScriptFileCol) : string.Empty,
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    startDate = cols.Length > StartDateCol ? PcItemCommon.Int(cols, StartDateCol) : 0,
                    endDate = cols.Length > EndDateCol ? PcItemCommon.Int(cols, EndDateCol) : 0,
                    type = cols.Length > TypeCol ? PcItemCommon.Int(cols, TypeCol) : 0,
                });
            }
            return rows;
        }

        public static PcServerEventRegistry BuildRegistry(string dir)
        {
            var reg = new PcServerEventRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            // Thử nhiều tên file index phổ biến
            string[] candidates = { "events.txt", "event.txt", "server_event.txt", "eventserver.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            // Fallback: quét tất cả *.txt trong dir
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcServerEventEntry
    {
        public int eventId;
        public string nameVi;
        public string scriptFile;
        public int mapId;
        public int startDate;     // yyyymmdd
        public int endDate;       // yyyymmdd
        public int type;          // 0 = open, 1 = limited
    }

    public sealed class PcServerEventRegistry
    {
        private readonly Dictionary<int, PcServerEventEntry> _byId = new();
        private readonly Dictionary<int, List<PcServerEventEntry>> _byMap = new();
        public int Count => _byId.Count;
        public void Register(PcServerEventEntry e)
        {
            if (e == null || e.eventId <= 0) return;
            _byId[e.eventId] = e;
            if (!_byMap.TryGetValue(e.mapId, out var list))
            {
                list = new List<PcServerEventEntry>();
                _byMap[e.mapId] = list;
            }
            list.Add(e);
        }
        public PcServerEventEntry Get(int eventId)
            => _byId.TryGetValue(eventId, out var v) ? v : null;

        /// <summary>Lọc sự kiện đang mở tại currentDate (yyyymmdd).</summary>
        public IEnumerable<PcServerEventEntry> GetActive(int currentDate)
        {
            foreach (var e in _byId.Values)
            {
                if (e.type == 0) { yield return e; continue; }
                if (e.startDate <= 0 || e.endDate <= 0) { yield return e; continue; }
                if (currentDate >= e.startDate && currentDate <= e.endDate) yield return e;
            }
        }

        public IEnumerable<PcServerEventEntry> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v)
                ? (IEnumerable<PcServerEventEntry>)v
                : System.Array.Empty<PcServerEventEntry>();

        public IEnumerable<PcServerEventEntry> All => _byId.Values;
    }
}
