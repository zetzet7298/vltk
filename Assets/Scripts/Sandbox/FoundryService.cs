// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.8 Foundry Service (Luyện Đồ runtime)
// Quản lý công thức đúc trang bị: yêu cầu 3 loại nguyên liệu cho mỗi thành phẩm.
// PC source: settings/item/foundryresdemand.ini (361 công thức).
// Vietnamese: "Luyện Đồ", "Nguyên Liệu", "Công Thức Đúc".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>Service quản lý Luyện Đồ / Foundry (công thức đúc trang bị).</summary>
    public class FoundryService
    {
        public const string LogTag = "Foundry";

        private PcFoundryRegistry _registry;

        public event Action OnFoundryLoaded;

        public int Count => _registry != null ? _registry.Count : 0;

        public FoundryService() : this(null) { }

        public FoundryService(PcFoundryRegistry registry)
        {
            AttachRegistry(registry);
        }

        public void AttachRegistry(PcFoundryRegistry registry)
        {
            _registry = registry ?? new PcFoundryRegistry();
            SubsystemLog.Info(LogTag, $"Đã tải {_registry.Count} công thức luyện đồ");
            OnFoundryLoaded?.Invoke();
        }

        /// <summary>Tra cứu công thức đúc theo (itemGenre, itemDetail).</summary>
        public PcFoundryEntry GetRecipe(int genre, int detail)
            => _registry != null ? _registry.Get(genre, detail) : null;

        public IEnumerable<PcFoundryEntry> GetAllRecipes()
            => _registry != null ? _registry.All : (IEnumerable<PcFoundryEntry>)Array.Empty<PcFoundryEntry>();

        public static FoundryService LoadFromStreamingAssets(string subdir = "Reference/PcItemFull")
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var svc = new FoundryService();
            if (Directory.Exists(dir))
            {
                var reg = PcFoundryParser.BuildRegistry(dir);
                svc.AttachRegistry(reg);
            }
            else
            {
                SubsystemLog.Warn(LogTag, $"Foundry: directory không tồn tại {dir}");
                svc.OnFoundryLoaded?.Invoke();
            }
            return svc;
        }
    }
}
