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
    /// EditMode consumer of the frozen KunLun canonical oracle
    /// (Assets/StreamingAssets/Reference/PcKunLunOracle.json). The oracle is the independent
    /// expected authority; this class never derives expected membership, order, or fields from the
    /// Unity factory. It only:
    ///   - hash-pins the learned slice, relationship-target slice, and oracle,
    ///   - parses schema / pcLearnedSkillIds (24) / relationshipTargetIds (17) /
    ///     unresolvedUnityOnly / uiOrder from the oracle,
    ///   - asserts CreateKunLunSkills() membership equals the oracle's learned set, then compares
    ///     each resolved learned skill's present static fields and relationship values,
    ///   - asserts all 17 relationship targets resolve in the full production catalog without being
    ///     promoted to learned membership, that the 14 support-only targets are absent from learned
    ///     while the 3 self-reference overlaps (178,181,372) are learned,
    ///   - proves GrantKunLunSkillPanelProgression and MaxAllSkillLevels exclude support/residual IDs.
    /// Field comparison is gated on membership so the factory surfaces one diagnostic gap instead of
    /// cascading null references.
    /// </summary>
    public class KunLunCanonicalOracleParityTests
    {
        private const string LearnedSliceSha256 = "34f7aef196656c44e9461d5e75960bb940c8be7c4e68ce12af644c289247236c";
        private const string TargetSliceSha256 = "d136e0be557a5055aa27163b26842166dc097f2d903a6e0911ae055d22b79e3b";
        private const string OracleSha256 = "3be6712946489b82d2595eae77894bcf022f0b6cd4d43977850572c700be399f";

        // Targets that are themselves learned via progression/skillbook evidence (not promoted by
        // the relationship closure). The remaining 14 relationship targets are support-only.
        private static readonly int[] SelfReferenceLearnedIds = { 178, 181, 372 };

        private static string ReferenceDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
        private static string LearnedSlicePath => Path.Combine(ReferenceDir, "PcKunLunSkills.txt");
        private static string TargetSlicePath => Path.Combine(ReferenceDir, "PcKunLunRelationshipTargets.txt");
        private static string OraclePath => Path.Combine(ReferenceDir, "PcKunLunOracle.json");

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
            public int[] relationshipSelfReferenceLearnedIds;
            public Unresolved[] unresolvedUnityOnly;
            public string uiOrder;
            public OracleSkill[] skills;
        }

        [Test]
        public void OracleArtifact_IsHashPinnedAndSchemaValid()
        {
            Assert.AreEqual(LearnedSliceSha256, Sha256Hex(LearnedSlicePath),
                "KunLun learned slice (PcKunLunSkills.txt) changed; regenerate via vltktool and review provenance");
            Assert.AreEqual(TargetSliceSha256, Sha256Hex(TargetSlicePath),
                "KunLun relationship-target slice (PcKunLunRelationshipTargets.txt) changed; regenerate via vltktool");
            Assert.AreEqual(OracleSha256, Sha256Hex(OraclePath),
                "KunLun oracle changed; regenerate and review provenance");
            Assert.AreEqual($"{OracleSha256}  PcKunLunOracle.json\n",
                File.ReadAllText(OraclePath + ".sha256").Replace("\r\n", "\n").Replace("\r", "\n"),
                "KunLun oracle .sha256 sidecar drifted");

            string raw = File.ReadAllText(OraclePath);
            Assert.That(raw, Does.Contain("\"uiOrder\":null"), "uiOrder must remain null in the static oracle");

            var oracle = LoadOracle();
            Assert.AreEqual("vltk.kunlun.static-oracle/v1", oracle.schema, "oracle schema");
            Assert.IsNotNull(oracle.pcLearnedSkillIds, "missing pcLearnedSkillIds");
            Assert.AreEqual(24, oracle.pcLearnedSkillIds.Length, "pcLearnedSkillIds cardinality must be 24");
            Assert.IsNotNull(oracle.relationshipTargetIds, "missing relationshipTargetIds");
            Assert.AreEqual(17, oracle.relationshipTargetIds.Length, "relationshipTargetIds cardinality must be 17");
            // JsonUtility materializes a JSON null string as ""; the authoritative null check is the
            // raw-text assertion above. Both forms must hold.
            Assert.IsTrue(string.IsNullOrEmpty(oracle.uiOrder),
                "uiOrder must remain null/empty in the static oracle");

            // 170/177/180/183/184 must remain unresolved Unity-only and excluded from learned membership.
            var unresolved = (oracle.unresolvedUnityOnly ?? Array.Empty<Unresolved>())
                .Select(u => u.skill_id).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(new[] { 170, 177, 180, 183, 184 }, unresolved, "unresolved Unity-only IDs drifted");
            foreach (var u in oracle.unresolvedUnityOnly)
                Assert.IsFalse(u.oracle_include, $"unresolved Unity-only skill {u.skill_id} must stay oracle_include=false");
            var learned = new HashSet<int>(oracle.pcLearnedSkillIds);
            foreach (var id in new[] { 170, 177, 180, 183, 184 })
                Assert.IsFalse(learned.Contains(id), $"residual {id} must stay excluded from learned membership");
        }

        [Test]
        public void ProductionKunLunMembership_EqualsLearnedOracle()
        {
            var oracle = LoadOracle();
            var expected = new HashSet<int>(oracle.pcLearnedSkillIds);
            var production = new HashSet<int>(
                PcCombatCatalogFactory.CreateKunLunSkills().Select(s => s.skillId));

            var extra = production.Except(expected).OrderBy(x => x).ToArray();
            var missing = expected.Except(production).OrderBy(x => x).ToArray();
            Assert.AreEqual(24, expected.Count, "oracle learned cardinality must be 24");
            Assert.IsTrue(production.Count == expected.Count && expected.SetEquals(production),
                $"KunLun production membership does not equal the oracle learned set: " +
                $"production={production.Count} skills, oracle=24; " +
                $"extra-in-production(excluded-unresolved)= [{string.Join(", ", extra)}]; " +
                $"missing-from-production(unimplemented-learned)= [{string.Join(", ", missing)}]");
        }

        [Test]
        public void LearnedStaticFieldsAndRelationships_MatchOracle()
        {
            var oracle = LoadOracle();
            var expected = new HashSet<int>(oracle.pcLearnedSkillIds);
            var production = new HashSet<int>(
                PcCombatCatalogFactory.CreateKunLunSkills().Select(s => s.skillId));
            // Gate field parity on membership: otherwise missing learned skills cascade as null refs.
            Assume.That(production, Is.EquivalentTo(expected),
                "KunLun production membership must equal the oracle learned set before field parity is checked");

            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            foreach (var row in oracle.skills)
            {
                SkillDefinition skill = catalog.Resolve(row.skillId);
                Assert.IsNotNull(skill, $"Missing KunLun learned skill {row.skillId} in production catalog");

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
                PcCombatCatalogFactory.CreateKunLunSkills().Select(s => s.skillId));
            Assume.That(production, Is.EquivalentTo(expected),
                "KunLun production membership must equal the oracle learned set before relationship-target resolution is checked");

            var learned = new HashSet<int>(oracle.pcLearnedSkillIds);
            var selfRef = new HashSet<int>(oracle.relationshipSelfReferenceLearnedIds ?? SelfReferenceLearnedIds);
            CollectionAssert.AreEqual(new[] { 178, 181, 372 }, selfRef.OrderBy(x => x).ToArray(),
                "relationship self-reference learned overlaps drifted");

            var supportOnly = oracle.relationshipTargetIds.Where(t => !selfRef.Contains(t)).OrderBy(x => x).ToArray();
            CollectionAssert.AreEqual(
                new[] { 14, 15, 16, 17, 18, 19, 20, 21, 22, 290, 342, 387, 399, 1109 },
                supportOnly, "support-only target set drifted");
            Assert.AreEqual(14, supportOnly.Length, "exactly 14 support-only relationship targets");

            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();
            foreach (var target in oracle.relationshipTargetIds)
            {
                var resolved = catalog.Resolve(target);
                Assert.IsNotNull(resolved,
                    $"relationship target {target} does not resolve in the full production catalog");
            }

            // Three overlaps are independently learned (progression/skillbook), not promoted.
            foreach (var id in selfRef)
            {
                Assert.IsTrue(learned.Contains(id), $"overlap {id} must be a learned root");
                Assert.IsTrue(production.Contains(id), $"overlap {id} must be in CreateKunLunSkills");
            }
            // The 14 support-only targets must never enter learned membership.
            foreach (var target in supportOnly)
            {
                Assert.IsFalse(learned.Contains(target),
                    $"support-only target {target} must not be promoted to learned membership");
                Assert.IsFalse(production.Contains(target),
                    $"support-only target {target} must not appear in CreateKunLunSkills");
            }
        }

        [Test]
        public void ProgressionAndMaxAll_ExcludeSupportAndResidualIds()
        {
            var oracle = LoadOracle();
            var learned = new HashSet<int>(oracle.pcLearnedSkillIds);
            var selfRef = new HashSet<int>(oracle.relationshipSelfReferenceLearnedIds ?? SelfReferenceLearnedIds);
            int[] supportOnly = oracle.relationshipTargetIds.Where(t => !selfRef.Contains(t)).ToArray();
            int[] residuals = { 170, 177, 180, 183, 184 };

            foreach (int id in learned)
                Assert.IsTrue(PlayerProgressionState.IsCanonicalLearnedSkillForFaction(CombatFaction.KunLun, id),
                    $"canonical learned predicate rejected oracle skill {id}");
            foreach (int id in residuals.Concat(supportOnly))
                Assert.IsFalse(PlayerProgressionState.IsCanonicalLearnedSkillForFaction(CombatFaction.KunLun, id),
                    $"canonical learned predicate admitted non-learned skill {id}");

            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog();

            // GrantKunLunSkillPanelProgression: knownSkills == 24 canonical learned + universal
            // lightness; residuals and support-only targets never enter learned/cast/upgrade state.
            var progression = new PlayerProgressionState();
            progression.GrantKunLunSkillPanelProgression(catalog);

            var expectedKnown = new HashSet<int>(learned) { PcCombatCatalogFactory.UniversalLightnessSkill };
            CollectionAssert.AreEquivalent(expectedKnown, progression.knownSkills,
                "knownSkills after GrantKunLun must equal the 24 canonical learned roots + universal lightness");
            foreach (int id in learned)
                Assert.AreEqual(0, progression.skillLevels[id], $"learned {id} should seed at level 0");

            foreach (int id in residuals.Concat(supportOnly))
            {
                Assert.IsFalse(progression.knownSkills.Contains(id),
                    $"{id} must not be known after GrantKunLun");
                Assert.IsFalse(progression.skillLevels.ContainsKey(id),
                    $"{id} must not have a level entry after GrantKunLun");
                var skill = catalog.Resolve(id);
                if (skill != null)
                    Assert.IsFalse(progression.CanUpgradeSkill(skill),
                        $"{id} must not be upgradeable after GrantKunLun");
            }

            // MaxAllSkillLevels: only the current faction (KunLun) + universal actions are maxed;
            // residuals and support-only targets are never promoted, and existing skills from
            // another faction remain untouched.
            var maxed = new PlayerProgressionState { faction = CombatFaction.KunLun };
            const int tangMenSkillId = 54;
            const int tangMenExistingLevel = 3;
            maxed.knownSkills.Add(tangMenSkillId);
            maxed.skillLevels[tangMenSkillId] = tangMenExistingLevel;
            maxed.MaxAllSkillLevels(catalog);

            Assert.IsTrue(maxed.knownSkills.Contains(tangMenSkillId),
                "MaxAll must preserve an existing skill from another faction");
            Assert.AreEqual(tangMenExistingLevel, maxed.skillLevels[tangMenSkillId],
                "MaxAll must not alter an existing skill from another faction");

            foreach (int id in residuals.Concat(supportOnly))
            {
                Assert.IsFalse(maxed.knownSkills.Contains(id),
                    $"{id} must not be known after MaxAll");
                Assert.IsFalse(maxed.skillLevels.ContainsKey(id),
                    $"{id} must not receive a max level");
            }
            foreach (int id in learned)
            {
                Assert.IsTrue(maxed.knownSkills.Contains(id), $"learned {id} must be maxed");
                Assert.AreEqual(catalog.Resolve(id).maxLevel, maxed.skillLevels[id],
                    $"learned {id} must reach its max level");
            }
        }

        private static Oracle LoadOracle()
        {
            var oracle = JsonUtility.FromJson<Oracle>(File.ReadAllText(OraclePath));
            Assert.IsNotNull(oracle, "failed to parse KunLun oracle JSON");
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
