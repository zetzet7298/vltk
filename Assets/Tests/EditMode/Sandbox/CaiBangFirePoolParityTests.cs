using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-FirePool 2026-07-17] Independent proof cho the addfiremagic_v / addfiredamage_v split
    // and the skill-117 fail-closed. Every expected value below is derived from canonical PC source,
    // NOT from the production catalog/runtime, so these tests fail on the pre-fix implementation and
    // pass after the fix:
    //   - PC gaibang.lua (jx-source .../bin/client/script/skill/gaibang.lua):
    //       gaibang_zhangfa:  addfiremagic_v={{{1,25},{20,275}}}            -> 116 grants magic pool
    //       zuidie_kuangwu:   addfiremagic_v={{{1,10},{30,215}}}            -> L20 floors to 144
    //                         addfiredamage_v={{{1,10},{30,175}}}           -> L20 floors to 118
    //   - PC skills.txt (jx-source pak_unpacked/update03/settings/skills.txt):
    //       row 117 IsPhysical=0, LvlData1="skill_cost_v" only (no yanmen_tuobo reference)
    //       row 119 IsPhysical=1, LvlData1="yanmen_tuobo" (legitimate)
    //   - PC KNpc damage branch (jx-source extracted_full.tar:3183-3192): fire damage consumes
    //       m_CurrentFireMagic (!bIsPhysical) or m_CurrentFireDamage (bIsPhysical).
    [TestFixture, Category("CaiBang")]
    public class CaiBangFirePoolParityTests
    {
        private SkillCatalog Catalog() => PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();

        private static CombatActorState Beggar() => new CombatActorState
        {
            actorId = 2,
            faction = CombatFaction.CaiBang,
            level = 60,
            fightMode = true,
            currentMana = 500,
            position = Vector2.zero,
            knownSkills = { 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126, 127, 128, 129, 130 },
            skillLevels = { [117] = 20, [119] = 20, [130] = 20 },
        };

        private static CombatActorState Enemy(Vector2 pos) =>
            new CombatActorState { actorId = 9, faction = CombatFaction.None, level = 1, currentLife = 100000, position = pos };

        private static SkillMagicAttribute Attr(MagicAttributeKind kind, int magnitude) =>
            new SkillMagicAttribute(kind, magnitude, -1, magnitude);

        // ---- Model: the two fire-add kinds must be distinct enum members. ----
        [Test]
        public void Model_SeparatesAddFireMagicVFromAddFireDamageV()
        {
            // Pre-fix: a single AddFireDamageV represented BOTH PC names. The new AddFireMagicV member
            // must exist and be a distinct value, otherwise the parser/runtime split is impossible.
            Assert.IsTrue(System.Enum.IsDefined(typeof(MagicAttributeKind), nameof(MagicAttributeKind.AddFireMagicV)),
                "AddFireMagicV must be its own enum member (PC addfiremagic_v)");
            Assert.AreNotEqual((int)MagicAttributeKind.AddFireMagicV, (int)MagicAttributeKind.AddFireDamageV,
                "AddFireMagicV and AddFireDamageV must not alias the same enum value");
        }

        // ---- Catalog: 116 grants the MAGIC fire pool (PC gaibang.lua::gaibang_zhangfa addfiremagic_v). ----
        [Test]
        public void CaiBang116_ChuongPhapa_GrantsAddFireMagicV_NotAddFireDamageV()
        {
            // PC gaibang.lua line 16-18: gaibang_zhangfa grants addfiremagic_v={{{1,25},{20,275}}}.
            // Pre-fix catalog emitted AddFireDamageV=275,-1,9 (conflated). Post-fix: AddFireMagicV.
            var data = Catalog().Resolve(116).GetPcLevelData(20);
            Assert.IsTrue(data.state.Any(a => a.kind == MagicAttributeKind.AddFireMagicV),
                "116 must grant the magic fire pool AddFireMagicV (PC addfiremagic_v)");
            Assert.IsFalse(data.state.Any(a => a.kind == MagicAttributeKind.AddFireDamageV),
                "116 must NOT grant the physical fire pool AddFireDamageV");
            var magic = data.First(MagicAttributeKind.AddFireMagicV);
            Assert.AreEqual(275, magic.value1, "PC gaibang_zhangfa L20 addfiremagic_v=275");
        }

        // ---- Catalog: 130 grants BOTH canonical fire pools, not a conflated FireDamageV. ----
        [Test]
        public void CaiBang130_ZuidieKuangwu_GrantsBothCanonicalFirePools()
        {
            // PC gaibang.lua::zuidie_kuangwu:
            //   addfiremagic_v={{{1,10},{30,215}}}  -> L20 = floor(10 + 205*19/29) = 144 -> AddFireMagicV
            //   addfiredamage_v={{{1,10},{30,175}}} -> L20 = floor(10 + 165*19/29) = 118 -> AddFireDamageV
            // Pre-fix catalog mislabeled addfiredamage_v as FireDamageV (base) and addfiremagic_v as AddFireDamageV.
            var data = Catalog().Resolve(130).GetPcLevelData(20);
            Assert.IsFalse(data.state.Any(a => a.kind == MagicAttributeKind.FireDamageV),
                "130 has no base firedamage_v; the 175 curve is addfiredamage_v (Add pool), not FireDamageV");
            Assert.IsTrue(data.state.Any(a => a.kind == MagicAttributeKind.AddFireMagicV),
                "130 must grant AddFireMagicV (PC addfiremagic_v)");
            Assert.IsTrue(data.state.Any(a => a.kind == MagicAttributeKind.AddFireDamageV),
                "130 must grant AddFireDamageV (PC addfiredamage_v)");
            Assert.AreEqual(144, data.First(MagicAttributeKind.AddFireMagicV).value1,
                "PC zuidie_kuangwu L20 addfiremagic_v floors to 144");
            Assert.AreEqual(118, data.First(MagicAttributeKind.AddFireDamageV).value1,
                "PC zuidie_kuangwu L20 addfiredamage_v floors to 118");
        }

        // ---- Lua service: 117 fails closed (no fabricated yanmen_tuobo borrow). ----
        [Test]
        public void CaiBang117_LuaServiceFailsClosed_NoYanmenTuoboBorrow()
        {
            // PC skills.txt row 117 LvlData1="skill_cost_v" only (verified jx-source
            // pak_unpacked/update03/settings/skills.txt col 73). Pre-fix mapped 117->"yanmen_tuobo"
            // (skill 119's table), fabricating radius/missile/fire data. Fail closed: not mapped.
            Assert.IsFalse(PcCaiBangLuaLevelService.Applies(117),
                "117 must not be mapped (PC LvlData has no yanmen_tuobo reference)");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetAttackRadius(117, 20),
                "117 fail-closed: no PC level data -> lua returns 0");
            Assert.AreEqual(0, PcCaiBangLuaLevelService.GetMissileSpeed(117, 20),
                "117 fail-closed: no PC level data -> lua returns 0");
            // 119 stays legitimately mapped (PC row 119 LvlData1="yanmen_tuobo").
            Assert.IsTrue(PcCaiBangLuaLevelService.Applies(119),
                "119 remains mapped to yanmen_tuobo (PC LvlData1=yanmen_tuobo)");
            Assert.AreEqual(384, PcCaiBangLuaLevelService.GetAttackRadius(119, 20),
                "119 L20 radius 384 (PC yanmen_tuobo skill_attackradius)");
        }

        // ---- Runtime: a MAGIC fire skill consumes AddFireMagicV, never the physical pool. ----
        // Independent of catalog damage numbers: both fire pools are set with DISTINCT magnitudes and
        // we assert the magic skill's damage depends only on the AddFireMagicV magnitude.
        [Test]
        public void CaiBang117_MagicFire_ConsumesAddFireMagicVPool_IgnoresPhysicalPool()
        {
            // PC: 117 IsPhysical=0 (magic). Canonical fire pool = m_CurrentFireMagic (AddFireMagicV).
            // Pre-fix runtime always read AddFireDamageV for fire regardless of isPhysical, so a magic
            // fire skill absorbed the physical pool and ignored the magic pool.
            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });
            var beggar = Beggar();

            // Both pools present, distinct magnitudes: magic=100, physical=500.
            beggar.states[MagicAttributeKind.AddFireMagicV] = Attr(MagicAttributeKind.AddFireMagicV, 100);
            beggar.states[MagicAttributeKind.AddFireDamageV] = Attr(MagicAttributeKind.AddFireDamageV, 500);
            var e1 = Enemy(new Vector2(200, 0));
            var r1 = svc.Cast(beggar, e1, 117, e1.position, CombatRelation.Enemy);
            Assert.IsTrue(r1.success, r1.detail);
            // PC KSkill::Cast: 117 SkillStyle=Missiles -> damage defers to missile impact.
            Assert.AreEqual(0, r1.damageResults.Count, "117 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260717);
            var m1 = r1.projectiles.First(p => p.skillId == 44);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, e1, r1, m1, e1.position));
            int dmgWithBoth = r1.damageResults.Sum(d => d.finalDamage);
            Assert.Greater(dmgWithBoth, 0, "117 magic fire must deal damage");

            // Drop the PHYSICAL pool only; keep the magic pool identical.
            beggar.states.Remove(MagicAttributeKind.AddFireDamageV);
            svc.AdvanceTime(20);
            var e2 = Enemy(new Vector2(200, 0));
            var r2 = svc.Cast(beggar, e2, 117, e2.position, CombatRelation.Enemy); // identical roll sequence
            Assert.IsTrue(r2.success, r2.detail);
            Assert.AreEqual(0, r2.damageResults.Count, "117 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260717);
            var m2 = r2.projectiles.First(p => p.skillId == 44);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, e2, r2, m2, e2.position));
            int dmgMagicOnly = r2.damageResults.Sum(d => d.finalDamage);

            // Magic fire reads AddFireMagicV(100) in both casts -> identical damage.
            // Pre-fix: cast 1 read AddFireDamageV(500), cast 2 read AddFireDamageV(0) -> very different.
            Assert.AreEqual(dmgMagicOnly, dmgWithBoth,
                "magic fire skill 117 must consume AddFireMagicV; the physical AddFireDamageV magnitude (500) must not change its damage");
        }

        // ---- Runtime: a PHYSICAL fire skill consumes AddFireDamageV (canonical confirmation). ----
        [Test]
        public void CaiBang119_PhysicalFire_ConsumesAddFireDamageVPool()
        {
            // PC: 119 IsPhysical=1 (physical). Canonical fire pool = m_CurrentFireDamage (AddFireDamageV).
            // Sanity confirmation of the physical branch of the isPhysical split (not the distinguishing case).
            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });
            var beggar = Beggar();
            beggar.skillLevels[119] = 20;

            beggar.states[MagicAttributeKind.AddFireMagicV] = Attr(MagicAttributeKind.AddFireMagicV, 500);
            beggar.states[MagicAttributeKind.AddFireDamageV] = Attr(MagicAttributeKind.AddFireDamageV, 100);
            var e1 = Enemy(new Vector2(200, 0));
            var r1 = svc.Cast(beggar, e1, 119, e1.position, CombatRelation.Enemy);
            Assert.IsTrue(r1.success, r1.detail);
            // PC KSkill::Cast: 119 SkillStyle=Missiles -> damage defers to missile impact.
            Assert.AreEqual(0, r1.damageResults.Count, "119 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260717);
            var m1 = r1.projectiles.First(p => p.skillId == 45);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, e1, r1, m1, e1.position));
            int dmgWithBoth = r1.damageResults.Sum(d => d.finalDamage);
            Assert.Greater(dmgWithBoth, 0, "119 physical fire must deal damage");

            beggar.states.Remove(MagicAttributeKind.AddFireMagicV);
            svc.AdvanceTime(20);
            var e2 = Enemy(new Vector2(200, 0));
            var r2 = svc.Cast(beggar, e2, 119, e2.position, CombatRelation.Enemy);
            Assert.IsTrue(r2.success, r2.detail);
            Assert.AreEqual(0, r2.damageResults.Count, "119 Missile-style: damage waits for missile impact");
            UnityEngine.Random.InitState(20260717);
            var m2 = r2.projectiles.First(p => p.skillId == 45);
            Assert.IsTrue(svc.TryResolveProjectileCollision(beggar, e2, r2, m2, e2.position));
            int dmgPhysOnly = r2.damageResults.Sum(d => d.finalDamage);

            // Physical fire reads AddFireDamageV(100) in both casts -> identical damage,
            // confirming the isPhysical split routes physical fire to the damage pool.
            Assert.AreEqual(dmgPhysOnly, dmgWithBoth,
                "physical fire skill 119 must consume AddFireDamageV; the AddFireMagicV magnitude must not change its damage");
        }

        // ---- Normal player path (learn/level/load): NO test-side state injection. ----
        // The two tests below exercise CombatSkillSlotController.MaterializePassiveStates, the exact
        // production helper CreateCombatActor calls when building the player combat actor. The state is
        // PRODUCED from the canonical catalog, never injected. Pre-fix CreateCombatActor had no
        // materialization step, so a player who learned 116 carried no AddFireMagicV -> these fail.

        // Player who LEARNED + LEVELED 116 (and 117 for the cast test). Mirrors CreateCombatActor's
        // knownSkills/skillLevels population from PlayerProgressionState, minus the persistent singleton.
        private static CombatActorState CaiBangPlayer(bool knows116)
        {
            var a = new CombatActorState
            {
                actorId = 2,
                faction = CombatFaction.CaiBang,
                level = 60,
                fightMode = true,
                currentMana = 500,
                position = Vector2.zero,
            };
            a.knownSkills.Add(117);
            a.skillLevels[117] = 20;
            if (knows116)
            {
                a.knownSkills.Add(116);
                a.skillLevels[116] = 20; // PC gaibang_zhangfa L20 addfiremagic_v=275
            }
            return a;
        }

        [Test]
        public void CaiBang116_NormalPlayerLoad_MaterializesAddFireMagicV_NoInjection()
        {
            // PC gaibang.lua::gaibang_zhangfa (skill 116) grants addfiremagic_v={{{1,25},{20,275}}}.
            // Learning + leveling 116 then loading the combat actor must materialize AddFireMagicV into
            // actor.states through the production helper. Pre-fix: actor.states stayed empty.
            var actor = CaiBangPlayer(knows116: true);
            CombatSkillSlotController.MaterializePassiveStates(actor, Catalog());

            Assert.IsTrue(actor.states.TryGetValue(MagicAttributeKind.AddFireMagicV, out var magic),
                "116 (learned, L20) must materialize AddFireMagicV via the normal player path");
            Assert.AreEqual(275, magic.value1, "PC gaibang_zhangfa L20 addfiremagic_v=275");
            Assert.AreEqual(9, magic.value3, "PC gaibang_zhangfa element param 9 (hỏa)");
            Assert.IsFalse(actor.states.ContainsKey(MagicAttributeKind.AddFireDamageV),
                "116 grants the MAGIC fire pool only; the physical AddFireDamageV must NOT appear");
        }

        [Test]
        public void CaiBang116_NormalPlayerLoad_AddsToExistingFireMagicPool_LikePcAccumulator()
        {
            var actor = CaiBangPlayer(knows116: true);
            actor.states[MagicAttributeKind.AddFireMagicV] =
                new SkillMagicAttribute(MagicAttributeKind.AddFireMagicV, 40, 0, 2);

            CombatSkillSlotController.MaterializePassiveStates(actor, Catalog());

            var stacked = actor.states[MagicAttributeKind.AddFireMagicV];
            Assert.AreEqual(315, stacked.value1,
                "PC KNpcAttribModify::AddFireMagicV adds passive magnitude to the current fire-magic pool");
            Assert.AreEqual(11, stacked.value3,
                "PC KNpcAttribModify::AddFireMagicV also adds the element accumulator");
        }

        [Test]
        public void CaiBang116_PersistenceCycle_DoesNotAddPassiveAgain()
        {
            var catalog = Catalog();
            var firstLoad = CaiBangPlayer(knows116: true);
            CombatSkillSlotController.MaterializePassiveStates(firstLoad, catalog);
            Assert.AreEqual(275, firstLoad.states[MagicAttributeKind.AddFireMagicV].value1);

            // Simulate a still-active skill-130 contribution sharing the same PC pool.
            var active130 = catalog.Resolve(130).GetPcLevelData(20).First(MagicAttributeKind.AddFireMagicV);
            firstLoad.states[MagicAttributeKind.AddFireMagicV].value1 += active130.value1;
            firstLoad.states[MagicAttributeKind.AddFireMagicV].value2 += active130.value2;
            firstLoad.states[MagicAttributeKind.AddFireMagicV].value3 += active130.value3;

            var persisted = new System.Collections.Generic.Dictionary<MagicAttributeKind, SkillMagicAttribute>();
            CombatSkillSlotController.PersistStatesWithoutPassiveContributions(firstLoad, catalog, persisted);
            Assert.AreEqual(144, persisted[MagicAttributeKind.AddFireMagicV].value1,
                "writeback must remove learned 116's 275 while preserving active skill-130's 144");

            var secondLoad = CaiBangPlayer(knows116: true);
            foreach (var kvp in persisted)
                secondLoad.states[kvp.Key] = new SkillMagicAttribute(kvp.Value.kind, kvp.Value.value1, kvp.Value.value2, kvp.Value.value3);
            CombatSkillSlotController.MaterializePassiveStates(secondLoad, catalog);

            Assert.AreEqual(419, secondLoad.states[MagicAttributeKind.AddFireMagicV].value1,
                "rehydrate active 144 + materialize passive 275 exactly once; it must not become 694");
        }

        [Test]
        public void CaiBang116_CreateCombatActor_MaterializesLearnedPassiveThroughProductionPath()
        {
            typeof(SandboxManager).GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)?
                .GetSetMethod(true)?.Invoke(null, new object[] { null });

            var controllerGo = new GameObject("CaiBangPassiveController");
            var playerGo = new GameObject("CaiBangPassivePlayer");
            try
            {
                var controller = controllerGo.AddComponent<CombatSkillSlotController>();
                var player = playerGo.AddComponent<SandboxPlayerController>();
                var progression = new PlayerProgressionState
                {
                    faction = CombatFaction.CaiBang,
                    level = 60,
                };
                progression.knownSkills.Add(116);
                progression.knownSkills.Add(117);
                progression.skillLevels[116] = 20;
                progression.skillLevels[117] = 20;

                var catalog = Catalog();
                typeof(CombatSkillSlotController).GetField("_progression", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                    .SetValue(controller, progression);
                typeof(CombatSkillSlotController).GetField("_catalog", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)?
                    .SetValue(controller, catalog);
                var createActor = typeof(CombatSkillSlotController).GetMethod("CreateCombatActor", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Assert.IsNotNull(createActor);

                var actor = createActor.Invoke(controller, new object[] { player, catalog.Resolve(117) }) as CombatActorState;

                Assert.IsNotNull(actor);
                Assert.IsTrue(actor.states.TryGetValue(MagicAttributeKind.AddFireMagicV, out var magic));
                Assert.AreEqual(275, magic.value1);
                Assert.AreEqual(9, magic.value3);
            }
            finally
            {
                Object.DestroyImmediate(controllerGo);
                Object.DestroyImmediate(playerGo);
            }
        }

        [Test]
        public void CaiBang116_NormalPath_AddFireMagicV_Feeds117MagicFireDamage()
        {
            // End-to-end normal path: the AddFireMagicV materialized from learned 116 must raise a
            // magic-fire skill's damage (117 IsPhysical=0). Two identical players, identical seed and
            // enemy; the ONLY difference is whether 116 was learned. No state is injected — both actors
            // are built solely through MaterializePassiveStates.
            var svc = new CombatRuntimeService(Catalog(), damage: new DamageFormulaService { RollPercent = _ => true });

            var without116 = CaiBangPlayer(knows116: false);
            CombatSkillSlotController.MaterializePassiveStates(without116, Catalog());
            Assert.IsFalse(without116.states.ContainsKey(MagicAttributeKind.AddFireMagicV),
                "baseline player has not learned 116 -> no AddFireMagicV materialized");

            var with116 = CaiBangPlayer(knows116: true);
            CombatSkillSlotController.MaterializePassiveStates(with116, Catalog());
            Assert.IsTrue(with116.states.ContainsKey(MagicAttributeKind.AddFireMagicV),
                "player who learned 116 -> AddFireMagicV materialized through the normal path");

            UnityEngine.Random.InitState(20260717);
            var eA = Enemy(new Vector2(200, 0));
            var rA = svc.Cast(without116, eA, 117, eA.position, CombatRelation.Enemy);
            // PC KSkill::Cast: 117 SkillStyle=Missiles -> damage defers to missile impact.
            var mA = rA.projectiles.First(p => p.skillId == 44);
            Assert.IsTrue(svc.TryResolveProjectileCollision(without116, eA, rA, mA, eA.position));
            int dmgA = rA.damageResults.Sum(d => d.finalDamage);
            Assert.Greater(dmgA, 0, "117 baseline magic fire must deal damage");

            UnityEngine.Random.InitState(20260717);
            var eB = Enemy(new Vector2(200, 0));
            var rB = svc.Cast(with116, eB, 117, eB.position, CombatRelation.Enemy);
            var mB = rB.projectiles.First(p => p.skillId == 44);
            Assert.IsTrue(svc.TryResolveProjectileCollision(with116, eB, rB, mB, eB.position));
            int dmgB = rB.damageResults.Sum(d => d.finalDamage);

            Assert.Greater(dmgB, dmgA,
                "the AddFireMagicV materialized from learned 116 must raise 117 magic-fire damage via the normal path");
        }

        [Test]
        public void CaiBang130_ReturnRes_ReducesSkill129MeleeReflection_LikePc()
        {
            // PC KNpcAttribModify.cpp:535-539: meleedamagereturn_p accumulates the
            // defender's percent return pool. KNpc.cpp:2665-2671 then reduces that
            // reflected amount by the ATTACKER's m_CurrentReturnResPercent.
            var catalog = Catalog();
            const int probeSkillId = 990130;
            var probe = new SkillDefinition
            {
                skillId = probeSkillId,
                nameNormalized = "ReturnRes PC probe",
                maxLevel = 1,
                skillStyle = PcSkillStyle.Melee,
                isMelee = true,
                meleeType = PcMeleeType.AttackWithBlur,
                targetEnemy = true,
            };
            var probeLevel = new SkillLevelData { level = 1 };
            probeLevel.damage.Add(new SkillMagicAttribute(MagicAttributeKind.FireDamageV, 100, 0, 100));
            probe.pcLevelData.Add(probeLevel);
            catalog.Register(probe);

            var attacker = new CombatActorState
            {
                actorId = 2,
                faction = CombatFaction.CaiBang,
                level = 60,
                currentLife = 1000,
                maxLife = 1000,
                currentMana = 1000,
                knownSkills = { 130, probeSkillId },
                skillLevels = { [130] = 20, [probeSkillId] = 1 },
            };
            var defender = new CombatActorState
            {
                actorId = 3,
                faction = CombatFaction.CaiBang,
                level = 60,
                currentLife = 1000,
                maxLife = 1000,
                currentMana = 1000,
                knownSkills = { 129 },
                skillLevels = { [129] = 20 },
            };
            var runtime = new CombatRuntimeService(catalog, damage: new DamageFormulaService
            {
                Roll = (min, _) => min,
                RollPercent = _ => true,
            });

            Assert.IsTrue(runtime.Cast(attacker, attacker, 130, attacker.position, CombatRelation.Self).success);
            Assert.IsTrue(runtime.Cast(defender, defender, 129, defender.position, CombatRelation.Self).success);
            Assert.AreEqual(10, attacker.states[MagicAttributeKind.ReturnResP].value1,
                "PC zuidie_kuangwu L20 returnres_p floors to 10");
            Assert.AreEqual(36, defender.states[MagicAttributeKind.MeleeDamageReturnP].value1,
                "PC huaxian_weiyi L20 meleedamagereturn_p floors to 36");

            int lifeBefore = attacker.currentLife;
            var report = runtime.Cast(attacker, defender, probeSkillId, defender.position, CombatRelation.Enemy);
            Assert.IsTrue(report.success, report.detail);
            var damage = report.damageResults.Single();
            int rawReflection = damage.finalDamage * 36 / DamageFormulaService.MaxPercent;
            int pcReflection = rawReflection - rawReflection * 10 / DamageFormulaService.MaxPercent;
            Assert.AreEqual(pcReflection, damage.meleeReturnDamage);
            Assert.AreEqual(lifeBefore - pcReflection, attacker.currentLife,
                "returnres_p must reduce reflected damage before it is applied to the attacker");
        }
    }
}
