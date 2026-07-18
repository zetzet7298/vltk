// -----------------------------------------------------------------------------
// VLTK Mobile — ST-2 Faction Vietnamese catalog
// Bảng ánh xạ tên môn phái PC sang tiếng Việt.
// Reference: JX Online / Võ Lâm Truyền Kỳ 10 môn phái.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public static class FactionVietnameseCatalog
    {
        private static readonly Dictionary<int, string> _names = new()
        {
            { 0, "Thiếu Lâm" },
            { 1, "Thiên Vương" },
            { 2, "Đường Môn" },
            { 3, "Ngũ Độc" },
            { 4, "Nga My" },
            { 5, "Cái Bang" },
            { 6, "Võ Đang" },
            { 7, "Côn Lôn" },
            { 8, "Thiên Nhẫn" },
            { 9, "Thuý Yên" },
            { 10, "Thiên Sơn" },
            { 11, "Tiêu Dao" },
            { 12, "Bát Tự Môn" },
            { 13, "Minh Giáo" },
            { 14, "Đại Lý" },
            { 15, "Đông Bắc" },
        };

        public static string GetVietnameseName(int factionId)
        {
            return _names.TryGetValue(factionId, out var name) ? name : null;
        }

        public static IReadOnlyDictionary<int, string> GetAllMapped()
            => _names;
    }
}
