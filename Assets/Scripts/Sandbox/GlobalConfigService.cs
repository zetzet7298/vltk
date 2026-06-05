// -----------------------------------------------------------------------------
// VLTK Mobile — Cấu hình chung (Global config) runtime service
// Wraps PcGlobalConfigRegistry. Exposes key/value lookup with int parsing.
// Vietnamese: "Cấu hình chung", "Tham số hệ thống".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn cấu hình chung.
    /// PC source: settings/global.ini.
    /// </summary>
    public class GlobalConfigService
    {
        public const string LogTag = "GlobalConfig";
        private PcGlobalConfigRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcGlobalConfigRegistry registry)
        {
            _registry = registry ?? new PcGlobalConfigRegistry();
            OnLoaded?.Invoke();
        }

        public static GlobalConfigService LoadFromStreamingAssets(string relativeDir = "Reference/PcAttrib")
        {
            var svc = new GlobalConfigService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcGlobalConfigParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} mục cấu hình chung từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public string GetValue(string key) => _registry.GetValue(key);
        public int GetIntValue(string key, int defaultVal = 0) => _registry.GetIntValue(key, defaultVal);
        public List<PcGlobalConfigEntry> GetAll() => _registry.GetAll();
        public IEnumerable<PcGlobalConfigEntry> All => _registry.All;
    }
}
