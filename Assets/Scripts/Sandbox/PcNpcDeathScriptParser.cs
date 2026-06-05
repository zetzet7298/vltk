// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcscript/death.txt NPC death script parser
// Source: server settings/npcscript/death.txt (kịch bản khi NPC chết).
//   NpcTemplateId  DropItemId  DropCount  ScriptId  ScriptFile
// Mỗi NPC có thể có 1 kịch bản khi chết: rơi đồ + chạy script.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcNpcDeathScriptParser
    {
        public const int NpcTemplateIdCol = 0;
        public const int DropItemIdCol = 1;
        public const int DropCountCol = 2;
        public const int ScriptIdCol = 3;
        public const int ScriptFileCol = 4;

        public static List<PcNpcDeathScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcNpcDeathScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                int npcId = PcItemCommon.Int(cols, NpcTemplateIdCol);
                if (npcId <= 0) continue;
                rows.Add(new PcNpcDeathScriptEntry
                {
                    npcTemplateId = npcId,
                    dropItemId = PcItemCommon.Int(cols, DropItemIdCol),
                    dropCount = PcItemCommon.Int(cols, DropCountCol),
                    scriptId = PcItemCommon.Int(cols, ScriptIdCol),
                    scriptFile = PcItemCommon.Str(cols, ScriptFileCol),
                });
            }
            return rows;
        }

        public static PcNpcDeathScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcNpcDeathScriptRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "death.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcNpcDeathScriptEntry
    {
        public int npcTemplateId;
        public int dropItemId;
        public int dropCount;
        public int scriptId;
        public string scriptFile;
    }

    public sealed class PcNpcDeathScriptRegistry
    {
        private readonly Dictionary<int, PcNpcDeathScriptEntry> _byNpc = new();
        public int Count => _byNpc.Count;
        public IEnumerable<PcNpcDeathScriptEntry> All => _byNpc.Values;

        public void Register(PcNpcDeathScriptEntry e)
        {
            if (e == null || e.npcTemplateId <= 0) return;
            _byNpc[e.npcTemplateId] = e;
        }

        public PcNpcDeathScriptEntry Get(int npcTemplateId)
            => _byNpc.TryGetValue(npcTemplateId, out var v) ? v : null;

        public IReadOnlyList<PcNpcDeathScriptEntry> GetAll()
            => (IReadOnlyList<PcNpcDeathScriptEntry>)new List<PcNpcDeathScriptEntry>(_byNpc.Values);
    }
}
