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

        // Horse unlock: PC source vltksource_new/vl_update_27/Client 6.0/settings/item/000/horseres.txt
        // Sandbox default: player joins at level 30 (CaiBang quest complete) and unlocks
        // a basic horse. SandboxBoot overrides to red (id=5) so testers see the 5-color mount.
        public const int MinHorseLevel = 30;
        public int horseId = 1; // 0 = no horse, 1/3/5/7/9 = blue/yellow/red/white/black

        public bool HasHorse => horseId > 0;

        public static readonly int[] AvailableHorseIds = { 1, 3, 5, 7, 9 };

        /// <summary>
        /// Compute unlocked horse id from level (PC tiering). 1-29 = none,
        /// 30-49 = 1 (blue), 50-69 = 3, 70-89 = 5, 90-109 = 7, 110+ = 9.
        /// </summary>
        public static int HorseIdForLevel(int playerLevel)
        {
            if (playerLevel < MinHorseLevel) return 0;
            int tier = (playerLevel - MinHorseLevel) / 20;
            if (tier < 0) tier = 0;
            if (tier >= AvailableHorseIds.Length) tier = AvailableHorseIds.Length - 1;
            return AvailableHorseIds[tier];
        }

        public void GrantFactionSkillPanelProgression(SkillCatalog catalog, CombatFaction targetFaction)
        {
            bool firstGrant = faction != targetFaction || knownSkills.Count == 0;
            level = CaiBangSkillPanelLevel;
            if (firstGrant)
                fightSkillPoints = CaiBangSkillPanelPoints;
            faction = targetFaction;

            if (catalog == null)
                return;

            foreach (var skill in catalog.All)
            {
                if (skill.faction != targetFaction)
                    continue;
                // Hide the NPC/boss variant (1539) from the player panel.
                if (skill.skillId == NpcVariantSkillId)
                    continue;

                knownSkills.Add(skill.skillId);
                if (!skillLevels.ContainsKey(skill.skillId))
                    skillLevels[skill.skillId] = 0;
            }
        }

        public void GrantCaiBangSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.CaiBang);
        }

        public void GrantWuDangSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.WuDang);
        }

        public void GrantShaolinSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.Shaolin);
        }

        public void GrantTangMenSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.TangMen);
        }

        public void MaxAllSkillLevels(SkillCatalog catalog)
        {
            if (catalog == null) return;
            faction = CombatFaction.CaiBang; // Default for test suite compatibility
            level = CaiBangSkillPanelLevel;
            fightSkillPoints = CaiBangSkillPanelPoints;
            foreach (var skill in catalog.All)
            {
                if (skill.faction != CombatFaction.CaiBang && skill.faction != CombatFaction.WuDang && skill.faction != CombatFaction.Shaolin && skill.faction != CombatFaction.TangMen) continue;
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
