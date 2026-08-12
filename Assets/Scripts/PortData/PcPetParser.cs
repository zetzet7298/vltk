// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/petsys/pet_skill_def.txt pet skill parser
// Source: pet_skill_def.txt (GB2312, 21 cols).
//   Level  MagAttr1  Param1..3  spr  MagAttr2  Param1..3  + more
// Pet auto-attack / buff tables per level.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcPetParser
    {
        public const int LevelCol = 0;
        public const int MagAttr1Col = 1;
        public const int Param1Col = 2;
        public const int Param2Col = 3;
        public const int Param3Col = 4;
        public const int SprCol = 5;
        public const int MagAttr2Col = 6;

        public static List<PcPetSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcPetSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                rows.Add(new PcPetSkillEntry
                {
                    level = PcItemCommon.Int(cols, LevelCol),
                    magAttr1 = PcItemCommon.Int(cols, MagAttr1Col),
                    param1 = PcItemCommon.Int(cols, Param1Col),
                    param2 = PcItemCommon.Int(cols, Param2Col),
                    param3 = PcItemCommon.Int(cols, Param3Col),
                    spr = PcItemCommon.Int(cols, SprCol),
                    magAttr2 = PcItemCommon.Int(cols, MagAttr2Col),
                });
            }
            return rows;
        }

        public static PcPetSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcPetSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "pet_skill_def.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcPetSkillEntry
    {
        public int level;
        public int magAttr1;
        public int param1;
        public int param2;
        public int param3;
        public int spr;
        public int magAttr2;
    }

    public sealed class PcPetSkillRegistry
    {
        private readonly Dictionary<int, PcPetSkillEntry> _byLevel = new();
        public int Count => _byLevel.Count;
        public void Register(PcPetSkillEntry e) { if (e == null || e.level <= 0) return; _byLevel[e.level] = e; }
        public PcPetSkillEntry GetLevel(int level) => _byLevel.TryGetValue(level, out var v) ? v : null;
    }
}
