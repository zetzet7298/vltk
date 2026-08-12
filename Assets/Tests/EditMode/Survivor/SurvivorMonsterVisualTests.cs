// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorMonsterVisualTests
// Ticket 35: mapping MonsterDef → NPC res JX (MonsterVisualResolver, pure C#).
// Seam: EditMode pure-logic, không scene, không PlayMode (spec §Testing Decisions).
// Cover: ≥5 loại quái, fail-closed unknown → null, path shape PC, cycle index.
// ponytail: staged-check runtime = PcNpcVisual.HasAnyClip (Sandbox fail-closed) —
// không duplicate root/hash logic ở đây.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorMonsterVisualTests
    {
        // --- gate: ≥5 loại quái khác nhau (mapping đầy đủ) ---
        [Test]
        public void Resolver_Pool_HasAtLeast5DistinctResTypes()
        {
            Assert.GreaterOrEqual(MonsterVisualResolver.Count, 5, "cần ≥5 loại quái (ticket 35)");
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < MonsterVisualResolver.Count; i++)
            {
                var spec = MonsterVisualResolver.ResolveByIndex(i);
                Assert.NotNull(spec, $"entry {i}");
                Assert.IsTrue(seen.Add(spec.resType), $"resType {spec.resType} trùng");
            }
        }

        // --- gate: mọi entry có path PC hợp lệ (stand + walk khác nhau) ---
        [Test]
        public void Resolver_AllEntries_HaveValidPcSprPaths()
        {
            for (int i = 0; i < MonsterVisualResolver.Count; i++)
            {
                var spec = MonsterVisualResolver.ResolveByIndex(i);
                Assert.IsFalse(string.IsNullOrEmpty(spec.standPath), $"{spec.resType} stand");
                Assert.IsFalse(string.IsNullOrEmpty(spec.walkPath), $"{spec.resType} walk");
                Assert.AreNotEqual(spec.standPath, spec.walkPath, $"{spec.resType} stand==walk");
                StringAssert.StartsWith("spr\\npcres\\", spec.standPath, $"{spec.resType} stand prefix");
                StringAssert.StartsWith("spr\\npcres\\", spec.walkPath, $"{spec.resType} walk prefix");
                StringAssert.EndsWith("_st.spr", spec.standPath, $"{spec.resType} stand suffix");
                StringAssert.EndsWith("_wlk.spr", spec.walkPath, $"{spec.resType} walk suffix");
            }
        }

        // --- gate: chưa map template → fail-closed null (proxy màu, không crash) ---
        [Test]
        public void Resolve_UnknownResType_ReturnsNull()
        {
            Assert.IsNull(MonsterVisualResolver.Resolve("enemy999"), "chưa map phải null");
            Assert.IsNull(MonsterVisualResolver.Resolve(""), "rỗng phải null");
            Assert.IsNull(MonsterVisualResolver.Resolve(null), "null phải null");
        }

        // --- gate: Resolve theo tên khớp entry đã map ---
        [Test]
        public void Resolve_KnownResType_ReturnsSameSpecAsPool()
        {
            var byIndex = MonsterVisualResolver.ResolveByIndex(0);
            var byName = MonsterVisualResolver.Resolve(byIndex.resType);
            Assert.NotNull(byName);
            Assert.AreEqual(byIndex.resType, byName.resType);
            Assert.AreEqual(byIndex.standPath, byName.standPath);
            Assert.AreEqual(byIndex.walkPath, byName.walkPath);
        }

        // --- gate: cycle theo spawn order (index âm an toàn) ---
        [Test]
        public void ResolveByIndex_CyclesWithinPool()
        {
            int n = MonsterVisualResolver.Count;
            Assert.AreEqual(MonsterVisualResolver.ResolveByIndex(0).resType, MonsterVisualResolver.ResolveByIndex(n).resType, "cycle n");
            Assert.AreEqual(MonsterVisualResolver.ResolveByIndex(0).resType, MonsterVisualResolver.ResolveByIndex(-n).resType, "index âm an toàn");
        }

        // --- gate: referencePixel mặc định PC 160x192 (PcNpcVisual.Configure) ---
        [Test]
        public void Resolver_AllEntries_DefaultReferencePixel()
        {
            for (int i = 0; i < MonsterVisualResolver.Count; i++)
            {
                var spec = MonsterVisualResolver.ResolveByIndex(i);
                Assert.AreEqual(new Vector2(160f, 192f), spec.referencePixel, $"{spec.resType} refPixel");
            }
        }
    }
}
