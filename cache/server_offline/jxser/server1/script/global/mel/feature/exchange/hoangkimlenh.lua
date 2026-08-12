Include("\\script\\lib\\composeex.lua")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")

----------------------------------------------------------------------------------------------------
--								            Hoµng Kim LÖnh							  	          --
----------------------------------------------------------------------------------------------------
function doihoangkimlenh()
    local tbSay = {
        "H·y chän lo¹i vËt phÈm muèn ®æi.",
		"§æi LÖnh bµi Boss Hoµng Kim NgÉu Nhiªn/DoiLenhBaiBoss",
		"§æi R­¬ng Hoµng Kim/DoiRuongHKMP",
        "§æi R­¬ng TrÊn Bang Chi B¶o/DoiRuongTBCB",
        "§Ó ta suy nghÜ thªm ®·./no"
    }
    CreateTaskSay(tbSay)
end

ID_HKL = {6, 1, 4908, -1}
SoLuong_HKL = 100	--Sè l­îng Hoµng Kim LÖnh ®æi R­¬ng Hoµng Kim
SoLuong_HKLTB = 150	--Sè l­îng Hoµng Kim LÖnh ®æi R­¬ng TrÊn Bang

----------------------------------------------------------------------------------------------------
--								         §æi R­¬ng Hoµng Kim							  	      --
----------------------------------------------------------------------------------------------------
tbDanhSachRuong = {
    {"Thiªn V­¬ng", 4879, "hkmpthienvuong"},
    {"ThiÕu L©m",   4884, "hkmpthieulam"},
    {"§­êng M«n",   4885, "hkmpduongmon"},
    {"Ngò §éc",     4880, "hkmpngudoc"},
    {"Nga My",      4886, "hkmpngamy"},
    {"Thóy Yªn",    4881, "hkmpthuyyen"},
    {"C¸i Bang",    4888, "hkmpcaibang"},
    {"Thiªn NhÉn",  4883, "hkmpthiennhan"},
    {"Vâ §ang",     4887, "hkmpvodang"},
    {"C«n L«n",     4882, "hkmpconlon"}
}

function DoiRuongHKMP()
    local nHoangKimLenh = CalcEquiproomItemCount(ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
    local szMsg = "Sè l­îng Hoµng Kim LÖnh hiÖn cã: <color=yellow>"..nHoangKimLenh.."<color><enter>"..
                  "Tû lÖ ®æi: "..SoLuong_HKL.." Hoµng Kim LÖnh = 1 R­¬ng Hoµng Kim M«n Ph¸i"
    
    local tbOpt = {}
    for i=1, getn(tbDanhSachRuong) do
        tinsert(tbOpt, "§æi R­¬ng Hoµng Kim "..tbDanhSachRuong[i][1].."./"..tbDanhSachRuong[i][3])
    end
    tinsert(tbOpt, "Ta sÏ quay l¹i sau!/no")
    
    Describe(szMsg, getn(tbOpt), unpack(tbOpt))
end

function ThucHienDoiRuong(nItemP)
    local nCount = CalcEquiproomItemCount(ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
    if nCount >= SoLuong_HKL then
        local nIndex = AddItem(6, 1, nItemP, 1, 0, 0)
        if nIndex > 0 then
            SyncItem(nIndex)
            ConsumeEquiproomItem(SoLuong_HKL, ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
            Msg2Player("B¹n ®· ®æi thµnh c«ng 1 R­¬ng Hoµng Kim M«n Ph¸i.")
        end
    else
        Talk(1, "", "B¹n vÉn ch­a ®ñ Hoµng Kim LÖnh, h·y cè g¾ng thu thËp thªm.")
    end
end

function hkmpthienvuong() ThucHienDoiRuong(4879) end
function hkmpthieulam()   ThucHienDoiRuong(4884) end
function hkmpduongmon()   ThucHienDoiRuong(4885) end
function hkmpngudoc()     ThucHienDoiRuong(4880) end
function hkmpngamy()      ThucHienDoiRuong(4886) end
function hkmpthuyyen()    ThucHienDoiRuong(4881) end
function hkmpcaibang()    ThucHienDoiRuong(4888) end
function hkmpthiennhan()  ThucHienDoiRuong(4883) end
function hkmpvodang()     ThucHienDoiRuong(4887) end
function hkmpconlon()     ThucHienDoiRuong(4882) end

----------------------------------------------------------------------------------------------------
--								         §æi R­¬ng TrÊn Bang							  	      --
----------------------------------------------------------------------------------------------------
function DoiRuongTBCB()
    local nHoangKimLenh = CalcEquiproomItemCount(ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
    local szMsg = "Sè l­îng Hoµng Kim LÖnh hiÖn cã: <color=yellow>"..nHoangKimLenh.."<color><enter>"..
                  "Tû lÖ ®æi:<enter><color=Green>"..SoLuong_HKLTB.." Hoµng Kim LÖnh = 1 R­¬ng TrÊn Bang<color>"				  
	local tbOpt = {}

    tinsert(tbOpt,"§­îc råi h·y ®æi cho ta./DoiRuongTBCB1")
    tinsert(tbOpt,"Ta sÏ quay l¹i sau!/no")
    
    Describe(szMsg, getn(tbOpt), unpack(tbOpt))
end

function DoiRuongTBCB1()
    local nCount = CalcEquiproomItemCount(ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
    if nCount >= SoLuong_HKLTB then
        local nIndex = AddItem(6, 1, 4904, 1, 0, 0)
        if nIndex > 0 then
            SyncItem(nIndex)
            ConsumeEquiproomItem(SoLuong_HKLTB, ID_HKL[1], ID_HKL[2], ID_HKL[3], ID_HKL[4])
            Msg2Player("B¹n ®· ®æi thµnh c«ng 1 R­¬ng TrÊn Bang.")
        end
    else
        Talk(1, "", "B¹n vÉn ch­a ®ñ Hoµng Kim LÖnh, h·y cè g¾ng thu thËp thªm.")
    end
end

----------------------------------------------------------------------------------------------------
--								    §æi ®å Hoµng Kim M«n Ph¸i r¸c							  	  --
----------------------------------------------------------------------------------------------------
ListHKMP = {
	-- ThiÕu L©m
	"Méng Long ChÝnh Hång T¨ng M·o",
	"Méng Long Kim Ti ChÝnh Hång Cµ Sa",
	"Méng Long HuyÒn Ti Ph¸t §¸i",
	"Méng Long PhËt Ph¸p HuyÒn Béi",
	"Méng Long §¹t Ma T¨ng Hµi",

	"Phôc Ma Tö Kim C«n",
	"Phôc Ma HuyÒn Hoµng Cµ Sa",
	"Phôc Ma ¤ Kim NhuyÔn §iÒu",
	"Phôc Ma PhËt T©m NhuyÔn KhÊu",
	"Phôc Ma Phæ §é T¨ng Hµi",

	"Tø Kh«ng Gi¸ng Ma Giíi §ao",
	"Tø Kh«ng Tö Kim Cµ Sa",
	"Tø Kh«ng Hé ph¸p Yªu §¸i",
	"Tø Kh«ng NhuyÔn B× Hé UyÓn",
	"Tø Kh«ng Giíi LuËt Ph¸p Giíi",

	-- Thiªn V­¬ng
	"H¸m Thiªn Kim Hoµn §¹i Nh·n ThÇn Chïy",
	"H¸m Thiªn Vò ThÇn T­¬ng Kim Gi¸p",
	"H¸m Thiªn Uy Vò Thóc Yªu §¸i",
	"H¸m Thiªn Hæ ®Çu KhÈn Thóc UyÓn",
	"H¸m Thiªn Thõa Long ChiÕn Ngoa",

	"KÕ NghiÖp B«n L«i Toµn Long Th­¬ng",
	"KÕ NghiÖp HuyÒn Vò Hoµng Kim Kh¶i",
	"KÕ NghiÖp B¹ch Hæ V« Song KhÊu",
	"KÕ NghiÖp Háa V©n Kú L©n Thñ",
	"KÕ NghiÖp Chu T­íc L¨ng V©n Ngoa",

	"Ngù Long L­îng Ng©n B¶o §ao",
	"Ngù Long ChiÕn ThÇn Phi Qu¶i Gi¸p",
	"Ngù Long Thiªn M«n Thóc Yªu Hoµn",
	"Ngù Long TÊn Phong Hé UyÓn",
	"Ngù Long TuyÖt MÖnh ChØ Hoµn",

	-- Nga My
	"V« Gian û Thiªn KiÕm",
	"V« Gian Thanh Phong Truy Y",
	"V« Gian PhÊt V©n Ti §¸i",
	"V« Gian CÇm VËn Hé UyÓn",
	"V« Gian B¹ch Ngäc Bµn ChØ",

	"V« Ma Ma Ni Qu¸n",
	"V« Ma Tö Kh©m Cµ Sa",
	"V« Ma B¨ng Tinh ChØ Hoµn",
	"V« Ma TÈy T­îng Ngäc KhÊu",
	"V« Ma Hång Truy NhuyÔn Th¸p Hµi",

	"V« TrÇn Ngäc N÷ Tè T©m Qu¸n",
	"V« TrÇn Thanh T©m H­íng ThiÖn Ch©u",
	"V« TrÇn Tõ Bi Ngäc Ban ChØ",
	"V« TrÇn PhËt T©m Tõ H÷u Yªu Phèi",
	"V« TrÇn PhËt Quang ChØ Hoµn",

	-- Thóy Yªn
	"Tª Hoµng Phông Nghi §ao",
	"Tª Hoµng TuÖ T©m Khinh Sa Y",
	"Tª Hoµng Phong TuyÕt B¹ch V©n Thóc §¸i",
	"Tª Hoµng B¨ng Tung CÈm UyÓn",
	"Tª Hoµng Thóy Ngäc ChØ Hoµn",

	"BÝch H¶i Uyªn ¦¬ng Liªn Hoµn §ao",
	"BÝch H¶i Hoµn Ch©u Vò Liªn",
	"BÝch H¶i Hång Linh Kim Ti §¸i",
	"BÝch H¶i Hång L¨ng Ba",
	"BÝch H¶i Khiªn TÕ ChØ Hoµn",

	-- Ngò §éc
	"U Lung Kim Xµ Ph¸t §¸i",
	"U Lung XÝch YÕt MËt Trang",
	"U Lung Thanh Ng« TriÒn Yªu",
	"U Lung Ng©n ThÒm Hé UyÓn",
	"U Lung MÆc Thï NhuyÔn Lý",

	"Minh ¶o Tµ S¸t §éc NhËn",
	"Minh ¶o U §éc ¸m Y",
	"Minh ¶o §éc YÕt ChØ Hoµn",
	"Minh ¶o Hñ Cèt Hé UyÓn",
	"Minh ¶o Song Hoµn Xµ Hµi",

	"Chó Ph­îc Ph¸ gi¸p §Çu Hoµn",
	"Chó Ph­îc DiÖt L«i C¶nh Phï",
	"Chó Ph­îc U ¶o ChØ Hoµn",
	"Chó Ph­îc Xuyªn T©m §éc UyÓn",
	"Chó Ph­îc B¨ng Háa Thùc Cèt Ngoa",

	-- §­êng M«n
	"B¨ng Hµn §¬n ChØ Phi §ao",
	"B¨ng Hµn HuyÒn Y Thóc Gi¸p",
	"B¨ng Hµn T©m TiÔn Yªu KhÊu",
	"B¨ng Hµn HuyÒn Thiªn B¨ng Háa Béi",
	"B¨ng Hµn NguyÖt ¶nh Ngoa",

	"Thiªn Quang Hoa Vò M¹n Thiªn",
	"Thiªn Quang §Þnh T©m Ng­ng ThÇn Phï",
	"Thiªn Quang S©m La Thóc §¸i",
	"Thiªn Quang Song B¹o Hµn ThiÕt Tr¹c",
	"Thiªn Quang Thóc Thiªn Ph­îc §Þa Hoµn",

	"S©m Hoang Phi Tinh §o¹t Hån",
	"S©m Hoang Kim TiÒn Liªn Hoµn Gi¸p",
	"S©m Hoang Hån Gi¶o Yªu Thóc",
	"S©m Hoang HuyÒn ThiÕt T­¬ng Ngäc Béi",
	"S©m Hoang Tinh VÉn Phi Lý",

	"§Þa Ph¸ch Ngò hµnh Liªn Hoµn Qu¸n",
	"§Þa Ph¸ch H¾c DiÖm Xung Thiªn Liªn",
	"§Þa Ph¸ch TÝch LÞch L«i Háa Giíi",
	"§Þa Ph¸ch KhÊu T©m Tr¹c",
	"§Þa Ph¸ch §Þa Hµnh Thiªn Lý Ngoa",

	-- C¸i Bang
	"§ång Cõu Phi Long §Çu Hoµn",
	"§ång Cõu Gi¸ng Long C¸i Y",
	"§ång Cõu TiÒm Long Yªu §¸i",
	"§ång Cõu Kh¸ng Long Hé UyÓn",
	"§ång Cõu KiÕn Long Ban ChØ",

	"§Þch Kh¸i Lôc Ngäc Tr­îng",
	"§Þch Kh¸i Cöu §¹i C¸i Y",
	"§Þch Kh¸i TriÒn M·ng Yªu §¸i",
	"§Þch Kh¸i CÈu TÝch B× Hé UyÓn",
	"§Þch Kh¸i Th¶o Gian Th¹ch Giíi",

	-- Thiªn NhÉn
	"Ma S¸t Quû Cèc U Minh Th­¬ng",
	"Ma S¸t Tµn D­¬ng ¶nh HuyÕt Gi¸p",
	"Ma S¸t XÝch Ký Táa Yªu KhÊu",
	"Ma S¸t Cö Háa Liªu Thiªn UyÓn",
	"Ma S¸t V©n Long Thæ Ch©u Giíi",

	"Ma Hoµng Kim Gi¸p Kh«i",
	"Ma Hoµng ¸n XuÊt Hæ H¹ng Khuyªn",
	"Ma Hoµng Khª Cèc Thóc Yªu §¸i",
	"Ma Hoµng HuyÕt Y Thó Tr¹c",
	"Ma Hoµng §¨ng §¹p Ngoa",

	"Ma ThÞ LiÖt DiÖm Qu¸n MiÖn",
	"Ma ThÞ LÖ Ma PhÖ T©m Liªn",
	"Ma ThÞ NghiÖp Háa U Minh Giíi",
	"Ma ThÞ HuyÕt Ngäc ThÊt S¸t Béi",
	"Ma ThÞ S¬n H¶i Phi Hång Lý",

	-- Vâ §ang
	"L¨ng Nh¹c Th¸i Cùc KiÕm",
	"L¨ng Nh¹c V« Ng· §¹o Bµo",
	"L¨ng Nh¹c Né L«i Giíi",
	"L¨ng Nh¹c V« Cùc HuyÒn Ngäc Béi",
	"L¨ng Nh¹c Thiªn §Þa HuyÒn Hoµng Giíi",

	"CËp Phong Ch©n Vò KiÕm",
	"CËp Phong Tam Thanh Phï",
	"CËp Phong HuyÒn Ti Tam §o¹n CÈm",
	"CËp Phong Thóy Ngäc HuyÒn Hoµng Béi",
	"CËp Phong Thanh Tïng Ph¸p Giíi",

	-- C«n L«n
	"S­¬ng Tinh Thiªn Niªn Hµn ThiÕt",
	"S­¬ng Tinh Ng¹o S­¬ng §¹o Bµo",
	"S­¬ng Tinh Thanh Phong Lò §¸i",
	"S­¬ng Tinh Thiªn Tinh B¨ng Tinh Thñ",
	"S­¬ng Tinh Phong B¹o ChØ Hoµn",

	"L«i Khung Hµn Tung B¨ng B¹ch Quan",
	"L«i Khung Thiªn §Þa Hé Phï",
	"L«i Khung Phong L«i Thanh CÈm §¸i",
	"L«i Khung Linh Ngäc UÈn L«i",
	"L«i Khung Cöu Thiªn DÉn L«i Giíi",

	"Vô ¶o B¾c Minh §¹o Qu¸n",
	"Vô ¶o Ki B¸n Phï Chó",
	"Vô ¶o Thóc T©m ChØ Hoµn",
	"Vô ¶o Thanh ¶nh HuyÒn Ngäc Béi",
	"Vô ¶o Tung Phong TuyÕt ¶nh Ngoa",

	-- TrÊn Bang Chi B¶o
	"[TrÊn Bang Chi B¶o] Méng Long Tö Kim B¸t Nh· Giíi",
	"[TrÊn Bang Chi B¶o] Phôc Ma V« L­îng Kim Cang UyÓn",
	"[TrÊn Bang Chi B¶o] Tø Kh«ng §¹t Ma T¨ng Hµi",
	"[TrÊn Bang Chi B¶o] Ngù Long TÊn Phong Ph¸t C¬",
	"[TrÊn Bang Chi B¶o] V« Gian Thanh Phong NhuyÔn KÞch",
	"[TrÊn Bang Chi B¶o] V« Ma Thu Thñy L­u Quang §¸i",
	"[TrÊn Bang Chi B¶o] V« TrÇn TÞnh ¶nh L­u T«",
	"[TrÊn Bang Chi B¶o] Tª Hoµng HuÖ T©m Tr­êng Sinh KhÊu",
	"[TrÊn Bang Chi B¶o] BÝch H¶i Hoµn Ch©u Tuyªn Thanh C©n",
	"[TrÊn Bang Chi B¶o] Minh ¶o Song Hoµn Xµ KhÊu",
	"[TrÊn Bang Chi B¶o] Chó Ph­îc Trïng Cèt Ngäc Béi",
	"[TrÊn Bang Chi B¶o] Thiªn Quang §Þa Hµnh Thiªn Lý Ngoa",
	"[TrÊn Bang Chi B¶o] §Þa Ph¸ch Phong Hµn Thóc Yªu",
	"[TrÊn Bang Chi B¶o] §ång Cõu Ngù Long Ngäc Béi",
	"[TrÊn Bang Chi B¶o] Ma S¸t Cö Háa Liªu Thiªn Hoµn",
	"[TrÊn Bang Chi B¶o] Ma Hoµng Dung Kim §o¹n NhËt Giíi",
	"[TrÊn Bang Chi B¶o] Ma ThÞ LÖ Ma PhÖ T©m §¸i",
	"[TrÊn Bang Chi B¶o] L¨ng Nh¹c V« Ng· Thóc §¸i",
	"[TrÊn Bang Chi B¶o] CËp Phong Thóy Ngäc HuyÒn Hoµng UyÓn",
	"[TrÊn Bang Chi B¶o] S­¬ng Tinh L­u Tinh C¶n NguyÖt KhÊu",
	"[TrÊn Bang Chi B¶o] L«i Khung Linh Ngäc Èn L«i UyÓn",
	"[TrÊn Bang Chi B¶o] Vô ¶o Th¸i Uyªn Ch©n Vò Liªn",
}

function DoiTrangBiHKMP()
	local tbOpt = {
		{"§­îc th«i!",HKMPRac},
		{"KÕt Thóc §èi Tho¹i",No},
	}
	CreateNewSayEx("§Æt vµo 5 mãn Trang BÞ<enter><color=yellow>Hoµng Kim M«n Ph¸i<color> hoÆc <color=yellow>TrÊn Bang Chi B¶o<color><enter>§æi lÊy <color=green>3 LÖnh Bµi Boss Hoµng Kim ChØ §Þnh<color>", tbOpt)
end

function HKMPRac()
	GiveItemUI( "§æi Hoµng Kim LÖnh", "§Æt vµo 5 mãn trang bÞ<enter>Hoµng Kim M«n Ph¸i<enter>hoÆc<enter>TrÊn Bang Chi B¶o", "HKMPRac_1", "onCancel",1 )
end

function HKMPRac_1( nCount )
	countvk = 0
	if nCount ~= 5 then						
		Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
		return 0
	else
		for i = 1, nCount do
			local nItemIndex = GetGiveItemUnit(i)
			szName = GetItemName(nItemIndex)
			for i=1, getn(%ListHKMP) do
				if szName == %ListHKMP[i] then
					countvk = countvk + 1
				end
			end
		end
		if countvk ~= 5 then
				Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
				return 0
		end
		if CalcFreeItemCellCount() < 10 then
			Say("H·y ch¾c ch¾n r»ng b¹n cã ®ñ chç trèng trong hµnh trang.",0)
			return 0
		end
		if (CalcEquiproomItemCount(4,417,1,1)>=50) then
			for i = 1, nCount do		
				nItemIndex = GetGiveItemUnit( i )
				k = RemoveItemByIndex( nItemIndex )
				if ( k ~= 1 ) then
					Say("ChuyÖn g× vËy, sao ng­¬i cã thÓ g¹t ta?",0)
					return 0
				end		
			end
			ConsumeEquiproomItem(50,4,417,1,1)
			AddStackItem (3,6,1,4915,1,0,0)
			Msg2Player("Chóc Mõng "..GetName().." §æi trang bÞ Thµnh C«ng")
		else
			Say("Kh¸ch quan ®ang trªu chäc tiÓu nh©n hay thËt sù ng­êi cã ®ñ 50 TiÒn §ång kh«ng?", 0)
		end	
	end
end

----------------------------------------------------------------------------------------------------
--									LÖnh Bµi Boss §¹i Hoµng Kim								  	  --
----------------------------------------------------------------------------------------------------
function DoiLenhBaiBoss()
	local nHoangKimLenh = CalcEquiproomItemCount(6,1,4908,-1)
	Describe("Sè l­îng Hoµng Kim LÖnh hiÖn cã: <color=yellow>: "..nHoangKimLenh.."<color><enter><color=Green>5 Hoµng Kim LÖnh = 1 LÖnh Bµi Boss Hoµng Kim<color><enter>",3,
	"Ta ®ång ý/DoiLenhBaiBoss1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiLenhBaiBoss1()
	local nHoangKimLenh = CalcEquiproomItemCount(6,1,4908,-1)/5
	AskClientForNumber("DoiLenhBaiBoss2",0,nHoangKimLenh, "5/1: ")
end

function DoiLenhBaiBoss2(n_key)
	local nHoangKimLenh = CalcEquiproomItemCount(6,1,4908,-1)
	local nHoangKimLenh2 = n_key*5
	if nHoangKimLenh2 > nHoangKimLenh then
		Talk(1,"","Kh«ng §ñ Hoµng Kim LÖnh")
		return 1
	end
	local nRuong = CalcFreeItemCellCount() 
	if n_key > nRuong then
		Talk(1,"","Kh«ng ®ñ r­¬ng chøa ®å")
		return 1
	end 
	for i=1,n_key do
		ItemIndex = AddItem(6,1,4914,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(5,6,1,4908,-1)
	end
end