// -----------------------------------------------------------------------------
// VLTK Mobile — PC mapblock.txt parser
// Source: settings/mapblock.txt (vật cản trên map: cây, đá, nước, nhà, hàng rào).
// Cols: MapId  BlockX  BlockY  Width  Height  BlockType (0=tree,1=rock,2=water,3=building,4=fence)  Passable
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapBlockParser
    {
        public const int MapIdCol = 0;
        public const int BlockXCol = 1;
        public const int BlockYCol = 2;
        public const int WidthCol = 3;
        public const int HeightCol = 4;
        public const int BlockTypeCol = 5;
        public const int PassableCol = 6;

        public const int BlockTree = 0;
        public const int BlockRock = 1;
        public const int BlockWater = 2;
        public const int BlockBuilding = 3;
        public const int BlockFence = 4;

        public static List<PcMapBlockEntry> ParseFile(string path)
        {
            var rows = new List<PcMapBlockEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapBlockEntry
                {
                    mapId = id,
                    blockX = PcItemCommon.Int(cols, BlockXCol),
                    blockY = PcItemCommon.Int(cols, BlockYCol),
                    width = PcItemCommon.Int(cols, WidthCol),
                    height = PcItemCommon.Int(cols, HeightCol),
                    blockType = PcItemCommon.Int(cols, BlockTypeCol),
                    passable = PcItemCommon.Int(cols, PassableCol) != 0,
                });
            }
            return rows;
        }

        public static PcMapBlockRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapBlockRegistry();
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
    public class PcMapBlockEntry
    {
        public int mapId;
        public int blockX;
        public int blockY;
        public int width;
        public int height;
        public int blockType;
        public bool passable;
    }

    public sealed class PcMapBlockRegistry
    {
        private readonly List<PcMapBlockEntry> _all = new();
        public int Count => _all.Count;
        public void Register(PcMapBlockEntry e) { if (e != null && e.mapId > 0) _all.Add(e); }
        public IReadOnlyList<PcMapBlockEntry> GetByMap(int mapId)
        {
            var list = new List<PcMapBlockEntry>();
            foreach (var e in _all) if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapBlockEntry> GetByType(int blockType)
        {
            var list = new List<PcMapBlockEntry>();
            foreach (var e in _all) if (e.blockType == blockType) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapBlockEntry> All => new List<PcMapBlockEntry>(_all);
    }
}
