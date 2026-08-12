IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--                                      TuyÖt §Ønh Trang Søc                                      --
----------------------------------------------------------------------------------------------------
if (not tbJewelryUpgrade) then
    tbJewelryUpgrade = {}
end

tbJewelryUpgrade.tbPath = {
    [1] = {1109, "TuyÖt §Ønh Trang Søc Hoµng Kim S¬ CÊp",    "Vâ L©m Hoµng Kim Trang Søc",               100, 10, 100,  10, 50000000},
    [2] = {1110, "TuyÖt §Ønh Trang Søc Hoµng Kim Trung CÊp", "TuyÖt §Ønh Trang Søc Hoµng Kim S¬ CÊp",    200, 20, 150, 20, 50000000},
    [3] = {1111, "TuyÖt §Ønh Trang Søc Hoµng Kim Cao CÊp",   "TuyÖt §Ønh Trang Søc Hoµng Kim Trung CÊp", 300, 30, 200, 30, 50000000},
    [4] = {1112, "TuyÖt §Ønh Trang Søc Hoµng Kim Hoµn Mü",   "TuyÖt §Ønh Trang Søc Hoµng Kim Cao CÊp",   500, 50, 200, 50, 50000000},
}
tbJewelryUpgrade.KLH  = {4, 2045, 1}
tbJewelryUpgrade.TDTT = {4, 2054, 1}
tbJewelryUpgrade.EKT  = {4, 2057, 1}
tbJewelryUpgrade.HKL  = {6, 1, 4908}

function nangcaptrangsuctuyetdinh()
    local tbMenu = {"<npc><enter>Ta cã thÓ luyÖn hãa <color=pink>Trang Søc<color> cña  ng­¬i ®¹t ®Õn c¶nh giíi <color=green>Hoµn Mü<color>.<enter><enter>Ng­¬i muèn n©ng cÊp lo¹i nµo?"}
    for i = 1, 4 do
        local szName = tbJewelryUpgrade.tbPath[i][2]
        tinsert(tbMenu, "N©ng cÊp " .. szName .. "/#tbJewelryUpgrade:ConfirmUpgrade(" .. i .. ")")
    end
    tinsert(tbMenu, "KÕt thóc ®èi tho¹i/cancel")
    CreateTaskSay(tbMenu)
end

function tbJewelryUpgrade:ConfirmUpgrade(nLevel)
    local tbData = self.tbPath[nLevel]
    if (not tbData) then return end
    local szMsg = "<npc>N©ng cÊp lªn: <color=green>"..tbData[2].."<color>\n" ..
                  "Nguyªn liÖu cÇn:\n" ..
                  "- <color=yellow>"..tbData[3].."<color>: 1 c¸i\n" ..
                  "- Kim Lo¹i HiÕm: <color=gold>"..tbData[4].." c¸i<color>\n" ..
                  "- TuyÖt §Ønh Tri Thøc: <color=gold>"..tbData[5].." c¸i<color>\n" ..
                  "- Trang Søc Kinh Th­: <color=gold>"..tbData[6].." c¸i<color>\n" ..
                  "- Hoµng Kim LÖnh: <color=gold>"..tbData[7].." c¸i<color>\n" ..
                  "- Ng©n l­îng: <color=gold>"..(tbData[8]/10000).." v¹n<color>";
    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng cÊp ngay", self.OpenUpgradeUI, {self, nLevel}},
        {"§Ó ta suy nghÜ l¹i/cancel"},
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbJewelryUpgrade:OpenUpgradeUI(nLevel)
    local tbData = self.tbPath[nLevel]
    local szTitle = "N©ng CÊp: "..tbData[2]
    local szDesc = "Bá vµo:<enter>- "..tbData[3].."<enter>- "..tbData[4].." Kim Lo¹i HiÕm<enter>- "..tbData[5].." TuyÖt §Ønh Tri Thøc<enter>- "..tbData[6].." Trang Søc Kinh Th­<enter>- "..tbData[7].." Hoµng Kim LÖnh"
    g_GiveItemUI(szTitle, szDesc, {self.OnConfirm, {self, nLevel}}, nil, 1)
end

function tbJewelryUpgrade:OnConfirm(nLevel, nNum)
    if (nNum <= 0) then return 0 end
    local tbData = self.tbPath[nLevel]
    local szReqName = tbData[3]
    local nJewIdx, nKLHCount, nTDTTCount, nEKTCount, nHKLCount = -1, 0, 0, 0, 0
    local tbItems = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == szReqName) then 
                nJewIdx = nIdx
            elseif (g == self.KLH[1] and d == self.KLH[2]) then 
                nKLHCount = nKLHCount + nStack
            elseif (g == self.TDTT[1] and d == self.TDTT[2]) then 
                nTDTTCount = nTDTTCount + nStack
            elseif (g == self.EKT[1] and d == self.EKT[2]) then 
                nEKTCount = nEKTCount + nStack
            elseif (g == self.HKL[1] and d == self.HKL[2] and p == self.HKL[3]) then 
                nHKLCount = nHKLCount + nStack
            else
                Talk(1, "", "VËt phÈm <color=red>"..szName.."<color> kh«ng hîp lÖ!")
                return 0
            end
            tinsert(tbItems, nIdx)
        end
    end
    if (nJewIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..szReqName.."<color>!")
        return 0
    end
    if (nKLHCount < tbData[4] or nTDTTCount < tbData[5] or nEKTCount < tbData[6] or nHKLCount < tbData[7] or GetCash() < tbData[8]) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu hoÆc ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItems) do RemoveItemByIndex(tbItems[i]) end
    Pay(tbData[8])
    local NewItemIdx = AddGoldItem(0, tbData[1])
    if (NewItemIdx > 0) then
        SyncItem(NewItemIdx)
        Msg2Player("N©ng cÊp thµnh c«ng <color=yellow>"..tbData[2].."<color>!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=yellow>"..tbData[2].."<color>!")
    else
        Msg2Player("Lçi t¹o vËt phÈm. H·y b¸o GM!")
    end
    return 1
end