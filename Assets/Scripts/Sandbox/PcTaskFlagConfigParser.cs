// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/taskflagconfig.txt Task Flag Config parser
// Source: settings/taskflagconfig.txt (29 entries, GB2312, tab-separated).
//   Cols: FlagId  FlagName  FlagDesc  TaskType  CategoryId  ReqLevel
// TaskType: 0 = chính tuyến, 1 = phụ tuyến, 2 = hằng ngày, 3 = tuần hoàn,
//           4 = môn phái, 5 = bang hội, 6 = sự kiện, 7 = tu luyện
// Tolerant of missing file (trả về registry rỗng).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTaskFlagConfigParser
    {
        public const int FlagIdCol = 0;
        public const int FlagNameCol = 1;
        public const int FlagDescCol = 2;
        public const int TaskTypeCol = 3;
        public const int CategoryIdCol = 4;
        public const int ReqLevelCol = 5;

        public const int TypeChinhTuyen = 0;
        public const int TypePhuTuyen = 1;
        public const int TypeHangNgay = 2;
        public const int TypeTuanHoan = 3;
        public const int TypeMonPhai = 4;
        public const int TypeBangHoi = 5;
        public const int TypeSuKien = 6;
        public const int TypeTuLuyen = 7;

        public static List<TaskFlagConfigEntry> ParseFile(string path)
        {
            var rows = new List<TaskFlagConfigEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            string[] lines;
            try { lines = PcItemCommon.ReadServerLines(path).ToArray(); }
            catch { try { lines = File.ReadAllLines(path); } catch { return rows; } }
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, FlagIdCol);
                if (id <= 0) continue;
                rows.Add(new TaskFlagConfigEntry
                {
                    flagId = id,
                    flagName = PcItemCommon.Str(cols, FlagNameCol),
                    flagDesc = PcItemCommon.Str(cols, FlagDescCol),
                    taskType = PcItemCommon.Int(cols, TaskTypeCol),
                    categoryId = PcItemCommon.Int(cols, CategoryIdCol),
                    reqLevel = PcItemCommon.Int(cols, ReqLevelCol),
                });
            }
            return rows;
        }

        public static TaskFlagConfigRegistry BuildRegistry(string dir)
        {
            var reg = new TaskFlagConfigRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string[] candidates = { "taskflagconfig.txt", "task_flag.txt", "task.txt", "flagconfig.txt" };
            foreach (var fn in candidates)
            {
                string main = Path.Combine(dir, fn);
                if (File.Exists(main))
                {
                    foreach (var s in ParseFile(main)) reg.Register(s);
                    return reg;
                }
            }
            foreach (var f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class TaskFlagConfigEntry
    {
        public int flagId;
        public string flagName;
        public string flagDesc;
        public int taskType;       // 0..7 — chính/phụ/hằng/tuần/phái/bang/sk/tu
        public int categoryId;
        public int reqLevel;
    }

    public sealed class TaskFlagConfigRegistry
    {
        private readonly Dictionary<int, TaskFlagConfigEntry> _byId = new();
        private readonly Dictionary<int, List<TaskFlagConfigEntry>> _byType = new();
        private readonly Dictionary<int, List<TaskFlagConfigEntry>> _byCategory = new();
        public int Count => _byId.Count;
        public IEnumerable<TaskFlagConfigEntry> All => _byId.Values;
        public void Register(TaskFlagConfigEntry e)
        {
            if (e == null || e.flagId <= 0) return;
            _byId[e.flagId] = e;
            if (!_byType.TryGetValue(e.taskType, out var tList))
            {
                tList = new List<TaskFlagConfigEntry>();
                _byType[e.taskType] = tList;
            }
            tList.Add(e);
            if (!_byCategory.TryGetValue(e.categoryId, out var cList))
            {
                cList = new List<TaskFlagConfigEntry>();
                _byCategory[e.categoryId] = cList;
            }
            cList.Add(e);
        }
        public TaskFlagConfigEntry Get(int flagId)
            => _byId.TryGetValue(flagId, out var v) ? v : null;
        public IReadOnlyList<TaskFlagConfigEntry> GetByType(int taskType)
            => _byType.TryGetValue(taskType, out var v)
                ? (IReadOnlyList<TaskFlagConfigEntry>)v
                : (IReadOnlyList<TaskFlagConfigEntry>)System.Array.Empty<TaskFlagConfigEntry>();
        public IReadOnlyList<TaskFlagConfigEntry> GetByCategory(int categoryId)
            => _byCategory.TryGetValue(categoryId, out var v)
                ? (IReadOnlyList<TaskFlagConfigEntry>)v
                : (IReadOnlyList<TaskFlagConfigEntry>)System.Array.Empty<TaskFlagConfigEntry>();
    }
}
