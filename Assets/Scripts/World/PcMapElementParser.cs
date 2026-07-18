// -----------------------------------------------------------------------------
// VLTK Mobile — PC mapelem.txt parser
// Source: settings/mapelem.txt (Ngũ hành map: Kim/Mộc/Thủy/Hỏa/Thổ).
// Cols: MapId  ElementType (0=metal,1=wood,2=water,3=fire,4=earth)  Power  RegenRate
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapElementParser
    {
        public const int MapIdCol = 0;
        public const int ElementTypeCol = 1;
        public const int PowerCol = 2;
        public const int RegenRateCol = 3;

        public const int ElementMetal = 0;
        public const int ElementWood = 1;
        public const int ElementWater = 2;
        public const int ElementFire = 3;
        public const int ElementEarth = 4;

        public static List<PcMapElementEntry> ParseFile(string path)
        {
            var rows = new List<PcMapElementEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapElementEntry
                {
                    mapId = id,
                    elementType = PcItemCommon.Int(cols, ElementTypeCol),
                    power = PcItemCommon.Int(cols, PowerCol),
                    regenRate = PcItemCommon.Int(cols, RegenRateCol),
                });
            }
            return rows;
        }

        public static PcMapElementRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapElementRegistry();
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
    public class PcMapElementEntry
    {
        public int mapId;
        public int elementType;
        public int power;
        public int regenRate;
    }

    public sealed class PcMapElementRegistry
    {
        private readonly Dictionary<int, PcMapElementEntry> _byMapId = new();
        public int Count => _byMapId.Count;
        public void Register(PcMapElementEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            _byMapId[e.mapId] = e;
        }
        public PcMapElementEntry Get(int mapId) => _byMapId.TryGetValue(mapId, out var v) ? v : null;
        public IReadOnlyList<PcMapElementEntry> GetByElement(int elementType)
        {
            var list = new List<PcMapElementEntry>();
            foreach (var e in _byMapId.Values) if (e.elementType == elementType) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapElementEntry> All => new List<PcMapElementEntry>(_byMapId.Values);
    }
}
