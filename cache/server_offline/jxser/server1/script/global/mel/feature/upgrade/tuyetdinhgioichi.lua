IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                      TuyÖt §Ønh Giíi ChØ                                       --
----------------------------------------------------------------------------------------------------
if (not tbRingUpgrade) then
    tbRingUpgrade = {}
end

tbRingUpgrade.nReqMetalHiem = 500
tbRingUpgrade.nReqMetalHK   = 2
tbRingUpgrade.nReqTDTT      = 50
tbRingUpgrade.nReqKhuon     = 1
tbRingUpgrade.nReqMoney     = 100000000
tbRingUpgrade.szRingName1 = "Ngù Vinh ThÇn ChØ Hoµn"
tbRingUpgrade.szRingName2 = "Hång Duyªn Muéi Giíi ChØ"
tbRingUpgrade.nTargetRingID = 1142 

function tbRingUpgrade:Main()
    local szMsg = "<npc><enter>RÌn ®óc <color=gold>TuyÖt §Ønh Giíi ChØ<color> thÕ gian v« song cÇn cã\n" ..
                  "<color=green>- CÆp nhÉn Ngù Vinh & Hång Duyªn\n" ..
                  "- "..tbRingUpgrade.nReqMetalHiem.." Kim Lo¹i HiÕm\n" ..
                  "- "..tbRingUpgrade.nReqMetalHK.." Kim Lo¹i Hoµng Kim\n" ..
                  "- "..tbRingUpgrade.nReqTDTT.." TuyÖt §Ønh Tri Thøc\n" ..
                  "- Khu«n §óc TuyÖt §Ønh Giíi ChØ\n" ..
                  "- 10.000 v¹n l­îng<color>"
    local tbOpt = {
        {"TiÕn hµnh n©ng cÊp NhÉn", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbRingUpgrade:OpenUI()
    local szNote = "Bá vµo:<enter>- CÆp nhÉn Uyªn ¦¬ng<enter>- 500 Kim Lo¹i HiÕm<enter>- 2 Kim Lo¹i Hoµng Kim<enter>- 50 TuyÖt §Ønh Tri Thøc<enter>- Khu«n §óc TuyÖt §Ønh Giíi ChØ"
    g_GiveItemUI("N©ng CÊp TuyÖt §Ønh NhÉn", szNote, {self.OnConfirm, {self}}, nil, 1)
end

function tbRingUpgrade:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nRing1Idx, nRing2Idx = -1, -1
    local nKhuonCount, nMetalHiemCount, nMetalHKCount, nTDTTCount = 0, 0, 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == self.szRingName1) then
                nRing1Idx = nIdx
            elseif (szName == self.szRingName2) then
                nRing2Idx = nIdx
            elseif (g == 4 and d == 2053) then
                nKhuonCount = nKhuonCount + nStack
            elseif (g == 4 and d == 2045) then
                nMetalHiemCount = nMetalHiemCount + nStack
            elseif (g == 4 and d == 2051) then
                nMetalHKCount = nMetalHKCount + nStack
            elseif (g == 4 and d == 2054) then
                nTDTTCount = nTDTTCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nRing1Idx == -1 or nRing2Idx == -1) then
        Talk(1, "", "B¹n ph¶i bá ®ñ 2 nhÉn: <color=yellow>"..self.szRingName1.."<color> vµ <color=yellow>"..self.szRingName2.."<color>!")
        return 0
    end
    if (nKhuonCount < self.nReqKhuon or nMetalHiemCount < self.nReqMetalHiem or nMetalHKCount < self.nReqMetalHK or nTDTTCount < self.nReqTDTT) then
        Talk(1, "", "B¹n kh«ng mang ®ñ nguyªn liÖu yªu cÇu!")
        return 0
    end
    if (GetCash() < self.nReqMoney) then
        Talk(1, "", "Hµnh trang cña b¹n kh«ng ®ñ <color=gold>10.000 v¹n l­îng<color>!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, self.nTargetRingID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=gold>TuyÖt §Ønh Giíi ChØ<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· rÌn ®óc thµnh c«ng <color=gold>TuyÖt §Ønh Giíi ChØ<color> danh chÊn giang hå!")
    end
    return 1
end