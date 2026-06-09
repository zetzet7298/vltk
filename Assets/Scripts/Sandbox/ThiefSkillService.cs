// -----------------------------------------------------------------------------
// VLTK Mobile — PC thiefskill runtime lookup service.
// Source: PC settings/thiefskill.txt + skills.txt (Reference/PcSkill).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ThiefSkillService
    {
        private PcThiefSkillRegistry _registry;
        public int Count => _registry != null ? _registry.Count : 0;

        public ThiefSkillService() { }
        public ThiefSkillService(PcThiefSkillRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcThiefSkillRegistry registry)
        {
            _registry = registry;
            if (_registry == null || _registry.Count == 0)
                SubsystemLog.Warn("ThiefSkill", "Thief skill registry rỗng");
        }

        public PcThiefSkillEntry GetSkill(int skillId)
            => _registry != null ? _registry.Get(skillId) : null;

        public PcThiefSkillEntry GetByThiefStyle(int thiefStyle)
            => _registry != null ? _registry.GetByThiefStyle(thiefStyle) : null;

        public IReadOnlyList<PcThiefSkillEntry> GetAllSkills()
            => _registry != null ? _registry.All : System.Array.Empty<PcThiefSkillEntry>();

        public static ThiefSkillService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference/PcSkill");
            var registry = PcThiefSkillParser.BuildRegistry(root);
            return new ThiefSkillService(registry);
        }
    }
}
