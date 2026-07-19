using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("TangMen")]
    public sealed class TangMenRelationshipPlayerSafeTests
    {
        private const string ResourcePath = "Reference/PcTangMenRelationshipTargets";
        private const string Sha256 = "888c93cde48ec22160e12386580bca3aafc2b74d5bc16ba21b70c06a9a8007ba";
        private static string CanonicalPath => Path.Combine(Directory.GetCurrentDirectory(),
            "Assets", "StreamingAssets", "Reference", "PcTangMenRelationshipTargets.txt");

        [Test]
        public void PackagedRelationshipSlice_IsExactCanonicalBytes()
        {
            var bundled = UnityEngine.Resources.Load<TextAsset>(ResourcePath);
            Assert.IsNotNull(bundled, "Android-safe Resources TextAsset missing");
            var canonical = File.ReadAllBytes(CanonicalPath);
            Assert.AreEqual(Sha256, Sha256Hex(canonical), "canonical slice provenance drifted");
            Assert.AreEqual(Sha256, Sha256Hex(bundled.bytes), "packaged slice provenance drifted");
            CollectionAssert.AreEqual(canonical, bundled.bytes, "Resources bytes must stay canonical");
        }

        [Test]
        public void Catalog_LoadsRelationshipTargetFromPackagedSlice()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: false, includeShaolin: false, includeTangMen: true,
                includeEMei: false, includeTianWang: false, includeWuDu: false,
                includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            var eventSkill = catalog.Resolve(352);
            Assert.IsNotNull(eventSkill, "relationship event 352 must resolve");
            Assert.AreEqual(162, eventSkill.childSkillId);
            Assert.AreEqual(SkillMissileForm.Stance, eventSkill.missileForm);
            Assert.IsFalse(eventSkill.byMissile, "canonical event row must remain non-byMissile");
        }

        private static string Sha256Hex(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
