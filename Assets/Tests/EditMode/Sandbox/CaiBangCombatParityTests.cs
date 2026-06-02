using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class CaiBangCombatParityTests
    {
        private SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
        private CombatActorState Novice(int weaponSkill = PcCombatCatalogFactory.NoviceShortWeaponAttack) => new CombatActorState
        {
            actorId = 1,
            faction = CombatFaction.None,
            level = 1,
            fightMode = true,
            currentMana = 100,
            currentWeaponSkillId = weaponSkill,
            activeSkillId = weaponSkill,
            position = Vector2.zero,
            knownSkills = { weaponSkill },
            skillLevels = { [weaponSkill] = 1 },
        };

        private CombatActorState Beggar(int level = 60) => new CombatActorState
        {
            actorId = 2,
            faction = CombatFaction.CaiBang,
            level = level,
            fightMode = true,
            currentMana = 500,
            position = Vector2.zero,
            knownSkills = { 115,116,117,118,119,120,121,122,123,124,125,126,127,128,129,130,209 },
            skillLevels = { [115]=20,[116]=20,[117]=20,[118]=20,[119]=20,[120]=20,[121]=20,[122]=20,[123]=20,[124]=20,[125]=20,[126]=20,[127]=20,[128]=30,[129]=20,[130]=20,[209]=20 },
        };

        private CombatActorState Enemy(Vector2 pos) => new CombatActorState { actorId = 9, faction = CombatFaction.None, level = 1, currentLife = 1000, position = pos };

        [Test]
        public void Catalog_LoadsNoviceAndAllCaiBangSkills()
        {
            var cat = Catalog();
            Assert.IsNotNull(cat.Resolve(PcCombatCatalogFactory.NoviceShortWeaponAttack));
            Assert.IsNotNull(cat.Resolve(PcCombatCatalogFactory.NoviceLongWeaponAttack));
            Assert.IsNotNull(cat.Resolve(PcCombatCatalogFactory.NoviceRangedAttack));
            for (int id = PcCombatCatalogFactory.CaiBangMinSkillId; id <= PcCombatCatalogFactory.CaiBangMaxSkillId; id++)
                Assert.IsNotNull(cat.Resolve(id), $"missing Cai Bang skill {id}");
            Assert.IsNotNull(cat.Resolve(PcCombatCatalogFactory.CaiBangDogBeatingAuraChild));
            Assert.AreEqual(30, cat.Count);
        }

        [Test]
        public void Novice_MeleeAttack_UsesPcGateRangeCostAndAction()
        {
            var svc = new CombatRuntimeService(Catalog());
            var novice = Novice();
            var enemy = Enemy(new Vector2(50, 0));
            var r = svc.Cast(novice, enemy, 53, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(0, r.manaCost);
            Assert.AreEqual(CombatActionState.Melee, r.actionState);
            Assert.AreEqual(18, r.totalFrames);
            Assert.AreEqual(1, r.childProjectileCount);

            var far = Enemy(new Vector2(76, 0));
            var rejected = svc.Cast(novice, far, 53, far.position, CombatRelation.Enemy);
            Assert.IsFalse(rejected.success);
            Assert.AreEqual(CombatCastRejectReason.OutOfRange, rejected.reason);
        }

        [Test]
        public void Novice_PhysicalAttack_RequiresFightModeAndMatchingWeaponSkill()
        {
            var svc = new CombatRuntimeService(Catalog());
            var novice = Novice();
            novice.fightMode = false;
            var r = svc.Cast(novice, Enemy(new Vector2(10,0)), 53, new Vector2(10,0), CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.NotInFightMode, r.reason);

            novice.fightMode = true;
            novice.currentWeaponSkillId = 1;
            r = svc.Cast(novice, Enemy(new Vector2(10,0)), 53, new Vector2(10,0), CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.WeaponSkillMismatch, r.reason);
        }

        [Test]
        public void CaiBang_DamageSkills_MatchLuaLevelFormulas()
        {
            var cat = Catalog();
            var throwStone = cat.Resolve(117).GetPcLevelData(20);
            Assert.AreEqual("PhysicsDamageV=88,0,248", throwStone.First(MagicAttributeKind.PhysicsDamageV).ToString());
            Assert.AreEqual("FireDamageV=215,0,215", throwStone.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=8,0,0", throwStone.First(MagicAttributeKind.SkillCostV).ToString());

            var dragon = cat.Resolve(128).GetPcLevelData(30);
            Assert.AreEqual("PhysicsDamageV=832,0,832", dragon.First(MagicAttributeKind.PhysicsDamageV).ToString());
            Assert.AreEqual("FireDamageV=900,0,900", dragon.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=70,0,0", dragon.First(MagicAttributeKind.SkillCostV).ToString());
        }

        [Test]
        public void CaiBang_ResistAndPassiveSkills_MatchLuaLevelFormulasIncludingBugs()
        {
            var cat = Catalog();
            Assert.AreEqual("AddPhysicsDamageP=215,-1,2", cat.Resolve(115).GetPcLevelData(20).First(MagicAttributeKind.AddPhysicsDamageP).ToString());
            Assert.AreEqual("DeadlyStrikeEnhanceP=25,-1,0", cat.Resolve(116).GetPcLevelData(20).First(MagicAttributeKind.DeadlyStrikeEnhanceP).ToString());
            Assert.AreEqual("PhysicsResP=34,-1,0", cat.Resolve(127).GetPcLevelData(20).First(MagicAttributeKind.PhysicsResP).ToString());
            Assert.AreEqual("ColdResP=52,25200,0", cat.Resolve(126).GetPcLevelData(20).First(MagicAttributeKind.ColdResP).ToString());
            // PC Lua 金乌映雪 returns Param2String(result,result,0) for skill_cost_v; preserve odd tuple.
            Assert.AreEqual("SkillCostV=20,20,0", cat.Resolve(126).GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString());
            // PC Lua 化险为夷 cost uses undefined result1/result2; runtime treats nil as 0 in this port evidence fixture.
            Assert.AreEqual("SkillCostV=0,0,0", cat.Resolve(129).GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString());
        }

        [Test]
        public void CaiBang_Cast_AppliesCostCooldownProjectileCountDamageAndHorseRestriction()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            var enemy = Enemy(new Vector2(300, 0));
            var r = svc.Cast(beggar, enemy, 125, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(50, r.manaCost); // PC Lua 天下无狗: fixed 50
            Assert.AreEqual(16, r.childProjectileCount);
            Assert.AreEqual(16, r.projectiles.Count);
            Assert.Less(enemy.currentLife, 1000);
            Assert.AreEqual(2, svc.NextCastTime(beggar.actorId, 125));

            var onCooldown = svc.Cast(beggar, enemy, 125, enemy.position, CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.OnCooldown, onCooldown.reason);

            svc.AdvanceTime(2);
            beggar.rideHorse = true;
            var horseBlocked = svc.Cast(beggar, enemy, 125, enemy.position, CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.HorseRestricted, horseBlocked.reason);
        }

        [Test]
        public void CaiBang_BuffsAndAura_TargetSelfOrAllyAndApplyState()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            var self = svc.Cast(beggar, beggar, 118, beggar.position, CombatRelation.Self);
            Assert.IsTrue(self.success, self.detail);
            Assert.IsTrue(beggar.states.ContainsKey(MagicAttributeKind.LightingResP));
            Assert.AreEqual(20, self.manaCost);

            svc.AdvanceTime(2);
            var aura = svc.Cast(beggar, beggar, 124, beggar.position, CombatRelation.Self);
            Assert.IsTrue(aura.success, aura.detail);
            Assert.IsTrue(beggar.states.ContainsKey(MagicAttributeKind.AddDefenseV));
            Assert.AreEqual(230, beggar.states[MagicAttributeKind.AddDefenseV].value1);
        }

        [Test]
        public void NonCaiBang_CannotCastCaiBangSkill()
        {
            var svc = new CombatRuntimeService(Catalog());
            var novice = Novice();
            novice.knownSkills.Add(117);
            novice.skillLevels[117] = 1;
            var r = svc.Cast(novice, Enemy(new Vector2(10,0)), 117, new Vector2(10,0), CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.FactionMismatch, r.reason);
        }

        [Test]
        public void SandboxManager_BootstrapsCombatRuntimeWithNoviceAndCaiBangCatalog()
        {
            var go = new GameObject("SandboxManagerCombatTest");
            try
            {
                var manager = go.AddComponent<SandboxManager>();
                manager.BootstrapCombatForTests(new AssetRegistry());
                Assert.IsNotNull(manager.CombatSkillCatalog);
                Assert.IsNotNull(manager.CombatRuntime);
                Assert.IsNotNull(manager.CombatSkillCatalog.Resolve(53));
                Assert.IsNotNull(manager.CombatSkillCatalog.Resolve(128));
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
