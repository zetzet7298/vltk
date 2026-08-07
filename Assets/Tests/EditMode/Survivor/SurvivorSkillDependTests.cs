// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorSkillDependTests
// Phase 1 PORT_CAIBANG_SKILLS_SURVIVOR self-check (pure logic, không scene):
//  - IsDependMet: null/empty → true; 1073 cần 128 ≥ Lv5; 1074 cần 125 ≥ Lv5
//    (lv4 blocked, lv5 unlocked) — parity dhcd RandomSkillDependEntry
//  - SkillChoicePool.Draw cand-filter depend: roster rỗng → chỉ 128/125;
//    128 lv5 → 1073 vào cand; 125 lv5 → 1074 vào cand
//  - FindById helper + IsActive whitelist
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using NUnit.Framework;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorSkillDependTests
    {
        private static SkillDef Def(int id, int maxLevel = 20)
        {
            return SkillDef.FromRow(new SkillRow { Id = id, Name = "S" + id, Form = 7, MaxLevel = maxLevel });
        }

        private static SurvivorSkillLibraryConfig Cfg(int id, List<SurvivorSkillDependEntry> deps = null)
        {
            return new SurvivorSkillLibraryConfig(Def(id), deps);
        }

        private static List<SurvivorSkillDependEntry> Deps(int id, int lv)
        {
            return new List<SurvivorSkillDependEntry> { new SurvivorSkillDependEntry(id, lv) };
        }

        // ------------------------------------------------------------------
        // whitelist (Gap A)
        // ------------------------------------------------------------------

        [Test]
        public void ActiveSkillSet_Exactly4Ids()
        {
            CollectionAssert.AreEqual(new[] { 128, 125, 1073, 1074 }, CaiBangActiveSkillSet.ActiveSkillIds);
            Assert.IsTrue(CaiBangActiveSkillSet.IsActive(128));
            Assert.IsTrue(CaiBangActiveSkillSet.IsActive(125));
            Assert.IsTrue(CaiBangActiveSkillSet.IsActive(1073));
            Assert.IsTrue(CaiBangActiveSkillSet.IsActive(1074));
            Assert.IsFalse(CaiBangActiveSkillSet.IsActive(1), "id ngoài whitelist → false");
            Assert.IsFalse(CaiBangActiveSkillSet.IsActive(0));
        }

        // ------------------------------------------------------------------
        // IsDependMet (Gap B — parity dhcd RandomSkillDependEntry)
        // ------------------------------------------------------------------

        [Test]
        public void IsDependMet_NullOrEmpty_AlwaysTrue()
        {
            Assert.IsTrue(Cfg(128).IsDependMet(new SkillCastRuntime()), "null depend → luôn sẵn");
            Assert.IsTrue(Cfg(128, new List<SurvivorSkillDependEntry>()).IsDependMet(new SkillCastRuntime()), "empty depend → luôn sẵn");
            Assert.IsFalse(Cfg(128, Deps(1, 5)).IsDependMet(null), "roster null → GetLevel 0 < Lv5 → false (fail-closed)");
        }

        [Test]
        public void IsDependMet_1073_Requires128Lv5()
        {
            var cfg = Cfg(1073, Deps(128, 5));
            var rt = new SkillCastRuntime();

            Assert.IsFalse(cfg.IsDependMet(rt), "chưa học 128 → blocked");
            rt.Learn(Def(128), 4);
            Assert.IsFalse(cfg.IsDependMet(rt), "128 lv4 → 1073 vẫn blocked");
            rt.Learn(Def(128)); // lv5
            Assert.IsTrue(cfg.IsDependMet(rt), "128 lv5 → 1073 unlocked");
        }

        [Test]
        public void IsDependMet_1074_Requires125Lv5()
        {
            var cfg = Cfg(1074, Deps(125, 5));
            var rt = new SkillCastRuntime();

            Assert.IsFalse(cfg.IsDependMet(rt), "chưa học 125 → blocked");
            rt.Learn(Def(125), 4);
            Assert.IsFalse(cfg.IsDependMet(rt), "125 lv4 → 1074 vẫn blocked");
            rt.Learn(Def(125)); // lv5
            Assert.IsTrue(cfg.IsDependMet(rt), "125 lv5 → 1074 unlocked");
        }

        // ------------------------------------------------------------------
        // pool.Draw cand-filter depend (validation #2)
        // ------------------------------------------------------------------

        /// <summary>Pool y hệt wiring director: 128/125 tier1 + 1073/1074 depend.</summary>
        private static SkillChoicePool CaiBangPool()
        {
            var pool = new SkillChoicePool();
            pool.Add(Cfg(128));
            pool.Add(Cfg(125));
            pool.Add(Cfg(1073, Deps(128, 5)));
            pool.Add(Cfg(1074, Deps(125, 5)));
            return pool;
        }

        private static HashSet<int> Ids(List<SkillDef> defs)
        {
            var s = new HashSet<int>();
            for (int i = 0; i < defs.Count; i++) s.Add(defs[i].Id);
            return s;
        }

        [Test]
        public void Draw_EmptyRoster_OnlyTier1()
        {
            var pool = CaiBangPool();
            var ids = Ids(pool.Draw(4, new SkillCastRuntime(), new System.Random(1)));

            Assert.AreEqual(2, ids.Count, "roster rỗng → cand chỉ 2 (128/125) → 2 card");
            Assert.IsTrue(ids.Contains(128), "card 128 có mặt");
            Assert.IsTrue(ids.Contains(125), "card 125 có mặt");
            Assert.IsFalse(ids.Contains(1073), "1073 chưa đủ prereq → không vào cand");
            Assert.IsFalse(ids.Contains(1074), "1074 chưa đủ prereq → không vào cand");
        }

        [Test]
        public void Draw_Roster128Lv5_1073EntersCandidates_1074StillBlocked()
        {
            var pool = CaiBangPool();
            var rt = new SkillCastRuntime();
            rt.Learn(Def(128), 5);

            var ids = Ids(pool.Draw(4, rt, new System.Random(1)));
            Assert.IsTrue(ids.Contains(1073), "128 lv5 → 1073 vào cand");
            Assert.IsFalse(ids.Contains(1074), "125 chưa lv5 → 1074 vẫn blocked");
            Assert.IsTrue(ids.Contains(128) && ids.Contains(125), "tier1 vẫn còn");
        }

        [Test]
        public void Draw_Roster125Lv5_1074EntersCandidates_1073StillBlocked()
        {
            var pool = CaiBangPool();
            var rt = new SkillCastRuntime();
            rt.Learn(Def(125), 5);

            var ids = Ids(pool.Draw(4, rt, new System.Random(1)));
            Assert.IsTrue(ids.Contains(1074), "125 lv5 → 1074 vào cand");
            Assert.IsFalse(ids.Contains(1073), "128 chưa lv5 → 1073 vẫn blocked");
            Assert.IsTrue(ids.Contains(128) && ids.Contains(125), "tier1 vẫn còn");
        }

        [Test]
        public void Draw_DependMet_StillRespectsMaxLevel()
        {
            // dùng CHUNG instance def cho pool + roster (y hệt production:
            // Select → Learn(card.Def) — cùng SkillDef); maxLevel:1 → cap ngay
            var def1073 = Def(1073, maxLevel: 1);
            var pool = new SkillChoicePool();
            pool.Add(Cfg(128));
            pool.Add(Cfg(125));
            pool.Add(new SurvivorSkillLibraryConfig(def1073, Deps(128, 5)));
            pool.Add(Cfg(1074, Deps(125, 5)));

            var rt = new SkillCastRuntime();
            rt.Learn(Def(128), 5);
            rt.Learn(def1073); // max 1 → đạt cap
            rt.Learn(Def(125), 5);

            var ids = Ids(pool.Draw(4, rt, new System.Random(1)));
            Assert.IsFalse(ids.Contains(1073), "depend đã thỏa nhưng MaxLevel → loại khỏi cand (filter giữ nguyên)");
            Assert.IsTrue(ids.Contains(1074), "1074 thỏa prereq → vào cand");
        }

        [Test]
        public void FindById_ReturnsConfig_OrNull()
        {
            var pool = CaiBangPool();
            Assert.AreEqual(1073, pool.FindById(1073).Def.Id);
            Assert.AreEqual(128, pool.FindById(128).Def.Id);
            Assert.IsNull(pool.FindById(999), "id không có trong pool → null (fail-closed)");
        }
    }
}
