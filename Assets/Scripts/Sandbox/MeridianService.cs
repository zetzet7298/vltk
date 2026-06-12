// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Meridian Service (Kinh Mạch runtime)
// Wraps PcMeridianRegistry. The PC file is 8 meridians (经脉ID 1-8) × 16 acupoints
// each (穴位ID = per-meridian tier 1-16) = 128 acupoints. An acupoint is therefore
// identified by the COMPOSITE (meridianId, level); the level alone is NOT unique.
// Player state = Dictionary<(meridian, level), playerTier 0-9>.
// TryUpgrade: roll vs entry.successRate (scaled from /10000). On fail drop to
// entry.fallbackLevel. On success tier++ clamped to 9. Req player level = acupoint
// level (穴位ID「同时也是等级」 — meridian unlocks scale by level).
// Vietnamese logs: "Huyền Khí", "Kinh Mạch", "Huyệt Đạo", "Đột Phá", "Thất Bại".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public enum UpgradeResult
    {
        Success = 0,    // Đột phá thành công
        Failed = 1,     // Thất bại, bị tụt cấp
        MaxLevel = 2,   // Đã đạt cấp tối đa
        PrereqLevel = 3,// Cấp nhân vật chưa đủ
        NotFound = 4,   // Không tìm thấy huyệt đạo
    }

    /// <summary>
    /// Service quản lý hệ thống Kinh Mạch (128 huyệt đạo = 8 mạch × 16 cấp).
    /// PC source: meridian_level.txt (successRate scaled by 10000, fallback level
    /// on fail, level unlock = acupoint level). Huyệt đạo được định danh bằng
    /// cặp (mạch, cấp) — chỉ số cấp lặp lại giữa các mạch nên không thể key đơn.
    /// </summary>
    public class MeridianService
    {
        public const int MaxAcupointLevel = 9;

        private PcMeridianRegistry _registry;
        private readonly Dictionary<(int meridianId, int level), int> _acupointLevels = new();
        private System.Random _rng = new System.Random();

        /// <summary>Sự kiện khi huyệt đạo được đột phá (thành công hoặc thất bại).</summary>
        public event Action<int, int, int, UpgradeResult> MeridianUpgradeResult; // (meridianId, level, newTier, result)

        public int Count => _registry != null ? _registry.Count : 0;

        public MeridianService() { }

        public MeridianService(PcMeridianRegistry registry)
        {
            _registry = registry;
        }

        public void RegisterRegistry(PcMeridianRegistry registry)
        {
            _registry = registry;
        }

        // ── Query APIs ────────────────────────────────────────────────

        /// <summary>Tra cứu huyệt đạo theo cặp (mạch, cấp).</summary>
        public PcMeridianEntry GetAcupoint(int meridianId, int level)
            => _registry != null ? _registry.GetAcupoint(meridianId, level) : null;

        public IReadOnlyList<PcMeridianEntry> GetMeridianPoints(int meridianId)
            => _registry != null
                ? _registry.GetMeridianPoints(meridianId)
                : (IReadOnlyList<PcMeridianEntry>)Array.Empty<PcMeridianEntry>();

        public IEnumerable<int> GetMeridianIds()
            => _registry != null ? _registry.MeridianIds : (IEnumerable<int>)Array.Empty<int>();

        // ── Player Progress APIs ──────────────────────────────────────

        public int GetPlayerAcupointLevel(int meridianId, int level)
            => _acupointLevels.TryGetValue((meridianId, level), out var lv) ? lv : 0;

        public void SetPlayerAcupointLevel(int meridianId, int level, int tier)
        {
            if (tier < 0) tier = 0;
            if (tier > MaxAcupointLevel) tier = MaxAcupointLevel;
            _acupointLevels[(meridianId, level)] = tier;
        }

        /// <summary>
        /// Kiểm tra nhân vật đã đạt cấp yêu cầu để tu luyện huyệt đạo hay chưa.
        /// PC file maps acupoint level (穴位ID) to required player level (each acupoint
        /// unlocks at the same numeric level as its tier — e.g. huyệt cấp 10 mở ở cấp 10).
        /// </summary>
        public bool IsPrereqMet(int meridianId, int level, int playerLevel)
        {
            if (_registry == null) return false;
            var entry = _registry.GetAcupoint(meridianId, level);
            if (entry == null) return false;
            return playerLevel >= level;
        }

        /// <summary>
        /// Thử đột phá huyệt đạo (mạch, cấp). successRate tính theo /10000 (e.g. 8000 = 80%).
        /// Random number vs rate → Success hoặc Failed. Thất bại tụt về
        /// entry.fallbackLevel. Thành công +1 (max 9).
        /// </summary>
        public UpgradeResult TryUpgrade(int meridianId, int level, int playerLevel)
        {
            if (_registry == null) return UpgradeResult.NotFound;
            var entry = _registry.GetAcupoint(meridianId, level);
            if (entry == null)
            {
                SubsystemLog.Warn("Meridian", $"Huyệt đạo mạch {meridianId} cấp {level} không tồn tại.");
                return UpgradeResult.NotFound;
            }
            if (!IsPrereqMet(meridianId, level, playerLevel))
            {
                SubsystemLog.Info("Meridian",
                    $"Nhân vật cấp {playerLevel} chưa đủ tu luyện huyệt mạch {meridianId} cấp {level} (cần cấp {level}).");
                return UpgradeResult.PrereqLevel;
            }
            int current = GetPlayerAcupointLevel(meridianId, level);
            if (current >= MaxAcupointLevel)
            {
                SubsystemLog.Info("Meridian", $"Huyệt đạo mạch {meridianId} cấp {level} đã đạt cấp tối đa ({MaxAcupointLevel}).");
                return UpgradeResult.MaxLevel;
            }

            int roll = _rng.Next(0, 10000);
            UpgradeResult result;
            int newTier;
            if (roll < entry.successRate)
            {
                newTier = current + 1;
                if (newTier > MaxAcupointLevel) newTier = MaxAcupointLevel;
                _acupointLevels[(meridianId, level)] = newTier;
                result = UpgradeResult.Success;
                SubsystemLog.Info("Meridian",
                    $"Đột phá thành công huyệt mạch {meridianId} cấp {level} ({entry.nameRaw}): {current} → {newTier}.");
            }
            else
            {
                newTier = entry.fallbackLevel;
                if (newTier < 0) newTier = 0;
                if (newTier > MaxAcupointLevel) newTier = MaxAcupointLevel;
                _acupointLevels[(meridianId, level)] = newTier;
                result = UpgradeResult.Failed;
                SubsystemLog.Info("Meridian",
                    $"Thất bại đột phá huyệt mạch {meridianId} cấp {level} ({entry.nameRaw}): tụt về cấp {newTier}.");
            }
            MeridianUpgradeResult?.Invoke(meridianId, level, newTier, result);
            return result;
        }

        /// <summary>Đặt seed cho random (phục vụ test deterministic).</summary>
        public void SetSeed(int seed) => _rng = new System.Random(seed);

        /// <summary>
        /// Tải registry từ StreamingAssets/Reference/PcMeridian và khởi tạo service.
        /// Trả về service mới; trả về null nếu thư mục không tồn tại.
        /// </summary>
        public static MeridianService LoadFromStreamingAssets(string subdir = "Reference/PcMeridian")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (!Directory.Exists(dir)) return null;
            var reg = PcMeridianParser.BuildRegistry(dir);
            return new MeridianService(reg);
        }
    }
}
