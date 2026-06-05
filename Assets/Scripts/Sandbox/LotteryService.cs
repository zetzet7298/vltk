// -----------------------------------------------------------------------------
// VLTK Mobile — Lottery Service (Quay Số / Rương Thần Bí)
// PC source: settings/lottery.txt + lotterys.txt + lotterys_.txt (254 entries).
// Runtime: gacha draw system với kiểm tra tái diễn theo ngày/tuần.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Phần thưởng từ quay số (Rương Thần Bí).</summary>
    [Serializable]
    public class LotteryReward
    {
        /// <summary>Thể loại vật phẩm (Genre từ PC).</summary>
        public int itemGenre;
        /// <summary>Loại chi tiết (DetailType từ PC).</summary>
        public int itemDetailType;
        /// <summary>Mã cụ thể (Particular từ PC).</summary>
        public int itemParticular;
        /// <summary>Số lượng nhận được.</summary>
        public int count = 1;
        /// <summary>Tên tiếng Việt (placeholder, runtime tự dịch).</summary>
        public string itemNameVi;
        /// <summary>Tên rương trúng thưởng.</summary>
        public string lotteryName;
    }

    /// <summary>
    /// Service quản lý quay số (Gacha / Rương Thần Bí).
    /// Hỗ trợ tái diễn hằng ngày (daysly) và hằng tuần (weekly).
    /// </summary>
    public class LotteryService
    {
        public const string DefaultStreamingDir = "Reference/PcLottery";

        private readonly PcLotteryRegistry _registry;
        private readonly Dictionary<string, int> _lotteryPullCounts = new();

        /// <summary>Sự kiện kích hoạt mỗi lần quay số thành công.</summary>
        public event Action<LotteryReward> OnLotteryDrawn;

        public int RegisteredCount => _registry?.Count ?? 0;
        public IReadOnlyDictionary<string, int> PullCounts => _lotteryPullCounts;

        public LotteryService(PcLotteryRegistry registry)
        {
            _registry = registry ?? new PcLotteryRegistry();
        }

        /// <summary>Khởi tạo từ thư mục StreamingAssets (gọi từ SandboxManager).</summary>
        public static LotteryService LoadFromStreamingAssets(string subDir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subDir) ? DefaultStreamingDir : subDir);
            var reg = PcLotteryParser.BuildRegistry(dir);
            return new LotteryService(reg);
        }

        /// <summary>Lấy thông tin rương theo tên.</summary>
        public PcLotteryEntry GetLottery(string name) => _registry.Get(name);

        /// <summary>Duyệt tất cả rương trong registry.</summary>
        public IEnumerable<PcLotteryEntry> GetAllLotteries() => _registry.All;

        /// <summary>
        /// Kiểm tra có thể quay rương tại thời điểm currentDayOfWeek (0=CN, 1=T2..7=T7).
        /// - daysly &gt; 0: có thể quay mỗi ngày (giới hạn theo daysly)
        /// - weekly &gt; 0: chỉ quay vào ngày trong tuần tương ứng
        /// - daysly=0 && weekly=0: luôn có thể quay
        /// </summary>
        public bool CanDraw(string name, int currentDayOfWeek)
        {
            var lot = _registry.Get(name);
            if (lot == null) return false;
            // Cả 2 cùng 0 = luôn quay được
            if (lot.daysly <= 0 && lot.weekly <= 0) return true;
            int currentCount = GetPullCount(name);
            if (lot.daysly > 0 && currentCount >= lot.daysly) return false;
            if (lot.weekly > 0)
            {
                if (lot.weekly == currentDayOfWeek) return true;
                if (currentCount >= lot.weekly) return false;
            }
            return true;
        }

        /// <summary>Quay rương theo tên. Trả về phần thưởng (item từ registry).</summary>
        public LotteryReward Draw(string name)
        {
            var lot = _registry.Get(name);
            if (lot == null) return null;
            var reward = new LotteryReward
            {
                itemGenre = lot.itemGenre,
                itemDetailType = lot.itemDetailType,
                itemParticular = lot.itemParticular,
                count = 1,
                itemNameVi = lot.name,
                lotteryName = lot.name,
            };
            _lotteryPullCounts[name] = GetPullCount(name) + 1;
            SubsystemLog.Info("Lottery", $"Quay số: {name} → {reward.itemNameVi} (đã quay {_lotteryPullCounts[name]} lần)");
            OnLotteryDrawn?.Invoke(reward);
            return reward;
        }

        /// <summary>Quay rương ngẫu nhiên bất kỳ trong registry.</summary>
        public LotteryReward DrawRandom()
        {
            var pool = new List<PcLotteryEntry>(_registry.All);
            if (pool.Count == 0) return null;
            var pick = pool[UnityEngine.Random.Range(0, pool.Count)];
            return Draw(pick.name);
        }

        /// <summary>Số lần đã quay của một rương cụ thể.</summary>
        public int GetPullCount(string name) =>
            _lotteryPullCounts.TryGetValue(name ?? string.Empty, out var c) ? c : 0;

        /// <summary>Reset toàn bộ đếm (gọi khi qua ngày mới / tuần mới).</summary>
        public void ResetCounts()
        {
            _lotteryPullCounts.Clear();
            SubsystemLog.Info("Lottery", "Đã reset toàn bộ đếm quay số");
        }

        /// <summary>Reset đếm cho một rương cụ thể.</summary>
        public void ResetCount(string name)
        {
            if (string.IsNullOrEmpty(name)) return;
            _lotteryPullCounts.Remove(name);
        }
    }
}
