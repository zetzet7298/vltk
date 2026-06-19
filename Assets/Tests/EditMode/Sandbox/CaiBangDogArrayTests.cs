using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // [CaiBang-DogArray 2026-06-19] Phase E: 打狗阵 (124) stance ally chain.
    // PC source: 打狗阵.lua adddefense_v(level) = 30+10*level (param1), 25 mana (param2), 0 (param3).
    // PC stance aura: apply state 44 (AddDefenseV) cho caster + allies trong AttackRadius=180.
    // Trước fix: chỉ buff self, allies không nhận → sai PC semantic.
    // Sau fix: PropagateAllyAura iterate AllyFinder → apply state 44 cho mỗi ally trong radius.
    [TestFixture, Category("CaiBang")]
    public class CaiBangDogArrayTests
    {
        private static readonly SkillCatalog _catalog = TestCatalogCache.NoviceAndCaiBang;
        private SkillCatalog Catalog() => _catalog;

        private CombatActorState Beggar(Vector2 pos, int level = 20) => new CombatActorState
        {
            actorId = 1,
            faction = CombatFaction.CaiBang,
            level = level,
            fightMode = true,
            currentMana = 1000,
            position = pos,
            knownSkills = { 124 },
            skillLevels = { [124] = 20 },
        };

        private CombatActorState Ally(Vector2 pos, int actorId = 2) => new CombatActorState
        {
            actorId = actorId,
            faction = CombatFaction.CaiBang,
            level = 20,
            fightMode = true,
            currentLife = 500,
            position = pos,
        };

        private CombatActorState EnemyAlly(Vector2 pos, int actorId = 3) => new CombatActorState
        {
            actorId = actorId,
            faction = CombatFaction.TianRen, // hostile faction (Thiên Nhẫn)
            level = 20,
            currentLife = 500,
            position = pos,
        };

        [Test]
        public void DogArray_StanceConfig_PcAccurate()
        {
            // 打狗阵 stance config đúng PC source:
            // SkillStyle=2 (InitiativeNpcState), IsAura=1, TargetAlly=1, AttackRadius=180,
            // StateSpecialId=44, CharAnimId=14, WaitTime=0.
            var cat = Catalog();
            var s = cat.Resolve(124);
            Assert.IsNotNull(s);
            Assert.AreEqual(PcSkillStyle.InitiativeNpcState, s.skillStyle);
            Assert.IsTrue(s.isAura);
            Assert.IsTrue(s.targetAlly);
            Assert.IsTrue(s.targetSelf);
            Assert.AreEqual(180, s.attackRadius);
            Assert.AreEqual(44, s.stateSpecialId);
            Assert.AreEqual(14, s.charAnimId);
            Assert.AreEqual(0, s.waitTime);
        }

        [Test]
        public void DogArray_Cast_AppliesSelfBuff()
        {
            // 打狗阵 stance cast: self nhận state 44 AddDefenseV (caster đã có sẵn qua ApplyStates).
            var svc = new CombatRuntimeService(Catalog());
            var caster = Beggar(Vector2.zero);
            caster.currentMana = 1000;
            var r = svc.Cast(caster, caster, 124, caster.position, CombatRelation.Self);
            Assert.IsTrue(r.success, r.detail);
            // caster.states[AddDefenseV] should be applied
            Assert.IsTrue(caster.states.ContainsKey(MagicAttributeKind.AddDefenseV),
                "caster nhận state 44 AddDefenseV từ 打狗阵 stance");
            var attr = caster.states[MagicAttributeKind.AddDefenseV];
            Assert.AreEqual(230, attr.value1, "L20 AddDefenseV = 30+10*20 = 230");
        }

        [Test]
        public void DogArray_AllyInRange_ReceivesBuff()
        {
            // 打狗阵 stance chain buff ally trong radius 180.
            var svc = new CombatRuntimeService(Catalog());
            var caster = Beggar(Vector2.zero, level: 20);
            var ally = Ally(new Vector2(50, 0)); // trong radius 180
            // Setup AllyFinder: filter giống GameplayLoopService.FindAlliesInRange (radius + faction).
            svc.AllyFinder = (center, radiusWu) =>
            {
                var list = new List<CombatActorState>();
                if (ally.faction == caster.faction && Vector2.Distance(center, ally.position) <= radiusWu && ally.currentLife > 0)
                    list.Add(ally);
                return list;
            };
            var r = svc.Cast(caster, caster, 124, caster.position, CombatRelation.Self);
            Assert.IsTrue(r.success, r.detail);
            // Ally nhận state 44 AddDefenseV
            Assert.IsTrue(ally.states.ContainsKey(MagicAttributeKind.AddDefenseV),
                "ally trong radius 180 nhận state 44 AddDefenseV từ 打狗阵 stance");
            Assert.AreEqual(230, ally.states[MagicAttributeKind.AddDefenseV].value1,
                "ally L20 AddDefenseV = 30+10*20 = 230");
        }

        [Test]
        public void DogArray_AllyOutOfRange_DoesNotReceiveBuff()
        {
            // 打狗阵 stance: ally ngoài radius KHÔNG nhận buff.
            var svc = new CombatRuntimeService(Catalog());
            var caster = Beggar(Vector2.zero, level: 20);
            var farAlly = Ally(new Vector2(500, 0)); // ngoài radius 180
            svc.AllyFinder = (center, radiusWu) =>
            {
                var list = new List<CombatActorState>();
                if (farAlly.faction == caster.faction && Vector2.Distance(center, farAlly.position) <= radiusWu && farAlly.currentLife > 0)
                    list.Add(farAlly);
                return list;
            };
            var r = svc.Cast(caster, caster, 124, caster.position, CombatRelation.Self);
            Assert.IsTrue(r.success, r.detail);
            Assert.IsFalse(farAlly.states.ContainsKey(MagicAttributeKind.AddDefenseV),
                "ally ngoài radius 180 KHÔNG nhận buff");
        }

        [Test]
        public void DogArray_HostileAlly_DoesNotReceiveBuff()
        {
            // 打狗阵 stance chỉ buff đồng đội (cùng faction). Enemy faction không nhận buff.
            var svc = new CombatRuntimeService(Catalog());
            var caster = Beggar(Vector2.zero, level: 20);
            var hostile = EnemyAlly(new Vector2(50, 0)); // khác faction
            svc.AllyFinder = (center, radiusWu) =>
            {
                var list = new List<CombatActorState>();
                if (hostile.faction == caster.faction && Vector2.Distance(center, hostile.position) <= radiusWu && hostile.currentLife > 0)
                    list.Add(hostile);
                return list;
            };
            var r = svc.Cast(caster, caster, 124, caster.position, CombatRelation.Self);
            Assert.IsTrue(r.success, r.detail);
            Assert.IsFalse(hostile.states.ContainsKey(MagicAttributeKind.AddDefenseV),
                "ally khác faction KHÔNG nhận 打狗阵 buff");
        }
    }
}
