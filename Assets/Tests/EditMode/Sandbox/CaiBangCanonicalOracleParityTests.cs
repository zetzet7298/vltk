using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    public class CaiBangCanonicalOracleParityTests
    {
        private const string SliceSha256 = "7aa82d708a8ecdbdcdf6d7e2ce1974fde9286832d6f2ffff1d3c2d182a440973";
        private const string OracleSha256 = "91d3251aef30f755f3480a2104a48227eaffd8e7ea8fbf495d189dd185ed84da";

        private static string ReferenceDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference");
        private static string SlicePath => Path.Combine(ReferenceDir, "PcCaiBangSkills.txt");
        private static string OraclePath => Path.Combine(ReferenceDir, "PcCaiBangOracle.json");
        private static string PackagedSlicePath => Path.Combine(Directory.GetCurrentDirectory(), "Assets/Resources/Reference/PcCaiBangSkills.bytes");

        [Serializable]
        private sealed class Oracle
        {
            public string schema;
            public string sliceSha256;
            public int[] rootSkillIds;
            public OracleSkill[] skills;
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

            public bool Has(string field) => Array.IndexOf(present, field) >= 0;
        }

        [Test]
        public void OracleArtifact_IsHashPinnedAndCoversPlayerRoots()
        {
            Assert.AreEqual(SliceSha256, Sha256Hex(SlicePath), "Canonical extracted slice changed");
            Assert.AreEqual(SliceSha256, Sha256Hex(PackagedSlicePath), "Packaged Android slice drifted from audit slice");
            Assert.AreEqual(OracleSha256, Sha256Hex(OraclePath), "Oracle changed; regenerate and review provenance");
            Assert.AreEqual($"{OracleSha256}  PcCaiBangOracle.json\n",
                File.ReadAllText(OraclePath + ".sha256").Replace("\r\n", "\n"));

            var oracle = LoadOracle();
            Assert.AreEqual("vltk.caibang.static-oracle/v1", oracle.schema);
            Assert.AreEqual(SliceSha256, oracle.sliceSha256);
            CollectionAssert.AreEqual(PcSkillPanelService.PcCaiBangSkillOrder, oracle.rootSkillIds);
            CollectionAssert.AreEqual(PcSkillPanelService.PcCaiBangSkillOrder, SkillTreePanelService.PcCaiBangSkillOrder);
            Assert.AreEqual(oracle.rootSkillIds.Length, oracle.skills.Length);
            for (int i = 0; i < oracle.skills.Length; i++)
                Assert.AreEqual(oracle.rootSkillIds[i], oracle.skills[i].skillId, $"oracle row {i}");
        }

        [Test]
        public void Catalog_StaticFieldsAndRelationshipsMatchIndependentOracle()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            foreach (var row in LoadOracle().skills)
            {
                SkillDefinition skill = catalog.Resolve(row.skillId);
                Assert.IsNotNull(skill, $"Missing Cái Bang root {row.skillId}");

                Eq(row, "skillStyle", row.skillStyle, (int)skill.skillStyle);
                Eq(row, "stateSpecialId", row.stateSpecialId, skill.stateSpecialId);
                Eq(row, "isAura", row.isAura, B(skill.isAura));
                Eq(row, "attackRadius", row.attackRadius, skill.attackRadius);
                Eq(row, "missilesGenerate", row.missilesGenerate, skill.missilesGenerate);
                Eq(row, "missilesGenerateData", row.missilesGenerateData, skill.missilesGenerateData);
                if (row.Has("missileForm"))
                {
                    if (row.missileForm > 0) Eq(row, "missileForm", row.missileForm, (int)skill.missileForm);
                    else
                    {
                        Assert.IsTrue(row.skillId == 357 || row.skillId == 359,
                            $"unexpected populated PC missileForm=0 for {row.skillId}");
                        Assert.AreEqual(SkillMissileForm.Single, skill.missileForm,
                            $"skill {row.skillId} PC form 0 fallback");
                    }
                }
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
                Assert.AreEqual(row.manCastSndPath ?? "", skill.manCastSndPath ?? "", $"skill {row.skillId}.manCastSndPath");
                Assert.AreEqual(row.fmCastSndPath ?? "", skill.fmCastSndPath ?? "", $"skill {row.skillId}.fmCastSndPath");
                Assert.AreEqual(row.lvlSetScript ?? "", skill.lvlSetScript ?? "", $"skill {row.skillId}.lvlSetScript");
                Assert.AreEqual(row.levelUpScript ?? "", skill.levelUpScript ?? "", $"skill {row.skillId}.levelUpScript");
            }
        }

        private static Oracle LoadOracle()
        {
            var oracle = JsonUtility.FromJson<Oracle>(File.ReadAllText(OraclePath));
            Assert.IsNotNull(oracle);
            Assert.IsNotNull(oracle.rootSkillIds);
            Assert.IsNotNull(oracle.skills);
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
