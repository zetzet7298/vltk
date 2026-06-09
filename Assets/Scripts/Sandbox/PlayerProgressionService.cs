using System.Collections.Generic;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Scoped sandbox progression for the HUD skill button requirement.</summary>
    public sealed class PlayerProgressionState
    {
        public const int CaiBangSkillPanelLevel = 200;
        public const int CaiBangSkillPanelPoints = 200;
        // Sandbox default baseline (PC parity chua co cho cac mon phai khac).
        // Moi mon phai deu dung gia tri nay cho den khi co PC data rieng.
        public const int SandboxDefaultSkillPanelLevel = 200;
        public const int SandboxDefaultSkillPanelPoints = 200;
        public const int SkillUpgradePointCost = 1;
        // 1539 = Thiên Hạ Vô Cẩu NPC variant (ReqLevel 1, MaxLevel 60). MOD-only boss skill
        // registered in the catalog for boss AI, but NOT shown in the player skill panel.
        public const int NpcVariantSkillId = 1539;

        public int level = 1;
        public int fightSkillPoints;
        public CombatFaction faction = CombatFaction.None;
        public HashSet<int> knownSkills = new();
        public Dictionary<int, int> skillLevels = new();
        public int translife4SkillPoints;
        public int translife4UsedSkillPoints;

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
            // Lay baseline theo mon phai (khong con hard-code CaiBang).
            var baseline = GetFactionSkillPanelBaseline(targetFaction);
            level = baseline.level;
            if (firstGrant)
                fightSkillPoints = baseline.points;
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

        /// <summary>
        /// Tra ve (level, points) baseline cho skill panel theo mon phai. PC parity
        /// cho moi mon phai chua co, nen mac dinh tat ca dung sandbox baseline 200/200.
        /// </summary>
        public static (int level, int points) GetFactionSkillPanelBaseline(CombatFaction targetFaction)
        {
            switch (targetFaction)
            {
                case CombatFaction.CaiBang:
                    return (CaiBangSkillPanelLevel, CaiBangSkillPanelPoints);
                case CombatFaction.Shaolin:
                case CombatFaction.TianWang:
                case CombatFaction.TangMen:
                case CombatFaction.None:
                default:
                    return (SandboxDefaultSkillPanelLevel, SandboxDefaultSkillPanelPoints);
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

        public void GrantEMeiSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.EMei);
        }

        public void GrantTianWangSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.TianWang);
        }

        public void GrantWuDuSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.WuDu);
        }

        public void GrantCuiYanSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.CuiYan);
        }

        public void GrantTianRenSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.TianRen);
        }

        public void GrantKunLunSkillPanelProgression(SkillCatalog catalog)
        {
            GrantFactionSkillPanelProgression(catalog, CombatFaction.KunLun);
        }

        public void MaxAllSkillLevels(SkillCatalog catalog)
        {
            if (catalog == null) return;
            if (faction == CombatFaction.None)
                faction = CombatFaction.CaiBang; // Default for test suite compatibility
            // Dung helper theo mon phai thay vi hard-code CaiBang.
            var baseline = GetFactionSkillPanelBaseline(faction);
            level = baseline.level;
            fightSkillPoints = baseline.points;
            foreach (var skill in catalog.All)
            {
                if (skill.faction != CombatFaction.CaiBang && skill.faction != CombatFaction.WuDang && skill.faction != CombatFaction.Shaolin && skill.faction != CombatFaction.TangMen && skill.faction != CombatFaction.EMei && skill.faction != CombatFaction.TianWang && skill.faction != CombatFaction.WuDu && skill.faction != CombatFaction.CuiYan && skill.faction != CombatFaction.TianRen && skill.faction != CombatFaction.KunLun) continue;
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
            return CanUpgradeSkill(skill, SkillLevelUpScriptCatalog.CreateDefault(), addPoint);
        }

        public bool CanUpgradeSkill(SkillDefinition skill, SkillLevelUpScriptCatalog rules, int addPoint = SkillUpgradePointCost)
        {
            if (skill == null || !knownSkills.Contains(skill.skillId) || addPoint <= 0)
                return false;

            var rule = ResolveLevelUpRule(skill, rules);
            int availablePoints = rule != null && rule.usesTranslife4PointPool ? translife4SkillPoints : fightSkillPoints;
            if (availablePoints < addPoint)
                return false;

            int current = GetSkillLevel(skill.skillId);
            int desired = current + addPoint;
            if (desired > GetLevelCap(skill))
                return false;

            return MeetsLevelUpPrerequisites(rule, current);
        }

        public bool TryUpgradeSkill(SkillDefinition skill, int addPoint = SkillUpgradePointCost)
        {
            return TryUpgradeSkill(skill, SkillLevelUpScriptCatalog.CreateDefault(), addPoint);
        }

        public bool TryUpgradeSkill(SkillDefinition skill, SkillLevelUpScriptCatalog rules, int addPoint = SkillUpgradePointCost)
        {
            if (!CanUpgradeSkill(skill, rules, addPoint))
                return false;

            var rule = ResolveLevelUpRule(skill, rules);
            skillLevels[skill.skillId] = GetSkillLevel(skill.skillId) + addPoint;
            if (rule != null && rule.usesTranslife4PointPool)
            {
                translife4SkillPoints -= addPoint;
                translife4UsedSkillPoints += addPoint;
            }
            else
            {
                fightSkillPoints -= addPoint;
            }
            return true;
        }

        private bool MeetsLevelUpPrerequisites(SkillLevelUpRule rule, int currentLevel)
        {
            if (rule == null || rule.prerequisites == null || rule.prerequisites.Count == 0)
                return true;

            foreach (var req in rule.prerequisites)
            {
                int requiredLevel = req.minimumLevel;
                if (currentLevel <= 15)
                    requiredLevel = currentLevel + 5;
                if (requiredLevel < req.minimumLevel)
                    requiredLevel = req.minimumLevel;
                if (GetSkillLevel(req.skillId) < requiredLevel)
                    return false;
            }
            return true;
        }

        private static SkillLevelUpRule ResolveLevelUpRule(SkillDefinition skill, SkillLevelUpScriptCatalog rules)
        {
            if (skill == null || rules == null) return null;
            return rules.Resolve(skill.skillId) ?? rules.ResolveScript(skill.levelUpScript);
        }
    }
}
