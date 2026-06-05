// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcscript/npc_level.txt NPC level script parser
// Source: server settings/npcscript/npc_level.txt (58 entries, GB2312, 7 cols).
//   NpcTemplateId  MinLevel  MaxLevel  ScriptId  ScriptFile  DialogFile  TriggerType
// Kịch bản NPC theo cấp — trigger khi nhân vật đạt cấp, click chuột, dùng đồ.
// TriggerType: 0=talk, 1=kill, 2=use.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcNpcLevelScriptParser
    {
        public const int NpcTemplateIdCol = 0;
        public const int MinLevelCol = 1;
        public const int MaxLevelCol = 2;
        public const int ScriptIdCol = 3;
        public const int ScriptFileCol = 4;
        public const int DialogFileCol = 5;
        public const int TriggerTypeCol = 6;

        public static List<PcNpcLevelScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcNpcLevelScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                int npcId = PcItemCommon.Int(cols, NpcTemplateIdCol);
                if (npcId <= 0) continue;
                rows.Add(new PcNpcLevelScriptEntry
                {
                    npcTemplateId = npcId,
                    minLevel = PcItemCommon.Int(cols, MinLevelCol),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol),
                    scriptId = PcItemCommon.Int(cols, ScriptIdCol),
                    scriptFile = PcItemCommon.Str(cols, ScriptFileCol),
                    dialogFile = PcItemCommon.Str(cols, DialogFileCol),
                    triggerType = PcItemCommon.Int(cols, TriggerTypeCol),
                });
            }
            return rows;
        }

        public static PcNpcLevelScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcNpcLevelScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcNpcLevelScriptEntry
    {
        public int npcTemplateId;
        public int minLevel;
        public int maxLevel;
        public int scriptId;
        public string scriptFile;
        public string dialogFile;
        public int triggerType; // 0=talk, 1=kill, 2=use
    }

    public sealed class PcNpcLevelScriptRegistry
    {
        private readonly List<PcNpcLevelScriptEntry> _all = new();
        private readonly Dictionary<int, List<PcNpcLevelScriptEntry>> _byNpc = new();
        private readonly Dictionary<string, List<PcNpcLevelScriptEntry>> _byFile = new();
        public int Count => _all.Count;
        public IEnumerable<PcNpcLevelScriptEntry> All => _all;

        public void Register(PcNpcLevelScriptEntry e)
        {
            if (e == null) return;
            _all.Add(e);
            if (!_byNpc.TryGetValue(e.npcTemplateId, out var nlist))
            {
                nlist = new List<PcNpcLevelScriptEntry>();
                _byNpc[e.npcTemplateId] = nlist;
            }
            nlist.Add(e);
            string fileKey = e.scriptFile ?? string.Empty;
            if (!string.IsNullOrEmpty(fileKey))
            {
                if (!_byFile.TryGetValue(fileKey, out var flist))
                {
                    flist = new List<PcNpcLevelScriptEntry>();
                    _byFile[fileKey] = flist;
                }
                flist.Add(e);
            }
        }

        /// <summary>Tìm script theo NPC và cấp nhân vật (chính xác theo minLevel/maxLevel).</summary>
        public PcNpcLevelScriptEntry Get(int npcTemplateId, int level)
        {
            if (_byNpc.TryGetValue(npcTemplateId, out var list))
            {
                foreach (var e in list)
                {
                    if (level >= e.minLevel && level <= e.maxLevel) return e;
                }
            }
            return null;
        }

        public IReadOnlyList<PcNpcLevelScriptEntry> GetByNpc(int npcTemplateId)
            => _byNpc.TryGetValue(npcTemplateId, out var v)
                ? (IReadOnlyList<PcNpcLevelScriptEntry>)v
                : System.Array.Empty<PcNpcLevelScriptEntry>();

        public IReadOnlyList<PcNpcLevelScriptEntry> GetByScriptFile(string fileName)
            => _byFile.TryGetValue(fileName ?? string.Empty, out var v)
                ? (IReadOnlyList<PcNpcLevelScriptEntry>)v
                : System.Array.Empty<PcNpcLevelScriptEntry>();
    }
}
