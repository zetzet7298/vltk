IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--                                       N©ng CÊp Phi Phong                                       --
----------------------------------------------------------------------------------------------------
if (not tbCloakUpgrade) then
    tbCloakUpgrade = {}
end

tbCloakUpgrade.tbPath = {
    [2]  = {1079, "Phi Phong TuyÖt ThÕ", "Phi Phong L¨ng V©n", 20, 200, 20000000},
    [3]  = {1080, "Phi Phong Ph¸ Qu©n", "Phi Phong TuyÖt ThÕ", 30, 300, 30000000},
    [4]  = {1081, "Phi Phong Ng¹o TuyÕt", "Phi Phong Ph¸ Qu©n", 40, 400, 40000000},
    [5]  = {1082, "Phi Phong K×nh L«i", "Phi Phong Ng¹o TuyÕt", 50, 500, 50000000},
    [6]  = {1083, "Phi Phong Ngù Phong", "Phi Phong K×nh L«i", 60, 600, 60000000},
    [7]  = {1084, "Phi Phong PhÖ Quang", "Phi Phong Ngù Phong", 70, 700, 70000000},
    [8]  = {1085, "Phi Phong KhÊp ThÇn", "Phi Phong PhÖ Quang", 80, 800, 80000000},
    [9]  = {1086, "Phi Phong K×nh Thiªn", "Phi Phong KhÊp ThÇn", 90, 900, 90000000},
    [10] = {1087, "Phi Phong V« Cùc", "Phi Phong K×nh Thiªn", 100, 1000, 100000000},
}
tbCloakUpgrade.MVG = {4, 2058, 1}
tbCloakUpgrade.KLH = {4, 2045, 1}

function nangcapphiphong()
    local tbMenu = {"<npc>Ta cã thÓ gia t¨ng ®¼ng cÊp Phi Phong cña ng­¬i lªn tÇm cao míi. Ng­¬i muèn chÕ t¹o lo¹i nµo?"}
    for i = 2, 10 do
        local szName = tbCloakUpgrade.tbPath[i][2]
        tinsert(tbMenu, "N©ng cÊp " .. szName .. "/#tbCloakUpgrade:ConfirmUpgrade(" .. i .. ")")
    end
    tinsert(tbMenu, "KÕt thóc ®èi tho¹i/cancel")
    CreateTaskSay(tbMenu)
end

function tbCloakUpgrade:ConfirmUpgrade(nLevel)
    local tbData = self.tbPath[nLevel]
    if (not tbData) then return end
    local szMsg = "<npc>N©ng cÊp lªn: <color=green>"..tbData[2].." (CÊp "..nLevel..")<color>\n" ..
                  "Nguyªn liÖu cÇn:\n" ..
                  "- <color=yellow>"..tbData[3].." (CÊp "..(nLevel-1)..")<color>: 1 c¸i\n" ..
                  "- M¶nh V¶i GÊm: <color=gold>"..tbData[4].." m¶nh<color>\n" ..
                  "- Kim Lo¹i HiÕm: <color=gold>"..tbData[5].." c¸i<color>\n" ..
                  "- Ng©n l­îng: <color=gold>"..(tbData[6]/10000).." v¹n<color>";
    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng cÊp ngay", self.OpenUpgradeUI, {self, nLevel}},
        {"§Ó ta suy nghÜ l¹i/cancel"},
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbCloakUpgrade:OpenUpgradeUI(nLevel)
    local tbData = self.tbPath[nLevel]
    local szTitle = "N©ng CÊp: "..tbData[2]
    local szDesc = "Bá vµo:<enter>- 1 "..tbData[3].."<enter>- "..tbData[4].." M¶nh V¶i GÊm<enter>- "..tbData[5].." Kim Lo¹i HiÕm"
    g_GiveItemUI(szTitle, szDesc, {self.OnConfirm, {self, nLevel}}, nil, 1)
end

function tbCloakUpgrade:OnConfirm(nLevel, nNum)
    if (nNum <= 0) then return 0 end
    local tbData = self.tbPath[nLevel]
    local szReqName = tbData[3]
    local nCloakIdx, nMVGCount, nKLHCount = -1, 0, 0
    local tbItems = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == szReqName) then 
                nCloakIdx = nIdx
            elseif (g == self.MVG[1] and d == self.MVG[2]) then 
                nMVGCount = nMVGCount + nStack
            elseif (g == self.KLH[1] and d == self.KLH[2]) then 
                nKLHCount = nKLHCount + nStack
            else
                Talk(1, "", "VËt phÈm <color=red>"..szName.."<color> kh«ng hîp lÖ!")
                return 0
            end
            tinsert(tbItems, nIdx)
        end
    end
    if (nCloakIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..szReqName.."<color>!")
        return 0
    end
    if (nMVGCount < tbData[4] or nKLHCount < tbData[5] or GetCash() < tbData[6]) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu hoÆc ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItems) do RemoveItemByIndex(tbItems[i]) end
    Pay(tbData[6])
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