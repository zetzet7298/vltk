// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.12 Honor Service (Vinh Danh runtime)
// Quản lý hệ thống vinh danh: 6 cấp bậc, danh hiệu thưởng, hào quang kích hoạt.
// PC source: settings/honor.txt (vinh danh).
// Vietnamese: "Vinh Danh", "Danh Hiệu", "Quang Huy", "Hào Quang".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Vinh Danh (Honor System runtime).</summary>
    public class HonorService
    {
        public const string LogTag = "Honor";

        private PcHonorRegistry _registry;

        public event Action OnHonorLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public HonorService() : this(null) { }

        public HonorService(PcHonorRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcHonorRegistry registry)
        {
            _registry = registry ?? new PcHonorRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} vinh danh");
            OnHonorLoaded?.Invoke();
        }

        public PcHonorEntry GetHonor(int honorId)
            => _registry != null ? _registry.Get(honorId) : null;

        public PcHonorEntry GetByPoints(int points)
            => _registry != null ? _registry.GetByPoints(points) : null;

        /// <summary>Kiểm tra có đủ điểm để đạt vinh danh này không.</summary>
        public bool CanAchieve(int honorId, int points)
        {
            var entry = GetHonor(honorId);
            if (entry == null) return false;
            return points >= entry.requiredPoints;
        }

        public IEnumerable<PcHonorEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcHonorEntry>)Array.Empty<PcHonorEntry>();

        public static HonorService LoadFromStreamingAssets(string subdir = "Reference/PcAttrib")
        {
            var svc = new HonorService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcHonorParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            // Fallback: tìm honor.txt ở root.
            if (svc.Count == 0)
            {
                var fallback = PcHonorParser.BuildRegistryFromRoot();
                if (fallback.Count > 0)
                {
                    svc.AttachRegistry(fallback);
                    return svc;
                }
            }
            if (svc.Count == 0)
                SubsystemLog.Warn(LogTag, "Honor: không tìm thấy honor.txt trong StreamingAssets");
            if (svc.Count == 0) svc.OnHonorLoaded?.Invoke();
            return svc;
        }
    }
}
