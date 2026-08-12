Include("\\script\\global\\mel\\configactivity.lua")

----------------------------------------------------------------------------------------------------
--                                         Kh«ng Sö Dông                                          --
----------------------------------------------------------------------------------------------------

----------------------------------------------------------------------------------------------------
--                                            Tèng Kim                                            --
----------------------------------------------------------------------------------------------------
-- Th­ëng Top Tèng Kim 21h00
tbThuongTop21 = {
	[1] = {
		{szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=5,},
		{szName="Tö Thñy Tinh",tbProp={4,239,1,0,0,0},nCount=3,},
    },
	[2] = {
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=3,},
		{szName="Tö Thñy Tinh",tbProp={4,239,1,0,0,0},nCount=2,},
    },
	[3] = {
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=2,},
		{szName="Tö Thñy Tinh",tbProp={4,239,1,0,0,0},nCount=1,},
    },
	[4] = {
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
    },
	[5] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,}
    },
	[6] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
    },
	[7] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
    },
	[8] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
    },
	[9] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
    },
	[10] = {		             
	    {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=1,},
	},
}
-- Th­ëng Top Tèng Kim cuèi tuÇn
tbThuongTopT7 = {
	[1] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=500},
    },
	[2] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=300},
    },
	[3] = {
	    {szName="TiÒn §ång",tbProp={4,417},nCount=200},
    },
	[4] = {
	    {szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[5] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[6] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[7] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[8] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[9] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
	[10] = {
		{szName="TiÒn §ång",tbProp={4,417},nCount=50},
    },
}
--Vßng S¸ng
function vongsangtop1()
    n_title = 5001 -- ID Danh HiÖu
    local nServerTime = GetCurServerTime()+ 72000
    local nDate = FormatTime2Number(nServerTime)
    local nDay = floor(mod(nDate,1000000) / 10000)
    local nMon = mod(floor(nDate / 1000000) , 100)
    local nTime = nMon * 1000000 + nDay * 10000 
    Title_AddTitle(n_title, 2, nTime)
    Title_ActiveTitle(n_title)
    SetTask(1122, n_title)
    Msg2Player("<color=pink>B¹n nhËn ®­îc vßng s¸ng danh hiÖu §Ö NhÊt Tèng Kim xin h·y tho¸t game ®Ó hiÖn vßng")
end

function vongsangtop2()
    n_title = 5002 -- ID Danh HiÖu
    local nServerTime = GetCurServerTime()+ 72000
    local nDate = FormatTime2Number(nServerTime)
    local nDay = floor(mod(nDate,1000000) / 10000)
    local nMon = mod(floor(nDate / 1000000) , 100)
    local nTime = nMon * 1000000 + nDay * 10000 
    Title_AddTitle(n_title, 2, nTime)
    Title_ActiveTitle(n_title)
    SetTask(1122, n_title)
    Msg2Player("<color=pink>B¹n nhËn ®­îc vßng s¸ng danh hiÖu §Ö NhÊt Tèng Kim xin h·y tho¸t game ®Ó hiÖn vßng")
end

function vongsangtop3()
    n_title = 5003 -- ID Danh HiÖu
    local nServerTime = GetCurServerTime()+ 72000
    local nDate = FormatTime2Number(nServerTime)
    local nDay = floor(mod(nDate,1000000) / 10000)
    local nMon = mod(floor(nDate / 1000000) , 100)
    local nTime = nMon * 1000000 + nDay * 10000 
    Title_AddTitle(n_title, 2, nTime)
    Title_ActiveTitle(n_title)
    SetTask(1122, n_title)
    Msg2Player("<color=pink>B¹n nhËn ®­îc vßng s¸ng danh hiÖu §Ö NhÊt Tèng Kim xin h·y tho¸t game ®Ó hiÖn vßng")
end	

----------------------------------------------------------------------------------------------------
--                                              TÝn Sø                                            --
----------------------------------------------------------------------------------------------------
function PhanThuong_TinSu()
    local nRuong = CalcFreeItemCellCount() 
	if nRuong < SoLuongRuongTrongNhanThuong then
		Talk(1,"","Kh«ng §ñ "..SoLuongRuongTrongNhanThuong.." r­¬ng chøa ®å, kh«ng thÓ nhËn th­ëng")
		return 1
	end
	local nTodayTaskCount = GetTask(5749)
	if nTodayTaskCount == 1 then
		tbAwardTemplet:GiveAwardByList({tbProp = {6,1,random(122,124),0,0,0},nCount=1}, "test", 1)
	end
	if nTodayTaskCount == 2 then
		tbAwardTemplet:GiveAwardByList({tbProp = {6,1,random(122,124),0,0,0},nCount=1}, "test", 1)
	end
end

----------------------------------------------------------------------------------------------------
--									  Lo¹n ChiÕn Cöu Ch©u Cèc									  --
----------------------------------------------------------------------------------------------------
-- Mçi mèc phÇn th­ëng ngÉu nhiªn 1 mãn
-- PhÇn th­ëng mçi trËn
PhanThuongMoiTranLoanChien = {
	[1] = 1000000,
}
--PhÇn th­ëng cuèi cïng
PhanThuongNguoiCuoiCungLoanChien = {
	[1] = {szName="TÈy Tñy Kinh",tbProp={6,1,22,1,0,0},nRate = 0.5,nCount=1},	
	[2] = {szName="Vâ L©m MËt TÞch",tbProp={6,1,26,1,0,0},nRate = 0.5,nCount=1},	
	[3] = {szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nRate = 0.5,nCount=1},
	[4] = {szName="QuÕ Hoa Töu",tbProp={6,1,125,1,0,0},nCount=1,nRate=0.5},
	[5] = {nExp_tl=1,nRate = 98,nCount = 10000000,},	
}

----------------------------------------------------------------------------------------------------
--											  §Êu Ng­u											  --
----------------------------------------------------------------------------------------------------
--PhÇn th­ëng r­¬ng ®Êu ng­u
PhanThuongRuongDauNguu = {
	{nExp = 5000000},
	{
		{nExp = 2000000, nRate = 60},				
		{szName="Vâ L©m MËt TÞch", tbProp={6, 1, 26, 1, 0, 0}, nRate = 0.1},
		{szName="TÈy Tñy Kinh", tbProp={6, 1, 22, 1, 0, 0}, nRate = 0.1},
		{szName="Phóc Duyªn Lé TiÓu", tbProp={6, 1, 122, 1, 0, 0}, nRate = 30},
		{szName="Phóc Duyªn Lé Trung", tbProp={6, 1, 123, 1, 0, 0}, nRate = 5},
		{szName="Phóc Duyªn Lé §¹i", tbProp={6, 1, 124, 1, 0, 0}, nRate = 2},
		{szName="Tiªn Th¶o Lé", tbProp={6, 1, 71, 1, 0, 0}, nRate = 2.7},
		{szName="Tiªn Th¶o Lé §Æc BiÖt", tbProp={6, 1, 1181, 1, 0, 0}, nRate = 0.1},
	}
}
--PhÇn th­ëng ng­êi th¾ng cuéc
PhanThuongNguoiThangCuocDauNguu = {
	{nExp = 10000000},
	{
		{szName="Vâ L©m MËt TÞch", tbProp={6, 1, 26, 1, 0, 0}, nRate = 0.1},
		{szName="TÈy Tñy Kinh", tbProp={6, 1, 22, 1, 0, 0}, nRate = 0.1},
		{szName="Phóc Duyªn Lé TiÓu", tbProp={6, 1, 122, 1, 0, 0}, nRate = 57},
		{szName="Phóc Duyªn Lé Trung", tbProp={6, 1, 123, 1, 0, 0}, nRate = 20},
		{szName="Phóc Duyªn Lé §¹i", tbProp={6, 1, 124, 1, 0, 0}, nRate = 2},
		{szName="Tiªn Th¶o Lé", tbProp={6, 1, 71, 1, 0, 0}, nRate = 20.7},
		{szName="Tiªn Th¶o Lé §Æc BiÖt", tbProp={6, 1, 1181, 1, 0, 0}, nRate = 0.1},
	}
}

----------------------------------------------------------------------------------------------------
--                                      PhÇn Th­ëng §ua Top                                       --
----------------------------------------------------------------------------------------------------
_TitleTopLvl = {
	{TitleID = 212, TitleName = "Duy Ng· §éc T«n", Xu = 500,},
	{TitleID = 213, TitleName = "Hµnh Gi¶ V« Song", Xu = 400,},
	{TitleID = 214, TitleName = "§éc B¸ Thiªn H¹", Xu = 300,},
}
tbAwardRank = {
	[1] = {
		{szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=5,nBindState = -2},			
	},
	[2] = {
		{szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=3,nBindState = -2},
	},
	[3] = {
		{szName="Tiªn Th¶o Lé",tbProp={6,1,71,1,0,0},nCount=2,nBindState = -2},
	},
}

----------------------------------------------------------------------------------------------------
--									  LÔ Bao C«ng Thµnh ChiÕn									  --
----------------------------------------------------------------------------------------------------
CongThanhLeBao = {
	{szName="Kinh nghiÖm", nExp_tl=1000000,nCount=1, nRate=60},
	{szName="Ng©n L­îng",nJxb=10000,nCount=1,nRate = 10},
	{szName="Phóc Duyªn Lé Trung", tbProp={6,1,123,1,0,0},nCount=1, nRate=10},
	{szName="Phóc Duyªn Lé §¹i", tbProp={6,1,124,1,0,0},nCount=1, nRate=10},
	{szName="LÖnh bµi phong l¨ng ®é", tbProp={4,489},nCount=1, nRate=5},
	{szName="Tiªn Th¶o Lé", tbProp={6,1,71,1,0,0},nCount=1, nRate=2},
	{szName="Lam Thñy Tinh", tbProp={4,238,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},
	{szName="Lôc Thñy Tinh", tbProp={4,240,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},
	{szName="Tö Thñy Tinh", tbProp={4,239,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},	
	{szName="Tiªn Th¶o Lé §Æc BiÖt", tbProp={6,1,4650,1,0,0},nCount=1, nRate=0.5},	
	{szName="Tinh Hång B¶o Th¹ch", tbProp={4,353,1,1,0,0},nCount=1,tbParam={60}, nRate=0.5},
	{szName="HuyÒn Tinh CÊp 5", tbProp={6,1,147,5,0,0,0},nCount=1, nRate=0.5},	
}

VuTruLeBao = {
	{szName="Kinh nghiÖm", nExp_tl=2000000,nCount=1, nRate=60},
	{szName="Phóc Duyªn Lé Trung", tbProp={6,1,123,1,0,0},nCount=1, nRate=20},
	{szName="Phóc Duyªn Lé §¹i", tbProp={6,1,124,1,0,0},nCount=1, nRate=5},
	{szName="LÖnh bµi phong l¨ng ®é", tbProp={4,489},nCount=1, nRate=5},
	{szName="Tiªn Th¶o Lé", tbProp={6,1,71,1,0,0},nCount=1, nRate=5},
	{szName="Tinh Hång B¶o Th¹ch", tbProp={4,353,1,1,0,0},nCount=1,tbParam={60}, nRate=0.5},	
	{szName="Lam Thñy Tinh", tbProp={4,238,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},
	{szName="Lôc Thñy Tinh", tbProp={4,240,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},
	{szName="Tö Thñy Tinh", tbProp={4,239,1,1,0,0},tbParam={60},nCount=1, nRate=0.5},
	{szName="HuyÒn Tinh CÊp 5", tbProp={6,1,147,5,0,0,0},nCount=1, nRate=1},
	{szName="Ch×a Khãa Hoµng Kim", tbProp={6,1,4889,1,0,0}, nRate=0.5},
	{szName="B¶o r­¬ng trang BÞ hoµng kim (NgÉu nhiªn)", tbProp={6,1,random(4879,4888),1,0,0}, nRate=0.5},		
	{szName="Tiªn Th¶o Lé §Æc BiÖt", tbProp={6,1,4650,1,0,0},nCount=1, nRate=1},	
}

----------------------------------------------------------------------------------------------------