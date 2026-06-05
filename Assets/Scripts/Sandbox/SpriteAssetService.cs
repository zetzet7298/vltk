// -----------------------------------------------------------------------------
// VLTK Mobile — ST-12.2 Sprite Asset runtime service
// Quản lý sprite asset catalog (player, NPC, item, effect, UI, map).
// PC source: settings/spriteasset.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class SpriteAssetService
    {
        public const string DefaultStreamingDir = "Reference/PcSprite";
        public const string LogTag = "SpriteAsset";

        private readonly PcSpriteAssetRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public SpriteAssetService() { }
        public SpriteAssetService(PcSpriteAssetRegistry registry) { _registry = registry ?? new PcSpriteAssetRegistry(); }

        public static SpriteAssetService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, DefaultStreamingDir);
            var reg = PcSpriteAssetParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} sprite asset");
            return new SpriteAssetService(reg);
        }

        public PcSpriteAssetEntry GetSprite(int spriteId) => _registry != null ? _registry.Get(spriteId) : null;

        public IReadOnlyList<PcSpriteAssetEntry> GetByCategory(int category)
            => _registry != null ? _registry.GetByCategory(category) : Array.Empty<PcSpriteAssetEntry>();

        public string GetSpritePath(int spriteId)
        {
            var e = GetSprite(spriteId);
            return e?.path ?? string.Empty;
        }

        public Sprite TryLoadSprite(int spriteId)
        {
            string p = GetSpritePath(spriteId);
            if (string.IsNullOrEmpty(p)) return null;
            try
            {
                // strip extension, use Resources.Load convention
                string resName = p;
                if (resName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    resName = resName.Substring(0, resName.Length - 4);
                else if (resName.EndsWith(".spr", StringComparison.OrdinalIgnoreCase))
                    resName = resName.Substring(0, resName.Length - 4);
                return Resources.Load<Sprite>(resName);
            }
            catch (Exception ex)
            {
                SubsystemLog.Warn(LogTag, $"TryLoadSprite({spriteId}) failed: {ex.Message}");
                return null;
            }
        }

        public string GetCategoryName(int category)
        {
            switch (category)
            {
                case 0: return "Nhân vật";
                case 1: return "NPC";
                case 2: return "Vật phẩm";
                case 3: return "Hiệu ứng";
                case 4: return "Giao diện";
                case 5: return "Bản đồ";
                default: return $"Loại {category}";
            }
        }
    }
}
