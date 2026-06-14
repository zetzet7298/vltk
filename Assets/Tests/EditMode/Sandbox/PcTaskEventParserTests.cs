// -----------------------------------------------------------------------------
// VLTK Mobile — PcTaskEventParser EditMode tests.
// Kiểm tra parse task_event.txt/task_type.txt/task_id.txt, BuildRegistry, host
// dispatch chain.
// PC source: server settings/task/{task_event,task_type,task_id}.txt.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PcTaskEventParserTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPcTaskEventHost
        {
            public int ParseStartCalls;
            public int ParseCompleteCalls;
            public int ParseFailedCalls;
            public int RegistryBuiltCalls;
            public int ShowCalls;
            public int LogCalls;
            public int SfxCalls;
            public int SaveCalls;
            public string LastFileName;
            public int LastEntryCount;
            public long LastDurationMs;
            public string LastReason;
            public int LastEventCount;
            public int LastTypeCount;
            public int LastIdCount;
            public long LastRegistryDurationMs;

            public void OnParseStart(string fileName) { ParseStartCalls++; LastFileName = fileName; }
            public void OnParseComplete(string fileName, int entryCount, long durationMs)
            {
                ParseCompleteCalls++;
                LastEntryCount = entryCount;
                LastDurationMs = durationMs;
            }
            public void OnParseFailed(string fileName, string reason) { ParseFailedCalls++; LastReason = reason; }
            public void OnRegistryBuilt(int eventCount, int typeCount, int idCount, long durationMs)
            {
                RegistryBuiltCalls++;
                LastEventCount = eventCount;
                LastTypeCount = typeCount;
                LastIdCount = idCount;
                LastRegistryDurationMs = durationMs;
            }
            public void ShowTaskLogUI(int totalCount) { ShowCalls++; }
            public void LogTaskEvent(string message) { LogCalls++; }
            public void PlayTaskLogSFX(string action) { SfxCalls++; }
            public void SaveTaskLog(int eventCount, int typeCount, int idCount) { SaveCalls++; }
        }

        private string _tmpDir;
        private FakeHost _host;

        [SetUp]
        public void SetUp()
        {
            _tmpDir = Path.Combine(Application.temporaryCachePath, "test_taskevent_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmpDir);
            _host = new FakeHost();
            PcTaskEventRegistry.AttachHost(_host);
        }

        [TearDown]
        public void TearDown()
        {
            PcTaskEventRegistry.AttachHost(null);
            try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true); } catch { }
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            PcTaskEventRegistry.AttachHost(host);
            PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, host.RegistryBuiltCalls);
        }

        // ── BuildRegistry: no dir ───────────────────────────────────────────

        [Test]
        public void BuildRegistry_NullDir_DispatchesFailed()
        {
            var reg = PcTaskEventRegistry.BuildRegistry(null);
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
            Assert.AreEqual("empty dir", _host.LastReason);
        }

        [Test]
        public void BuildRegistry_EmptyDir_DispatchesFailed()
        {
            var reg = PcTaskEventRegistry.BuildRegistry("");
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
        }

        [Test]
        public void BuildRegistry_NonexistentDir_DispatchesFailed()
        {
            var reg = PcTaskEventRegistry.BuildRegistry("/tmp/nonexistent_dir_" + System.Guid.NewGuid());
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
            Assert.AreEqual("dir not found", _host.LastReason);
        }

        [Test]
        public void BuildRegistry_NonexistentDir_DispatchesRegistryBuilt()
        {
            PcTaskEventRegistry.BuildRegistry("/tmp/nonexistent_dir_" + System.Guid.NewGuid());
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
            Assert.AreEqual(0, _host.LastEventCount);
        }

        // ── BuildRegistry: with files ───────────────────────────────────────

        [Test]
        public void BuildRegistry_EmptyDir_RegistryBuilt()
        {
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
        }

        [Test]
        public void BuildRegistry_WithEventFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"),
                "1\tEvent1\tDescription1\n" +
                "2\tEvent2\tDescription2\n" +
                "3\tEvent3\tDescription3\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(3, reg.EventCount);
        }

        [Test]
        public void BuildRegistry_WithTypeFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"),
                "T1\tcond1\tentity1\taward1\ttalk1\n" +
                "T2\tcond2\tentity2\taward2\ttalk2\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.TypeCount);
        }

        [Test]
        public void BuildRegistry_WithIdFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"),
                "100\tTaskA\t1\tT1\t1\tTaskTextA\n" +
                "200\tTaskB\t2\tT2\t0\tTaskTextB\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.IdCount);
        }

        [Test]
        public void BuildRegistry_AllFiles()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"), "1\tE1\tT1\n2\tE2\tT2\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"), "T1\tc1\te1\ta1\tt1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"), "100\tN1\t1\tT1\t1\tX1\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.EventCount);
            Assert.AreEqual(1, reg.TypeCount);
            Assert.AreEqual(1, reg.IdCount);
            Assert.AreEqual(4, reg.Count);
        }

        [Test]
        public void BuildRegistry_DispatchesRegistryBuilt()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"), "1\tE1\tT1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"), "100\tN1\t1\tT1\t1\tX1\n");
            PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
            Assert.AreEqual(1, _host.LastEventCount);
            Assert.AreEqual(0, _host.LastTypeCount);
            Assert.AreEqual(1, _host.LastIdCount);
            Assert.AreEqual(1, _host.ShowCalls);
            Assert.AreEqual(1, _host.SaveCalls);
        }

        [Test]
        public void BuildRegistry_DispatchesParseStart_3x()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"), "1\tE1\tT1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"), "T1\tc1\te1\ta1\tt1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"), "100\tN1\t1\tT1\t1\tX1\n");
            PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(3, _host.ParseStartCalls);
        }

        [Test]
        public void BuildRegistry_DispatchesParseComplete_3x()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"), "1\tE1\tT1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"), "T1\tc1\te1\ta1\tt1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"), "100\tN1\t1\tT1\t1\tX1\n");
            PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(3, _host.ParseCompleteCalls);
        }

        // ── ParseEvents details ─────────────────────────────────────────────

        [Test]
        public void ParseEvents_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"),
                "EventID\tEventName\tEventText\n" +
                "1\tRealEvent\tRealDescription\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.EventCount);
        }

        [Test]
        public void ParseEvents_InvalidId_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"),
                "abc\tInvalidEvent\tInvalidDescription\n" +
                "1\tRealEvent\tRealDescription\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.EventCount);
        }

        [Test]
        public void ParseEvents_FewerColumns_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"),
                "1\n" +  // just 1 column, skipped
                "1\tE1\tT1\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.EventCount);
        }

        [Test]
        public void ParseEvents_EmptyLines_Handled()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"),
                "\n\n1\tE1\tT1\n\n2\tE2\tT2\n\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.EventCount);
        }

        [Test]
        public void ParseEvents_DispatchesFailed_WhenNoFile()
        {
            PcTaskEventRegistry.BuildRegistry(_tmpDir);
            // No files → 3 ParseFailed (one for each)
            Assert.AreEqual(3, _host.ParseFailedCalls);
        }

        // ── ParseTypes details ──────────────────────────────────────────────

        [Test]
        public void ParseTypes_FewerColumns_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"),
                "T1\tcond1\n" +  // only 2 cols
                "T2\tc2\te2\ta2\tt2\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.TypeCount);
        }

        [Test]
        public void ParseTypes_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_type.txt"),
                "TaskType\tConditionFile\tEntityFile\tAwardFile\tTalkFile\n" +
                "T1\tc1\te1\ta1\tt1\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.TypeCount);
        }

        // ── ParseIds details ───────────────────────────────────────────────

        [Test]
        public void ParseIds_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"),
                "TaskID\tTaskName\tEventID\tTaskType\tCanCancel\tTaskText\n" +
                "100\tN1\t1\tT1\t1\tX1\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.IdCount);
        }

        [Test]
        public void ParseIds_InvalidId_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"),
                "xyz\tN0\t1\tT1\t1\tX0\n" +
                "100\tN1\t1\tT1\t1\tX1\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.IdCount);
        }

        [Test]
        public void ParseIds_FewerColumns_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"),
                "100\n" +  // just 1 col
                "200\tN2\t1\tT1\t1\tX2\n");
            var reg = PcTaskEventRegistry.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.IdCount);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void Parser_WithoutHost_DoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "task_event.txt"), "1\tE1\tT1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "task_id.txt"), "100\tN1\t1\tT1\t1\tX1\n");
            PcTaskEventRegistry.AttachHost(null);
            Assert.DoesNotThrow(() => PcTaskEventRegistry.BuildRegistry(_tmpDir));
        }

        // ── Count properties ────────────────────────────────────────────────

        [Test]
        public void EmptyRegistry_AllZero()
        {
            var reg = new PcTaskEventRegistry();
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(0, reg.EventCount);
            Assert.AreEqual(0, reg.TypeCount);
            Assert.AreEqual(0, reg.IdCount);
        }
    }
}
