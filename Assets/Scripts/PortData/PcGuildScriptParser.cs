// -----------------------------------------------------------------------------
// VLTK Mobile — PC guild script parser (Guild Scripts - 65 configs)
// Source: guildscript.txt / script/tong/*.lua configs (Reference/PcTong).
// Columns: ScriptId  Name  Type  TriggerOn  Action  RequiredLevel
// Type: 0=create, 1=join, 2=leave, 3=donate, 4=build, 5=war, 6=disband
// Vietnamese: "Kịch Bản Bang", "Tạo Bang", "Gia Nhập", "Rời Bang", "Đóng Góp".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcGuildScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int NameCol = 1;
        public const int TypeCol = 2;
        public const int TriggerOnCol = 3;
        public const int ActionCol = 4;
        public const int RequiredLevelCol = 5;

        public static List<PcGuildScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcGuildScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 1) continue;
                int id = PcItemCommon.Int(cols, ScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcGuildScriptEntry
                {
                    scriptId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    type = PcItemCommon.Int(cols, TypeCol),
                    triggerOn = PcItemCommon.Str(cols, TriggerOnCol),
                    action = PcItemCommon.Str(cols, ActionCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                });
            }
            return rows;
        }

        public static PcGuildScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcGuildScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".lua", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcGuildScriptEntry
    {
        public int scriptId;
        public string name;
        public int type; // 0=create, 1=join, 2=leave, 3=donate, 4=build, 5=war, 6=disband
        public string triggerOn;
        public string action;
        public int requiredLevel;
    }

    public sealed class PcGuildScriptRegistry
    {
        private readonly Dictionary<int, PcGuildScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcGuildScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcGuildScriptEntry Get(int scriptId)
            => _byId.TryGetValue(scriptId, out var v) ? v : null;

        public IReadOnlyList<PcGuildScriptEntry> GetByType(int type)
        {
            var list = new List<PcGuildScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.type == type) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcGuildScriptEntry> All => new List<PcGuildScriptEntry>(_byId.Values);
    }
}
