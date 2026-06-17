Include("\\script\\lib\\composeex.lua")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")

----------------------------------------------------------------------------------------------------
--										§æi Phong Háa LÖnh										  --
----------------------------------------------------------------------------------------------------
function doiphonghoalenh()
    local tbSay = {"H·y chän lo¹i vËt phÈm muèn ®æi."}
        tinsert(tbSay,"§æi Héi Qu¸n Linh D­îc Lé/DoiTTLSC")
		tinsert(tbSay,"§æi R­¬ng §éng S¸t/DoiRuongDongSat")
		tinsert(tbSay,"§æi R­¬ng Hång ¶nh/DoiRuongHongAnh")
		tinsert(tbSay,"§æi T¬ Lôa Hoµng Kim/DoiToLua")
		tinsert(tbSay,"§æi Linh VËt ThÇn M·/DoiLinhVatThanMa")
		tinsert(tbSay,"§æi C«ng Thøc MÆt N¹/DoiCongThucMatNa")
		tinsert(tbSay,"§æi Khu«n ®óc TuyÖt §Ønh Giíi ChØ/DoiKhuonDuc")
		tinsert(tbSay,"§Ó ta suy nghÜ thªm ®·./no")
    CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
--									   Héi Qu¸n Linh D­îc Lé									  --
----------------------------------------------------------------------------------------------------
function DoiTTLSC()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>100 Phong Háa LÖnh = Héi Qu¸n Linh D­îc Lé<color><enter>",11,
	"Ta ®ång ý/DoiTTLSC1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiTTLSC1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 99 then
		ItemIndex = AddItem(6,1,1181,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(100,6,1,4907,-1)
		Msg2Player("B¹n ®· ®æi thµnh c«ng <color=green>Héi Qu¸n Linh D­îc Lé<color>.")
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										   R­¬ng §éng S¸t										  --
----------------------------------------------------------------------------------------------------
function DoiRuongDongSat()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>500 Phong Háa LÖnh = R­¬ng §éng S¸t<color><enter>",11,
	"Ta ®ång ý/DoiRuongDongSat1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiRuongDongSat1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 499 then
		ItemIndex = AddItem(6,1,4926,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(500,6,1,4907,-1)
		Msg2Player("B¹n ®· ®æi thµnh c«ng <color=green>R­¬ng §éng S¸t<color>.")
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										   R­¬ng Hång ¶nh										  --
----------------------------------------------------------------------------------------------------
function DoiRuongHongAnh()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>500 Phong Háa LÖnh = R­¬ng Hång ¶nh<color><enter>",11,
	"Ta ®ång ý/DoiRuongHongAnh1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiRuongHongAnh1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 499 then
		ItemIndex = AddItem(6,1,4927,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(500,6,1,4907,-1)
		Msg2Player("B¹n ®· ®æi thµnh c«ng <color=green>R­¬ng Hång ¶nh<color>.")
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										 T¬ Lôa Hoµng Kim								  	  	  --
----------------------------------------------------------------------------------------------------
function DoiToLua()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>10000 Phong Háa LÖnh = T¬ Lôa Hoµng Kim<color><enter>",11,
	"Ta ®ång ý/DoiToLua1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiToLua1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 9999 then
		ItemIndex = AddItem(4,2055,0,0,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(10000,6,1,4907,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										  Linh VËt ThÇn M·										  --
----------------------------------------------------------------------------------------------------
function DoiLinhVatThanMa()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>10000 Phong Háa LÖnh = Linh VËt ThÇn M·<color><enter>",11,
	"Ta ®ång ý/DoiLinhVatThanMa1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiLinhVatThanMa1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 9999 then
		ItemIndex = AddItem(4,2059,0,0,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(10000,6,1,4907,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--								   Khu«n ®óc TuyÖt §Ønh Giíi ChØ								  --
----------------------------------------------------------------------------------------------------
function DoiKhuonDuc()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>10000 Phong Háa LÖnh = Khu«n ®óc TuyÖt §Ønh Giíi ChØ<color><enter>",11,
	"Ta ®ång ý/DoiKhuonDuc1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiKhuonDuc1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 9999 then
		ItemIndex = AddItem(4,2053,0,0,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(10000,6,1,4907,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										  C«ng Thøc MÆt N¹										  --
----------------------------------------------------------------------------------------------------
function DoiCongThucMatNa()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	Describe("Sè l­îng Phong Háa LÖnh hiÖn cã: <color=yellow>: "..nPhongHoaLenh.."<color><enter><color=Green>10000 Phong Háa LÖnh = C«ng Thøc MÆt N¹<color><enter>",11,
	"Ta ®ång ý/DoiCongThucMatNa1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiCongThucMatNa1()
	local nPhongHoaLenh = CalcEquiproomItemCount(6,1,4907,-1)
	if nPhongHoaLenh > 9999 then
		ItemIndex = AddItem(4,2064,0,0,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(10000,6,1,4907,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Phong Háa LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end
----------------------------------------------------------------------------------------------------