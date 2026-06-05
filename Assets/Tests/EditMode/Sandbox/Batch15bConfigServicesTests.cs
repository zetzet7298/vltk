// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 15b: Config services
// Vietnamese: Kiểm thử dịch vụ cấu hình (cấm item, thuế, tiến trình, rank, lò, platina, đúc lại, hồng bao thành)
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ForbitItemServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ForbitItemService.LoadFromStreamingAssets());
            var svc = ForbitItemService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaxRateServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaxRateService.LoadFromStreamingAssets());
            var svc = TaxRateService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class ProgressConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ProgressConfigService.LoadFromStreamingAssets());
            var svc = ProgressConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class RankSettingServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => RankSettingService.LoadFromStreamingAssets());
            var svc = RankSettingService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class FoundryResDemandServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FoundryResDemandService.LoadFromStreamingAssets());
            var svc = FoundryResDemandService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class PlatinaMagicRateServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PlatinaMagicRateService.LoadFromStreamingAssets());
            var svc = PlatinaMagicRateService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class RecoinServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => RecoinService.LoadFromStreamingAssets());
            var svc = RecoinService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class CityHongbaoServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => CityHongbaoService.LoadFromStreamingAssets());
            var svc = CityHongbaoService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
