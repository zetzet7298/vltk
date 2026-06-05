// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Title Effect (Hiệu Ứng Danh Hiệu)
// Bảng UI hiển thị các hiệu ứng của danh hiệu đang trang bị (tăng máu, công, thủ...).
// Vietnamese: "Hiệu Ứng Danh Hiệu", "Tăng máu", "Tăng công", "Đang kích hoạt".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct TitleEffectPanelRow
    {
        public readonly int effectId;
        public readonly int effectType;
        public readonly string effectName;
        public readonly int effectValue;
        public readonly bool isPercent;
        public readonly bool isActive;

        public TitleEffectPanelRow(int effectId, int effectType, string effectName, int effectValue, bool isPercent, bool isActive)
        {
            this.effectId = effectId;
            this.effectType = effectType;
            this.effectName = effectName ?? string.Empty;
            this.effectValue = effectValue;
            this.isPercent = isPercent;
            this.isActive = isActive;
        }
    }

    public sealed class TitleEffectPanelSnapshot
    {
        public int equippedTitleId;
        public string equippedTitleName;
        public int totalEffects;
        public IReadOnlyList<TitleEffectPanelRow> rows;
    }

    public static class TitleEffectPanelService
    {
        public const string LabelTitleEffect = "Hiệu Ứng Danh Hiệu";
        public const string LabelHpBoost = "Tăng máu";
        public const string LabelAtkBoost = "Tăng công";
        public const string LabelActive = "Đang kích hoạt";

        public static TitleEffectPanelSnapshot BuildSnapshot(TitleEffectService service, int equippedTitleId)
        {
            var snapshot = new TitleEffectPanelSnapshot
            {
                equippedTitleId = equippedTitleId,
                equippedTitleName = string.Empty,
                totalEffects = 0,
                rows = Array.Empty<TitleEffectPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetEffectsForTitle(equippedTitleId);
            var rows = new List<TitleEffectPanelRow>();
            foreach (var effect in all)
            {
                if (effect == null) continue;
                rows.Add(new TitleEffectPanelRow(
                    effect.effectId, effect.effectType, GetEffectTypeName(effect.effectType),
                    effect.effectValue, effect.isPercent, true));
            }
            snapshot.totalEffects = rows.Count;
            snapshot.equippedTitleName = service.GetTitleName(equippedTitleId);
            snapshot.rows = rows;
            return snapshot;
        }

        public static IReadOnlyList<TitleEffectPanelRow> GetByType(TitleEffectService service, int type)
        {
            if (service == null) return Array.Empty<TitleEffectPanelRow>();
            var rows = new List<TitleEffectPanelRow>();
            foreach (var effect in service.GetAllEffects())
            {
                if (effect == null) continue;
                if (effect.effectType == type)
                {
                    rows.Add(new TitleEffectPanelRow(
                        effect.effectId, effect.effectType, GetEffectTypeName(effect.effectType),
                        effect.effectValue, effect.isPercent, true));
                }
            }
            return rows;
        }

        public static int ComputeTotalEffect(TitleEffectService service, int titleId)
        {
            if (service == null || titleId <= 0) return 0;
            int total = 0;
            foreach (var effect in service.GetEffectsForTitle(titleId))
            {
                if (effect == null) continue;
                total += effect.effectValue;
            }
            return total;
        }

        public static string GetEffectTypeName(int type)
        {
            switch (type)
            {
                case 0: return "Tăng máu";
                case 1: return "Tăng nội lực";
                case 2: return "Tăng công";
                case 3: return "Tăng thủ";
                case 4: return "Tăng tốc đánh";
                case 5: return "Tăng chí mạng";
                case 6: return "Tăng chính xác";
                case 7: return "Tăng né tránh";
                case 8: return "Kháng băng";
                case 9: return "Kháng hỏa";
                case 10: return "Kháng độc";
                case 11: return "Kháng tâm";
                case 12: return "Hút máu";
                case 13: return "Hút nội";
                case 14: return "Giảm hồi chiêu";
                case 15: return "Tăng tầm đánh";
                default: return $"Hiệu ứng #{type}";
            }
        }
    }
}
