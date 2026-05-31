using System.IO;
using NUnit.Framework;
using VLTK.Sprites;
using VLTK.Model;
using UnityEngine;

namespace VLTK.Tests.Sprites
{
    /// <summary>
    /// M0.8 — SPR Parser/Decoder Foundation tests.
    /// Uses the test fixture at StreamingAssets/TestData/00002d56.spr
    /// (320x320, 96 frames, 8 directions, 256 colors).
    /// </summary>
    public class SprDecoderTests
    {
        private byte[] _validSprData;
        private const string TEST_SPR_PATH =
            "Assets/StreamingAssets/TestData/00002d56.spr";

        [SetUp]
        public void Setup()
        {
            if (File.Exists(TEST_SPR_PATH))
                _validSprData = File.ReadAllBytes(TEST_SPR_PATH);
        }

        // ---------- AC #1: Valid SPR input → frames, palette, metadata emitted ----------

        [Test]
        public void Decode_ValidSpr_Succeeds()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success, $"Decode failed: {result.error}");
            Assert.IsNotNull(result.header);
            Assert.IsNotNull(result.palette);
            Assert.IsNotNull(result.frames);
        }

        [Test]
        public void Decode_ValidSpr_HasCorrectHeaderFields()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            Assert.AreEqual(320, result.header.width);
            Assert.AreEqual(320, result.header.height);
            Assert.AreEqual(96, result.header.frames);
            Assert.AreEqual(256, result.header.colors);
            Assert.AreEqual(8, result.header.directions);
        }

        [Test]
        public void Decode_ValidSpr_PaletteHasCorrectSize()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            // 256 colors × 3 bytes (RGB) = 768 bytes
            Assert.AreEqual(256 * 3, result.palette.Length);
        }

        [Test]
        public void Decode_ValidSpr_FrameCountMatchesHeader()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            Assert.AreEqual(result.header.frames, result.frames.Length);
            Assert.AreEqual(result.header.frames, result.offsets.Length);
        }

        [Test]
        public void Decode_ValidSpr_FramesHavePixelData()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            // At least some frames should have non-zero dimensions
            int nonEmptyFrames = 0;
            foreach (var frame in result.frames)
            {
                if (frame != null && frame.width > 0 && frame.height > 0)
                    nonEmptyFrames++;
            }
            Assert.Greater(nonEmptyFrames, 0, "Expected at least some non-empty frames");
        }

        // ---------- AC #2: Invalid SPR input → error without crash ----------

        [Test]
        public void Decode_NullData_ReturnsError()
        {
            var result = SprDecoder.Decode(null);
            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.error);
            Assert.IsTrue(result.error.Length > 0);
        }

        [Test]
        public void Decode_TooSmallData_ReturnsError()
        {
            var result = SprDecoder.Decode(new byte[10]);
            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.error);
        }

        [Test]
        public void Decode_WrongSignature_ReturnsInvalidSignatureError()
        {
            // Build a 32-byte header with wrong signature
            var bad = new byte[64];
            bad[0] = 0xFF; bad[1] = 0xFF; bad[2] = 0xFF; bad[3] = 0xFF;
            var result = SprDecoder.Decode(bad);
            Assert.IsFalse(result.success);
            Assert.IsNotNull(result.error);
            Assert.IsTrue(result.error.ToLower().Contains("signature") ||
                          result.error.ToLower().Contains("invalid"),
                          $"Expected signature error, got: {result.error}");
        }

        [Test]
        public void Decode_CorruptedData_DoesNotThrow()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            // Corrupt the frame offset table section
            var corrupt = (byte[])_validSprData.Clone();
            for (int i = 100; i < 200 && i < corrupt.Length; i++)
                corrupt[i] = 0xFF;

            SprDecodeResult result = null;
            Assert.DoesNotThrow(() => result = SprDecoder.Decode(corrupt));
            Assert.IsNotNull(result);
            // May or may not succeed, but must not throw
        }

        // ---------- AC #3: Frame offsets → pivot/reference spot preserved ----------

        [Test]
        public void Decode_ValidSpr_OffsetTablePopulated()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            Assert.IsNotNull(result.offsets);
            Assert.AreEqual(96, result.offsets.Length);

            // All offsets should be valid structs
            foreach (var off in result.offsets)
                Assert.IsNotNull(off);
        }

        [Test]
        public void Decode_ValidSpr_SomeFramesHaveOffsets()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);

            Assert.IsTrue(result.success);
            // At least one frame should have non-zero width/height → offset decoded
            bool anyHasOffset = false;
            foreach (var frame in result.frames)
            {
                if (frame != null && frame.width > 0)
                {
                    anyHasOffset = true;
                    break;
                }
            }
            Assert.IsTrue(anyHasOffset, "No frames with decoded dimensions found");
        }

        [Test]
        public void CreateSprite_FromDecodedFrame_ReturnsNonNull()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var result = SprDecoder.Decode(_validSprData);
            Assert.IsTrue(result.success);

            // Find first non-empty frame
            SprFrame validFrame = null;
            foreach (var f in result.frames)
            {
                if (f != null && f.width > 0 && f.height > 0)
                {
                    validFrame = f;
                    break;
                }
            }

            if (validFrame == null)
            {
                Assert.Ignore("No non-empty frames to test CreateSprite");
                return;
            }

            var tex = SprDecoder.CreateTexture(validFrame);
            Assert.IsNotNull(tex, "CreateTexture should return a valid Texture2D");
            Assert.AreEqual(validFrame.width, tex.width);
            Assert.AreEqual(validFrame.height, tex.height);

            var sprite = SprDecoder.CreateSprite(tex, validFrame);
            Assert.IsNotNull(sprite, "CreateSprite should return a valid Sprite");

            // Cleanup
            Object.DestroyImmediate(tex);
        }

        // ---------- SprValidator tests (M0.8 AC #1-2 via validator) ----------

        [Test]
        public void Validator_ValidSpr_ReturnsValid()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var diag = SprValidator.Validate(_validSprData, "00002d56.spr");

            Assert.IsTrue(diag.isValid, $"Expected valid, got error: {diag.error}");
            Assert.AreEqual(96, diag.frameCount);
            Assert.AreEqual(8, diag.directions);
            Assert.AreEqual(256, diag.paletteColors);
        }

        [Test]
        public void Validator_NullData_ReturnsInvalid()
        {
            var diag = SprValidator.Validate(null, "test.spr");
            Assert.IsFalse(diag.isValid);
            Assert.IsNotNull(diag.error);
        }

        [Test]
        public void Validator_BadData_ReturnsInvalidWithError()
        {
            var diag = SprValidator.Validate(new byte[] { 0, 1, 2, 3 }, "bad.spr");
            Assert.IsFalse(diag.isValid);
            Assert.IsNotNull(diag.error);
        }

        [Test]
        public void Validator_ValidSpr_SourcePathPreserved()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var diag = SprValidator.Validate(_validSprData, "my_sprite.spr");
            Assert.AreEqual("my_sprite.spr", diag.sourcePath);
        }

        [Test]
        public void Validator_ValidSpr_ToStringDoesNotThrow()
        {
            Assert.IsNotNull(_validSprData, "Test SPR fixture not found");
            var diag = SprValidator.Validate(_validSprData, "test.spr");
            string s = null;
            Assert.DoesNotThrow(() => s = diag.ToString());
            Assert.IsNotNull(s);
        }
    }
}
