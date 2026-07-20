using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VLTK.Backend;
using VLTK.Production.App;
using VLTK.Production.UI.Runtime;
using VLTK.Production.World.Unity;

namespace VLTK.Tests.Production.PlayMode
{
    public sealed class ProductionCompositionSmokeTests
    {
        [UnityTest]
        public IEnumerator Composition_PresentsMapAvatarAndAppliesJoystickMove_WithoutLiveBackend()
        {
            GameObject root = new GameObject("production-smoke-root");
            var composition = ProductionCompositionRoot.Create(root.transform);
            var manifest = Manifest();
            var validation = MapRuntimeValidator.Validate(Catalog(), manifest, new MapRuntimeSignatureFile { artifactSha256 = MapRuntimeContract.PinnedArtifactSha256, verification = new MapRuntimeSignatureVerification() }, MapRuntimeContract.PinnedArtifactSha256, MapRuntimeContract.PinnedProvenanceSha256, MapRuntimeContract.PinnedSignatureSha256, MapRuntimeTrustMode.EditorPinnedDigest);

            Assert.That(validation.ok, Is.True, validation.code);
            Assert.That(composition.mapRenderer.Present(manifest), Is.True);
            composition.avatarController.PlaceAt(composition.mapRenderer.Spawn, composition.mapRenderer.LoadedBounds);
            composition.avatarVisual.Present(Color.cyan);

            JoystickIntent intent = ProductionJoystickInput.Quantize(new Vector2(1f, 0f));
            composition.avatarController.ApplyMoveIntent(intent.move, 0.1f);
            yield return null;

            Assert.That(composition.mapRenderer.LoadedMapId, Is.EqualTo(53));
            Assert.That(composition.avatarController.transform.position.x, Is.GreaterThan(manifest.spawn.world.x));
            Assert.That(composition.avatarVisual.LastMoveInput.x, Is.GreaterThan(0f));
            Object.Destroy(root);
        }

        [UnityTest]
        public IEnumerator Bootstrapper_BindsPythonClientFlowToProductionAvatar()
        {
            GameObject root = new GameObject("production-bootstrapper-playmode");
            root.SetActive(false);
            var bootstrapper = root.AddComponent<ProductionBootstrapper>();
            var config = ScriptableObject.CreateInstance<BackendConfig>();
            config.baseUrl = "http://127.0.0.1:8020";
            config.apiPrefix = "/v1";
            config.useMock = true;

            root.SetActive(true);
            BackendClientRunner runner = bootstrapper.BackendRunner;
            runner.runOnStart = false;
            runner.ConfigOverride = config;
            var task = runner.RunAsync(default);
            while (!task.IsCompleted)
                yield return null;

            Assert.That(task.IsFaulted, Is.False);
            Assert.That(runner.LastError, Is.Null);
            Assert.That(runner.enterMapId, Is.EqualTo(53));
            Assert.That(runner.SyncedRoleId, Is.EqualTo(1));
            Assert.That(runner.MovementSync, Is.Not.Null);
            Assert.That(runner.MovementSync.IsBound, Is.True);

            Object.Destroy(root);
            Object.Destroy(config);
            yield return null;
        }

        private static MapRuntimeCatalog Catalog() => new MapRuntimeCatalog { schema = MapRuntimeContract.CatalogSchema, security = new MapRuntimeCatalogSecurity(), maps = new[] { new MapRuntimeCatalogMap { mapId = 53, artifact = "map-runtime.v1.json", sha256 = MapRuntimeContract.PinnedArtifactSha256 } } };
        private static MapRuntimeManifest Manifest() => new MapRuntimeManifest { schema = MapRuntimeContract.ManifestSchema, mapId = 53, canonicalIdentity = new CanonicalIdentity { mapId = 53 }, sourceProvenanceSha256 = MapRuntimeContract.PinnedProvenanceSha256, bounds = new MapRuntimeBounds { world = new MapRuntimeWorldRect { x = 41984, y = -55808, width = 16896, height = 10240 } }, spawn = new MapRuntimeSpawn { world = new MapRuntimeWorldPoint { x = 50432, y = -50432 }, regionCell = new[] { 98, 98 } }, movement = new MapRuntimeMovement { rules = new MapRuntimeMovementRules { allowMapIds = new[] { 53 }, rejectMapIds = new[] { 79 }, requiresWalkableRegionCell = true } } };
    }
}
