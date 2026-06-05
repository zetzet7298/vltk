// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 16: Tollgate/Newtask + Remaining Config
// Vietnamese: Kiểm thử dịch vụ trạm kiểm tra, nhiệm vụ nhánh/chính, cấu hình còn lại
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class TollgateKillerServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TollgateKillerService.LoadFromStreamingAssets()); Assert.IsNotNull(TollgateKillerService.LoadFromStreamingAssets()); }
    }
    public class NewTaskBranchServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NewTaskBranchService.LoadFromStreamingAssets()); Assert.IsNotNull(NewTaskBranchService.LoadFromStreamingAssets()); }
    }
    public class MainPassTaskServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => MainPassTaskService.LoadFromStreamingAssets()); Assert.IsNotNull(MainPassTaskService.LoadFromStreamingAssets()); }
    }
    public class AutoUpdateConfigServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => AutoUpdateConfigService.LoadFromStreamingAssets()); Assert.IsNotNull(AutoUpdateConfigService.LoadFromStreamingAssets()); }
    }
    public class TiredWarningServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TiredWarningService.LoadFromStreamingAssets()); Assert.IsNotNull(TiredWarningService.LoadFromStreamingAssets()); }
    }
    public class PlayerLimitTimeServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PlayerLimitTimeService.LoadFromStreamingAssets()); Assert.IsNotNull(PlayerLimitTimeService.LoadFromStreamingAssets()); }
    }
    public class PermitDialogNpcServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => PermitDialogNpcService.LoadFromStreamingAssets()); Assert.IsNotNull(PermitDialogNpcService.LoadFromStreamingAssets()); }
    }
    public class ProductConfigServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ProductConfigService.LoadFromStreamingAssets()); Assert.IsNotNull(ProductConfigService.LoadFromStreamingAssets()); }
    }
    public class UtilitiesServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => UtilitiesService.LoadFromStreamingAssets()); Assert.IsNotNull(UtilitiesService.LoadFromStreamingAssets()); }
    }
    public class ForbitHeartServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ForbitHeartService.LoadFromStreamingAssets()); Assert.IsNotNull(ForbitHeartService.LoadFromStreamingAssets()); }
    }
    public class StringResourceCatalogServiceTests
    {
        [Test] public void Load() { ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => StringResourceCatalogService.LoadFromStreamingAssets()); Assert.IsNotNull(StringResourceCatalogService.LoadFromStreamingAssets()); }
    }
}
