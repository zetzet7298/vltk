using NUnit.Framework;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Model
{
    public class AssetRegistryTests
    {
        private AssetRegistry BuildRegistry(params AssetRegistryEntry[] entries)
        {
            var reg = new AssetRegistry();
            foreach (var e in entries)
                reg.Register(e);
            return reg;
        }

        private AssetRegistryEntry MakeEntry(string path, int uid = 0,
            AssetStatus status = AssetStatus.Available,
            ArtifactType type = ArtifactType.MapDefinition)
        {
            return new AssetRegistryEntry
            {
                sourceId = new SourceAssetId
                {
                    sourcePath = path,
                    packageName = "maps_pak",
                    uid = uid,
                    resourceKind = ResourceKind.Map,
                },
                artifactType = type,
                unityAssetPath = $"Assets/Generated/{path}",
                loadMode = LoadMode.StreamingAssets,
                status = status,
            };
        }

        // --- M0.6 AC #1: resolve by source path ---
        [Test]
        public void Resolve_ByPath_ReturnsCorrectEntry()
        {
            var entry = MakeEntry("maps/971b75ae.dat", uid: 1);
            var reg = BuildRegistry(entry);

            var result = reg.Resolve("maps/971b75ae.dat");

            Assert.IsNotNull(result);
            Assert.AreEqual("maps/971b75ae.dat", result.sourceId.sourcePath);
        }

        // --- M0.6 AC #1: resolve by uid ---
        [Test]
        public void Resolve_ByUid_ReturnsCorrectEntry()
        {
            var entry = MakeEntry("maps/abc.dat", uid: 42);
            var reg = BuildRegistry(entry);

            var result = reg.Resolve(42);

            Assert.IsNotNull(result);
            Assert.AreEqual(42, result.sourceId.uid);
        }

        // --- M0.6 AC #1: resolve by SourceAssetId prefers path ---
        [Test]
        public void Resolve_BySourceAssetId_PrefersPath()
        {
            var entry = MakeEntry("maps/abc.dat", uid: 42);
            var reg = BuildRegistry(entry);

            var id = new SourceAssetId { sourcePath = "maps/abc.dat", uid = 42 };
            var result = reg.Resolve(id);

            Assert.IsNotNull(result);
            Assert.AreEqual("maps/abc.dat", result.sourceId.sourcePath);
        }

        // --- M0.5 AC #2: missing asset returns Missing status ---
        [Test]
        public void Resolve_UnknownPath_ReturnsNull()
        {
            var reg = new AssetRegistry();
            var result = reg.Resolve("nonexistent/path.dat");
            Assert.IsNull(result);
        }

        // --- M0.5 AC #2: registry can report Missing status diagnostics ---
        [Test]
        public void GetByStatus_Missing_ReturnsMissingEntries()
        {
            var reg = BuildRegistry(
                MakeEntry("maps/ok.dat", uid: 1, status: AssetStatus.Available),
                MakeEntry("maps/bad.dat", uid: 2, status: AssetStatus.Missing),
                MakeEntry("maps/inv.dat", uid: 3, status: AssetStatus.Invalid)
            );

            var missing = reg.GetByStatus(AssetStatus.Missing);
            Assert.AreEqual(1, missing.Count);
            Assert.AreEqual("maps/bad.dat", missing[0].sourceId.sourcePath);
        }

        // --- M0.6 AC #2: duplicate source identity reported ---
        [Test]
        public void Validate_DuplicateKey_ReportsWarning()
        {
            var e1 = MakeEntry("maps/dup.dat", uid: 1);
            var e2 = MakeEntry("maps/dup.dat", uid: 1);  // same key

            var reg = BuildRegistry(e1, e2);
            var result = reg.Validate();

            Assert.IsTrue(result.Warnings.Count > 0);
            Assert.IsTrue(result.Warnings.Exists(w => w.Contains("dup") || w.Contains("Duplicate")));
        }

        // --- M0.6 AC #3: invalid asset reported ---
        [Test]
        public void Validate_InvalidEntry_ReportsError()
        {
            var reg = BuildRegistry(MakeEntry("maps/corrupt.dat", uid: 5, status: AssetStatus.Invalid));
            var result = reg.Validate();

            Assert.IsFalse(result.IsOk);
            Assert.IsTrue(result.Errors.Count > 0);
        }

        // --- M0.5 AC #1: converted artifact is registered and resolved ---
        [Test]
        public void Register_ThenResolve_ReturnsSameEntry()
        {
            var reg = new AssetRegistry();
            var entry = MakeEntry("sprites/hero.spr", uid: 100,
                status: AssetStatus.Available, type: ArtifactType.SpriteAtlas);
            reg.Register(entry);

            var result = reg.Resolve("sprites/hero.spr");

            Assert.IsNotNull(result);
            Assert.AreEqual(ArtifactType.SpriteAtlas, result.artifactType);
            Assert.AreEqual(AssetStatus.Available, result.status);
        }

        // --- GetAll returns all registered entries ---
        [Test]
        public void GetAll_ReturnsAllRegisteredEntries()
        {
            var reg = BuildRegistry(
                MakeEntry("a.dat", uid: 1),
                MakeEntry("b.dat", uid: 2),
                MakeEntry("c.dat", uid: 3)
            );

            Assert.AreEqual(3, reg.GetAll().Count);
        }
    }
}
