IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--                                         Tø §¹i ThÇn M·                                         --
----------------------------------------------------------------------------------------------------
if (not tbUpgradeThanMa) then
    tbUpgradeThanMa = {}
end

tbUpgradeThanMa.Config = {
    szOldHorse = "H·n HuyÕt Long C©u Hoµng Kim",
    nLevelReq  = 150,
    nMoney     = 200000000,
    nReqBD     = 200,
    nReqLH     = 200,
    nReqHKL    = 50,
    nReqLVTM   = 1,
}
tbUpgradeThanMa.BDTM  = {6, 1, 4894, 0, 0, 0}
tbUpgradeThanMa.LHCM  = {4, 2052, 1, 1}
tbUpgradeThanMa.HKL   = {6, 1, 4908, 0, 0, 0}
tbUpgradeThanMa.LVTM  = {4, 2059, 1, 1}
tbUpgradeThanMa.ListThanMa = {
    [1] = {1074, "Háa Viªm ThÇn M·"},
    [2] = {1075, "H¾c Tinh ThÇn M·"},
    [3] = {1076, "H¾c Hãa ThÇn M·"},
    [4] = {1077, "Giao Phong ThÇn M·"},
}

function tbUpgradeThanMa:ConfirmUpgrade()
    local cfg = self.Config
    local szMsg = "N©ng cÊp:<enter><color=yellow>"..cfg.szOldHorse.."<color> lªn <color=pink>Tø §¹i ThÇn M·<color>\n" ..
                  "Yªu cÇu nguyªn liÖu:\n" ..
                  "- B¾c §Èu ThuÇn M· ThuËt: <color=gold>"..cfg.nReqBD.." c¸i<color>\n" ..
                  "- Linh Hån ChiÕn M·: <color=gold>"..cfg.nReqLH.." c¸i<color>\n" ..
                  "- Hoµng Kim LÖnh: <color=gold>"..cfg.nReqHKL.." c¸i<color>\n" ..
                  "- Linh VËt ThÇn M·: <color=gold>"..cfg.nReqLVTM.." c¸i<color>\n" ..
                  "- Ng©n l­îng: <color=gold>"..(cfg.nMoney/10000).." v¹n<color>\n\n" ..
                  "TØ lÖ thµnh c«ng: <color=green>100% (NgÉu nhiªn 1 trong 4)<color>";
    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng ngay", self.OpenUI, {self}},
        {"§Ó ta suy nghÜ l¹i"},
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUpgradeThanMa:OpenUI()
    local cfg = self.Config
    local szTitle = "N©ng cÊp Tø §¹i ThÇn M·"
    local szDescription = "Yªu cÇu:<enter>H·n HuyÕt Long C©u Hoµng Kim<enter>"..cfg.nReqBD.." B¾c §Èu ThuÇn M· ThuËt<enter>"..cfg.nReqLH.." Linh Hån ChiÕn M·<enter>"..cfg.nReqHKL.." Hoµng Kim LÖnh<enter>"..cfg.nReqLVTM.." Linh VËt ThÇn M·<enter>"..(cfg.nMoney/10000).." v¹n"
    g_GiveItemUI(szTitle, szDescription, {self.OnConfirm, {self}}, nil, 1)
end

function tbUpgradeThanMa:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local cfg = self.Config
    local nHorseIdx = -1
    local nBDCount, nLHCount, nHKLCount, nLVCount = 0, 0, 0, 0
    local tbItems = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p, l = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (szName == cfg.szOldHorse) then
                nHorseIdx = nIdx
            elseif (g == self.BDTM[1] and d == self.BDTM[2] and p == self.BDTM[3]) then
                nBDCount = nBDCount + nStack
            elseif (g == self.LHCM[1] and d == self.LHCM[2] and p == self.LHCM[3]) then
                nLHCount = nLHCount + nStack
            elseif (g == self.HKL[1] and d == self.HKL[2] and p == self.HKL[3]) then
                nHKLCount = nHKLCount + nStack
            elseif (g == self.LVTM[1] and d == self.LVTM[2] and p == self.LVTM[3]) then -- KiÓm tra LVTM
                nLVCount = nLVCount + nStack
            else
                Talk(1, "", "VËt phÈm <color=red>"..szName.."<color> kh«ng hîp lÖ!")
                return 0
            end
            tinsert(tbItems, nIdx)
        end
    end
    if (nHorseIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo mét con <color=yellow>"..cfg.szOldHorse.."<color>!")
        return 0
    end
    if (nBDCount < cfg.nReqBD or nLHCount < cfg.nReqLH or nHKLCount < cfg.nReqHKL or nLVCount < cfg.nReqLVTM) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu yªu cÇu!")
        return 0
    end
    if (GetCash() < cfg.nMoney) then
        Talk(1, "", "Kh«ng ®ñ ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItems) do RemoveItemByIndex(tbItems[i]) end
    Pay(cfg.nMoney)
    local nRand = random(1, 4)
    local nTargetID = self.ListThanMa[nRand][1]
    local szTargetName = self.ListThanMa[nRand][2]
    local pNewIdx = AddGoldItem(0, nTargetID)
    if (pNewIdx > 0) then
        SyncItem(pNewIdx)
        Msg2Player("Chóc mõng <color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=red>"..szTargetName.."<color>!")
        Talk(1, "", "B¹n nhËn ®­îc: " .. szTargetName)
    else
        Talk(1, "", "Lçi t¹o vËt phÈm! H·y liªn hÖ Admin kiÓm tra ID: " .. nTargetID)
    end
    return 1
end