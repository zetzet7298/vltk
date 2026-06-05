// -----------------------------------------------------------------------------
// VLTK Mobile — StringResourceCatalogService: runtime service cho stringresource.txt
// Catalog bản dịch tiếng Việt cho các chuỗi GM/event/dialog từ PC.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class StringResourceCatalogService
    {
        public const string LogTag = "StringResource";

        private readonly PcStringResourceRegistry _reg;
        public int Count => _reg?.Count ?? 0;

        public StringResourceCatalogService() { _reg = new PcStringResourceRegistry(); }
        public StringResourceCatalogService(PcStringResourceRegistry reg) { _reg = reg ?? new PcStringResourceRegistry(); }

        public void RegisterRegistry(PcStringResourceRegistry reg)
        {
            if (reg == null) return;
            foreach (var e in reg.All) _reg.Add(e);
            SubsystemLog.Info(LogTag, $"Đã tải {_reg.Count} chuỗi tài nguyên");
        }

        public static StringResourceCatalogService LoadFromStreamingAssets(string subDir = "Reference")
        {
            var svc = new StringResourceCatalogService();
            var path = Path.Combine(Application.streamingAssetsPath, subDir);
            if (Directory.Exists(path))
            {
                svc.RegisterRegistry(PcStringResourceParser.BuildRegistry(path));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Directory không tồn tại {path}");
            }
            return svc;
        }

        public PcStringResourceEntry Get(int id) => _reg.Get(id);
        public string GetText(int id) => _reg.Get(id)?.Text;
        public IEnumerable<PcStringResourceEntry> GetAll() => _reg.All;
    }
}
