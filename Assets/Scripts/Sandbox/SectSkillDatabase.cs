// -----------------------------------------------------------------------------
// VLTK Mobile — Sect Skill Database Bridge
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Bridge giữa SkillSectCatalog tiếng Việt và SkillCatalog runtime. Dùng để tra
    /// skill theo phái + chọn animation PC (Magic/Attack/Idle) cho player visual.
    /// </summary>
    public sealed class SectSkillDatabase
    {
        private readonly SkillCatalog _catalog;

        public SectSkillDatabase(SkillCatalog catalog)
        {
            _catalog = catalog;
        }

        public List<SectSkillEntry> GetSkillsBySect(SectType sect)
        {
            return SkillSectCatalog.GetSkills((int)sect);
        }

        public List<SkillDefinition> GetRuntimeSkillsBySect(SectType sect)
        {
            var result = new List<SkillDefinition>();
            if (_catalog == null) return result;

            foreach (var entry in GetSkillsBySect(sect))
            {
                var skill = _catalog.Resolve(entry.skillId);
                if (skill != null) result.Add(skill);
            }
            return result;
        }

        public PlayerVisualAction? GetSkillAnimation(SectSkillEntry skill, PcWeaponType weapon = PcWeaponType.EmptyHand)
        {
            var action = MalePlayerSpriteCatalog.ResolveAction(skill.charAnimId, weapon);
            if (action.HasValue) return action;
            if (skill.isMelee) return PlayerVisualAction.Attack;
            return skill.tier == SkillTier.Buff || skill.tier == SkillTier.Ultimate ? PlayerVisualAction.Magic : null;
        }

        public PlayerVisualAction? GetSkillAnimation(SkillDefinition skill, PcWeaponType weapon = PcWeaponType.EmptyHand)
        {
            if (skill == null) return null;
            var action = MalePlayerSpriteCatalog.ResolveAction(skill.charAnimId, weapon);
            if (action.HasValue) return action;
            if (skill.isMelee || skill.skillStyle == PcSkillStyle.Melee) return PlayerVisualAction.Attack;
            return skill.isAura ? null : PlayerVisualAction.Magic;
        }
    }
}
