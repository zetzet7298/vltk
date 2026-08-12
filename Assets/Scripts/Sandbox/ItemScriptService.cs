// -----------------------------------------------------------------------------
// VLTK Mobile — ST-15.5 Item Scripts runtime service
// Quản lý 635 metadata scripts cho vật phẩm.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class ItemScriptService
    {
        public const string LogTag = "ItemScript";
        public const string DefaultStreamingDir = "Reference/PcItemFull";

        private PcItemScriptRegistry _registry;

        public int Count => _registry?.Count ?? 0;

        public ItemScriptService() { }
        public ItemScriptService(PcItemScriptRegistry registry) { _registry = registry; }

        public void RegisterRegistry(PcItemScriptRegistry reg)
        {
            _registry = reg;
            if (_registry == null) SubsystemLog.Warn(LogTag, "Item script registry rỗng");
        }

        public static string GetTriggerName(int trigger)
        {
            return trigger switch
            {
                0 => "Sử dụng",
                1 => "Trang bị",
                2 => "Tháo trang bị",
                3 => "Vứt bỏ",
                4 => "Nhận được",
                _ => $"Khác ({trigger})",
            };
        }

        public PcItemScriptEntry GetScript(int id) => _registry != null ? _registry.Get(id) : null;
        public IReadOnlyList<PcItemScriptEntry> GetByItem(int itemId)
            => _registry != null ? _registry.GetByItem(itemId) : System.Array.Empty<PcItemScriptEntry>();
        public IReadOnlyList<PcItemScriptEntry> GetByTrigger(int trigger)
            => _registry != null ? _registry.GetByTrigger(trigger) : System.Array.Empty<PcItemScriptEntry>();

        public string GetFunctionName(int scriptId)
        {
            return GetScript(scriptId)?.functionName ?? string.Empty;
        }

        public static ItemScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new ItemScriptService();
            if (Directory.Exists(dir))
            {
                svc.RegisterRegistry(PcItemScriptParser.BuildRegistry(dir));
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Không tìm thấy {dir}");
            }
            return svc;
        }
    }
}
