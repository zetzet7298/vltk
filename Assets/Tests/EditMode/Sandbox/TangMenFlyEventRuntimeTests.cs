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
            Assert.AreEqual(before, report.projectiles.Count,
                "301 is SkillStyle=Missiles but ByMissle=0 in the canonical row, so FlyEvent applies direct damage");
            Assert.Greater(report.damageResults.Count, damageBefore, "FlyEvent must apply sourced 301 damage");
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
              Assert.AreEqual(32, PcTangMenLuaLevelService.MissileSpeed(1069, 1));
              Assert.AreEqual(28, PcTangMenLuaLevelService.MissileSpeed(1071, 1));

              var baoyu = visual.PlaySkillCast(catalog.Resolve(302), Vector2.zero, new Vector2(2000, 0), 20);
              var nutang = visual.PlaySkillCast(catalog.Resolve(1070), Vector2.zero, new Vector2(2000, 0), 1);
              var tianluo = visual.PlaySkillCast(catalog.Resolve(58), Vector2.zero, new Vector2(2000, 0), 20);
              var feidao = visual.PlaySkillCast(catalog.Resolve(1069), Vector2.zero, new Vector2(2000, 0), 1);
              var biaotang = visual.PlaySkillCast(catalog.Resolve(1071), Vector2.zero, new Vector2(2000, 0), 1);

              Assert.AreEqual(36, baoyu.pcMissileLifeTicks, "raw missile 96 lifetime is 90");
              Assert.AreEqual(18, nutang.pcMissileLifeTicks, "raw missile 332 lifetime is 36");
              Assert.AreEqual(28, tianluo.pcMissileSpeedPerTick, "raw missile 67 speed is 16");
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
              Assert.IsTrue(runtime.TryResolveProjectileFly(caster, target, report, parentMissile, 1, target.position));
              Assert.Greater(report.damageResults.Count, damageBefore,
                  "canonical 1098 is direct-damage ByMissle=0 and must execute through the runtime event path");
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
              Assert.Greater(report.damageResults.Count, damageBefore);
              Assert.IsFalse(runtime.TryResolveProjectileVanish(caster, target, report, parentMissile, target.position),
                  "VanishEvent must be idempotent per projectile");
          }
      }
}
