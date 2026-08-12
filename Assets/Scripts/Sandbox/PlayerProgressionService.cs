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
        // 1539  = Thiên Hạ Vô Cẩu NPC variant (ReqLevel 1, MaxLevel 60). MOD-only boss skill.
        // 1101/1103/1161/1162 = Thừa Lục Long / Cửu Cái Bang 150 NPC variants — registered
        // in the catalog for boss/mob AI, but NOT shown in the player skill panel.
        public const int NpcVariantSkillId = 1539;
        public static readonly System.Collections.Generic.HashSet<int> NpcVariantSkillIds = new()
        {
            1539,   // Thiên Hạ Vô Cẩu NPC
            1101,   // Thừa Lục Long (đa mục tiêu) NPC
            1103,   // Thừa Lục Long (không script) NPC
            1161,   // Thừa Lục Long NPC
            1162,   // Cửu Cái Bang 150 NPC
        };
          public static bool IsNpcVariant(int skillId) => NpcVariantSkillIds.Contains(skillId);

          // Observed Unity panel rows without canonical PC learning evidence. They may be
          // resolved for the legacy display contract, but must never enter player
          // learned/cast/upgrade state.
          public static readonly HashSet<int> TangMenDisplayResidualSkillIds = new()
          {
              51, 55, 57,
          };
          // SKL-KL-PROOF-001: canonical KunLun learned membership = the 24 progression/skillbook
          // roots frozen in PcKunLunOracle.json (sha256 3be67129...). GrantFactionSkillPanel
          // Progression, upgrade eligibility (via knownSkills), and MaxAllSkillLevels all honor this
          // predicate: the catalog KunLun faction skills minus the five unresolved residuals below
          // equal exactly this set. 170/177/180/183/184 and the 14 support-only relationship
          // targets (14-22,290,342,387,399,1109) never enter learned/cast/upgrade state.
          public static readonly HashSet<int> KunLunLearnedSkillIds = new()
          {
              90, 167, 168, 169, 171, 172, 173, 174, 175, 176, 178, 179, 181, 182,
              275, 372, 375, 392, 393, 394, 630, 717, 1080, 1081,
          };
            public static readonly HashSet<int> KunLunDisplayResidualSkillIds = new()
          {
              170, 177, 180, 183, 184,
            };
            public static bool IsKunLunLearnedSkill(int skillId) => KunLunLearnedSkillIds.Contains(skillId);
            public static bool IsCanonicalLearnedSkillForFaction(CombatFaction targetFaction, int skillId)
            {
                if (targetFaction == CombatFaction.KunLun)
                    return IsKunLunLearnedSkill(skillId);
                if (targetFaction == CombatFaction.TangMen)
                    return !TangMenDisplayResidualSkillIds.Contains(skillId);
                return true;
            }
            public static bool IsDisplayOnlyResidual(int skillId) =>
              TangMenDisplayResidualSkillIds.Contains(skillId) ||
              KunLunDisplayResidualSkillIds.Contains(skillId);

        public int level = 1;
        public int fightSkillPoints;
        public CombatFaction faction = CombatFaction.None;
        public HashSet<int> knownSkills = new();
        public Dictionary<int, int> skillLevels = new();
        public int translife4SkillPoints;
        public int translife4UsedSkillPoints;

        // Horse unlock: PC source jx-pc/00.src-tinh-kiem/Client 6.0/settings/item/000/horseres.txt
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
            // A runtime faction switch replaces the learned set. Keeping the previous
            // faction here makes the combat actor pass the known-skill gate for both
            // factions and leaves the hotbar populated with stale skills.
            if (firstGrant)
            {
                knownSkills.Clear();
                skillLevels.Clear();
            }
            // Lay baseline theo mon phai (khong con hard-code CaiBang).
            var baseline = GetFactionSkillPanelBaseline(targetFaction);
            level = baseline.level;
            if (firstGrant)
                fightSkillPoints = baseline.points;
            faction = targetFaction;

              if (targetFaction == CombatFaction.TangMen)
              {
                  foreach (int residualId in TangMenDisplayResidualSkillIds)
                  {
                      knownSkills.Remove(residualId);
                      skillLevels.Remove(residualId);
                  }
              }
              if (targetFaction == CombatFaction.KunLun)
              {
                  // SKL-KL-PROOF-001: defensively drop unresolved residuals; the faction-filter
                  // loop below also excludes them via IsDisplayOnlyResidual so knownSkills ends up
                  // exactly the 24 canonical learned roots.
                  foreach (int residualId in KunLunDisplayResidualSkillIds)
                  {
                      knownSkills.Remove(residualId);
                      skillLevels.Remove(residualId);
                  }
              }

              if (catalog == null)
                  return;

              foreach (var skill in catalog.All)
              {
                    if (skill.faction != targetFaction)
                        continue;
                    if (!IsCanonicalLearnedSkillForFaction(targetFaction, skill.skillId))
                        continue;
                    // Hide NPC/boss variants from the player skill panel.
                  if (IsNpcVariant(skill.skillId))
                      continue;
                  if (IsDisplayOnlyResidual(skill.skillId))
                      continue;

                    knownSkills.Add(skill.skillId);
                    if (!skillLevels.ContainsKey(skill.skillId))
                        skillLevels[skill.skillId] = 0;
                }

            // PC universal action skill: Khinh Công (轻功, id=210) is not tied to a faction,
            // but it is a player action button/slot and must be known for KSkillList::FindSame.
            var lightness = catalog.Resolve(PcCombatCatalogFactory.UniversalLightnessSkill);
            if (lightness != null && !IsNpcVariant(lightness.skillId))
            {
                knownSkills.Add(lightness.skillId);
                if (!skillLevels.ContainsKey(lightness.skillId) || skillLevels[lightness.skillId] <= 0)
                    skillLevels[lightness.skillId] = 1;
            }
        }

        /// <summary>GM runtime switch: rebuild the learned set even when selecting the current faction.</summary>
        public void ReplaceFactionSkillPanelProgression(SkillCatalog catalog, CombatFaction targetFaction)
        {
            knownSkills.Clear();
            skillLevels.Clear();
            GrantFactionSkillPanelProgression(catalog, targetFaction);
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
                // SKL-KL-PROOF-001: operate on the current faction plus universal PC actions, not
                // all faction definitions. Residuals (incl. KunLun 170/177/180/183/184) and NPC
                // variants are excluded so only the canonical learned set can be maxed.
                bool factionSkill = skill.faction == faction;
                bool universalPcActionSkill = skill.skillId == PcCombatCatalogFactory.UniversalLightnessSkill || skill.isLeapSkill;
                if (!factionSkill && !universalPcActionSkill) continue;
                if (factionSkill && !IsCanonicalLearnedSkillForFaction(faction, skill.skillId)) continue;
                  if (IsNpcVariant(skill.skillId)) continue;
                  if (IsDisplayOnlyResidual(skill.skillId)) continue;
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
