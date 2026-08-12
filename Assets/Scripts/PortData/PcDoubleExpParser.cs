// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/doubleexp.txt Double EXP schedule parser
// Source: doubleexp.txt — lịch nhân đôi kinh nghiệm theo giờ/ngày trong tuần.
//   ScheduleId  StartHour  EndHour  DayOfWeek  Multiplier
// DayOfWeek: 0=CN, 1=T2..6=T7, 7=All (mỗi ngày)
// Multiplier: 2.0 = 2x EXP; PC quy ước 10000 = 1.0x
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcDoubleExpParser
    {
        public const int ScheduleIdCol = 0;
        public const int StartHourCol = 1;
        public const int EndHourCol = 2;
        public const int DayOfWeekCol = 3;
        public const int MultiplierCol = 4;

        public static List<PcDoubleExpEntry> ParseFile(string path)
        {
            var rows = new List<PcDoubleExpEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcDoubleExpEntry
                {
                    scheduleId = PcItemCommon.Int(cols, ScheduleIdCol),
                    startHour = PcItemCommon.Int(cols, StartHourCol),
                    endHour = PcItemCommon.Int(cols, EndHourCol),
                    dayOfWeek = PcItemCommon.Int(cols, DayOfWeekCol),
                    multiplierRaw = PcItemCommon.Int(cols, MultiplierCol),
                });
            }
            return rows;
        }

        public static PcDoubleExpRegistry BuildRegistry(string dir)
        {
            var reg = new PcDoubleExpRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "doubleexp.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcDoubleExpEntry
    {
        public int scheduleId;
        public int startHour;       // 0-23
        public int endHour;         // 0-23 (exclusive; cho phép qua ngày nếu > startHour)
        public int dayOfWeek;       // 0=CN, 1-6, 7=All
        public int multiplierRaw;   // PC quy ước: 20000 = 2.0x

        public float Multiplier => multiplierRaw / 10000f;
    }

    public sealed class PcDoubleExpRegistry
    {
        private readonly Dictionary<int, PcDoubleExpEntry> _byId = new();
        private readonly List<PcDoubleExpEntry> _all = new();
        public int Count => _byId.Count;
        public IEnumerable<PcDoubleExpEntry> All => _all;
        public void Register(PcDoubleExpEntry e)
        {
            if (e == null || e.scheduleId <= 0) return;
            _byId[e.scheduleId] = e;
            _all.Add(e);
        }
        public PcDoubleExpEntry Get(int scheduleId)
            => _byId.TryGetValue(scheduleId, out var v) ? v : null;

        /// <summary>Trả về schedule đang active tại hour/dayOfWeek (hoặc null).</summary>
        public PcDoubleExpEntry GetActiveByHour(int hour, int dayOfWeek)
        {
            if (hour < 0 || hour > 23) return null;
            foreach (var e in _all)
            {
                if (e == null) continue;
                bool dayMatch = e.dayOfWeek == 7 || e.dayOfWeek == dayOfWeek;
                if (!dayMatch) continue;
                bool hourMatch = e.startHour <= e.endHour
                    ? hour >= e.startHour && hour < e.endHour
                    : (hour >= e.startHour || hour < e.endHour); // wrap qua ngày
                if (hourMatch) return e;
            }
            return null;
        }
    }
}
