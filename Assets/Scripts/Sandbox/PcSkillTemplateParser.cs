// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/skill_template.txt Template Skill parser
// Source: skill_template.txt / missletemplate.txt (219 entries, GB2312, tab).
//   TemplateId  TemplateName  MissleId  EffectType  Duration  PeriodMs
//   MaxStacks
// Template = cấu hình hiệu ứng đạn (buff, debuff, dot, hot,...) áp dụng cho
// missle/chiêu thức. Cùng MissleId có thể nhiều template tùy cấp.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcSkillTemplateParser
    {
        public const int TemplateIdCol = 0;
        public const int TemplateNameCol = 1;
        public const int MissleIdCol = 2;
        public const int EffectTypeCol = 3;
        public const int DurationCol = 4;
        public const int PeriodMsCol = 5;
        public const int MaxStacksCol = 6;

        public static List<PcSkillTemplateEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillTemplateEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, TemplateIdCol);
                if (id <= 0) continue;
                rows.Add(new PcSkillTemplateEntry
                {
                    templateId = id,
                    nameRaw = PcItemCommon.Str(cols, TemplateNameCol),
                    missleId = PcItemCommon.Int(cols, MissleIdCol),
                    effectType = PcItemCommon.Int(cols, EffectTypeCol),
                    duration = cols.Length > DurationCol ? PcItemCommon.Int(cols, DurationCol) : 0,
                    periodMs = cols.Length > PeriodMsCol ? PcItemCommon.Int(cols, PeriodMsCol) : 0,
                    maxStacks = cols.Length > MaxStacksCol ? PcItemCommon.Int(cols, MaxStacksCol) : 0,
                });
            }
            return rows;
        }

        public static PcSkillTemplateRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillTemplateRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var s in ParseFile(f)) reg.Register(s);
            return reg;
        }
    }

    [System.Serializable]
    public class PcSkillTemplateEntry
    {
        public int templateId;
        public string nameRaw;
        public int missleId;
        public int effectType;
        public int duration;
        public int periodMs;
        public int maxStacks;
    }

    public sealed class PcSkillTemplateRegistry
    {
        private readonly Dictionary<int, PcSkillTemplateEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcSkillTemplateEntry e)
        {
            if (e == null || e.templateId <= 0) return;
            _byId[e.templateId] = e;
        }
        public PcSkillTemplateEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IEnumerable<PcSkillTemplateEntry> GetAll() => _byId.Values;
    }
}
