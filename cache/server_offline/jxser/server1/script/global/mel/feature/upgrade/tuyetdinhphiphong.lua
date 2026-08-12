IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--                                      TuyÖt §Ønh Phi Phong                                      --
----------------------------------------------------------------------------------------------------
if (not tbUltimateCloak) then
    tbUltimateCloak = {}
end

tbUltimateCloak.nReqKLH     = 500
tbUltimateCloak.nReqKLHK    = 3
tbUltimateCloak.nReqTDTT    = 50
tbUltimateCloak.nReqTLHK    = 1
tbUltimateCloak.nReqHKL     = 100
tbUltimateCloak.nReqMoney   = 200000000
tbUltimateCloak.szBaseName  = "Phi Phong V« Cùc"
tbUltimateCloak.nTargetID   = 1113

function tbUltimateCloak:Main()
    local szMsg = "<npc>Ng­¬i muèn ®¹t ®Õn c¶nh giíi tèi cao cña Phi Phong: <color=gold>TuyÖt §Ønh V« Cùc<color>? CÇn cã:\n" ..
                  "<color=green>- Phi Phong V« Cùc (CÊp 10)\n" ..
                  "- T¬ Lôa Hoµng Kim\n" ..
                  "- 3 Kim Lo¹i Hoµng Kim\n" ..
                  "- 500 Kim Lo¹i HiÕm\n" ..
                  "- 50 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 100 Hoµng Kim LÖnh\n" ..
                  "- 20.000 v¹n l­îng<color>"
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, chÕ t¹o ngay", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUltimateCloak:OpenUI()
    local szNote = "Bá vµo:<enter>- Phi Phong V« Cùc<enter>- T¬ Lôa Hoµng Kim<enter>- 3 Kim lo¹i Hoµng Kim<enter>- 50 TuyÖt §Ønh Tri Thøc<enter>- 500 Kim Lo¹i HiÕm<enter>- 100 Hoµng Kim LÖnh"
    g_GiveItemUI("ChÕ T¹o Phi Phong TuyÖt §Ønh", szNote, {self.OnConfirm, {self}}, nil, 1)
end

function tbUltimateCloak:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nCloakIdx = -1
    local nKLHCount, nKLHKCount, nTDTTCount, nTLHKCount, nHKLCount = 0, 0, 0, 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == self.szBaseName) then
                nCloakIdx = nIdx
            elseif (g == 4 and d == 2045) then
                nKLHCount = nKLHCount + nStack
            elseif (g == 4 and d == 2051) then
                nKLHKCount = nKLHKCount + nStack
            elseif (g == 4 and d == 2054) then
                nTDTTCount = nTDTTCount + nStack
            elseif (g == 4 and d == 2055) then
                nTLHKCount = nTLHKCount + nStack
            elseif (g == 6 and d == 1 and p == 4908) then
                nHKLCount = nHKLCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nCloakIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..self.szBaseName.."<color>!")
        return 0
    end
    if (nTLHKCount < self.nReqTLHK or nKLHKCount < self.nReqKLHK) then
        Talk(1, "", "B¹n kh«ng mang ®ñ T¬ Lôa hoÆc Kim Lo¹i Hoµng Kim!")
        return 0
    end
    if (nKLHCount < self.nReqKLH or nTDTTCount < self.nReqTDTT or nHKLCount < self.nReqHKL) then
        Talk(1, "", "Kh«ng ®ñ sè l­îng Kim Lo¹i HiÕm, Tri Thøc hoÆc HK LÖnh!")
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
    local NewIdx = AddGoldItem(0, self.nTargetID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=gold>TuyÖt §Ønh Phi Phong V« Cùc<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· chÕ t¹o thµnh c«ng ThÇn Trang: <color=gold>TuyÖt §Ønh Phi Phong V« Cùc<color>!")
    end
    return 1
end