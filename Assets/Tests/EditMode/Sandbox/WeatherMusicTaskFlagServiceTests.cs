// -----------------------------------------------------------------------------
// VLTK Mobile — Weather / Music / TaskFlag Service Tests
// Coverage: WeatherService, MusicService, TaskFlagService runtime + serialization.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WeatherServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WeatherService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetWeatherForMap_ReturnsNullForInvalid()
        {
            var svc = WeatherService.LoadFromStreamingAssets();
            Assert.IsNull(svc.GetWeatherForMap(-1, 12));
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = WeatherService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MusicServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MusicService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetTrack_ReturnsNullForInvalid()
        {
            var svc = MusicService.LoadFromStreamingAssets();
            Assert.IsNull(svc.GetTrack(-1));
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = MusicService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaskFlagServiceTests
    {
        [Test]
        public void SetFlag_StoresStatus()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(100, 2, progress: 5, targetCount: 10, desc: "Tiêu diệt quái");
            Assert.AreEqual(2, svc.GetFlag(100));
            Assert.IsTrue(svc.HasFlag(100));
        }

        [Test]
        public void GetFlag_ReturnsCorrectValue()
        {
            var svc = new TaskFlagService();
            Assert.AreEqual(0, svc.GetFlag(999)); // Unknown
            svc.SetFlag(1, 1);
            Assert.AreEqual(1, svc.GetFlag(1));
        }

        [Test]
        public void IsTaskFinished_TrueWhenStatus3()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 3);
            Assert.IsTrue(svc.IsTaskFinished(1));
            Assert.IsFalse(svc.IsTaskComplete(1));
        }

        [Test]
        public void CanAccept_RejectsLowLevel()
        {
            var svc = new TaskFlagService();
            Assert.IsFalse(svc.CanAcceptTask(1, playerLevel: 5, reqLevel: 10));
            Assert.IsTrue(svc.CanAcceptTask(1, playerLevel: 15, reqLevel: 10));
        }

        [Test]
        public void SerializeToSave_ProducesJson()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(1, 1, desc: "Nhiệm vụ test");
            var json = svc.SerializeToSave();
            Assert.IsNotEmpty(json);
            Assert.IsTrue(json.Contains("\"taskId\":1"));
        }

        [Test]
        public void DeserializeFromSave_RestoresState()
        {
            var svc = new TaskFlagService();
            svc.SetFlag(42, 2, progress: 3, targetCount: 5, desc: "Hồi sinh");
            var json = svc.SerializeToSave();

            var svc2 = new TaskFlagService();
            svc2.DeserializeFromSave(json);
            Assert.AreEqual(2, svc2.GetFlag(42));
            var data = svc2.GetTaskData(42);
            Assert.IsNotNull(data);
            Assert.AreEqual(3, data.progress);
        }
    }
}
