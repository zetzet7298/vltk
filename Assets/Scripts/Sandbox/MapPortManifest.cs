// -----------------------------------------------------------------------------
// VLTK Mobile — PC Map Port Manifest
// Việt hoá tên map và theo dõi trạng thái port từ Region_S/Region_C PC.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public enum MapPortStatus
    {
        NotPorted,
        InProgress,
        Ported,
    }

    public readonly struct MapPortEntry
    {
        public readonly int mapId;
        public readonly string nameVi;
        public readonly string pcNameHint;
        public readonly MapPortStatus status;

        public MapPortEntry(int mapId, string nameVi, string pcNameHint, MapPortStatus status)
        {
            this.mapId = mapId;
            this.nameVi = nameVi;
            this.pcNameHint = pcNameHint;
            this.status = status;
        }
    }

    /// <summary>Danh mục map ưu tiên port từ PC sang Unity mobile.</summary>
    public static class MapPortManifest
    {
        public const int BaLangHuyenId = 1;
        public const int GiangTanThonId = 2;
        public const int DaoHoaDaoId = 3;
        public const int TuongDuongId = 11;
        public const int ThanhDoId = 37;
        public const int DaiLyId = 80;
        public const int BienKinhId = 78;
        public const int LamAnId = 103;
        public const int QuangChauId = 176;
        public const int PhuongTuongId = 121;

        public static readonly IReadOnlyDictionary<int, MapPortEntry> Entries = new Dictionary<int, MapPortEntry>
        {
            [BaLangHuyenId] = new(BaLangHuyenId, "Ba Lăng Huyện", "巴陵县", MapPortStatus.Ported),
            [GiangTanThonId] = new(GiangTanThonId, "Giang Tân Thôn", "江津村", MapPortStatus.NotPorted),
            [DaoHoaDaoId] = new(DaoHoaDaoId, "Đào Hoa Đảo", "桃花岛", MapPortStatus.NotPorted),
            [TuongDuongId] = new(TuongDuongId, "Tương Dương", "襄阳", MapPortStatus.NotPorted),
            [ThanhDoId] = new(ThanhDoId, "Thành Đô", "成都", MapPortStatus.NotPorted),
            [DaiLyId] = new(DaiLyId, "Đại Lý", "大理", MapPortStatus.NotPorted),
            [BienKinhId] = new(BienKinhId, "Biện Kinh", "汴京", MapPortStatus.NotPorted),
            [LamAnId] = new(LamAnId, "Lâm An", "临安", MapPortStatus.NotPorted),
            [QuangChauId] = new(QuangChauId, "Quảng Châu", "广州", MapPortStatus.NotPorted),
            [PhuongTuongId] = new(PhuongTuongId, "Phượng Tường", "凤翔", MapPortStatus.NotPorted),
        };

        public static bool TryGet(int mapId, out MapPortEntry entry) => Entries.TryGetValue(mapId, out entry);

        public static string GetNameVi(int mapId) => Entries.TryGetValue(mapId, out var entry) ? entry.nameVi : $"Map {mapId}";
    }
}
