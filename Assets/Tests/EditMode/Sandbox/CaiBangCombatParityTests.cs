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
            Assert.IsNotNull(cat.Resolve(714), "missing Hỗn Thiên Khí Công 120");
            Assert.IsNotNull(cat.Resolve(720), "missing Hỗn Thiên Khí Công Quyết Chí");
            Assert.AreEqual(34, cat.Count, "33 PC + Novice skills + NguDieuCanKhon (1072) CollideEvent sub-skill");
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
            Assert.AreEqual("PhysicsEnhanceP=55,0,0", throwStone.First(MagicAttributeKind.PhysicsEnhanceP).ToString());
            Assert.AreEqual("FireDamageV=100,0,150", throwStone.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=10,0,0", throwStone.First(MagicAttributeKind.SkillCostV).ToString());

            var dragon = cat.Resolve(128).GetPcLevelData(20);
            Assert.AreEqual("FireDamageV=536,0,536", dragon.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=50,0,0", dragon.First(MagicAttributeKind.SkillCostV).ToString());
        }

        [Test]
        public void CaiBang_ResistAndPassiveSkills_MatchLuaLevelFormulasIncludingBugs()
        {
            var cat = Catalog();
            Assert.AreEqual("AddPhysicsDamageP=150,-1,2", cat.Resolve(115).GetPcLevelData(20).First(MagicAttributeKind.AddPhysicsDamageP).ToString());
            Assert.AreEqual("DeadlyStrikeEnhanceP=25,-1,0", cat.Resolve(115).GetPcLevelData(20).First(MagicAttributeKind.DeadlyStrikeEnhanceP).ToString());
            Assert.AreEqual("AddFireDamageV=275,-1,9", cat.Resolve(116).GetPcLevelData(20).First(MagicAttributeKind.AddFireDamageV).ToString());
            Assert.AreEqual("FastWalkRunP=66,3240,0", cat.Resolve(127).GetPcLevelData(20).First(MagicAttributeKind.FastWalkRunP).ToString());
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
            Assert.AreEqual(48, r.manaCost); // PC Lua 天下无狗: fixed 50 -> now 48
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
            var aura = svc.Cast(beggar, beggar, 129, beggar.position, CombatRelation.Self);
            Assert.IsTrue(aura.success, aura.detail);
            Assert.IsTrue(beggar.states.ContainsKey(MagicAttributeKind.AddDefenseV));
            Assert.AreEqual(800, beggar.states[MagicAttributeKind.AddDefenseV].value1);
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

        [Test]
        public void CaiBang_AttackRadius_ScalesPerLevelFromPcGaibangLua()
        {
            // PC gaibang.lua short-range skills (117, 119, 122): skill_attackradius={{{1,320},{20,384}}}.
            // PC gaibang.lua long-range skills (125, 128, 357, 359, 1073, 1074): skill_attackradius={{{1,448},{20,512}}}.
            // 117/119/122 use PcCaiBangSkillTuning; 128/357/359/1073/1074 use KangLong/ModTuning.
            int[] shortRange = { 117, 119, 122 };
            int[] longRange  = { 125, 128, 357, 359, 1073, 1074, 1539 };

            foreach (int id in shortRange)
            {
                Assert.AreEqual(320, PcCaiBangSkillTuning.AtLevel(id, 1).attackRadius, $"L1 radius for {id}");
                Assert.AreEqual(384, PcCaiBangSkillTuning.AtLevel(id, 20).attackRadius, $"L20 radius for {id}");
                int mid = PcCaiBangSkillTuning.AtLevel(id, 10).attackRadius;
                Assert.GreaterOrEqual(mid, 320, $"L10 radius for {id} should be >= L1");
                Assert.LessOrEqual(mid, 384, $"L10 radius for {id} should be <= L20");
            }
            foreach (int id in longRange)
            {
                // 128 uses KangLong; 357/359/1073/1074 use ModTuning; 125/1539 use PcCaiBangSkillTuning.
                if (id == 128)
                {
                    Assert.AreEqual(448, PcKangLongYouHuiTuning.AtLevel(1).attackRadius, "128 L1");
                    Assert.AreEqual(512, PcKangLongYouHuiTuning.AtLevel(20).attackRadius, "128 L20");
                }
                else if (id == 357 || id == 359 || id == 1073 || id == 1074)
                {
                    Assert.AreEqual(448, PcCaiBangModTuning.AtLevel(id, 1).attackRadius, $"{id} L1");
                    Assert.AreEqual(512, PcCaiBangModTuning.AtLevel(id, 20).attackRadius, $"{id} L20");
                }
                else
                {
                    Assert.AreEqual(448, PcCaiBangSkillTuning.AtLevel(id, 1).attackRadius, $"{id} L1");
                    Assert.AreEqual(512, PcCaiBangSkillTuning.AtLevel(id, 20).attackRadius, $"{id} L20");
                }
            }
        }

        [Test]
        public void CaiBang_117_CastAtR20_ReachesPcL20Radius()
        {
            // PC L20 attackRadius=384. Cast at distance 380 should succeed; at 400 should fail.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            var near = Enemy(new Vector2(380, 0));
            var r = svc.Cast(beggar, near, 117, near.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);

            svc.AdvanceTime(20);
            var far = Enemy(new Vector2(400, 0));
            var rejected = svc.Cast(beggar, far, 117, far.position, CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.OutOfRange, rejected.reason);
        }

        [Test]
        public void CaiBang_117_CastAtLevel1_ReachesPcL1Radius()
        {
            // PC L1 attackRadius=320. At L1, distance 300 should succeed; 350 should fail.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.skillLevels[117] = 1;
            var near = Enemy(new Vector2(300, 0));
            var r = svc.Cast(beggar, near, 117, near.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);

            svc.AdvanceTime(20);
            var far = Enemy(new Vector2(350, 0));
            var rejected = svc.Cast(beggar, far, 117, far.position, CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.OutOfRange, rejected.reason);
        }

        [Test]
        public void CaiBang_122_MaxFireDamageAtL20_Is215()
        {
            // PC gaibang.lua jianren_shenshou firedamage_v[3]={{1,15},{20,215}}.
            // Unity pre-fix had 120; corrected to 215.
            var cat = Catalog();
            var data = cat.Resolve(122).GetPcLevelData(20);
            Assert.AreEqual("FireDamageV=75,0,215", data.First(MagicAttributeKind.FireDamageV).ToString());
        }

        [Test]
        public void CaiBang_1073_CostScalesFrom12To78()
        {
            // PC gaibang.lua zhanggaibang150 skill_cost_v={{{1,12},{20,78}}}.
            // Unity pre-fix had L1=20, L20=50; corrected to L1=12, L20=78.
            var cat = Catalog();
            Assert.AreEqual("SkillCostV=12,0,0", cat.Resolve(1073).GetPcLevelData(1).First(MagicAttributeKind.SkillCostV).ToString());
            Assert.AreEqual("SkillCostV=78,0,0", cat.Resolve(1073).GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString());
        }

        [Test]
        public void CaiBang_PhiLongAtLevel11_TriggersLongChienUYuye()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(357);
            beggar.skillLevels[357] = 11;
            var enemy = Enemy(new Vector2(200, 0));
            
            var r = svc.Cast(beggar, enemy, 357, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            
            Assert.AreEqual(2, r.damageResults.Count, "Should apply damage twice (both 357 and 389)");
            Assert.AreEqual(2, r.projectiles.Count, "Should spawn 2 projectiles (1 main missile + 1 stationary child)");
            
            var stationaryProj = r.projectiles.FirstOrDefault(p => p.skillId == 195);
            Assert.IsNotNull(stationaryProj, "Should spawn stationary projectile ID 195");
            Assert.AreEqual(0f, stationaryProj.speed, "Stationary projectile speed should be 0");
            Assert.AreEqual(15f / 18f, stationaryProj.duration, "Stationary projectile duration should be 15 ticks");
            Assert.AreEqual(enemy.position, stationaryProj.position, "Stationary projectile should start at target position");
        }

        // === Phase 2 + 3 — Comprehensive PC parity tests for damage, radius, cost, count ===

        [Test]
        public void CaiBang_117_VisualServiceUsesPcMissile44SpeedAndLife()
        {
            // PC PcMissles.txt missile 44 (Đầu Thạch Vấn Lộ): Speed=14, LifeTime=40, MoveKind=7.
            // Runtime's ProjectileService uses DefaultMissileSpeed=12 (legacy placeholder),
            // so we verify the visual service (which drives actual visual flight) has the PC values.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(117);
            beggar.skillLevels[117] = 20;
            var enemy = Enemy(new Vector2(300, 0));
            var fx = visual.PlaySkillCast(cat.Resolve(117), beggar.position, enemy.position, 20);
            Assert.IsNotNull(fx, "117 visual should be configured");
            Assert.AreEqual(14, fx.pcMissileSpeedPerTick, "PC missile 44 Speed=14 ticks/sec");
            Assert.AreEqual(40, fx.pcMissileLifeTicks, "PC missile 44 LifeTime=40");
        }

        [Test]
        public void CaiBang_122_FireDamageMaxesAtPc215_AtLevel20()
        {
            // PC gaibang.lua::jianren_shenshou (122) firedamage_v[3]={{1,15},{20,215}}.
            // rolledBase is the damage value before defender mitigation (armor/resist).
            // The actual roll is 1..215; sum across multiple damageResults stays in that range.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(122);
            beggar.skillLevels[122] = 20;
            var enemy = Enemy(new Vector2(200, 0));
            var r = svc.Cast(beggar, enemy, 122, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            var rolled = r.damageResults.Sum(d => d.rolledBase);
            Assert.That(rolled, Is.GreaterThan(50), $"L20 fire rolled base should be substantial, got {rolled}");
            Assert.That(rolled, Is.LessThanOrEqualTo(220), $"L20 fire rolled base should not exceed PC max 215+var, got {rolled}");
        }

        [Test]
        public void CaiBang_128_VisualServiceUsesGaibangLuaMissileSpeed()
        {
            // PC gaibang.lua::kanglong_youhui (128) missle_speed_v={{1,28},{20,32}}.
            // The engine speed (missles.txt missile 48 Speed=10) is the engine ticks/sec,
            // while gaibang.lua gives the visual missile-speed attribute. We use the latter
            // so the dragon looks correct in 2D.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(128);
            beggar.skillLevels[128] = 20;
            var enemy = Enemy(new Vector2(400, 0));
            var fx = visual.PlaySkillCast(cat.Resolve(128), beggar.position, enemy.position, 20);
            Assert.IsNotNull(fx);
            Assert.AreEqual(32, fx.pcMissileSpeedPerTick, "PC gaibang.lua L20 missile speed = 32");
            Assert.AreEqual(16, fx.pcMissileLifeTicks, "PC missile 48 LifeTime=16");
        }

        [Test]
        public void CaiBang_359_VisualServiceUsesPcMissile168HomingSpeed()
        {
            // PC PcMissles.txt missile 168 (Thiên Hạ Vô Cẩu): Speed=24, LifeTime=32, MoveKind=5.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(359);
            beggar.skillLevels[359] = 20;
            var enemy = Enemy(new Vector2(300, 0));
            var fx = visual.PlaySkillCast(cat.Resolve(359), beggar.position, enemy.position, 20);
            Assert.IsNotNull(fx);
            Assert.AreEqual(24, fx.pcMissileSpeedPerTick, "PC missile 168 Speed=24");
            Assert.AreEqual(32, fx.pcMissileLifeTicks, "PC missile 168 LifeTime=32");
            Assert.AreEqual(3, fx.missileCount, "L20 359 spawns 3 homing missiles");
        }

        [Test]
        public void CaiBang_1073_CollideEvent1072_RegisteredInCatalog()
        {
            // PC gaibang.lua::zhanggaibang150 (1073) CollideEvent[3]={{1,1072},{20,1072}}.
            // Visual service SpawnCollideSubEffect spawns 1072 effect at the 335 missile impact.
            var cat = Catalog();
            var skill1072 = cat.Resolve(1072);
            Assert.IsNotNull(skill1072, "Catalog should have 1072 (NguDieuCanKhon) for 1073 CollideEvent");
            Assert.AreEqual(334, skill1072.childSkillId, "1072 child missile = 334");
            // 1072 has form=7 (aura/stationary) with form None in our model — no missile form.
            Assert.IsTrue(skill1072.HasMissile == false, "1072 stationary, not a flying missile");
        }

        [Test]
        public void CaiBang_117_MoveKind7_HasLongerFlightTime_ThanStraightSkills()
        {
            // PC missile 44 MoveKind=7 LifeTime=40 vs missile 45 LifeTime=16.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(117);
            beggar.knownSkills.Add(119);
            beggar.skillLevels[117] = 20;
            beggar.skillLevels[119] = 20;
            var enemy = Enemy(new Vector2(300, 0));
            var fx117 = visual.PlaySkillCast(cat.Resolve(117), beggar.position, enemy.position, 20);
            var fx119 = visual.PlaySkillCast(cat.Resolve(119), beggar.position, enemy.position, 20);
            Assert.AreEqual(40, fx117.pcMissileLifeTicks, "117 missile 44 LifeTime=40 (MoveKind=7)");
            Assert.AreEqual(16, fx119.pcMissileLifeTicks, "119 missile 45 LifeTime=16 (MoveKind=1)");
            Assert.Greater(fx117.pcMissileLifeTicks, fx119.pcMissileLifeTicks, "117 (MoveKind=7) flies longer than 119 (MoveKind=1)");
        }

        [Test]
        public void CaiBang_357_HomingSpreadKeepsPerMissileOffsetsForLiveTarget()
        {
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var liveTarget = new Vector2(200, 0);
            var fx = visual.PlaySkillCast(cat.Resolve(357), Vector2.zero, new Vector2(100, 0), 20, () => liveTarget);

            Assert.IsNotNull(fx);
            Assert.AreEqual(4, fx.missileCount, "Phi Long level 20 should spawn 4 parallel homing missiles");
            Assert.IsNotNull(fx.missileTargetOffsets, "Parallel homing missiles need stable per-missile target offsets");

            fx.phase = SkillEffectPhase.Missile;
            fx.phaseStart = fx.elapsed;
            var originalY = fx.missilePositions.Select(p => p.y).ToArray();

            visual.Update(0.01f);

            for (int i = 0; i < fx.missilePositions.Length; i++)
            {
                Assert.AreEqual(originalY[i], fx.missilePositions[i].y, 0.001f, $"Missile {i} should chase live target plus its own offset, not collapse into center target");
                Assert.Greater(fx.missilePositions[i].x, 0f, $"Missile {i} should advance toward the live target");
            }
        }

        [Test]
        public void CaiBang_1074_MslCountInterpolatesLinearly_FromL1ToL20()
        {
            // PC gaibang.lua::gungaibang150 (1074) skill_misslenum_v={{1,1},{20,5}}.
            // Verify counts: L1=1, L10=~3, L20=5.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(1074);
            int[] expectedCounts = { 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4, 4, 4, 5, 5, 5 };
            for (int lv = 1; lv <= 20; lv++)
            {
                var enemy = Enemy(new Vector2(400, 0));
                var fx = visual.PlaySkillCast(cat.Resolve(1074), beggar.position, enemy.position, lv);
                Assert.IsNotNull(fx, $"L{lv}: visual should be configured");
                Assert.AreEqual(expectedCounts[lv - 1], fx.missileCount, $"L{lv}: expected {expectedCounts[lv-1]} missiles (linear 1→5)");
            }
        }

        [Test]
        public void CaiBang_1539_VisualServiceUsesPcMissile47Speed()
        {
            // PC PcMissles.txt missile 47 (Bổng Đả ác Cẩu): Speed=31, LifeTime=16.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(1539);
            beggar.skillLevels[1539] = 20;
            var enemy = Enemy(new Vector2(400, 0));
            var fx = visual.PlaySkillCast(cat.Resolve(1539), beggar.position, enemy.position, 20);
            Assert.IsNotNull(fx);
            Assert.AreEqual(31, fx.pcMissileSpeedPerTick, "PC missile 47 Speed=31");
            Assert.AreEqual(16, fx.pcMissileLifeTicks, "PC missile 47 LifeTime=16");
        }
    }
}
