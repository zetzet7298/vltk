// -----------------------------------------------------------------------------
// VLTK Mobile — PC Task Flag parser (29 cấu hình nhiệm vụ)
// Source: settings/task/taskflag.txt (Reference/PcTask).
// File format (GB2312, tab-separated):
//   FlagId  FlagName  FlagDesc  TaskType  CategoryId  ReqLevel
//   TaskType: 0=main, 1=side, 2=daily, 3=faction, 4=event
// Trả về registry runtime tra cứu theo flagId / type / category.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTaskFlagParser
    {
        public const int FlagNameCol = 1;
        public const int FlagDescCol = 2;
        public const int TaskTypeCol = 3;
        public const int CategoryIdCol = 4;
        public const int ReqLevelCol = 5;

        public static List<PcTaskFlagEntry> ParseFile(string path)
        {
            var rows = new List<PcTaskFlagEntry>();
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
                rows.Add(new PcTaskFlagEntry
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

        public static PcTaskFlagRegistry BuildRegistry(string dir)
        {
            var reg = new PcTaskFlagRegistry();
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
    public class PcTaskFlagEntry
    {
        public int flagId;
        public string flagName;
        public string flagDesc;
        public int taskType;   // 0=main, 1=side, 2=daily, 3=faction, 4=event
        public int categoryId;
        public int reqLevel;
    }

    public sealed class PcTaskFlagRegistry
    {
        private readonly Dictionary<int, PcTaskFlagEntry> _byId = new();

        public int Count => _byId.Count;

        public void Register(PcTaskFlagEntry e)
        {
            if (e == null || e.flagId <= 0) return;
            _byId[e.flagId] = e;
        }

        public PcTaskFlagEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcTaskFlagEntry> GetByType(int taskType)
        {
            var list = new List<PcTaskFlagEntry>();
            foreach (var e in _byId.Values)
                if (e.taskType == taskType) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTaskFlagEntry> GetByCategory(int categoryId)
        {
            var list = new List<PcTaskFlagEntry>();
            foreach (var e in _byId.Values)
                if (e.categoryId == categoryId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTaskFlagEntry> All
            => new List<PcTaskFlagEntry>(_byId.Values);
    }
}
