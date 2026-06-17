IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                        MÆt N¹ Héi Qu¸n                                         --
----------------------------------------------------------------------------------------------------
if (not tbMaskHoiQuanUpgrade) then
    tbMaskHoiQuanUpgrade = {}
end

tbMaskHoiQuanUpgrade.nReqTDTT      = 10
tbMaskHoiQuanUpgrade.nReqPhoi      = 1
tbMaskHoiQuanUpgrade.nReqVoLam     = 500
tbMaskHoiQuanUpgrade.nReqTongKim   = 100
tbMaskHoiQuanUpgrade.nReqPhongHoa  = 500
tbMaskHoiQuanUpgrade.nReqMoney     = 50000000
tbMaskHoiQuanUpgrade.szMaskName1 = "MÆt N¹ May M¾n"
tbMaskHoiQuanUpgrade.szMaskName2 = "MÆt N¹ Kinh NghiÖm"
tbMaskHoiQuanUpgrade.nTargetMaskID = 1116

function tbMaskHoiQuanUpgrade:Main()
    local szMsg = "<npc>Ng­¬i muèn chÕ t¹o <color=gold>MÆt N¹ Héi Qu¸n<color>?\n" ..
                  "CÇn cã:\n" ..
                  "<color=green>- MÆt N¹ May M¾n\n" ..
                  "- MÆt N¹ Kinh NghiÖm\n" ..
                  "- Ph«i MÆt N¹\n" ..
                  "- 10 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 500 Vâ L©m LÖnh\n" ..
                  "- 100 Tèng Kim LÖnh\n" ..
                  "- 500 Phong Háa LÖnh\n" ..
                  "- 5000 v¹n l­îng Ng©n l­îng<color>"
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, n©ng cÊp ngay", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbMaskHoiQuanUpgrade:OpenUI()
    local szNote = "Bá vµo:<enter>- MÆt N¹ May M¾n<enter>- MÆt N¹ Kinh NghiÖm<enter>- Ph«i MÆt N¹<enter>- 10 TuyÖt §Ønh Tri Thøc<enter>- 500 Vâ L©m LÖnh<enter>- 100 Tèng Kim LÖnh<enter>- 500 Phong Háa LÖnh"
    g_GiveItemUI("N©ng CÊp MÆt N¹ Héi Qu¸n", szNote, {self.OnConfirm, {self}}, nil, 1)
end

function tbMaskHoiQuanUpgrade:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nMask1Idx, nMask2Idx = -1, -1
    local nPhoiCount, nTDTTCount = 0, 0
    local nVoLamCount, nTongKimCount, nPhongHoaCount = 0, 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == self.szMaskName1) then
                nMask1Idx = nIdx
            elseif (szName == self.szMaskName2) then
                nMask2Idx = nIdx
            elseif (g == 4 and d == 2065) then
                nPhoiCount = nPhoiCount + nStack
            elseif (g == 4 and d == 2054) then
                nTDTTCount = nTDTTCount + nStack
            elseif (g == 6 and d == 1 and p == 4905) then
                nVoLamCount = nVoLamCount + nStack
            elseif (g == 6 and d == 1 and p == 4906) then
                nTongKimCount = nTongKimCount + nStack
            elseif (g == 6 and d == 1 and p == 4907) then
                nPhongHoaCount = nPhongHoaCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nMask1Idx == -1 or nMask2Idx == -1) then
        Talk(1, "", "B¹n ph¶i bá ®ñ <color=yellow>"..self.szMaskName1.."<color> vµ <color=yellow>"..self.szMaskName2.."<color>!")
        return 0
    end
    if (nPhoiCount < self.nReqPhoi) then
        Talk(1, "", "B¹n thiÕu <color=yellow>Ph«i MÆt N¹<color>!")
        return 0
    end
    if (nTDTTCount < self.nReqTDTT) then
        Talk(1, "", "B¹n thiÕu <color=yellow>TuyÖt §Ønh Tri Thøc<color> (CÇn "..self.nReqTDTT.." c¸i)!")
        return 0
    end
    if (nVoLamCount < self.nReqVoLam or nTongKimCount < self.nReqTongKim or nPhongHoaCount < self.nReqPhongHoa) then
        Talk(1, "", "B¹n kh«ng mang ®ñ bé 3 lo¹i LÖnh bµi (Mçi lo¹i 100 c¸i)!")
        return 0
    end
    if (GetCash() < self.nReqMoney) then
        Talk(1, "", "B¹n kh«ng mang ®ñ <color=gold>5000 v¹n l­îng<color>!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, self.nTargetMaskID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=gold>MÆt N¹ Héi Qu¸n<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=gold>MÆt N¹ Héi Qu¸n<color>!")
    end
    return 1
end