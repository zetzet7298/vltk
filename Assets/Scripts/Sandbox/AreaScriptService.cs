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

        public int Count => _reg?.Count ?? 0;

        public AreaScriptService() { }
        public AreaScriptService(PcAreaScriptRegistry reg) { _reg = reg; }

        public void RegisterRegistry(PcAreaScriptRegistry reg)
        {
            _reg = reg;
            if (_reg == null || _reg.Count == 0)
                SubsystemLog.Warn(LogTag, "Area script registry rỗng");
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

        public PcAreaScriptEntry GetArea(int id) => _reg != null ? _reg.Get(id) : null;
        public IReadOnlyList<PcAreaScriptEntry> GetByCategory(int category)
            => _reg != null ? _reg.GetByCategory(category) : System.Array.Empty<PcAreaScriptEntry>();
        public IReadOnlyList<PcAreaScriptEntry> GetByMap(int mapId)
            => _reg != null ? _reg.GetByMap(mapId) : System.Array.Empty<PcAreaScriptEntry>();
        public IReadOnlyList<PcAreaScriptEntry> GetAreasInCategory(int category) => GetByCategory(category);
        public int GetTotalScriptCount() => _reg != null ? _reg.GetTotalScriptCount() : 0;

        public string GetCategoryName(int category)
        {
            return category switch
            {
                0 => "Khu Vực Bản Đồ",
                1 => "Nhiệm Vụ Môn Phái",
                2 => "Thị Trấn",
                3 => "PvP",
                4 => "Thành Phố Lớn",
                _ => $"Khác ({category})",
            };
        }

        public string GetAreaName(int areaId)
        {
            var e = GetArea(areaId);
            return e != null ? e.areaNameRaw : null;
        }
    }
}
