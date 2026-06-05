// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/npcskills.txt Kỹ Năng Quái / Boss parser
// Source: npcskills.txt (43 entries, GB2312, tab-separated).
//   SkillId  SkillName  NpcTemplateId  MinNpcLevel  MaxNpcLevel
//   Damage  Radius  CoolDownMs
// Skill quái = skill riêng của từng template NPC / boss, dùng cho AI dùng skill.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcNpcSkillParser
    {
        public const int SkillIdCol = 0;
        public const int SkillNameCol = 1;
        public const int NpcTemplateCol = 2;
        public const int MinNpcLevelCol = 3;
        public const int MaxNpcLevelCol = 4;
        public const int DamageCol = 5;
        public const int RadiusCol = 6;
        public const int CoolDownCol = 7;

        public static List<PcNpcSkillEntry> ParseFile(string path)
        {
            var rows = new List<PcNpcSkillEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 5) continue;
                int id = PcItemCommon.Int(cols, SkillIdCol);
                if (id <= 0) continue;
                rows.Add(new PcNpcSkillEntry
                {
                    skillId = id,
                    nameRaw = PcItemCommon.Str(cols, SkillNameCol),
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateCol),
                    minNpcLevel = PcItemCommon.Int(cols, MinNpcLevelCol),
                    maxNpcLevel = cols.Length > MaxNpcLevelCol ? PcItemCommon.Int(cols, MaxNpcLevelCol) : 0,
                    damage = cols.Length > DamageCol ? PcItemCommon.Int(cols, DamageCol) : 0,
                    radius = cols.Length > RadiusCol ? PcItemCommon.Int(cols, RadiusCol) : 0,
                    coolDownMs = cols.Length > CoolDownCol ? PcItemCommon.Int(cols, CoolDownCol) : 0,
                });
            }
            return rows;
        }

        public static PcNpcSkillRegistry BuildRegistry(string dir)
        {
            var reg = new PcNpcSkillRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "npcskills.txt");
            if (File.Exists(main))
                foreach (var s in ParseFile(main)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcNpcSkillEntry
    {
        public int skillId;
        public string nameRaw;
        public int npcTemplateId;
        public int minNpcLevel;
        public int maxNpcLevel;
        public int damage;
        public int radius;
        public int coolDownMs;
    }

    public sealed class PcNpcSkillRegistry
    {
        private readonly Dictionary<int, PcNpcSkillEntry> _byId = new();
        private readonly Dictionary<int, List<PcNpcSkillEntry>> _byNpc = new();
        public int Count => _byId.Count;
        public void Register(PcNpcSkillEntry e)
        {
            if (e == null || e.skillId <= 0) return;
            _byId[e.skillId] = e;
            if (e.npcTemplateId > 0)
            {
                if (!_byNpc.TryGetValue(e.npcTemplateId, out var list))
                {
                    list = new List<PcNpcSkillEntry>();
                    _byNpc[e.npcTemplateId] = list;
                }
                list.Add(e);
            }
        }
        public PcNpcSkillEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcNpcSkillEntry> GetByNpcTemplate(int templateId)
            => _byNpc.TryGetValue(templateId, out var v)
                ? (IReadOnlyList<PcNpcSkillEntry>)v
                : (IReadOnlyList<PcNpcSkillEntry>)System.Array.Empty<PcNpcSkillEntry>();
    }
}
