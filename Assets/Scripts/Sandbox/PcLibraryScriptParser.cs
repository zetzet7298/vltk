// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.1 Core Libraries metadata parser
// Source: libraryscripts.txt (Reference/PcGlobal/library or root). 44 libraries.
// Cols: ScriptId  LibraryName  FunctionName  ParamsCount  ReturnType  Description
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcLibraryScriptParser
    {
        public const int ScriptIdCol = 0;
        public const int LibraryNameCol = 1;
        public const int FunctionNameCol = 2;
        public const int ParamsCountCol = 3;
        public const int ReturnTypeCol = 4;
        public const int DescriptionCol = 5;

        public static List<PcLibraryScriptEntry> ParseFile(string path)
        {
            var rows = new List<PcLibraryScriptEntry>();
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
                rows.Add(new PcLibraryScriptEntry
                {
                    scriptId = id,
                    libraryName = PcItemCommon.Str(cols, LibraryNameCol),
                    functionName = PcItemCommon.Str(cols, FunctionNameCol),
                    paramsCount = PcItemCommon.Int(cols, ParamsCountCol),
                    returnType = PcItemCommon.Str(cols, ReturnTypeCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcLibraryScriptRegistry BuildRegistry(string dir)
        {
            var reg = new PcLibraryScriptRegistry();
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
    public class PcLibraryScriptEntry
    {
        public int scriptId;
        public string libraryName;
        public string functionName;
        public int paramsCount;
        public string returnType;
        public string description;
    }

    public sealed class PcLibraryScriptRegistry
    {
        private readonly Dictionary<int, PcLibraryScriptEntry> _byId = new();
        public int Count => _byId.Count;

        public void Register(PcLibraryScriptEntry e)
        {
            if (e == null || e.scriptId <= 0) return;
            _byId[e.scriptId] = e;
        }

        public PcLibraryScriptEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcLibraryScriptEntry> GetByLibrary(string libraryName)
        {
            var list = new List<PcLibraryScriptEntry>();
            foreach (var e in _byId.Values) if (string.Equals(e.libraryName, libraryName, System.StringComparison.OrdinalIgnoreCase)) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcLibraryScriptEntry> GetByReturnType(string returnType)
        {
            var list = new List<PcLibraryScriptEntry>();
            foreach (var e in _byId.Values) if (string.Equals(e.returnType, returnType, System.StringComparison.OrdinalIgnoreCase)) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcLibraryScriptEntry> All => new List<PcLibraryScriptEntry>(_byId.Values);
    }
}
