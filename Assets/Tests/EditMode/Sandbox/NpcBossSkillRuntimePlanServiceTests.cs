using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class NpcBossSkillRuntimePlanServiceTests
    {
        private static string NpcSkillDir => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcSkill");

        private static NpcSkillService CreateService()
        {
            var registry = PcNpcSkillParser.BuildRegistry(NpcSkillDir);
            return new NpcSkillService(registry);
        }

        [Test]
        public void BuildCastPlan_RepresentativeRowsPreservePcCastFieldsAndMissingScriptGuards()
        {
            var service = CreateService();

            var daTau = service.BuildCastPlan(753);
            Assert.IsTrue(daTau.canCast);
            Assert.AreEqual(753, daTau.skillId);
            Assert.AreEqual(202, daTau.childSkillId);
            Assert.AreEqual(-1, daTau.childSkillLevel);
            Assert.AreEqual(1, daTau.childSkillNum);
            Assert.AreEqual(440, daTau.attackRadius);
            Assert.AreEqual(2, daTau.cooldownTicks);
            Assert.IsTrue(daTau.targetOnly);
            Assert.IsTrue(daTau.targetEnemy);
            Assert.AreEqual("\\script\\skill\\npc\\randomtask_npc.lua", daTau.levelSetScript);
            Assert.IsFalse(daTau.missingScriptGuard);

            var chunNiu = service.BuildCastPlan(933);
            Assert.IsTrue(chunNiu.canCast);
            Assert.AreEqual(291, chunNiu.childSkillId);
            Assert.AreEqual(8, chunNiu.childSkillNum);
            Assert.AreEqual(1080, chunNiu.cooldownTicks);
            Assert.AreEqual(400, chunNiu.attackRadius);

            var tongCastle = service.BuildCastPlan(1208);
            Assert.IsTrue(tongCastle.canCast);
            Assert.AreEqual(392, tongCastle.childSkillId);
            Assert.AreEqual(0, tongCastle.childSkillNum);
            Assert.AreEqual(9, tongCastle.cooldownTicks);

            var bigGoldBoss = service.BuildCastPlan(1584);
            Assert.IsTrue(bigGoldBoss.canCast);
            Assert.AreEqual(432, bigGoldBoss.childSkillId);
            Assert.AreEqual(64, bigGoldBoss.childSkillNum);
            Assert.AreEqual(700, bigGoldBoss.attackRadius);
            Assert.IsTrue(bigGoldBoss.missingScriptGuard);
            Assert.AreEqual("\\script\\skill\\biggoldboss.lua", bigGoldBoss.levelSetScript);

            var liBaiBoss = service.BuildCastPlan(1604);
            Assert.IsTrue(liBaiBoss.canCast);
            Assert.AreEqual(10, liBaiBoss.childSkillId);
            Assert.AreEqual(1, liBaiBoss.childSkillNum);
            Assert.AreEqual(72, liBaiBoss.attackRadius);
            Assert.IsTrue(liBaiBoss.missingScriptGuard);
            Assert.AreEqual("\\script\\skill\\special\\boss_libaiskill.lua", liBaiBoss.levelSetScript);
        }

        [Test]
        public void EnemyAiDecision_ChoosesPcListedSkillByRangeAndCooldown()
        {
            var service = CreateService();
            var decision = EnemyAiService.Tick(new AiContext
            {
                position = Vector2.zero,
                playerPosition = new Vector2(390f, 0f),
                distanceToPlayer = 390f,
                visionRadius = 800f,
                activeRadius = 800f,
                aiMode = (int)PcAiMode.Aggressive,
                deltaTime = 1f / 60f,
                npcSkillService = service,
                npcSkillIds = new[] { 753, 933 },
                currentTime = 10,
            });

            Assert.AreEqual(AiState.Attack, decision.state);
            Assert.IsTrue(decision.shouldAttack);
            Assert.AreEqual(753, decision.skillId);
            Assert.AreEqual(202, decision.childSkillId);
            Assert.AreEqual(440, decision.attackRange);
            Assert.AreEqual(2, decision.cooldownTicks);
        }

        [Test]
        public void CombatRuntime_CastsNpcPlanWithoutPlayerKnownSkills()
        {
            var service = CreateService();
            var catalog = new SkillCatalog();
            var plan = service.BuildCastPlan(933);
            catalog.Register(plan.ToSkillDefinition());
            var runtime = new CombatRuntimeService(catalog);
            runtime.AdvanceTime(10);
            var npc = new CombatActorState
            {
                actorId = 91,
                faction = CombatFaction.None,
                position = Vector2.zero,
                currentMana = 0,
                maxMana = 0,
            };
            var target = new CombatActorState
            {
                actorId = 7,
                faction = CombatFaction.CaiBang,
                position = new Vector2(120f, 0f),
            };

            var playerPath = runtime.Cast(npc, target, 933, target.position, CombatRelation.Enemy);
            Assert.IsFalse(playerPath.success);
            Assert.AreEqual(CombatCastRejectReason.SkillNotKnown, playerPath.reason);

            var npcPath = runtime.CastNpcPlan(npc, target, plan, target.position, CombatRelation.Enemy);
            Assert.IsTrue(npcPath.success, npcPath.detail);
            Assert.AreEqual(933, npcPath.skill.skillId);
            Assert.AreEqual(20, npcPath.skillLevel);
            Assert.AreEqual(8, npcPath.childProjectileCount);
            Assert.AreEqual(1090, runtime.NextCastTime(npc.actorId, 933));
        }
    }
}
