// -----------------------------------------------------------------------------
// SurvivorCollectTests — ticket 32 self-check (EditMode pure logic).
// Cover: rate roll (seed fixed), drop table roll, wave bonus, level curve,
// LevelExpCalc.AddExp, magnet math. KHÔNG scene, KHÔNG PlayMode.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorCollectTests
    {
        // --- rate roll: deterministic theo seed + phân phối đúng ---

        [Test]
        public void TestRate_SameSeed_SameSequence()
        {
            var a = new System.Random(42);
            var b = new System.Random(42);
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(
                    SurvivorCollectItemMgr.TestRate(0.3f, a),
                    SurvivorCollectItemMgr.TestRate(0.3f, b),
                    "same seed phải cho cùng kết quả");
        }

        [Test]
        public void TestRate_Seed42_Rate025_Within250PlusMinus50()
        {
            var rng = new System.Random(42);
            int hits = 0;
            for (int i = 0; i < 1000; i++)
                if (SurvivorCollectItemMgr.TestRate(0.25f, rng)) hits++;
            Assert.GreaterOrEqual(hits, 200, "binomial 1000@0.25: quá thấp");
            Assert.LessOrEqual(hits, 300, "binomial 1000@0.25: quá cao");
        }

        // --- drop table roll ---

        private static DropTableSO MakeTable()
        {
            var t = ScriptableObject.CreateInstance<DropTableSO>();
            t.Entries = new List<DropEntry>
            {
                // pool 1: xp chắc chắn, gold 0% (không bao giờ), xp x2
                new DropEntry { PoolID = 1, ItemID = 101, OutputType = DropOutputType.Xp, Param1 = 5, DropRate = 1f },
                new DropEntry { PoolID = 1, ItemID = 102, OutputType = DropOutputType.Gold, Param1 = 2, DropRate = 0f },
                new DropEntry { PoolID = 1, ItemID = 103, OutputType = DropOutputType.Xp, Param1 = 1, DropRate = 1f, CountMin = 2, CountMax = 2 },
                // pool 2: riêng biệt
                new DropEntry { PoolID = 2, ItemID = 201, OutputType = DropOutputType.Heal, Param1 = 3, DropRate = 1f },
            };
            return t;
        }

        [Test]
        public void RollActorDrop_PerEntryRate_CountAndAmount()
        {
            var mgr = new SurvivorCollectItemMgr(MakeTable());
            var drops = mgr.RollActorDrop(1, new System.Random(7));
            Assert.AreEqual(3, drops.Count, "entry 101 + 103 x2 (102 rate=0 loại)");
            int xpTotal = 0, goldTotal = 0;
            foreach (var d in drops)
            {
                if (d.OutputType == DropOutputType.Xp) xpTotal += d.Amount;
                if (d.OutputType == DropOutputType.Gold) goldTotal += d.Amount;
            }
            Assert.AreEqual(7, xpTotal, "5 + 1 + 1");
            Assert.AreEqual(0, goldTotal, "rate 0 không bao giờ rơi");
        }

        [Test]
        public void RollActorDrop_FiltersPool()
        {
            var mgr = new SurvivorCollectItemMgr(MakeTable());
            var drops = mgr.RollActorDrop(2, new System.Random(7));
            Assert.AreEqual(1, drops.Count);
            Assert.AreEqual(DropOutputType.Heal, drops[0].OutputType);
            Assert.AreEqual(3, drops[0].Amount);
            Assert.AreEqual(201, drops[0].ItemID);
        }

        [Test]
        public void RollWaveBonus_ForcesAllEntries()
        {
            var mgr = new SurvivorCollectItemMgr(MakeTable());
            var drops = mgr.RollWaveBonus(1, new System.Random(7));
            Assert.AreEqual(4, drops.Count, "bonus đợt: cả 102 (rate 0) cũng rơi");
        }

        [Test]
        public void RollActorDrop_NullTable_Empty()
        {
            var mgr = new SurvivorCollectItemMgr(null);
            Assert.IsEmpty(mgr.RollActorDrop(1, new System.Random(1)));
        }

        // --- level curve: default giữ P1 5+(L-1)*3 ---

        [Test]
        public void Curve_Linear_Default_5_8_11()
        {
            var c = LevelCurveConfig.Default();
            Assert.AreEqual(5, c.XpToNext(1), "L1");
            Assert.AreEqual(8, c.XpToNext(2), "L2");
            Assert.AreEqual(11, c.XpToNext(3), "L3");
        }

        [Test]
        public void Curve_Step_JumpEvery5()
        {
            var c = LevelCurveConfig.Default();
            c.Kind = XpCurveKind.Step;
            Assert.AreEqual(5, c.XpToNext(1), "L1");
            Assert.AreEqual(5, c.XpToNext(5), "L5");
            Assert.AreEqual(20, c.XpToNext(6), "L6: +3*5");
            Assert.AreEqual(20, c.XpToNext(10), "L10");
            Assert.AreEqual(35, c.XpToNext(11), "L11: +3*10");
        }

        [Test]
        public void Curve_Exponential_Grows()
        {
            var c = LevelCurveConfig.Default();
            c.Kind = XpCurveKind.Exponential;
            Assert.AreEqual(5, c.XpToNext(1), "L1 = base");
            Assert.AreEqual(7, c.XpToNext(2), "L2 = round(5*1.35)");
            Assert.AreEqual(9, c.XpToNext(3), "L3 = round(5*1.35^2)");
        }

        [Test]
        public void Curve_MaxLevel_Cap()
        {
            var c = LevelCurveConfig.Default();
            c.MaxLevel = 10;
            Assert.AreEqual(int.MaxValue, c.XpToNext(10), "cap: không lên nữa");
        }

        // --- LevelExpCalc.AddExp parity-shape ---

        [Test]
        public void AddExp_SingleLevel_Carryover()
        {
            var c = LevelCurveConfig.Default();
            int xp = 0, level = 1;
            int ups = SurvivorLevelExpCalc.AddExp(ref xp, ref level, 6, c);
            Assert.AreEqual(1, ups, "5 xp đủ lên L2, dư 1");
            Assert.AreEqual(2, level);
            Assert.AreEqual(1, xp, "carryover");
        }

        [Test]
        public void AddExp_MultiLevel_Chain()
        {
            var c = LevelCurveConfig.Default();
            int xp = 0, level = 1;
            int ups = SurvivorLevelExpCalc.AddExp(ref xp, ref level, 5 + 8 + 11 + 2, c);
            Assert.AreEqual(3, ups, "5+8+11 chain lên L4, dư 2");
            Assert.AreEqual(4, level);
            Assert.AreEqual(2, xp);
        }

        [Test]
        public void AddExp_ZeroOrNegative_NoChange()
        {
            var c = LevelCurveConfig.Default();
            int xp = 0, level = 1;
            Assert.AreEqual(0, SurvivorLevelExpCalc.AddExp(ref xp, ref level, 0, c));
            Assert.AreEqual(0, SurvivorLevelExpCalc.AddExp(ref xp, ref level, -3, c));
            Assert.AreEqual(1, level);
            Assert.AreEqual(0, xp);
        }

        // --- magnet math ---

        private static CollectSettings Magnet()
        {
            var s = CollectSettings.Default();
            s.MagnetSpeed = 8f;
            s.PickupDistance = 0.4f;
            return s;
        }

        [Test]
        public void Magnet_OutsideRadius_NoMove()
        {
            var s = Magnet();
            s.MagnetRadius = 1.6f;
            bool picked = MagnetMath.Pull(Vector2.zero, new Vector2(5f, 0f), s, 1f, out var pos);
            Assert.IsFalse(picked);
            Assert.AreEqual(Vector2.zero, pos, "ngoài radius: đứng yên");
        }

        [Test]
        public void Magnet_InsideRadius_ClampedStep_NoOvershoot()
        {
            var s = Magnet();
            s.MagnetRadius = 1.6f;
            // gem (4.5,0) → player (5,0): dist 0.5, step = min(8*1, 0.5-0.4) = 0.1
            bool picked = MagnetMath.Pull(new Vector2(4.5f, 0f), new Vector2(5f, 0f), s, 1f, out var pos);
            Assert.IsFalse(picked, "chưa tới pickup distance");
            Assert.AreEqual(4.6f, pos.x, 1e-4f, "step clamp không overshoot");
            // frame kế: dist 0.4 → pickup
            bool picked2 = MagnetMath.Pull(pos, new Vector2(5f, 0f), s, 1f, out var pos2);
            Assert.IsTrue(picked2);
            Assert.AreEqual(new Vector2(5f, 0f), pos2);
        }

        [Test]
        public void Magnet_SpeedLimit_SmallDt()
        {
            var s = Magnet();
            s.MagnetRadius = 1.6f;
            // dist 2 (trong radius? không — radius 1.6 → ngoài). dùng radius 3:
            s.MagnetRadius = 3f;
            bool picked = MagnetMath.Pull(new Vector2(3f, 0f), new Vector2(5f, 0f), s, 0.1f, out var pos);
            Assert.IsFalse(picked);
            Assert.AreEqual(3.8f, pos.x, 1e-4f, "speed 8 * dt 0.1 = 0.8");
        }

        [Test]
        public void Magnet_Pickup_WithinPickupDistance()
        {
            var s = Magnet();
            s.MagnetRadius = 1.6f;
            bool picked = MagnetMath.Pull(new Vector2(4.95f, 0f), new Vector2(5f, 0f), s, 1f, out var pos);
            Assert.IsTrue(picked, "dist 0.05 <= 0.4");
            Assert.AreEqual(new Vector2(5f, 0f), pos);
        }

        [Test]
        public void Magnet_Deterministic_SameInputsSameOutput()
        {
            var s = Magnet();
            s.MagnetRadius = 1.6f;
            MagnetMath.Pull(new Vector2(4.5f, 1f), new Vector2(5f, 1f), s, 0.25f, out var a);
            MagnetMath.Pull(new Vector2(4.5f, 1f), new Vector2(5f, 1f), s, 0.25f, out var b);
            Assert.AreEqual(a, b);
        }
    }
}
