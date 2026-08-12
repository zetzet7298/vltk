Include("\\script\\global\\mel\\configserver.lua")
----------------------------------------------------------------------------------------------------
--                                  TÝnh N¨ng Kh¸c - Kh«ng Sö Dông                                --
----------------------------------------------------------------------------------------------------
-- C«ng Thµnh ChiÕn 3 Trô
NPCCongThanhQuan3Tru = 0 -- 0: §ãng, 1: Më
-- ThÊt Thµnh §¹i ChiÕn
ThatThanhDaiChien = 0 -- 0: §ãng, 1: Më
SoTienBaoDanhCanNop = 10000000
SoKhieuChienLenhCanNop = 500

----------------------------------------------------------------------------------------------------
-- TÝn Sø
HoatDongTinSu = 0 -- 0: §ãng, 1: Më
TinSuPhongKy = 0 -- Phong Kú: BiÖn Kinh - D­¬ng Ch©u
TinSuThienBaoKho = 0 -- Thiªn B¶o Khè: Thµnh §« - T­¬ng D­¬ng
TinSuSonThanMieu = 0 -- S¬n ThÇn MiÕu: L©m An - Ph­îng T­êng
Mo3LoaiTinSuNhuVNG = 0 -- §¹i Lý random 3 map

----------------------------------------------------------------------------------------------------
BauCua = 0 -- 0: §ãng, 1: Më - Trung T©m T­¬ng D­¬ng

----------------------------------------------------------------------------------------------------
-- Tèng Kim
VongSangTopTongKim9h = 0
ThuongTopTongKimTuDong9h = 0
Nhan_2_Diem_TongKim_9H = 0

----------------------------------------------------------------------------------------------------
NPCVoLamLienDau = 0 -- 0: §ãng, 1: Më
LoanChienCuuChauCoc = 0 -- 0: §ãng, 1: Më - TÞa NPC Ch­ëng §¨ng Cung N÷
HoatDongDauNguu = 0 -- 0: §ãng, 1: Më - TÞa NPC Ch­ëng §¨ng Cung N÷
CauCa = 1 -- 0: §ãng, 1: Më - T¹i ThuyÒn Phu C¸c thµnh

----------------------------------------------------------------------------------------------------
-- NPC TiÒn Trang
PhuongThucDoi = 0 -- 0: §ãng, 1: KNB, 2: TiÒn §ång
TyLeNapThe = 100 --VD 50.000VND ®®­îc 500xu hoÆc 50.000 ®­îc 500 KNB
TyLeDoiKnbSangTienDong = 50 -- 1 KNB = 50 TiÒn §ång (Më nÕu bËc ph­¬ng thøc KNB)

----------------------------------------------------------------------------------------------------
-- HiÖu Thuèc
TatNPCHieuThuocAllThanh = 0 -- 0: §ãng, 1: Më
ScriptMuaThuoc = 0

----------------------------------------------------------------------------------------------------
--T¹p Hãa
TatNPCTapHoaAllThanh = 0 -- 0: §ãng, 1: Më
ScriptMuaTBTapHoa = 0

----------------------------------------------------------------------------------------------------
--Thî RÌn
TatNPCThoRenAllThanh = 0 -- 0: §ãng, 1: Më
ScriptMuaTBThoRen = 0

----------------------------------------------------------------------------------------------------
--B¸n Ngùa
TatNPCBanNguaAllThanh = 0 -- 0: §ãng, 1: Më
ScriptBanNgua = 0

----------------------------------------------------------------------------------------------------
ThoiGianBaoTriServer = 30 -- TÝnh b»ng gi©y
UongChaoLapBat = 0 -- 0: §ãng, 1: Më
AllowUyThacBachCauHoan = 0 -- 0: §ãng, 1: Më
ThayDoiNgoaiTrang = 0 -- 0: §ãng, 1: Më - Thay §æi Ngo¹i Trang trung t©m T­¬ng D­¬ng
DoiTenNhanVat = 0 -- 0: §ãng, 1: Më - Vâ L©m Minh Chñ L©m An
GiftCode = 0 -- 0: §ãng, 1: Më - Trung T©m Ba L¨ng HuyÖn
KhoaChucNangGiaoDich = 0 -- 0: §ãng, 1: Më
KhoaChucNangBayBan = 0 -- 0: §ãng, 1: Më

----------------------------------------------------------------------------------------------------
-- Thêi Gian Open Server
ThoiGianOpenServer = 202006111700 -- N¨m/Th¸ng/Ngµy/Giê/Phót
ThoiGianOpenServerText = "Thêi gian open server lµ 17h, xin h·y quay lai sau" -- Söa l¹i c©u tho¹i cho phï hîp
ThoiGianNhanThuongDuaTop = 202007050000 -- NhËn trong NPC LÔ Quan
ThoiGianKetThucNhanThuongDuaTop = 202007110000

----------------------------------------------------------------------------------------------------
-- BËt TÝnh N¨ng Test Game
HoTroTestGame = 0 -- 0: §ãng, 1: Më

----------------------------------------------------------------------------------------------------
--C¸c TÝnh N¨ng Hæ Trî T©n Thñ
NhanHoTroKyNang1xDen6x = 0 -- 0: §ãng, 1: Më
VongSangHoTroTanThu = 0 -- 0: §ãng, 1: Më
GioiHanCapNhanHoTroVongSang = 80
TocDoHoiPhucManaVongSangHoTro = 100
TocDoHoiPhucMauVongSangHoTro = 100

----------------------------------------------------------------------------------------------------
-- ChØnh Server Theo D¹ng NhËn §å Free PK
ChinhServerPkNhanFullDoVaCap = 0 -- 0: §ãng, 1: Më
--Th«ng tin: 
-- + NhËn 1 bé trang bÞ hoµng kim m«n ph¸i tïy chän khãa, chØ chän ®­îc 1 lÇn
-- + NhËn c¸c lo¹i ®iÓm (trõ tiÒn ®ång vµ KNB), thó c­ëi, ®æi mµu, thµnh lËp Bang, trang bÞ xanh, tÝm.
-- + NPC chøc n¨ng tËp trung ë Trung T©m T­¬ng D­¬ng
-- + §æi Vò KhÝ Xanh Trung T©m T­¬ng D­¬ng
-- NPC
ChuyenDoiTrangBiHoangKim = 0 -- 0: §ãng, 1: Më
TienDongChuyenTrangBi = 500
DoiVatPham = 0 -- 0: §ãng, 1: Më - §æi nguyªn liÖu c¸c ho¹t ®éng
BanItemHoTro = 0 -- 0: §ãng, 1: Më - NPC b¸n c¸c vËt phÈm hæ trî nh­ Thuèc lag Tèng Kim,..
DoiVuKhiXanh = 0 -- 0: §ãng, 1: Më - §æi Vò KhÝ Xanh, cßn lçi, test l¹i sau

----------------------------------------------------------------------------------------------------
-- Kú Tr©n C¸c
DiemNapTheSuDungKTC = 0 --B»ng víi tû lÖ n¹p xu VD 50.000VND ®­îc 500 ®iÓm, nÕu kh«ng ®ñ sè ®iÓm sÏ kh«ng sö dông ®­îc Kú Tr©n C¸c (NÕu kh«ng sö dông tÝnh n¨ng nµy th× cho b»ng 0)

----------------------------------------------------------------------------------------------------
-- Shop Tèng Kim
ShopTongKim = 0 -- 0: §ãng, 1: Më
ScriptShopTongKim = 0
-- Shop Liªn §Êu
ShopLienDau = 0 -- 0: §ãng, 1: Më
ScriptShopLienDau = 0
-- Shop ThÇn BÝ Th­¬ng Nh©n
OpenShopThanBiThuongNhan = 0 -- 0: §ãng, 1: Më

----------------------------------------------------------------------------------------------------
-- Di ChuyÓn
KiemTraCapDoTrainMapVuotCap = 0 -- 0: §ãng, 1: Më
DiDenNgonNuiTruongBachThanBiThuongNhan = 0 -- 0: §ãng, 1: Më
ChienLongDong = 0 -- 0: §ãng, 1: Më
DiViSonDao = 0 -- 0: §ãng, 1: Më

----------------------------------------------------------------------------------------------------
RotDoTimViSonDaoVaMacBac = 0 -- 0: §ãng, 1: Më
TyLeRotTrangBiTim = 0 -- ChØnh cµng cao cµng rít nhiÒu
EpTrangBiBachKim = 0 -- 0: §ãng, 1: Më
NangCapNgua = 0 -- 0: §ãng, 1: Më
----------------------------------------------------------------------------------------------------
VoDanhTangHocSkill150 = 0

----------------------------------------------------------------------------------------------------
--                                     Sù KiÖn Tù §éng 12 Th¸ng                                   --
----------------------------------------------------------------------------------------------------
-- T¾t më Sù KiÖn Tù §éng
EventTuDong = 0 -- 0: §ãng, 1: Më
-- Giíi h¹n sö dông sù KiÖn lo¹i th­êng vµ ®Æc biÖt
nGioiHanEventThuong = 1000
nGioiHanEventDacBiet = 2000
-- Giíi h¹n sù kiÖn mèc 1 2 3
nGioiHanMoc1 = 1000
nGioiHanMoc2 = 1500
nGioiHanMoc3 = 2000

----------------------------------------------------------------------------------------------------
--                             Tû LÖ GhÐp Sù KiÖn (100 t­¬ng ®­¬ng 100%)						  --
----------------------------------------------------------------------------------------------------
-- Th¸ng 2 -- GhÐp Ph¸o
TyLePhaoTrungPhaoDai = {50,50}
TyLeGhepPhongPhaoDai = {20}	
TyLeGhepPhongPhaoTrung = {50}
TyLeGhepPhongPhaoTieu = {70}
-- Th¸ng 4 -- GhÐp L¸ Cê ChiÕn Th¾ng
TyLeGhepLaCoChienThang = {100}
-- Th¸ng 6 -- GhÐp B¸nh Kem
TyLeBanhKemCatTuong = {100}
TyLeBanhKemNhuY = {100}
-- Th¸ng 9 -- GhÐp Quµ Quèc Kh¸nh
TyLeGhepQuaQuocKhanh = {100}
-- Th¸ng 11 -- GhÐp BÝ KiÕp Gia TruyÒn
TyLeGhepBiKiepGiaTruyen = {100}

----------------------------------------------------------------------------------------------------