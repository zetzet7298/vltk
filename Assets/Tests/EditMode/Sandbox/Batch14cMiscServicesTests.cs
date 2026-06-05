// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 14c: Object/ItemValue/Music/Weather/Partner/Native/Timer
// Vietnamese: Kiểm thử dịch vụ vật thể, giá trị vật phẩm, âm nhạc, thời tiết, đồng hành, quê hương, định thời
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ObjDataServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ObjDataService.LoadFromStreamingAssets());
            var svc = ObjDataService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class ObjectSettingServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ObjectSettingService.LoadFromStreamingAssets());
            var svc = ObjectSettingService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MusicConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MusicConfigService.LoadFromStreamingAssets());
            var svc = MusicConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class WeatherConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WeatherConfigService.LoadFromStreamingAssets());
            var svc = WeatherConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class ItemValueServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ItemValueService.LoadFromStreamingAssets());
            var svc = ItemValueService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class PartnerEventServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PartnerEventService.LoadFromStreamingAssets());
            var svc = PartnerEventService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class PartnerBagServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PartnerBagService.LoadFromStreamingAssets());
            var svc = PartnerBagService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class PartnerSettingServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PartnerSettingService.LoadFromStreamingAssets());
            var svc = PartnerSettingService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class NativePlaceServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NativePlaceService.LoadFromStreamingAssets());
            var svc = NativePlaceService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TimerTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TimerTaskService.LoadFromStreamingAssets());
            var svc = TimerTaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
