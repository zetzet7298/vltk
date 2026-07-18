// -----------------------------------------------------------------------------
// VLTK Mobile — PC Town Script parser
// Source: townscript.txt — script thị trấn: NPC, nhiệm vụ, shop, service, event.
// Cols: TownScriptId, TownId, TownName, ScriptFile, ScriptType, FunctionName, Description.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTownScriptParser
    {
        public const int TownScriptIdCol = 0;
        public const int TownIdCol = 1;
        public const int TownNameCol = 2;
        public const int ScriptFileCol = 3;
        public const int ScriptTypeCol = 4;
        public const int FunctionNameCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcTownScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcTownScriptEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TownScriptIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTownScriptEntry
                {
                    townScriptId = id,
                    townId = PcItemCommon.Int(cols, TownIdCol),
                    townNameRaw = PcItemCommon.Str(cols, TownNameCol),
                    scriptFile = PcItemCommon.Str(cols, ScriptFileCol),
                    scriptType = PcItemCommon.Int(cols, ScriptTypeCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    descriptionRaw = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcTownScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcTownScriptRegistry();
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
    public class PcTownScriptEntry
    {
        public int townScriptId;
        public int townId;
        public string townNameRaw;
        public string scriptFile;
        public int scriptType;
        public string functionName;
        public string descriptionRaw;
    }

    public sealed class PcTownScriptRegistry
    {
        private readonly Dictionary<int, PcTownScriptEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcTownScriptEntry e) { if (e == null || e.townScriptId <= 0) return; _byId[e.townScriptId] = e; }
        public PcTownScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcTownScriptEntry> All => new List<PcTownScriptEntry>(_byId.Values);

        public IReadOnlyList<PcTownScriptEntry> GetByTown(int townId)
        {
            var list = new List<PcTownScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.townId == townId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcTownScriptEntry> GetByType(int scriptType)
        {
            var list = new List<PcTownScriptEntry>();
            foreach (var e in _byId.Values)
                if (e.scriptType == scriptType) list.Add(e);
            return list;
        }
    }
}
