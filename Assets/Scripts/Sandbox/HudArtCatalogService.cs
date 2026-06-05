// -----------------------------------------------------------------------------
// VLTK Mobile — ST-12.2 HUD Art Catalog Service (Bảng HUD Art)
// Wraps HudArtRegistry. PC source: settings/hudart.txt (1,851 SPR).
// Vietnamese: "HUD", "Nút Bấm", "Biểu Tượng", "Nền", "Thanh Tiến Trình", "Nhãn".
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Service runtime cho HUD art catalog: lookup, get path, load sprite.
    /// </summary>
    public class HudArtCatalogService
    {
        public const string LogTag = "HudArtCatalog";
        public const string DefaultStreamingDir = "UI/HUD/Art";

        private HudArtRegistry _registry;
        private string _root = "";

        public int Count => _registry != null ? _registry.Count : 0;

        public HudArtCatalogService() { }
        public HudArtCatalogService(HudArtRegistry reg) { _registry = reg; }

        public void AttachRegistry(HudArtRegistry reg, string root = "")
        {
            _registry = reg ?? new HudArtRegistry();
            _root = root ?? "";
            SubsystemLog.Info(LogTag, $"HudArtCatalog loaded: {Count} art (root={_root})");
        }

        public HudArtEntry GetArt(int artId)
            => _registry != null ? _registry.Get(artId) : null;

        public string GetArtPath(int artId)
        {
            var e = GetArt(artId);
            if (e == null || string.IsNullOrEmpty(e.path)) return string.Empty;
            if (string.IsNullOrEmpty(_root)) return e.path;
            // Kết hợp root với relative path (Windows path -> Unity path)
            string rel = e.path.Replace('\\', '/');
            return $"{_root.TrimEnd('/')}/{rel.TrimStart('/')}";
        }

        public IReadOnlyList<HudArtEntry> GetByType(int type)
            => _registry != null
                ? _registry.GetByType(type)
                : (IReadOnlyList<HudArtEntry>)System.Array.Empty<HudArtEntry>();

        public IReadOnlyList<HudArtEntry> All
            => _registry != null
                ? (IReadOnlyList<HudArtEntry>)new List<HudArtEntry>(_registry.All)
                : (IReadOnlyList<HudArtEntry>)System.Array.Empty<HudArtEntry>();

        /// <summary>
        /// Try load Sprite qua Resources.Load. Trả về null khi path sai.
        /// </summary>
        public Sprite TryLoadSprite(int artId)
        {
            string p = GetArtPath(artId);
            if (string.IsNullOrEmpty(p)) return null;
            try { return Resources.Load<Sprite>(p); }
            catch (System.Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"TryLoadSprite {artId} lỗi: {ex.Message}");
                return null;
            }
        }

        public static HudArtCatalogService LoadFromStreamingAssets(string subdir = null)
        {
            string dir = Path.Combine(
                Application.streamingAssetsPath,
                string.IsNullOrEmpty(subdir) ? DefaultStreamingDir : subdir);
            var svc = new HudArtCatalogService();
            if (Directory.Exists(dir))
            {
                var reg = PcHudArtCatalogParser.BuildRegistry(dir);
                svc.AttachRegistry(reg, root: dir);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"HudArtCatalog dir không tồn tại {dir}");
            }
            return svc;
        }
    }
}
