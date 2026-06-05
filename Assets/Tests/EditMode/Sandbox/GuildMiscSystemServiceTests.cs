// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for Guild Rank/Stunt/Task, Honor, Shitu, Foundry, WorldRank
// Tập test runtime services cho hệ thống Bang Hội mở rộng và các hệ thống phụ.
// Vietnamese: Kiểm thử các service Bang Hội (cấp bậc, stunt, task), Vinh Danh, Sư Đồ, Luyện Đồ, Bảng Xếp Hạng.
// -----------------------------------------------------------------------------

using System;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class GuildRankServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            // Phải load an toàn kể cả khi file không tồn tại.
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildRankService.LoadFromStreamingAssets());
            var svc = GuildRankService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetByAuthority_FiltersCorrectly()
        {
            // Nếu registry rỗng (file thiếu), test pass với list rỗng.
            var svc = GuildRankService.LoadFromStreamingAssets();
            var members = svc.GetByAuthority(0);
            Assert.IsNotNull(members);
            foreach (var r in members)
            {
                Assert.AreEqual(0, r.authority);
            }
            var leaders = svc.GetByAuthority(2);
            Assert.IsNotNull(leaders);
            foreach (var r in leaders)
            {
                Assert.AreEqual(2, r.authority);
            }
        }
    }

    public class GuildStuntServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildStuntService.LoadFromStreamingAssets());
            var svc = GuildStuntService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetForLevel_FiltersCorrectly()
        {
            var svc = GuildStuntService.LoadFromStreamingAssets();
            var list = svc.GetForLevel(99);
            Assert.IsNotNull(list);
            foreach (var s in list)
            {
                Assert.LessOrEqual(s.cycleWeeks, 99,
                    $"Stunt id={s.stuntId} cycle={s.cycleWeeks} phải <= guildLevel 99");
            }
            var empty = svc.GetForLevel(0);
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Count, "guildLevel=0 → không có stunt nào mở khóa");
        }
    }

    public class GuildTaskServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => GuildTaskService.LoadFromStreamingAssets());
            var svc = GuildTaskService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetByLevel_FiltersCorrectly()
        {
            var svc = GuildTaskService.LoadFromStreamingAssets();
            var list = svc.GetByLevel(100);
            Assert.IsNotNull(list);
            foreach (var t in list)
            {
                Assert.LessOrEqual(t.requiredLevel, 100,
                    $"Task id={t.taskId} required={t.requiredLevel} phải <= level 100");
            }
        }
    }

    public class HonorServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => HonorService.LoadFromStreamingAssets());
            var svc = HonorService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanAchieve_RejectsInsufficientPoints()
        {
            var svc = HonorService.LoadFromStreamingAssets();
            // Với id bất kỳ: tìm entry có requiredPoints > 0 để test.
            PcHonorEntry target = null;
            foreach (var e in svc.GetAll())
            {
                if (e != null && e.requiredPoints > 0) { target = e; break; }
            }
            if (target == null)
            {
                Assert.Pass("Không có entry nào có requiredPoints > 0 để test");
                return;
            }
            Assert.IsTrue(svc.CanAchieve(target.honorId, target.requiredPoints),
                "Đủ điểm → CanAchieve=true");
            Assert.IsFalse(svc.CanAchieve(target.honorId, target.requiredPoints - 1),
                "Thiếu 1 điểm → CanAchieve=false");
            Assert.IsFalse(svc.CanAchieve(999_999, 9_999_999),
                "ID không tồn tại → CanAchieve=false");
        }
    }

    public class ShituServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ShituService.LoadFromStreamingAssets());
            var svc = ShituService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanBecome_ValidatesLevels()
        {
            var svc = ShituService.LoadFromStreamingAssets();
            PcShituEntry target = null;
            foreach (var e in svc.GetAll())
            {
                if (e != null) { target = e; break; }
            }
            if (target == null)
            {
                Assert.Pass("Registry rỗng → skip");
                return;
            }
            // Đủ cấp master + đồ đệ trong range → true
            Assert.IsTrue(svc.CanBecome(target.shituId, target.masterLevel, Math.Min(target.apprenticeLevel, 1)));
            // Sư phụ dưới cấp yêu cầu → false
            if (target.masterLevel > 0)
                Assert.IsFalse(svc.CanBecome(target.shituId, target.masterLevel - 1, 1));
            // Đồ đệ quá cấp → false
            Assert.IsFalse(svc.CanBecome(target.shituId, target.masterLevel, target.apprenticeLevel + 1));
            // Đồ đệ cấp 0 → false
            Assert.IsFalse(svc.CanBecome(target.shituId, target.masterLevel, 0));
            // ID không tồn tại → false
            Assert.IsFalse(svc.CanBecome(999_999, 100, 50));
        }
    }

    public class FoundryServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FoundryService.LoadFromStreamingAssets());
            var svc = FoundryService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetRecipe_ReturnsNullForInvalid()
        {
            var svc = FoundryService.LoadFromStreamingAssets();
            // ID không tồn tại → null
            Assert.IsNull(svc.GetRecipe(0, 0), "GetRecipe(0,0) phải trả null");
            Assert.IsNull(svc.GetRecipe(999_999, 999_999), "GetRecipe với genre/detail không tồn tại phải trả null");
            Assert.IsNull(svc.GetRecipe(-1, -1), "Genre/detail âm phải trả null");
        }
    }

    public class WorldRankServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => WorldRankService.LoadFromStreamingAssets());
            var svc = WorldRankService.LoadFromStreamingAssets();
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanRank_ValidatesScore()
        {
            var svc = WorldRankService.LoadFromStreamingAssets();
            // ID không tồn tại → false
            Assert.IsFalse(svc.CanRank(999, 1_000_000));
            Assert.IsFalse(svc.CanRank(-1, 0));
            // Có entry: kiểm tra minScore / maxScore range
            PcWorldRankEntry any = null;
            foreach (var e in svc.GetAllRanks()) { any = e; break; }
            if (any == null)
            {
                Assert.Pass("Registry rỗng → skip");
                return;
            }
            // Điểm vượt maxScore → false (nếu maxScore > 0)
            if (any.maxScore > 0)
            {
                Assert.IsFalse(svc.CanRank(any.rankType, any.maxScore + 1),
                    "Điểm > maxScore → CanRank=false");
            }
            // Điểm >= minScore → true (nếu minScore > 0)
            if (any.minScore > 0)
            {
                Assert.IsTrue(svc.CanRank(any.rankType, any.minScore),
                    "Điểm = minScore → CanRank=true");
                Assert.IsFalse(svc.CanRank(any.rankType, any.minScore - 1),
                    "Điểm < minScore → CanRank=false");
            }
        }
    }
}
