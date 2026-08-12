// -----------------------------------------------------------------------------
// VLTK Mobile — ST Adjust Color runtime service
// Source: PC settings/adjustcolor.txt.
// Quản lý cấu hình điều chỉnh màu sắc (R/G/B/A).
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service quản lý Adjust Color (điều chỉnh màu sắc).
    /// </summary>
    public class AdjustColorService
    {
        private PcAdjustColorRegistry _reg;
        private IAdjustColorServiceHost _host;
        public int Count => _reg?.Count ?? 0;

        public AdjustColorService() { }
        public AdjustColorService(PcAdjustColorRegistry reg) { _reg = reg; }

        public void AttachHost(IAdjustColorServiceHost host) { _host = host; }

        public void RegisterRegistry(PcAdjustColorRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
            {
                SubsystemLog.Warn("AdjustColor", "Adjust color registry rỗng");
                if (_host != null) _host.OnColorRegistryEmpty();
            }
            else if (_host != null)
            {
                _host.OnColorRegistryAttached(_reg.Count);
                _host.LogColorEvent("load", 0, $"Loaded {_reg.Count} color presets");
                _host.PlayColorSFX("load", 0);
            }
        }

        public static AdjustColorService LoadFromStreamingAssets()
        {
            string root = Path.Combine(Application.streamingAssetsPath, "Reference");
            var reg = PcAdjustColorParser.BuildRegistry(root);
            return new AdjustColorService(reg);
        }

        public PcAdjustColorEntry GetColor(int id)
        {
            var c = _reg != null ? _reg.Get(id) : null;
            if (_host != null)
            {
                if (c != null)
                    _host.OnColorResolved(c.settingId, c.r, c.g, c.b, c.a, c.description);
                else
                    _host.LogColorEvent("query_missing", id, "Color preset not found in registry");
            }
            return c;
        }
        public IReadOnlyList<PcAdjustColorEntry> All
        {
            get
            {
                var list = _reg != null ? _reg.All : (IReadOnlyList<PcAdjustColorEntry>)System.Array.Empty<PcAdjustColorEntry>();
                if (_host != null) _host.OnAllColorsQueried(list.Count);
                return list;
            }
        }

        public void ApplyColor(int settingId)
        {
            var c = GetColor(settingId);
            if (c == null) return;
            if (_host != null)
            {
                _host.OnColorApplied(c.settingId, c.r, c.g, c.b, c.a);
                _host.ShowColorUI(c.settingId, c.r, c.g, c.b, c.a);
                _host.LogColorEvent("apply", c.settingId, $"Applied RGBA({c.r},{c.g},{c.b},{c.a})");
                _host.PlayColorSFX("apply", c.settingId);
                _host.SaveColorState(c.settingId, c.r, c.g, c.b, c.a);
            }
        }

        public void PreviewColor(int settingId)
        {
            var c = GetColor(settingId);
            if (c == null) return;
            if (_host != null)
            {
                _host.OnColorPreviewed(c.settingId, c.r, c.g, c.b, c.a);
                _host.ShowColorUI(c.settingId, c.r, c.g, c.b, c.a);
                _host.LogColorEvent("preview", c.settingId, $"Previewed RGBA({c.r},{c.g},{c.b},{c.a})");
                _host.PlayColorSFX("preview", c.settingId);
            }
        }
    }
}
