IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                        TÝn VËt Hoµng Kim                                       --
----------------------------------------------------------------------------------------------------
SetHiepCot = {
    "HiÖp Cèt ThiÕt HuyÕt Sam",
    "HiÖp Cèt §a T×nh Hoµn",
    "HiÖp Cèt §an T©m Giíi",
    "HiÖp Cèt T×nh ý KÕt"
}
SetNhuTinh = {
    "Nhu T×nh C©n Quèc Nghª Th­êng",
    "Nhu T×nh Thôc N÷ H¹ng Liªn",
    "Nhu T×nh Phông Nghi Giíi ChØ",
    "Nhu T×nh TuÖ T©m Ngäc Béi"
}
SetDinhQuoc = {
    "§Þnh Quèc Thanh Sa Tr­êng Sam",
    "§Þnh Quèc ¤ Sa Ph¸t Qu¸n",
    "§Þnh Quèc XÝch Quyªn NhuyÔn Ngoa",
    "§Þnh Quèc Tö §»ng Hé UyÓn",
    "§Þnh Quèc Ng©n Tµm Yªu §¸i"
}
SetAnBang = {
    "An Bang B¨ng Tinh Th¹ch H¹ng Liªn",
    "An Bang Cóc Hoa Th¹ch ChØ Hoµn",
    "An Bang §iÒn Hoµng Th¹ch Ngäc Béi",
    "An Bang Kª HuyÕt Th¹ch Giíi ChØ"
}

TB_CONFIG_TIN_VAT = {
    ["HiepCot"]  = { szName = "HiÖp Cèt",  tbSet = SetHiepCot,  nCount = 4, nResultID = 2060, nMoney = 5000000, nToken = 50 },
    ["NhuTinh"]  = { szName = "Nhu T×nh",  tbSet = SetNhuTinh,  nCount = 4, nResultID = 2061, nMoney = 5000000, nToken = 50 },
    ["DinhQuoc"] = { szName = "§Þnh Quèc", tbSet = SetDinhQuoc, nCount = 5, nResultID = 2062, nMoney = 5000000, nToken = 50 },
    ["AnBang"]   = { szName = "An Bang",   tbSet = SetAnBang,   nCount = 4, nResultID = 2063, nMoney = 5000000, nToken = 50 },
}

szCurrentType = ""

function doidat_tinvat(szType)
    dofile("script/global/mel/feature/exchange/tinvathoangkim.lua")
    szCurrentType = szType
    local cfg = TB_CONFIG_TIN_VAT[szType]
    if (cfg == nil) then return end
    local tbOpt = {
        {"§Æt bé trang bÞ "..cfg.szName.." vµo", open_ui_tinvat},
        {"KÕt Thóc §èi Tho¹i", No},
    }
    local szMsg = format("§Æt vµo bé trang bÞ %s<enter><enter><color=Green>ChØ bá bé %s!<color> <color=Red>Kh«ng bá TiÒn §ång!<color><enter><enter><color=Gold>CÇn %d TiÒn §ång vµ %d v¹n ®Êy nhÐ!<color>", 
        cfg.szName, cfg.szName, cfg.nToken, (cfg.nMoney/10000))
        
    CreateNewSayEx(szMsg, tbOpt)
end

function open_ui_tinvat()
    local cfg = TB_CONFIG_TIN_VAT[szCurrentType]
    GiveItemUI("§æi TÝn VËt "..cfg.szName, "ChØ cÇn bá bé "..cfg.szName.."<enter>Kh«ng cÇn bá TiÒn §ång", "confirm_exchange_tinvat", "onCancel", 1)
end

function confirm_exchange_tinvat(nCount)
    local cfg = TB_CONFIG_TIN_VAT[szCurrentType]
    local countvk = 0

    if nCount ~= cfg.nCount then
        Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu (CÇn "..cfg.nCount.." mãn)!", 0)
        return 0
    end

    for i = 1, nCount do
        local nItemIndex = GetGiveItemUnit(i)
        local szName = GetItemName(nItemIndex)
        for j = 1, getn(cfg.tbSet) do
            if szName == cfg.tbSet[j] then
                countvk = countvk + 1
                break
            end
        end
    end

    if countvk ~= cfg.nCount then
        Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng ®óng bé "..cfg.szName.."!", 0)
        return 0
    end

    if GetCash() < cfg.nMoney then
        Say("Kh«ng ®ñ "..(cfg.nMoney/10000).." v¹n l­îng.<enter>TiÒn ®©u sao kh«ng ®ñ!", 0)
        return 0
    end

    if (CalcEquiproomItemCount(4, 417, 1, 1) >= cfg.nToken) then
        for i = 1, nCount do
            local nItemIndex = GetGiveItemUnit(i)
            RemoveItemByIndex(nItemIndex)
        end

        ConsumeEquiproomItem(cfg.nToken, 4, 417, 1, 1)
        Pay(cfg.nMoney)
        AddItem(4, cfg.nResultID, 1, 1, 0, 0)

        Msg2Player("Chóc Mõng "..GetName().." §æi TÝn VËt "..cfg.szName.." thµnh c«ng")
    else
        Say("Kh¸ch quan ®ang trªu chäc tiÓu nh©n hay thËt sù ng­êi cã ®ñ "..cfg.nToken.." TiÒn §ång kh«ng?", 0)
    end
end

function doihiepcot()  doidat_tinvat("HiepCot")  end
function doinhutinh()  doidat_tinvat("NhuTinh")  end
function doidinhquoc() doidat_tinvat("DinhQuoc") end
function doianbang()   doidat_tinvat("AnBang")   end

----------------------------------------------------------------------------------------------------
if (not tbKimQuangCraft) then
    tbKimQuangCraft = {}
end

tbKimQuangCraft.nMoneyReq = 50000000
tbKimQuangCraft.nTDTTReq = 20
tbKimQuangCraft.szSpearName = "KÕ NghiÖp B«n L«i Toµn Long Th­¬ng"
tbKimQuangCraft.tbMaterialIDs = {
    [2060] = "TÝn VËt HiÖp Cèt",
    [2061] = "TÝn VËt Nhu T×nh",
    [2062] = "TÝn VËt §Þnh Quèc",
    [2063] = "TÝn VËt An Bang",
    [2054] = "TuyÖt §Ønh Tri Thøc"
}

function tbKimQuangCraft:Main()
    local szMsg = "<npc>Ng­¬i muèn chÕ t¹o <color=yellow>R­¬ng Kim Quang<color>?\n" ..
                  "Yªu cÇu lÔ vËt:\n" ..
                  "- <color=yellow>"..self.szSpearName.."<color>\n" ..
                  "- <color=green>4 TÝn VËt (HiÖp Cèt, Nhu T×nh, §Þnh Quèc, An Bang)\n" ..
                  "- 20 TuyÖt §Ønh Tri Thøc\n" ..
                  "- 5000 v¹n l­îng Ng©n l­îng<color>";
    local tbOpt = {
        {"Ta mang ®ñ lÔ vËt, chÕ t¹o ngay", self.OpenUI, {self}},
        {"§Ó ta suy nghÜ l¹i/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbKimQuangCraft:OpenUI()
    g_GiveItemUI("ChÕ T¹o Kim Quang", "Bá vµo:<enter>- Th­¬ng KÕ NghiÖp<enter>- 4 lo¹i TÝn vËt<enter>- 20 TuyÖt §Ønh Tri Thøc", {self.OnConfirm, {self}}, nil, 1)
end

function tbKimQuangCraft:OnConfirm(nNum)
    if (nNum <= 0) then return 0 end
    local nSpearIdx = -1
    local tbMaterialsFound = { [2060]=0, [2061]=0, [2062]=0, [2063]=0, [2054]=0 }
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            if (szName == self.szSpearName) then
                nSpearIdx = nIdx
            elseif (g == 4 and tbMaterialsFound[d] ~= nil) then
                local nStack = GetItemStackCount(nIdx)
                if (nStack <= 0) then nStack = 1 end
                tbMaterialsFound[d] = tbMaterialsFound[d] + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nSpearIdx == -1) then
        Say("B¹n ph¶i bá vµo <color=yellow>"..self.szSpearName.."<color>!", 0)
        return 0
    end
    local tbTinvatIDs = {2060, 2061, 2062, 2063}
    for i = 1, 4 do
        local pID = tbTinvatIDs[i]
        if (tbMaterialsFound[pID] < 1) then
            Say("B¹n thiÕu <color=yellow>"..self.tbMaterialIDs[pID].."<color>!", 0)
            return 0
        end
    end
    if (tbMaterialsFound[2054] < self.nTDTTReq) then
        Say("B¹n thiÕu <color=yellow>TuyÖt §Ønh Tri Thøc<color>. CÇn cã "..self.nTDTTReq.." c¸i!", 0)
        return 0
    end
    if (GetCash() < self.nMoneyReq) then
        Say("B¹n kh«ng mang ®ñ <color=gold>5000 v¹n l­îng<color>!", 0)
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do
        RemoveItemByIndex(tbItemsToRemove[i])
    end
    Pay(self.nMoneyReq)
    local NewIdx = AddItem(6, 1, 4925, 1, 0, 0)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2Player("ChÕ t¹o <color=yellow>R­¬ng Kim Quang<color> thµnh c«ng!")
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· chÕ t¹o thµnh c«ng <color=gold>R­¬ng Kim Quang TuyÖt ThÕ<color>!")
    end
    return 1
end