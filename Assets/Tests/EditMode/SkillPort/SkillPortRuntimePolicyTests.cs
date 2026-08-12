using System.Collections.Generic;
using NUnit.Framework;
using VLTK.SkillPort;

namespace VLTK.Tests.SkillPort
{
    [Category("SkillPort")]
    public class SkillPortRuntimePolicyTests
    {
        private const string ReleaseId = "0f44f6d2-f1ca-4f2d-a228-4ea1875e59aa";
        private static readonly string ManifestHash = new string('a', 64);
        private static readonly string ProjectionHash = new string('b', 64);

        [Test]
        public void ContentReleaseSelector_SelectsOnlyExactInstalledDigest()
        {
            var active = new ContentReleaseDigest(ReleaseId, ManifestHash, ProjectionHash);
            var wrongPrevious = new ContentReleaseDigest(
                "8cc70752-eb52-4b29-a824-108ce177db11", new string('c', 64), new string('d', 64));
            var exact = new ContentReleaseDigest(ReleaseId, ManifestHash, ProjectionHash);

            ContentSelectionResult result = ContentReleaseSelector.SelectExact(
                active, new[] { wrongPrevious, exact });

            Assert.IsTrue(result.success);
            Assert.AreSame(exact, result.selected);
            Assert.AreEqual(ContentSelectionFailure.None, result.failure);
        }

        [Test]
        public void ContentReleaseSelector_BlocksProjectionHashMismatch()
        {
            var active = new ContentReleaseDigest(ReleaseId, ManifestHash, ProjectionHash);
            var installed = new ContentReleaseDigest(ReleaseId, ManifestHash, new string('c', 64));

            ContentSelectionResult result = ContentReleaseSelector.SelectExact(active, new[] { installed });

            Assert.IsFalse(result.success);
            Assert.AreEqual(ContentSelectionFailure.ProjectionHashMismatch, result.failure);
        }

        [Test]
        public void ContentDigest_RejectsNonCanonicalCaseAndUuid()
        {
            Assert.IsFalse(new ContentReleaseDigest(
                ReleaseId.ToUpperInvariant(), ManifestHash, ProjectionHash).IsCanonical());
            Assert.IsFalse(new ContentReleaseDigest(
                ReleaseId, ManifestHash.ToUpperInvariant(), ProjectionHash).IsCanonical());
        }

        [Test]
        public void RuntimePolicy_MissingOrKilledSkillFailsClosed()
        {
            var policy = new RuntimePolicySnapshot(7);
            Assert.AreEqual(SkillAuthorityMode.Disabled, policy.Resolve(100, "CaiBang").authorityMode);

            policy.SetSkill(new SkillRuntimeMode
            {
                skillId = 100,
                factionKey = "CaiBang",
                exposed = true,
                authorityMode = SkillAuthorityMode.GoActiveLegacyShadow,
                presentationMode = SkillPresentationMode.GraphV2,
            });
            Assert.AreEqual(
                SkillAuthorityMode.GoActiveLegacyShadow,
                policy.Resolve(100, "CaiBang").authorityMode);

            policy.SetFactionDisabled("CaiBang", true);
            Assert.AreEqual(SkillAuthorityMode.Disabled, policy.Resolve(100, "CaiBang").authorityMode);

            policy.SetFactionDisabled("CaiBang", false);
            policy.SetGlobalKillSwitch(true);
            Assert.AreEqual(SkillAuthorityMode.Disabled, policy.Resolve(100, "CaiBang").authorityMode);
        }

        [TestCase(4096, 256L * 1024 * 1024)]
        [TestCase(6144, 384L * 1024 * 1024)]
        [TestCase(8192, 512L * 1024 * 1024)]
        [TestCase(16384, 512L * 1024 * 1024)]
        public void AssetBudget_UsesApprovedMemoryTiers(int memoryMb, long expected)
        {
            Assert.AreEqual(expected, AssetMemoryBudget.ForSystemMemoryMegabytes(memoryMb));
        }

        [Test]
        public void EncounterGate_RequiresAllDependenciesBeforeReveal()
        {
            string a = new string('a', 64);
            string b = new string('b', 64);
            var gate = new EncounterPreloadGate();
            gate.Start("enc-1", new[]
            {
                new AssetDependency(a, 100),
                new AssetDependency(b, 200),
            }, nowMilliseconds: 0, assetBudgetBytes: 1000, activePinnedBytes: 100);

            Assert.IsFalse(gate.canReveal);
            Assert.AreEqual(
                EncounterPreloadState.Loading,
                gate.Evaluate(new HashSet<string> { a }, 9999));
            Assert.AreEqual(1, gate.missingHashes.Count);
            Assert.AreEqual(b, gate.missingHashes[0]);
            Assert.AreEqual(
                EncounterPreloadState.Ready,
                gate.Evaluate(new HashSet<string> { a, b }, 9999));
            Assert.IsTrue(gate.canReveal);
        }

        [Test]
        public void EncounterGate_AllowsOneRetryThenFails()
        {
            string hash = new string('a', 64);
            var gate = new EncounterPreloadGate();
            gate.Start("enc-1", new[] { new AssetDependency(hash, 100) }, 0, 1000, 0);

            Assert.AreEqual(
                EncounterPreloadState.RetryRequired,
                gate.Evaluate(new HashSet<string>(), 10000));
            Assert.IsTrue(gate.BeginRetryAfterEviction(10001));
            Assert.AreEqual(
                EncounterPreloadState.Failed,
                gate.Evaluate(new HashSet<string>(), 20001));
            Assert.AreEqual(EncounterPreloadFailure.Timeout, gate.failure);
        }

        [Test]
        public void EncounterGate_FailsWhenActiveAndRequiredExceedTier()
        {
            var gate = new EncounterPreloadGate();
            gate.Start("enc-1", new[] { new AssetDependency(new string('a', 64), 600) },
                0, assetBudgetBytes: 1000, activePinnedBytes: 500);

            Assert.AreEqual(EncounterPreloadState.Failed, gate.state);
            Assert.AreEqual(EncounterPreloadFailure.MemoryBudgetExceeded, gate.failure);
        }

        [Test]
        public void WorkingSetPlanner_EvictsOldestUnpinnedAssetDeterministically()
        {
            string old = new string('a', 64);
            string recent = new string('b', 64);
            string pinned = new string('c', 64);
            string required = new string('d', 64);
            var cache = new[]
            {
                new CachedAssetState { sha256 = old, sizeBytes = 100, resident = true, lastUsedSequence = 1 },
                new CachedAssetState { sha256 = recent, sizeBytes = 100, resident = true, lastUsedSequence = 2 },
                new CachedAssetState { sha256 = pinned, sizeBytes = 100, resident = true, pinnedByActiveEncounter = true },
            };

            AssetWorkingSetPlan plan = AssetWorkingSetPlanner.Plan(
                cache,
                new[] { new AssetDependency(required, 100) },
                budgetBytes: 300);

            Assert.IsTrue(plan.success);
            CollectionAssert.AreEqual(new[] { required }, plan.loadHashes);
            CollectionAssert.AreEqual(new[] { old }, plan.evictHashes);
            Assert.AreEqual(300, plan.projectedResidentBytes);
        }
    }
}
