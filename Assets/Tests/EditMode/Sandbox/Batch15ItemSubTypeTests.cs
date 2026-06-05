// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 15: Item sub-types
// Vietnamese: Kiểm thử dịch vụ trang bị tân hư, vân cương, phi phong, mặt nạ, ấn, trang sức, suite, công thức
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class BrokenEquipServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BrokenEquipService.LoadFromStreamingAssets());
            var svc = BrokenEquipService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class FusionServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FusionService.LoadFromStreamingAssets());
            var svc = FusionService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MantleServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MantleService.LoadFromStreamingAssets());
            var svc = MantleService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class MaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MaskService.LoadFromStreamingAssets());
            var svc = MaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class SignetServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SignetService.LoadFromStreamingAssets());
            var svc = SignetService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class ShipinServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ShipinService.LoadFromStreamingAssets());
            var svc = ShipinService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class SuiteActivateCountServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SuiteActivateCountService.LoadFromStreamingAssets());
            var svc = SuiteActivateCountService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class CompoundScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => CompoundScriptService.LoadFromStreamingAssets());
            var svc = CompoundScriptService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
