// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: Title Effect (Hiệu Ứng Danh Hiệu)
// Bảng UI hiển thị các hiệu ứng của danh hiệu đang trang bị (tăng máu/công/thủ...).
// PC source: settings/titleeffect.txt → TitleEffectService (effectType 0-7,
// effectValue, requiredTitleLevel). Panel chỉ surface dữ liệu service thật;
// service==null → snapshot rỗng (null-safe, không throw).
// Vietnamese: "Hiệu Ứng Danh Hiệu", "Sinh lực", "Công kích", "Đang kích hoạt".
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
        public string equippedTitleName = string.Empty;
        public int totalEffects;
        public IReadOnlyList<TitleEffectPanelRow> rows = System.Array.Empty<TitleEffectPanelRow>();
    }

    public static class TitleEffectPanelService
    {
        public const string LabelTitleEffect = "Hiệu Ứng Danh Hiệu";
        public const string LabelHpBoost = "Tăng máu";
        public const string LabelAtkBoost = "Tăng công";
        public const string LabelActive = "Đang kích hoạt";

        // PC titleeffect.txt effectType: 0=hp,1=mp,2=atk,3=def,4=exp,5=gold,6=reputation,7=drop.
        // exp/gold/drop là hiệu ứng theo %, các loại còn lại là giá trị tuyệt đối.
        public const int EffectHp = 0;
        public const int EffectMp = 1;
        public const int EffectAtk = 2;
        public const int EffectDef = 3;
        public const int EffectExp = 4;
        public const int EffectGold = 5;
        public const int EffectReputation = 6;
        public const int EffectDrop = 7;

        /// <summary>
        /// Dựng snapshot toàn bộ hiệu ứng của danh hiệu đang trang bị (equippedTitleId).
        /// service==null → snapshot rỗng. Khi equippedTitleId &lt;= 0 cũng trả rỗng.
        /// </summary>
        public static TitleEffectPanelSnapshot BuildSnapshot(TitleEffectService service, int equippedTitleId)
        {
            var snap = new TitleEffectPanelSnapshot { equippedTitleId = equippedTitleId };
            if (service == null || equippedTitleId <= 0) return snap;

            var effects = service.GetByTitle(equippedTitleId);
            var rows = new List<TitleEffectPanelRow>(effects.Count);
            foreach (var e in effects)
            {
                if (e == null) continue;
                rows.Add(new TitleEffectPanelRow(
                    e.effectId,
                    e.effectType,
                    service.GetEffectTypeName(e.effectType),
                    e.effectValue,
                    IsPercentEffect(e.effectType),
                    isActive: true)); // danh hiệu đang trang bị → hiệu ứng đang kích hoạt.
            }

            snap.rows = rows;
            snap.totalEffects = rows.Count;
            return snap;
        }

        /// <summary>Lọc hiệu ứng theo loại (effectType). service==null → rỗng.</summary>
        public static IReadOnlyList<TitleEffectPanelRow> GetByType(TitleEffectService service, int type)
        {
            if (service == null) return System.Array.Empty<TitleEffectPanelRow>();
            var src = service.GetByType(type);
            var rows = new List<TitleEffectPanelRow>(src.Count);
            foreach (var e in src)
            {
                if (e == null) continue;
                rows.Add(new TitleEffectPanelRow(
                    e.effectId, e.effectType, service.GetEffectTypeName(e.effectType),
                    e.effectValue, IsPercentEffect(e.effectType), isActive: true));
            }
            return rows;
        }

        /// <summary>Tổng giá trị hiệu ứng của một danh hiệu (đủ cấp). service==null → 0.</summary>
        public static int ComputeTotalEffect(TitleEffectService service, int titleId)
        {
            if (service == null) return 0;
            // playerLevel cao để gộp toàn bộ hiệu ứng đã đủ requiredTitleLevel.
            return service.ComputeTotalEffect(titleId, int.MaxValue);
        }

        public static string GetEffectTypeName(int type)
        {
            switch (type)
            {
                case EffectHp: return "Sinh lực";
                case EffectMp: return "Nội lực";
                case EffectAtk: return "Công kích";
                case EffectDef: return "Phòng thủ";
                case EffectExp: return "Kinh nghiệm";
                case EffectGold: return "Vàng";
                case EffectReputation: return "Danh tiếng";
                case EffectDrop: return "Tỉ lệ rơi đồ";
                default: return $"Loại {type}";
            }
        }

        private static bool IsPercentEffect(int effectType)
            => effectType == EffectExp || effectType == EffectGold || effectType == EffectDrop;
    }
}
