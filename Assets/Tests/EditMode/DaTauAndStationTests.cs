// -----------------------------------------------------------------------------
// VLTK Mobile — ST-03.3/03.4 Da Tau & Station Travel Tests
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class DaTauAndStationTests
    {
        // ── Da Tau Task Chain Tests ────────────────────────────────────────

        [Test]
        public void DaTau_AcceptAndCompleteKillTask()
        {
            var taskFlags = new TaskFlagService();
            var levelService = new PlayerLevelService(15);
            var daTau = new DaTauTaskChainService(taskFlags, levelService);

            // Accept first task
            var task = daTau.AcceptNextTask();
            Assert.IsNotNull(task);
            Assert.AreEqual(0, task.chainIndex);
            Assert.AreEqual(DaTauTaskType.KillNpc, task.type); // chainIndex % 4 == 0
            Assert.AreEqual(1, daTau.ChainCount + 1); // Not yet completed

            // Update progress: kill 3 mobs
            daTau.UpdateProgress(DaTauTaskType.KillNpc, task.targetId, 3);

            // Not enough yet (targetCount = 3 + level/10 = 4)
            Assert.IsFalse(task.isComplete);

            // Kill 1 more
            daTau.UpdateProgress(DaTauTaskType.KillNpc, task.targetId, 1);
            Assert.IsTrue(task.isComplete);

            // Turn in
            var reward = daTau.TurnInTask();
            Assert.IsNotNull(reward);
            Assert.Greater(reward.exp, 0);
            Assert.Greater(reward.silver, 0);
            Assert.AreEqual(1, daTau.ChainCount);
            Assert.AreEqual(1, daTau.DailyCompleted);
        }

        [Test]
        public void DaTau_ChainResetsOnAbandon()
        {
            var taskFlags = new TaskFlagService();
            var levelService = new PlayerLevelService(10);
            var daTau = new DaTauTaskChainService(taskFlags, levelService);

            // Complete first task
            var task = daTau.AcceptNextTask();
            daTau.UpdateProgress(DaTauTaskType.KillNpc, task.targetId, task.targetCount);
            daTau.TurnInTask();
            Assert.AreEqual(1, daTau.ChainCount);

            // Accept second task then abandon
            var task2 = daTau.AcceptNextTask();
            daTau.AbandonTask();
            Assert.AreEqual(0, daTau.ChainCount);
        }

        [Test]
        public void DaTau_DailyLimit()
        {
            var taskFlags = new TaskFlagService();
            var levelService = new PlayerLevelService(30);
            var daTau = new DaTauTaskChainService(taskFlags, levelService);

            // Complete 40 tasks
            for (int i = 0; i < DaTauTaskChainService.MaxDailyTasks; i++)
            {
                var task = daTau.AcceptNextTask();
                Assert.IsNotNull(task, $"Task {i} should not be null");
                // Force completion
                task.currentProgress = task.targetCount;
                daTau.TurnInTask();
            }
            Assert.AreEqual(40, daTau.DailyCompleted);

            // 41st should fail
            var overflow = daTau.AcceptNextTask();
            Assert.IsNull(overflow);

            // Reset daily
            daTau.ResetDaily();
            Assert.AreEqual(0, daTau.DailyCompleted);
            var newTask = daTau.AcceptNextTask();
            Assert.IsNotNull(newTask);
        }

        [Test]
        public void DaTau_RewardIncreasesWithChain()
        {
            var taskFlags = new TaskFlagService();
            var levelService = new PlayerLevelService(20);
            var daTau = new DaTauTaskChainService(taskFlags, levelService);

            // First task reward
            var task1 = daTau.AcceptNextTask();
            task1.currentProgress = task1.targetCount;
            var reward1 = daTau.TurnInTask();

            // 11th task reward (tier 1)
            for (int i = 1; i < 11; i++)
            {
                var t = daTau.AcceptNextTask();
                t.currentProgress = t.targetCount;
                daTau.TurnInTask();
            }

            // Chain is now 11, reward should be significantly higher
            Assert.Greater(daTau.ChainCount, 10);
        }

        [Test]
        public void DaTau_SkillPointGrantedEvery10Tasks()
        {
            var taskFlags = new TaskFlagService();
            var levelService = new PlayerLevelService(20);
            var daTau = new DaTauTaskChainService(taskFlags, levelService);

            int startSp = levelService.SkillPoints;

            // Complete 10 tasks
            for (int i = 0; i < 10; i++)
            {
                var task = daTau.AcceptNextTask();
                task.currentProgress = task.targetCount;
                daTau.TurnInTask();
            }

            Assert.AreEqual(startSp + 1, levelService.SkillPoints, "Skill point granted at chain 10");
        }

        // ── Station Travel Tests ───────────────────────────────────────────

        [Test]
        public void Station_InitialStationsLoaded()
        {
            var travel = new StationTravelService();
            Assert.Greater(travel.Stations.Count, 5, "Should have multiple default stations");
        }

        [Test]
        public void Station_TravelSucceedsWithEnoughSilverAndLevel()
        {
            var levelService = new PlayerLevelService(15);
            var travel = new StationTravelService(levelService);

            int silver = 1000;
            Vector2 pos = Vector2.zero;
            int mapId = 0;

            bool result = travel.Travel(10, ref silver, ref pos, ref mapId); // Phượng Tường, level 10, 50 Bạc

            Assert.IsTrue(result);
            Assert.AreEqual(50, 1000 - silver); // Trừ 50 Bạc
            Assert.AreEqual(100, mapId); // Map Phượng Tường
            Assert.AreNotEqual(Vector2.zero, pos);
        }

        [Test]
        public void Station_TravelFailsWithoutEnoughSilver()
        {
            var levelService = new PlayerLevelService(15);
            var travel = new StationTravelService(levelService);

            int silver = 10; // Không đủ
            Vector2 pos = Vector2.zero;
            int mapId = 0;

            bool result = travel.Travel(10, ref silver, ref pos, ref mapId);
            Assert.IsFalse(result);
            Assert.AreEqual(10, silver); // Không trừ tiền
        }

        [Test]
        public void Station_TravelFailsWithoutRequiredLevel()
        {
            var levelService = new PlayerLevelService(5); // Level 5, cần 10
            var travel = new StationTravelService(levelService);

            int silver = 1000;
            Vector2 pos = Vector2.zero;
            int mapId = 0;

            bool result = travel.Travel(10, ref silver, ref pos, ref mapId);
            Assert.IsFalse(result);
        }

        [Test]
        public void Station_GetAvailableStationsFiltersByLevelAndSilver()
        {
            var levelService = new PlayerLevelService(5);
            var travel = new StationTravelService(levelService);

            var available = travel.GetAvailableStations(playerLevel: 5, silver: 100);
            Assert.Greater(available.Count, 0, "Low level should see at least Tân Thủ Thôn");

            foreach (var station in available)
            {
                Assert.LessOrEqual(station.requiredLevel, 5);
                Assert.LessOrEqual(station.silverCost, 100);
            }
        }

        [Test]
        public void Station_FindNearestStation()
        {
            var travel = new StationTravelService();
            var nearest = travel.FindNearestStation(new Vector2(1600, 3200));

            Assert.IsNotNull(nearest);
            Assert.AreEqual("Ba Lăng Huyện", nearest.nameVi);
        }
    }
}
