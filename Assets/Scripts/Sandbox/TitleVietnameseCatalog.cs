// -----------------------------------------------------------------------------
// VLTK Mobile — Catalog ánh xạ ID danh hiệu PC sang tên tiếng Việt
// Vietnamese names cho PlayerTitle, FactionTitle, WorldRank titles.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Ánh xạ ID danh hiệu → tên tiếng Việt theo chuẩn VLTK Mobile.
    /// </summary>
    public static class TitleVietnameseCatalog
    {
        private static readonly Dictionary<int, string> _names = new()
        {
            // Rank cơ bản
            { 1, "Tân Thủ" },
            { 2, "Cao Thủ" },
            { 3, "Võ Lâm Tân Nhân" },
            { 4, "Hiệp Khách" },
            { 5, "Đại Hiệp" },
            { 6, "Trung Nguyên Đệ Nhất Nhân" },
            { 7, "Thiên Hạ Đệ Nhất" },
            { 8, "Võ Lâm Chí Tôn" },
            { 9, "Bắc Đẩu Kiếm Khách" },
            { 10, "Nam Cương Đao Khách" },

            // Phái
            { 11, "Thiếu Lâm Tăng Nhân" },
            { 12, "Thiên Vương Hộ Pháp" },
            { 13, "Đường Môn Thích Khách" },
            { 14, "Ngũ Độc Tông Sư" },
            { 15, "Nga My Ni Cô" },
            { 16, "Cái Bang Bang Chủ" },
            { 17, "Võ Đang Chưởng Môn" },
            { 18, "Côn Lôn Tiên Tử" },
            { 19, "Thiên Nhẫn Sát Thủ" },
            { 20, "Thuý Yên Thanh Tịnh" },

            // Đặc biệt theo cấp
            { 21, "Võ Lâm Nhất Phái" },
            { 22, "Giang Hồ Hảo Hán" },
            { 23, "Hiệp Nghĩa Song Thân" },
            { 24, "Bạch Y Hiệp Nữ" },
            { 25, "Hắc Y Ma Đầu" },
            { 26, "Tẩy Tủy Kinh Nhân" },
            { 27, "Lục Khí Tông Sư" },
            { 28, "Bát Hoang Kiếm Thánh" },
            { 29, "Cửu Dương Thần Công" },
            { 30, "Thập Phương Ma Tổ" },

            // Bang hội
            { 31, "Bang Chủ Vạn Lý" },
            { 32, "Bang Chủ Cửu Thiên" },
            { 33, "Hộ Pháp Kim Đao" },
            { 34, "Hộ Pháp Ngân Thương" },
            { 35, "Tả Hộ Pháp" },
            { 36, "Hữu Hộ Pháp" },
            { 37, "Trưởng Lão Thứ Nhất" },
            { 38, "Trưởng Lão Thứ Nhị" },
            { 39, "Đường Chủ Đại La" },
            { 40, "Đường Chủ Thanh Vân" },

            // PK
            { 41, "Sát Phạt Tinh Hà" },
            { 42, "Giết Người Như Ngóe" },
            { 43, "Ma Đầu Lục Tục" },
            { 44, "Thiên Sát Tinh Quân" },
            { 45, "Tẩu Hỏa Nhập Ma" },
            { 46, "Huyết Hải Thâm Thù" },
            { 47, "Oan Gia Ngõ Hẹp" },
            { 48, "Tử Địch Cửu Tinh" },
            { 49, "Độc Cô Cầu Bại" },
            { 50, "Kiếm Thần Bất Bại" },

            // Thành tựu
            { 51, "Đệ Nhất Bang" },
            { 52, "Võ Lâm Tân Tú" },
            { 53, "Siêu Phàm Thoát Tục" },
            { 54, "Thần Thoại Truyền Thuyết" },
            { 55, "Thiên Hạ Vô Song" },
            { 56, "Trường Sinh Bất Lão" },
            { 57, "Độ Kiếp Thành Tiên" },
            { 58, "Cửu Thiên Tiên Tôn" },
            { 59, "Đại La Kim Tiên" },
            { 60, "Hỗn Nguyên Thánh Tổ" },
        };

        public static string GetVietnameseName(int titleId)
        {
            return _names.TryGetValue(titleId, out var n) ? n : null;
        }

        public static IReadOnlyDictionary<int, string> GetAllMapped() => _names;

        public static int Count => _names.Count;
    }
}
