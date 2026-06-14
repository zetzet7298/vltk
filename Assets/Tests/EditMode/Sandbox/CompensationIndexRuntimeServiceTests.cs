// -----------------------------------------------------------------------------
// VLTK Mobile — CompensationIndexRuntimeService EditMode tests.
// Kiểm tra index loading, lookup theo filename/rel_path, prefix counting,
// host dispatch chain.
// PC source: CompensationIndex.json + vng_event/* + activitysys/config/* lua.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class CompensationIndexRuntimeServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : ICompensationHost
        {
            public int LoadStartCalls;
            public int LoadCompleteCalls;
            public int LoadFailedCalls;
            public int QueryCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public string LastIndexPath;
            public int LastEntryCount;
            public int LastFilenameCount;
            public int LastRelPathCount;
            public string LastReason;
            public string LastQueryType;
            public string LastQueryKey;
            public bool LastFound;
            public int LastMatchCount;

            public void OnLoadStart(string indexPath) { LoadStartCalls++; LastIndexPath = indexPath; }
            public void OnLoadComplete(int entryCount, int filenameCount, int relPathCount)
            {
                LoadCompleteCalls++;
                LastEntryCount = entryCount;
                LastFilenameCount = filenameCount;
                LastRelPathCount = relPathCount;
            }
            public void OnLoadFailed(string indexPath, string reason) { LoadFailedCalls++; LastReason = reason; }
            public void OnQuery(string queryType, string queryKey, bool found, int matchCount)
            {
                QueryCalls++;
                LastQueryType = queryType;
                LastQueryKey = queryKey;
                LastFound = found;
                LastMatchCount = matchCount;
            }
            public void ShowCompensationList(int count, int filteredCount) { ShowCalls++; }
            public void LogCompensationEvent(string message) { LogCalls++; }
            public void PlayCompensationSFX(string action) { SfxCalls++; }
            public void SaveCompensationLog(string queryType, string queryKey, int resultCount) { SaveCalls++; }
        }

        private const string SampleJson = "[" +
            "{\"filename\":\"main.lua\",\"rel_path\":\"vng_event/denbu_baotri_5server/main.lua\",\"category\":\"event\"}," +
            "{\"filename\":\"head.lua\",\"rel_path\":\"vng_event/denbu_baotri_5server/head.lua\",\"category\":\"event\"}," +
            "{\"filename\":\"main.lua\",\"rel_path\":\"vng_event/denbu_congthanh/main.lua\",\"category\":\"event\"}," +
            "{\"filename\":\"config.lua\",\"rel_path\":\"activitysys/config/37/config.lua\",\"category\":\"activity\"}" +
            "]";

        // ── Ctor / Count ────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.AreEqual(0, svc.Count);
            Assert.IsFalse(svc.IsLoaded);
        }

        [Test]
        public void Constructor_WithHost()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService();
            svc.AttachHost(host);
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(1, host.LoadStartCalls);
        }

        // ── LoadFromJson ────────────────────────────────────────────────────

        [Test]
        public void LoadFromJson_Success()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsTrue(svc.IsLoaded);
            Assert.AreEqual(4, svc.Count);
        }

        [Test]
        public void LoadFromJson_Empty_NoLoad()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson("");
            Assert.IsFalse(svc.IsLoaded);
            Assert.AreEqual(0, svc.Count);
        }

        [Test]
        public void LoadFromJson_Null_NoLoad()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(null);
            Assert.IsFalse(svc.IsLoaded);
        }

        [Test]
        public void LoadFromJson_EmptyArray_NoLoad()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson("[]");
            Assert.IsFalse(svc.IsLoaded);
        }

        [Test]
        public void LoadFromJson_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(1, host.LoadStartCalls);
            Assert.AreEqual(1, host.LoadCompleteCalls);
            Assert.AreEqual(4, host.LastEntryCount);
            Assert.AreEqual(1, host.ShowCalls);
            Assert.AreEqual(1, host.SfxCalls);
        }

        [Test]
        public void LoadFromJson_Fail_DispatchesFailed()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromJson("");
            Assert.AreEqual(1, host.LoadFailedCalls);
            Assert.AreEqual("empty json", host.LastReason);
        }

        // ── AllEntries ──────────────────────────────────────────────────────

        [Test]
        public void AllEntries_Empty()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.AreEqual(0, svc.AllEntries.Count);
        }

        [Test]
        public void AllEntries_AfterLoad()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(4, svc.AllEntries.Count);
        }

        // ── GetByFilename ───────────────────────────────────────────────────

        [Test]
        public void GetByFilename_Exists()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsNotNull(svc.GetByFilename("config.lua"));
        }

        [Test]
        public void GetByFilename_NotFound_ReturnsNull()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsNull(svc.GetByFilename("nonexistent.lua"));
        }

        [Test]
        public void GetByFilename_Empty_ReturnsNull()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.IsNull(svc.GetByFilename(""));
        }

        [Test]
        public void GetByFilename_CaseInsensitive()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsNotNull(svc.GetByFilename("MAIN.LUA"));
        }

        [Test]
        public void GetByFilename_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromJson(SampleJson);
            host.QueryCalls = 0;
            host.SaveCalls = 0;
            svc.GetByFilename("config.lua");
            Assert.AreEqual(1, host.QueryCalls);
            Assert.AreEqual("filename", host.LastQueryType);
            Assert.IsTrue(host.LastFound);
            Assert.AreEqual(1, host.LastMatchCount);
            Assert.AreEqual(1, host.SaveCalls);
        }

        // ── GetByRelPath ────────────────────────────────────────────────────

        [Test]
        public void GetByRelPath_Exists()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsNotNull(svc.GetByRelPath("vng_event/denbu_baotri_5server/main.lua"));
        }

        [Test]
        public void GetByRelPath_NotFound_ReturnsNull()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.IsNull(svc.GetByRelPath("vng_event/missing/file.lua"));
        }

        [Test]
        public void GetByRelPath_Empty_ReturnsNull()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.IsNull(svc.GetByRelPath(""));
        }

        [Test]
        public void GetByRelPath_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromJson(SampleJson);
            host.QueryCalls = 0;
            svc.GetByRelPath("vng_event/denbu_baotri_5server/main.lua");
            Assert.AreEqual(1, host.QueryCalls);
            Assert.AreEqual("relpath", host.LastQueryType);
        }

        // ── GetAllByFilename ────────────────────────────────────────────────

        [Test]
        public void GetAllByFilename_Multiple()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            var all = svc.GetAllByFilename("main.lua");
            Assert.AreEqual(2, all.Count);
        }

        [Test]
        public void GetAllByFilename_Single()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            var all = svc.GetAllByFilename("head.lua");
            Assert.AreEqual(1, all.Count);
        }

        [Test]
        public void GetAllByFilename_NotFound_Empty()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(0, svc.GetAllByFilename("xxx.lua").Count);
        }

        [Test]
        public void GetAllByFilename_Empty_Empty()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.AreEqual(0, svc.GetAllByFilename("").Count);
        }

        [Test]
        public void GetAllByFilename_DispatchesHost()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromJson(SampleJson);
            host.QueryCalls = 0;
            svc.GetAllByFilename("main.lua");
            Assert.AreEqual(1, host.QueryCalls);
            Assert.AreEqual(2, host.LastMatchCount);
        }

        // ── CountByDirectoryPrefix ──────────────────────────────────────────

        [Test]
        public void CountByDirectoryPrefix()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(3, svc.CountByDirectoryPrefix("vng_event/"));
            Assert.AreEqual(1, svc.CountByDirectoryPrefix("activitysys/"));
        }

        [Test]
        public void CountByDirectoryPrefix_NotFound_Zero()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(0, svc.CountByDirectoryPrefix("missing/"));
        }

        [Test]
        public void CountByDirectoryPrefix_Empty_Zero()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.AreEqual(0, svc.CountByDirectoryPrefix(""));
        }

        [Test]
        public void CountByDirectoryPrefix_CaseInsensitive()
        {
            var svc = new CompensationIndexRuntimeService();
            svc.LoadFromJson(SampleJson);
            Assert.AreEqual(3, svc.CountByDirectoryPrefix("VNG_EVENT/"));
        }

        // ── LoadFromPath ────────────────────────────────────────────────────

        [Test]
        public void LoadFromPath_Empty_NoLoad()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromPath("");
            Assert.IsFalse(svc.IsLoaded);
            Assert.AreEqual(1, host.LoadFailedCalls);
        }

        [Test]
        public void LoadFromPath_Nonexistent_NoLoad()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromPath("/tmp/nonexistent_path_12345.json");
            Assert.IsFalse(svc.IsLoaded);
            Assert.AreEqual(1, host.LoadFailedCalls);
        }

        [Test]
        public void LoadFromPath_Nonexistent_DispatchesFailed()
        {
            var host = new FakeHost();
            var svc = new CompensationIndexRuntimeService(host);
            svc.LoadFromPath("/tmp/nonexistent_path.json");
            Assert.AreEqual("file not found", host.LastReason);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void CompensationIndex_WithoutHost_DoesNotThrow()
        {
            var svc = new CompensationIndexRuntimeService();
            Assert.DoesNotThrow(() => svc.LoadFromJson(SampleJson));
            Assert.DoesNotThrow(() => svc.GetByFilename("config.lua"));
            Assert.DoesNotThrow(() => svc.GetByRelPath("vng_event/denbu_baotri_5server/main.lua"));
            Assert.DoesNotThrow(() => svc.GetAllByFilename("main.lua"));
            Assert.DoesNotThrow(() => svc.CountByDirectoryPrefix("vng_event/"));
        }
    }
}
