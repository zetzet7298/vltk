// -----------------------------------------------------------------------------
// VLTK Mobile — ProductConfigService: runtime service cho product_config.ini
// Cấu hình vùng (Region) và ngôn ngữ (Language) của sản phẩm (PC).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ProductConfigService
    {
        public const string LogTag = "ProductConfig";

        private readonly PcProductConfigRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public ProductConfigService() { _reg = new PcProductConfigRegistry(); }
        public ProductConfigService(PcProductConfigRegistry reg) { _reg = reg ?? new PcProductConfigRegistry(); }

        public void RegisterRegistry(PcProductConfigRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} section product config");
        }

        public static ProductConfigService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new ProductConfigService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcProductConfigParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcProductConfigEntry GetVersion() => _reg.Get("VersionCfg");
        public string GetRegion() => GetVersion()?.KeyValues.TryGetValue("ProductRegion", out var v) == true ? v : "4";
        public string GetLanguage() => GetVersion()?.KeyValues.TryGetValue("ProductLanguage", out var v) == true ? v : "5";
        public IEnumerable<PcProductConfigEntry> GetAll() => _reg.All;
    }
}
