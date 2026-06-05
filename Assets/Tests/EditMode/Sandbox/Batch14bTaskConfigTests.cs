// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Batch 14b: Task Detail Config Services
// Vietnamese: Kiểm thử dịch vụ nhiệm vụ chi tiết (hằng ngày, ngẫu nhiên, liên kết, đối thoại, sự kiện)
// -----------------------------------------------------------------------------

using System;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class TaskDailyConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskDailyConfigService.LoadFromStreamingAssets());
            var svc = TaskDailyConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaskRandomConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskRandomConfigService.LoadFromStreamingAssets());
            var svc = TaskRandomConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaskLevelLinkServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskLevelLinkService.LoadFromStreamingAssets());
            var svc = TaskLevelLinkService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaskTalkConfigServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskTalkConfigService.LoadFromStreamingAssets());
            var svc = TaskTalkConfigService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class TaskEventServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TaskEventService.LoadFromStreamingAssets());
            var svc = TaskEventService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
