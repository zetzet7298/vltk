// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/missles.txt missile (đạn) parser
// Source: missles.txt + missles1.txt + missletemplate.txt (GB2312, 57 cols).
//   MissleId  MissleName  MoveKind  FollowKind  ColFollowTarget  MissleHeight
//   CollidRange  IsRangeDmg  + 49 more (damage, speed, lifetime, ...)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMissleParser
    {
        public const int MissleIdCol = 0;
        public const int MissleNameCol = 1;
        public const int MoveKindCol = 2;
        public const int FollowKindCol = 3;
        public const int ColFollowTargetCol = 4;
        public const int MissleHeightCol = 5;
        public const int CollidRangeCol = 6;
        public const int IsRangeDmgCol = 7;

        public static List<PcMissleEntry> ParseFile(string path)
        {
            var rows = new List<PcMissleEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcMissleEntry
                {
                    missleId = PcItemCommon.Int(cols, MissleIdCol),
                    nameRaw = PcItemCommon.Str(cols, MissleNameCol),
                    moveKind = PcItemCommon.Int(cols, MoveKindCol),
                    followKind = PcItemCommon.Int(cols, FollowKindCol),
                    colFollowTarget = PcItemCommon.Int(cols, ColFollowTargetCol),
                    missleHeight = PcItemCommon.Int(cols, MissleHeightCol),
                    collidRange = PcItemCommon.Int(cols, CollidRangeCol),
                    isRangeDmg = PcItemCommon.Int(cols, IsRangeDmgCol) > 0,
                });
            }
            return rows;
        }

        public static PcMissleRegistry BuildRegistry(string dir)
        {
            var reg = new PcMissleRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMissleEntry
    {
        public int missleId;
        public string nameRaw;
        public int moveKind;
        public int followKind;
        public int colFollowTarget;
        public int missleHeight;
        public int collidRange;
        public bool isRangeDmg;
    }

    public sealed class PcMissleRegistry
    {
        private readonly Dictionary<int, PcMissleEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMissleEntry e) { if (e == null || e.missleId <= 0) return; _byId[e.missleId] = e; }
        public PcMissleEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcMissleEntry> All => _byId.Values;
    }
}
