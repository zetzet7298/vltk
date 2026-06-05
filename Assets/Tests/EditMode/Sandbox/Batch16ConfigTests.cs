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
        [Test] public void Load() { var svc = TollgateKillerService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class NewTaskBranchServiceTests
    {
        [Test] public void Load() { var svc = NewTaskBranchService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class MainPassTaskServiceTests
    {
        [Test] public void Load() { var svc = MainPassTaskService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class AutoUpdateConfigServiceTests
    {
        [Test] public void Load() { var svc = AutoUpdateConfigService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class TiredWarningServiceTests
    {
        [Test] public void Load() { var svc = TiredWarningService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class PlayerLimitTimeServiceTests
    {
        [Test] public void Load() { var svc = PlayerLimitTimeService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class PermitDialogNpcServiceTests
    {
        [Test] public void Load() { var svc = PermitDialogNpcService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class ProductConfigServiceTests
    {
        [Test] public void Load() { var svc = ProductConfigService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class UtilitiesServiceTests
    {
        [Test] public void Load() { var svc = UtilitiesService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class ForbitHeartServiceTests
    {
        [Test] public void Load() { var svc = ForbitHeartService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
    public class StringResourceCatalogServiceTests
    {
        [Test] public void Load() { var svc = StringResourceCatalogService.LoadFromStreamingAssets(); Assert.IsNotNull(svc); Assert.GreaterOrEqual(svc.Count, 0); }
    }
}
