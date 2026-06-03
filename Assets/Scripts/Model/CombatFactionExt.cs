// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.1 Combat Faction Extensions
// 10 phái VLTK. Extension methods cho CombatFaction enum.
// PcSkills.txt CharClass groups phái in pairs: 1=TL+TV, 2=NM+TY, 3=DM+ND, 4=CB+TN, 5=VD+CL
// Sub-faction determined by Lua script path in LvlSetScript column.
// Source: PcSkills.txt CharClass + LvlSetScript columns.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// Extension methods cho CombatFaction. Hỗ trợ 10 phái VLTK.
    /// PcSkills.txt CharClass column groups phái theo cặp:
    ///   CharClass 1 = Thiếu Lâm + Thiên Vương
    ///   CharClass 2 = Nga My + Thúy Yên
    ///   CharClass 3 = Đường Môn + Ngũ Độc
    ///   CharClass 4 = Cái Bang + Thiên Nhẫn
    ///   CharClass 5 = Võ Đang + Côn Lôn
    /// Sub-faction xác định bằng Lua script path trong LvlSetScript column.
    /// </summary>
    public static class CombatFactionExt
    {
        public const int NoneId     = 0;
        public const int ShaolinId  = 1;
        public const int TianWangId = 2;
        public const int TangMenId  = 3;
        public const int WuDuId     = 4;
        public const int CaiBangId  = 5;
        public const int TianRenId  = 6;
        public const int EMeiId     = 7;
        public const int CuiYanId   = 8;
        public const int WuDangId   = 9;
        public const int KunLunId   = 10;

        /// <summary>Map faction ID → PC CharClass value.</summary>
        public static int ToCharClass(this int factionId) => factionId switch
        {
            ShaolinId or TianWangId => 1,
            EMeiId or CuiYanId     => 2,
            TangMenId or WuDuId    => 3,
            CaiBangId or TianRenId => 4,
            WuDangId or KunLunId   => 5,
            _ => 0,
        };

        /// <summary>Tên phái tiếng Việt.</summary>
        public static string FactionViName(this int factionId) => factionId switch
        {
            ShaolinId  => "Thiếu Lâm",
            TianWangId => "Thiên Vương",
            TangMenId  => "Đường Môn",
            WuDuId     => "Ngũ Độc",
            CaiBangId  => "Cái Bang",
            TianRenId  => "Thiên Nhẫn",
            EMeiId     => "Nga My",
            CuiYanId   => "Thúy Yên",
            WuDangId   => "Võ Đang",
            KunLunId   => "Côn Lôn",
            _ => "Không",
        };

        /// <summary>
        /// Xác định faction từ Lua script path (PcSkills.txt LvlSetScript column).
        /// Ví dụ: \script\skill\shaolin.lua → ShaolinId
        /// </summary>
        public static int FactionFromLuaScript(string scriptPath)
        {
            if (string.IsNullOrEmpty(scriptPath)) return NoneId;
            var s = scriptPath.Replace('\\', '/').ToLowerInvariant();
            if (s.Contains("shaolin"))  return ShaolinId;
            if (s.Contains("tianwang")) return TianWangId;
            if (s.Contains("tangmen"))  return TangMenId;
            if (s.Contains("wudu"))     return WuDuId;
            if (s.Contains("gaibang"))  return CaiBangId;
            if (s.Contains("tianren"))  return TianRenId;
            if (s.Contains("emei"))     return EMeiId;
            if (s.Contains("cuiyan"))   return CuiYanId;
            if (s.Contains("wudang"))   return WuDangId;
            if (s.Contains("kunlun"))   return KunLunId;
            return NoneId;
        }

        /// <summary>Tất cả 10 phái.</summary>
        public static readonly int[] AllFactions =
        {
            ShaolinId, TianWangId, TangMenId, WuDuId, CaiBangId,
            TianRenId, EMeiId, CuiYanId, WuDangId, KunLunId,
        };

        /// <summary>CharClass → faction pairs.</summary>
        public static readonly Dictionary<int, int[]> FactionsByCharClass = new()
        {
            [1] = new[] { ShaolinId, TianWangId },
            [2] = new[] { EMeiId, CuiYanId },
            [3] = new[] { TangMenId, WuDuId },
            [4] = new[] { CaiBangId, TianRenId },
            [5] = new[] { WuDangId, KunLunId },
        };
    }
}
