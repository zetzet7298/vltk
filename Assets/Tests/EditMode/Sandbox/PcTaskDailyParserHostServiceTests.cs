// -----------------------------------------------------------------------------
// VLTK Mobile — PcTaskDailyParser EditMode tests.
// Kiểm tra parse nhiệm vụ hàng ngày: gather/kill/talk/position files,
// BuildRegistry, host dispatch chain.
// PC source: server settings/task/dailytask/{gather,killmonster,talk,gather_pos,talk_pos}.txt.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PcTaskDailyParserHostServiceTests
    {
        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPcTaskDailyHost
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
            public int LastGatherCount;
            public int LastKillCount;
            public int LastTalkCount;
            public int LastPositionCount;

            public void OnParseStart(string fileName) { ParseStartCalls++; LastFileName = fileName; }
            public void OnParseComplete(string fileName, int entryCount, long durationMs)
            {
                ParseCompleteCalls++;
                LastEntryCount = entryCount;
                LastDurationMs = durationMs;
            }
            public void OnParseFailed(string fileName, string reason) { ParseFailedCalls++; LastReason = reason; }
            public void OnRegistryBuilt(int gatherCount, int killCount, int talkCount, int positionCount, long durationMs)
            {
                RegistryBuiltCalls++;
                LastGatherCount = gatherCount;
                LastKillCount = killCount;
                LastTalkCount = talkCount;
                LastPositionCount = positionCount;
            }
            public void ShowDailyQuestUI(int totalCount) { ShowCalls++; }
            public void LogDailyQuestEvent(string message) { LogCalls++; }
            public void PlayDailyQuestSFX(string action) { SfxCalls++; }
            public void SaveDailyQuestLog(int gatherCount, int killCount, int talkCount, int positionCount) { SaveCalls++; }
        }

        private string _tmpDir;
        private FakeHost _host;

        [SetUp]
        public void SetUp()
        {
            _tmpDir = Path.Combine(Application.temporaryCachePath, "test_dailytask_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tmpDir);
            _host = new FakeHost();
            PcTaskDailyParser.AttachHost(_host);
        }

        [TearDown]
        public void TearDown()
        {
            PcTaskDailyParser.AttachHost(null);
            try { if (Directory.Exists(_tmpDir)) Directory.Delete(_tmpDir, true); } catch { }
        }

        // ── AttachHost ──────────────────────────────────────────────────────

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            PcTaskDailyParser.AttachHost(host);
            PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, host.RegistryBuiltCalls);
        }

        // ── BuildRegistry: no dir ───────────────────────────────────────────

        [Test]
        public void BuildRegistry_NullDir_DispatchesFailed()
        {
            var reg = PcTaskDailyParser.BuildRegistry(null);
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
        }

        [Test]
        public void BuildRegistry_EmptyDir_DispatchesFailed()
        {
            var reg = PcTaskDailyParser.BuildRegistry("");
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void BuildRegistry_NonexistentDir_DispatchesFailed()
        {
            var reg = PcTaskDailyParser.BuildRegistry("/tmp/nonexistent_dailytask_" + System.Guid.NewGuid());
            Assert.AreEqual(0, reg.Count);
            Assert.AreEqual(1, _host.ParseFailedCalls);
            Assert.AreEqual("dir not found", _host.LastReason);
        }

        [Test]
        public void BuildRegistry_EmptyDir_DispatchesRegistryBuilt()
        {
            PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
        }

        // ── Parse files ─────────────────────────────────────────────────────

        [Test]
        public void BuildRegistry_WithGatherFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"),
                "1\tTaskGather1\t100\tMap1\tItemA\t1\t2\t3\t10\n" +
                "2\tTaskGather2\t101\tMap2\tItemB\t2\t3\t4\t20\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.GetByType("gather").Count);
        }

        [Test]
        public void BuildRegistry_WithKillFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "killmonster.txt"),
                "1\tTaskKill1\t200\tMap3\tMobX\t5\n" +
                "2\tTaskKill2\t201\tMap4\tMobY\t10\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.GetByType("kill").Count);
        }

        [Test]
        public void BuildRegistry_WithTalkFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "talk.txt"),
                "1\tTaskTalk1\tMap5\tNpcA\t1\n" +
                "2\tTaskTalk2\tMap6\tNpcB\t0\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.GetByType("talk").Count);
        }

        [Test]
        public void BuildRegistry_WithPositionFile()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"),
                "300\t100\t200\t5\tPosA\tscript1\t1\n" +
                "301\t150\t250\t6\tPosB\tscript2\t2\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.GetByType("position").Count);
        }

        [Test]
        public void BuildRegistry_AllFiles()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"), "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            File.WriteAllText(Path.Combine(_tmpDir, "killmonster.txt"), "2\tB\t101\tM2\tY\t5\n");
            File.WriteAllText(Path.Combine(_tmpDir, "talk.txt"), "3\tC\tM3\tZ\t1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"), "102\t50\t60\t3\tP\tlua\t4\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(4, reg.Count);
        }

        [Test]
        public void BuildRegistry_DispatchesRegistryBuilt()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"), "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            File.WriteAllText(Path.Combine(_tmpDir, "killmonster.txt"), "2\tB\t101\tM2\tY\t5\n");
            File.WriteAllText(Path.Combine(_tmpDir, "talk.txt"), "3\tC\tM3\tZ\t1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"), "102\t50\t60\t3\tP\tlua\t4\n");
            PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, _host.RegistryBuiltCalls);
            Assert.AreEqual(1, _host.LastGatherCount);
            Assert.AreEqual(1, _host.LastKillCount);
            Assert.AreEqual(1, _host.LastTalkCount);
            Assert.AreEqual(1, _host.LastPositionCount);
            Assert.AreEqual(1, _host.ShowCalls);
            Assert.AreEqual(1, _host.SaveCalls);
        }

        [Test]
        public void BuildRegistry_DispatchesParseStart_6x()
        {
            // gather + kill + talk + talk_old + gather_pos + talk_pos = 6
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"), "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(6, _host.ParseStartCalls);
        }

        [Test]
        public void BuildRegistry_DispatchesParseFailed_6x_WhenNoFiles()
        {
            PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(6, _host.ParseFailedCalls);
        }

        // ── ParseGather details ─────────────────────────────────────────────

        [Test]
        public void ParseGather_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"),
                "TaskId\tTaskName\tMapId\tMapName\tGatherName\tG\tD\tP\tGatherCount\n" +
                "1\tRealGather\t100\tMap1\tItem\t1\t2\t3\t10\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("gather").Count);
        }

        [Test]
        public void ParseGather_InvalidId_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"),
                "abc\tInvalid\t100\tMap1\tItem\t1\t2\t3\t10\n" +
                "1\tReal\t100\tMap1\tItem\t1\t2\t3\t10\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("gather").Count);
        }

        [Test]
        public void ParseGather_FewerColumns_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"),
                "1\tOnly2Cols\n" +
                "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("gather").Count);
        }

        // ── ParseKill details ───────────────────────────────────────────────

        [Test]
        public void ParseKill_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "killmonster.txt"),
                "TaskId\tTaskName\tMapId\tMapName\tMonsterName\tKillCount\n" +
                "1\tRealKill\t100\tMap1\tMobX\t5\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("kill").Count);
        }

        // ── ParseTalk details ───────────────────────────────────────────────

        [Test]
        public void ParseTalk_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "talk.txt"),
                "TaskId\tTaskName\tMapName\tNpcName\tGender\n" +
                "1\tRealTalk\tMap1\tNpcA\t1\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("talk").Count);
        }

        [Test]
        public void ParseTalk_BothFiles_Combined()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "talk.txt"), "1\tA\tM1\tNpcA\t1\n");
            File.WriteAllText(Path.Combine(_tmpDir, "talk_old.txt"), "2\tB\tM2\tNpcB\t0\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(2, reg.GetByType("talk").Count);
        }

        // ── ParsePosition details ───────────────────────────────────────────

        [Test]
        public void ParsePosition_HeaderLine_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"),
                "MapId\tX\tY\tNpcRes\tNpcName\tNpcScript\tTaskId\n" +
                "100\t50\t60\t3\tP\tlua\t1\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("position").Count);
        }

        [Test]
        public void ParsePosition_InvalidTaskId_Skipped()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"),
                "100\t50\t60\t3\tP\tlua\tabc\n" +
                "100\t50\t60\t3\tP\tlua\t1\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            Assert.AreEqual(1, reg.GetByType("position").Count);
        }

        [Test]
        public void ParsePosition_ExistingTask_Skipped()
        {
            // Same taskId appears in both gather.txt and gather_pos.txt — should not double-add
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"), "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            File.WriteAllText(Path.Combine(_tmpDir, "gather_pos.txt"), "100\t50\t60\t3\tP\tlua\t1\n");
            var reg = PcTaskDailyParser.BuildRegistry(_tmpDir);
            // Should have 1 gather (the original) and not double-add position
            Assert.AreEqual(1, reg.GetByType("gather").Count);
        }

        // ── Registry Get/GetByType ─────────────────────────────────────────

        [Test]
        public void Registry_Get_NotFound_Null()
        {
            var reg = new PcTaskDailyRegistry();
            Assert.IsNull(reg.Get(999));
        }

        [Test]
        public void Registry_GetByType_NotFound_Empty()
        {
            var reg = new PcTaskDailyRegistry();
            Assert.AreEqual(0, reg.GetByType("nonexistent").Count);
        }

        [Test]
        public void Registry_Add_Null_Skipped()
        {
            var reg = new PcTaskDailyRegistry();
            reg.Add(null);
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void Registry_Add_InvalidId_Skipped()
        {
            var reg = new PcTaskDailyRegistry();
            reg.Add(new PcTaskDailyEntry { TaskId = 0 });
            Assert.AreEqual(0, reg.Count);
        }

        [Test]
        public void Registry_All_Empty()
        {
            var reg = new PcTaskDailyRegistry();
            int n = 0;
            foreach (var _ in reg.All) n++;
            Assert.AreEqual(0, n);
        }

        // ── No-host ─────────────────────────────────────────────────────────

        [Test]
        public void Parser_WithoutHost_DoesNotThrow()
        {
            File.WriteAllText(Path.Combine(_tmpDir, "gather.txt"), "1\tA\t100\tM1\tX\t1\t1\t1\t10\n");
            PcTaskDailyParser.AttachHost(null);
            Assert.DoesNotThrow(() => PcTaskDailyParser.BuildRegistry(_tmpDir));
        }
    }
}
