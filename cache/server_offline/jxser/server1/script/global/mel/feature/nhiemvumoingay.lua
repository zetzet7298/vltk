Include("\\script\\battles\\battlehead.lua")
Include("\\script\\missions\\newcitydefence\\headinfo.lua")
Include("\\script\\missions\\fengling_ferry\\fld_head.lua")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--                                        NhiÖm Vô Mçi Ngµy                                       --
----------------------------------------------------------------------------------------------------
Task_Daily = {
    Count_Min_PHLT = 1,
    Point_Min_TongKim = 10000,
    Count_Min_PLD = 1,
    Count_Min_BossST = 10,
    TASKID_AWARD_DAILY = 5966,
}

function Task_Daily:Init()
     if GetTask(TASK_ID_DAY_TK) ~= tonumber(GetLocalDate("%m%d")) then
        SetTask(TASK_ID_DAY_TK, tonumber(GetLocalDate("%m%d")))
        SetTask(TASK_ID_COUNT_TK, 0)
		SetTask(TASK_ID_POINT_TK_PER_DAY, 0)
    end
    if GetTask(TASKID_DAY_PHLT) ~= tonumber(GetLocalDate("%m%d")) then
        SetTask(TASKID_COUNT_PHLT, 0)
        SetTask(TASKID_DAY_PHLT, tonumber(GetLocalDate("%m%d")))
    end
    if GetTask(TASKID_DAY_PLD) ~= tonumber(GetLocalDate("%m%d")) then
        SetTask(TASKID_COUNT_PLD, 0)
        SetTask(TASKID_DAY_PLD, tonumber(GetLocalDate("%m%d")))
    end
    if GetTask(1192) ~= tonumber(GetLocalDate("%y%m%d")) then
        SetTask(1193, 0)
        SetTask(1192, tonumber(GetLocalDate("%y%m%d")))
    end
end

function Task_Daily:Check()
    self:Init()
    local nPoint_TongKim = tonumber(GetTask(TASK_ID_POINT_TK_PER_DAY)) or 0
    local nCount_PHLT = tonumber(GetTask(TASKID_COUNT_PHLT)) or 0
    local nCount_PLD = tonumber(GetTask(TASKID_COUNT_PLD)) or 0
    local nCount_BossST = GetTask(1193) or 0
    if nPoint_TongKim < self.Point_Min_TongKim then

        return 0 
    end
    if nCount_PHLT < self.Count_Min_PHLT then

        return 0 
    end
    if nCount_PLD < self.Count_Min_PLD then

        return 0 
    end
    if nCount_BossST < self.Count_Min_BossST then
        
        return 0        
    end
    return 1
end

function Task_Daily:AwardDaily()
    SetTask(self.TASKID_AWARD_DAILY, tonumber(GetLocalDate("%m%d")))
    tbAwardTemplet:GiveAwardByList({tbProp = {4,417}, nCount = 10}, "TiÒn §ång", 1)
    tbAwardTemplet:GiveAwardByList({tbProp = {4,2045}, nCount = 10}, "Kim Lo¹i HiÕm", 1)
    tbAwardTemplet:GiveAwardByList({tbProp = {6,1,4907,1,0,0}, nCount = 10}, "Phong Háa LÖnh", 1)
    tbAwardTemplet:GiveAwardByList({tbProp = {6,1,4905,1,0,0}, nCount = 10}, "Vâ L©m LÖnh", 1)
    tbAwardTemplet:GiveAwardByList({tbProp = {6,1,124,0,0,0}, nCount = 20}, "Phóc Duyªn §¹i", 1)
    SetTask(747, GetTask(747) + 10000)
    Msg2Player("B¹n ®· nhËn ®­îc thªm <color=green>10000<color> ®iÓm tÝch lòy Tèng Kim!")
end

function Task_Daily:main()
    self:Init()
    if GetTask(self.TASKID_AWARD_DAILY) == tonumber(GetLocalDate("%m%d")) then
        Say("B¹n ®· nhËn phÇn th­ëng trong ngµy råi",1,"§ãng/no")
        return
    end
    local nLen = 25
    local szTitle1 = format("%-"..nLen.."s", "§iÓm Tèng Kim")
    local szTitle2 = format("%-"..nLen.."s", "Phong Háa Liªn Thµnh")
    local szTitle3 = format("%-"..nLen.."s", "Phong L¨ng §é")
    local szTitle4 = format("%-"..nLen.."s", "Boss S¸t Thñ")
    local szThongtin = ""
    szThongtin = szThongtin.."<pic=135> <color=green>"..szTitle1.."<color>: <bclr=blue>"..GetTask(TASK_ID_POINT_TK_PER_DAY).."/"..self.Point_Min_TongKim.."<bclr>\n"
    szThongtin = szThongtin.."<pic=135> <color=green>"..szTitle2.."<color>: <bclr=blue>"..GetTask(TASKID_COUNT_PHLT).."/"..self.Count_Min_PHLT.."<bclr>\n"
    szThongtin = szThongtin.."<pic=135> <color=green>"..szTitle3.."<color>: <bclr=blue>"..GetTask(TASKID_COUNT_PLD).."/"..self.Count_Min_PLD.."<bclr>\n"
    szThongtin = szThongtin.."<pic=135> <color=green>"..szTitle4.."<color>: <bclr=blue>"..GetTask(1193).."/"..self.Count_Min_BossST.."<bclr>\n"
    local szMsg = "<dec><npc>\nNhiÖm vô Mçi Ngµy cã nhiÒu phÇn th­ëng thó vÞ. Lµm ®ñ ®iÒu kiÖn bªn d­íi ta sÏ trao th­ëng cho ng­êi!\n\n" .. (szThongtin or "")
    local tbSay = {szMsg}
    if self:Check() == 1 then
		tinsert(tbSay, "NhËn th­ëng:/#Task_Daily:AwardDaily()")
		tinsert(tbSay, "KÕt thóc ®èi tho¹i./no")
	else 
		tinsert(tbSay, "Ch­a ®ñ ®iÒu kiÖn nhËn th­ëng./no")
	end
	CreateTaskSay(tbSay)
	return 1
end