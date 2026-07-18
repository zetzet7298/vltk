using System.Collections.Generic;
using System.Linq;

namespace VLTK.Sandbox
{
    public sealed class SkillLevelUpPrerequisite
    {
        public int skillId;
        public int minimumLevel;
    }

    public sealed class SkillLevelUpRule
    {
        public int skillId;
        public string levelUpScript;
        public bool usesTranslife4PointPool;
        public List<SkillLevelUpPrerequisite> prerequisites = new();
    }

    public sealed class SkillLevelUpScriptCatalog
    {
        private readonly Dictionary<int, SkillLevelUpRule> _bySkillId = new();
        private readonly Dictionary<string, SkillLevelUpRule> _byScript = new(System.StringComparer.OrdinalIgnoreCase);

        public int Count => _bySkillId.Count;
        public IReadOnlyCollection<SkillLevelUpRule> All => _bySkillId.Values;

        public SkillLevelUpRule Resolve(int skillId)
            => _bySkillId.TryGetValue(skillId, out var rule) ? rule : null;

        public SkillLevelUpRule ResolveScript(string levelUpScript)
        {
            if (string.IsNullOrEmpty(levelUpScript)) return null;
            return _byScript.TryGetValue(Normalize(levelUpScript), out var rule) ? rule : null;
        }

        public void Register(SkillLevelUpRule rule)
        {
            if (rule == null || rule.skillId <= 0) return;
            rule.levelUpScript = Normalize(rule.levelUpScript);
            _bySkillId[rule.skillId] = rule;
            if (!string.IsNullOrEmpty(rule.levelUpScript))
                _byScript[rule.levelUpScript] = rule;
        }

        public static SkillLevelUpScriptCatalog CreateDefault()
        {
            var catalog = new SkillLevelUpScriptCatalog();
            catalog.Register(Main(332, @"\script\skill\lvlup_pudu_zhongsheng.lua", 93, 89, 86, 92, 282));
            catalog.Register(Main(351, @"\script\skill\lvlup_luanhuan_ji.lua", 347, 303, 343, 345, 349));
            catalog.Register(Main(390, @"\script\skill\lvlup_duanjin_fugu.lua", 67, 70, 64, 356, 72));
            catalog.Register(Main(391, @"\script\skill\lvlup_shehun_luanxin.lua", 136, 137, 140, 364, 143));
            catalog.Register(Main(394, @"\script\skill\lvlup_zuixian_cuogu.lua", 392, 174, 393, 175, 90));
            catalog.Register(Main(1110, @"\script\skill\lvlup_pililuanhuan_ji.lua", 45, 351));

            catalog.Register(Translife(1123, @"\script\skill\translife_4\lvlup_waigong.lua"));
            catalog.Register(Translife(1124, @"\script\skill\translife_4\lvlup_neigong.lua"));
            catalog.Register(Translife(1125, @"\script\skill\translife_4\lvlup_liliang.lua"));
            catalog.Register(Translife(1126, @"\script\skill\translife_4\lvlup_shenfa.lua"));
            catalog.Register(Translife(1127, @"\script\skill\translife_4\lvlup_shengming.lua"));
            catalog.Register(Translife(1128, @"\script\skill\translife_4\lvlup_neili.lua"));
            catalog.Register(Translife(1129, @"\script\skill\translife_4\lvlup_mingzhong.lua"));
            catalog.Register(Translife(1130, @"\script\skill\translife_4\lvlup_shanbi.lua"));
            return catalog;
        }

        private static SkillLevelUpRule Main(int skillId, string script, params int[] prerequisiteIds)
        {
            return new SkillLevelUpRule
            {
                skillId = skillId,
                levelUpScript = script,
                prerequisites = prerequisiteIds.Select(id => new SkillLevelUpPrerequisite { skillId = id, minimumLevel = 5 }).ToList()
            };
        }

        private static SkillLevelUpRule Translife(int skillId, string script)
        {
            return new SkillLevelUpRule
            {
                skillId = skillId,
                levelUpScript = script,
                usesTranslife4PointPool = true
            };
        }

        private static string Normalize(string script)
        {
            return (script ?? string.Empty).Trim().Replace('/', '\\');
        }
    }
}
