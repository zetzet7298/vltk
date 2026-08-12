// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/mount.txt / horse_detail.txt parser
// Source: mount.txt hoặc horse_detail.txt.
// Cols: MountId, Name, SpritePath, Speed, StaminaCost, RequiredLevel, CostSilver, SpriteMountedPath
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMountParser
    {
        public const int MountIdCol = 0;
        public const int NameCol = 1;
        public const int SpritePathCol = 2;
        public const int SpeedCol = 3;
        public const int StaminaCostCol = 4;
        public const int RequiredLevelCol = 5;
        public const int CostSilverCol = 6;
        public const int SpriteMountedPathCol = 7;

        public static List<PcMountEntry> ParseFile(string path)
        {
            var rows = new List<PcMountEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, MountIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMountEntry
                {
                    mountId = id,
                    name = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpritePathCol),
                    speed = PcItemCommon.Int(cols, SpeedCol),
                    staminaCost = PcItemCommon.Int(cols, StaminaCostCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    costSilver = PcItemCommon.Int(cols, CostSilverCol),
                    spriteMountedPath = PcItemCommon.Str(cols, SpriteMountedPathCol),
                });
            }
            return rows;
        }

        public static PcMountRegistry BuildRegistry(string dir)
        {
            var reg = new PcMountRegistry();
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
    public class PcMountEntry
    {
        public int mountId;
        public string name;
        public string spritePath;
        public int speed;
        public int staminaCost;
        public int requiredLevel;
        public int costSilver;
        public string spriteMountedPath;
    }

    public sealed class PcMountRegistry
    {
        private readonly Dictionary<int, PcMountEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMountEntry e) { if (e == null || e.mountId <= 0) return; _byId[e.mountId] = e; }
        public PcMountEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMountEntry> GetByLevel(int level)
        {
            var list = new List<PcMountEntry>();
            foreach (var e in _byId.Values)
                if (e.requiredLevel <= level) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMountEntry> All => new List<PcMountEntry>(_byId.Values);
    }
}
