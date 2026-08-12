IncludeLib("ITEM")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")
Include("\\script\\global\\pgaming\\configserver\\configall.lua")

----------------------------------------------------------------------------------------------------
--                                           Ho¹t §éng                                            --
----------------------------------------------------------------------------------------------------
-- §iÒu kiÖn nhËn th­ëng khi tham gia ho¹t ®éng Phong L¨ng §é, Boss S¸t Thñ, V­ît ¶i
SoLuongRuongTrongNhanThuong = 5 -- CÇn ®Ó 5 « trèng trong r­¬ng míi nhËn ®­îc phÇn th­ëng

----------------------------------------------------------------------------------------------------
--                                         Phong L¨ng §é                                          --
----------------------------------------------------------------------------------------------------
-- Phong L¨ng §é b¾t ®Çu mçi giê lóc phót thø 45. VÝ dô: 1:45, 2:45, 3:45, ..., 23:45
function PhanThuong_BossThuyTacPhongLangDo()
    local nRuong = CalcFreeItemCellCount() 
	if nRuong < SoLuongRuongTrongNhanThuong then
		Talk(1,"","Kh«ng §ñ "..SoLuongRuongTrongNhanThuong.." r­¬ng chøa ®å, kh«ng thÓ nhËn th­ëng")
		return 1
	end
	AddOwnExp(2000000)
	tbAwardTemplet:GiveAwardByList({tbProp = {4,417,1,0,0,0}, nCount = 5}, "TiÒn §ång", 1)
	SetTask(2501, GetTask(2501) + 10)
	Msg2Player("Tiªu diÖt Thñy TÆc, nhËn ®­îc phÇn th­ëng 10 <color=green>§iÓm Vinh Dù<color>")
end

--PhÇn th­ëng kÕt thóc Phong L¨ng §é b»ng lÖnh bµi phong l¨ng ®é
TAB_LBPLD = {
	{szName="<color=green>Vâ L©m LÖnh", tbProp={6,1,4905,1,0,0}, nCount = 5},
}
--PhÇn th­ëng kÕt thóc Phong L¨ng §é b»ng lÖnh bµi thñy tÆc
TAB_LBTT = {
	{szName="<color=green>Vâ L©m LÖnh", tbProp={6,1,4905,1,0,0}, nCount = 5},
}

----------------------------------------------------------------------------------------------------
--                                     Boss S¸t Thñ - V­ît ¶i                                     --
----------------------------------------------------------------------------------------------------
function PhanThuong_VuotAi()
    local nRuong = CalcFreeItemCellCount() 
	if nRuong < SoLuongRuongTrongNhanThuong then
		Talk(1,"","Kh«ng §ñ "..SoLuongRuongTrongNhanThuong.." r­¬ng chøa ®å, kh«ng thÓ nhËn th­ëng")
		return 1
	end
	AddOwnExp(3000000)
	tbAwardTemplet:GiveAwardByList({tbProp = {4,2045,1,0,0,0}, nCount = 5}, "Kim Lo¹i HiÕm", 1)
	tbAwardTemplet:GiveAwardByList({tbProp = {4,2054,1,0,0,0}, nCount = 1}, "TuyÖt §Ønh Tri Thøc", 1)
	SetTask(2501, GetTask(2501) + 50)
	Msg2Player("Hoµn thµnh V­ît ¶i, nhËn ®­îc phÇn th­ëng 50 <color=green>§iÓm Vinh Dù<color>")
end

----------------------------------------------------------------------------------------------------
-- Boss S¸t Thñ 10-80
function bosssatthu2x() AddOwnExp(20000) end
function bosssatthu3x() AddOwnExp(30000) end
function bosssatthu4x() AddOwnExp(40000) end
function bosssatthu5x() AddOwnExp(50000) end
function bosssatthu6x() AddOwnExp(60000) end
function bosssatthu7x() AddOwnExp(70000) end
function bosssatthu8x() AddOwnExp(80000) end

-- Boss S¸t Thñ 90
function bosssatthu9x()
    local nRuong = CalcFreeItemCellCount() 
	if nRuong < SoLuongRuongTrongNhanThuong then
		Talk(1,"","Kh«ng §ñ "..SoLuongRuongTrongNhanThuong.." r­¬ng chøa ®å, kh«ng thÓ nhËn th­ëng")
		return 1
	end
    AddOwnExp(1000000)
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,124,1,0,0}, nCount = 2}, "Phóc Duyªn Lé (§¹i)", 1)
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,4905,1,0,0}, nCount = 2}, "Vâ L©m LÖnh", 1)
end

----------------------------------------------------------------------------------------------------
--                                             D· TÈu                                             --
----------------------------------------------------------------------------------------------------
function Task_NewVersionAward()
	local nNum = GetTask(ID_TASKLINK_LIMITNUM)
	local nCancelNum = GetTask(ID_TASKLINK_LIMITCancelCount)
	for i=1,So_Lan_Da_Tau_Trong_Ngay do
		if ((nNum - nCancelNum) == i) then
			tbAwardTemplet:GiveAwardByList({tbProp = {6,1,196,1,0,0}, nCount = 10}, "MËt §å ThÇn BÝ", 1)
			tbAwardTemplet:GiveAwardByList({tbProp = {4,417}, nCount = 1}, "TiÒn §ång", 1)
			SetTask(2501, GetTask(2501) + 10)
			Msg2Player("Hoµn thµnh nhiÖm vô D· TÈu, nhËn ®­îc 10 <color=green>§iÓm Vinh Dù<color>")
		end
	end
end

----------------------------------------------------------------------------------------------------
--                                         Boss Hoµng Kim                                         --
----------------------------------------------------------------------------------------------------
-- Boss §¹i Hoµng Kim M«n Ph¸i
tbVnNewItemDropBossAward = {
	{{szName="Héi Qu¸n Linh D­îc Lé", tbProp={6,1,1181,1,0,0}, nCount=1},},
	{{szName="Hoµng Kim LÖnh", tbProp={6,1,4908,1,0,0}, nCount=1},},
}

-- Boss TuyÖt §Ønh Vò §Õ
tbTuyetDinhVuDeAward = {
	{{szName="TuyÖt §Ønh Th¹ch", tbProp={6,1,4959,1,0,0}, nCount=1},},
}

----------------------------------------------------------------------------------------------------