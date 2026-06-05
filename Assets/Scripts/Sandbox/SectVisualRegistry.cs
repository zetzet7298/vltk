// -----------------------------------------------------------------------------
// VLTK Mobile — 10 Phái Combat Visual Registry
// Màu ngũ hành + hint SPR/effect cho combat visual parity.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using VLTK.Model;

namespace VLTK.Sandbox
{
    public enum SectType
    {
        ThieuLam = CombatFactionExt.ShaolinId,
        ThienVuong = CombatFactionExt.TianWangId,
        DuongMon = CombatFactionExt.TangMenId,
        NguDoc = CombatFactionExt.WuDuId,
        CaiBang = CombatFactionExt.CaiBangId,
        ThienNhan = CombatFactionExt.TianRenId,
        NgaMy = CombatFactionExt.EMeiId,
        ThuyYen = CombatFactionExt.CuiYanId,
        VoDang = CombatFactionExt.WuDangId,
        ConLuan = CombatFactionExt.KunLunId,
    }

    public readonly struct SectVisualConfig
    {
        public readonly SectType sect;
        public readonly string nameVi;
        public readonly string elementVi;
        public readonly Color skillColor;
        public readonly Color auraColor;
        public readonly string projectileSpriteHint;
        public readonly string auraSpriteHint;

        public SectVisualConfig(SectType sect, string nameVi, string elementVi, Color skillColor, Color auraColor, string projectileSpriteHint, string auraSpriteHint)
        {
            this.sect = sect;
            this.nameVi = nameVi;
            this.elementVi = elementVi;
            this.skillColor = skillColor;
            this.auraColor = auraColor;
            this.projectileSpriteHint = projectileSpriteHint;
            this.auraSpriteHint = auraSpriteHint;
        }
    }

    /// <summary>
    /// Registry visual cho 10 phái. Dùng làm lớp trung gian trước khi gắn SPR thật
    /// từ Skills.txt/Missles.txt, không tự bịa asset ngoài PC scope.
    /// </summary>
    public static class SectVisualRegistry
    {
        private static readonly Color Kim = new Color(1.00f, 0.86f, 0.30f, 1f);
        private static readonly Color Moc = new Color(0.20f, 0.85f, 0.30f, 1f);
        private static readonly Color Thuy = new Color(0.25f, 0.65f, 1.00f, 1f);
        private static readonly Color Hoa = new Color(1.00f, 0.25f, 0.15f, 1f);
        private static readonly Color Tho = new Color(0.78f, 0.55f, 0.28f, 1f);

        public static readonly IReadOnlyDictionary<SectType, SectVisualConfig> All = new Dictionary<SectType, SectVisualConfig>
        {
            [SectType.ThieuLam] = new(SectType.ThieuLam, "Thiếu Lâm", "Kim", Kim, Color.white, "shaolin_projectile", "shaolin_aura"),
            [SectType.ThienVuong] = new(SectType.ThienVuong, "Thiên Vương", "Kim", Kim, Color.white, "tianwang_projectile", "tianwang_aura"),
            [SectType.DuongMon] = new(SectType.DuongMon, "Đường Môn", "Mộc", Moc, Color.green, "tangmen_projectile", "tangmen_aura"),
            [SectType.NguDoc] = new(SectType.NguDoc, "Ngũ Độc", "Mộc", Moc, Color.green, "wudu_projectile", "wudu_aura"),
            [SectType.CaiBang] = new(SectType.CaiBang, "Cái Bang", "Hỏa", Hoa, Color.red, "gaibang_projectile", "gaibang_aura"),
            [SectType.ThienNhan] = new(SectType.ThienNhan, "Thiên Nhẫn", "Hỏa", Hoa, Color.red, "tianren_projectile", "tianren_aura"),
            [SectType.NgaMy] = new(SectType.NgaMy, "Nga My", "Thủy", Thuy, Color.cyan, "emei_projectile", "emei_aura"),
            [SectType.ThuyYen] = new(SectType.ThuyYen, "Thúy Yên", "Thủy", Thuy, Color.cyan, "cuiyan_projectile", "cuiyan_aura"),
            [SectType.VoDang] = new(SectType.VoDang, "Võ Đang", "Thổ", Tho, Color.yellow, "wudang_projectile", "wudang_aura"),
            [SectType.ConLuan] = new(SectType.ConLuan, "Côn Lôn", "Thổ", Tho, Color.yellow, "kunlun_projectile", "kunlun_aura"),
        };

        public static SectVisualConfig GetVisualConfig(SectType sect) => All.TryGetValue(sect, out var cfg) ? cfg : All[SectType.CaiBang];

        public static SectVisualConfig GetVisualConfig(int factionId) => GetVisualConfig((SectType)factionId);
    }
}
