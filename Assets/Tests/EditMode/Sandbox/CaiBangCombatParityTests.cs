using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CaiBang")]
    public class CaiBangCombatParityTests
    {
        // [CaiBang-TestIsolation 2026-06-19] Reverted: sharing static catalog across tests broke
        //   CaiBang_Cast test (damage = 0 sau SandboxManager_Bootstraps test). Fresh catalog per test
        //   ensures isolation. Catalog build cost ~50ms is acceptable cho 24 tests trong fixture này.
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
            Assert.AreEqual(40, cat.Count, "Cai Bang catalog includes novice/universal skills, stock 115-130 skills, MOD/player extensions, Phi Long collide child 389, NguDieuCanKhon 1072, and NPC variants (1101/1103/1161/1162/1539)");
        }

        [Test]
        public void CaiBangCatalog_UsesCanonicalSlistcacheStaticRows()
        {
            Assert.IsNotNull(UnityEngine.Resources.Load<TextAsset>("Reference/PcCaiBangSkills"),
                "canonical static rows must be sync-loadable from packaged Resources on Android");
            var cat = Catalog();

            var s125 = cat.Resolve(125);
            Assert.AreEqual(16, s125.childSkillNum);
            Assert.AreEqual(47, s125.childSkillId);
            Assert.AreEqual(SkillMissileForm.Surround, s125.missileForm);
            Assert.IsFalse(s125.byMissile);

            var s359 = cat.Resolve(359);
            Assert.AreEqual(16, s359.childSkillNum);
            Assert.AreEqual(168, s359.childSkillId);
            Assert.AreEqual(SkillMissileForm.Single, s359.missileForm,
                "PC form 0 still has child missiles; Unity keeps single-projectile render fallback");
            Assert.IsFalse(s359.byMissile);

            var s1073 = cat.Resolve(1073);
            Assert.AreEqual(27, s1073.maxLevel);
            Assert.IsTrue(s1073.byMissile);
            Assert.AreEqual(1101, s1073.startSkillId);
            Assert.AreEqual(1103, s1073.flySkillId);
            Assert.AreEqual(1, s1073.flyEventTime);

            var s1074 = cat.Resolve(1074);
            Assert.AreEqual(27, s1074.maxLevel);
            Assert.AreEqual(5, s1074.childSkillNum);
            Assert.IsTrue(s1074.byMissile);
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
            // [CaiBang-slistcache 2026-07-17] PC row 117 LvlData1..3 RỖNG (mọi nguồn) → 0 attribs.
            //   Trước fix (sai): mượn bảng yanmen_tuobo của 119 → 55/100-150/cost 10.
            var throwStone = cat.Resolve(117).GetPcLevelData(20);
            Assert.AreEqual("PhysicsEnhanceP=0,0,0", throwStone.First(MagicAttributeKind.PhysicsEnhanceP).ToString());
            Assert.AreEqual("FireDamageV=0,0,0", throwStone.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=0,0,0", throwStone.First(MagicAttributeKind.SkillCostV).ToString());

            var dragon = cat.Resolve(128).GetPcLevelData(20);
            Assert.AreEqual("FireDamageV=536,0,536", dragon.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=50,0,0", dragon.First(MagicAttributeKind.SkillCostV).ToString());
            // [CaiBang-slistcache 2026-07-16] PC gaibang.lua::kanglong_youhui seriesdamage_p L20=50.
            Assert.AreEqual("SeriesDamageP=50,0,0", dragon.First(MagicAttributeKind.SeriesDamageP).ToString());
        }

        [Test]
        public void CaiBang_ResistAndPassiveSkills_MatchLuaLevelFormulasIncludingBugs()
        {
            var cat = Catalog();
            Assert.AreEqual("AddPhysicsDamageP=150,-1,2", cat.Resolve(115).GetPcLevelData(20).First(MagicAttributeKind.AddPhysicsDamageP).ToString());
            Assert.AreEqual("DeadlyStrikeEnhanceP=25,-1,0", cat.Resolve(115).GetPcLevelData(20).First(MagicAttributeKind.DeadlyStrikeEnhanceP).ToString());
            // [CaiBang-FirePool 2026-07-17] PC gaibang.lua::gaibang_zhangfa grants addfiremagic_v (25→275),
            //   the MAGIC fire pool (m_CurrentFireMagic, consumed by !bIsPhysical skills). Formerly
            //   conflated under AddFireDamageV; now its own kind AddFireMagicV.
            Assert.AreEqual("AddFireMagicV=275,-1,9", cat.Resolve(116).GetPcLevelData(20).First(MagicAttributeKind.AddFireMagicV).ToString());
            Assert.AreEqual("FastWalkRunP=33,3240,0", cat.Resolve(127).GetPcLevelData(20).First(MagicAttributeKind.FastWalkRunP).ToString());
            Assert.AreEqual("ColdResP=52,25200,0", cat.Resolve(126).GetPcLevelData(20).First(MagicAttributeKind.ColdResP).ToString());
            // PC Lua 金乌映雪 returns Param2String(result,result,0) for skill_cost_v; preserve odd tuple.
            Assert.AreEqual("SkillCostV=20,20,0", cat.Resolve(126).GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString());
            // PC Lua 化险为夷 cost uses undefined result1/result2; runtime treats nil as 0 in this port evidence fixture.
            Assert.AreEqual("SkillCostV=0,0,0", cat.Resolve(129).GetPcLevelData(20).First(MagicAttributeKind.SkillCostV).ToString());
        }

        [Test]
        public void CaiBang_PlayerSkills_HaveNoFabricatedConfuseP()
        {
            // PC truth (verified 2026-06-29): gaibang.lua has NO confuse/混乱/迷惑 keyword at all,
            // and missles.txt has no state-apply column. Therefore NO Cai Bang player skill applies
            // a Confuse state at cast time. Previous catalog entries adding ConfuseP were fabricated.
            // PC source: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/script/skill/gaibang.lua
            //           + pak_unpacked/slistcache/unknown/08bcd3fc.dat (missles, no state cols).
            var cat = Catalog();
            int[] ids = { 117, 119, 122, 125, 128, 357, 359, 1073, 1074 };
            foreach (var id in ids)
            {
                var d = cat.Resolve(id).GetPcLevelData(20);
                Assert.IsNotNull(d, $"skill {id} missing from catalog");
                Assert.IsFalse(d.state.Any(a => a.kind == MagicAttributeKind.ConfuseP),
                    $"skill {id} must have NO ConfuseP state (PC gaibang.lua has no confuse)");
            }
        }

        [Test]
        public void CaiBang_Cast_AppliesCostCooldownProjectileCountDamageAndHorseRestriction()
        {
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var beggar = Beggar();
            var enemy = Enemy(new Vector2(300, 0));
            var r = svc.Cast(beggar, enemy, 125, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(48, r.manaCost); // PC bangda_egou L20 skill_cost_v=48.
            // slistcache skills.txt row 125: ChildSkillId=47, ChildSkillNum=16, MisslesForm=3.
            // addskilldamage remains a passive bonus for 359/1074; these are 125's own 16 missiles.
            Assert.AreEqual(35, r.addSkillDamagePercent, "learned 119 grants canonical addskilldamage2 +35% to 125");
            Assert.AreEqual(16, r.childProjectileCount, "PC row 125 spawns sixteen child-47 missiles");
            Assert.AreEqual(16, r.projectiles.Count(p => p.skillId == 47));
            Assert.IsFalse(r.projectiles.Any(p => p.skillId == 168), "casting 125 must not spawn 359/1074 chain dragons (missile 168)");
            // PC KSkill::Cast (KSkills.cpp:348): SkillStyle=Missiles -> CastMissles() only, no direct damage.
            // 125 damage resolves on the first spawned missile impact (OnMissleEvent).
            Assert.AreEqual(1000, enemy.currentLife, "125 is Missile-style: parent damage waits for missile impact");
            var first125Missile = r.projectiles.First(p => p.skillId == 47);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, first125Missile, enemy.position));
            Assert.Less(enemy.currentLife, 1000, "125 missile impact applies its own levelData damage");
            Assert.AreEqual(0, svc.NextCastTime(beggar.actorId, 125), "canonical slistcache TimePerCast=0");

            var repeated = svc.Cast(beggar, enemy, 125, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(repeated.success, repeated.detail);

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
        public void CaiBang_127_HoatBatLuuThu_AppliesPcFastWalkRunDuration()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(127);
            beggar.skillLevels[127] = 20;

            var r = svc.Cast(beggar, beggar, 127, beggar.position, CombatRelation.Self);

            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(50, r.manaCost, "PC huabu_liushou L20 skill_cost_v=50");
            Assert.IsTrue(beggar.states.TryGetValue(MagicAttributeKind.FastWalkRunP, out var speed));
            Assert.AreEqual(33, speed.value1, "PC slistcache huabu_liushou L20 fastwalkrun_p=33 (giảm hiệu quả gia tốc)");
            Assert.AreEqual(3240, speed.value2, "PC huabu_liushou L20 duration=18*180 ticks");
        }

        [Test]
        public void CaiBang_130_TuyDiepCuongVu_AppliesPcBuffDurations()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(130);
            beggar.skillLevels[130] = 20;

            var r = svc.Cast(beggar, beggar, 130, beggar.position, CombatRelation.Self);

            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(100, r.manaCost, "PC zuidie_kuangwu L20 skill_cost_v=100");
            // [CaiBang-slistcache 2026-07-15] slistcache đổi allres_p→allres_yan_p (1→15 L30): L20≈10.
            Assert.IsTrue(beggar.states.TryGetValue(MagicAttributeKind.AllResYanP, out var allRes),
                "PC slistcache zuidie allres_yan_p");
            Assert.AreEqual(10, allRes.value1, "PC slistcache zuidie_kuangwu L20 allres_yan_p floors to 10");
            Assert.AreEqual(2867, allRes.value2, "PC zuidie_kuangwu L20 duration interpolates between 18*120 at L1 and 18*180 at L30");
            // [CaiBang-FirePool 2026-07-17] zuidie_kuangwu grants BOTH PC fire pools:
            //   addfiremagic_v(10→215, L20≈144) -> AddFireMagicV (magic pool)
            //   addfiredamage_v(10→175, L20≈118) -> AddFireDamageV (physical pool)
            Assert.IsTrue(beggar.states.TryGetValue(MagicAttributeKind.AddFireMagicV, out var addFireMagic),
                "PC zuidie_kuangwu L20 addfiremagic_v stored under AddFireMagicV");
            Assert.AreEqual(144, addFireMagic.value1, "PC zuidie_kuangwu L20 addfiremagic_v floors to 144");
            Assert.AreEqual(2867, addFireMagic.value2);
            Assert.IsTrue(beggar.states.TryGetValue(MagicAttributeKind.AddFireDamageV, out var addFireDmg),
                "PC zuidie_kuangwu L20 addfiredamage_v stored under AddFireDamageV");
            Assert.AreEqual(118, addFireDmg.value1, "PC zuidie_kuangwu L20 addfiredamage_v floors to 118");
            Assert.IsTrue(beggar.states.TryGetValue(MagicAttributeKind.LifeMaxYanP, out var lifeMaxYan));
            Assert.AreEqual(2867, lifeMaxYan.value2, "PC slistcache lifemax_yan_p duration finite (18*120→18*180), không còn sentinel -1");
        }

        [Test]
        public void CaiBang_127_MatchesPcSlistcacheRow_HoatBatLuuThu()
        {
            // PC slistcache ec1243ff.dat skill 127 (authoritative, overrides stale comments):
            //   SkillStyle=0 (active cast buff), MisslesForm=6 (Stance/Self), CharAnimId=11, TargetSelf=1.
            // Mobile comment cũ nói Style=3/anim=14 là ĐỌC SAI → fix về PC truth.
            var s = Catalog().Resolve(127);
            Assert.IsNotNull(s, "127 must be in catalog");
            Assert.AreEqual(PcSkillStyle.Missiles, s.skillStyle,
                "PC slistcache 127 SkillStyle=0 maps to active Missiles style, NOT passive always-on");
            Assert.AreEqual(11, s.charAnimId, "PC slistcache 127 CharAnimId=11");
            Assert.AreEqual(SkillMissileForm.Stance, s.missileForm, "PC slistcache 127 MisslesForm=6 (Stance)");
            Assert.IsTrue(s.targetSelf, "PC slistcache 127 TargetSelf=1");
        }

        [Test]
          public void CaiBang_720_QuyetChu_AppliesAllPcFiveDebuffAttrs()
        {
            // PC gaibang.lua::gaibang120zuzhou (skill 720) defines 5 debuff attrs at L20:
            //   physicsres_p=-10, fireres_p=-15, physicsresmax_p=-4, fireresmax_p=-6,
            //   rangedamagereturn_p=-30; duration=9*18=162 ticks.
            // Mobile cũ chỉ apply 2 (physicsres/fireres) → thiếu 3 debuff.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(720);
            beggar.skillLevels[720] = 20;
            var enemy = Enemy(new Vector2(2, 0));

            var r = svc.Cast(beggar, enemy, 720, enemy.position, CombatRelation.Enemy);

            Assert.IsTrue(r.success, r.detail);
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.PhysicsResYanP, out var pres), "PC slistcache physicsres_yan_p");
            Assert.AreEqual(-17, pres.value1, "PC slistcache L20 physicsres_yan_p floor(-16.04)=-17");
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.FireResYanP, out var fres), "PC slistcache fireres_yan_p");
            Assert.AreEqual(-17, fres.value1, "PC slistcache L20 fireres_yan_p floor(-16.04)=-17");
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.PhysicsResMaxP, out var pmax), "PC physicsresmax_p");
            Assert.AreEqual(-10, pmax.value1, "PC slistcache L20 physicsresmax_p=-10");
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.FireResMaxP, out var fmax), "PC fireresmax_p");
            Assert.AreEqual(-15, fmax.value1, "PC slistcache L20 fireresmax_p=-15");
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.RangeDamageReturnP, out var rret), "PC rangedamagereturn_p");
            Assert.AreEqual(-30, rret.value1, "PC L20 rangedamagereturn_p=-30");
            Assert.AreEqual(162, pres.value2, "PC L20 duration=9*18=162 ticks");
            // PC slistcache 720: MisslesForm=6 (Stance), not mobile Surround.
              Assert.AreEqual(SkillMissileForm.Stance, Catalog().Resolve(720).missileForm, "PC 720 MisslesForm=6");
          }

          [Test]
          public void CaiBang_720_FireResMaxDebuffChangesPlayerDamageOutcome()
          {
              var catalog = Catalog();
              const int probeSkillId = 990720;
              var probe = new SkillDefinition
              {
                  skillId = probeSkillId,
                  nameNormalized = "720 fire-cap probe",
                  maxLevel = 1,
                  // Test fixture: Melee delivery so the fire-resist-cap math is exercised via direct
              // damage. A Missile-style probe with childSkillNum=0 would spawn no missile and (PC
              // KSkill::Cast) never apply damage, which is irrelevant to the res-cap assertion here.
              skillStyle = PcSkillStyle.Melee,
                  targetEnemy = true,
                  timePerCast = 0,
              };
              var probeData = new SkillLevelData { level = 1 };
              probeData.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, 100, 0, 100));
              probe.pcLevelData.Add(probeData);
              catalog.Register(probe);

              var damage = new DamageFormulaService { Roll = (min, _) => min, RollPercent = _ => true };
              var runtime = new CombatRuntimeService(catalog, damage: damage);
              var source = Beggar();
              source.knownSkills.Add(probeSkillId);
              source.skillLevels[probeSkillId] = 1;

              var control = Beggar();
              control.actorId = 31;
              control.currentLife = control.maxLife = 1000;
              control.states[MagicAttributeKind.FireResP] = new SkillMagicAttribute(MagicAttributeKind.FireResP, 100, 0, 0);
              control.states[MagicAttributeKind.FireResYanP] = new SkillMagicAttribute(MagicAttributeKind.FireResYanP, -17, 0, 0);
              int controlDamage = runtime.Cast(source, control, probeSkillId, control.position, CombatRelation.Enemy)
                  .damageResults.Single().finalDamage;

              var debuffed = Beggar();
              debuffed.actorId = 32;
              debuffed.currentLife = debuffed.maxLife = 1000;
              debuffed.states[MagicAttributeKind.FireResP] = new SkillMagicAttribute(MagicAttributeKind.FireResP, 100, 0, 0);
              var applier = Beggar();
              applier.actorId = 33;
              applier.knownSkills.Add(720);
              applier.skillLevels[720] = 20;
              Assert.IsTrue(runtime.Cast(applier, debuffed, 720, debuffed.position, CombatRelation.Enemy).success);
              int debuffedDamage = runtime.Cast(source, debuffed, probeSkillId, debuffed.position, CombatRelation.Enemy)
                  .damageResults.Single().finalDamage;

              Assert.AreEqual(25, controlDamage,
                  "player fire resist is capped at canonical BASE_FIRE_RESIST_MAX=75");
              Assert.AreEqual(40, debuffedDamage,
                  "skill 720 L20 lowers the fire cap by 15 (75→60), so fixed 100 fire takes 40");
              Assert.Greater(debuffedDamage, controlDamage,
                  "physics/fire res-max fields must affect combat, not remain static catalog metadata");
          }

        [Test]
        public void CaiBang_714_PassiveAutoAttack_ProcsSkill720OnBearerHit()
        {
            // PC slistcache + gaibang.lua::gaibang120 (verified 2026-06-30):
            //   714 Hỗn Thiên Khí Công = PASSIVE (Style=3). When learned, attaches 'autoattackskill'
            //   magic attrib. When bearer is HIT, roll proc% → cast skill 720 (Quyết Chú debuff)
            //   on attacker + 12s CD. autoattackskill[3]=12*18*256+N → /256=216 ticks CD, %256=proc%.
            //   jx-cocos server: KNpcAttribModify::autoskill stores m_AutoAttackSkill; KNpc::AutoDoSkill
            //   casts 720 + SetNextCastTime(714, +nDelay).
            // Mobile cũ SAI: UtilitySkill active + fabricated AddPhysicsDamageP.
            var dmg = new DamageFormulaService { RollPercent = (pct) => true }; // force proc + crit
            var svc = new CombatRuntimeService(Catalog(), damage: dmg);
            var beggar = Beggar();
            beggar.knownSkills.Add(714);
            beggar.skillLevels[714] = 20;
            beggar.currentLife = 1000;
            var enemy = Enemy(new Vector2(2, 0)); // attacker (becomes target of proc'd 720)
            enemy.knownSkills.Add(122); // enemy needs a damaging CaiBang magic-fire skill (PC 117 deals 0)
            enemy.skillLevels[122] = 20;
            enemy.faction = CombatFaction.CaiBang;
            enemy.currentMana = 500;

            // enemy casts 122 on beggar → beggar is hit → beggar's 714 procs → 720 debuff on enemy.
            var r = svc.Cast(enemy, beggar, 122, beggar.position, CombatRelation.Enemy);

            Assert.IsTrue(r.success, r.detail);
            // PC KSkill::Cast: 122 SkillStyle=Missiles -> the proc fires on missile impact, not on Cast.
            var missile122 = r.projectiles.FirstOrDefault(p => p.skillId == 46);
            Assert.IsNotNull(missile122, "122 spawned its child-46 missile");
            Assert.IsTrue(svc.TryResolveProjectileCollision(enemy, beggar, r, missile122, beggar.position),
                "122 missile impact lands on beggar");
            // PC: 714 is passive (Style=3), NOT active UtilitySkill.
            Assert.AreEqual(PcSkillStyle.PassivityNpcState, Catalog().Resolve(714).skillStyle,
                "PC slistcache 714 SkillStyle=3 (passive)");
            // PC: 714 has NO fabricated AddPhysicsDamageP; proc casts 720 on attacker instead.
            Assert.IsFalse(beggar.states.ContainsKey(MagicAttributeKind.AddPhysicsDamageP),
                "PC 714 has no AddPhysicsDamageP (was fabricated in mobile)");
            // PC: proc fired → 720 debuff applied to attacker (enemy).
            Assert.IsTrue(enemy.states.TryGetValue(MagicAttributeKind.PhysicsResYanP, out var pres),
                "720 debuff proc'd on attacker via 714 on-hit (slistcache physicsres_yan_p)");
              Assert.AreEqual(-17, pres.value1, "PC slistcache 720 L20 physicsres_yan_p floor=-17 (proc'd from 714)");
          }

          [Test]
          public void CaiBang_714_DecodesPcProcRateAtLevels1_15_20()
          {
              // PC gaibang120.autoattackskill slot[3] = {{1,12*18*256+1},{20,12*18*256+10},{21,12*18*256+10}}
              // → low byte proc%: L1=1, L15=floor(1+14/19*9)=7, L20=10. (verified 2026-07-17, 3 nguồn PC).
              foreach (var expected in new[] { (level: 1, rate: 1), (level: 15, rate: 7), (level: 20, rate: 10) })
              {
                  var observed = new System.Collections.Generic.List<int>();
                  var runtime = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService
                  {
                      RollPercent = pct => { observed.Add(pct); return true; },
                  });
                  var bearer = Beggar();
                  bearer.knownSkills.Add(714);
                  bearer.skillLevels[714] = expected.level;
                  bearer.currentLife = 100000;
                  var attacker = Enemy(new Vector2(2, 0));
                  attacker.faction = CombatFaction.CaiBang;
                  attacker.currentMana = 1000;
                  attacker.knownSkills.Add(122);
                  attacker.skillLevels[122] = 20;

                  var report = runtime.Cast(attacker, bearer, 122, bearer.position, CombatRelation.Enemy);
                  Assert.IsTrue(report.success, report.detail);
                  // PC KSkill::Cast: 122 SkillStyle=Missiles -> proc fires on missile impact.
                  var missile = report.projectiles.FirstOrDefault(p => p.skillId == 46);
                  Assert.IsNotNull(missile, "122 spawned its child-46 missile");
                  Assert.IsTrue(runtime.TryResolveProjectileCollision(attacker, bearer, report, missile, bearer.position),
                      "122 missile impact lands on bearer");
                  CollectionAssert.Contains(observed, expected.rate,
                      $"PC autoattackskill[3] low byte at L{expected.level} must be {expected.rate}%");
                  Assert.AreEqual(216, runtime.NextCastTime(bearer.actorId, 714),
                      "PC autoattackskill[3] high bytes encode 12*18=216 cooldown ticks");
              }
          }

          [Test]
          public void CaiBang_714_LuaServiceDecodesAutoattackskillFullPcExpression()
          {
              // [CaiBang-714 2026-07-17] PC gaibang.lua::gaibang120.autoattackskill (line 370,
              // client_offline + server_offline + newest slistcache deu dong y):
              //   slot[1] = {{1,720*256 + 1},{20,720*256 + 20},{21,720*256 + 21}}
              //   slot[3] = {{1,12*18*256 + 1},{20,12*18*256 + 10},{21,12*18*256 + 10}}
              // Parser truoc fix chi evaluate * va / -> doc 12*18*256 (low byte 0), sai PC (+10).
              // Sau fix: doc du 12*18*256+10=55306 -> /256=216 ticks CD, %256=10 proc%.
              Assert.AreEqual(12 * 18 * 256 + 10,
                  PcCaiBangLuaLevelService.GetSingleValue(714, 20, "autoattackskill", 3),
                  "PC autoattackskill[3] L20 raw = 12*18*256+10 (parser phai ho tro +)");
              int raw = PcCaiBangLuaLevelService.GetSingleValue(714, 20, "autoattackskill", 3);
              Assert.AreEqual(216, raw / 256, "PC: /256 = 12*18=216 cooldown ticks");
              Assert.AreEqual(10, raw % 256, "PC: %256 = proc% 10 tai L20");
              Assert.AreEqual(1, PcCaiBangLuaLevelService.GetSingleValue(714, 1, "autoattackskill", 3) % 256,
                  "PC: L1 proc% = 1");
              Assert.AreEqual(720 * 256 + 20,
                  PcCaiBangLuaLevelService.GetSingleValue(714, 20, "autoattackskill", 1),
                  "PC autoattackskill[1] L20 raw = 720*256+20 (target skill 720, level 20)");
          }

          [Test]
          public void CaiBang_714_CooldownBoundaryRequiresCurrentTimeStrictlyGreaterThanNext()
          {
              int procRolls = 0;
              var catalog = Catalog();
              catalog.Resolve(117).timePerCast = 0; // isolate the 714 boundary from the attacker's own cooldown
              var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService
              {
                  RollPercent = pct => { if (pct == 10) procRolls++; return true; },
              });
              var bearer = Beggar();
              bearer.knownSkills.Add(714);
              bearer.skillLevels[714] = 20;
              bearer.currentLife = 100000;
              var attacker = Enemy(new Vector2(2, 0));
              attacker.faction = CombatFaction.CaiBang;
              attacker.currentMana = 1000;
              attacker.knownSkills.Add(122);
              attacker.skillLevels[122] = 20;

              var c0 = runtime.Cast(attacker, bearer, 122, bearer.position, CombatRelation.Enemy);
              Assert.IsTrue(c0.success, c0.detail);
              ResolveFirstChildMissile(runtime, attacker, bearer, c0, 46);
              Assert.AreEqual(1, procRolls);
              Assert.AreEqual(216, runtime.NextCastTime(bearer.actorId, 714));

              runtime.AdvanceTime(215);
              var c1 = runtime.Cast(attacker, bearer, 122, bearer.position, CombatRelation.Enemy);
              Assert.IsTrue(c1.success, c1.detail);
              ResolveFirstChildMissile(runtime, attacker, bearer, c1, 46);
              Assert.AreEqual(1, procRolls, "t+215 remains on cooldown");

              runtime.AdvanceTime(1);
              var c2 = runtime.Cast(attacker, bearer, 122, bearer.position, CombatRelation.Enemy);
              Assert.IsTrue(c2.success, c2.detail);
              ResolveFirstChildMissile(runtime, attacker, bearer, c2, 46);
              Assert.AreEqual(1, procRolls, "PC nextCastTime < currentTime: equality at t+216 still blocks");

              runtime.AdvanceTime(1);
              var c3 = runtime.Cast(attacker, bearer, 122, bearer.position, CombatRelation.Enemy);
              Assert.IsTrue(c3.success, c3.detail);
              ResolveFirstChildMissile(runtime, attacker, bearer, c3, 46);
              Assert.AreEqual(2, procRolls, "t+217 is the first eligible tick");
          }

          // PC KSkill::Cast: 122 SkillStyle=Missiles -> damage/proc defers to missile impact.
          // Resolving the spawned child-46 missile against the bearer triggers the 714
          // passive-autoattack proc exactly when the bearer actually takes a damaging hit.
          private static void ResolveFirstChildMissile(CombatRuntimeService runtime, CombatActorState attacker, CombatActorState bearer, CombatCastReport report, int childSkillId)
          {
              var missile = report.projectiles.FirstOrDefault(p => p.skillId == childSkillId);
              if (missile != null)
                  runtime.TryResolveProjectileCollision(attacker, bearer, report, missile, bearer.position);
          }

        [Test]
        public void CaiBang_357_CollideEvent_NoFireBelowLevel10_PcL10Gate()
        {
            // PC gaibang.lua::feilong_zaitian skill_collideevent:
            //   [1]={{1,0},{10,0},{10,1},{20,1}}  ← gate flag: 0 for L1-9, 1 for L10+
            //   [3]={{1,389},{20,389}}            ← sub-skill id
            // => 389 (Long Chiến Ư Dã) fires on missile collide ONLY at L10+.
            // Trước fix (SECT-QUICKWIN): fired 389 at all levels — sai PC. Sau fix: honor L10 gate.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(357);
            beggar.skillLevels[357] = 5; // below L10 gate
            var enemy = Enemy(new Vector2(200, 0));

            var r = svc.Cast(beggar, enemy, 357, enemy.position, CombatRelation.Enemy);

            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(0, r.damageResults.Count, "Missile 166 damage must wait for collision");
            var missile = r.projectiles.Single(p => p.skillId == 166);
            Assert.IsTrue(svc.TryResolvePhiLongCollision(beggar, enemy, r, missile, enemy.position));
            // PC: below L10, collide sub-skill 389 does NOT fire — only 357's missile damage applies.
            Assert.AreEqual(1, r.damageResults.Count, "L5 < L10 gate → 389 must NOT fire");
            Assert.IsFalse(r.projectiles.Any(p => p.skillId == 195),
                "L5 < L10 gate → no stationary child 195 (389 did not fire)");
            Assert.IsFalse(svc.TryResolvePhiLongCollision(beggar, enemy, r, missile, enemy.position),
                "Each missile collision must resolve exactly once");
            Assert.AreEqual(1, r.damageResults.Count, "Duplicate collision must not apply damage again");
        }

        [Test]
        public void CaiBang_1073_DamageAnd1072ResolveAtMissileImpact()
        {
            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });
            var beggar = Beggar();
            beggar.knownSkills.Add(1073);
            beggar.skillLevels[1073] = 20;
            var enemy = Enemy(new Vector2(200, 0));

            var r = svc.Cast(beggar, enemy, 1073, enemy.position, CombatRelation.Enemy);

            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(0, r.damageResults.Count, "PC SkillStyle=Missiles: parent damage waits for missile impact");
            Assert.AreEqual(1000, enemy.currentLife);
            Assert.AreEqual(85, r.addSkillDamagePercent, "122(+40) and 128(+45) both grant 1073 damage");
            Assert.AreEqual(1072, r.skill.collideSkillId);
            Assert.AreEqual(1101, r.skill.startSkillId);
            Assert.AreEqual(1103, r.skill.flySkillId);

            var missile = r.projectiles.First(p => p.skillId == 335);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, missile, enemy.position));
            Assert.AreEqual(1, r.damageResults.Count, "missile 335 impact applies only parent 1073 damage");
            var stationary = r.projectiles.Single(p => p.skillId == 334);
            Assert.IsFalse(svc.TryResolveProjectileCollision(beggar, enemy, r, missile, enemy.position),
                "same projectile collision resolves once");

            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, stationary, enemy.position));
            Assert.AreEqual(2, r.damageResults.Count, "1072 damage resolves through its own stationary missile 334 lifecycle");
            var childFire = Catalog().Resolve(1072).GetPcLevelData(20).damage
                .Single(attr => attr.kind == MagicAttributeKind.FireDamageV);
            Assert.AreEqual(childFire.value1, r.damageResults[1].rolledBase,
                "skill_eventskilllevel passes parent L20 into collide child 1072");
        }

        [Test]
        public void CaiBang_1073_FlyEvent_Dispatches1103PerDistinctTick()
        {
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.knownSkills.Add(1073);
            beggar.skillLevels[1073] = 20;
            var enemy = Enemy(new Vector2(200, 0));
            var report = svc.Cast(beggar, enemy, 1073, enemy.position, CombatRelation.Enemy);
            var parentMissile = report.projectiles.First(p => p.skillId == 335);
            int projectileCount = report.projectiles.Count;
            int flyChildCount = report.projectiles.Count(p => p.skillId == 344);

            Assert.AreEqual(1, report.skill.flyEventTime, "PC row 1073 FlyEventTime=1");
            Assert.AreEqual(3, PcCaiBangLuaLevelService.GetMissileCount(1103, 20),
                "PC zhanggaibang150 L20 skill_misslenum_v emits three 1103 child missiles");
            Assert.IsTrue(svc.TryResolveProjectileFly(beggar, enemy, report, parentMissile, 1, enemy.position));
            Assert.AreEqual(projectileCount + 3, report.projectiles.Count,
                "PC 1073 FlyEvent must spawn all three L20 1103 child missiles");
            var firstFlyChildren = report.projectiles.Skip(projectileCount).Where(p => p.skillId == 344).ToList();
            Assert.AreEqual(3, firstFlyChildren.Count);
            foreach (var flyChild in firstFlyChildren)
            {
                Assert.AreEqual(enemy.position, flyChild.origin,
                    "PC ByMissle FlyEvent launches 1103 from parent missile event point");
                Assert.AreEqual(1103, report.projectileImpactSkillIds[flyChild.instanceId]);
                Assert.AreEqual(20, report.projectileImpactSkillLevels[flyChild.instanceId],
                    "PC skill_eventskilllevel overrides row 1073 EventSkillLevel=1 with parent L20 for 1103");
            }
            CollectionAssert.Contains(report.resolvedLifecycleSkillIds, 1103);
            Assert.IsFalse(svc.TryResolveProjectileFly(beggar, enemy, report, parentMissile, 1, enemy.position),
                "same projectile/tick must be idempotent");
            Assert.IsTrue(svc.TryResolveProjectileFly(beggar, enemy, report, parentMissile, 2, enemy.position),
                "FlyEventTime=1 permits next distinct tick");
            Assert.AreEqual(flyChildCount + 6, report.projectiles.Count(p => p.skillId == 344));
        }

        [Test]
        public void CaiBang_1073_VisualServiceArmsCanonicalFlyCallback()
        {
            var catalog = Catalog();
            var visual = new SkillEffectVisualService(null, catalog);
            var fx = visual.PlaySkillCast(catalog.Resolve(1073), Vector2.zero, new Vector2(2000, 0), 20);

            Assert.IsNotNull(fx);
            Assert.IsTrue(fx.pcFlyEventEnabled);
            Assert.AreEqual(1, fx.pcFlyEventIntervalTicks, "PC row 1073 FlyEventTime=1");
            Assert.AreEqual(1103, fx.pcFlySkillId);
            Assert.IsNotNull(fx.missileFlyEventOrdinals,
                "visual flight must allocate ordinal tracking before the controller wires its callback");
            Assert.AreEqual(fx.missileCount, fx.missileFlyEventOrdinals.Length);
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

        [Test, Category("CaiBang")]
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
                // [CaiBang-TestIsolation 2026-06-19] Reset SandboxManager.Instance singleton — otherwise
                //   subsequent tests trong cùng fixture hoặc category-only runs see corrupted global state
                //   (CombatSkillCatalog từ CreateNoviceAndCoreSectCatalog thay vì CreateNoviceAndCaiBangCatalog,
                //   gây ra damage=0 cho các test cast skill 122/125 v.v. sau đó).
                var instanceProp = typeof(SandboxManager).GetProperty("Instance");
                instanceProp?.GetSetMethod(true)?.Invoke(null, new object[] { null });
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void CaiBang_AttackRadius_ScalesPerLevelFromPcGaibangLua()
        {
            // PC gaibang.lua short-range skills (119, 122): skill_attackradius={{{1,320},{20,384}}}.
            // [CaiBang-FailClosed117 2026-07-17] 117 (Đầu Thạch Vấn Lộ) has NO PC LvlData beyond skill_cost_v
            //   (jx-source pak_unpacked/update03/settings/skills.txt row 117 col 73 = "skill_cost_v").
            //   It must NOT borrow yanmen_tuobo (119); the lua service fails closed (returns 0).
            // PC gaibang.lua long-range skills (125, 128, 357, 359, 1073, 1074, 1539): skill_attackradius={{{1,448},{20,512}}}.
            // [CaiBang-LuaPort 2026-06-17] PcCaiBangSkillTuning/PcCaiBangModTuning removed;
            // PcCaiBangLuaLevelService reads radius straight từ gaibang.lua SKILLS dict.
            // 128 keeps its dedicated PcKangLongYouHuiTuning (level-curve richer than SKILLS dict).
            int[] shortRange = { 119, 122 };
            int[] longRange  = { 125, 357, 359, 1073, 1074, 1539 };

            foreach (int id in shortRange)
            {
                Assert.AreEqual(320, PcCaiBangLuaLevelService.GetAttackRadius(id, 1), $"L1 radius for {id}");
                Assert.AreEqual(384, PcCaiBangLuaLevelService.GetAttackRadius(id, 20), $"L20 radius for {id}");
                int mid = PcCaiBangLuaLevelService.GetAttackRadius(id, 10);
                Assert.GreaterOrEqual(mid, 320, $"L10 radius for {id} should be >= L1");
                Assert.LessOrEqual(mid, 384, $"L10 radius for {id} should be <= L20");
            }
            // 117 fail-closed: no PC LvlData → lua returns 0 at every level (no yanmen_tuobo borrow).
            Assert.IsFalse(PcCaiBangLuaLevelService.Applies(117), "117 must not be mapped (no PC LvlData)");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetAttackRadius(117, 1), "117 fail-closed L1");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetAttackRadius(117, 20), "117 fail-closed L20");
            foreach (int id in longRange)
            {
                Assert.AreEqual(448, PcCaiBangLuaLevelService.GetAttackRadius(id, 1), $"{id} L1");
                Assert.AreEqual(512, PcCaiBangLuaLevelService.GetAttackRadius(id, 20), $"{id} L20");
            }
            // 128 still routed qua PcKangLongYouHuiTuning (448→512 curve).
            Assert.AreEqual(448, PcKangLongYouHuiTuning.AtLevel(1).attackRadius, "128 L1");
            Assert.AreEqual(512, PcKangLongYouHuiTuning.AtLevel(20).attackRadius, "128 L20");
        }

        [Test]
        public void CaiBang_117_CastUsesCanonicalStaticRadius_WhenLuaDataAbsent()
        {
            // PC Skills.txt static row pins attackRadius=280. Its Lua LvlData is empty, so no
            // level curve may replace that static value.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            var near = Enemy(new Vector2(270, 0));
            var r = svc.Cast(beggar, near, 117, near.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);

            svc.AdvanceTime(20);
            var far = Enemy(new Vector2(300, 0));
            var rejected = svc.Cast(beggar, far, 117, far.position, CombatRelation.Enemy);
            Assert.AreEqual(CombatCastRejectReason.OutOfRange, rejected.reason);
        }

        [Test]
        public void CaiBang_117_CastAtLevel1_UsesFlatCatalogRadius_FailClosed()
        {
            // [CaiBang-FailClosed117 2026-07-17] 117 has NO PC LvlData (row 117 LvlData1="skill_cost_v" only),
            //   so there is no yanmen_tuobo L1 radius curve. Cast radius stays at the canonical static
            //   catalog value (280) at every level — no fabricated L1 tightening.
            var svc = new CombatRuntimeService(Catalog());
            var beggar = Beggar();
            beggar.skillLevels[117] = 1;
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetAttackRadius(117, 1), "117 fail-closed: lua returns 0");
            var near = Enemy(new Vector2(270, 0));
            var r = svc.Cast(beggar, near, 117, near.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);

            // Use a fresh cast state so this assertion isolates range rather than cooldown.
            var svc2 = new CombatRuntimeService(Catalog());
            var beggar2 = Beggar();
            beggar2.skillLevels[117] = 1;
            // 270 < catalog 280 → still in range at L1 (no PC L1 Lua curve exists for 117).
            var mid = Enemy(new Vector2(270, 0));
            var inRange = svc2.Cast(beggar2, mid, 117, mid.position, CombatRelation.Enemy);
            Assert.IsTrue(inRange.success,
                $"117 uses canonical static radius 280 at L1 (fail-closed, no PC L1 curve): {inRange.reason} {inRange.detail}");
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
        public void CaiBang_PhiLongAtLevel11_WaitsForMissileCollisionBeforeLongChienUYuye()
        {
            // PC gaibang.lua::feilong_zaitian enables skill_collideevent 389 at L10+, but
            // missile 166 must collide before the child skill fires. Cast itself only emits 166.
            var damage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: damage);
            var beggar = Beggar();
            beggar.knownSkills.Add(357);
            beggar.skillLevels[357] = 11;
            var enemy = Enemy(new Vector2(200, 0));
            
            var r = svc.Cast(beggar, enemy, 357, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            
            Assert.AreEqual(0, r.damageResults.Count, "No Phi Long damage applies before missile 166 collides");
            Assert.AreEqual(1, r.projectiles.Count, "Cast should spawn only Phi Long's missile 166");
            Assert.IsFalse(r.projectiles.Any(p => p.skillId == 195),
                "389's stationary child 195 must wait for the collision lifecycle");

            var missile = r.projectiles.Single(p => p.skillId == 166);
            Assert.IsTrue(svc.TryResolvePhiLongCollision(beggar, enemy, r, missile, enemy.position));
            Assert.AreEqual(1, r.damageResults.Count, "missile 166 collision applies only Phi Long damage");
            Assert.AreEqual(2, r.projectiles.Count, "collision spawns 389's stationary child missile 195");
            var stationary = r.projectiles.Single(p => p.skillId == 195);
            Assert.IsFalse(svc.TryResolvePhiLongCollision(beggar, enemy, r, missile, enemy.position),
                "the same missile must not fire 389 twice");

            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, stationary, enemy.position));
            Assert.AreEqual(2, r.damageResults.Count,
                "389 damage resolves through its own stationary missile 195 lifecycle");
            var childFire = Catalog().Resolve(389).GetPcLevelData(11).damage
                .Single(attr => attr.kind == MagicAttributeKind.FireDamageV);
            Assert.AreEqual(childFire.value1, r.damageResults[1].rolledBase,
                "PC skill_eventskilllevel passes Phi Long L11 to collide child 389");
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
            // [CaiBang-FailClosed117 2026-07-17] 117 no longer borrows yanmen_tuobo (no PC LvlData),
            //   so the lua missle_speed_v override does not apply; visual falls back to engine
            //   missile 44 Speed=14 (PC PcMissles.txt row 44 col 12). LifeTime=40 is engine, unchanged.
            Assert.AreEqual(14, fx.pcMissileSpeedPerTick, "117 fail-closed: engine missile 44 Speed=14 (no lua override)");
            Assert.AreEqual(40, fx.pcMissileLifeTicks, "PC missile 44 LifeTime=40");
        }

        [Test]
        public void CaiBang_122_FireDamageMaxesAtPc215_AtLevel20()
        {
            // PC gaibang.lua::jianren_shenshou (122) firedamage_v[3]={{1,15},{20,215}}.
            // rolledBase is the damage value before defender mitigation (armor/resist).
            // The actual roll is 1..215; sum across multiple damageResults stays in that range.
            // Force hit chance in this damage-value test; CheckHitTarget itself is covered separately.
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var beggar = Beggar();
            beggar.knownSkills.Add(122);
            beggar.skillLevels[122] = 20;
            var enemy = Enemy(new Vector2(200, 0));
            var r = svc.Cast(beggar, enemy, 122, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            // PC KSkill::Cast: 122 SkillStyle=Missiles -> CastMissles() only. Damage defers to the
            // single spawned child-46 missile impact (OnMissleEvent). addskilldamage is a passive
            // %-amp (see CaiBang_AddSkillDamage_IsPassiveDamageAmp_NotChainSpawn), not a chain spawn.
            Assert.AreEqual(1, r.projectiles.Count, "122 spawns a single child-46 missile");
            var missile122 = r.projectiles.First(p => p.skillId == 46);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, missile122, enemy.position));
            var rolled = r.damageResults.Sum(d => d.rolledBase);
            Assert.That(rolled, Is.GreaterThan(50), $"L20 fire rolled base should be substantial, got {rolled}");
            // PC gaibang.lua::jianren_shenshou L20 firedamage_v[3] max=215. Deterministic RollPercent=true
            // rolls the max, so a single missile impact yields exactly 215 (no chain spawn).
            Assert.That(rolled, Is.LessThanOrEqualTo(215), $"L20 single-missile fire rolled base max=215, got {rolled}");
        }

        [Test]
        public void CaiBang_117_NoFabricatedDamage_MatchesPcEmptyLvlData()
        {
            // PC truth (verified 2026-07-17, 3 nguồn đồng ý — client_offline_extracted/skills.txt,
            // server_offline/settings/skills.txt, slistcache ec1243ff PcCaiBangSkills.txt):
            //   row 117 LvlSetting1..3 = physicsdamage_v / firedamage_v / skill_cost_v nhưng
            //   LvlData1..3 RỖNG → GetSkillLevelData("", level) → "" → 0 attribs, cost = row CostValue = 0.
            //   AttackRadius = 280 (row). Đầu Thạch Vấn Lộ chỉ là skill "thăm dò" (SkillDesc PC).
            //   Trước fix (sai): mượn bảng yanmen_tuobo của 119 → 117 gây 125+ dmg, tốn 10 mana.
            var d1 = Catalog().Resolve(117).GetPcLevelData(1);
            var d20 = Catalog().Resolve(117).GetPcLevelData(20);
            Assert.IsFalse(d1.damage.Any(a => a.kind == MagicAttributeKind.PhysicsDamageV && a.value1 != 0),
                "117 L1 must have NO nonzero PhysicsDamageV (PC LvlData empty)");
            Assert.IsFalse(d20.damage.Any(a => a.kind == MagicAttributeKind.PhysicsDamageV && a.value1 != 0),
                "117 L20 must have NO nonzero PhysicsDamageV");
            Assert.IsFalse(d20.damage.Any(a => a.kind == MagicAttributeKind.FireDamageV && (a.value1 != 0 || a.value3 != 0)),
                "117 L20 must have NO nonzero FireDamageV");
            Assert.IsFalse(d20.skill.Any(a => a.kind == MagicAttributeKind.SkillCostV && a.value1 != 0),
                "117 L20 cost=0 (PC skill_cost_v empty → row CostValue=0)");
            Assert.AreEqual(280, Catalog().Resolve(117).attackRadius, "117 AttackRadius=280 (PC row value)");

            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });
            var beggar = Beggar();
            beggar.knownSkills.Add(117);
            beggar.skillLevels[117] = 20;
            var enemy = Enemy(new Vector2(200, 0));
            var r = svc.Cast(beggar, enemy, 117, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(0, r.manaCost, "PC 117 mana cost = 0");
            Assert.AreEqual(1, r.childProjectileCount, "PC row 117 ChildSkillNum=1 → missile 44 vẫn bay (thăm dò)");
            Assert.AreEqual(0, r.damageResults.Count, "117 Missile-style: damage waits for missile impact");
            var m = r.projectiles.First(p => p.skillId == 44);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, r, m, enemy.position));
            Assert.AreEqual(0, r.damageResults.Sum(d => d.finalDamage),
                "PC 117 has no damage attribs → missile impact deals 0 damage (probe skill)");
        }

        [Test]
        public void CaiBang_122_DefenderAllResStateReducesIncomingDamage()
        {
            // PC KNpc.cpp::CalcDamage reads m_CurrentXxxResist from the defender, then applies:
            //   nDamage -= nDamage * nRes / MAX_PERCENT.
            // Runtime parity: CombatRuntimeService must convert active defender states into DefenderStats.
            // (Dùng 122 — magic fire thật của PC; 117 giờ là probe 0 damage theo PC LvlData rỗng.)
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var beggar = Beggar();
            beggar.knownSkills.Add(122);
            beggar.skillLevels[122] = 20;

            // PC KSkill::Cast: 122 SkillStyle=Missiles -> CastMissles() only. Damage defers to the
            // single spawned child-46 missile impact (OnMissleEvent). Random roll happens at impact.
            var noResEnemy = Enemy(new Vector2(200, 0));
            var noRes = svc.Cast(beggar, noResEnemy, 122, noResEnemy.position, CombatRelation.Enemy);
            Assert.IsTrue(noRes.success, noRes.detail);
            Assert.AreEqual(0, noRes.damageResults.Count, "122 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260629);
            var noResMissile = noRes.projectiles.First(p => p.skillId == 46);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, noResEnemy, noRes, noResMissile, noResEnemy.position));
            int noResDamage = noRes.damageResults.Sum(d => d.finalDamage);

            svc.AdvanceTime(2);
            var resistedEnemy = Enemy(new Vector2(200, 0));
            resistedEnemy.states[MagicAttributeKind.AllResP] = new SkillMagicAttribute(MagicAttributeKind.AllResP, 50, -1, 0);
            var resisted = svc.Cast(beggar, resistedEnemy, 122, resistedEnemy.position, CombatRelation.Enemy);
            Assert.IsTrue(resisted.success, resisted.detail);
            Assert.AreEqual(0, resisted.damageResults.Count, "122 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260629);
            var resistedMissile = resisted.projectiles.First(p => p.skillId == 46);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, resistedEnemy, resisted, resistedMissile, resistedEnemy.position));
            int resistedDamage = resisted.damageResults.Sum(d => d.finalDamage);

            Assert.That(noResDamage, Is.GreaterThan(0), "baseline damage should hit with deterministic RollPercent=true");
            Assert.That(resistedDamage, Is.LessThan(noResDamage),
                "defender AllResP state must reduce incoming Cai Bang damage via PC CalcDamage resist path");
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
        public void CaiBang_128_KhangLongUsesFanSpreadFromLuaMissileForm()
        {
            // PC evidence: skills.txt 128 MisslesForm=2, missile 48 MoveKind=1.
            // gaibang.lua::kanglong_youhui L20 skill_misslenum_v=15 and skill_param1_v=2.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var fx = visual.PlaySkillCast(cat.Resolve(128), Vector2.zero, new Vector2(400, 0), 20);

            Assert.IsNotNull(fx);
            Assert.AreEqual(15, fx.missileCount, "Kháng Long L20 uses gaibang.lua skill_misslenum_v=15");
            Assert.AreEqual(1, fx.pcMissileMoveKind, "Kháng Long missile 48 is non-homing MoveKind=1");
            Assert.IsNull(fx.missileTargetOffsets, "Kháng Long fan spread must not use Phi Long parallel homing lane offsets");
            Assert.IsNotNull(fx.missileTargets);
            Assert.AreEqual(15, fx.missileTargets.Length);
            Assert.That(fx.missileTargets.Any(p => p.y > 1f), Is.True, "fan spread should include upward lanes");
            Assert.That(fx.missileTargets.Any(p => p.y < -1f), Is.True, "fan spread should include downward lanes");
            Assert.AreEqual(0f, fx.missileTargets[7].y, 0.001f, "center lane should remain aimed at the target center");
        }

        [Test]
        public void CaiBang_359_TianxiaUsesPcDerivedDamageRangeAndMissileData()
        {
            // Newest PC Thiên Hạ Vô Cẩu data comes from skills.txt row 359 plus gaibang.lua::tianxia_wugou:
            //   row: ChildSkillId=168, WaitTime=5, CharAnimId=11;
            //   Lua L20: attackradius=512, skill_misslenum_v=3, skill_cost_v=50,
            //            physics=206, fire min/max=285/432, confuse=60.
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var beggar = Beggar();
            beggar.knownSkills.Add(359);
            beggar.skillLevels[359] = 20;
            var enemy = Enemy(new Vector2(500, 0));

            var skill = Catalog().Resolve(359);
            var data = skill.GetPcLevelData(20);
            Assert.AreEqual(168, skill.childSkillId, "PC skills.txt row 359 ChildSkillId=168");
            Assert.AreEqual(5, skill.waitTime, "PC skills.txt row 359 WaitTime=5");
            Assert.AreEqual(11, skill.charAnimId, "PC skills.txt row 359 CharAnimId=11");
            Assert.AreEqual("PhysicsEnhanceP=206,0,0", data.First(MagicAttributeKind.PhysicsEnhanceP).ToString());
            Assert.AreEqual("FireDamageV=285,0,432", data.First(MagicAttributeKind.FireDamageV).ToString());
            // PC truth [2026-06-29]: gaibang.lua::tianxia_wugou has NO confuse state; the prior
            //   "ConfuseP=60,-1,0" assertion encoded a FABRICATED catalog entry (PC gaibang.lua
            //   has zero confuse/混乱/迷惑 keyword). Assert absence instead.
            Assert.IsFalse(data.state.Any(a => a.kind == MagicAttributeKind.ConfuseP),
                "PC gaibang.lua::tianxia_wugou applies no Confuse state at cast");
            Assert.AreEqual(512, PcCaiBangLuaLevelService.GetAttackRadius(359, 20), "Lua L20 range overrides row radius for runtime cast");
            Assert.AreEqual(3, PcCaiBangLuaLevelService.GetMissileCount(359, 20), "Lua L20 missile count overrides row ChildSkillNum");

            var report = svc.Cast(beggar, enemy, 359, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(report.success, report.detail);
            Assert.AreEqual(50, report.manaCost, "PC Lua tianxia_wugou L20 skill_cost_v=50");
            Assert.AreEqual(3, report.childProjectileCount, "Runtime should spawn Lua-derived 3 homing child missiles at L20");
            Assert.AreEqual(3, report.projectiles.Count);
            Assert.That(report.projectiles.All(p => p.skillId == 168), Is.True, "All runtime children should use PC missile 168");
            // PC KSkill::Cast: 359 SkillStyle=Missiles -> CastMissles() only. Damage defers to missile
            // impact (OnMissleEvent). Resolve the first of the three spawned missiles.
            Assert.AreEqual(1000, enemy.currentLife, "359 Missile-style: parent damage waits for missile impact");
            var first359Missile = report.projectiles.First(p => p.skillId == 168);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, enemy, report, first359Missile, enemy.position));
            Assert.Less(enemy.currentLife, 1000, "359 missile impact applies PC-derived damage");
        }

        [Test]
        public void CaiBang_AddSkillDamage_IsPassiveDamageAmp_NotChainSpawn()
        {
            // PC KSkillList::GetAddSkillDamage(nSkillID) + KNpc::AppendSkillEffect: addskilldamage is a
            // passive flat %-damage amplifier on the CAST skill, summed from LEARNED skills whose
            // addskilldamage entries target it. No proc chance, no sub-skill missiles.
            // Beggar() has learned 119 (addskilldamage1 → 359, +40% L20) and 125 (addskilldamage1 →
            // 359, +60% L20). Casting 359 must therefore get +100% and spawn only its own 3 missiles.
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var beggar = Beggar();
            beggar.knownSkills.Add(359);
            beggar.skillLevels[359] = 20;
            var enemy = Enemy(new Vector2(300, 0));

            var r = svc.Cast(beggar, enemy, 359, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(100, r.addSkillDamagePercent,
                "addskilldamage sums learned grants targeting 359: 119(+40) + 125(+60) = 100");
            Assert.AreEqual(3, r.childProjectileCount, "359 still spawns only its own 3 missiles");
            Assert.AreEqual(3, r.projectiles.Count);
            Assert.That(r.projectiles.All(p => p.skillId == 168), Is.True,
                "no chain sub-skill missiles — only 359's own PC missile 168");
        }

        [Test]
        public void CaiBang_AddSkillDamage_ZeroWhenGrantSkillNotLearned()
        {
            // Same cast, but caster has NOT learned any skill that grants addskilldamage to 359.
            var deterministicDamage = new DamageFormulaService { RollPercent = _ => true };
            var svc = new CombatRuntimeService(Catalog(), damage: deterministicDamage);
            var caster = new CombatActorState
            {
                actorId = 3,
                faction = CombatFaction.CaiBang,
                level = 60,
                currentLife = 1000,
                currentMana = 500,
                position = Vector2.zero,
                knownSkills = { 359 },
                skillLevels = { [359] = 20 },
            };
            var enemy = Enemy(new Vector2(300, 0));

            var r = svc.Cast(caster, enemy, 359, enemy.position, CombatRelation.Enemy);
            Assert.IsTrue(r.success, r.detail);
            Assert.AreEqual(0, r.addSkillDamagePercent, "no learned grant skill targets 359 → no bonus");
            Assert.AreEqual(3, r.projectiles.Count);
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
            Assert.AreEqual(SkillMissileForm.Stationary, skill1072.missileForm,
                "canonical slistcache row 1072 uses stationary missile form 7");
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
        public void CaiBang_357_WallFormationUsesPcOriginsAndOneLiveTarget()
        {
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var liveTarget = new Vector2(200, 0);
            var fx = visual.PlaySkillCast(cat.Resolve(357), Vector2.zero, new Vector2(100, 0), 20, () => liveTarget);

            Assert.IsNotNull(fx);
            Assert.AreEqual(4, fx.missileCount, "Phi Long level 20 should spawn four PC wall-form missiles.");
            float[] expectedOffsets = { -64f, -32f, 0f, 32f };
            for (int i = 0; i < fx.missilePositions.Length; i++)
            {
                Assert.AreEqual(expectedOffsets[i], fx.missileOrigins[i].y, 0.001f,
                    "KSkill::CastWall uses -Param1*count/2, then increments by Param1.");
                Assert.AreEqual(liveTarget, fx.ResolveMissileTarget(i),
                    "KMissle MoveKind=5 stores the same followed NPC on every dragon.");
            }
        }

        [Test]
        public void CaiBang_358_TiemLongUsesNewestPcRowDefaultsWhenLuaTableIsCommented()
        {
            // Newest checked PC sources keep gaibang.lua::qianlong_zaiyuan commented out.
            // So row 358 must not borrow Kháng Long (128) Lua data; it uses skills.txt row defaults + missile 167.
            var cat = Catalog();
            var skill = cat.Resolve(358);
            var data = skill.GetPcLevelData(20);

            Assert.AreEqual("Tiềm Long Tại Uyên", skill.DisplayName);
            Assert.AreEqual(167, skill.childSkillId, "PC skills.txt row 358 ChildSkillId=167");
            Assert.AreEqual(SkillMissileForm.Stationary, skill.missileForm, "PC skills.txt row 358 MisslesForm=7");
            Assert.AreEqual(570, skill.attackRadius, "PC skills.txt row 358 AttackRadius=570");
            Assert.AreEqual(5, skill.waitTime, "PC skills.txt row 358 WaitTime=5");
            Assert.AreEqual(11, skill.charAnimId, "PC skills.txt row 358 CharAnimId=11");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetSingleValue(358, 20, "firedamage_v", 1), "qianlong_zaiyuan table is commented out; no Lua firedamage should be parsed");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetSingleValue(358, 20, "seriesdamage_p", 1), "qianlong_zaiyuan table is commented out; no Lua series damage should be parsed");
            Assert.AreEqual("PhysicsEnhanceP=0,0,0", data.First(MagicAttributeKind.PhysicsEnhanceP).ToString());
            Assert.AreEqual("FireDamageV=0,0,0", data.First(MagicAttributeKind.FireDamageV).ToString());
            Assert.AreEqual("SkillCostV=0,0,0", data.First(MagicAttributeKind.SkillCostV).ToString());
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
        public void CaiBang_1539_VisualServiceUsesPcMissile168HomingSpeed()
        {
            // Newest PC row 1539 is Thiên Hạ Vô Cẩu NPC variant: child missile 168, Lua `tianxia_wugou`.
            var cat = Catalog();
            var visual = new SkillEffectVisualService(null, cat);
            var beggar = Beggar();
            beggar.knownSkills.Add(1539);
            beggar.skillLevels[1539] = 20;
            var enemy = Enemy(new Vector2(400, 0));
            var fx = visual.PlaySkillCast(cat.Resolve(1539), beggar.position, enemy.position, 20);
            Assert.IsNotNull(fx);
            Assert.AreEqual(24, fx.pcMissileSpeedPerTick, "PC Lua tianxia_wugou missle_speed_v L20=24");
            Assert.AreEqual(32, fx.pcMissileLifeTicks, "PC missile 168 LifeTime=32");
            Assert.AreEqual(3, fx.missileCount, "L20 1539 spawns 3 homing missiles like 359");
        }
    }
}
