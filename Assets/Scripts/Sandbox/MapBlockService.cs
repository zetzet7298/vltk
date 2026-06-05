// -----------------------------------------------------------------------------
// VLTK Mobile — MapBlock runtime service
// Wraps PcMapBlockRegistry. PC source: settings/mapblock.txt.
// Vietnamese: "Vật Cản", "Cây", "Đá", "Nước", "Nhà", "Hàng Rào".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class MapBlockService
    {
        private readonly PcMapBlockRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public MapBlockService() { _reg = new PcMapBlockRegistry(); }
        public MapBlockService(PcMapBlockRegistry reg) { _reg = reg ?? new PcMapBlockRegistry(); }

        public static MapBlockService LoadFromStreamingAssets(string subDir = "Reference/PcMap")
        {
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            return new MapBlockService(PcMapBlockParser.BuildRegistry(path));
        }

        public IReadOnlyList<PcMapBlockEntry> GetBlocks(int mapId) => _reg.GetByMap(mapId);
        public IReadOnlyList<PcMapBlockEntry> GetByType(int blockType) => _reg.GetByType(blockType);
        public IReadOnlyList<PcMapBlockEntry> GetAll() => _reg.All;

        public IReadOnlyList<PcMapBlockEntry> GetBlocksByType(int mapId, int blockType)
        {
            var list = new List<PcMapBlockEntry>();
            foreach (var e in _reg.GetByMap(mapId)) if (e.blockType == blockType) list.Add(e);
            return list;
        }

        /// <summary>Kiểm tra vị trí (x,y) có đi qua được không. Passable=true thì luôn pass.</summary>
        public bool IsPassable(int mapId, float x, float y)
        {
            foreach (var b in _reg.GetByMap(mapId))
            {
                if (b.passable) continue;
                if (x >= b.blockX && x < b.blockX + b.width
                    && y >= b.blockY && y < b.blockY + b.height)
                    return false;
            }
            return true;
        }

        public Dictionary<int, int> CountBlocksByType(int mapId)
        {
            var dict = new Dictionary<int, int>();
            foreach (var e in _reg.GetByMap(mapId))
            {
                if (dict.ContainsKey(e.blockType)) dict[e.blockType]++;
                else dict[e.blockType] = 1;
            }
            return dict;
        }
    }
}
