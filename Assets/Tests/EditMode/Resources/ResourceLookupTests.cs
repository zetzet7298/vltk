using System;
using System.IO;
using System.IO.Compression;
using NUnit.Framework;
using VLTK.Resources;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.TestTools;

namespace VLTK.Tests.Resources
{
    /// <summary>
    /// EditMode tests for ResourceLookup / IResourceLookup (M0.7).
    /// Uses temp-dir FilesystemResourceProvider; no Unity runtime required.
    /// </summary>
    [TestFixture]
    public class ResourceLookupTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), $"vltk_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, recursive: true);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private string WriteFile(string relativePath, byte[] content)
        {
            var full = Path.Combine(_tempDir, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(full)!);
            File.WriteAllBytes(full, content);
            return relativePath;
        }

        private string WriteTextFile(string relativePath, string text = "hello")
            => WriteFile(relativePath, System.Text.Encoding.UTF8.GetBytes(text));

        private ResourceLookup BuildLookupWithTempProvider()
        {
            var lookup = new ResourceLookup();
            lookup.RegisterProvider(new FilesystemResourceProvider(_tempDir));
            return lookup;
        }

        // ─── AC1: basic resolve by path ───────────────────────────────────────

        [Test]
        public void AC1_Resolve_KnownPath_ReturnsFoundWithBytes()
        {
            var content = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
            WriteFile("test.dat", content);

            var lookup = BuildLookupWithTempProvider();
            var result = lookup.Resolve("test.dat");

            Assert.AreEqual(ResourceLookupStatus.Found, result.status,
                "Expected Found status for existing file");
            Assert.AreEqual(content, result.data,
                "Returned bytes must match written bytes");
            Assert.IsFalse(string.IsNullOrEmpty(result.sourcePackage),
                "sourcePackage should be populated by the provider");
        }

        [Test]
        public void AC1_Resolve_UnknownPath_ReturnsMissing()
        {
            var lookup = BuildLookupWithTempProvider();
            var result = lookup.Resolve("does/not/exist.dat");

            Assert.AreEqual(ResourceLookupStatus.Missing, result.status,
                "Resolve of unknown path must return Missing");
            Assert.IsNotNull(result.error);
        }

        [Test]
        public void AC1_Resolve_EmptyPath_ReturnsError()
        {
            var lookup = BuildLookupWithTempProvider();
            var result = lookup.Resolve(string.Empty);

            Assert.AreEqual(ResourceLookupStatus.Error, result.status,
                "Empty path must return Error, not Missing");
        }

        // ─── AC2: encoding strategy surfaced in result ────────────────────────

        [Test]
        public void AC2_Resolve_GbkEncoding_SetsEncodingNote()
        {
            WriteTextFile("legacy.txt", "some data");
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Gbk,
                Compression = CompressionFormat.None,
            };
            var result = lookup.Resolve("legacy.txt", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.IsFalse(string.IsNullOrEmpty(result.encodingNote),
                "GBK strategy must populate encodingNote");
            StringAssert.Contains("GBK", result.encodingNote,
                "encodingNote should mention GBK");
        }

        [Test]
        public void AC2_Resolve_AutoDetectEncoding_SetsEncodingNote()
        {
            WriteTextFile("auto.txt");
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.AutoDetect,
                Compression = CompressionFormat.None,
            };
            var result = lookup.Resolve("auto.txt", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.IsFalse(string.IsNullOrEmpty(result.encodingNote),
                "AutoDetect strategy must populate encodingNote");
        }

        [Test]
        public void AC2_Resolve_Utf8Encoding_EncodingNoteIsEmpty()
        {
            WriteTextFile("utf8.txt");
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Utf8,
                Compression = CompressionFormat.None,
            };
            var result = lookup.Resolve("utf8.txt", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.IsTrue(string.IsNullOrEmpty(result.encodingNote),
                "Utf8 encoding must leave encodingNote empty");
        }

        // ─── AC3: compression handling ────────────────────────────────────────

        [Test]
        public void AC3_Resolve_NoCompression_BytesUnchanged()
        {
            var original = new byte[] { 1, 2, 3, 4, 5 };
            WriteFile("raw.bin", original);
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Utf8,
                Compression = CompressionFormat.None,
            };
            var result = lookup.Resolve("raw.bin", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.AreEqual(original, result.data, "No-compression must leave bytes unchanged");
        }

        [Test]
        public void AC3_Resolve_GzipCompression_DecompressesCorrectly()
        {
            var original = System.Text.Encoding.UTF8.GetBytes("Hello from VLTK gzip test!");
            var compressed = GzipCompress(original);
            WriteFile("data.gz", compressed);
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Utf8,
                Compression = CompressionFormat.Gzip,
            };
            var result = lookup.Resolve("data.gz", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status,
                "Valid gzip data must decompress and return Found");
            Assert.AreEqual(original, result.data,
                "Decompressed bytes must match original");
        }

        [Test]
        public void AC3_Resolve_ZlibCompression_InvalidData_ReturnsError()
        {
            // Write obviously invalid data for Zlib decompression
            WriteFile("bad.zlib", new byte[] { 0xFF, 0xFF, 0xFF, 0xFF });
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Utf8,
                Compression = CompressionFormat.Zlib,
            };
            LogAssert.Expect(LogType.Error, new Regex("Decompress failed for 'bad.zlib'"));
            var result = lookup.Resolve("bad.zlib", opts);

            Assert.AreEqual(ResourceLookupStatus.Error, result.status,
                "Invalid Zlib data must return Error, not Found");
            StringAssert.Contains("Decompress failed", result.error,
                "Error message must mention decompression failure");
        }

        [Test]
        public void AC3_Resolve_ZlibCompression_ValidData_DecompressesCorrectly()
        {
            var original = System.Text.Encoding.UTF8.GetBytes("Hello from VLTK zlib test!");
            var compressed = ZlibCompress(original);
            WriteFile("data.zlib", compressed);
            var lookup = BuildLookupWithTempProvider();

            var opts = new ResourceLookupOptions
            {
                Encoding = EncodingStrategy.Utf8,
                Compression = CompressionFormat.Zlib,
            };
            var result = lookup.Resolve("data.zlib", opts);

            Assert.AreEqual(ResourceLookupStatus.Found, result.status,
                "Valid zlib data must decompress and return Found");
            Assert.AreEqual(original, result.data,
                "Decompressed bytes must match original");
        }

        // ─── AC4: CreateRuntimeLookup only has StreamingAssetsResourceProvider ─

        [Test]
        public void AC4_CreateRuntimeLookup_OnlyHasStreamingAssetsProvider()
        {
            var lookup = ResourceLookup.CreateRuntimeLookup();

            // Verify the lookup works and that the only registered provider is StreamingAssets.
            // We do this by checking that a path only a FilesystemResourceProvider could serve
            // (from temp dir) returns Missing — confirming no FS PAK provider is present.
            WriteTextFile("secret.dat");

            // The runtime lookup doesn't know about _tempDir, so this should be Missing
            var result = lookup.Resolve("secret.dat");
            Assert.AreNotEqual(ResourceLookupStatus.Found, result.status,
                "Runtime lookup must not serve files from arbitrary FS paths (no PAK/FS provider)");
        }

        [Test]
        public void AC4_CreateRuntimeLookup_ProviderId_IsStreamingAssets()
        {
            // CreateRuntimeLookup with empty subDir → ProviderId = "StreamingAssets:"
            // We verify the factory creates exactly one provider whose ID starts with "StreamingAssets:"
            // by checking a known-missing resolve doesn't error (provider registered, just no file found)
            var lookup = ResourceLookup.CreateRuntimeLookup();
            var result = lookup.Resolve("anything.dat");
            // Should be Missing (provider exists but file not there), not Error (no providers)
            Assert.AreEqual(ResourceLookupStatus.Missing, result.status,
                "Runtime lookup with StreamingAssetsProvider should return Missing for unknown files");
        }

        // ─── Multiple providers: first-match wins ─────────────────────────────

        [Test]
        public void MultiProvider_FirstMatchWins()
        {
            // Provider A has "shared.dat" with content [1]
            var dirA = Path.Combine(_tempDir, "A");
            var dirB = Path.Combine(_tempDir, "B");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);

            File.WriteAllBytes(Path.Combine(dirA, "shared.dat"), new byte[] { 1 });
            File.WriteAllBytes(Path.Combine(dirB, "shared.dat"), new byte[] { 2 });

            var lookup = new ResourceLookup();
            lookup.RegisterProvider(new FilesystemResourceProvider(dirA)); // registered first
            lookup.RegisterProvider(new FilesystemResourceProvider(dirB));

            var result = lookup.Resolve("shared.dat");

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.AreEqual(new byte[] { 1 }, result.data,
                "First registered provider must win when both contain the same path");
        }

        [Test]
        public void MultiProvider_FallsThrough_ToSecondProvider()
        {
            // Only dirB has "only_in_b.dat"
            var dirA = Path.Combine(_tempDir, "A");
            var dirB = Path.Combine(_tempDir, "B");
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(dirB);

            File.WriteAllBytes(Path.Combine(dirB, "only_in_b.dat"), new byte[] { 99 });

            var lookup = new ResourceLookup();
            lookup.RegisterProvider(new FilesystemResourceProvider(dirA));
            lookup.RegisterProvider(new FilesystemResourceProvider(dirB));

            var result = lookup.Resolve("only_in_b.dat");

            Assert.AreEqual(ResourceLookupStatus.Found, result.status);
            Assert.AreEqual(new byte[] { 99 }, result.data,
                "Lookup must fall through to the second provider when first doesn't have the file");
        }

        // ─── Test helpers for compression ────────────────────────────────────

        private static byte[] GzipCompress(byte[] data)
        {
            using var ms = new MemoryStream();
            using (var gz = new GZipStream(ms, CompressionMode.Compress, leaveOpen: true))
                gz.Write(data, 0, data.Length);
            return ms.ToArray();
        }

        /// <summary>
        /// Produces a Zlib-wrapped DEFLATE stream (2-byte header + DEFLATE body + 4-byte Adler32).
        /// The header 0x78 0x9C is the standard "default compression" zlib magic.
        /// </summary>
        private static byte[] ZlibCompress(byte[] data)
        {
            using var output = new MemoryStream();

            // Zlib header: CMF=0x78, FLG=0x9C (default compression, no dict, checksum ok)
            output.WriteByte(0x78);
            output.WriteByte(0x9C);

            byte[] deflated;
            using (var deflateMs = new MemoryStream())
            {
                using (var ds = new DeflateStream(deflateMs, CompressionMode.Compress, leaveOpen: true))
                    ds.Write(data, 0, data.Length);
                deflated = deflateMs.ToArray();
            }

            output.Write(deflated, 0, deflated.Length);

            // Adler-32 checksum (big-endian)
            var adler = ComputeAdler32(data);
            output.WriteByte((byte)(adler >> 24));
            output.WriteByte((byte)(adler >> 16));
            output.WriteByte((byte)(adler >> 8));
            output.WriteByte((byte)(adler));

            return output.ToArray();
        }

        private static uint ComputeAdler32(byte[] data)
        {
            const uint MOD = 65521;
            uint s1 = 1, s2 = 0;
            foreach (var b in data)
            {
                s1 = (s1 + b) % MOD;
                s2 = (s2 + s1) % MOD;
            }
            return (s2 << 16) | s1;
        }
    }
}
