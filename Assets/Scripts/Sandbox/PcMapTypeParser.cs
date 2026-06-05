// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings map_type.txt parser
// Source: map_type.txt (loại bản đồ).
// Columns: TypeId  TypeName  IsInstance  IsPvp  IsBattlefield
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapTypeParser
    {
        public const int TypeIdCol = 0;
        public const int TypeNameCol = 1;
        public const int IsInstanceCol = 2;
        public const int IsPvpCol = 3;
        public const int IsBattlefieldCol = 4;

        public static List<PcMapTypeEntry> ParseFile(string path)
        {
            var rows = new List<PcMapTypeEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, TypeIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapTypeEntry
                {
                    typeId = id,
                    typeName = PcItemCommon.Str(cols, TypeNameCol),
                    isInstance = PcItemCommon.Int(cols, IsInstanceCol) != 0,
                    isPvp = PcItemCommon.Int(cols, IsPvpCol) != 0,
                    isBattlefield = PcItemCommon.Int(cols, IsBattlefieldCol) != 0,
                });
            }
            return rows;
        }

        public static PcMapTypeRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapTypeRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapTypeEntry
    {
        public int typeId;
        public string typeName;
        public bool isInstance;
        public bool isPvp;
        public bool isBattlefield;
    }

    public sealed class PcMapTypeRegistry
    {
        private readonly Dictionary<int, PcMapTypeEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMapTypeEntry e) { if (e == null || e.typeId <= 0) return; _byId[e.typeId] = e; }
        public PcMapTypeEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMapTypeEntry> GetAll() => new List<PcMapTypeEntry>(_byId.Values);
        public IReadOnlyList<PcMapTypeEntry> All => new List<PcMapTypeEntry>(_byId.Values);
    }
}
