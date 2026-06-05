// -----------------------------------------------------------------------------
// VLTK Mobile — ST-10.5 Title Effect runtime service
// Quản lý hiệu ứng (HP/MP/ATK/DEF/EXP/Gold/Reputation/Drop) theo danh hiệu.
// PC source: settings/titleeffect.txt.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public class TitleEffectService
    {
        public const string LogTag = "TitleEffect";

        private readonly PcTitleEffectRegistry _registry;
        public int Count => _registry?.Count ?? 0;

        public TitleEffectService() { }
        public TitleEffectService(PcTitleEffectRegistry registry) { _registry = registry ?? new PcTitleEffectRegistry(); }

        public static TitleEffectService LoadFromStreamingAssets()
        {
            string dir = Path.Combine(Application.streamingAssetsPath, "Reference/PcTitle");
            var reg = PcTitleEffectParser.BuildRegistry(dir);
            SubsystemLog.Info(LogTag, $"Đã tải {reg.Count} hiệu ứng danh hiệu");
            return new TitleEffectService(reg);
        }

        public PcTitleEffectEntry GetEffect(int effectId) => _registry != null ? _registry.Get(effectId) : null;
        public IReadOnlyList<PcTitleEffectEntry> GetByTitle(int titleId)
            => _registry != null ? _registry.GetByTitle(titleId) : Array.Empty<PcTitleEffectEntry>();
        public IReadOnlyList<PcTitleEffectEntry> GetByType(int effectType)
            => _registry != null ? _registry.GetByType(effectType) : Array.Empty<PcTitleEffectEntry>();

        public int ComputeTotalEffect(int titleId, int playerLevel)
        {
            if (_registry == null) return 0;
            int total = 0;
            foreach (var e in _registry.GetByTitle(titleId))
                if (playerLevel >= e.requiredTitleLevel)
                    total += e.effectValue;
            return total;
        }

        public int ComputeTotalAtkBonus(int titleId)
        {
            if (_registry == null) return 0;
            int total = 0;
            foreach (var e in _registry.GetByTitle(titleId))
                if (e.effectType == 2) total += e.effectValue;
            return total;
        }

        public int ComputeTotalHpBonus(int titleId)
        {
            if (_registry == null) return 0;
            int total = 0;
            foreach (var e in _registry.GetByTitle(titleId))
                if (e.effectType == 0) total += e.effectValue;
            return total;
        }

        public string GetEffectTypeName(int effectType)
        {
            switch (effectType)
            {
                case 0: return "Sinh lực";
                case 1: return "Nội lực";
                case 2: return "Công kích";
                case 3: return "Phòng thủ";
                case 4: return "Kinh nghiệm";
                case 5: return "Vàng";
                case 6: return "Danh tiếng";
                case 7: return "Tỉ lệ rơi đồ";
                default: return $"Loại {effectType}";
            }
        }
    }
}
