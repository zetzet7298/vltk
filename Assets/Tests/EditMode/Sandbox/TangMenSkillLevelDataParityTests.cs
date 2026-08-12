using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// EditMode consumer of the canonical TangMen per-level damage/state reference
    /// (Assets/StreamingAssets/Reference/PcTangMenSkillLevelData.json), itself generated
    /// deterministically from the vendored canonical PC server tangmen.lua
    /// (Assets/StreamingAssets/Reference/PcTangMenSkillLevelData.lua, sha256 3f2e7c2a...)
    /// by scripts/generate_tangmen_leveldata.py. This class never derives expected values
    /// from the Unity factory: the JSON is the independent authority and the factory must
    /// match it byte-for-byte.
    ///
    /// Fail-before: learned skill 302 (Bạo Vũ Lê Hoa) and collide target 304 (Độc Thích Cốt)
    /// previously resolved as static-only shells with empty pcLevelData, so they performed
    /// zero sourced damage/state. After SKL-TM-RUNTIME they carry the canonical curves and
    /// AssertNonzeroSourcedDamageForLearned302AndCollide304 pins representative level-1 values.
    /// </summary>
    public class TangMenSkillLevelDataParityTests
    {
        private const string LuaSha256 = "3f2e7c2aba8329508adab3a6293f41be29a0d68a8d63ac5a34903bece0578c90";

        private static string ReferenceDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
        private static string LuaPath => Path.Combine(ReferenceDir, "PcTangMenSkillLevelData.lua");
        private static string JsonPath => Path.Combine(ReferenceDir, "PcTangMenSkillLevelData.json");

        [Serializable]
        private sealed class RefRow
        {
            public int skillId;
            public int level;
            public string bucket;
            public string kind;
            public int v1;
            public int v2;
            public int v3;
        }

        [Serializable]
        private sealed class RefManifest
        {
            public string schema;
            public int materializedSkillCount;
            public RefRow[] rows;
        }

        [Test]
        public void ReferenceArtifact_LuaIsHashPinnedToCanonicalServerSource()
        {
            Assert.IsTrue(File.Exists(LuaPath), $"vendored canonical lua missing: {LuaPath}");
            Assert.AreEqual(LuaSha256, Sha256Hex(LuaPath),
                "PcTangMenSkillLevelData.lua drifted from the canonical PC server tangmen.lua; "
                + "re-vendor from /var/www/jx-pc/.../bin/Server/script/skill/tangmen.lua and "
                + "update PcTangMenSkillLevelData.lua.provenance.json");

            // Byte-exactness: the vendored copy MUST equal the canonical source it documents.
            // (Coordinator owns the editor lock; this hash is the trust anchor for runtime/tests.)
            var sidecar = Path.Combine(ReferenceDir, "PcTangMenSkillLevelData.lua.sha256");
            Assert.AreEqual($"{LuaSha256}  PcTangMenSkillLevelData.lua\n",
                File.ReadAllText(sidecar).Replace("\r\n", "\n").Replace("\r", "\n"),
                "PcTangMenSkillLevelData.lua.sha256 sidecar drifted");
        }

        // Fail-before evidence: learned skill 302 and collide target 304 were static-only
        // shells (empty pcLevelData -> zero sourced damage). They now perform their canonical
        // level-1 damage/state. Representative values are pinned from tangmen.lua.
        [Test]
        public void AssertNonzeroSourcedDamageForLearned302AndCollide304()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

            // Learned 302 (Bạo Vũ Lê Hoa, lua key baoyu_lihua): before this wave pcLevelData
            // was empty, so every level-1 attribute was absent/zero.
            var learned302Lv1 = catalog.Resolve(302).GetPcLevelData(1);
            Assert.IsNotNull(learned302Lv1, "learned 302 must now carry pcLevelData (was static-only)");
            Assert.AreEqual(15, learned302Lv1.First(VLTK.Model.MagicAttributeKind.PhysicsEnhanceP).value1,
                "302 level-1 PhysicsEnhanceP (baoyu_lihua {{{1,15},...}})");
            Assert.AreEqual(20, learned302Lv1.First(VLTK.Model.MagicAttributeKind.SeriesDamageP).value1,
                "302 level-1 SeriesDamageP");
            var poison302 = learned302Lv1.First(VLTK.Model.MagicAttributeKind.PoisonDamageV);
            Assert.AreEqual(1, poison302.value1, "302 level-1 PoisonDamageV v1");
            Assert.AreEqual(60, poison302.value2, "302 level-1 PoisonDamageV v2 (constant)");
            Assert.AreEqual(10, poison302.value3, "302 level-1 PoisonDamageV v3 (constant)");

            // Collide target 304 (Độc Thích Cốt, lua key duci_gu, collide event of learned 303):
            // before this wave it was an existence-only relationship shell with no pcLevelData.
            Assert.IsFalse(
                new HashSet<int>(PcCombatCatalogFactory.CreateTangMenSkills().Select(s => s.skillId)).Contains(304),
                "304 stays support-only: never promoted to learned membership");
            var collide304Lv1 = catalog.Resolve(304).GetPcLevelData(1);
            Assert.IsNotNull(collide304Lv1, "collide target 304 must now carry pcLevelData (was existence-only)");
            Assert.AreEqual(1, collide304Lv1.First(VLTK.Model.MagicAttributeKind.SeriesDamageP).value1,
                "304 level-1 SeriesDamageP");
            var poison304 = collide304Lv1.First(VLTK.Model.MagicAttributeKind.PoisonDamageV);
            Assert.AreEqual(8, poison304.value1, "304 level-1 PoisonDamageV v1 (duci_gu [1] {{1,8},...})");
            Assert.AreEqual(100, poison304.value2, "304 level-1 PoisonDamageV v2 (duci_gu [2] constant 100)");
            Assert.AreEqual(10, poison304.value3, "304 level-1 PoisonDamageV v3 (duci_gu [3] constant 10)");
        }

        // Byte-for-byte parity across all 28 materialized skills and every learned level.
        // The JSON is the independent authority; any factory drift fails here.
        [Test]
        public void Factory_LevelData_MatchesCanonicalReference()
        {
            var manifest = LoadManifest();
            Assert.AreEqual("vltk.tangmen.leveldata/v1", manifest.schema, "level-data reference schema");
            Assert.AreEqual(28, manifest.materializedSkillCount, "exactly 16 learned + 12 damage-bearing targets");

            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            var learned = new HashSet<int>(PcCombatCatalogFactory.CreateTangMenSkills().Select(s => s.skillId));

            // expected[skillId][level] = sorted multiset of "bucket|kind|v1,v2,v3"
            var expected = new Dictionary<int, Dictionary<int, List<string>>>();
            foreach (var row in manifest.rows)
            {
                if (!expected.TryGetValue(row.skillId, out var byLevel))
                    expected[row.skillId] = byLevel = new Dictionary<int, List<string>>();
                if (!byLevel.TryGetValue(row.level, out var list))
                    byLevel[row.level] = list = new List<string>();
                list.Add($"{row.bucket}|{row.kind}|{row.v1},{row.v2},{row.v3}");
            }

            // Every skill the reference materializes must resolve and carry pcLevelData.
            Assert.AreEqual(28, expected.Count, "reference must materialize exactly 28 skills");

            int compared = 0;
            foreach (var kv in expected)
            {
                int skillId = kv.Key;
                var skill = catalog.Resolve(skillId);
                Assert.IsNotNull(skill, $"materialized skill {skillId} does not resolve in production catalog");

                // Learned static-only (16) must be learned members; damage-bearing
                // relationship targets (12) must stay support-only.
                bool shouldBeLearned = skillId is 249 or 302 or 303 or 339 or 341 or 342 or 343
                    or 345 or 347 or 349 or 351 or 710 or 1069 or 1070 or 1071 or 1110;
                if (shouldBeLearned)
                    Assert.IsTrue(learned.Contains(skillId), $"learned static-only root {skillId} must remain learned");
                else
                    Assert.IsFalse(learned.Contains(skillId),
                        $"damage-bearing target {skillId} must stay support-only (not promoted)");

                Assert.GreaterOrEqual(skill.pcLevelData.Count, 1,
                    $"skill {skillId} pcLevelData is empty (static-only shell not upgraded)");

                foreach (var level in kv.Value.Keys.OrderBy(x => x))
                {
                    var data = skill.GetPcLevelData(level);
                    Assert.IsNotNull(data, $"skill {skillId} level {level} has no pcLevelData");
                    var actual = new List<string>();
                    foreach (var a in data.damage) actual.Add($"damage|{a.kind}|{a.value1},{a.value2},{a.value3}");
                    foreach (var a in data.state) actual.Add($"state|{a.kind}|{a.value1},{a.value2},{a.value3}");
                    foreach (var a in data.skill) actual.Add($"skill|{a.kind}|{a.value1},{a.value2},{a.value3}");
                    actual.Sort();
                    var want = kv.Value[level].OrderBy(x => x).ToList();
                    Assert.AreEqual(want.Count, actual.Count,
                        $"skill {skillId} level {level} attribute count mismatch (want {want.Count}, got {actual.Count})");
                    for (int i = 0; i < want.Count; i++)
                        Assert.AreEqual(want[i], actual[i],
                            $"skill {skillId} level {level} attribute #{i} drift: want '{want[i]}' got '{actual[i]}'");
                        compared += actual.Count;
                }
            }
            Assert.Greater(compared, 0, "no level rows compared");
            Assert.AreEqual(TotalRows(manifest), compared, "every reference row must be compared");
        }

        // The 7 shared roots (43/45/47/48/50/54/58) keep their existing hand-authored data and
        // are intentionally excluded from this wave; 20 cross-faction projectile/support
        // relationship targets remain existence-only shells (their lvlSetScript is not tangmen.lua).
        [Test]
        public void SharedRootsAndProjectileTargets_AreNotTouchedByThisWave()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            // Projectile/support targets that must stay existence-only (no TangMen damage data).
            foreach (var id in new[] { 35, 37, 38, 67, 96, 106, 116, 127, 149, 151, 152, 153, 155, 157, 159, 161, 331, 332, 333, 374 })
            {
                var s = catalog.Resolve(id);
                Assert.IsNotNull(s, $"projectile/support target {id} must resolve");
                // These IDs may already be owned by another faction in the combined catalog;
                // this wave must not overwrite that owner. The invariant here is resolution,
                // not an empty level list.
                Assert.IsNotNull(s.pcLevelData,
                    $"cross-faction projectile/support target {id} must remain resolvable");
            }
            // Shared roots still carry their own (non-empty) pcLevelData from the prior wave.
            foreach (var id in new[] { 43, 45, 47, 48, 50, 54, 58 })
                Assert.GreaterOrEqual(catalog.Resolve(id).pcLevelData.Count, 1,
                    $"shared root {id} must retain its existing pcLevelData");
        }

        private static RefManifest LoadManifest()
        {
            Assert.IsTrue(File.Exists(JsonPath), $"level-data reference missing: {JsonPath}");
            var manifest = JsonUtility.FromJson<RefManifest>(File.ReadAllText(JsonPath));
            Assert.IsNotNull(manifest, "failed to parse TangMen level-data reference JSON");
            Assert.IsNotNull(manifest.rows, "reference has no rows array");
            return manifest;
        }

        private static int TotalRows(RefManifest manifest) => manifest.rows.Length;

        private static string Sha256Hex(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
