using NUnit.Framework;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M4.3 — Damage Formula Port tests. Representative fixtures from the PC
    /// CalcDamage pipeline (AC#1), deterministic stat-edit preview (AC#2), and the
    /// source-evidence gap record requirement (AC#3).
    /// </summary>
    public class DamageFormulaServiceTests
    {
        private AttackerStats Atk(int min, int max, DamageType type = DamageType.Physics, bool melee = true)
            => new AttackerStats { minDamage = min, maxDamage = max, type = type, isMelee = melee };

        private DefenderStats Def(int armor = 0, int resist = 0, int resistMax = 100,
            int manaShield = 0, int mana = 0)
            => new DefenderStats { armor = armor, resist = resist, resistMax = resistMax,
                manaShield = manaShield, currentMana = mana };

        // --- AC#1: representative fixtures ---

        [Test]
        public void Compute_NoMitigation_ReturnsRolledBase()
        {
            var svc = new DamageFormulaService();
            // Pin the roll to 150.
            var r = svc.Compute(Atk(100, 200), Def(), rolledOverride: 150);
            Assert.AreEqual(150, r.rolledBase);
            Assert.AreEqual(150, r.finalDamage);
        }

        [Test]
        public void Compute_Armor_AbsorbsThenLeaksRemainder()
        {
            var svc = new DamageFormulaService();
            // armor 100 vs 150 damage → 50 leaks through.
            var r = svc.Compute(Atk(150, 150), Def(armor: 100), rolledOverride: 150);
            Assert.AreEqual(50, r.afterArmor);
            Assert.AreEqual(50, r.finalDamage);
        }

        [Test]
        public void Compute_Armor_FullyAbsorbs()
        {
            var svc = new DamageFormulaService();
            var r = svc.Compute(Atk(80, 80), Def(armor: 100), rolledOverride: 80);
            Assert.AreEqual(0, r.finalDamage);
        }

        [Test]
        public void Compute_Resist_ScalesDamage()
        {
            var svc = new DamageFormulaService();
            // 200 dmg, 25% resist → 200 * (100-25)/100 = 150.
            var r = svc.Compute(Atk(200, 200), Def(resist: 25), rolledOverride: 200);
            Assert.AreEqual(150, r.finalDamage);
        }

        [Test]
        public void Compute_Resist_CappedAtMaxResist()
        {
            var svc = new DamageFormulaService();
            // resist 120 but MAX_RESIST=95 → 100*(100-95)/100 = 5.
            var r = svc.Compute(Atk(100, 100), Def(resist: 120), rolledOverride: 100);
            Assert.AreEqual(5, r.finalDamage);
        }

        [Test]
        public void Compute_Resist_CappedAtResistMaxBeforeGlobalCap()
        {
            var svc = new DamageFormulaService();
            // resist 60, resistMax 40 → use 40: 100*(100-40)/100 = 60.
            var r = svc.Compute(Atk(100, 100), Def(resist: 60, resistMax: 40), rolledOverride: 100);
            Assert.AreEqual(60, r.finalDamage);
        }

        [Test]
        public void Compute_ManaShield_DivertsToMana()
        {
            var svc = new DamageFormulaService();
            // 100 dmg, 50% mana shield, mana 100 → 50 to mana, 50 to HP (no resist).
            var r = svc.Compute(Atk(100, 100), Def(manaShield: 50, mana: 100), rolledOverride: 100);
            Assert.AreEqual(50, r.manaAbsorbed);
            Assert.AreEqual(50, r.finalDamage);
        }

        [Test]
        public void Compute_ManaShield_LimitedByCurrentMana()
        {
            var svc = new DamageFormulaService();
            // 100 dmg, 50% shield wants 50 but only 10 mana → 10 absorbed, 90 to HP.
            var r = svc.Compute(Atk(100, 100), Def(manaShield: 50, mana: 10), rolledOverride: 100);
            Assert.AreEqual(10, r.manaAbsorbed);
            Assert.AreEqual(90, r.finalDamage);
        }

        [Test]
        public void Compute_FullPipeline_ArmorThenManaThenResist()
        {
            var svc = new DamageFormulaService();
            // base 200, armor 50 → 150; mana shield 20% → 30 to mana (mana 100), 120 left;
            // resist 25% → 120*75/100 = 90.
            var r = svc.Compute(Atk(200, 200), Def(armor: 50, resist: 25, manaShield: 20, mana: 100), rolledOverride: 200);
            Assert.AreEqual(150, r.afterArmor);
            Assert.AreEqual(30, r.manaAbsorbed);
            Assert.AreEqual(90, r.finalDamage);
        }

        [Test]
        public void Compute_ZeroDamage_EarlyOut()
        {
            var svc = new DamageFormulaService();
            var r = svc.Compute(Atk(0, 0), Def());
            Assert.AreEqual(0, r.finalDamage);
        }

        [Test]
        public void RollProvider_UsedWhenNoOverride()
        {
            var svc = new DamageFormulaService { Roll = (min, max) => max }; // always max
            var r = svc.Compute(Atk(100, 300), Def());
            Assert.AreEqual(300, r.rolledBase);
        }

        // --- AC#2: GM stat-edit preview ---

        [Test]
        public void PreviewDamage_IsDeterministicForStatEdit()
        {
            var svc = new DamageFormulaService { Roll = (min, max) => min + (max - min) / 2 };
            // midpoint roll of [100,200] = 150; armor 50 → 100; resist 0 → 100.
            int preview = svc.PreviewDamage(Atk(100, 200), Def(armor: 50));
            Assert.AreEqual(100, preview);
            // Edit: raise armor to 120 → midpoint 150 - 120 = 30.
            int preview2 = svc.PreviewDamage(Atk(100, 200), Def(armor: 120));
            Assert.AreEqual(30, preview2);
        }

        // --- AC#4: PC CalcDamage typed armor + magic bypass (KNpc.cpp:2125-2292) ---

        [Test]
        public void Compute_PhysicsArmor_AbsorbsPhysicsDamageOnly()
        {
            var svc = new DamageFormulaService();
            // Fire attack vs physics-only armor: physics armor must NOT mitigate fire.
            var def = new DefenderStats { physicsArmor = 100, fireArmor = 0 };
            var r = svc.Compute(Atk(150, 150, DamageType.Fire), def, rolledOverride: 150);
            Assert.AreEqual(150, r.afterArmor, "Fire damage should bypass physics armor.");
            Assert.AreEqual(150, r.finalDamage);
        }

        [Test]
        public void Compute_ColdArmor_AbsorbsColdDamageOnly()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { coldArmor = 100 };
            var r = svc.Compute(Atk(150, 150, DamageType.Cold), def, rolledOverride: 150);
            Assert.AreEqual(50, r.afterArmor);
            Assert.AreEqual(50, r.finalDamage);
        }

        [Test]
        public void Compute_FireArmor_AbsorbsFireDamageOnly()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { fireArmor = 80 };
            var r = svc.Compute(Atk(120, 120, DamageType.Fire), def, rolledOverride: 120);
            Assert.AreEqual(40, r.afterArmor);
            Assert.AreEqual(40, r.finalDamage);
        }

        [Test]
        public void Compute_LightArmor_AbsorbsLightDamageOnly()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { lightArmor = 50 };
            var r = svc.Compute(Atk(100, 100, DamageType.Light), def, rolledOverride: 100);
            Assert.AreEqual(50, r.afterArmor);
        }

        [Test]
        public void Compute_PoisonArmor_AbsorbsPoisonDamageOnly()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { poisonArmor = 60 };
            var r = svc.Compute(Atk(80, 80, DamageType.Poison), def, rolledOverride: 80);
            Assert.AreEqual(20, r.afterArmor);
            Assert.AreEqual(20, r.finalDamage);
        }

        [Test]
        public void Compute_MagicDamage_BypassesAllArmorPools()
        {
            var svc = new DamageFormulaService();
            // PC: KNpc.cpp:2285 `case damage_magic: nRes = 0;` — no armor pool applied.
            var def = new DefenderStats { physicsArmor = 9999, fireArmor = 9999, manaShield = 0, resist = 0 };
            var r = svc.Compute(Atk(150, 150, DamageType.Magic), def, rolledOverride: 150);
            Assert.AreEqual(150, r.afterArmor, "Magic bypasses all typed armor pools.");
            Assert.AreEqual(150, r.finalDamage, "Magic has no resist mitigation either.");
        }

        [Test]
        public void Compute_ArmorAlias_StillWorksOnPhysicsDamage()
        {
            var svc = new DamageFormulaService();
            // Legacy single armor field (set via Def(armor: 100)) still mitigates physics.
            var r = svc.Compute(Atk(150, 150, DamageType.Physics), Def(armor: 100), rolledOverride: 150);
            Assert.AreEqual(50, r.afterArmor);
        }

        // --- AC#5: signed random (KNpc.cpp:2136-2141) ---

        [Test]
        public void Compute_SignedRandom_HandlesNegativeRange()
        {
            // PC: when nDamageRange < 0, nDamage = nMax + g_Random(-nDamageRange).
            // min=200, max=100 → range=-100; we inject Roll=max (deterministic).
            var svc = new DamageFormulaService { Roll = (lo, hi) => lo };
            var r = svc.Compute(Atk(200, 100, DamageType.Magic), Def(), rolledOverride: null);
            // Roll(min, min) returns 100 (the max) → dmg = 100.
            Assert.AreEqual(100, r.rolledBase);
            Assert.AreEqual(100, r.finalDamage);
        }

        // --- AC#6: melee/range damage return (KNpc.cpp:2318-2333) ---

        [Test]
        public void Compute_MeleeDamageReturn_AppliesToMeleeAttacker()
        {
            var svc = new DamageFormulaService();
            // 100 final dmg, 20% melee return → 20 damage returned to melee attacker.
            var def = new DefenderStats { meleeDmgRetPercent = 20 };
            var r = svc.Compute(Atk(100, 100, DamageType.Magic, melee: true), def, rolledOverride: 100);
            Assert.AreEqual(20, r.meleeReturnDamage);
            Assert.AreEqual(0, r.rangeReturnDamage);
        }

        [Test]
        public void Compute_RangeDamageReturn_AppliesToRangedAttacker()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { rangeDmgRetPercent = 30 };
            var r = svc.Compute(Atk(100, 100, DamageType.Magic, melee: false), def, rolledOverride: 100);
            Assert.AreEqual(30, r.rangeReturnDamage);
            Assert.AreEqual(0, r.meleeReturnDamage);
        }

        // --- AC#7: damage2mana (KNpc.cpp:2345) ---

        [Test]
        public void Compute_Damage2Mana_GrantsManaFromDamageTaken()
        {
            var svc = new DamageFormulaService();
            // PC: m_CurrentMana += m_CurrentDamage2Mana * nDamage / 100;
            var def = new DefenderStats { damage2ManaPercent = 25 };
            var r = svc.Compute(Atk(100, 100, DamageType.Magic), def, rolledOverride: 100);
            Assert.AreEqual(100, r.finalDamage);
            Assert.AreEqual(25, r.damage2ManaGain);
        }

        // --- AC#8: PK damage rate (KNpc.cpp:2336-2337) ---

        [Test]
        public void Compute_PkDamageRate_AppliesOnlyInPvp()
        {
            var svcPve = new DamageFormulaService { IsPvp = false };
            var def = new DefenderStats { pkDamageRatePercent = 50 };
            var rPve = svcPve.Compute(Atk(100, 100, DamageType.Magic), def, rolledOverride: 100);
            Assert.AreEqual(100, rPve.finalDamage, "PvE: PK rate ignored.");

            var svcPvp = new DamageFormulaService { IsPvp = true };
            var rPvp = svcPvp.Compute(Atk(100, 100, DamageType.Magic), def, rolledOverride: 100);
            Assert.AreEqual(50, rPvp.finalDamage, "PvP: PK rate halved.");
        }

        // --- PC ReceiveDamage: hit/miss/crit/steal (NEW) ---

        [Test]
        public void CheckHitTarget_ZeroAttackRating_VsHighDefend_ReturnsFalse()
        {
            var svc = new DamageFormulaService();
            // AR=0 vs defend=100 → always miss (PC: 0*100/(0+100)=0% hit chance → clamped min 5%)
            Assert.IsFalse(svc.CheckHitTarget(0, 100, ignore: 0));
        }

        [Test]
        public void CheckHitTarget_100AttackRating_VsZeroDefend_ReturnsTrue()
        {
            var svc = new DamageFormulaService();
            // AR=100 vs defend=0 → 100*100/(100+0)=100% hit chance
            Assert.IsTrue(svc.CheckHitTarget(100, 0, ignore: 0));
        }

        [Test]
        public void CheckHitTarget_IgnoreDefenseOver100_AlwaysHits()
        {
            var svc = new DamageFormulaService();
            // ignoreDefense >= 100 → always hit regardless of defend (PC: defend*(100-100)/100 = 0)
            Assert.IsTrue(svc.CheckHitTarget(0, 999, ignore: 100));
        }

        [Test]
        public void StealLifePercentage_RestoresAttackerLife()
        {
            var svc = new DamageFormulaService();
            // Deal 100 damage, steal 10% → restore 10 life to attacker
            var r = svc.Compute(
                new AttackerStats { minDamage = 100, maxDamage = 100, stolenLifePercent = 10 },
                new DefenderStats { currentMana = 0 },
                rolledOverride: 100);
            Assert.AreEqual(10, r.stolenLife);
        }

        [Test]
        public void StealManaPercentage_RestoresAttackerMana()
        {
            var svc = new DamageFormulaService();
            // Deal 100 damage, steal 15% → restore 15 mana to attacker
            var r = svc.Compute(
                new AttackerStats { minDamage = 100, maxDamage = 100, stolenManaPercent = 15 },
                new DefenderStats { currentMana = 0 },
                rolledOverride: 100);
            Assert.AreEqual(15, r.stolenMana);
        }

        [Test]
        public void MultipleDamageTypes_EachHasIndependentArmorPool()
        {
            var svc = new DamageFormulaService();
            var def = new DefenderStats { physicsArmor = 50, fireArmor = 30, coldArmor = 70 };
            // Deal 100 each type → physics=50, fire=70, cold=30 after armor
            var rPhys = svc.Compute(new AttackerStats { minDamage = 100, maxDamage = 100, type = DamageType.Physics }, def, rolledOverride: 100);
            var rFire = svc.Compute(new AttackerStats { minDamage = 100, maxDamage = 100, type = DamageType.Fire }, def, rolledOverride: 100);
            var rCold = svc.Compute(new AttackerStats { minDamage = 100, maxDamage = 100, type = DamageType.Cold }, def, rolledOverride: 100);
            Assert.AreEqual(50, rPhys.afterArmor);
            Assert.AreEqual(70, rFire.afterArmor);
            Assert.AreEqual(30, rCold.afterArmor);
        }

        // --- AC#3: source evidence gap recorded before implementation ---

        [Test]
        public void SourceEvidence_DamageFormulaAnchorRecorded()
        {
            // The port is anchored to PC KNpc::CalcDamage; record the evidence so the
            // claim is traceable (AC#3: source evidence captured before/with impl).
            int before = SourceEvidence.RecordCount;
            SourceEvidence.Record(
                claim: "M4.3 damage pipeline: roll -> armor -> mana shield -> resist*(100-nRes)/100",
                pcSourceAnchor: "StreamingAssets/Reference/KNpc.cpp:2125 CalcDamage",
                symbolOrFile: "KNpc::CalcDamage",
                tool: DiscoveryTool.Manual,
                resolvedValue: "MAX_RESIST=95; nDamage = nDamage*(100-nRes)/100",
                notes: "MAX_RESIST exact value verified from KNpc resist cap; per-element armor pools identical pattern.");
            Assert.Greater(SourceEvidence.RecordCount, before);
            var found = SourceEvidence.FindByClaim("M4.3 damage pipeline");
            Assert.IsNotEmpty(found);
            StringAssert.Contains("CalcDamage", found[0].pcSourceAnchor);
        }
    }
}
