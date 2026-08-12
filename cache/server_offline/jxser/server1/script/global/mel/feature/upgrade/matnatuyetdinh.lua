IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                       MÆt N¹ TuyÖt §Ønh                                        --
----------------------------------------------------------------------------------------------------
if (not tbMaskTuyetDinhUpgrade) then
    tbMaskTuyetDinhUpgrade = {}
end

tbMaskTuyetDinhUpgrade.nReqTDTT      = 50
tbMaskTuyetDinhUpgrade.nReqPhoi      = 1
tbMaskTuyetDinhUpgrade.nReqCongThuc  = 1
tbMaskTuyetDinhUpgrade.nReqMetalHK   = 1
tbMaskTuyetDinhUpgrade.nReqHKLenh    = 50
tbMaskTuyetDinhUpgrade.nReqMoney     = 200000000
tbMaskTuyetDinhUpgrade.szMaskName = "MÆt N¹ DiÖn Qu©n"
tbMaskTuyetDinhUpgrade.nTargetMaskID = 1118

function tbMaskTuyetDinhUpgrade:Main()
    local szMsg = "<npc>ChÕ t¹o <color=gold>MÆt N¹ TuyÖt §Ønh<color> cÇn cã:\n" ..
                  "<color=green>- MÆt N¹ DiÖn Qu©n\n" ..
                  "- C«ng Thøc MÆt N¹\n" ..
                  "- Ph«i MÆt N¹\n" ..
                  "- Kim Lo¹i Hoµng Kim\n" ..
                  "- 50 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 50 Hoµng Kim LÖnh\n" ..
                  "- 20.000 v¹n l­îng<color>"
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, chÕ t¹o ngay", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbMaskTuyetDinhUpgrade:OpenUI()
    local szNote = "Bá vµo:<enter>- MÆt N¹ DiÖn Qu©n<enter>- C«ng Thøc MÆt N¹<enter>- Ph«i MÆt N¹<enter>- Kim Lo¹i Hoµng Kim<enter>- 50 TuyÖt §Ønh Tri Thøc<enter>- 50 Hoµng Kim LÖnh"
    g_GiveItemUI("ChÕ T¹o MÆt N¹ TuyÖt §Ønh", szNote, {self.OnConfirm, {self}}, nil, 1)
end

function tbMaskTuyetDinhUpgrade:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nMaskIdx = -1
    local nPhoiCount, nTDTTCount, nCongThucCount = 0, 0, 0
    local nMetalHKCount, nHKLenhCount = 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == self.szMaskName) then
                nMaskIdx = nIdx
            elseif (g == 4 and d == 2065) then
                nPhoiCount = nPhoiCount + nStack
            elseif (g == 4 and d == 2054) then
                nTDTTCount = nTDTTCount + nStack
            elseif (g == 4 and d == 2064) then
                nCongThucCount = nCongThucCount + nStack
            elseif (g == 4 and d == 2051) then
                nMetalHKCount = nMetalHKCount + nStack
            elseif (g == 6 and d == 1 and p == 4908) then
                nHKLenhCount = nHKLenhCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nMaskIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..self.szMaskName.."<color>!")
        return 0
    end
    if (nPhoiCount < self.nReqPhoi or nCongThucCount < self.nReqCongThuc or nMetalHKCount < self.nReqMetalHK) then
        Talk(1, "", "B¹n kh«ng mang ®ñ Ph«i, C«ng thøc hoÆc Kim lo¹i Hoµng Kim!")
        return 0
    end
    if (nTDTTCount < self.nReqTDTT) then
        Talk(1, "", "B¹n thiÕu <color=yellow>TuyÖt §Ønh Tri Thøc<color> (CÇn "..self.nReqTDTT.." c¸i)!")
        return 0
    end
    if (nHKLenhCount < self.nReqHKLenh) then
        Talk(1, "", "B¹n thiÕu <color=yellow>Hoµng Kim LÖnh<color> (CÇn "..self.nReqHKLenh.." c¸i)!")
        return 0
    end
    if (GetCash() < self.nReqMoney) then
        Talk(1, "", "B¹n kh«ng mang ®ñ <color=gold>20.000 v¹n l­îng<color>!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, self.nTargetMaskID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("ChÕ t¹o thµnh c«ng b¶o vËt <color=gold>MÆt N¹ TuyÖt §Ønh<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· chÕ t¹o thµnh c«ng b¶o vËt tèi cao: <color=gold>MÆt N¹ TuyÖt §Ønh<color>!")
    end
    return 1
end