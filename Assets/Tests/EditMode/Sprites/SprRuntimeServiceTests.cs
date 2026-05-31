using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sprites;

namespace VLTK.Tests.EditMode.Sprites
{
    public class SprRuntimeServiceTests
    {
        private SprRuntimeService _service;
        private string _testDir;

        // Minimal valid SPR: header(32) + palette(6) + 1 offset entry(8) + 1 frame
        // Frame: 2+2+2+2 + RLE data
        private static byte[] MakeMinimalSpr(ushort w = 4, ushort h = 4)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            // Header (32 bytes)
            bw.Write(new byte[] { 0x53, 0x50, 0x52, 0x00 }); // "SPR\0"
            bw.Write((ushort)w);    // width
            bw.Write((ushort)h);    // height
            bw.Write((ushort)2);    // centerX
            bw.Write((ushort)2);    // centerY
            bw.Write((ushort)1);    // frames
            bw.Write((ushort)2);    // colors (need at least 2)
            bw.Write((ushort)1);    // directions
            bw.Write((ushort)100);  // interval
            bw.Write(new byte[12]); // reserved[6]

            // Palette: 2 colors * 3 bytes = 6 bytes
            bw.Write(new byte[] { 255, 0, 0 });   // color 0 = red
            bw.Write(new byte[] { 0, 255, 0 });   // color 1 = green

            // Offset table: 1 entry (offset=0, length)
            // Frame data starts after offset table
            // Frame header: 8 bytes + pixel data
            // For a 4x4 frame, all transparent (simplest):
            //   Each row: 1 run (runLength=4, alpha=0) = 2 bytes per row = 8 bytes
            int frameDataSize = 8 + h * 2; // frame header + rows
            bw.Write((uint)0);                    // offset
            bw.Write((uint)frameDataSize);        // length

            // Frame data
            bw.Write((ushort)w);    // frame width
            bw.Write((ushort)h);    // frame height
            bw.Write((short)0);     // offsetX
            bw.Write((short)0);     // offsetY

            // RLE rows: all transparent
            for (int row = 0; row < h; row++)
            {
                bw.Write((byte)w);  // run length = full width
                bw.Write((byte)0);  // alpha = 0 (transparent)
            }

            return ms.ToArray();
        }

        private static byte[] MakeSprWithPixels(ushort w = 4, ushort h = 4)
        {
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            bw.Write(new byte[] { 0x53, 0x50, 0x52, 0x00 });
            bw.Write(w);
            bw.Write(h);
            bw.Write((ushort)2);
            bw.Write((ushort)2);
            bw.Write((ushort)1);    // 1 frame
            bw.Write((ushort)256);  // 256 palette colors
            bw.Write((ushort)1);
            bw.Write((ushort)100);
            bw.Write(new byte[12]);

            // Palette: 256 colors
            for (int i = 0; i < 256 * 3; i++)
                bw.Write((byte)(i % 256));

            int frameDataSize = 8 + h * (1 + 1 + w); // header + rows: run(1) + alpha(1) + w color indices
            bw.Write((uint)0);
            bw.Write((uint)frameDataSize);

            bw.Write(w);
            bw.Write(h);
            bw.Write((short)0);
            bw.Write((short)0);

            // RLE rows: solid fill with color index 0
            for (int row = 0; row < h; row++)
            {
                bw.Write((byte)w);    // run length
                bw.Write((byte)255);  // alpha = fully opaque
                for (int col = 0; col < w; col++)
                    bw.Write((byte)0); // color index 0
            }

            return ms.ToArray();
        }

        [SetUp]
        public void SetUp()
        {
            _testDir = Path.Combine(Path.GetTempPath(), $"SprRuntimeTest_{System.Guid.NewGuid():N}");
            Directory.CreateDirectory(_testDir);
            _service = new SprRuntimeService(_testDir);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.ClearCache();
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, true);
        }

        [Test]
        public void ResolveSprite_NullOrEmpty_ReturnsNull()
        {
            Assert.IsNull(_service.ResolveSprite(null));
            Assert.IsNull(_service.ResolveSprite(""));
        }

        [Test]
        public void ResolveSprite_NotFound_ReturnsNullAndCachesMiss()
        {
            var sprite = _service.ResolveSprite("nonexistent_sprite");
            Assert.IsNull(sprite);
            Assert.AreEqual(1, _service.MissCount);
            Assert.AreEqual(0, _service.CacheCount);
        }

        [Test]
        public void ResolveSprite_Found_DecodesAndReturnsSprite()
        {
            var sprData = MakeSprWithPixels();
            File.WriteAllBytes(Path.Combine(_testDir, "test_spr.spr"), sprData);

            var sprite = _service.ResolveSprite("test_spr");
            Assert.IsNotNull(sprite);
            Assert.AreEqual(1, _service.CacheCount);
            Assert.AreEqual(0, _service.MissCount);
        }

        [Test]
        public void ResolveSprite_CachesResult_SecondCallIsSame()
        {
            var sprData = MakeSprWithPixels();
            File.WriteAllBytes(Path.Combine(_testDir, "cached.spr"), sprData);

            var first = _service.ResolveSprite("cached");
            var second = _service.ResolveSprite("cached");

            Assert.IsNotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, _service.CacheCount);
        }

        [Test]
        public void ResolveSprite_MissIsCached_SecondCallSkipsDisk()
        {
            _service.ResolveSprite("missing");
            _service.ResolveSprite("missing");

            Assert.AreEqual(1, _service.MissCount);
        }

        [Test]
        public void ResolveSpriteOrDefault_Missing_ReturnsFallback()
        {
            var sprite = _service.ResolveSpriteOrDefault("nonexistent", 16, 16);
            Assert.IsNotNull(sprite);
            Assert.AreEqual(16, (int)sprite.rect.width);
            Assert.AreEqual(16, (int)sprite.rect.height);
        }

        [Test]
        public void ResolveSpriteOrDefault_Found_ReturnsRealSprite()
        {
            var sprData = MakeSprWithPixels(8, 8);
            File.WriteAllBytes(Path.Combine(_testDir, "real.spr"), sprData);

            var sprite = _service.ResolveSpriteOrDefault("real", 16, 16);
            Assert.IsNotNull(sprite);
            Assert.AreEqual(8, (int)sprite.rect.width);
        }

        [Test]
        public void PreloadAll_LoadsAllSprFiles()
        {
            File.WriteAllBytes(Path.Combine(_testDir, "a.spr"), MakeSprWithPixels(4, 4));
            File.WriteAllBytes(Path.Combine(_testDir, "b.spr"), MakeSprWithPixels(8, 8));
            File.WriteAllBytes(Path.Combine(_testDir, "c.spr"), MakeSprWithPixels(2, 2));
            // Non-SPR file should be ignored
            File.WriteAllText(Path.Combine(_testDir, "d.txt"), "not a spr");

            int count = _service.PreloadAll();
            Assert.AreEqual(3, count);
            Assert.AreEqual(3, _service.CacheCount);
        }

        [Test]
        public void PreloadAll_EmptyDir_ReturnsZero()
        {
            int count = _service.PreloadAll();
            Assert.AreEqual(0, count);
        }

        [Test]
        public void PreloadAll_DirNotExist_ReturnsZero()
        {
            var svc = new SprRuntimeService("/nonexistent/path");
            int count = svc.PreloadAll();
            Assert.AreEqual(0, count);
        }

        [Test]
        public void ClearCache_FreesAll()
        {
            File.WriteAllBytes(Path.Combine(_testDir, "x.spr"), MakeSprWithPixels());
            _service.ResolveSprite("x");
            Assert.AreEqual(1, _service.CacheCount);

            _service.ClearCache();
            Assert.AreEqual(0, _service.CacheCount);
            Assert.AreEqual(0, _service.MissCount);
        }

        [Test]
        public void GetDiagnostic_ReturnsInfoForResolvedSprite()
        {
            File.WriteAllBytes(Path.Combine(_testDir, "diag.spr"), MakeSprWithPixels());
            _service.ResolveSprite("diag");

            var diag = _service.GetDiagnostic("diag");
            Assert.IsNotNull(diag);
            Assert.IsTrue(diag.isValid);
        }

        [Test]
        public void GetAllDiagnostics_TracksResolvedSprites()
        {
            File.WriteAllBytes(Path.Combine(_testDir, "d1.spr"), MakeSprWithPixels());
            _service.ResolveSprite("d1");

            var all = _service.GetAllDiagnostics();
            Assert.AreEqual(1, all.Count);
        }

        [Test]
        public void ResolveSprite_WithBackslashPath_ResolvesByUID()
        {
            // Ground/builtin objects use paths like "image\effect\00002d56"
            // SprRuntimeService should extract the UID portion
            var sprData = MakeSprWithPixels();
            var uid = "abc12345";
            File.WriteAllBytes(Path.Combine(_testDir, $"{uid}.spr"), sprData);

            var sprite = _service.ResolveSprite($"image\\effect\\{uid}");
            Assert.IsNotNull(sprite);
        }

        [Test]
        public void ComputePathUidHex_MatchesVltktoolHasher()
        {
            Assert.AreEqual("50d1a3a0", SprRuntimeService.ComputePathUidHex("\\image\\effect\\abc12345.spr"));
            Assert.AreEqual("50d1a3a0", SprRuntimeService.ComputePathUidHex("\\Image\\Effect\\ABC12345.SPR"));
            Assert.AreEqual("bccbbad2", SprRuntimeService.ComputePathUidHex("\\游戏资源\\美术图素\\野外\\st_01.spr"));
        }

        [Test]
        public void NormalizeResourcePath_AddsLeadingBackslashAndStripsNull()
        {
            Assert.AreEqual("\\image\\effect\\abc.spr", SprRuntimeService.NormalizeResourcePath("image/effect/abc.spr\0"));
            Assert.AreEqual("\\image\\effect\\abc.spr", SprRuntimeService.NormalizeResourcePath("\\image\\effect\\abc.spr"));
        }

        [Test]
        public void ResolveSprite_SourcePath_ResolvesByComputedPakUid()
        {
            var sprData = MakeSprWithPixels();
            File.WriteAllBytes(Path.Combine(_testDir, "50d1a3a0.spr"), sprData);

            var sprite = _service.ResolveSprite("\\image\\effect\\abc12345.spr");
            Assert.IsNotNull(sprite);
        }

        [Test]
        public void ResolveSprite_SourcePathWithNullTerminator_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _service.ResolveSprite("\\游戏资源\\室外地表\\中型地表图素\\黄稀.spr\0"));
        }

        [Test]
        public void ResolveSprite_HexUID_ResolvesDirectly()
        {
            var sprData = MakeSprWithPixels();
            File.WriteAllBytes(Path.Combine(_testDir, "00002d56.spr"), sprData);

            var sprite = _service.ResolveSprite("00002d56");
            Assert.IsNotNull(sprite);
        }

        [Test]
        public void ResolveSprite_TransparentFrame_ReturnsSprite()
        {
            // All-transparent frame should still create a valid sprite
            var sprData = MakeMinimalSpr(4, 4);
            File.WriteAllBytes(Path.Combine(_testDir, "transparent.spr"), sprData);

            var sprite = _service.ResolveSprite("transparent");
            Assert.IsNotNull(sprite);
            Assert.AreEqual(4, (int)sprite.rect.width);
            Assert.AreEqual(4, (int)sprite.rect.height);
        }
    }
}
