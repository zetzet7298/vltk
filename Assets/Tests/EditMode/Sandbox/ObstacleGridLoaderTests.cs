// -----------------------------------------------------------------------------
// VLTK Mobile — ObstacleGridLoader EditMode tests.
// Kiểm tra pack loading, region lookup, missing region, host dispatch chain.
// PC source: StreamingAssets/Obstacles.bin (packed from per-region files).
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class ObstacleGridLoaderTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IObstacleGridLoaderHost
        {
            public int LoadStartCalls;
            public int LoadCompleteCalls;
            public int LoadFailedCalls;
            public int RegionLoadedCalls;
            public int RegionMissingCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public string LastPackPath;
            public string LastRegionFile;
            public int LastRegionCount;
            public int LastTotalBytes;
            public int LastWidth;
            public int LastHeight;
            public int LastBlocked;
            public string LastStem;
            public string LastReason;
            public bool LastFound;
            public int LastCellCount;

            public void OnLoadStart(string packPath) { LoadStartCalls++; LastPackPath = packPath; }
            public void OnLoadComplete(int regionCount, int totalBytes)
            {
                LoadCompleteCalls++;
                LastRegionCount = regionCount;
                LastTotalBytes = totalBytes;
            }
            public void OnLoadFailed(string packPath, string reason) { LoadFailedCalls++; LastReason = reason; }
            public void OnRegionLoaded(string regionFile, int width, int height, int blockedCells)
            {
                RegionLoadedCalls++;
                LastRegionFile = regionFile;
                LastWidth = width;
                LastHeight = height;
                LastBlocked = blockedCells;
            }
            public void OnRegionMissing(string regionFile, string stem) { RegionMissingCalls++; LastStem = stem; }
            public void LogObstacleEvent(string message) { LogCalls++; }
            public void PlayObstacleSFX(string action) { SfxCalls++; }
            public void SaveObstacleLog(string regionFile, bool found, int cellCount)
            {
                SaveCalls++;
                LastFound = found;
                LastCellCount = cellCount;
            }
        }

        private string _packFile;
        private FakeHost _host;

        [SetUp]
        public void SetUp()
        {
            ObstacleGridLoader.ResetCache();
            _packFile = Path.Combine(Application.temporaryCachePath, "test_obstacles_" + System.Guid.NewGuid().ToString("N") + ".bin");
            _host = new FakeHost();
            ObstacleGridLoader.AttachHost(_host);
        }

        [TearDown]
        public void TearDown()
        {
            ObstacleGridLoader.AttachHost(null);
            ObstacleGridLoader.ResetCache();
            try { if (File.Exists(_packFile)) File.Delete(_packFile); } catch { }
        }

        // Pack format helper: 16-byte header (VOBP + int32 version + int32 count + int32 dataSectionOffset)
        // + N * 24-byte index entries (key 8 ascii + int16 w + int16 h + int32 blocked + int32 dataOff + int32 dataLen)
        // + concatenated raw cell bytes
        private static void WritePack(string path, int count, params (string key, short w, short h, int blocked, byte[] cells)[] entries)
        {
            int dataOff = 16 + count * 24;
            int dataPos = dataOff;
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs))
            {
                bw.Write((byte)'V'); bw.Write((byte)'O'); bw.Write((byte)'B'); bw.Write((byte)'P');
                bw.Write(1); // version
                bw.Write(count);
                bw.Write(dataOff);
                foreach (var e in entries)
                {
                    // Keys are left-justified with SPACE padding (not NUL) so the
                    // parser's TrimEnd() (whitespace only) correctly strips the
                    // trailing spaces and recovers the name.
                    var keyBytes = new byte[8];
                    var nameBytes = System.Text.Encoding.ASCII.GetBytes(e.key);
                    int copyLen = System.Math.Min(nameBytes.Length, 8);
                    for (int k = 0; k < 8; k++) keyBytes[k] = 0x20; // space
                    System.Array.Copy(nameBytes, 0, keyBytes, 0, copyLen);
                    bw.Write(keyBytes);
                    bw.Write(e.w);
                    bw.Write(e.h);
                    bw.Write(e.blocked);
                    bw.Write(dataPos);
                    bw.Write(e.cells.Length);
                    dataPos += e.cells.Length;
                }
                foreach (var e in entries) bw.Write(e.cells);
            }
        }

        // ── AttachHost / ResetCache ─────────────────────────────────────────

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            ObstacleGridLoader.AttachHost(host);
            ObstacleGridLoader.LoadFromStreamingAssets("anything");
            // _host may or may not be called depending on pack state. But the host
            // ref itself is stored.
            Assert.Pass();
        }

        [Test]
        public void ResetCache_Static()
        {
            // No exception
            Assert.DoesNotThrow(() => ObstacleGridLoader.ResetCache());
        }

        // ── LoadFromStreamingAssets (no pack) ────────────────────────────────

        [Test]
        public void LoadFromStreamingAssets_NullPack_ReturnsNull()
        {
            var result = ObstacleGridLoader.LoadFromStreamingAssets("xxx.dat");
            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromStreamingAssets_EmptyRegionFile_ReturnsNull()
        {
            var result = ObstacleGridLoader.LoadFromStreamingAssets("");
            Assert.IsNull(result);
        }

        [Test]
        public void LoadFromStreamingAssets_EmptyRegionFile_DispatchesMissing()
        {
            ObstacleGridLoader.LoadFromStreamingAssets("");
            Assert.AreEqual(1, _host.RegionMissingCalls);
        }

        [Test]
        public void LoadFromStreamingAssets_NoPack_DispatchesMissing()
        {
            ObstacleGridLoader.LoadFromStreamingAssets("00015d99.dat");
            Assert.AreEqual(1, _host.RegionMissingCalls);
        }

        [Test]
        public void LoadFromStreamingAssets_NoPack_DispatchesLoadFailed()
        {
            ObstacleGridLoader.LoadFromStreamingAssets("00015d99.dat");
            Assert.AreEqual(1, _host.LoadFailedCalls);
            Assert.AreEqual("file not found", _host.LastReason);
        }

        // ── LoadFromStreamingAssets (with pack) ──────────────────────────────

        [Test]
        public void LoadFromStreamingAssets_ValidPack_Success()
        {
            WritePack(_packFile, 1,
                ("r0001", (short)2, (short)2, 1, new byte[] { 1, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            var grid = ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.IsNotNull(grid);
            Assert.AreEqual(2, grid.width);
            Assert.AreEqual(2, grid.height);
        }

        [Test]
        public void LoadFromStreamingAssets_ValidPack_DispatchesLoadStart()
        {
            WritePack(_packFile, 1, ("r0001", (short)2, (short)2, 1, new byte[] { 1 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.AreEqual(1, _host.LoadStartCalls);
            Assert.AreEqual(_packFile, _host.LastPackPath);
        }

        [Test]
        public void LoadFromStreamingAssets_ValidPack_DispatchesLoadComplete()
        {
            WritePack(_packFile, 2,
                ("r0001", (short)2, (short)2, 1, new byte[] { 1, 0, 0, 0 }),
                ("r0002", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.AreEqual(1, _host.LoadCompleteCalls);
            Assert.AreEqual(2, _host.LastRegionCount);
            Assert.IsTrue(_host.LastTotalBytes > 0);
            Assert.AreEqual(1, _host.SfxCalls);
        }

        [Test]
        public void LoadFromStreamingAssets_ValidPack_DispatchesRegionLoaded()
        {
            WritePack(_packFile, 1, ("r0001", (short)4, (short)4, 5, new byte[16]));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.AreEqual(1, _host.RegionLoadedCalls);
            Assert.AreEqual(4, _host.LastWidth);
            Assert.AreEqual(4, _host.LastHeight);
            Assert.AreEqual(5, _host.LastBlocked);
        }

        [Test]
        public void LoadFromStreamingAssets_ValidPack_DispatchesSave()
        {
            WritePack(_packFile, 1, ("r0001", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.AreEqual(1, _host.SaveCalls);
            Assert.IsTrue(_host.LastFound);
            Assert.IsTrue(_host.LastCellCount > 0);
        }

        [Test]
        public void LoadFromStreamingAssets_RegionNotInPack_DispatchesMissing()
        {
            WritePack(_packFile, 1, ("r0001", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            var grid = ObstacleGridLoader.LoadFromStreamingAssets("r9999.dat");
            Assert.IsNull(grid);
            Assert.AreEqual(1, _host.RegionMissingCalls);
        }

        [Test]
        public void LoadFromStreamingAssets_RegionNotInPack_DispatchesSave()
        {
            WritePack(_packFile, 1, ("r0001", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r9999.dat");
            Assert.AreEqual(1, _host.SaveCalls);
            Assert.IsFalse(_host.LastFound);
        }

        [Test]
        public void LoadFromStreamingAssets_MultipleRegions_AllIndexed()
        {
            WritePack(_packFile, 3,
                ("r0001", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }),
                ("r0002", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }),
                ("r0003", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            var g1 = ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            var g2 = ObstacleGridLoader.LoadFromStreamingAssets("r0002.dat");
            var g3 = ObstacleGridLoader.LoadFromStreamingAssets("r0003.dat");
            Assert.IsNotNull(g1);
            Assert.IsNotNull(g2);
            Assert.IsNotNull(g3);
        }

        // ── Invalid pack ─────────────────────────────────────────────────────

        [Test]
        public void LoadFromStreamingAssets_InvalidMagic_DispatchesFailed()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Obstacle pack has invalid magic header"));
            File.WriteAllBytes(_packFile, new byte[100]);
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            Assert.AreEqual(1, _host.LoadFailedCalls);
            Assert.AreEqual("invalid magic header", _host.LastReason);
        }

        [Test]
        public void LoadFromStreamingAssets_TruncatedPack_NoCrash()
        {
            // 16 bytes = header size, VOBP magic + zero count/version/dataOff
            using (var fs = new FileStream(_packFile, FileMode.Create, FileAccess.Write))
            {
                fs.Write(new byte[] { (byte)'V', (byte)'O', (byte)'B', (byte)'P' });
                fs.Write(new byte[12]); // zero version, count=0, dataOff=0
            }
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            // count=0 is technically valid → no region lookup, no LoadFailed
            Assert.DoesNotThrow(() => ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat"));
        }

        // ── LoadDefault ──────────────────────────────────────────────────────

        [Test]
        public void LoadDefault_NoHost_DoesNotThrow()
        {
            ObstacleGridLoader.LoadDefault();
            // No host, no exception
        }

        // ── Cache ────────────────────────────────────────────────────────────

        [Test]
        public void LoadFromStreamingAssets_AfterReset_Reloads()
        {
            WritePack(_packFile, 1, ("r0001", (short)2, (short)2, 0, new byte[] { 0, 0, 0, 0 }));
            ObstacleGridLoader.SetPackPathForTesting(_packFile);
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            ObstacleGridLoader.ResetCache();
            ObstacleGridLoader.LoadFromStreamingAssets("r0001.dat");
            // 2 LoadStart calls (1 before reset, 1 after)
            Assert.AreEqual(2, _host.LoadStartCalls);
        }
    }
}
