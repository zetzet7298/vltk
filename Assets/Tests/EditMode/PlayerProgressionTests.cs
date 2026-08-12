// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.2 Player Progression Tests
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PlayerProgressionTests
    {
        [Test]
        public void ExpRequired_CalculatesCorrectly()
        {
            // Level 1 -> 100
            Assert.AreEqual(100, PlayerStatService.GetExpRequired(1));
            // Level 2 -> 132
            long level2Exp = PlayerStatService.GetExpRequired(2);
            Assert.Greater(level2Exp, 100);
            Assert.Less(level2Exp, 1000);
            // Cap at 99
            Assert.AreEqual(long.MaxValue, PlayerStatService.GetExpRequired(99));
        }

        [Test]
        public void CalculateStats_AppliesDexterityToDefenseAndAR()
        {
            var equip = new EquipmentBonus();
            var stats = PlayerStatService.CalculateStats(
                level: 10,
                factionId: CombatFactionExt.ShaolinId,
                strength: 30,
                dexterity: 40,
                vitality: 30,
                innerStrength: 20,
                equip: equip
            );

            // Defense = dex * 0.25 = 10
            Assert.AreEqual(10, stats.defense);
            // AttackRating = dex * 4 = 160
            Assert.AreEqual(160, stats.attackRating);
            // HP max = 50 + vit * 4 + level * 1.5 = 50 + 120 + 15 = 185
            Assert.AreEqual(185, stats.hpMax);
        }

        [Test]
        public void CalculateStats_CapsResistancesAt95()
        {
            var equip = new EquipmentBonus
            {
                fireResist = 150, // Massive bonus
                coldResist = 80
            };

            var stats = PlayerStatService.CalculateStats(
                level: 10,
                factionId: CombatFactionExt.ShaolinId,
                strength: 20,
                dexterity: 20,
                vitality: 20,
                innerStrength: 20,
                equip: equip
            );

            Assert.AreEqual(95, stats.fireResist);
            Assert.AreEqual(80, stats.coldResist); // Shaolin has no cold resist, so 80
        }

        [Test]
        public void LevelService_LevelsUp_GrantsPotentialAndSkillPoints()
        {
            var levelService = new PlayerLevelService(1);
            Assert.AreEqual(1, levelService.Level);
            Assert.AreEqual(0, levelService.PotentialPoints);
            Assert.AreEqual(0, levelService.SkillPoints);

            // Add enough EXP to level up to level 2 (requires 100 exp)
            levelService.AddExp(120);

            Assert.AreEqual(2, levelService.Level);
            Assert.AreEqual(5, levelService.PotentialPoints);
            Assert.AreEqual(1, levelService.SkillPoints);
            Assert.AreEqual(20, levelService.CurrentExp); // 120 - 100
        }

        [Test]
        public void LevelService_DistributesPotentialPoints()
        {
            var levelService = new PlayerLevelService(1);
            levelService.AddExp(100); // Level 2, grants 5 points

            Assert.AreEqual(5, levelService.PotentialPoints);
            int startStr = levelService.Strength;

            bool success = levelService.DistributePotential(str: 3, dex: 2, vit: 0, inner: 0);

            Assert.IsTrue(success);
            Assert.AreEqual(0, levelService.PotentialPoints);
            Assert.AreEqual(startStr + 3, levelService.Strength);
        }

        [Test]
        public void SkillPointService_UpgradesAndResetsSkills()
        {
            var catalog = new SkillCatalog();
            // Đăng ký skill Cái Bang
            var skill = new SkillDefinition
            {
                skillId = 117,
                nameRaw = "Đầu Thạch Vấn Lộ",
                reqLevel = 10,
                maxLevel = 20,
                faction = (CombatFaction)CombatFactionExt.CaiBangId
            };
            catalog.Register(skill);

            var prog = new PlayerProgressionState();
            var levelService = new PlayerLevelService(10); // Start at level 10
            levelService.GrantSkillPoint(5); // Grant 5 skill points for testing

            var pointService = new PlayerSkillPointService(prog, levelService, catalog);
            pointService.JoinFaction(CombatFactionExt.CaiBangId);

            // Kiểm tra xem knownSkills có chứa 117 không
            Assert.IsTrue(prog.knownSkills.Contains(117), "knownSkills does not contain 117 after JoinFaction!");

            // Level cap for skill at lvl 10 = playerLevel(10) - reqLevel(10) + 1 = 1
            Assert.AreEqual(1, prog.GetLevelCap(skill));

            // Upgrade
            bool upgraded = pointService.UpgradeSkill(117);
            Assert.IsTrue(upgraded);
            Assert.AreEqual(1, prog.GetSkillLevel(117));
            Assert.AreEqual(4, levelService.SkillPoints);

            // Try to upgrade again (should fail because level cap at player level 10 is 1)
            bool upgradedAgain = pointService.UpgradeSkill(117);
            Assert.IsFalse(upgradedAgain);
            Assert.AreEqual(1, prog.GetSkillLevel(117));

            // Reset
            pointService.ResetSkills();
            Assert.AreEqual(0, prog.GetSkillLevel(117));
            Assert.AreEqual(5, levelService.SkillPoints); // Reclaimed point
        }

        [Test]
        public void LevelUpScriptRule_RequiresPrerequisiteSkillsAndSpendsFightPoint()
        {
            var catalog = SkillLevelUpScriptCatalog.CreateDefault();
            var skill = new SkillDefinition
            {
                skillId = 332,
                nameRaw = "Phổ Độ Chúng Sinh",
                reqLevel = 80,
                maxLevel = 20,
                levelUpScript = @"\script\skill\lvlup_pudu_zhongsheng.lua"
            };
            var prog = new PlayerProgressionState { level = 95, fightSkillPoints = 2 };
            prog.knownSkills.Add(332);
            prog.skillLevels[332] = 1;
            foreach (int req in new[] { 93, 89, 86, 92, 282 })
                prog.skillLevels[req] = 5;

            Assert.IsFalse(prog.TryUpgradeSkill(skill, catalog), "PC requires prereqs >= current main skill level + 5 before level 16");

            foreach (int req in new[] { 93, 89, 86, 92, 282 })
                prog.skillLevels[req] = 6;

            Assert.IsTrue(prog.TryUpgradeSkill(skill, catalog));
            Assert.AreEqual(2, prog.GetSkillLevel(332));
            Assert.AreEqual(1, prog.fightSkillPoints);
        }

        [Test]
        public void Translife4Rule_UsesSeparatePointPool()
        {
            var catalog = SkillLevelUpScriptCatalog.CreateDefault();
            var skill = new SkillDefinition
            {
                skillId = 1123,
                nameRaw = "Vô Uy Thuẫn",
                reqLevel = 0,
                maxLevel = 20,
                levelUpScript = @"\script\skill\translife_4\lvlup_waigong.lua"
            };
            var prog = new PlayerProgressionState { level = 1, fightSkillPoints = 0, translife4SkillPoints = 1 };
            prog.knownSkills.Add(1123);

            Assert.IsTrue(prog.TryUpgradeSkill(skill, catalog));
            Assert.AreEqual(1, prog.GetSkillLevel(1123));
            Assert.AreEqual(0, prog.translife4SkillPoints);
            Assert.AreEqual(1, prog.translife4UsedSkillPoints);
            Assert.AreEqual(0, prog.fightSkillPoints);
        }
    }
}
