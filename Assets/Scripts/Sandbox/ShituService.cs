// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.13 Shitu Service (Sư Đồ runtime)
// Quản lý quan hệ Sư Phụ - Đồ Đệ, phần thưởng khi đạt đủ điều kiện.
// PC source: settings/shitu.txt (sư đồ).
// Vietnamese: "Sư Phụ", "Đồ Đệ", "Danh Vọng", "Phần Thưởng Sư Đồ".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý quan hệ Sư Đồ (Master/Apprentice runtime).</summary>
    public class ShituService
    {
        public const string LogTag = "Shitu";

        private PcShituRegistry _registry;

        public event Action OnShituLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public ShituService() : this(null) { }

        public ShituService(PcShituRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcShituRegistry registry)
        {
            _registry = registry ?? new PcShituRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} quan hệ sư đồ");
            OnShituLoaded?.Invoke();
        }

        public PcShituEntry GetShitu(int shituId)
            => _registry != null ? _registry.Get(shituId) : null;

        /// <summary>
        /// Kiểm tra có thể thiết lập quan hệ sư đồ với cấp master/apprentice cho trước.
        /// </summary>
        public bool CanBecome(int shituId, int masterLevel, int apprenticeLevel)
        {
            var entry = GetShitu(shituId);
            if (entry == null) return false;
            return masterLevel >= entry.masterLevel
                && apprenticeLevel > 0
                && apprenticeLevel <= entry.apprenticeLevel;
        }

        public IEnumerable<PcShituEntry> GetAll()
            => _registry != null ? _registry.All : (IEnumerable<PcShituEntry>)Array.Empty<PcShituEntry>();

        public static ShituService LoadFromStreamingAssets(string subdir = "Reference/PcAttrib")
        {
            var svc = new ShituService();
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            if (Directory.Exists(dir))
            {
                var reg = PcShituParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            if (svc.Count == 0)
            {
                // Thử root
                string rootMain = Path.Combine(Application.streamingAssetsPath, "Reference/shitu.txt");
                if (File.Exists(rootMain))
                {
                    var reg2 = new PcShituRegistry();
                    foreach (var e in PcShituParser.ParseFile(rootMain)) reg2.Register(e);
                    svc.AttachRegistry(reg2);
                    return svc;
                }
                SubsystemLog.Warn(LogTag, "Shitu: không tìm thấy shitu.txt trong StreamingAssets");
                svc.OnShituLoaded?.Invoke();
            }
            return svc;
        }
    }
}
