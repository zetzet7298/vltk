using System.Collections.Generic;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Scoped sandbox progression for the HUD skill button requirement.</summary>
    public sealed class PlayerProgressionState
    {
        public const int CaiBangSkillPanelLevel = 200;
        public const int CaiBangSkillPanelPoints = 200;
        public const int SkillUpgradePointCost = 1;
        // 1539 = Thiên Hạ Vô Cẩu NPC variant (ReqLevel 1, MaxLevel 60). MOD-only boss skill
        // registered in the catalog for boss AI, but NOT shown in the player skill panel.
        public const int NpcVariantSkillId = 1539;

        public int level = 1;
        public int fightSkillPoints;
        public CombatFaction faction = CombatFaction.None;
        public HashSet<int> knownSkills = new();
        public Dictionary<int, int> skillLevels = new();

        public void GrantCaiBangSkillPanelProgression(SkillCatalog catalog)
        {
            bool firstGrant = faction != CombatFaction.CaiBang || knownSkills.Count == 0;
            level = CaiBangSkillPanelLevel;
            if (firstGrant)
                fightSkillPoints = CaiBangSkillPanelPoints;
            faction = CombatFaction.CaiBang;

            if (catalog == null)
                return;

            foreach (var skill in catalog.All)
            {
                if (!skill.IsCaiBang)
                    continue;
                // Hide the NPC/boss variant (1539) from the player panel.
                if (skill.skillId == NpcVariantSkillId)
                    continue;

                // PC faction join seeds faction skills into KSkillList at level 0; left-clicking a skill slot spends
                // one fight skill point and asks GOI_TONE_UP_SKILL / ApplyAddSkillLevel to raise it. Re-opening
                // the skills window must not reset learned levels or remaining points.
                knownSkills.Add(skill.skillId);
                if (!skillLevels.ContainsKey(skill.skillId))
                    skillLevels[skill.skillId] = 0;
            }
        }

        /// <summary>
        /// Set all known CaiBang skills to their maximum level for testing.
        /// Mirrors PC GM command that sets all skills to max.
        /// Called on every SandboxManager boot / domain reload.
        /// </summary>
        public void MaxAllSkillLevels(SkillCatalog catalog)
        {
            if (catalog == null) return;

            // Ensure progression is granted first
            GrantCaiBangSkillPanelProgression(catalog);

            foreach (var skill in catalog.All)
            {
                if (!skill.IsCaiBang) continue;
                if (skill.skillId == NpcVariantSkillId) continue;

                int maxLv = skill.maxLevel > 0 ? skill.maxLevel : 1;
                knownSkills.Add(skill.skillId);
                skillLevels[skill.skillId] = maxLv;
            }
        }

        public int GetSkillLevel(int skillId)
        {
            return skillLevels.TryGetValue(skillId, out var value) ? value : 0;
        }

        public int GetLevelCap(SkillDefinition skill)
        {
            if (skill == null)
                return 0;
            int max = skill.maxLevel > 0 ? skill.maxLevel : 1;
            int playerGate = level - skill.reqLevel + 1;
            if (playerGate < 0)
                playerGate = 0;
            return playerGate < max ? playerGate : max;
        }

        public bool CanUpgradeSkill(SkillDefinition skill, int addPoint = SkillUpgradePointCost)
        {
            if (skill == null || !knownSkills.Contains(skill.skillId) || addPoint <= 0 || fightSkillPoints < addPoint)
                return false;

            int current = GetSkillLevel(skill.skillId);
            int desired = current + addPoint;
            return desired <= GetLevelCap(skill);
        }

        public bool TryUpgradeSkill(SkillDefinition skill, int addPoint = SkillUpgradePointCost)
        {
            if (!CanUpgradeSkill(skill, addPoint))
                return false;

            skillLevels[skill.skillId] = GetSkillLevel(skill.skillId) + addPoint;
            fightSkillPoints -= addPoint;
            return true;
        }
    }
}
