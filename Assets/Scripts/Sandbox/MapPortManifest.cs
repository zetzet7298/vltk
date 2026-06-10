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
        public const int BaLangHuyenId = 53;
        public const int GiangTanThonId = 20;
        public const int DaoHoaDaoId = 235;
        public const int TuongDuongId = 78;
        public const int ThanhDoId = 11;
        public const int DaiLyId = 162;
        public const int BienKinhId = 37;
        public const int LamAnId = 176;
        public const int PhuongTuongId = 1;
        public const int TinSuVuotAiPhongKy120Id = 389;
        public const int VuotAiNhiepThiTranId = 907;
        public const int ThachThucThoiGianSoCap1Id = 464;

        public static readonly IReadOnlyDictionary<int, MapPortEntry> Entries = new Dictionary<int, MapPortEntry>
        {
            [BaLangHuyenId] = new(BaLangHuyenId, "Ba Lăng huyện", "巴陵县", MapPortStatus.Ported),
            [GiangTanThonId] = new(GiangTanThonId, "Giang Tân Thôn", "江津村", MapPortStatus.Ported),
            [DaoHoaDaoId] = new(DaoHoaDaoId, "Đào Hoa đảo (1)", "桃花岛", MapPortStatus.Ported),
            [TuongDuongId] = new(TuongDuongId, "Tương Dương", "襄阳", MapPortStatus.Ported),
            [ThanhDoId] = new(ThanhDoId, "Thành Đô", "成都", MapPortStatus.Ported),
            [DaiLyId] = new(DaiLyId, "Đại Lý phủ", "大理府", MapPortStatus.Ported),
            [BienKinhId] = new(BienKinhId, "Biện Kinh", "汴京", MapPortStatus.Ported),
            [LamAnId] = new(LamAnId, "Lâm An", "临安", MapPortStatus.Ported),
            [PhuongTuongId] = new(PhuongTuongId, "Phượng Tường", "凤翔", MapPortStatus.Ported),
            [TinSuVuotAiPhongKy120Id] = new(TinSuVuotAiPhongKy120Id, "Phong Kỳ (Vượt ải 120+)", "风之骑", MapPortStatus.Ported),
            [VuotAiNhiepThiTranId] = new(VuotAiNhiepThiTranId, "Vượt ải Nhiếp Thí Trần", "沙漠山洞1", MapPortStatus.Ported),
            [ThachThucThoiGianSoCap1Id] = new(ThachThucThoiGianSoCap1Id, "Thách thức thời gian (Sơ cấp 1)", "特殊用地\\杀手的试炼", MapPortStatus.Ported),
        };

        public static bool TryGet(int mapId, out MapPortEntry entry) => Entries.TryGetValue(mapId, out entry);

        public static string GetNameVi(int mapId) => Entries.TryGetValue(mapId, out var entry) ? entry.nameVi : $"Map {mapId}";
    }
}
