IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                        MÆt N¹ DiÖn Qu©n                                        --
----------------------------------------------------------------------------------------------------
if (not tbMaskDienQuanUpgrade) then
    tbMaskDienQuanUpgrade = {}
end

tbMaskDienQuanUpgrade.nReqTDTT      = 20
tbMaskDienQuanUpgrade.nReqPhoi      = 1
tbMaskDienQuanUpgrade.nReqVoLam     = 800
tbMaskDienQuanUpgrade.nReqTongKim   = 200
tbMaskDienQuanUpgrade.nReqPhongHoa  = 800
tbMaskDienQuanUpgrade.nReqMoney     = 100000000
tbMaskDienQuanUpgrade.szMaskName = "MÆt N¹ Héi Qu¸n"
tbMaskDienQuanUpgrade.nTargetMaskID = 1117

function tbMaskDienQuanUpgrade:Main()
    local szMsg = "<npc><enter>N©ng cÊp <color=gold>MÆt N¹ DiÖn Qu©n<color> cÇn cã:\n" ..
                  "<color=green>- MÆt N¹ Héi Qu¸n\n" ..
                  "- Ph«i MÆt N¹\n" ..
                  "- 20 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 800 Vâ L©m LÖnh\n" ..
                  "- 200 Tèng Kim LÖnh\n" ..
                  "- 800 Phong Háa LÖnh\n" ..
                  "- 10.000 v¹n l­îng<color>"
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, n©ng cÊp ngay", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbMaskDienQuanUpgrade:OpenUI()
    local szNote = "Bá vµo:<enter>- MÆt N¹ Héi Qu¸n<enter>- Ph«i MÆt N¹<enter>- 20 TuyÖt §Ønh Tri Thøc<enter>- 800 Vâ L©m LÖnh<enter>- 200 Tèng Kim LÖnh<enter>- 800 Phong Háa LÖnh"
    g_GiveItemUI("N©ng CÊp MÆt N¹ DiÖn Qu©n", szNote, {self.OnConfirm, {self}}, nil, 1)
end

function tbMaskDienQuanUpgrade:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nMaskIdx = -1
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
            if (szName == self.szMaskName) then
                nMaskIdx = nIdx
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
    if (nMaskIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..self.szMaskName.."<color>!")
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
        Talk(1, "", "B¹n kh«ng mang ®ñ sè l­îng LÖnh bµi yªu cÇu!")
        return 0
    end
    if (GetCash() < self.nReqMoney) then
        Talk(1, "", "B¹n kh«ng mang ®ñ <color=gold>10.000 v¹n l­îng<color>!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, self.nTargetMaskID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=gold>MÆt N¹ DiÖn Qu©n<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=gold>MÆt N¹ DiÖn Qu©n<color>!")
    end
    return 1
end