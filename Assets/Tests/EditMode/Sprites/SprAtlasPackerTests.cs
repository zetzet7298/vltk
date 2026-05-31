using System.IO;
using NUnit.Framework;
using VLTK.Sprites;
using VLTK.Sandbox;
using VLTK.Model;
using UnityEngine;

namespace VLTK.Tests.Sprites
{
    /// <summary>
    /// M0.8 AC#4 — Atlas packing and Asset Registry integration.
    /// </summary>
    public class SprAtlasPackerTests
    {
        private byte[] _validSprData;
        private const string TEST_SPR_PATH = "Assets/StreamingAssets/TestData/00002d56.spr";

        private SourceAssetId MakeSourceId(string path = "sprites/test.spr", int uid = 1)
            => new SourceAssetId
            {
                sourcePath = path,
                packageName = "spr_pak",
                uid = uid,
                resourceKind = ResourceKind.Sprite,
                discoveryTool = DiscoveryTool.Manual,
            };

        [SetUp]
        public void Setup()
        {
            if (File.Exists(TEST_SPR_PATH))
                _validSprData = File.ReadAllBytes(TEST_SPR_PATH);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup any created textures handled by Unity GC
        }

        // --- AC#4: atlas packing runs → output loadable via Asset Registry ---

        [Test]
        public void Pack_ValidDecodeResult_Succeeds()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            Assert.IsTrue(decoded.success);

            var result = SprAtlasPacker.Pack(decoded, MakeSourceId());

            Assert.IsTrue(result.success, $"Pack failed: {result.error}");
            Assert.IsNotNull(result.atlas);
            Assert.IsNotNull(result.frameRects);
            Assert.IsNotNull(result.clipDefinition);
        }

        [Test]
        public void Pack_ValidDecodeResult_AtlasIsPow2()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var result = SprAtlasPacker.Pack(decoded, MakeSourceId());

            Assert.IsTrue(result.success);
            Assert.Greater(result.atlas.width, 0);
            Assert.Greater(result.atlas.height, 0);

            // Width and height should be power of 2
            Assert.AreEqual(0, result.atlas.width & (result.atlas.width - 1),
                $"Atlas width {result.atlas.width} is not power of 2");
            Assert.AreEqual(0, result.atlas.height & (result.atlas.height - 1),
                $"Atlas height {result.atlas.height} is not power of 2");

            Object.DestroyImmediate(result.atlas);
        }

        [Test]
        public void Pack_ValidDecodeResult_FrameRectCountMatchesFrames()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var result = SprAtlasPacker.Pack(decoded, MakeSourceId());

            Assert.IsTrue(result.success);
            Assert.AreEqual(decoded.frames.Length, result.frameRects.Length);

            Object.DestroyImmediate(result.atlas);
        }

        [Test]
        public void Pack_WithRegistry_RegistersAtlasEntry()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var registry = new AssetRegistry();
            var sourceId = MakeSourceId("sprites/00002d56.spr", uid: 999);

            var result = SprAtlasPacker.Pack(decoded, sourceId, entry => registry.Register(entry));

            Assert.IsTrue(result.success, $"Pack failed: {result.error}");

            // M0.8 AC#4: Sprite frames can be loaded through Asset Registry
            var entry = registry.Resolve("sprites/00002d56.spr");
            Assert.IsNotNull(entry, "Atlas should be registered in Asset Registry");
            Assert.AreEqual(ArtifactType.SpriteAtlas, entry.artifactType);
            Assert.AreEqual(AssetStatus.Available, entry.status);

            Object.DestroyImmediate(result.atlas);
        }

        [Test]
        public void Pack_WithRegistry_ResolveByUid_Works()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var registry = new AssetRegistry();
            var sourceId = MakeSourceId("sprites/00002d56.spr", uid: 42);

            SprAtlasPacker.Pack(decoded, sourceId, entry => registry.Register(entry));

            var entry = registry.Resolve(42);
            Assert.IsNotNull(entry, "Should resolve atlas by uid");

            Object.DestroyImmediate(SprDecoder.CreateTexture(decoded.frames[0]));
        }

        [Test]
        public void Pack_ClipDefinition_HasCorrectFrameCount()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var result = SprAtlasPacker.Pack(decoded, MakeSourceId());

            Assert.IsTrue(result.success);
            Assert.AreEqual(96, result.clipDefinition.frameCount);
            Assert.AreEqual(8, result.clipDefinition.directionCount);
            Assert.IsNotNull(result.clipDefinition.frameOffsets);
            Assert.AreEqual(96, result.clipDefinition.frameOffsets.Length);

            Object.DestroyImmediate(result.atlas);
        }

        [Test]
        public void Pack_ClipDefinition_AtlasRefMatchesRegistryKey()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);
            var registry = new AssetRegistry();
            var sourceId = MakeSourceId("sprites/test.spr", uid: 77);

            var result = SprAtlasPacker.Pack(decoded, sourceId, entry => registry.Register(entry));

            Assert.IsTrue(result.success);
            Assert.AreEqual(result.atlasKey, result.clipDefinition.atlasRef);

            Object.DestroyImmediate(result.atlas);
        }

        [Test]
        public void Pack_NullDecodeResult_ReturnsError()
        {
            var result = SprAtlasPacker.Pack(null, MakeSourceId());
            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.error);
        }

        [Test]
        public void Pack_FailedDecodeResult_ReturnsError()
        {
            var failed = new SprDecodeResult { success = false, error = "bad data" };
            var result = SprAtlasPacker.Pack(failed, MakeSourceId());
            Assert.IsFalse(result.success);
        }

        [Test]
        public void Pack_WithoutRegistry_DoesNotThrow()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var decoded = SprDecoder.Decode(_validSprData);

            SprAtlasPacker.AtlasPackResult result = null;
            Assert.DoesNotThrow(() => result = SprAtlasPacker.Pack(decoded, MakeSourceId(), null));
            Assert.IsTrue(result.success);

            Object.DestroyImmediate(result.atlas);
        }
    }
}
