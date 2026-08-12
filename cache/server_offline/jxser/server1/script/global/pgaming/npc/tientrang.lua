IncludeLib("ITEM")
Include("\\script\\global\\systemconfig.lua")
Include("\\script\\global\\head_qianzhuang.lua")
Include("\\script\\task\\newtask\\newtask_head.lua")
Include("\\script\\task\\newtask\\education\\jiaoyutasknpc.lua")	
Include("\\script\\global\\vn\\extpointfunc_proc.lua")
Include("\\script\\activitysys\\npcdailog.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")
Include("\\script\\global\\pgaming\\configserver\\configall.lua")

----------------------------------------------------------------------------------------------------
--											 TiÒn Trang											  --
----------------------------------------------------------------------------------------------------
function myplayersex()
	if GetSex() == 1 then 
		return "N÷ HiÖp"
	else
		return "§¹i HiÖp"
	end
end

function main()
	dofile("script/global/pgaming/npc/tientrang.lua")
	if NPCTienTrang ~=1 then
		Talk(1,"","TÝnh n¨ng nµy t¹m ®ãng, xin h·y quay l¹i sau.")
		return 1
	end
	-- NhiÖm vô Long Ngò
	Uworld1000 = nt_getTask(1000)
	if ( Uworld1000 == 360 ) or ( Uworld1000 == 370 ) then
		education_qianzhuanglaoban()
	else
		tientranghoiquan()
	end
	/*
	-- T¾t ®æi KNB - TiÒn §ång
	if PhuongThucDoi == 1 then
		if (GetBoxLockState() == 0) then
			local tbOpt = {
				{"Ta Muèn Rót KNB",Rut_KNB},
				{"§æi KNB Thµnh TiÒn §ång",KNBthanhTienDong},
				{"Nh©n TiÖn GhÐ Qua Th«i",No},
			}
			CreateNewSayEx("<color=green>Tµi Kho¶n: <color=red>"..GetAccount().."<color> - Nh©n VËt: <color=red>"..GetName().."<color>\nKNB cßn l¹i: <color=yellow>"..GetExtPoint(1).."<color>\n§iÓm N¹p ThÎ: <color=yellow>"..GetTask(5733).."<color>", tbOpt)
		else
			Talk(1,"","<color=green>"..myplayersex().." H·y Më Khãa R­¬ng Tr­íc<color>")
		end
	else
		if (GetBoxLockState() == 0) then
			local tbOpt = {
				{"Ta Muèn Rót tiÒn ®ång",Rut_KNB},
				{"Nh©n TiÖn GhÐ Qua Th«i",No},
			}
			CreateNewSayEx("<color=green>Tµi Kho¶n: <color=red>"..GetAccount().."<color> - Nh©n VËt: <color=red>"..GetName().."<color>\nTiÒn §ång cßn l¹i: <color=yellow>"..GetExtPoint(1).."<color>\n§iÓm N¹p ThÎ: <color=yellow>"..GetTask(5733).."<color>", tbOpt)
		else
			Talk(1,"","<color=green>"..myplayersex().." H·y Më Khãa R­¬ng Tr­íc<color>")
		end
	end
	*/
end

----------------------------------------------------------------------------------------------------
function tientranghoiquan()
	local TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ chñ <color=green>TiÒn Trang<color>!<enter>Ta cã thÓ trao ®æi tiÒn tÖ!"
	local player_name = GetName() 
	local tbSay = {format(TITLEDIALOG, GetName())}
		if DoiTienTe == 1 then
			tinsert(tbSay,"§æi TiÒn TÖ/doitien_main")
		end
		tinsert(tbSay,"§æi ThÇn BÝ §å ChÝ thµnh tiÒn v¹n/thanbidochi")
		tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
--											§æi TiÒn TÖ											  --
----------------------------------------------------------------------------------------------------
function doitien_main()
	Say("H·y chän ph­¬ng thøc giao dÞch!",3,
	"§æi tiÒn v¹n lÊy tiÒn ®ång./vanvsxu",
	"§æi tiÒn ®ång lÊy tiÒn v¹n./xuvsvan",
	"§Ó ta suy nghÜ ®·./no")
end

-- §æi TiÒn V¹n ra TiÒn §ång
function vanvsxu()
	Say("H·y chän mÖnh gi¸ quy ®æi.",4,
	"LÊy 10 v¹n ®æi 1 tiÒn ®ång./vanvsxu_1",
	"LÊy 100 v¹n ®æi 10 tiÒn ®ång./vanvsxu_2",
	"LÊy 1000 v¹n ®æi 100 tiÒn ®ång./vanvsxu_3",
	"§Ó ta suy nghÜ ®·./no")
end

function vanvsxu_1()
	if GetCash() >= 100000 then
		Pay(100000)
		AddStackItem (1,4,417,1,1,0,0,0)
		Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>1 tiÒn ®ång.<color>")
	else
		Msg2Player("H·y xem l¹i hµnh trang kh«ng ®ñ ng©n l­îng.")
	end
end
	
function vanvsxu_2()
	if GetCash() >= 1000000 then
		Pay(1000000)
		AddStackItem (10,4,417,1,1,0,0,0)
		Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>10 tiÒn ®ång.<color>")
	else
		Msg2Player("H·y xem l¹i hµnh trang kh«ng ®ñ ng©n l­îng.")
	end
end
	
function vanvsxu_3()
	if GetCash() >= 10000000 then
		Pay(10000000)
		AddStackItem (100,4,417,1,1,0,0,0)
		Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>100 tiÒn ®ång.<color>")
	else
		Msg2Player("H·y xem l¹i hµnh trang kh«ng ®ñ ng©n l­îng.")
	end
end

-- §æi TiÒn §ång ra TiÒn V¹n
function xuvsvan()
	Say("H·y chän mÖnh gi¸ quy ®æi.",4,
	"LÊy 1 tiÒn ®ång ®æi 10 v¹n./xuvsvan_1",
	"LÊy 10 tiÒn ®ång ®æi 100 v¹n./xuvsvan_2",
	"LÊy 100 tiÒn ®ång ®æi 1000 v¹n./xuvsvan_3",
	"§Ó ta suy nghÜ ®·./no")
end

function xuvsvan_1()
	if CalcEquiproomItemCount (4,417,1,1) < 1 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 1 tiÒn ®ång.")
		return
	end
	ConsumeEquiproomItem (1,4,417,1,1)
	Earn (100000)
	Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>10 v¹n l­îng.<color>")
end
	
function xuvsvan_2()
	if CalcEquiproomItemCount (4,417,1,1) < 10 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 10 tiÒn ®ång.")
		return
	end
	ConsumeEquiproomItem (10,4,417,1,1)
	Earn (1000000)
	Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>100 v¹n l­îng.<color>")
end
	
function xuvsvan_3()
	if CalcEquiproomItemCount (4,417,1,1) < 100 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 100 tiÒn ®ång.")
		return
	end
	ConsumeEquiproomItem (100,4,417,1,1)
	Earn (10000000)
	Msg2Player("<color=green>Giao dÞch thµnh c«ng nhËn<color> <color=yellow>1000 v¹n l­îng.<color>")
end

----------------------------------------------------------------------------------------------------
--										   ThÇn BÝ §å ChÝ										  --
----------------------------------------------------------------------------------------------------
function thanbidochi()
	Say("C¸c h¹ muèn ®æi ThÇn BÝ §å ChÝ sang tiÒn v¹n chø? §iÒu kiÖn quy ®æi lµ ph¶i cã trªn 1000 ®å chÝ. TØ lÖ quy ®æi 1 ®å chÝ = 1000 l­îng b¹c",2,
	"§óng vËy, ta muèn quy ®æi ThÇn BÝ §å ChÝ/doi_tbdc_tv",
	"§Ó ta suy nghÜ ®·./OnCancel")
end

function doi_tbdc_tv()
	local myMapNum = GetTask(1027)
	if myMapNum >= 1000 then
		local a = myMapNum - 1000
		if a > 0 then
			local remain = a
			while remain > 0 do
				local batch = 999999
				if remain < 999999 then
					batch = remain*1000
				end
			Earn(batch)
			remain = remain*1000 - batch
			end
			Msg2Player("<color=green>B¹n ®· quy ®æi <color=yellow>"..a.." <color> ThÇn BÝ §å ChÝ, nhËn vÒ "..(a*1000).." l­îng b¹c<color>")
			Msg2Player("<color=green>Sè ThÇn BÝ §å ChÝ cßn l¹i lµ 1000<color>")
			SetTask(1027, 1000)
		else
			Msg2Player("<color=red>B¹n kh«ng cã tháa m·n ®Ó quy ®æi<color>")
		end
	else
		Msg2Player("<color=red>Sè ThÇn BÝ §å ChÝ cña b¹n nhá h¬n 1000, kh«ng thÓ quy ®æi<color>")
	end
end

----------------------------------------------------------------------------------------------------
function Rut_KNB()
	if PhuongThucDoi == 1 then
	AskClientForNumber("RutKNB",0,500,"Sè L­îng Rót")
	else
	AskClientForNumber("RutTienDong",0,500,"Sè L­îng Rót")
	end
end

function RutKNB(num)
	local nRuong = CalcFreeItemCellCount() 
	if nRuong < 30 then
		Talk(1,"","CÇn trèng 30 « r­¬ng chøa ®å")
		return 1
	end 
	if (GetExtPoint(1) >= num) then
		SetTask(5733,GetTask(5733)+num)
		PayExtPoint(1,num)
		tbAwardTemplet:GiveAwardByList({tbProp={4,343,1,1,0,0},nCount=num},1)
		SetTask(5997,GetTask(5997)+num)
		Msg2Player("Chóc Mõng "..myplayersex().." §· Rót Thµnh C«ng <color=yellow>"..num.."<color> KNB")
		WriteLogPro("dulieu/RutKimNguyenBao.txt",""..GetAccount().."  "..GetName().."\t "..tonumber(GetLocalDate("%Y%m%d%H%M")).."   "..GetIP().."\t §· Rót "..num.." KNB")
	else
		Talk(1, "", "<color=red>"..myplayersex().." §ang Cã: <color=yellow>"..GetExtPoint(1).."<color> KNB\n         Sè L­îng CÇn Rót: <color=yellow>"..num.."<color> KNB\n                    Cßn ThiÕu: <color=yellow>"..num-GetExtPoint(1).."<color> KNB<color>")
	end
end

function RutTienDong(num)
	local nRuong = CalcFreeItemCellCount() 
	if nRuong < 30 then
		Talk(1,"","CÇn trèng 30 « r­¬ng chøa ®å")
		return 1
	end 
	if (GetExtPoint(1) >= num) then
		SetTask(5733,GetTask(5733)+num)
		PayExtPoint(1,num)
		tbAwardTemplet:GiveAwardByList({tbProp={4,417,1,1,0,0},nCount=num},1)
		SetTask(5997,GetTask(5997)+num)
		Msg2Player("Chóc Mõng "..myplayersex().." §· Rót Thµnh C«ng <color=yellow>"..num.."<color> TiÒn §ång")
		WriteLogPro("dulieu/RutKimNguyenBao.txt",""..GetAccount().."  "..GetName().."\t "..tonumber(GetLocalDate("%Y%m%d%H%M")).."   "..GetIP().."\t §· Rót "..num.." TiÒn §ång")
	else
		Talk(1, "", "<color=red>"..myplayersex().." §ang Cã: <color=yellow>"..GetExtPoint(1).."<color> TiÒn §ång\n         Sè L­îng CÇn Rót: <color=yellow>"..num.."<color> TiÒn §ång\n                    Cßn ThiÕu: <color=yellow>"..num-GetExtPoint(1).."<color> TiÒn §ång<color>")
	end
end

function KNBthanhTienDong()
	local nKNB = CalcEquiproomItemCount(4,343,1,-1)
	AskClientForNumber("doiknbthanhtiendong2",0,nKNB, "NhËp sè l­îng: ")
end

function doiknbthanhtiendong2(n_key)
	local nRuong = CalcFreeItemCellCount() 
	if nRuong < 30 then
		Talk(1,"","CÇn trèng 30 « r­¬ng chøa ®å")
		return 1
	end 
	for i=1,n_key do
		tbAwardTemplet:GiveAwardByList({tbProp = {4,417,0,0,0,0}, nCount = TyLeDoiKnbSangTienDong}, "test", 1)
		ConsumeEquiproomItem(1,4,343,1,-1)
	end
end

function WriteLogPro(data,str)
	local Data2 = openfile(""..data.."", "a+")
	write(Data2,tostring(str))
	closefile(Data2)
end