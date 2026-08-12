IncludeLib("ITEM")

----------------------------------------------------------------------------------------------------
--                                     N©ng CÊp Ên TuyÖt §Ønh                                     --
----------------------------------------------------------------------------------------------------
if (not tbAnUpgrade) then
    tbAnUpgrade = {}
end

tbAnUpgrade.tbPath = {
    ["ThiÕu L©m Hoµng Kim Ên"]   = {1099, "TuyÖt §Ønh ThiÕu L©m Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Thiªn V­¬ng Hoµng Kim Ên"] = {1100, "TuyÖt §Ønh Thiªn V­¬ng Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["§­êng M«n Hoµng Kim Ên"]   = {1101, "TuyÖt §Ønh §­êng M«n Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Ngò §éc Hoµng Kim Ên"]     = {1102, "TuyÖt §Ønh Ngò §éc Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Nga My Hoµng Kim Ên"]      = {1103, "TuyÖt §Ønh Nga My Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Thóy Yªn Hoµng Kim Ên"]    = {1104, "TuyÖt §Ønh Thóy Yªn Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["C¸i Bang Hoµng Kim Ên"]    = {1105, "TuyÖt §Ønh C¸i Bang Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Thiªn NhÉn Hoµng Kim Ên"]  = {1106, "TuyÖt §Ønh Thiªn NhÉn Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["Vâ §ang Hoµng Kim Ên"]     = {1107, "TuyÖt §Ønh Vâ §ang Hoµng Kim Ên", 500, 50, 50, 100000000},
    ["C«n L«n Hoµng Kim Ên"]     = {1108, "TuyÖt §Ønh C«n L«n Hoµng Kim Ên", 500, 50, 50, 100000000},
}
tbAnUpgrade.KLH  = {4, 2045, 1}
tbAnUpgrade.TDTT = {4, 2054, 1}
tbAnUpgrade.HKL  = {6, 1, 4908}

function tbAnUpgrade:ThieuLam()   self:ConfirmUpgrade("ThiÕu L©m Hoµng Kim Ên") end
function tbAnUpgrade:ThienVuong() self:ConfirmUpgrade("Thiªn V­¬ng Hoµng Kim Ên") end
function tbAnUpgrade:DuongMon()   self:ConfirmUpgrade("§­êng M«n Hoµng Kim Ên") end
function tbAnUpgrade:NguDoc()     self:ConfirmUpgrade("Ngò §éc Hoµng Kim Ên") end
function tbAnUpgrade:NgaMy()      self:ConfirmUpgrade("Nga My Hoµng Kim Ên") end
function tbAnUpgrade:ThuyYen()    self:ConfirmUpgrade("Thóy Yªn Hoµng Kim Ên") end
function tbAnUpgrade:CaiBang()    self:ConfirmUpgrade("C¸i Bang Hoµng Kim Ên") end
function tbAnUpgrade:ThienNhan()  self:ConfirmUpgrade("Thiªn NhÉn Hoµng Kim Ên") end
function tbAnUpgrade:VoDang()     self:ConfirmUpgrade("Vâ §ang Hoµng Kim Ên") end
function tbAnUpgrade:ConLon()     self:ConfirmUpgrade("C«n L«n Hoµng Kim Ên") end

function tbAnUpgrade:ConfirmUpgrade(szAnName)
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local tbData = self.tbPath[szAnName]
        if (not tbData) then return end
        local szNextName = tbData[2]
        local szMsg = "N©ng cÊp <color=yellow>"..szAnName.."<color> lªn <color=green>"..szNextName.."<color> cÇn cã\n" ..
                    "- Kim Lo¹i HiÕm: <color=gold>"..tbData[3].." c¸i<color>\n" ..
                    "- TuyÖt §Ønh Tri Thøc: <color=gold>"..tbData[4].." quyÓn<color>\n" ..
                    "- Hoµng Kim LÖnh: <color=gold>"..tbData[5].." c¸i<color>\n" ..
                    "- Ng©n l­îng: <color=gold>"..(tbData[6]/10000).." v¹n<color>\n" ..
                    "Ng­¬i muèn n©ng cÊp ch­a?";
        local tbOpt = {
            {"Ta ®· mang ®ñ, n©ng cÊp ngay", self.OpenUpgradeUI, {self, szAnName}},
            {"§Ó ta suy nghÜ l¹i/cancel"},
        }
        CreateNewSayEx(szMsg, tbOpt)
    end
end

function tbAnUpgrade:OpenUpgradeUI(szAnName)
    local tbData = self.tbPath[szAnName]
    local szTitle = "N©ng CÊp: "..szAnName
    local szDescription = "§Æt vµo:<enter>- Ên Hoµng Kim<enter>- "..tbData[3].." Kim Lo¹i HiÕm<enter>- "..tbData[4].." TuyÖt §Ønh Tri Thøc<enter>- "..tbData[5].." Hoµng Kim LÖnh."
    g_GiveItemUI(szTitle, szDescription, {self.OnConfirm, {self, szAnName}}, nil, 1)
end

function tbAnUpgrade:OnConfirm(szAnName, nNum)
    if (nNum <= 0) then return 0 end
    local tbData = self.tbPath[szAnName]
    local nAnIdx = -1
    local nKLHCount = 0
    local nTDTTCount = 0
    local nHKLCount = 0
    local tbItems = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szName = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szName == szAnName) then
                nAnIdx = nIdx
            elseif (g == self.KLH[1] and d == self.KLH[2] and p == self.KLH[3]) then
                nKLHCount = nKLHCount + nStack
            elseif (g == self.TDTT[1] and d == self.TDTT[2] and p == self.TDTT[3]) then
                nTDTTCount = nTDTTCount + nStack
            elseif (g == self.HKL[1] and d == self.HKL[2] and p == self.HKL[3]) then
                nHKLCount = nHKLCount + nStack
            else
                Talk(1, "", "VËt phÈm <color=red>"..szName.."<color> kh«ng ph¶i nguyªn liÖu n©ng cÊp!")
                return 0
            end
            tinsert(tbItems, nIdx)
        end
    end
    if (nAnIdx == -1) then
        Talk(1, "", "B¹n ph¶i bá vµo <color=yellow>"..szAnName.."<color>!")
        return 0
    end
    if (nKLHCount < tbData[3] or nTDTTCount < tbData[4] or nHKLCount < tbData[5]) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu yªu cÇu!")
        return 0
    end
    if (GetCash() < tbData[6]) then
        Talk(1, "", "Kh«ng ®ñ ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItems) do RemoveItemByIndex(tbItems[i]) end
    Pay(tbData[6])
    local NewItemIdx = AddGoldItem(0, tbData[1])
    SyncItem(NewItemIdx)
    Msg2Player("Chóc mõng b¹n n©ng cÊp thµnh c«ng <color=yellow>"..tbData[2].."<color>!")
    Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=yellow>"..tbData[2].."<color> uy chÊn giang hå!")
    return 1
end