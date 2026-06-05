// -----------------------------------------------------------------------------
// VLTK Mobile — Magic Script runtime service
// Wraps PcMagicScriptRegistry. Exposes script lookup by id/attrib/trigger.
// Vietnamese: "Script phép", "Kích hoạt khi đánh trúng", "Kích hoạt khi giết".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service truy vấn script phép (Magic Script).
    /// PC source: settings/magicscript.txt.
    /// </summary>
    public class MagicScriptService
    {
        public const string LogTag = "MagicScript";
        private PcMagicScriptRegistry _registry = new();

        public event Action OnLoaded;
        public int Count => _registry?.Count ?? 0;

        public void AttachRegistry(PcMagicScriptRegistry registry)
        {
            _registry = registry ?? new PcMagicScriptRegistry();
            OnLoaded?.Invoke();
        }

        public static MagicScriptService LoadFromStreamingAssets(string relativeDir = "Reference/PcItemFull")
        {
            var svc = new MagicScriptService();
            try
            {
                var path = Path.Combine(Application.streamingAssetsPath, relativeDir);
                var reg = PcMagicScriptParser.BuildRegistry(path);
                svc.AttachRegistry(reg);
                Debug.Log($"[{LogTag}] Loaded {svc.Count} script phép từ {path}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[{LogTag}] Load thất bại: {ex.Message}");
            }
            return svc;
        }

        public PcMagicScriptEntry GetScript(int id) => _registry.Get(id);
        public List<PcMagicScriptEntry> GetByAttrib(int attribId) => _registry.GetByAttrib(attribId);
        public List<PcMagicScriptEntry> GetByTrigger(int trigger) => _registry.GetByTrigger(trigger);
        public IEnumerable<PcMagicScriptEntry> All => _registry.All;
    }
}
