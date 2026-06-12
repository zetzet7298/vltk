// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/translifeskill.txt Skill Chuyển Sinh parser
// Source: translifeskill.txt (9 entries, GB2312, tab-separated).
//   SkillId  SkillName  TranslifeLevel(1-4)  SkillType  ManaCost  Damage
// Skill Chuyển Sinh = skill đặc biệt ở mỗi lần chuyển sinh (1, 2, 3, 4).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcTranslifeSkillParser
    {
        public const int SkillIdCol = 0;
        public const int SkillNameCol = 1;
        public const int TranslifeLevelCol = 2;
        public const int SkillTypeCol = 3;
        public const int ManaCostCol = 4;
        public const int DamageCol = 5;

        public static List<PcTranslifeSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcTranslifeSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcText.ReadLinesTcvn3(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0) continue;
                rows.Add(new PcTranslifeSkillEntry
                {
                    skillId = id,
                    nameRaw = PcItemCommon.Str(cols, SkillNameCol),
                    translifeLevel = PcItemCommon.Int(cols, TranslifeLevelCol),
                    skillType = PcItemCommon.Int(cols, SkillTypeCol),
                    manaCost = cols.Length > ManaCostCol ? PcItemCommon.Int(cols, ManaCostCol) : 0,
                    damage = cols.Length > DamageCol ? PcItemCommon.Int(cols, DamageCol) : 0,
                });
            }
            return rows;
        }

        public static PcTranslifeSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcTranslifeSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "translifeskill.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcTranslifeSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public int translifeLevel;
        public int skillType;
        public int manaCost;
        public int damage;
    }

    public sealed class PcTranslifeSkillRegistry
    {
        private readonly Dictionary<int, PcTranslifeSkillEntry> _byId = new();
        private readonly Dictionary<int, List<PcTranslifeSkillEntry>> _byLevel = new();
        public int Count => _byId.Count;
        public void Register(PcTranslifeSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byId[e.skillId] = e;
            if (e.translifeLevel > 0)
            {
                if (!_byLevel.TryGetValue(e.translifeLevel, out var list))
                {
                    list = new List<PcTranslifeSkillEntry>();
                    _byLevel[e.translifeLevel] = list;
                }
                list.Add(e);
            }
        }
        public PcTranslifeSkillEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcTranslifeSkillEntry> GetByTranslifeLevel(int level)
            => _byLevel.TryGetValue(level, out var v)
                ? (IReadOnlyList<PcTranslifeSkillEntry>)v
                : (IReadOnlyList<PcTranslifeSkillEntry>)System.Array.Empty<PcTranslifeSkillEntry>();
    }
}
