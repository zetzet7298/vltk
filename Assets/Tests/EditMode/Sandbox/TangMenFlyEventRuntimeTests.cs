using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("TangMen")]
    public sealed class TangMenFlyEventRuntimeTests
    {
        private static SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

          private static CombatActorState TangMen(int level, int skillId = 302) => new CombatActorState
          {
            actorId = 2,
            faction = CombatFaction.TangMen,
            level = level,
            fightMode = true,
            currentMana = 1000,
            position = Vector2.zero,
              knownSkills = { skillId },
              skillLevels = { [skillId] = level },
        };

        private static CombatActorState Enemy() => new CombatActorState
        {
            actorId = 9,
            faction = CombatFaction.None,
            level = 1,
            currentLife = 5000,
            position = new Vector2(200, 0),
        };

        [Test]
        public void BaoyuLihua_FlyEventDispatches301OncePerInterval()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
            var caster = TangMen(20);
            var target = Enemy();
            var report = runtime.Cast(caster, target, 302, target.position, CombatRelation.Enemy);
            Assert.IsTrue(report.success, report.detail);
            Assert.AreEqual(301, report.skill.flySkillId);

            var parentMissile = report.projectiles.First(p => p.skillId == report.skill.childSkillId);
            int before = report.projectiles.Count;
            int damageBefore = report.damageResults.Count;
            Assert.IsTrue(runtime.TryResolveProjectileFly(caster, target, report, parentMissile, 1, target.position));
            Assert.AreEqual(before + 8, report.projectiles.Count,
                "PC 302 FlyEvent casts skill 301, whose canonical row emits eight child missiles 126");
            Assert.AreEqual(damageBefore, report.damageResults.Count,
                "event skill 301 must not apply immediate damage before a child missile collision");
            var children = report.projectiles.Skip(before).ToList();
            Assert.IsTrue(children.All(p => p.skillId == 126));
            Assert.IsTrue(children.All(p => p.origin == target.position && p.target == target.position),
                "parent 302 ByMissle=1 launches event children at the missile event point");
            Assert.IsTrue(children.All(p => report.projectileImpactSkillIds[p.instanceId] == 301));
            Assert.IsTrue(runtime.TryResolveProjectileCollision(caster, target, report, children[0], target.position));
            Assert.Greater(report.damageResults.Count, damageBefore,
                "skill 301 damage arrives through its child missile lifecycle");
            Assert.IsFalse(runtime.TryResolveProjectileFly(caster, target, report, parentMissile, 1, target.position),
                "the same projectile and fly interval must be idempotent");
        }

        [Test]
        public void BaoyuLihua_FlyEventGateFailsClosedAtPcBreakpointLevelTen()
        {
            var catalog = Catalog();
            var runtime = new CombatRuntimeService(catalog);
            var caster = TangMen(10);
            var target = Enemy();
            var report = runtime.Cast(caster, target, 302, target.position, CombatRelation.Enemy);
            var parentMissile = report.projectiles.First(p => p.skillId == report.skill.childSkillId);

            Assert.IsFalse(PcTangMenLuaLevelService.FlyEnabled(302, 10));
            Assert.IsFalse(runtime.TryResolveProjectileFly(caster, target, report, parentMissile, 1, target.position));
            Assert.AreEqual(1, report.projectiles.Count, "level-10 gate must not spawn a fly child");
        }

        [Test]
          public void BaoyuLihua_VisualFlightEmitsRepeatedThirtyTickCallbacks()
        {
            var catalog = Catalog();
            var visual = new SkillEffectVisualService(null, catalog);
            int fired = 0;
            var fx = visual.PlaySkillCast(catalog.Resolve(302), Vector2.zero, new Vector2(2000, 0), 20);
            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.pcFlyEventEnabled, "302 must load the canonical Lua fly gate");
            Assert.Greater(fx.pcFlyEventIntervalTicks, 0, "302 must load a positive Lua fly interval");
            Assert.AreEqual(1, fx.missileFlyEventOrdinals.Length,
                "stationary 302 visual must keep one logical FlyEvent emitter");
            fx.onMissileFlyEvent = (_, _, _) => fired++;

            for (int i = 0; i < 70; i++)
                visual.Update(1f / 18f);

              Assert.AreEqual(36, fx.pcMissileLifeTicks,
                  "Lua L20 lifetime 36 replaces raw Missles.txt missile 96 lifetime 90");
              Assert.AreEqual(1, fired,
                  "KMissle expires at tick 36 before a second interval-30 FlyEvent");
          }

          [Test]
          public void TangMenLuaTemporalOverridesReplaceRawMissleDefaults()
          {
              var catalog = Catalog();
              var visual = new SkillEffectVisualService(null, catalog);

              Assert.AreEqual(36, PcTangMenLuaLevelService.MissileLifetime(302, 20));
              Assert.AreEqual(18, PcTangMenLuaLevelService.MissileLifetime(1070, 1));
              Assert.AreEqual(28, PcTangMenLuaLevelService.MissileSpeed(58, 20));
              Assert.AreEqual(28, PcTangMenLuaLevelService.MissileSpeed(339, 1));
              Assert.AreEqual(32, PcTangMenLuaLevelService.MissileSpeed(339, 20));
              Assert.AreEqual(32, PcTangMenLuaLevelService.MissileSpeed(1069, 1));
              Assert.AreEqual(28, PcTangMenLuaLevelService.MissileSpeed(1071, 1));

              var baoyu = visual.PlaySkillCast(catalog.Resolve(302), Vector2.zero, new Vector2(2000, 0), 20);
              var nutang = visual.PlaySkillCast(catalog.Resolve(1070), Vector2.zero, new Vector2(2000, 0), 1);
              var tianluo = visual.PlaySkillCast(catalog.Resolve(58), Vector2.zero, new Vector2(2000, 0), 20);
              var shehun = visual.PlaySkillCast(catalog.Resolve(339), Vector2.zero, new Vector2(2000, 0), 1);
              var feidao = visual.PlaySkillCast(catalog.Resolve(1069), Vector2.zero, new Vector2(2000, 0), 1);
              var biaotang = visual.PlaySkillCast(catalog.Resolve(1071), Vector2.zero, new Vector2(2000, 0), 1);

              Assert.AreEqual(36, baoyu.pcMissileLifeTicks, "raw missile 96 lifetime is 90");
              Assert.AreEqual(18, nutang.pcMissileLifeTicks, "raw missile 332 lifetime is 36");
              Assert.AreEqual(28, tianluo.pcMissileSpeedPerTick, "raw missile 67 speed is 16");
              Assert.AreEqual(28, shehun.pcMissileSpeedPerTick,
                  "Lua L1 missle_speed_v overrides missile 149 speed");
              Assert.AreEqual(32, feidao.pcMissileSpeedPerTick, "raw missile 331 speed is 40");
              Assert.AreEqual(28, biaotang.pcMissileSpeedPerTick, "raw missile 333 speed is 32");
          }

          [Test]
          public void Nutang150_LifetimeTerminalTickDoesNotDispatchFlyEvent()
          {
              var catalog = Catalog();
              var visual = new SkillEffectVisualService(null, catalog);
              var fx = visual.PlaySkillCast(catalog.Resolve(1070), Vector2.zero, new Vector2(2000, 0), 20);
              int fired = 0;
              fx.onMissileFlyEvent = (_, _, _) => fired++;

              for (int i = 0; i < 6; i++) visual.Update(1f / 18f);
              Assert.AreEqual(SkillEffectPhase.Impact, fx.phase);
              for (int i = 0; i < 36; i++) visual.Update(1f / 18f);

              Assert.AreEqual(36, fx.pcMissileLifeTicks);
              Assert.AreEqual(1, fired,
                  "KMissle checks lifetime before OnFly, so only tick 18 fires; terminal tick 36 does not");
              Assert.AreEqual(SkillEffectPhase.Finished, fx.phase);
          }

          [Test]
          public void Nutang150_FlyEventDispatches1098ThroughFullRuntime()
          {
              var catalog = Catalog();
              var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var caster = TangMen(20, 1070);
              var target = Enemy();
              var report = runtime.Cast(caster, target, 1070, target.position, CombatRelation.Enemy);
              Assert.IsTrue(report.success, report.detail);
              Assert.AreEqual(1098, report.skill.flySkillId);

            var parentMissile = report.projectiles.First(p => p.skillId == report.skill.childSkillId);
            int damageBefore = report.damageResults.Count;
            int projectilesBefore = report.projectiles.Count;
            Assert.IsTrue(runtime.TryResolveProjectileFly(caster, target, report, parentMissile, 1, target.position));
            Assert.AreEqual(damageBefore, report.damageResults.Count,
                "1098 is a missile event skill; it must not damage before a child collision");
            Assert.AreEqual(projectilesBefore + 8, report.projectiles.Count);
            var children = report.projectiles.Skip(projectilesBefore).ToList();
            Assert.IsTrue(children.All(p => p.skillId == 360));
            Assert.IsTrue(children.All(p => report.projectileImpactSkillIds[p.instanceId] == 1098));
            Assert.IsTrue(children.All(p => p.origin == target.position));
          }

          [Test]
          public void Feidaotang150_CollideGateUsesTangMenLuaBreakpointAnd1097()
          {
              var catalog = Catalog();
              var target = Enemy();
              Assert.AreEqual(0, PcTangMenLuaLevelService.CollideEnabled(1069, 10));
              Assert.AreEqual(1, PcTangMenLuaLevelService.CollideEnabled(1069, 11));
              Assert.AreEqual(1097, PcTangMenLuaLevelService.CollideSkillId(1069, 11));

              var level10 = TangMen(10, 1069);
              var runtime10 = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var report10 = runtime10.Cast(level10, target, 1069, target.position, CombatRelation.Enemy);
              var missile10 = report10.projectiles.First(p => p.skillId == report10.skill.childSkillId);
              int damage10 = report10.damageResults.Count;
              Assert.IsTrue(runtime10.TryResolveProjectileCollision(level10, target, report10, missile10, target.position));
              Assert.AreEqual(damage10 + 1, report10.damageResults.Count,
                  "level 10 duplicate breakpoint keeps the TangMen collide child disabled");
              CollectionAssert.DoesNotContain(report10.resolvedLifecycleSkillIds, 1097);

              target = Enemy();
              var level11 = TangMen(11, 1069);
              var runtime11 = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var report11 = runtime11.Cast(level11, target, 1069, target.position, CombatRelation.Enemy);
              var missile11 = report11.projectiles.First(p => p.skillId == report11.skill.childSkillId);
              int damage11 = report11.damageResults.Count;
              Assert.IsTrue(runtime11.TryResolveProjectileCollision(level11, target, report11, missile11, target.position));
              Assert.GreaterOrEqual(report11.damageResults.Count, damage11 + 1,
                  "the 1069 parent collision itself must apply once");
                CollectionAssert.Contains(report11.resolvedLifecycleSkillIds, 1097,
                    "level 11 must dispatch the source-backed 1069→1097 collide child");
                var collideChild = report11.projectiles.Single(p => p.skillId == 359);
                Assert.AreEqual(target.position, collideChild.origin);
                Assert.AreEqual(1097, report11.projectileImpactSkillIds[collideChild.instanceId]);
            }

          [Test]
          public void ShehunYueying_339_CollideGateBlocks340ThroughL10ThenDispatchesOnceAtL11()
          {
              var catalog = Catalog();
              Assert.AreEqual(0, PcTangMenLuaLevelService.CollideEnabled(339, 10));
              Assert.AreEqual(1, PcTangMenLuaLevelService.CollideEnabled(339, 11));
              Assert.AreEqual(340, PcTangMenLuaLevelService.CollideSkillId(339, 11));

              var level10Target = Enemy();
              var level10Caster = TangMen(10, 339);
              var runtime10 = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var report10 = runtime10.Cast(level10Caster, level10Target, 339, level10Target.position, CombatRelation.Enemy);
              var missile10 = report10.projectiles.First(p => p.skillId == report10.skill.childSkillId);
              Assert.IsTrue(runtime10.TryResolveProjectileCollision(level10Caster, level10Target, report10, missile10, level10Target.position));
              CollectionAssert.DoesNotContain(report10.resolvedLifecycleSkillIds, 340,
                  "PC skill_collideevent stays disabled through level 10");

              var level11Target = Enemy();
              var level11Caster = TangMen(11, 339);
              var runtime11 = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var report11 = runtime11.Cast(level11Caster, level11Target, 339, level11Target.position, CombatRelation.Enemy);
              var missile11 = report11.projectiles.First(p => p.skillId == report11.skill.childSkillId);
              Assert.IsTrue(runtime11.TryResolveProjectileCollision(level11Caster, level11Target, report11, missile11, level11Target.position));
              CollectionAssert.Contains(report11.resolvedLifecycleSkillIds, 340,
                  "PC level 11 enables canonical 339→340 collide child");
              Assert.IsFalse(runtime11.TryResolveProjectileCollision(level11Caster, level11Target, report11, missile11, level11Target.position),
                  "same projectile collision must resolve once");
              Assert.AreEqual(1, report11.resolvedLifecycleSkillIds.Count(id => id == 340));
          }

          [Test]
          public void PiliDan_LiveVisualVanishThenCollisionSpawns1113And352Once()
          {
              var catalog = Catalog();
              var visual = new SkillEffectVisualService(null, catalog);
              var collisions = new List<int>();
              visual.OnMissileCollided = (fx, _, _) => collisions.Add(fx.skillId);
              visual.PlaySkillCast(catalog.Resolve(1110), Vector2.zero, new Vector2(200, 0), 20);

              for (int i = 0; i < 100 && !visual.GetActiveEffects().Any(fx => fx.skillId == 1113); i++)
                  visual.Update(1f / 18f);

              var vanished = visual.GetActiveEffects().Single(fx => fx.skillId == 1113);
              Assert.IsFalse(vanished.HasMissile, "child missile 161 is PC MoveKind=0 stationary");
              Assert.IsFalse(string.IsNullOrEmpty(vanished.pcImpactSpriteKey), "1113 must map child missile 161");

              for (int i = 0; i < 1079; i++) visual.Update(1f / 18f);
              Assert.IsFalse(visual.GetActiveEffects().Any(fx => fx.skillId == 352),
                  "stationary 161 must not collide before its terminal frame");
              visual.Update(1f / 18f);

              var collided = visual.GetActiveEffects().Single(fx => fx.skillId == 352);
              Assert.IsFalse(collided.HasMissile, "ByMissle=0 does not suppress stationary child visual 162");
              Assert.IsFalse(string.IsNullOrEmpty(collided.pcImpactSpriteKey), "352 must map child missile 162");
              Assert.AreEqual(1, collisions.Count(id => id == 1113), "1113 terminal collision dispatches 352 once");

              for (int i = 0; i < 10; i++) visual.Update(1f / 18f);
              Assert.AreEqual(1, collisions.Count(id => id == 1113), "per-missile collision stays idempotent");
          }

          [Test]
          public void LifecycleVisualCycle_IsBoundedByAncestorGuard()
          {
              var catalog = new SkillCatalog();
              catalog.Register(new SkillDefinition
              {
                  skillId = 9001, nameNormalized = "CycleRoot", missileForm = SkillMissileForm.Single,
                  childSkillId = 374, vanishSkillId = 9002,
              });
              catalog.Register(new SkillDefinition
              {
                  skillId = 9002, nameNormalized = "CycleChild", missileForm = SkillMissileForm.Single,
                  childSkillId = 374, collideSkillId = 9001,
              });
              var visual = new SkillEffectVisualService(null, catalog);
              var collisions = new List<int>();
              visual.OnMissileCollided = (fx, _, _) => collisions.Add(fx.skillId);
              visual.PlaySkillCast(catalog.Resolve(9001), Vector2.zero, Vector2.zero, 1);

              for (int i = 0; i < 40; i++) visual.Update(1f / 18f);

              Assert.AreEqual(1, collisions.Count(id => id == 9001), "cycle must not recreate root");
              Assert.AreEqual(1, collisions.Count(id => id == 9002));
          }

          [Test]
            public void PiliDan_VanishEventDispatches1113Once()
          {
              var catalog = Catalog();
              var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService { RollPercent = _ => true });
              var caster = TangMen(20, 1110);
              var target = Enemy();
              var report = runtime.Cast(caster, target, 1110, target.position, CombatRelation.Enemy);
              Assert.IsTrue(report.success, report.detail);
              Assert.AreEqual(1113, report.skill.vanishSkillId);

                var parentMissile = report.projectiles.First(p => p.skillId == report.skill.childSkillId);
                int damageBefore = report.damageResults.Count;
                int projectilesBefore = report.projectiles.Count;
                int callbacks = 0;
              var visual = new SkillEffectVisualService(null, catalog);
              var fx = visual.PlaySkillCast(report.skill, caster.position, target.position, report.skillLevel);
              fx.onMissileVanishEvent = (_, missileIndex, eventPoint) =>
              {
                  callbacks++;
                  Assert.AreEqual(0, missileIndex);
                  Assert.IsTrue(runtime.TryResolveProjectileVanish(caster, target, report, parentMissile, eventPoint));
              };
              for (int i = 0; i < 100 && visual.GetActiveEffects().Count > 0; i++)
                  visual.Update(1f / 18f);

                Assert.AreEqual(1, callbacks, "visual lifecycle must dispatch exactly one VanishEvent per missile");
                Assert.AreEqual(damageBefore, report.damageResults.Count,
                    "1113 has ByMissle=1, so the vanish event spawns its child instead of applying direct damage");
                Assert.Greater(report.projectiles.Count, projectilesBefore);
                  Assert.IsTrue(report.projectiles.Any(p => p.skillId == 161),
                      "1113 must spawn canonical child missile 161");
                  var child161 = report.projectiles.Single(p => p.skillId == 161);
                  Assert.AreEqual(1113, report.projectileImpactSkillIds[child161.instanceId]);
                  int damageBeforeNestedCollision = report.damageResults.Count;
                  int projectilesBeforeNestedCollision = report.projectiles.Count;
                  Assert.IsTrue(runtime.TryResolveProjectileCollision(
                      caster, target, report, child161, target.position));
                  Assert.Greater(report.damageResults.Count, damageBeforeNestedCollision,
                      "1113 damage resolves when child missile 161 reaches its collision lifecycle");
                  var children162 = report.projectiles.Skip(projectilesBeforeNestedCollision).ToList();
                  Assert.AreEqual(4, children162.Count, "1113 collide event casts 352, which emits four missiles 162");
                  Assert.IsTrue(children162.All(p => p.skillId == 162));
                  Assert.IsTrue(children162.All(p => report.projectileImpactSkillIds[p.instanceId] == 352));
                  Assert.IsTrue(children162.All(p => p.origin == target.position));
                  int damageBefore352 = report.damageResults.Count;
                  Assert.IsTrue(runtime.TryResolveProjectileCollision(
                      caster, target, report, children162[0], target.position));
                  Assert.Greater(report.damageResults.Count, damageBefore352,
                      "352 damage resolves through child missile 162, never inline at the 1113 event");
                  int projectilesAfterVanish = report.projectiles.Count;
                Assert.IsFalse(runtime.TryResolveProjectileVanish(caster, target, report, parentMissile, target.position),
                    "VanishEvent must be idempotent per projectile");
                  Assert.AreEqual(projectilesAfterVanish, report.projectiles.Count);
              }

            [Test]
            public void MissileEvent_ParentByMissileFalse_UsesCasterLauncherAndDefersDamage()
            {
                var catalog = new SkillCatalog();
                var root = new SkillDefinition
                {
                    skillId = 9001,
                    nameNormalized = "Root",
                    skillStyle = PcSkillStyle.Missiles,
                    missileForm = SkillMissileForm.Single,
                    childSkillId = 9002,
                    childSkillNum = 1,
                    collideSkillId = 9003,
                    collideSkillLevel = 1,
                    targetEnemy = true,
                    byMissile = false,
                };
                var rootLevel = new SkillLevelData { level = 1 };
                rootLevel.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, 10, 0, 10));
                root.pcLevelData.Add(rootLevel);
                catalog.Register(root);

                var eventSkill = new SkillDefinition
                {
                    skillId = 9003,
                    nameNormalized = "Event",
                    skillStyle = PcSkillStyle.Missiles,
                    missileForm = SkillMissileForm.Single,
                    childSkillId = 9004,
                    childSkillNum = 1,
                };
                var eventLevel = new SkillLevelData { level = 1 };
                eventLevel.damage.Add(new SkillMagicAttribute(MagicAttributeKind.PhysicsDamageV, 20, 0, 20));
                eventSkill.pcLevelData.Add(eventLevel);
                catalog.Register(eventSkill);

                var caster = TangMen(1, 9001);
                caster.position = new Vector2(12f, 34f);
                var target = Enemy();
                var runtime = new CombatRuntimeService(
                    catalog, damage: new DamageFormulaService { RollPercent = _ => true });
                var report = runtime.Cast(caster, target, 9001, target.position, CombatRelation.Enemy);
                Assert.AreEqual(0, report.damageResults.Count,
                    "SkillStyle=Missiles defers damage even when the canonical ByMissle field is zero");

                var rootMissile = report.projectiles.Single(p => p.skillId == 9002);
                Assert.IsTrue(runtime.TryResolveProjectileCollision(
                    caster, target, report, rootMissile, new Vector2(80f, 90f)));
                Assert.AreEqual(1, report.damageResults.Count, "root damage resolves at root missile collision");

                var eventMissile = report.projectiles.Single(p => p.skillId == 9004);
                Assert.AreEqual(caster.position, eventMissile.origin,
                    "PC parent ByMissle=0 selects the NPC/caster launcher");
                Assert.AreEqual(caster.position, eventMissile.target);
                Assert.AreEqual(9003, report.projectileImpactSkillIds[eventMissile.instanceId]);
                Assert.IsTrue(runtime.TryResolveProjectileCollision(
                    caster, target, report, eventMissile, caster.position));
                Assert.AreEqual(2, report.damageResults.Count, "event damage resolves at its child missile collision");
            }
      }
}
