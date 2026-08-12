// -----------------------------------------------------------------------------
// VLTK.Survivor — CaiBangActiveSkillSet (plan PORT_CAIBANG_SKILLS_SURVIVOR Gap A)
// Pool scope = đúng 4 skill active Cái Bang (KHÔNG cả phái — catalog Defs trả
// về toàn bộ skill Cái Bang gồm passive/support/NPC variant).
//  128 Kháng Long Hữu Hối (kanglong_youhui)  — tier 1
//  125 Bổng Đả Ác Cẩu   (bangda_egou)        — tier 1
//  1073 Thời Thặng Lục Long (zhanggaibang150) — mở khi 128 ≥ Lv5
//  1074 Bổng Huýnh Lược Địa (gungaibang150)   — mở khi 125 ≥ Lv5
// -----------------------------------------------------------------------------

namespace VLTK.Survivor
{
    /// <summary>Whitelist 4 skill active Cái Bang port vào Survivor pool.</summary>
    public static class CaiBangActiveSkillSet
    {
        public static readonly int[] ActiveSkillIds = { 128, 125, 1073, 1074 };

        public static bool IsActive(int id) => System.Array.IndexOf(ActiveSkillIds, id) >= 0;
    }
}
