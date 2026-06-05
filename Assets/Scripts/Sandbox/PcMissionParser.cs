// -----------------------------------------------------------------------------
// VLTK Mobile — PC task/player_task_def.txt mission (nhiệm vụ) parser
// Source: server settings/task/player_task_def.txt (GB2312, 6 tab columns).
//   TASK_ID_FIRST  TASK_ID_LAST  TASK_NAME  SYNC_FLAG  CLIENT_FLAG  TASK_DESCRIBE
// Each row is an ID range + a Vietnamese name + description. We index by taskId
// (TASK_ID_FIRST) and expose the describe text for the quest log.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public static class PcMissionParser
    {
        public const int TaskIdFirstCol = 0;
        public const int TaskIdLastCol = 1;
        public const int TaskNameCol = 2;
        public const int SyncFlagCol = 3;
        public const int ClientFlagCol = 4;
        public const int TaskDescribeCol = 5;

        public static List<PcMissionEntry> ParseFile(string path)
        {
            var rows = new List<PcMissionEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int idCursor = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int first = PcItemCommon.Int(cols, TaskIdFirstCol);
                int last = PcItemCommon.Int(cols, TaskIdLastCol);
                if (last <= 0) last = first;
                if (first <= 0) first = ++idCursor;
                else idCursor = last;
                var entry = new PcMissionEntry
                {
                    taskIdFirst = first,
                    taskIdLast = last,
                    nameRaw = PcItemCommon.Str(cols, TaskNameCol),
                    syncFlag = PcItemCommon.Int(cols, SyncFlagCol),
                    clientFlag = PcItemCommon.Int(cols, ClientFlagCol),
                    describe = PcItemCommon.Str(cols, TaskDescribeCol),
                };
                rows.Add(entry);
            }
            return rows;
        }

        public static PcMissionRegistry BuildRegistry(string dir)
        {
            var reg = new PcMissionRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
            {
                foreach (var s in ParseFile(f)) reg.Register(s);
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcMissionEntry
    {
        public int taskIdFirst;
        public int taskIdLast;
        public string nameRaw;
        public int syncFlag;
        public int clientFlag;
        public string describe;
    }

    public sealed class PcMissionRegistry
    {
        private readonly Dictionary<int, PcMissionEntry> _byFirst = new();
        private readonly Dictionary<int, PcMissionEntry> _byId = new();
        public int Count => _byFirst.Count;

        public void Register(PcMissionEntry e)
        {
            if (e == null || e.taskIdFirst <= 0) return;
            _byFirst[e.taskIdFirst] = e;
            for (int id = e.taskIdFirst; id <= e.taskIdLast; id++)
                _byId[id] = e;
        }

        public PcMissionEntry GetByFirstId(int id) => _byFirst.TryGetValue(id, out var v) ? v : null;
        public PcMissionEntry ResolveId(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMissionEntry> All => _byFirst.Values;
    }
}
