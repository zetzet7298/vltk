// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.4 Global Scripts metadata parser
// Source: globalscripts.txt (Reference/PcGlobal or root). 579 scripts.
// Cols: ScriptId  FileName  FunctionName  Trigger  ParamsCount  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGlobalScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int FileNameCol = 1;
        public const int FunctionNameCol = 2;
        public const int TriggerCol = 3;
        public const int ParamsCountCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcGlobalScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcGlobalScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, ScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcGlobalScriptEntry
                {
                    scriptId = id,
                    fileName = PcItemCommon.Str(cols, FileNameCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    trigger = PcItemCommon.Int(cols, TriggerCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcGlobalScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcGlobalScriptRegistry();
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
    public class PcGlobalScriptEntry
    {
        public int scriptId;
        public string fileName;
        public string functionName;
        public int trigger;        // 0=login, 1=logout, 2=heartbeat, 3=gm_command, 4=server_start, 5=server_stop
        public int paramsCount;
        public string description;
    }

    public sealed class PcGlobalScriptRegistry
    {
        private readonly Dictionary<int, PcGlobalScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcGlobalScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcGlobalScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcGlobalScriptEntry> GetByTrigger(int trigger)
        {
            var list = new List<PcGlobalScriptEntry>();
            foreach (var e in _byId.Values) if (e.trigger == trigger) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGlobalScriptEntry> GetByFile(string fileName)
        {
            var list = new List<PcGlobalScriptEntry>();
            foreach (var e in _byId.Values) if (string.Equals(e.fileName, fileName, System.StringComparison.OrdinalIgnoreCase)) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGlobalScriptEntry> All => new List<PcGlobalScriptEntry>(_byId.Values);
    }
}
