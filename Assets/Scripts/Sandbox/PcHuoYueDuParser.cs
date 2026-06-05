// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/huoyuedu/huoyuedu.txt daily activity points parser
// Source: huoyuedu.txt (GB2312, tab-separated, 41 entries).
//   ActivityId \t ActivityName \t CountTask \t MaxCount \t Param1..10 \t WeekResetFlag
// Mobile maps: 0=BOSS, 1=Thủy Phong Lăng Độ, 2=Thời Gian Khiêu Chiến,
//              3=Tống Kim, 4=Bảo Tường Viêm Đế, 5=Công Thành Chiến, ...
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcHuoYueDuParser
    {
        public const string MainFile = "huoyuedu.txt";

        public static List<PcHuoYueDuEntry> ParseFile(string path)
        {
            var rows = new List<PcHuoYueDuEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                rows.Add(new PcHuoYueDuEntry
                {
                    activityId = PcItemCommon.Int(cols, 0),
                    nameRaw = PcItemCommon.Str(cols, 1),
                    type = InferType(cols),
                    dailyLimit = cols.Length > 3 ? PcItemCommon.Int(cols, 3) : 0,
                    scoreReward = cols.Length > 4 ? PcItemCommon.Int(cols, 4) : 0,
                    expReward = cols.Length > 5 ? PcItemCommon.Int(cols, 5) : 0,
                    weekReset = cols.Length > 14 ? PcItemCommon.Int(cols, 14) : 0,
                });
            }
            return rows;
        }

        // Heuristic: type comes from PC activity id. Mapping table per port_docs.
        private static int InferType(string[] cols)
        {
            int id = PcItemCommon.Int(cols, 0);
            // 1=BOSS, 2=Thủy Phong Lăng Độ, 3=Thời Gian Khiêu Chiến, 4=Tống Kim
            // 5=Bảo Tường Viêm Đế, 6=Công Thành Chiến, 7=Võ Lâm Liên Đấu
            if (id >= 1 && id <= 41) return id - 1;
            return id;
        }

        public static PcHuoYueDuRegistry BuildRegistry(string dir)
        {
            var reg = new PcHuoYueDuRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, MainFile);
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcHuoYueDuEntry
    {
        public int activityId;
        public string nameRaw;
        public int type;               // 0..40 mapping to PC activity category
        public int dailyLimit;         // Số lần/ngày
        public int scoreReward;        // Điểm hoạt động thưởng
        public int expReward;          // Kinh nghiệm thưởng
        public int weekReset;          // 1=reset theo tuần
    }

    public sealed class PcHuoYueDuRegistry
    {
        private readonly Dictionary<int, PcHuoYueDuEntry> _byId = new();
        private readonly Dictionary<int, List<PcHuoYueDuEntry>> _byType = new();
        public int Count => _byId.Count;
        public IEnumerable<PcHuoYueDuEntry> All => _byId.Values;

        public void Register(PcHuoYueDuEntry e)
        {
            if (e == null || e.activityId <= 0) return;
            _byId[e.activityId] = e;
            if (!_byType.TryGetValue(e.type, out var list))
            {
                list = new List<PcHuoYueDuEntry>();
                _byType[e.type] = list;
            }
            list.Add(e);
        }

        public PcHuoYueDuEntry Get(int activityId)
            => _byId.TryGetValue(activityId, out var v) ? v : null;

        public IReadOnlyList<PcHuoYueDuEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcHuoYueDuEntry>)v
                : System.Array.Empty<PcHuoYueDuEntry>();
    }
}
