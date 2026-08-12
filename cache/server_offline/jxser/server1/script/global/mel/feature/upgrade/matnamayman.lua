IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                         MÆt N¹ May M¾n                                         --
----------------------------------------------------------------------------------------------------
if (not tbMaskMMUpgrade) then
    tbMaskMMUpgrade = {}
end

tbMaskMMUpgrade.nReqTDTT      = 5
tbMaskMMUpgrade.nReqPhoi      = 1
tbMaskMMUpgrade.nReqMoney     = 20000000
tbMaskMMUpgrade.tbJewelryNames = {
    "§éng S¸t B¹ch Kim §iªu Long Giíi",
    "§éng S¸t B¹ch Ngäc Cµn Kh«n Béi",
    "§éng S¸t B¹ch Kim Tó Phông Giíi",
    "§éng S¸t PhØ Thóy Ngäc H¹ng Khuyªn"
}
tbMaskMMUpgrade.nTargetMaskID = 1114

function tbMaskMMUpgrade:Main()
    local szMsg = "<npc>Ng­¬i muèn n©ng cÊp <color=gold>MÆt N¹ May M¾n<color>?\n" ..
                  "CÇn cã:\n" ..
                  "<color=green>- Bé trang bÞ §éng S¸t\n" ..
                  "- Ph«i MÆt N¹\n" ..
                  "- 5 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 2000 v¹n l­îng <color>"
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, n©ng cÊp ngay", self.OpenUI, {self}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbMaskMMUpgrade:OpenUI()
    g_GiveItemUI("N©ng CÊp MÆt N¹ May M¾n", "Bá vµo:<enter>- Bé trang bÞ §éng S¸t<enter>- Ph«i MÆt N¹<enter>- 5 TuyÖt §Ønh Tri Thøc", {self.OnConfirm, {self}}, nil, 1)
end

function tbMaskMMUpgrade:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local tbJewelryFound = {}
    for _, name in self.tbJewelryNames do tbJewelryFound[name] = 0 end
    local nPhoiCount, nTDTTCount = 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (tbJewelryFound[szName] ~= nil) then
                tbJewelryFound[szName] = tbJewelryFound[szName] + 1
            elseif (g == 4 and d == 2065) then
                nPhoiCount = nPhoiCount + nStack
            elseif (g == 4 and d == 2054) then
                nTDTTCount = nTDTTCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    for name, count in tbJewelryFound do
        if (count < 1) then
            Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..name.."<color>!")
            return 0
        end
    end
    if (nPhoiCount < self.nReqPhoi) then
        Talk(1, "", "B¹n thiÕu <color=yellow>Ph«i MÆt N¹<color>!")
        return 0
    end
    if (nTDTTCount < self.nReqTDTT) then
        Talk(1, "", "B¹n thiÕu <color=yellow>TuyÖt §Ønh Tri Thøc<color> (CÇn 5 c¸i)!")
        return 0
    end
    if (GetCash() < self.nReqMoney) then
        Talk(1, "", "B¹n kh«ng mang ®ñ <color=gold>2000 v¹n l­îng<color>!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, self.nTargetMaskID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=gold>MÆt N¹ May M¾n<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=gold>MÆt N¹ May M¾n<color>!")
    end
    return 1
end