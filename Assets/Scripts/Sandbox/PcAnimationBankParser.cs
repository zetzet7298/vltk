// -----------------------------------------------------------------------------
// VLTK Mobile — PC animation.txt parser
// Source: settings/animation/animation.txt (Animation Bank - sprite animation data).
// Columns: AnimId Name SpritePath FrameCount FrameDelayMs IsLooping Direction
// Direction: 0-7 cho 8-way
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcAnimationBankParser
    {
        public const int AnimIdCol = 0;
        public const int NameCol = 1;
        public const int SpritePathCol = 2;
        public const int FrameCountCol = 3;
        public const int FrameDelayMsCol = 4;
        public const int IsLoopingCol = 5;
        public const int DirectionCol = 6;

        public static List<PcAnimationBankEntry> ParseFile(string path)
        {
            var rows = new List<PcAnimationBankEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, AnimIdCol);
                if (id <= 0) continue;
                rows.Add(new PcAnimationBankEntry
                {
                    animId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    spritePath = PcItemCommon.Str(cols, SpritePathCol),
                    frameCount = PcItemCommon.Int(cols, FrameCountCol),
                    frameDelayMs = PcItemCommon.Int(cols, FrameDelayMsCol),
                    isLooping = PcItemCommon.Int(cols, IsLoopingCol) != 0,
                    direction = PcItemCommon.Int(cols, DirectionCol),
                });
            }
            return rows;
        }

        public static PcAnimationBankRegistry BuildRegistry(string dir)
        {
            var reg = new PcAnimationBankRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("animation") || name.StartsWith("anim"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcAnimationBankEntry
    {
        public int animId;
        public string nameRaw;
        public string spritePath;
        public int frameCount;
        public int frameDelayMs;
        public bool isLooping;
        public int direction;
    }

    public sealed class PcAnimationBankRegistry
    {
        private readonly Dictionary<int, PcAnimationBankEntry> _byId = new();
        private readonly Dictionary<string, PcAnimationBankEntry> _byName = new(System.StringComparer.OrdinalIgnoreCase);
        public int Count => _byId.Count;
        public void Register(PcAnimationBankEntry e)
        {
            if (e == null || e.animId <= 0) return;
            _byId[e.animId] = e;
            if (!string.IsNullOrEmpty(e.nameRaw)) _byName[e.nameRaw] = e;
        }
        public PcAnimationBankEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public PcAnimationBankEntry GetByName(string name)
            => _byName.TryGetValue(name ?? string.Empty, out var v) ? v : null;
        public IReadOnlyList<PcAnimationBankEntry> GetByDirection(int direction)
        {
            var list = new List<PcAnimationBankEntry>();
            foreach (var e in _byId.Values)
                if (e.direction == direction) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcAnimationBankEntry> All => new List<PcAnimationBankEntry>(_byId.Values);
    }
}
