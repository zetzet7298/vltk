using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissileSourceAuditTests
    {
        private const string PcServerSettings = "/var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/settings";
        private static string RepoRoot => Directory.GetCurrentDirectory();
        private static string ReferenceRoot => Path.Combine(RepoRoot, "Assets/StreamingAssets/Reference");
        private static string PcAttribRoot => Path.Combine(ReferenceRoot, "PcAttrib");

        [Test]
        public void ParseCanonicalPcMisslesTxt_ReportsExactSourceTruth()
        {
            string path = Path.Combine(PcAttribRoot, "missles.txt");

            var audit = PcMissileSourceAudit.ParseFile(path);

            Assert.AreEqual("6c5e1720905278e09e7dc7fc8786b6062b07edf2f9ffd2113aec86a5566d7b4a", audit.sha256);
            Assert.AreEqual(PcMissileSourceAudit.ExpectedHeaderSha256, audit.headerSha256);
            Assert.AreEqual(89787, audit.byteCount);
            Assert.AreEqual(442, audit.physicalLineCount);
            Assert.AreEqual(441, audit.dataRowCount);
            Assert.AreEqual(441, audit.parsedIdCount);
            Assert.AreEqual(441, audit.uniqueIdCount);
            Assert.AreEqual(0, audit.duplicateIdCount);
            Assert.AreEqual(1, audit.minMissileId);
            Assert.AreEqual(441, audit.maxMissileId);
            Assert.IsTrue(audit.HasExactPcMissileSchema);
        }

        [Test]
        public void ParseCanonicalPcMissles1Txt_ReportsExactSourceTruthAndDuplicateId408()
        {
            string path = Path.Combine(PcAttribRoot, "missles1.txt");

            var audit = PcMissileSourceAudit.ParseFile(path);

            Assert.AreEqual("94b1c29ce689c5432e9c21e39e0d374982df7e13f7f058de08b95677520f83cf", audit.sha256);
            Assert.AreEqual(PcMissileSourceAudit.ExpectedHeaderSha256, audit.headerSha256);
            Assert.AreEqual(95797, audit.byteCount);
            Assert.AreEqual(468, audit.physicalLineCount);
            Assert.AreEqual(467, audit.dataRowCount);
            Assert.AreEqual(467, audit.parsedIdCount);
            Assert.AreEqual(466, audit.uniqueIdCount);
            Assert.AreEqual(1, audit.duplicateIdCount);
            Assert.AreEqual(1, audit.minMissileId);
            Assert.AreEqual(467, audit.maxMissileId);
            CollectionAssert.AreEqual(new[] { 408 }, audit.duplicateMissileIds);
            Assert.IsTrue(audit.HasExactPcMissileSchema);
        }

        [Test]
        public void ExistingPcMisslesTxt_IsNotByteExactPcSourceButKeepsMisslesTxtIdSequence()
        {
            string sourcePath = Path.Combine(PcAttribRoot, "missles.txt");
            string existingPath = Path.Combine(ReferenceRoot, "PcMissles.txt");

            var comparison = PcMissileSourceAudit.CompareFiles(sourcePath, existingPath);

            Assert.IsFalse(comparison.exactBytes);
            Assert.IsTrue(comparison.sameHeaderSchema);
            Assert.IsTrue(comparison.sameDataRowCount);
            Assert.IsTrue(comparison.sameIdSequence);
            Assert.IsTrue(comparison.sameUniqueIdSet);
            Assert.AreEqual(440, comparison.differingRowByteCount);
            Assert.AreEqual("7999eb0b7a892b4eb2f2b43cb577457ce985f5f3d1cf94770bc26a4c90035f8a", comparison.right.sha256);
        }

        [Test]
        public void ExistingModMisslesTxt_IsMisslesTxtShapeButNotPcMissles1Source()
        {
            string misslesPath = Path.Combine(PcAttribRoot, "missles.txt");
            string missles1Path = Path.Combine(PcAttribRoot, "missles1.txt");
            string existingPath = Path.Combine(ReferenceRoot, "ModMissles.txt");

            var versusMissles = PcMissileSourceAudit.CompareFiles(misslesPath, existingPath);
            var versusMissles1 = PcMissileSourceAudit.CompareFiles(missles1Path, existingPath);

            Assert.IsFalse(versusMissles.exactBytes);
            Assert.IsTrue(versusMissles.sameDataRowCount);
            Assert.IsTrue(versusMissles.sameIdSequence);
            Assert.AreEqual(5, versusMissles.differingRowByteCount);
            Assert.AreEqual("4f79cde57b199747ce1fa65216c46ede3596c8e5c9bafd1b73c8f137269ee66e", versusMissles.right.sha256);

            Assert.IsFalse(versusMissles1.sameDataRowCount);
            Assert.IsFalse(versusMissles1.sameIdSequence);
            CollectionAssert.AreEqual(new[] { 442, 443, 444, 445, 446, 447, 448, 449, 450, 451, 452, 453, 454, 455, 456, 457, 458, 459, 460, 461, 462, 463, 464, 465, 466, 467 }, versusMissles1.idsOnlyInLeft);
        }

        [Test]
        public void ServerPcSourceFiles_MatchCanonicalRepoPcAttribCopies_WhenAvailable()
        {
            string serverMissles = Path.Combine(PcServerSettings, "missles.txt");
            string serverMissles1 = Path.Combine(PcServerSettings, "missles1.txt");
            if (!File.Exists(serverMissles) || !File.Exists(serverMissles1))
            {
                Assert.Ignore("PC source tree is not mounted in this environment.");
            }

            Assert.IsTrue(PcMissileSourceAudit.CompareFiles(serverMissles, Path.Combine(PcAttribRoot, "missles.txt")).exactBytes);
            Assert.IsTrue(PcMissileSourceAudit.CompareFiles(serverMissles1, Path.Combine(PcAttribRoot, "missles1.txt")).exactBytes);
        }
        [Test]
        public void PcMissileRegistry_RuntimeLoadsFullMissles1TableAndLateDuplicate408Wins()
        {
            string streamingAssets = Path.Combine(RepoRoot, "Assets/StreamingAssets");

            PcMissileRegistry.ClearAndInitialize(streamingAssets);

            Assert.AreEqual(466, PcMissileRegistry.Count, "Runtime registry should load full PC missles1.txt unique-id coverage; id 408 is duplicated in source.");
            Assert.IsTrue(PcMissileRegistry.TryGet(442, out var missile442), "missles1-only id 442 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(443, out var missile443), "missles1-only id 443 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(467, out var missile467), "missles1-only id 467 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(408, out var missile408), "duplicated id 408 must still resolve.");
            Assert.AreEqual(32, missile442.speed);
            Assert.AreEqual(5, missile443.lifetime);
            Assert.AreEqual(156, missile467.speed);
            StringAssert.Contains("Truy Phong", missile408.nameNormalized, "Duplicate policy is last-row-wins, matching sequential PC table load semantics.");
        }
    }
}
