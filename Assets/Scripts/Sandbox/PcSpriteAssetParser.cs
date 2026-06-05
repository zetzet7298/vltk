// -----------------------------------------------------------------------------
// VLTK Mobile — PC spriteasset.txt sprite asset registry parser
// Source: server settings/spriteasset.txt (Reference/PcSprite).
// Cols: SpriteId, Name, Path, Width, Height, Category, FramesCount
// Categories: 0=player, 1=npc, 2=item, 3=effect, 4=ui, 5=map
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSpriteAssetParser
    {
        public const int SpriteIdCol = 0;
        public const int NameCol = 1;
        public const int PathCol = 2;
        public const int WidthCol = 3;
        public const int HeightCol = 4;
        public const int CategoryCol = 5;
        public const int FramesCountCol = 6;

        public static List<PcSpriteAssetEntry> ParseFile(string path)
        {
            var rows = new List<PcSpriteAssetEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, SpriteIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSpriteAssetEntry
                {
                    spriteId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    path = PcItemCommon.Str(cols, PathCol),
                    width = PcItemCommon.Int(cols, WidthCol),
                    height = PcItemCommon.Int(cols, HeightCol),
                    category = PcItemCommon.Int(cols, CategoryCol),
                    framesCount = PcItemCommon.Int(cols, FramesCountCol),
                });
            }
            return rows;
        }

        public static PcSpriteAssetRegistry BuildRegistry(string dir)
        {
            var reg = new PcSpriteAssetRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSpriteAssetEntry
    {
        public int spriteId;
        public string name;
        public string path;
        public int width;
        public int height;
        public int category; // 0=player, 1=npc, 2=item, 3=effect, 4=ui, 5=map
        public int framesCount;
    }

    public sealed class PcSpriteAssetRegistry
    {
        private readonly Dictionary<int, PcSpriteAssetEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcSpriteAssetEntry e) { if (e == null || e.spriteId <= 0) return; _byId[e.spriteId] = e; }
        public PcSpriteAssetEntry Get(int spriteId) => _byId.TryGetValue(spriteId, out var v) ? v : null;
        public IReadOnlyList<PcSpriteAssetEntry> GetByCategory(int category)
        {
            var list = new List<PcSpriteAssetEntry>();
            foreach (var e in _byId.Values)
                if (e.category == category) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcSpriteAssetEntry> All => new List<PcSpriteAssetEntry>(_byId.Values);
    }
}
