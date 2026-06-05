// -----------------------------------------------------------------------------
// VLTK Mobile — ST-XX Meridian Service (Kinh Mạch runtime)
// Wraps PcMeridianRegistry. Player state = Dictionary<acupointId, level 0-9>.
// TryUpgrade: roll vs entry.successRate (scaled from /10000). On fail drop to
// entry.fallbackLevel. On success level++ clamped to 9. Req level = acupointId
// (the PC file uses acupointId as the level — meridian unlocks scale by level).
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
    /// Service quản lý hệ thống Kinh Mạch (128 huyệt đạo, 12+ mạch).
    /// PC source: meridian_level.txt (successRate scaled by 10000, fallback level
    /// on fail, level unlock = acupointId).
    /// </summary>
    public class MeridianService
    {
        public const int MaxAcupointLevel = 9;

        private PcMeridianRegistry _registry;
        private readonly Dictionary<int, int> _acupointLevels = new();
        private System.Random _rng = new System.Random();

        /// <summary>Sự kiện khi huyệt đạo được đột phá (thành công hoặc thất bại).</summary>
        public event Action<int, int, UpgradeResult> MeridianUpgradeResult; // (acupointId, newLevel, result)

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

        public PcMeridianEntry GetAcupoint(int acupointId)
            => _registry != null ? _registry.GetAcupoint(acupointId) : null;

        public IReadOnlyList<PcMeridianEntry> GetMeridianPoints(int meridianId)
            => _registry != null
                ? _registry.GetMeridianPoints(meridianId)
                : (IReadOnlyList<PcMeridianEntry>)Array.Empty<PcMeridianEntry>();

        public IEnumerable<int> GetMeridianIds()
        {
            if (_registry == null) yield break;
            // De-duplicate meridianIds by walking each acupoint
            var seen = new HashSet<int>();
            for (int id = 1; id <= _registry.MaxAcupointId; id++)
            {
                var e = _registry.GetAcupoint(id);
                if (e != null && seen.Add(e.meridianId))
                    yield return e.meridianId;
            }
        }

        // ── Player Progress APIs ──────────────────────────────────────

        public int GetPlayerAcupointLevel(int acupointId)
            => _acupointLevels.TryGetValue(acupointId, out var lv) ? lv : 0;

        public void SetPlayerAcupointLevel(int acupointId, int level)
        {
            if (level < 0) level = 0;
            if (level > MaxAcupointLevel) level = MaxAcupointLevel;
            _acupointLevels[acupointId] = level;
        }

        /// <summary>
        /// Kiểm tra nhân vật đã đạt cấp yêu cầu để tu luyện huyệt đạo hay chưa.
        /// PC file maps acupointId to required player level (each acupoint unlocks
        /// at the same numeric level as its ID — e.g. huyệt 10 mở ở cấp 10).
        /// </summary>
        public bool IsPrereqMet(int acupointId, int playerLevel)
        {
            if (_registry == null) return false;
            var entry = _registry.GetAcupoint(acupointId);
            if (entry == null) return false;
            return playerLevel >= acupointId;
        }

        /// <summary>
        /// Thử đột phá huyệt đạo. successRate tính theo /10000 (e.g. 8000 = 80%).
        /// Random number vs rate → Success hoặc Failed. Thất bại tụt về
        /// entry.fallbackLevel. Thành công +1 (max 9).
        /// </summary>
        public UpgradeResult TryUpgrade(int acupointId, int playerLevel)
        {
            if (_registry == null) return UpgradeResult.NotFound;
            var entry = _registry.GetAcupoint(acupointId);
            if (entry == null)
            {
                SubsystemLog.Warn("Meridian", $"Huyệt đạo {acupointId} không tồn tại.");
                return UpgradeResult.NotFound;
            }
            if (!IsPrereqMet(acupointId, playerLevel))
            {
                SubsystemLog.Info("Meridian",
                    $"Nhân vật cấp {playerLevel} chưa đủ tu luyện huyệt {acupointId} (cần cấp {acupointId}).");
                return UpgradeResult.PrereqLevel;
            }
            int current = GetPlayerAcupointLevel(acupointId);
            if (current >= MaxAcupointLevel)
            {
                SubsystemLog.Info("Meridian", $"Huyệt đạo {acupointId} đã đạt cấp tối đa ({MaxAcupointLevel}).");
                return UpgradeResult.MaxLevel;
            }

            int roll = _rng.Next(0, 10000);
            UpgradeResult result;
            int newLevel;
            if (roll < entry.successRate)
            {
                newLevel = current + 1;
                if (newLevel > MaxAcupointLevel) newLevel = MaxAcupointLevel;
                _acupointLevels[acupointId] = newLevel;
                result = UpgradeResult.Success;
                SubsystemLog.Info("Meridian",
                    $"Đột phá thành công huyệt {acupointId} ({entry.nameRaw}): cấp {current} → {newLevel}.");
            }
            else
            {
                newLevel = entry.fallbackLevel;
                if (newLevel < 0) newLevel = 0;
                if (newLevel > MaxAcupointLevel) newLevel = MaxAcupointLevel;
                _acupointLevels[acupointId] = newLevel;
                result = UpgradeResult.Failed;
                SubsystemLog.Info("Meridian",
                    $"Thất bại đột phá huyệt {acupointId} ({entry.nameRaw}): tụt về cấp {newLevel}.");
            }
            MeridianUpgradeResult?.Invoke(acupointId, newLevel, result);
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
