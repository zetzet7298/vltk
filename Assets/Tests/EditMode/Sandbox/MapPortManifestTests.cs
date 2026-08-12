using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MapPortManifestTests
    {
        [Test]
        public void PriorityMapIds_MatchPcMapAliasCatalogTruth()
        {
            Assert.AreEqual(1, MapPortManifest.PhuongTuongId, "PC maplist.ini: 1 = Phượng Tường / 凤翔");
            Assert.AreEqual(11, MapPortManifest.ThanhDoId, "PC maplist.ini: 11 = Thành Đô / 成都");
            Assert.AreEqual(20, MapPortManifest.GiangTanThonId, "PC maplist.ini: 20 = Giang Tân Thôn / 江津村");
            Assert.AreEqual(37, MapPortManifest.BienKinhId, "PC maplist.ini: 37 = Biện Kinh / 汴京");
            Assert.AreEqual(53, MapPortManifest.BaLangHuyenId, "PC maplist.ini: 53 = Ba Lăng huyện / 巴陵县");
            Assert.AreEqual(78, MapPortManifest.TuongDuongId, "PC maplist.ini: 78 = Tương Dương / 襄阳");
            Assert.AreEqual(162, MapPortManifest.DaiLyId, "PC maplist.ini: 162 = Đại Lý phủ / 大理府");
            Assert.AreEqual(176, MapPortManifest.LamAnId, "PC maplist.ini: 176 = Lâm An / 临安");
            Assert.AreEqual(235, MapPortManifest.DaoHoaDaoId, "PC maplist.ini: 235 = Đào Hoa đảo (1) / 桃花岛");
            Assert.AreEqual(907, MapPortManifest.VuotAiNhiepThiTranId);
        }

        [Test]
        public void PriorityMapNames_AreNotAssignedToWrongLegacyIds()
        {
            Assert.AreEqual("Phượng Tường", MapPortManifest.GetNameVi(1));
            Assert.AreEqual("Ba Lăng huyện", MapPortManifest.GetNameVi(53));
            Assert.AreEqual("Tương Dương", MapPortManifest.GetNameVi(78));
            Assert.AreEqual("Lâm An", MapPortManifest.GetNameVi(176));
            Assert.AreEqual("Map 79", MapPortManifest.GetNameVi(79), "79 is Mật đạo Nha môn Tương Dương in generated PC catalog, not Ba Lăng.");
            Assert.AreEqual("Map 0", MapPortManifest.GetNameVi(0), "No scoped PC maplist.ini entry contains 广州/Quảng Châu; do not bind it to Lâm An or a fake mapId 0.");
        }

        [Test]
        public void Entries_DoNotIncludeSyntheticGuangzhou()
        {
            Assert.IsFalse(MapPortManifest.Entries.ContainsKey(0));
            Assert.IsFalse(MapPortManifest.Entries.ContainsKey(176) && MapPortManifest.GetNameVi(176) == "Quảng Châu");
        }
    }
}
