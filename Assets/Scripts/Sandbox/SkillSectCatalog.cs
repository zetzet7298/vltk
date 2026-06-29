// -----------------------------------------------------------------------------
// VLTK Mobile — ST-04.1 Skill Sect Catalog
// Phân loại skill theo 10 phái, skill tree, tier (passive/active/ultimate).
// Source: PcSkills.txt — tất cả skills cho 10 phái.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>Tier classification cho skill.</summary>
    public enum SkillTier
    {
        Passive,   // Bị động
        Active,    // Chủ động tấn công
        Ultimate,  // Chiêu cuối (60+ level)
        Buff,      // Hỗ trợ/buff
        Utility,   // Tiện ích
    }

    /// <summary>Thông tin một skill trong phái.</summary>
    public struct SectSkillEntry
    {
        public int skillId;
        public string nameVi;        // Tên tiếng Việt
        public string rawName;       // Tên gốc từ PcSkills.txt
        public SkillTier tier;
        public int reqLevel;         // Yêu cầu level
        public int maxLevel;         // Max skill level
        public int baseSkillId;      // Skill prerequisite
        public int childSkillId;     // Child skill chain
        public int charAnimId;       // PC CharAnimId
        public bool isMelee;
    }

    /// <summary>Thông tin một phái.</summary>
    public struct SectInfo
    {
        public int factionId;
        public string nameVi;
        public string elementDesc;   // Ngũ hành description
        public List<SectSkillEntry> skills;
    }

    /// <summary>
    /// Skill catalog phân loại theo 10 phái VLTK.
    /// Mỗi phái có skill tree với tier: Passive, Active, Ultimate, Buff, Utility.
    /// Dữ liệu từ PcSkills.txt, tất cả skills cho 10 phái.
    /// </summary>
    public static class SkillSectCatalog
    {
        private static Dictionary<int, SectInfo> _sects;

        public static Dictionary<int, SectInfo> AllSects => _sects ??= BuildAllSects();

        public static SectInfo GetSect(int factionId)
            => AllSects.TryGetValue(factionId, out var s) ? s : default;

        public static List<SectSkillEntry> GetSkills(int factionId)
            => AllSects.TryGetValue(factionId, out var s) ? s.skills : new List<SectSkillEntry>();

        private static Dictionary<int, SectInfo> BuildAllSects()
        {
            var d = new Dictionary<int, SectInfo>
            {
                [CombatFactionExt.ShaolinId]  = BuildShaolin(),
                [CombatFactionExt.TianWangId] = BuildTianWang(),
                [CombatFactionExt.TangMenId]  = BuildTangMen(),
                [CombatFactionExt.WuDuId]     = BuildWuDu(),
                [CombatFactionExt.CaiBangId]  = BuildCaiBang(),
                [CombatFactionExt.TianRenId]  = BuildTianRen(),
                [CombatFactionExt.EMeiId]     = BuildEMei(),
                [CombatFactionExt.CuiYanId]   = BuildCuiYan(),
                [CombatFactionExt.WuDangId]   = BuildWuDang(),
                [CombatFactionExt.KunLunId]   = BuildKunLun(),
            };
            return d;
        }

        // ── Thiếu Lâm (Shaolin) ────────────────────────────────────────────
        private static SectInfo BuildShaolin() => new()
        {
            factionId = CombatFactionExt.ShaolinId,
            nameVi = "Thiếu Lâm",
            elementDesc = "Kim hành — Ngoại công cận chiến",
            skills = new List<SectSkillEntry>
            {
                S(3, "Thiếu Lâm Kiếm Pháp",     "Thiếu Lâm Kiếm pháp",      SkillTier.Passive, 10, 20),
                S(4, "Thiếu Lâm Côn Pháp",       "Thiếu Lâm Côn pháp",       SkillTier.Passive, 10, 20),
                S(6, "Thiếu Lâm Đao Pháp",       "Thiếu Lâm Đao pháp",       SkillTier.Passive, 10, 20),
                S(8, "Thiếu Lâm Quyền Pháp",     "Thiếu Lâm Quyền Pháp",     SkillTier.Passive, 10, 20),
                S(9, "Hỗn Nguyên Nhất Khí Công", "Hỗn Nguyên Nhất Khí công", SkillTier.Passive, 10, 20),
                S(10, "Kim Cang Phục Ma",        "Kim Cang Phục Ma",         SkillTier.Active,  10, 20, charAnim: 8, melee: true),
                S(11, "Hoành Tảo Lục Hợp",       "Hoành Tảo Lục Hợp",       SkillTier.Active,  50, 20, charAnim: 7, melee: true),
                S(12, "Kim Cang Hộ Thể",         "Kim Cang Hộ Thể",          SkillTier.Passive, 20, 20),
                S(13, "Lập Địa Thành Phật",      "Lập Địa Thành Phật",       SkillTier.Buff,    30, 20, charAnim: 3),
                S(14, "Hàng Long Bất Vũ",        "Hàng Long Bất Vũ",         SkillTier.Active,  10, 20, charAnim: 7, melee: true),
                S(15, "Bất Động Minh Vương",     "Bất động Minh Vương",      SkillTier.Buff,    20, 20, charAnim: 3),
                S(16, "La Hán Trận",             "La Hán Trận",              SkillTier.Active,  30, 20, charAnim: 7),
                S(17, "Long Trảo Hổ Trảo",       "Long Trảo Hổ Trảo",        SkillTier.Active,  40, 20, charAnim: 8, melee: true),
                S(18, "Huệ Nhãn Chú",            "Huệ Nhãn chú",             SkillTier.Buff,    40, 20, charAnim: 3),
                S(19, "Ma Ha Vô Lượng",          "Ma Ha Vô Lượng",           SkillTier.Active,  50, 20, charAnim: 1, melee: true),
                S(20, "Sư Tử Hống",              "Sư Tử Hống",               SkillTier.Active,  40, 20, charAnim: 7),
                S(21, "Dịch Cân Kinh",           "Dịch Cân kinh",            SkillTier.Ultimate, 60, 20),
            },
        };

        // ── Thiên Vương (TianWang) ──────────────────────────────────────────
        private static SectInfo BuildTianWang() => new()
        {
            factionId = CombatFactionExt.TianWangId,
            nameVi = "Thiên Vương",
            elementDesc = "Kim hành — Ngoại công cận chiến (thương, đao, chùy)",
            skills = new List<SectSkillEntry>
            {
                S(23, "Thiên Vương Thương Pháp",  "Thiên Vương Thương pháp",  SkillTier.Passive, 10, 20),
                S(24, "Thiên Vương Đao Pháp",      "Thiên Vương Đao pháp",     SkillTier.Passive, 10, 20),
                S(26, "Thiên Vương Chùy Pháp",     "Thiên Vương Chùy Pháp",    SkillTier.Passive, 10, 20),
                S(29, "Trảm Long Quyết",           "Trảm Long quyết",          SkillTier.Active,  10, 20, charAnim: 12, melee: true),
                S(30, "Hồi Phong Lạc Nhạn",        "Hồi Phong Lạc Nhạn",       SkillTier.Active,  10, 20, charAnim: 12, melee: true),
                S(31, "Hàng Vân Quyết",            "Hàng Vân Quyết",           SkillTier.Active,  30, 20, charAnim: 12, melee: true),
                S(32, "Vô Tâm Trảm",               "Vô Tâm Trảm",              SkillTier.Active,  60, 20, charAnim: 12, melee: true),
                S(33, "Tĩnh Tâm Quyết",            "Tĩnh Tâm Quyết",           SkillTier.Buff,    20, 20),
                S(34, "Kinh Lôi Trảm",             "Kinh Lôi Trảm",            SkillTier.Active,  10, 20, charAnim: 12, melee: true),
                S(35, "Dương Quan Tam Điệp",       "Dương Quan Tam Điệp",      SkillTier.Active,  30, 20, charAnim: 12, melee: true),
                S(36, "Thiên Vương Chiến Ý",       "Thiên Vương Chiến ý",      SkillTier.Passive, 60, 30),
                S(37, "Bát Phong Trảm",            "Bát Phong Trảm",           SkillTier.Active,  30, 20, charAnim: 12, melee: true),
                S(38, "Bàn Cổ Cửu Thức",           "Bàn Cổ Cửu Thức",          SkillTier.Active,  40, 20, charAnim: 10, melee: true),
                S(39, "Triêm Y Thập Bát Điệt",     "Triêm Y Thập Bát Điệt",    SkillTier.Buff,    40, 20),
                S(40, "Đoạn Hồn Thích",            "Đoạn Hồn Thích",           SkillTier.Active,  40, 20, charAnim: 11, melee: true),
                S(41, "Huyết Chiến Bát Phương",    "Huyết Chiến Bát Phương",   SkillTier.Active,  60, 20, charAnim: 12, melee: true),
                S(42, "Kim Chung Tráo",            "Kim Chung Tráo",           SkillTier.Buff,    50, 20),
            },
        };

        // ── Đường Môn (TangMen) ─────────────────────────────────────────────
        private static SectInfo BuildTangMen() => new()
        {
            factionId = CombatFactionExt.TangMenId,
            nameVi = "Đường Môn",
            elementDesc = "Mộc hành — Ám khí, bẫy, phi tiêu",
            skills = new List<SectSkillEntry>
            {
                S(43, "Đường Môn Ám Khí",   "Đường Môn ám khí",    SkillTier.Passive, 10, 20),
                S(45, "Tích Lịch Đơn",       "Tích Lịch đơn",       SkillTier.Active,  10, 20, charAnim: 1),
                S(47, "Đoạt Hồn Tiêu",       "Đoạt Hồn Tiêu",       SkillTier.Active,  30, 20, charAnim: 1),
                S(48, "Tâm Nhãn",            "Tâm Nhãn",             SkillTier.Ultimate, 60, 30),
                S(50, "Truy Tâm Tiễn",       "Truy Tâm Tiễn",       SkillTier.Active,  30, 20, charAnim: 1),
                S(51, "Thanh Mộc",           "Thanh Mộc",            SkillTier.Passive, 30, 20),
                S(54, "Mạn Thiên Hoa Vũ",    "Mạn Thiên Hoa Vũ",    SkillTier.Active,  30, 20, charAnim: 6),
                S(55, "Thối Độc Thuật",      "Thối Độc Thuật",      SkillTier.Buff,    40, 20),
                S(57, "Băng Phách Hàn Quang", "Băng Phách Hàn Quang", SkillTier.Buff,   50, 20),
                S(58, "Thiên La Địa Võng",   "Thiên La Địa Võng",   SkillTier.Active,  60, 20, charAnim: 1),
            },
        };

        // ── Ngũ Độc (WuDu) ─────────────────────────────────────────────────
        private static SectInfo BuildWuDu() => new()
        {
            factionId = CombatFactionExt.WuDuId,
            nameVi = "Ngũ Độc",
            elementDesc = "Thổ hành — Độc sát, chú thuật",
            skills = new List<SectSkillEntry>
            {
                S(60, "Ngũ Độc Đao Pháp",    "Ngũ Độc Đao pháp",     SkillTier.Passive, 10, 20),
                S(62, "Ngũ Độc Chưởng Pháp",  "Ngũ Độc Chưởng Pháp",  SkillTier.Passive, 10, 20),
                S(63, "Độc Sa Chưởng",        "Độc Sa chưởng",        SkillTier.Active,  10, 20, charAnim: 1),
                S(64, "Băng Lam Huyền Tinh",  "Băng Lam Huyền Tinh",  SkillTier.Buff,    30, 20),
                S(65, "Huyết Đao Độc Sát",    "Huyết Đao Độc Sát",    SkillTier.Active,  10, 20, charAnim: 1),
                S(66, "Tạp Nan Dược Kinh",    "Tạp Nan Dược Kinh",    SkillTier.Passive, 20, 20),
                S(67, "Cửu Thiên Cuồng Lôi",  "Cửu Thiên Cuồng Lôi",  SkillTier.Buff,    10, 20),
                S(68, "U Minh Khô Lâu",       "U Minh Khô Lâu",       SkillTier.Active,  30, 20, charAnim: 1),
                S(69, "Vô Hình Độc",          "Vô Hình Độc",          SkillTier.Buff,    30, 20),
                S(70, "Xích Diệm Thực Thiên", "Xích Diệm Thực Thiên", SkillTier.Buff,    20, 20),
                S(71, "Thiên Cương Địa Sát",  "Thiên Cương Địa Sát",  SkillTier.Active,  60, 20, charAnim: 6),
                S(72, "Xuyên Tâm Độc Thích",  "Xuyên Tâm Độc Thích",  SkillTier.Buff,    50, 20),
                S(73, "Vạn Độc Thực Tâm",     "Vạn Độc Thực Tâm",     SkillTier.Buff,    40, 20),
                S(74, "Chu Cáp Thanh Minh",    "Chu Cáp Thanh Minh",   SkillTier.Active,  60, 20, charAnim: 1),
                S(75, "Ngũ Độc Kỳ Kinh",      "Ngũ Độc Kỳ Kinh",      SkillTier.Ultimate, 60, 30),
                S(76, "Di Hoa Tiếp Ngọc",     "Di Hoa Tiếp Ngọc",     SkillTier.Buff,    50, 20),
            },
        };

        // ── Cái Bang (CaiBang) ─────────────────────────────────────────────
        private static SectInfo BuildCaiBang() => new()
        {
            factionId = CombatFactionExt.CaiBangId,
            nameVi = "Cái Bang",
            elementDesc = "Hỏa hành — Bổng pháp, chưởng pháp",
            skills = new List<SectSkillEntry>
            {
                S(115, "Cái Bang Bổng Pháp",    "Cái Bang Bổng pháp",     SkillTier.Passive, 10, 20),
                S(116, "Cái Bang Chưởng Pháp",  "Cái Bang Chưởng Pháp",   SkillTier.Passive, 10, 20),
                S(117, "Đầu Thạch Vấn Lộ",      "Đầu Thạch Vấn Lộ",       SkillTier.Active,  10, 20, charAnim: 11),
                S(118, "Cô Mộc Độn Lôi",        "Cô Mộc Độn Lôi",         SkillTier.Buff,    10, 20),
                S(119, "Diên Môn Thác Bát",      "Diên Môn Thác Bát",      SkillTier.Active,  10, 20, charAnim: 11),
                S(120, "Bôn Lưu Đáo Hải",        "Bôn Lưu Đáo Hải",        SkillTier.Buff,    20, 20),
                S(121, "Diệu Thủ Không Không",   "Diệu Thủ Không Không",   SkillTier.Utility, 20, 20, charAnim: 14),
                S(122, "Kiến Nhân Thần Thủ",     "Kiến Nhân Thần Thủ",     SkillTier.Active,  10, 20, charAnim: 11),
                S(123, "Khuê Mộc Tinh Chiếu",    "Khuê Mộc Tinh Chiếu",    SkillTier.Buff,    30, 20),
                S(124, "Đả Cẩu bổng",            "Đả Cẩu Bổng Pháp",       SkillTier.Passive, 30, 20, charAnim: 11),
                S(125, "Thiên Hạ Vô Cẩu",        "Bổng Đả Ác Cẩu",        SkillTier.Active,  50, 20, charAnim: 11),
                S(126, "Kim Ô Ánh Tuyết",        "Kim Ô ánh Tuyết",        SkillTier.Buff,    40, 20),
                S(127, "Hoạt Bất Lưu Thủ",       "Hoạt Bất Lưu Thủ",       SkillTier.Passive, 10, 20, charAnim: 14),
                S(128, "Kháng Long Hữu Hối",     "Kháng Long Hữu Hối",     SkillTier.Active,  50, 20, charAnim: 11),
                S(129, "Hóa Hiểm Vi Di",         "Hóa Hiểm Vi Di",         SkillTier.Buff,    20, 20),
                S(130, "Túy Điệp Cuồng Vũ",      "Túy Điệp Cuồng Vũ",     SkillTier.Ultimate, 60, 30, charAnim: 43),
            },
        };

        // ── Thiên Nhẫn (TianRen) ───────────────────────────────────────────
        private static SectInfo BuildTianRen() => new()
        {
            factionId = CombatFactionExt.TianRenId,
            nameVi = "Thiên Nhẫn",
            elementDesc = "Hỏa hành — Hỏa sát, hút sinh lực/nội lực",
            skills = new List<SectSkillEntry>
            {
                S(131, "Thiên Nhẫn Đao Pháp",   "Thiên Nhẫn Đao pháp",    SkillTier.Passive, 10, 20),
                S(132, "Thiên Nhẫn Mâu Pháp",    "Thiên Nhẫn Mâu pháp",    SkillTier.Passive, 10, 20),
                S(135, "Tàn Dương Như Huyết",    "Tàn Dương Như Huyết",    SkillTier.Active,  10, 20, charAnim: 4),
                S(136, "Hỏa Liên Phần Hoa",      "Hỏa Liên Phần Hoa",      SkillTier.Buff,    10, 20),
                S(137, "Ảo Ảnh Phi Hồ",          "Ảo Ảnh Phi Hồ",          SkillTier.Buff,    20, 20),
                S(138, "Thôi Sơn Điền Hải",      "Thôi Sơn Điền Hải",      SkillTier.Active,  30, 20, charAnim: 0),
                S(139, "Hỗn Thủy Mạc Ngư",       "Hỗn Thủy Mạc Ngư",       SkillTier.Active,  20, 20, charAnim: 4, melee: true),
                S(140, "Phi Hồng Vô Tích",       "Phi Hồng Vô Tích",       SkillTier.Buff,    30, 20),
                S(141, "Liệt Hỏa Tình Thiên",    "Liệt Hỏa Tình Thiên",    SkillTier.Active,  30, 20, charAnim: 3),
                S(142, "Thâu Thiên Hoán Nhật",    "Thâu Thiên Hoán Nhật",   SkillTier.Active,  60, 20, charAnim: 4, melee: true),
                S(143, "Lịch Ma Đoạt Hồn",       "Lịch Ma Đoạt Hồn",       SkillTier.Buff,    50, 20),
                S(144, "Minh Tôn Bản Sinh",      "Minh Tôn Bản Sinh",      SkillTier.Passive, 30, 20),
                S(145, "Đơn Chỉ Liệt Diệm",      "Đơn Chỉ Liệt Diệm",     SkillTier.Active,  10, 20, charAnim: 4),
                S(146, "Ngũ Hành Trận",          "Ngũ Hành Trận",          SkillTier.Buff,    40, 20),
                S(147, "Huyền Minh Hấp Tinh",    "Huyền Minh Hấp Tinh",    SkillTier.Active,  40, 20, charAnim: 4, melee: true),
                S(148, "Ma Diệm Thất Sát",       "Ma Diệm Thất Sát",       SkillTier.Active,  60, 20, charAnim: 6),
                S(149, "Thực Cốt Huyết Nhận",    "Thực Cốt Huyết Nhận",    SkillTier.Buff,    50, 20),
                S(150, "Thiên Ma Giải Thể",      "Thiên Ma Giải Thể",      SkillTier.Ultimate, 60, 30),
            },
        };

        // ── Nga My (EMei) ──────────────────────────────────────────────────
        private static SectInfo BuildEMei() => new()
        {
            factionId = CombatFactionExt.EMeiId,
            nameVi = "Nga My",
            elementDesc = "Thủy hành — Băng sát, hồi phục, hỗ trợ",
            skills = new List<SectSkillEntry>
            {
                S(77, "Nga My Kiếm Pháp",        "Nga My Kiếm pháp",        SkillTier.Passive, 10, 20),
                S(79, "Nga My Chưởng Pháp",       "Nga My Chưởng pháp",      SkillTier.Passive, 10, 20),
                S(80, "Phiêu Tuyết Xuyên Vân",    "Phiêu Tuyết Xuyên Vân",   SkillTier.Active,  10, 20, charAnim: 2),
                S(81, "Thu Phong Diệp",           "Thu Phong Diệp",          SkillTier.Buff,    10, 20),
                S(82, "Tứ Tượng Đồng Quy",        "Tứ Tượng Đồng Quy",       SkillTier.Active,  30, 20, charAnim: 2),
                S(83, "Vọng Nguyệt",              "Vọng Nguyệt",             SkillTier.Buff,    20, 20),
                S(84, "Phong Vũ Phiêu Hương",     "Phong Vũ Phiêu Hương",    SkillTier.Buff,    20, 20),
                S(85, "Nhất Diệp Tri Thu",        "Nhất Diệp Tri Thu",       SkillTier.Active,  10, 20, charAnim: 2),
                S(86, "Lưu Thủy",                 "Lưu Thủy",                SkillTier.Buff,    40, 20),
                S(87, "Băng Tâm Quyết",           "Băng Tâm Quyết",          SkillTier.Passive, 30, 20),
                S(88, "Bất Diệt Bất Tuyệt",       "Bất Diệt Bất Tuyệt",     SkillTier.Active,  60, 20, charAnim: 2),
                S(89, "Mộng Điệp",                "Mộng Điệp",               SkillTier.Buff,    30, 20),
                S(90, "Mê Tung Ảo Ảnh",           "Mê Tung Ảo Ảnh",         SkillTier.Buff,    50, 20),
                S(91, "Phật Quang Phổ Chiếu",     "Phật Quang Phổ Chiếu",    SkillTier.Active,  60, 20, charAnim: 2),
                S(92, "Phật Tâm Từ Hữu",          "Phật Tâm Từ Hữu",        SkillTier.Buff,    50, 20),
                S(93, "Từ Hàng Phổ Độ",           "Từ Hàng Phổ Độ",         SkillTier.Buff,    20, 20),
            },
        };

        // ── Thúy Yên (CuiYan) ──────────────────────────────────────────────
        private static SectInfo BuildCuiYan() => new()
        {
            factionId = CombatFactionExt.CuiYanId,
            nameVi = "Thúy Yên",
            elementDesc = "Thủy hành — Băng sát, song đao, y thuật",
            skills = new List<SectSkillEntry>
            {
                S(95, "Thúy Yên Đao Pháp",        "Thúy Yên Đao pháp",       SkillTier.Passive, 10, 20),
                S(97, "Thúy Yên Song Đao",         "Thúy Yên Song đao",       SkillTier.Passive, 10, 20),
                S(99, "Phong Hoa Tuyết Nguyệt",    "Phong Hoa Tuyết Nguyệt",  SkillTier.Active,  10, 20, charAnim: 2),
                S(100, "Hộ Thể Hàn Băng",          "Hộ Thể Hàn Băng",         SkillTier.Buff,    40, 20),
                S(101, "Trị Liệu Thuật",           "Trị Liệu Thuật",          SkillTier.Buff,    10, 20),
                S(102, "Phong Quyển Tàn Tuyết",    "Phong Quyển Tàn Tuyết",   SkillTier.Active,  10, 20, charAnim: 2),
                S(103, "Thiên Lý Băng Phong",      "Thiên Lý Băng Phong",     SkillTier.Buff,    20, 20),
                S(104, "Băng Hồn",                 "Băng Hồn",                SkillTier.Passive, 20, 20),
                S(105, "Vũ Đả Lê Hoa",            "Vũ Đả Lê Hoa",            SkillTier.Active,  30, 20, charAnim: 2),
                S(107, "Nhiếp Tâm Thuật",          "Nhiếp Tâm Thuật",         SkillTier.Active,  30, 20, charAnim: 2),
                S(108, "Mục Dã Lưu Tinh",          "Mục Dã Lưu Tinh",         SkillTier.Active,  60, 20, charAnim: 2),
                S(109, "Tuyết Ảnh",                "Tuyết Ảnh",               SkillTier.Buff,    50, 20),
                S(110, "Ngũ Hành Độn",             "Ngũ Hành Độn",            SkillTier.Utility, 40, 20),
                S(111, "Bích Hải Triều Sinh",      "Bích Hải Triều Sinh",     SkillTier.Active,  60, 20, charAnim: 2),
                S(113, "Phù Vân Tán Tuyết",        "Phù Vân Tán Tuyết",       SkillTier.Active,  30, 20, charAnim: 2),
                S(114, "Băng Cốt Tuyết Tâm",       "Băng Cốt Tuyết Tâm",      SkillTier.Ultimate, 60, 30),
            },
        };

        // ── Võ Đang (WuDang) ──────────────────────────────────────────────
        private static SectInfo BuildWuDang() => new()
        {
            factionId = CombatFactionExt.WuDangId,
            nameVi = "Võ Đang",
            elementDesc = "Thổ hành — Kiếm pháp, lôi sát, nội công",
            skills = new List<SectSkillEntry>
            {
                S(151, "Võ Đang Kiếm Pháp",       "Võ Đang Kiếm pháp",       SkillTier.Passive, 10, 20),
                S(152, "Võ Đang Quyền Pháp",       "Võ Đang Quyền Pháp",      SkillTier.Passive, 10, 20),
                S(153, "Nộ Lôi Chỉ",               "Nộ Lôi Chỉ",              SkillTier.Active,  10, 20, charAnim: 5),
                S(154, "Âm Dương Khí",             "Âm Dương Khí",            SkillTier.Passive, 10, 20),
                S(155, "Thương Hải Minh Nguyệt",   "Thương Hải Minh Nguyệt",  SkillTier.Active,  10, 20, charAnim: 5),
                S(156, "Thuần Dương Tâm Pháp",     "Thuần Dương Tâm Pháp",    SkillTier.Passive, 20, 20),
                S(157, "Tọa Vọng Vô Ngã",          "Tọa Vọng Vô Ngã",         SkillTier.Buff,    50, 20),
                S(158, "Kiếm Phi Kinh Thiên",       "Kiếm Phi Kinh Thiên",     SkillTier.Active,  30, 20, charAnim: 5),
                S(159, "Thất Tinh Trận",            "Thất Tinh Trận",          SkillTier.Buff,    20, 20),
                S(160, "Thế Vân Tung",              "Thế Vân Tung",            SkillTier.Passive, 40, 20),
                S(161, "Lưỡng Nghi Tâm Pháp",       "Lưỡng Nghi Tâm Pháp",     SkillTier.Passive, 40, 20),
                S(162, "Huyền Nhất Vô Tượng",       "Huyền Nhất Vô Tượng",     SkillTier.Active,  50, 20),
                S(163, "Nhân Kiếm Hợp Nhất",       "Nhân Kiếm Hợp Nhất",     SkillTier.Active,  50, 20),
                S(164, "Bác Cấp Nhi Phục",          "Bác Cấp Nhi Phục",        SkillTier.Active,  50, 20),
                S(165, "Vô Ngã Vô Kiếm",            "Vô Ngã Vô Kiếm",          SkillTier.Active,  50, 20),
                S(166, "Thái Cực Thần Công",        "Thái Cực Thần Công",      SkillTier.Ultimate, 60, 20),
            },
        };

        // ── Côn Lôn (KunLun) ──────────────────────────────────────────────
        private static SectInfo BuildKunLun() => new()
        {
            factionId = CombatFactionExt.KunLunId,
            nameVi = "Côn Lôn",
            elementDesc = "Mộc hành — Phong/Lôi sát, kiếm pháp, phù chú",
            skills = new List<SectSkillEntry>
            {
                S(167, "Côn Lôn Đao Pháp",         "Côn Lôn Đao pháp",        SkillTier.Passive, 10, 20),
                S(168, "Côn Lôn Kiếm Pháp",         "Côn Lôn Kiếm pháp",       SkillTier.Passive, 10, 20),
                S(169, "Hô Phong Pháp",             "Hô Phong Pháp",           SkillTier.Active,  10, 20, charAnim: 1),
                S(170, "Đại Lãng Thực Không",       "Đại Lãng Thực Không",     SkillTier.Active,  10, 20),
                S(171, "Thanh Phong Phù",           "Thanh Phong Phù",         SkillTier.Buff,    10, 20),
                S(172, "Thiên Tế Tấn Lôi",          "Thiên Tế Tấn Lôi",        SkillTier.Active,  30, 20, charAnim: 1),
                S(173, "Thiên Thanh Địa Trọc",      "Thiên Thanh Địa Trọc",    SkillTier.Active,  30, 20),
                S(174, "Ký Bán Phù",                "Ký Bán Phù",              SkillTier.Buff,    20, 20),
                S(175, "Khi Hàn Ngạo Tuyết",        "Khi Hàn Ngạo Tuyết",      SkillTier.Active,  30, 20),
                S(176, "Cuồng Phong Sậu Điện",      "Cuồng Phong Sậu Điện",    SkillTier.Active,  30, 20),
                S(177, "Bách Xuyên Nạp Hải",        "Bách Xuyên Nạp Hải",      SkillTier.Active,  30, 20),
                S(178, "Nhất Khí Tam Thanh",         "Nhất Khí Tam Thanh",      SkillTier.Active,  30, 20, charAnim: 1),
                S(179, "Cuồng Lôi Chấn Địa",        "Cuồng Lôi Chấn Địa",      SkillTier.Active,  30, 20),
                S(180, "Độc Tê Tị Tà",              "Độc Tê Tị Tà",            SkillTier.Buff,    30, 20),
                S(181, "Khí Tâm Phù",                "Khí Tâm Phù",             SkillTier.Buff,    30, 20),
                S(182, "Ngũ Lôi Chánh Pháp",         "Ngũ Lôi Chánh Pháp",      SkillTier.Active,  60, 20),
                S(183, "Tuế Nguyệt Vô Tình Phù",    "Tuế Nguyệt Vô Tình Phù", SkillTier.Active,  60, 20),
                S(184, "Kim Thiền Thoát Xác",       "Kim Thiền Thoát Xác",     SkillTier.Ultimate, 60, 20),
            },
        };

        // ── Helper ─────────────────────────────────────────────────────────

        private static SectSkillEntry S(
            int skillId, string nameVi, string rawName, SkillTier tier,
            int reqLevel, int maxLevel,
            int baseSkillId = 0, int childSkillId = 0,
            int charAnim = 7, bool melee = false)
            => new()
            {
                skillId = skillId,
                nameVi = nameVi,
                rawName = rawName,
                tier = tier,
                reqLevel = reqLevel,
                maxLevel = maxLevel,
                baseSkillId = baseSkillId,
                childSkillId = childSkillId,
                charAnimId = charAnim,
                isMelee = melee,
            };
    }
}
