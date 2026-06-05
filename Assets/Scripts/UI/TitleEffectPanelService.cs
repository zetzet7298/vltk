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
            return new TitleEffectPanelSnapshot { rows = System.Array.Empty<TitleEffectPanelRow>() };
        }

        public static IReadOnlyList<TitleEffectPanelRow> GetByType(TitleEffectService service, int type)
        {
            return System.Array.Empty<TitleEffectPanelRow>();
        }

        public static int ComputeTotalEffect(TitleEffectService service, int titleId)
        {
            return 0;
        }

        public static string GetEffectTypeName(int type)
        {
            return string.Empty;
        }

    }
}
