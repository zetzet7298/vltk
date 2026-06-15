// -----------------------------------------------------------------------------
// VLTK Mobile — ST-14.x Area Script Service
// Quản lý 9 vùng bản đồ GBK (Đông Bắc, Đại Lý, Thiên Vương, ...).
// Vietnamese: "Vùng Bản Đồ", "Nhiệm Vụ Môn Phái", "Thị Trấn", "PvP", "Thành Phố Lớn".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý khu vực bản đồ GBK (Đại Lý, Phượng Tường, ...).</summary>
    public class AreaScriptService
    {
        public const string LogTag = "AreaScript";
        public const string DefaultStreamingDir = "Reference/PcArea";

        private PcAreaScriptRegistry _reg;
        private IAreaScriptServiceHost _host;

        public int Count => _reg?.Count ?? 0;

        public AreaScriptService() { }
        public AreaScriptService(PcAreaScriptRegistry reg) { _reg = reg; }

        public void AttachHost(IAreaScriptServiceHost host) { _host = host; }

        public void RegisterRegistry(PcAreaScriptRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
            {
                SubsystemLog.Warn(LogTag, "Area script registry rỗng");
                if (_host != null) _host.OnAreaRegistryEmpty();
            }
            else if (_host != null)
            {
                _host.OnAreaRegistryAttached(_reg.Count);
                _host.LogAreaEvent("load", 0, $"Loaded {_reg.Count} areas");
                _host.PlayAreaSFX("load", 0);
            }
        }

        public static AreaScriptService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new AreaScriptService();
            if (Directory.Exists(dir))
            {
                var reg = PcAreaScriptParser.BuildRegistry(dir);
                svc.RegisterRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Area script directory không tồn tại {dir}");
            }
            return svc;
        }

        public PcAreaScriptEntry GetArea(int id)
        {
            var e = _reg != null ? _reg.Get(id) : null;
            if (_host != null)
            {
                if (e != null)
                    _host.OnAreaResolved(e.areaId, e.areaNameRaw, e.mapId, e.category);
                else
                    _host.LogAreaEvent("query_missing", id, "Area not found in registry");
            }
            return e;
        }
        public IReadOnlyList<PcAreaScriptEntry> GetByCategory(int category)
        {
            var list = _reg != null ? _reg.GetByCategory(category) : System.Array.Empty<PcAreaScriptEntry>();
            if (_host != null)
                _host.OnAreasByCategoryQueried(category, list.Count, GetCategoryName(category));
            return list;
        }
        public IReadOnlyList<PcAreaScriptEntry> GetByMap(int mapId)
        {
            var list = _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcAreaScriptEntry>();
            if (_host != null)
                _host.OnAreasByMapQueried(mapId, list.Count);
            return list;
        }
        public IReadOnlyList<PcAreaScriptEntry> GetAreasInCategory(int category) => GetByCategory(category);
        public int GetTotalScriptCount()
        {
            int n = _reg != null ? _reg.GetTotalScriptCount() : 0;
            if (_host != null) _host.OnTotalScriptCountQueried(n);
            return n;
        }

        public string GetCategoryName(int category)
        {
            string name = category switch
            {
                0 => "Khu Vực Bản Đồ",
                1 => "Nhiệm Vụ Môn Phái",
                2 => "Thị Trấn",
                3 => "PvP",
                4 => "Thành Phố Lớn",
                _ => $"Khác ({category})",
            };
            if (_host != null) _host.OnCategoryNameResolved(category, name);
            return name;
        }

        public string GetAreaName(int areaId)
        {
            var e = GetArea(areaId);
            var name = e != null ? e.areaNameRaw : null;
            if (_host != null)
            {
                _host.OnAreaNameResolved(areaId, name, e != null);
                if (e != null)
                {
                    _host.ShowAreaUI(areaId, name, e.mapId);
                    _host.LogAreaEvent("area_named", areaId, name);
                    _host.PlayAreaSFX("open", areaId);
                    _host.SaveAreaState(areaId, e.category, e.mapId);
                }
            }
            return name;
        }
    }
}
