// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho NpcLevelScriptService, NpcDeathScriptService,
// DailyTaskService, BossMissionService.
// Tests phải chạy được kể cả khi StreamingAssets/Reference thiếu data.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class NpcLevelScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            // Phải tạo được service (kể cả khi folder không tồn tại)
            NpcLevelScriptService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NpcLevelScriptService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = NpcLevelScriptService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0, "Count phải >= 0 (0 nếu không có data)");
        }

        [Test]
        public void GetScriptForNpc_ReturnsNullForUnknownNpc()
        {
            var svc = NpcLevelScriptService.LoadFromStreamingAssets();
            // Nếu registry rỗng, GetScriptForNpc trả null
            var entry = svc.GetScriptForNpc(999_999, level: 50);
            Assert.IsNull(entry);
        }
    }

    public class NpcDeathScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            NpcDeathScriptService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NpcDeathScriptService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
        }

        [Test]
        public void GetDeathScript_ReturnsNullForInvalid()
        {
            var svc = NpcDeathScriptService.LoadFromStreamingAssets();
            var entry = svc.GetDeathScript(999_999_999);
            Assert.IsNull(entry, "NPC id không tồn tại phải trả null");
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = NpcDeathScriptService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class DailyTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            DailyTaskService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => DailyTaskService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
        }

        [Test]
        public void CanAccept_RejectsLevelMismatch()
        {
            var reg = new PcDailyTaskRegistry();
            reg.Register(new PcDailyTaskEntry
            {
                taskId = 100,
                taskType = 0,
                targetId = 1,
                targetCount = 5,
                minLevel = 20,
                maxLevel = 50,
                rewardExp = 1000,
                rewardSilver = 500,
                rewardItem = 0,
            });
            var svc = new DailyTaskService(reg);
            // Player cấp 10 < minLevel 20
            Assert.IsFalse(svc.CanAccept(100, 10), "Cấp dưới minLevel phải từ chối");
            // Player cấp 30 trong range
            Assert.IsTrue(svc.CanAccept(100, 30), "Cấp trong range phải chấp nhận");
            // Player cấp 60 > maxLevel 50
            Assert.IsFalse(svc.CanAccept(100, 60), "Cấp trên maxLevel phải từ chối");
            // Task id không tồn tại
            Assert.IsFalse(svc.CanAccept(999_999, 30));
        }

        [Test]
        public void GetTasksForLevel_FiltersCorrectly()
        {
            var reg = new PcDailyTaskRegistry();
            reg.Register(new PcDailyTaskEntry
            {
                taskId = 1, taskType = 0, minLevel = 10, maxLevel = 20,
            });
            reg.Register(new PcDailyTaskEntry
            {
                taskId = 2, taskType = 0, minLevel = 30, maxLevel = 50,
            });
            reg.Register(new PcDailyTaskEntry
            {
                taskId = 3, taskType = 1, minLevel = 1, maxLevel = 100,
            });
            var svc = new DailyTaskService(reg);
            var at15 = svc.GetTasksForLevel(15);
            int count15 = 0;
            foreach (var _ in at15) count15++;
            Assert.AreEqual(1, count15, "Cấp 15 chỉ thấy task 1");
            var at99 = svc.GetTasksForLevel(99);
            int count99 = 0;
            foreach (var _ in at99) count99++;
            Assert.AreEqual(1, count99, "Cấp 99 chỉ thấy task 3");
            var at5 = svc.GetTasksForLevel(5);
            int count5 = 0;
            foreach (var _ in at5) count5++;
            Assert.AreEqual(0, count5, "Cấp 5 không thấy task nào");
        }
    }

    public class BossMissionServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            BossMissionService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BossMissionService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
        }

        [Test]
        public void CanEnter_RejectsLevelMismatch()
        {
            var reg = new PcBossMissionRegistry();
            reg.Register(new PcBossMissionEntry
            {
                missionId = 1,
                mapId = 100,
                bossNpcId = 5000,
                minLevel = 50,
                maxLevel = 80,
                minPartySize = 3,
                rewardId = 0,
                rewardCount = 0,
                resetHour = 0,
            });
            var svc = new BossMissionService(reg);
            Assert.AreEqual(BossEnterResult.LevelTooLow, svc.CanEnter(1, 30, 5));
            Assert.AreEqual(BossEnterResult.LevelTooHigh, svc.CanEnter(1, 90, 5));
            Assert.AreEqual(BossEnterResult.CanEnter, svc.CanEnter(1, 60, 5));
            Assert.AreEqual(BossEnterResult.NotEnoughParty, svc.CanEnter(1, 60, 1));
            Assert.AreEqual(BossEnterResult.NotFound, svc.CanEnter(9999, 60, 5));
        }

        [Test]
        public void GetMissionsForMap_FiltersCorrectly()
        {
            var reg = new PcBossMissionRegistry();
            reg.Register(new PcBossMissionEntry { missionId = 1, mapId = 100, bossNpcId = 1000, minLevel = 0, maxLevel = 0, minPartySize = 0 });
            reg.Register(new PcBossMissionEntry { missionId = 2, mapId = 100, bossNpcId = 1001, minLevel = 0, maxLevel = 0, minPartySize = 0 });
            reg.Register(new PcBossMissionEntry { missionId = 3, mapId = 200, bossNpcId = 1002, minLevel = 0, maxLevel = 0, minPartySize = 0 });
            var svc = new BossMissionService(reg);
            var map100 = svc.GetMissionsForMap(100);
            int count100 = 0;
            foreach (var _ in map100) count100++;
            Assert.AreEqual(2, count100, "Map 100 phải có 2 mission");
            var map200 = svc.GetMissionsForMap(200);
            int count200 = 0;
            foreach (var _ in map200) count200++;
            Assert.AreEqual(1, count200, "Map 200 phải có 1 mission");
            var mapEmpty = svc.GetMissionsForMap(999);
            int countEmpty = 0;
            foreach (var _ in mapEmpty) countEmpty++;
            Assert.AreEqual(0, countEmpty, "Map không tồn tại phải trả rỗng");
        }
    }
}
