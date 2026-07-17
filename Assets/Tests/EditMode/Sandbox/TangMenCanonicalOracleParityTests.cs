using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// EditMode consumer of the frozen TangMen canonical oracle
    /// (Assets/StreamingAssets/Reference/PcTangMenOracle.json). The oracle is the
    /// independent expected authority; this class never derives expected
    /// membership, order, or fields from the Unity factory. It only:
    ///   - hash-pins the learned slice, relationship-target slice, and oracle,
    ///   - parses schema / pcLearnedSkillIds (23) / relationshipTargetIds (32) /
    ///     unresolvedUnityOnly / uiOrder from the oracle,
    ///   - asserts CreateTangMenSkills() membership equals the oracle's learned
    ///     set, then compares each resolved learned skill's present static fields
    ///     and relationship values,
    ///   - asserts 58.collideSkillId == 227 and that every relationship target
    ///     resolves in the full production catalog without being promoted to
    ///     learned membership.
    /// Field comparison is gated on membership so the current 10-skill factory
    /// surfaces one diagnostic gap instead of cascading null references.
    /// </summary>
    public class TangMenCanonicalOracleParityTests
    {
        private const string LearnedSliceSha256 = "e4a6657ccfd87be51e5404143df81ce60a022fbbd17303cb9c9c1c59841108ad";
        private const string TargetSliceSha256 = "888c93cde48ec22160e12386580bca3aafc2b74d5bc16ba21b70c06a9a8007ba";
        private const string OracleSha256 = "e4270bd12a534b229c962c3fc322a9271aaefc6b99d062e3df0711a5b0f84f89";

        private static string ReferenceDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
        private static string LearnedSlicePath => Path.Combine(ReferenceDir, "PcTangMenSkills.txt");
        private static string TargetSlicePath => Path.Combine(ReferenceDir, "PcTangMenRelationshipTargets.txt");
        private static string OraclePath => Path.Combine(ReferenceDir, "PcTangMenOracle.json");

        [Serializable]
        private sealed class Unresolved
        {
            public int skill_id;
            public bool oracle_include;
            public string membership;
            public string reason;
            public int[] relationship_target_of;
        }

        [Serializable]
        private sealed class OracleSkill
        {
            public int skillId;
            public string[] present;
            public int skillStyle, stateSpecialId, isAura, attackRadius, missilesGenerate, missilesGenerateData;
            public int missileForm, childSkillId, childSkillLevel, childSkillNum, baseSkill, charAnimId, isMelee;
            public int waitTime, skillCostType, cost, timePerCast, isPhysical, targetOnly, targetEnemy, targetAlly;
            public int targetSelf, targetObj, byMissile, isUseAttackRating, reqLevel, maxLevel, equipLimit, horseLimit;
            public int doHurt, weaponSkill, startSkillId, flySkillId, flyEventTime, collideSkillId, vanishSkillId;
            public string manCastSndPath, fmCastSndPath, lvlSetScript, levelUpScript;

            public bool Has(string field) => present != null && Array.IndexOf(present, field) >= 0;
        }

        [Serializable]
        private sealed class Oracle
        {
            public string schema;
            public int[] pcLearnedSkillIds;
            public int[] relationshipTargetIds;
            public Unresolved[] unresolvedUnityOnly;
            public string uiOrder;
            public OracleSkill[] skills;
        }

        [Test]
        public void OracleArtifact_IsHashPinnedAndSchemaValid()
        {
            Assert.AreEqual(LearnedSliceSha256, Sha256Hex(LearnedSlicePath),
                "TangMen learned slice (PcTangMenSkills.txt) changed; regenerate via vltktool and review provenance");
            Assert.AreEqual(TargetSliceSha256, Sha256Hex(TargetSlicePath),
                "TangMen relationship-target slice (PcTangMenRelationshipTargets.txt) changed; regenerate via vltktool");
            Assert.AreEqual(OracleSha256, Sha256Hex(OraclePath),
                "TangMen oracle changed; regenerate and review provenance");
            Assert.AreEqual($"{OracleSha256}  PcTangMenOracle.json\n",
                File.ReadAllText(OraclePath + ".sha256").Replace("\r\n", "\n").Replace("\r", "\n"),
                "TangMen oracle .sha256 sidecar drifted");

            string raw = File.ReadAllText(OraclePath);
            Assert.That(raw, Does.Contain("\"uiOrder\":null"), "uiOrder must remain null in the static oracle");

            var oracle = LoadOracle();
            Assert.AreEqual("vltk.tangmen.static-oracle/v1", oracle.schema, "oracle schema");
            Assert.IsNotNull(oracle.pcLearnedSkillIds, "missing pcLearnedSkillIds");
            Assert.AreEqual(23, oracle.pcLearnedSkillIds.Length, "pcLearnedSkillIds cardinality must be 23");
            Assert.IsNotNull(oracle.relationshipTargetIds, "missing relationshipTargetIds");
            Assert.AreEqual(32, oracle.relationshipTargetIds.Length, "relationshipTargetIds cardinality must be 32");
            // JsonUtility materializes a JSON null string as ""; the authoritative
            // null check is the raw-text assertion above. Both forms must hold.
            Assert.IsTrue(string.IsNullOrEmpty(oracle.uiOrder),
                "uiOrder must remain null/empty in the static oracle");

            // 51/55/57 must remain unresolved Unity-only and excluded from learned membership.
            var unresolved = (oracle.unresolvedUnityOnly ?? Array.Empty<Unresolved>())
                .Select(u => u.skill_id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 51, 55, 57 }, unresolved, "unresolved Unity-only IDs drifted");
            foreach (var u in oracle.unresolvedUnityOnly)
                Assert.IsFalse(u.oracle_include, $"unresolved Unity-only skill {u.skill_id} must stay oracle_include=false");
            var learned = new HashSet<int>(oracle.pcLearnedSkillIds);
            CollectionAssert.DoesNotContain(learned, 51, "51 must stay excluded from learned membership");
            CollectionAssert.DoesNotContain(learned, 55, "55 must stay excluded from learned membership");
            CollectionAssert.DoesNotContain(learned, 57, "57 must stay excluded from learned membership");
        }

        [Test]
        public void ProductionTangMenMembership_EqualsLearnedOracle()
        {
            var oracle = LoadOracle();
            var expected = new HashSet<int>(oracle.pcLearnedSkillIds);
            var production = new HashSet<int>(
                PcCombatCatalogFactory.CreateTangMenSkills().Select(s => s.skillId));

            var extra = production.Except(expected).OrderBy(x => x).ToArray();
            var missing = expected.Except(production).OrderBy(x => x).ToArray();
            Assert.AreEqual(23, expected.Count, "oracle learned cardinality must be 23");
            Assert.IsTrue(production.Count == expected.Count && expected.SetEquals(production),
                $"TangMen production membership does not equal the oracle learned set: " +
                $"production={production.Count} skills, oracle=23; " +
                $"extra-in-production(excluded-unresolved)= [{string.Join(", ", extra)}]; " +
                $"missing-from-production(unimplemented-learned)= [{string.Join(", ", missing)}]");
        }

        [Test]
        public void LearnedStaticFieldsAndRelationships_MatchOracle()
        {
            var oracle = LoadOracle();
            var expected = new HashSet<int>(oracle.pcLearnedSkillIds);
            var production = new HashSet<int>(
                PcCombatCatalogFactory.CreateTangMenSkills().Select(s => s.skillId));
            // Gate field parity on membership: otherwise missing learned skills cascade as null refs.
            Assume.That(production, Is.EquivalentTo(expected),
                "TangMen production membership must equal the oracle learned set before field parity is checked");

            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            foreach (var row in oracle.skills)
            {
                SkillDefinition skill = catalog.Resolve(row.skillId);
                Assert.IsNotNull(skill, $"Missing TangMen learned skill {row.skillId} in production catalog");

                Eq(row, "skillStyle", row.skillStyle, (int)skill.skillStyle);
                Eq(row, "stateSpecialId", row.stateSpecialId, skill.stateSpecialId);
                Eq(row, "isAura", row.isAura, B(skill.isAura));
                Eq(row, "attackRadius", row.attackRadius, skill.attackRadius);
                Eq(row, "missilesGenerate", row.missilesGenerate, skill.missilesGenerate);
                Eq(row, "missilesGenerateData", row.missilesGenerateData, skill.missilesGenerateData);
                Eq(row, "missileForm", row.missileForm, (int)skill.missileForm);
                Eq(row, "childSkillId", row.childSkillId, skill.childSkillId);
                Eq(row, "childSkillLevel", row.childSkillLevel, skill.childSkillLevel);
                Eq(row, "childSkillNum", row.childSkillNum, skill.childSkillNum);
                Eq(row, "baseSkill", row.baseSkill, B(skill.baseSkill));
                Eq(row, "charAnimId", row.charAnimId, skill.charAnimId);
                Eq(row, "isMelee", row.isMelee, B(skill.isMelee));
                Eq(row, "waitTime", row.waitTime, skill.waitTime);
                Eq(row, "skillCostType", row.skillCostType, skill.skillCostType);
                Eq(row, "cost", row.cost, skill.cost);
                Eq(row, "timePerCast", row.timePerCast, skill.timePerCast);
                Eq(row, "isPhysical", row.isPhysical, B(skill.isPhysical));
                Eq(row, "targetOnly", row.targetOnly, B(skill.targetOnly));
                Eq(row, "targetEnemy", row.targetEnemy, B(skill.targetEnemy));
                Eq(row, "targetAlly", row.targetAlly, B(skill.targetAlly));
                Eq(row, "targetSelf", row.targetSelf, B(skill.targetSelf));
                Eq(row, "targetObj", row.targetObj, B(skill.targetObj));
                Eq(row, "byMissile", row.byMissile, B(skill.byMissile));
                Eq(row, "isUseAttackRating", row.isUseAttackRating, B(skill.isUseAttackRating));
                Eq(row, "reqLevel", row.reqLevel, skill.reqLevel);
                Eq(row, "maxLevel", row.maxLevel, skill.maxLevel);
                Eq(row, "equipLimit", row.equipLimit, skill.equipLimit);
                Eq(row, "horseLimit", row.horseLimit, skill.horseLimit);
                Eq(row, "doHurt", row.doHurt, B(skill.doHurt));
                Eq(row, "weaponSkill", row.weaponSkill, B(skill.weaponSkill));
                Eq(row, "startSkillId", row.startSkillId, skill.startSkillId);
                Eq(row, "flySkillId", row.flySkillId, skill.flySkillId);
                Eq(row, "flyEventTime", row.flyEventTime, skill.flyEventTime);
                Eq(row, "collideSkillId", row.collideSkillId, skill.collideSkillId);
                Eq(row, "vanishSkillId", row.vanishSkillId, skill.vanishSkillId);

                if (row.Has("manCastSndPath")) Assert.AreEqual(row.manCastSndPath, skill.manCastSndPath ?? "", $"skill {row.skillId}.manCastSndPath");
                if (row.Has("fmCastSndPath")) Assert.AreEqual(row.fmCastSndPath, skill.fmCastSndPath ?? "", $"skill {row.skillId}.fmCastSndPath");
                if (row.Has("lvlSetScript")) Assert.AreEqual(row.lvlSetScript, skill.lvlSetScript ?? "", $"skill {row.skillId}.lvlSetScript");
                if (row.Has("levelUpScript")) Assert.AreEqual(row.levelUpScript, skill.levelUpScript ?? "", $"skill {row.skillId}.levelUpScript");
            }
        }

        [Test]
        public void RelationshipTargets_ResolveInFullCatalogWithoutPromotion()
        {
            var oracle = LoadOracle();
            var expected = new HashSet<int>(oracle.pcLearnedSkillIds);
            var production = new HashSet<int>(
                PcCombatCatalogFactory.CreateTangMenSkills().Select(s => s.skillId));
            Assume.That(production, Is.EquivalentTo(expected),
                "TangMen production membership must equal the oracle learned set before relationship-target resolution is checked");

            // 58 -> 227 via collideSkillId (event chain when a missile collides).
            var skill58 = PcCombatCatalogFactory.CreateTangMenSkills().FirstOrDefault(s => s.skillId == 58);
            Assert.IsNotNull(skill58, "skill 58 must exist in TangMen factory");
            Assert.AreEqual(227, skill58.collideSkillId, "skill 58 collideSkillId must target relationship id 227");

            var learned = new HashSet<int>(oracle.pcLearnedSkillIds);
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            foreach (var target in oracle.relationshipTargetIds)
            {
                Assert.IsFalse(learned.Contains(target),
                    $"relationship target {target} must not be promoted to learned membership");
                var resolved = catalog.Resolve(target);
                Assert.IsNotNull(resolved,
                    $"relationship target {target} does not resolve in the full production catalog");
            }
        }

        private static Oracle LoadOracle()
        {
            var oracle = JsonUtility.FromJson<Oracle>(File.ReadAllText(OraclePath));
            Assert.IsNotNull(oracle, "failed to parse TangMen oracle JSON");
            Assert.IsNotNull(oracle.skills, "oracle has no skills array");
            return oracle;
        }

        private static void Eq(OracleSkill row, string field, int expected, int actual)
        {
            if (row.Has(field)) Assert.AreEqual(expected, actual, $"skill {row.skillId}.{field}");
        }

        private static int B(bool value) => value ? 1 : 0;

        private static string Sha256Hex(string path)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "").ToLowerInvariant();
        }
    }
}
