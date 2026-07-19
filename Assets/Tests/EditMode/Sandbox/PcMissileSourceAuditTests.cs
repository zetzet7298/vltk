using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissileSourceAuditTests
    {
        private const string PcLooseServerMissles = "/var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/settings/Missles.txt";
        private const string PcPakUnpackedMissles = "/var/www/jx-source/pak_unpacked/slistcache/settings/missles.txt";
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
        public void ParseCanonicalPcMissles1Txt_ReportsExactPakTruthWithoutDuplicates()
        {
            string path = Path.Combine(PcAttribRoot, "missles1.txt");

            var audit = PcMissileSourceAudit.ParseFile(path);
            // Runtime winner is the slistcache.pak \settings\missles.txt payload.
            Assert.AreEqual("e893c7af74d43672f1513b8325e31ba3270ebe425ac668f1b444e81db845e8bc", audit.sha256);
            Assert.AreEqual(PcMissileSourceAudit.ExpectedHeaderSha256, audit.headerSha256);
            Assert.AreEqual(105850, audit.byteCount);
            Assert.AreEqual(514, audit.physicalLineCount);
            Assert.AreEqual(513, audit.dataRowCount);
            Assert.AreEqual(513, audit.parsedIdCount);
            Assert.AreEqual(513, audit.uniqueIdCount);
            Assert.AreEqual(0, audit.duplicateIdCount);
            Assert.AreEqual(1, audit.minMissileId);
            Assert.AreEqual(636, audit.maxMissileId);
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
            Assert.AreEqual(114, versusMissles1.differingRowByteCount);
            Assert.AreEqual(72, versusMissles1.idsOnlyInLeft.Length);
            Assert.AreEqual(0, versusMissles1.idsOnlyInRight.Length);
            CollectionAssert.Contains(versusMissles1.idsOnlyInLeft, 523);
            CollectionAssert.Contains(versusMissles1.idsOnlyInLeft, 636);
        }

        [Test]
        public void PakMisslesComparedWithRepoMissles_CountsChangedAndMissingRows()
        {
            var comparison = PcMissileSourceAudit.CompareFiles(
                Path.Combine(PcAttribRoot, "missles1.txt"),
                Path.Combine(PcAttribRoot, "missles.txt"));

            Assert.IsFalse(comparison.sameDataRowCount);
            Assert.IsFalse(comparison.sameIdSequence);
            Assert.IsFalse(comparison.sameUniqueIdSet);
            Assert.AreEqual(110, comparison.differingRowByteCount);
            Assert.AreEqual(72, comparison.idsOnlyInLeft.Length);
            Assert.AreEqual(0, comparison.idsOnlyInRight.Length);
        }

        [Test]
        public void MountedPcSources_MatchSelectedRepoCopies_WhenAvailable()
        {
            if (!File.Exists(PcLooseServerMissles) || !File.Exists(PcPakUnpackedMissles))
            {
                Assert.Ignore("PC source tree is not mounted in this environment.");
            }

            Assert.IsTrue(PcMissileSourceAudit.CompareFiles(
                PcLooseServerMissles,
                Path.Combine(ReferenceRoot, "ModMissles.txt")).exactBytes);
            Assert.IsTrue(PcMissileSourceAudit.CompareFiles(
                PcPakUnpackedMissles,
                Path.Combine(PcAttribRoot, "missles1.txt")).exactBytes);
        }

        [Test]
        public void PcMissileRegistry_RuntimeLoadsFullPakTableWith513UniqueIds()
        {
            string streamingAssets = Path.Combine(RepoRoot, "Assets/StreamingAssets");

            PcMissileRegistry.ClearAndInitialize(streamingAssets);

            // slistcache.pak missles payload has 513 unique missile rows (id 1..636).
            Assert.AreEqual(513, PcMissileRegistry.Count,
                "Runtime registry should load full PC missles1.txt unique-id coverage.");
            Assert.IsTrue(PcMissileRegistry.TryGet(1, out _), "First id 1 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(441, out _), "Last contiguous id 441 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(523, out _), "Post-migration id 523 must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(636, out _), "Post-migration id 636 (max) must resolve at runtime.");
            Assert.IsTrue(PcMissileRegistry.TryGet(408, out _), "id 408 must resolve.");
        }
    }
}
