Include("\\script\\lib\\composeex.lua")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")

----------------------------------------------------------------------------------------------------
--										   §æi Vâ L©m LÖnh								  		  --
----------------------------------------------------------------------------------------------------
function doivolamlenh()
    local tbSay = {"H·y chän lo¹i vËt phÈm muèn ®æi."}
        tinsert(tbSay,"§æi TÈy Tñy Kinh/DoiTayTuyKinh")
        tinsert(tbSay,"§æi Vâ L©m MËt TÞch/DoiVoLamMatTich")
		tinsert(tbSay,"§æi LÖnh Bµi Boss Hoµng Kim NgÉu Nhiªn/DoiLenhBaiBossHK")
		tinsert(tbSay,"§æi Phi Phong L¨ng V©n/DoiPhiPhongLangVan")
		tinsert(tbSay,"§æi R­¬ng §Þnh Quèc/DoiRuongDinhQuoc")
		tinsert(tbSay,"§æi R­¬ng An Bang/DoiRuongAnBang")
		tinsert(tbSay,"§Ó ta suy nghÜ thªm ®·./no")
    CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
--										    TÈy Tñy Kinh								  		  --
----------------------------------------------------------------------------------------------------
function DoiTayTuyKinh()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>100 Vâ L©m LÖnh = 1 TÈy Tñy Kinh<color><enter>",2,
	"Ta ®ång ý ®æi/doittk",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function doittk()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)/100
	AskClientForNumber("doittk2",0,nVoLamLenh, "100/1: ")
end

function doittk2(n_key)
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	local nVoLamLenh2 = n_key*100
	if nVoLamLenh2 > nVoLamLenh then
		Talk(1,"","Kh«ng §ñ Vâ L©m LÖnh")
		return 1
	end
	local nRuong = CalcFreeItemCellCount()
	if n_key > nRuong then
		Talk(1,"","Kh«ng ®ñ r­¬ng chøa ®å")
		return 1
	end
	for i=1,n_key do
		ItemIndex = AddItem(6,1,22,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(100,6,1,4905,-1)
	end
end

----------------------------------------------------------------------------------------------------
--										  Vâ L©m MËt TÞch								  		  --
----------------------------------------------------------------------------------------------------
function DoiVoLamMatTich()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>100 Vâ L©m LÖnh = 1 Vâ L©m MËt TÞch<color><enter>",3,
	"Ta ®ång ý ®æi/doivlmt",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function doivlmt()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)/100
	AskClientForNumber("doivlmt2",0,nVoLamLenh, "100/1: ")
end

function doivlmt2(n_key)
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	local nVoLamLenh2 = n_key*100
	if nVoLamLenh2 > nVoLamLenh then
		Talk(1,"","Kh«ng §ñ Vâ L©m LÖnh")
		return 1
	end
	local nRuong = CalcFreeItemCellCount() 
	if n_key > nRuong then
		Talk(1,"","Kh«ng ®ñ r­¬ng chøa ®å")
		return 1
	end
	for i=1,n_key do
		ItemIndex = AddItem(6,1,26,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(100,6,1,4905,-1)
	end
end

----------------------------------------------------------------------------------------------------
--										  R­¬ng §Þnh Quèc								  		  --
----------------------------------------------------------------------------------------------------
function DoiRuongDinhQuoc()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>300 Vâ L©m LÖnh = 1 R­¬ng §Þnh Quèc<color><enter>",3,
	"Ta ®ång ý/doidq",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function doidq()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	if nVoLamLenh > 299 then
		ItemIndex = AddItem(6,1,4922,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(300,6,1,4905,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Vâ L©m LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--										   R­¬ng An Bang										  --
----------------------------------------------------------------------------------------------------
function DoiRuongAnBang()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>500 Vâ L©m LÖnh = 1 R­¬ng An Bang<color><enter>",3,
	"Ta ®ång ý/doiab",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function doiab()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	if nVoLamLenh > 499 then
		ItemIndex = AddItem(6,1,4923,1,0,0)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(500,6,1,4905,-1)
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Vâ L©m LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end

----------------------------------------------------------------------------------------------------
--							   LÖnh Bµi Boss §¹i Hoµng Kim NgÉu Nhiªn							  --
----------------------------------------------------------------------------------------------------
function DoiLenhBaiBossHK()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>100 Vâ L©m LÖnh = 1 LÖnh Bµi Boss Hoµng Kim<color><enter>",3,
	"Ta ®ång ý/doilbb",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function doilbb()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)/100
	AskClientForNumber("doilbb1",0,nVoLamLenh, "100/1: ")
end

function doilbb1(n_key)
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	local nVoLamLenh2 = n_key*100
	if nVoLamLenh2 > nVoLamLenh then
	Talk(1,"","Kh«ng §ñ Vâ L©m LÖnh")
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
		ConsumeEquiproomItem(100,6,1,4905,-1)
	end
end

----------------------------------------------------------------------------------------------------
--										 Phi Phong L¨ng V©n										  --
----------------------------------------------------------------------------------------------------
function DoiPhiPhongLangVan()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	Describe("Sè l­îng Vâ L©m LÖnh hiÖn cã: <color=yellow>: "..nVoLamLenh.."<color><enter><color=Green>200 Vâ L©m LÖnh = Phi Phong L¨ng V©n<color><enter>",3,
	"Ta ®ång ý/DoiPhiPhongLangVan1",
	"Ta sÏ quay l¹i sau!/no"
	)
end

function DoiPhiPhongLangVan1()
	local nVoLamLenh = CalcEquiproomItemCount(6,1,4905,-1)
	if nVoLamLenh > 199 then
		ItemIndex = AddGoldItem(0, 1078)
		SyncItem(ItemIndex)
		ConsumeEquiproomItem(200,6,1,4905,-1)
		Msg2Player("B¹n nhËn ®­îc <color=yellow>Phi Phong L¨ng V©n<color>")
	else
		Talk(1,"","B¹n vÉn ch­a ®ñ Vâ L©m LÖnh, h·y cè g¾ng thu thËp thªm")
		return 1
	end
end