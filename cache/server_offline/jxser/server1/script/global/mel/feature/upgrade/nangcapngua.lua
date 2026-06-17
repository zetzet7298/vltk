IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--									  N©ng CÊp Ngùa Hoµng Kim								  	  --
----------------------------------------------------------------------------------------------------
if (not tbUpgradeSystem) then
    tbUpgradeSystem = {}
end

tbUpgradeSystem.tbPath = {
    ["Phi V©n Hoµng Kim"] = {1068, "B«n Tiªu Hoµng Kim", 10, 10, 10000000},
    ["B«n Tiªu Hoµng Kim"] = {1069, "Phiªn Vò Hoµng Kim", 20, 20, 20000000},
    ["Phiªn Vò Hoµng Kim"] = {1070, "XÝch Long C©u Hoµng Kim", 30, 30, 30000000},
    ["XÝch Long C©u Hoµng Kim"] = {1071, "Du Huy Hoµng Kim", 40, 40, 40000000},
    ["Du Huy Hoµng Kim"] = {1072, "Siªu Quang Hoµng Kim", 50, 50, 50000000},
    ["Siªu Quang Hoµng Kim"] = {1073, "H·n HuyÕt Long C©u Hoµng Kim", 100, 100, 100000000},
}
tbUpgradeSystem.BDTM = {6, 1, 4894, 0, 0, 0}
tbUpgradeSystem.LHCM = {4, 2052, 1, 1}

function tbUpgradeSystem:ConfirmUpgrade(szHorseName)
    local tbData = self.tbPath[szHorseName]
    if (not tbData) then return end
    local szNextName = tbData[2]
    local nReqBD     = tbData[3]
    local nReqLH     = tbData[4]
    local nReqMoney  = tbData[5] / 10000
    local szMsg = "N©ng cÊp: <color=yellow>"..szHorseName.."<color> lªn <color=green>"..szNextName.."<color>\n" ..
                  "Yªu cÇu nguyªn liÖu:\n" ..
                  "- B¾c §Èu ThuÇn M· ThuËt: <color=gold>"..nReqBD.." c¸i<color>\n" ..
                  "- Linh Hån ChiÕn M·: <color=gold>"..nReqLH.." linh hån<color>\n" ..
                  "- Ng©n l­îng: <color=gold>"..nReqMoney.." v¹n<color>\n\n" ..
                  "Ng­¬i ®· mang ®ñ nguyªn liÖu ch­a?";

    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng cÊp ngay", self.OpenUpgradeUI, {self, szHorseName}},
        {"§Ó ta kiÓm tra l¹i"},
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUpgradeSystem:OpenUpgradeUI(szHorseName)
    local tbData = self.tbPath[szHorseName]
    local szNextName = tbData[2]
    local szTitle = "N©ng cÊp: "..szNextName
    local szDescription = "Yªu cÇu:<enter>"..szHorseName.."<enter>"..tbData[3].." B¾c §Èu ThuÇn M· ThuËt<enter>"..tbData[4].." Linh Hån ChiÕn M· Hoµng Kim<enter>"..(tbData[5]/10000).." v¹n."
    g_GiveItemUI(szTitle, szDescription, {self.OnConfirm, {self, szHorseName}}, nil, 1)
end

function tbUpgradeSystem:OnConfirm(szHorseName, nNum)
    if (nNum <= 0) then return 0 end
    local tbData = self.tbPath[szHorseName]
    local nHorseIdx = -1
    local nBDCount = 0
    local nLHCount = 0
    local tbItems = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p, l = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            
            if (szName == szHorseName) then
                nHorseIdx = nIdx
            elseif (g == self.BDTM[1] and d == self.BDTM[2] and p == self.BDTM[3]) then
                nBDCount = nBDCount + nStack
            elseif (g == self.LHCM[1] and d == self.LHCM[2] and p == self.LHCM[3]) then
                nLHCount = nLHCount + nStack
            else
                Talk(1, "", "ChiÕn M· <color=red>"..szName.."<color> kh«ng hîp lÖ!")
                return 0
            end
            tinsert(tbItems, nIdx)
        end
    end
    if (nHorseIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo ®óng lo¹i ChiÕn M· <color=yellow>"..szHorseName.."<color>!")
        return 0
    end
    if (nBDCount < tbData[3] or nLHCount < tbData[4]) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu! CÇn "..tbData[3].." B¾c §Èu ThuÇn M· ThuËt vµ "..tbData[4].." Linh Hån ChiÕn M· Hoµng Kim.")
        return 0
    end
    if (GetCash() < tbData[5]) then
        Talk(1, "", "Kh«ng ®ñ ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItems) do RemoveItemByIndex(tbItems[i]) end
    Pay(tbData[5])
    ItemIndex = AddGoldItem(0, tbData[1])
	SyncItem(ItemIndex)
    Msg2Player("Chóc mõng <color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=yellow>"..tbData[2].."<color>!")
    return 1
end