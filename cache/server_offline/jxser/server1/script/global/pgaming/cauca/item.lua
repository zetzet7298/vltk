Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\task\\system\\task_string.lua")
Include("\\script\\lib\\progressbar.lua")
IncludeLib("ITEM")

TaskExpCauCa	= 5746
TaskDateCauCa = 5745
MaxExpMoiNgay =  20000000

function main(nItemIndex)
	local G,D,P,nLevel = GetItemProp(nItemIndex)
	if (G ~= 6) then
		return 1
	end
	if CalcFreeItemCellCount() < 6 then
		CreateTaskSay({"Xin h·y s¾p xÕp l¹i hµnh trang! Nhí ®Ó trèng 6 « trë lªn nhÐ!",  "§­îc råi./Cancel",})
		return 1
	end
	if CheckGioiHan()==1 then
		return 1
	end
	if P >= 4898 and P <= 4902 then
		Exp(P)		
		return 0
	end
	return 1
end

function Exp(P)
	if (P == 4898) then		
		nExp = 1000000
		Msg2Player("Chóc mõng b¹n nhËn "..nExp.." ®iÓm kinh nghiÖm")
		SetTask(TaskExpCauCa,(nExp+GetTask(TaskExpCauCa)))
		phanthuongcamap()
		AddOwnExp(nExp)
	elseif (P == 4902) then		
		nExp = 2000000
		Msg2Player("Chóc mõng b¹n nhËn "..nExp.." ®iÓm kinh nghiÖm")
		SetTask(TaskExpCauCa,(nExp+GetTask(TaskExpCauCa)))
		phanthuongtom()
		AddOwnExp(nExp)
	end
end

function phanthuongcamap()
	local tbAward = {
		[1] = {szName="<color=green>Phóc Duyªn Lé (§¹i)",tbProp={6,1,124,1,0,0},nCount=1,nRate=50},
		[2] = {szName="<color=green>Vâ L©m LÖnh",tbProp={6,1,4905,1,0,0},nCount=1,nRate=50},
	}
	tbAwardTemplet:GiveAwardByList(tbAward, "PhÇn th­ëng mèc ho¹t ®éng")
end

function phanthuongtom()
	local tbAward = {
		[1] = {szName="<color=green>Vâ L©m LÖnh",tbProp={6,1,4905,1,0,0},nCount=2,nRate=30},
		[2] = {szName="<color=green>Kim Lo¹i HiÕm",tbProp={4,2045,1,0,0,0},nCount=5,nRate=9},
		[3] = {szName="<color=green>Phóc Duyªn Lé (§¹i)",tbProp={6,1,124,1,0,0},nCount=2,nRate=40},
		[4] = {szName="<color=green>Tèng Kim LÖnh",tbProp={6,1,4906,1,0,0},nCount=1,nRate=10},
		[5] = {szName="<color=green>Phong Háa LÖnh",tbProp={6,1,4907,1,0,0},nCount=1,nRate=10},
		[6] = {szName="<color=green>Hoµng Kim LÖnh",tbProp={6,1,4908,1,0,0},nCount=1,nRate=1},
	}
	tbAwardTemplet:GiveAwardByList(tbAward, "PhÇn th­ëng mèc ho¹t ®éng")
end

function CheckGioiHan()
	old_date = GetByte(GetTask(TaskDateCauCa), 1)
	old_month = GetByte(GetTask(TaskDateCauCa), 2)
	old_year = GetByte(GetTask(TaskDateCauCa), 3)
	now_date = tonumber(date("%d"))
	now_month = tonumber(date("%m"))
	now_year = tonumber(date("%y"))
	if old_date == now_date and old_month == now_month and old_year == now_year then
		if GetTask(TaskExpCauCa) > MaxExpMoiNgay then
			Msg2Player("Mâi ngµy chØ sö dông tèi ®a "..MaxExpMoiNgay.." ®iÓm kinh nghiÖm")
			return 1
		end
	else
		SetTask(TaskDateCauCa, SetByte(GetTask(TaskDateCauCa), 1, now_date))
		SetTask(TaskDateCauCa, SetByte(GetTask(TaskDateCauCa), 2, now_month))
		SetTask(TaskDateCauCa, SetByte(GetTask(TaskDateCauCa), 3, now_year))
		SetTask(TaskExpCauCa,0)
		return 0
	end
end