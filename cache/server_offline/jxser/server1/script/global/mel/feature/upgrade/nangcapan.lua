IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--                                          N©ng CÊp Ên                                           --
----------------------------------------------------------------------------------------------------
TB_UPGRADE_CONFIG = {
    [1]  = { name = "S¬ CÊp",       klh = 100, akt = 10, money = 10000000, targetLvl = 1,  reqLvl = 1, reqSect = 0 },
    [5]  = { name = "Trung CÊp",    klh = 200, akt = 20, money = 20000000, targetLvl = 5,  reqLvl = 1, reqSect = "current" },
    [10] = { name = "Cao CÊp",      klh = 300, akt = 30, money = 30000000, targetLvl = 10, reqLvl = 5, reqSect = "current" }
}
TB_SECTS = {
    {"ThiÕu L©m", 1, 0},
    {"Thiªn V­¬ng", 2, 0},
    {"§­êng M«n", 3, 1},
    {"Ngò §éc", 4, 1},
    {"Nga My", 5, 2},
    {"Thóy Yªn", 6, 2},
    {"C¸i Bang", 7, 3},
    {"Thiªn NhÉn", 8, 3},
    {"Vâ §ang", 9, 4},
    {"C«n L«n", 10, 4}
}
nSelSectID = 0
szSelSectName = ""
nSelSeriesID = 0
nSelTier = 0

function nangcapan()
    local tbSay = {"H·y chän lo¹i Ên cÇn n©ng cÊp.<enter>Chän ®óng <color=Green>M«n Ph¸i<color><enter>NÕu chän sai hËu qu¶ tù chÞu!"}
    for i=1, getn(TB_SECTS) do
        tinsert(tbSay, "N©ng cÊp "..TB_SECTS[i][1].." Ên/SelectSect_"..i)
    end
    tinsert(tbSay, "§Ó t«i suy nghÜ thªm ®·./OnCancel")
    CreateTaskSay(tbSay)
end

function SectMenu()
    local tbSay = {
        "Chän cÊp ®é n©ng cÊp "..szSelSectName.." Ên:",
        "N©ng cÊp "..szSelSectName.." Ên - S¬ CÊp/Upgrade_Tier_1",
        "N©ng cÊp "..szSelSectName.." Ên - Trung CÊp/Upgrade_Tier_5",
        "N©ng cÊp "..szSelSectName.." Ên - Cao CÊp/Upgrade_Tier_10",
        "Quay l¹i/nangcapan"
    }
    CreateTaskSay(tbSay)
end

function ConfirmUpgrade()
    local cfg = TB_UPGRADE_CONFIG[nSelTier]
    local reqName = (nSelTier == 1) and "Ên T©n Thñ" or szSelSectName.." Ên - "..(nSelTier == 5 and "S¬ CÊp" or "Trung CÊp")
    local szMsg = format("§Ó n©ng cÊp <color=Green>%s Ên - %s<color>\nNguyªn liÖu cÇn cã:\n<color=Green>- %s\n- %d Kim Lo¹i HiÕm\n- %d Ên Kinh Th­\n- %d v¹n l­îng<color>\n<color=Yellow>L­u ý trong hµnh trang cã ®ñ vËt phÈm!<color>", szSelSectName, cfg.name, reqName, cfg.klh, cfg.akt, cfg.money/10000)
    local tbSay = {
        szMsg,
        "Ch¾c ch¾n råi/ActionUpgrade",
        "§Ó t«i suy nghÜ ®·/OnCancel"
    }
    CreateTaskSay(tbSay)
end

function ActionUpgrade()
    local cfg = TB_UPGRADE_CONFIG[nSelTier]
    if (cfg == nil) then return end
    local reqSect = (cfg.reqSect == "current") and nSelSectID or cfg.reqSect
    if CalcEquiproomItemCount(4, 2045, 1, 1) < cfg.klh then
        Say("Hµnh trang kh«ng ®ñ "..cfg.klh.." Kim Lo¹i HiÕm.", 0) return
    end
    if CalcEquiproomItemCount(0, 13, reqSect, cfg.reqLvl) < 1 then
        local reqName = (nSelTier == 1) and "Ên T©n Thñ" or "Ên cÊp tr­íc"
        Say("B¹n kh«ng cã "..reqName.." trong hµnh trang.", 0) return
    end
    if CalcEquiproomItemCount(4, 2056, 1, 1) < cfg.akt then
        Say("Hµnh trang kh«ng ®ñ "..cfg.akt.." Ên Kinh Th­.", 0) return
    end
    if GetCash() < cfg.money then
        Say("Kh«ng ®ñ ".. (cfg.money/10000) .." v¹n l­îng.", 0) return
    end
    Pay(cfg.money)
    ConsumeEquiproomItem(cfg.klh, 4, 2045, 1, 1)
    ConsumeEquiproomItem(1, 0, 13, reqSect, cfg.reqLvl)
    ConsumeEquiproomItem(cfg.akt, 4, 2056, 1, 1)
    local nIdx = AddItem(0, 13, nSelSectID, cfg.targetLvl, nSelSeriesID, 0, 0)
    SyncItem(nIdx)
    Msg2Player("<color=green>N©ng cÊp thµnh c«ng:<color> <color=yellow>"..szSelSectName.." Ên - "..cfg.name.."<color>")
end

function Upgrade_Tier_1() nSelTier = 1 ConfirmUpgrade() end
function Upgrade_Tier_5() nSelTier = 5 ConfirmUpgrade() end
function Upgrade_Tier_10() nSelTier = 10 ConfirmUpgrade() end

function SelectSect_1() nSelSectID = TB_SECTS[1][2] szSelSectName = TB_SECTS[1][1] nSelSeriesID = TB_SECTS[1][3] SectMenu() end
function SelectSect_2() nSelSectID = TB_SECTS[2][2] szSelSectName = TB_SECTS[2][1] nSelSeriesID = TB_SECTS[2][3] SectMenu() end
function SelectSect_3() nSelSectID = TB_SECTS[3][2] szSelSectName = TB_SECTS[3][1] nSelSeriesID = TB_SECTS[3][3] SectMenu() end
function SelectSect_4() nSelSectID = TB_SECTS[4][2] szSelSectName = TB_SECTS[4][1] nSelSeriesID = TB_SECTS[4][3] SectMenu() end
function SelectSect_5() nSelSectID = TB_SECTS[5][2] szSelSectName = TB_SECTS[5][1] nSelSeriesID = TB_SECTS[5][3] SectMenu() end
function SelectSect_6() nSelSectID = TB_SECTS[6][2] szSelSectName = TB_SECTS[6][1] nSelSeriesID = TB_SECTS[6][3] SectMenu() end
function SelectSect_7() nSelSectID = TB_SECTS[7][2] szSelSectName = TB_SECTS[7][1] nSelSeriesID = TB_SECTS[7][3] SectMenu() end
function SelectSect_8() nSelSectID = TB_SECTS[8][2] szSelSectName = TB_SECTS[8][1] nSelSeriesID = TB_SECTS[8][3] SectMenu() end
function SelectSect_9() nSelSectID = TB_SECTS[9][2] szSelSectName = TB_SECTS[9][1] nSelSeriesID = TB_SECTS[9][3] SectMenu() end
function SelectSect_10() nSelSectID = TB_SECTS[10][2] szSelSectName = TB_SECTS[10][1] nSelSeriesID = TB_SECTS[10][3] SectMenu() end

function OnCancel() end

----------------------------------------------------------------------------------------------------
--                                     N©ng CÊp Ên Hoµng Kim                                      --
----------------------------------------------------------------------------------------------------
TB_UPGRADE_AN_HK = {
    [1]  = { szSect = "ThiÕu L©m",      token = 2046, szToken = "Kim Tinh Phï",  oldP = 1,  reward = 1088 },
    [2]  = { szSect = "Thiªn V­¬ng",    token = 2046, szToken = "Kim Tinh Phï",  oldP = 2,  reward = 1089 },
    [3]  = { szSect = "§­êng M«n",      token = 2047, szToken = "Méc Tinh Phï",  oldP = 3,  reward = 1090 },
    [4]  = { szSect = "Ngò §éc",        token = 2047, szToken = "Méc Tinh Phï",  oldP = 4,  reward = 1091 },
    [5]  = { szSect = "Nga My",         token = 2048, szToken = "Thñy Tinh Phï", oldP = 5,  reward = 1092 },
    [6]  = { szSect = "Thóy Yªn",       token = 2048, szToken = "Thñy Tinh Phï", oldP = 6,  reward = 1093 },
    [7]  = { szSect = "C¸i Bang",       token = 2049, szToken = "Háa Tinh Phï",  oldP = 7,  reward = 1094 },
    [8]  = { szSect = "Thiªn NhÉn",     token = 2049, szToken = "Háa Tinh Phï",  oldP = 8,  reward = 1095 },
    [9]  = { szSect = "Vâ §ang",        token = 2050, szToken = "Thæ Tinh Phï",  oldP = 9,  reward = 1096 },
    [10] = { szSect = "C«n L«n",        token = 2050, szToken = "Thæ Tinh Phï",  oldP = 10, reward = 1097 }
}
nIndexAnHK = 0
nAnMoney  = 50000000
nAnKLH    = 500
nAnKinhThu = 50
nAnHKLenh = 10

function nangcapanhoangkim()
    local tbSay = {
        "Ng­¬i muèn n©ng cÊp Ên <color=Yellow>Hoµng Kim<color>?<enter>H·y chän lo¹i Ên <color=Green>M«n Ph¸i<color> nhÐ.",
        "N©ng cÊp ThiÕu L©m Hoµng Kim Ên/anthieulamhoangkim",
        "N©ng cÊp Thiªn V­¬ng Hoµng Kim Ên/anthienvuonghoangkim",
        "N©ng cÊp §­êng M«n Hoµng Kim Ên/anduongmonhoangkim",
        "N©ng cÊp Ngò §éc Hoµng Kim Ên/anngudochoangkim",
        "N©ng cÊp Nga My Hoµng Kim Ên/anngamihoangkim",
        "N©ng cÊp Thóy Yªn Hoµng Kim Ên/anthuyyenhoangkim",
        "N©ng cÊp C¸i Bang Hoµng Kim Ên/ancaibanghoangkim",
        "N©ng cÊp Thiªn NhÉn Hoµng Kim Ên/anthiennhanhoangkim",
        "N©ng cÊp Vâ §ang Hoµng Kim Ên/anvodanghoangkim",
        "N©ng cÊp C«n L«n Hoµng Kim Ên/anconlonhoangkim",
        "§Ó t«i suy nghÜ thªm ®·./OnCancel"
    }
    CreateTaskSay(tbSay)
end

function ConfirmAnHK()
    local cfg = TB_UPGRADE_AN_HK[nIndexAnHK]
    if (cfg == nil) then return end
    
    local szMsg = "§Ó n©ng cÊp <color=Yellow>"..cfg.szSect.." Hoµng Kim Ên<color> cÇn cã:<enter>"..
                  "<color=Green>- 1 Ên "..cfg.szSect.." (Cao CÊp)<enter>"..
                  "- 1 "..cfg.szToken.."<enter>"..
                  "- "..nAnKLH.." Kim Lo¹i HiÕm<enter>"..
                  "- "..nAnKinhThu.." Ên Kinh Th­<enter>"..
                  "- "..nAnHKLenh.." Hoµng Kim LÖnh<enter>"..
                  "- "..(nAnMoney/10000).." v¹n l­îng<color><enter>"..
                  "<color=Yellow>L­u ý h·y mang ®ñ vËt phÈm<color>"
    
    local tbSayAn = {
        szMsg,
        "TiÕn hµnh n©ng cÊp/ActionAnHK",
        "Quay l¹i/nangcapanhoangkim"
    }
    CreateTaskSay(tbSayAn)
end

function ActionAnHK()
    local cfg = TB_UPGRADE_AN_HK[nIndexAnHK]
    if (cfg == nil) then return end
    if CalcEquiproomItemCount(4, 2045, 1, 1) < nAnKLH then
        Say("Hµnh trang kh«ng ®ñ "..nAnKLH.." Kim Lo¹i HiÕm.", 0) return
    end
    if CalcEquiproomItemCount(4, cfg.token, 1, 1) < 1 then
        Say("B¹n kh«ng cã "..cfg.szToken.." trong hµnh trang.", 0) return
    end
    if CalcEquiproomItemCount(4, 2056, 1, 1) < nAnKinhThu then
        Say("Hµnh trang kh«ng ®ñ "..nAnKinhThu.." Ên Kinh Th­.", 0) return
    end
    if CalcEquiproomItemCount(6, 1, 4908, -1) < nAnHKLenh then
        Say("Hµnh trang kh«ng ®ñ "..nAnHKLenh.." Hoµng Kim LÖnh.", 0) return
    end
    if CalcEquiproomItemCount(0, 13, cfg.oldP, 10) < 1 then
        Say("B¹n kh«ng cã Ên "..cfg.szSect.." Cao CÊp trong hµnh trang.", 0) return
    end
    if GetCash() < nAnMoney then
        Say("Kh«ng ®ñ "..(nAnMoney/10000).." v¹n l­îng.", 0) return
    end
    if CalcFreeItemCellCount() < 1 then
        Say("Hµnh trang kh«ng ®ñ chç trèng.", 0) return
    end
    Pay(nAnMoney)
    ConsumeEquiproomItem(nAnKLH, 4, 2045, 1, 1)
    ConsumeEquiproomItem(1, 4, cfg.token, 1, 1)
    ConsumeEquiproomItem(nAnKinhThu, 4, 2056, 1, 1)
    ConsumeEquiproomItem(nAnHKLenh, 6, 1, 4908, -1)
    ConsumeEquiproomItem(1, 0, 13, cfg.oldP, 10)
    local nIdx = AddGoldItem(0, cfg.reward)
    if (nIdx > 0) then
        SyncItem(nIdx)
        Msg2Player("<color=green>N©ng cÊp thµnh c«ng:<color> <color=yellow>"..cfg.szSect.." Hoµng Kim Ên<color>")
        AddLocalNews("Chóc mõng <color=cyan>"..GetName().."<color> n©ng cÊp thµnh c«ng <color=yellow>"..cfg.szSect.." Hoµng Kim Ên<color>")
    end
end

function anthieulamhoangkim() nIndexAnHK = 1 ConfirmAnHK() end
function anthienvuonghoangkim() nIndexAnHK = 2 ConfirmAnHK() end
function anduongmonhoangkim() nIndexAnHK = 3 ConfirmAnHK() end
function anngudochoangkim() nIndexAnHK = 4 ConfirmAnHK() end
function anngamihoangkim() nIndexAnHK = 5 ConfirmAnHK() end
function anthuyyenhoangkim() nIndexAnHK = 6 ConfirmAnHK() end
function ancaibanghoangkim() nIndexAnHK = 7 ConfirmAnHK() end
function anthiennhanhoangkim() nIndexAnHK = 8 ConfirmAnHK() end
function anvodanghoangkim() nIndexAnHK = 9 ConfirmAnHK() end
function anconlonhoangkim() nIndexAnHK = 10 ConfirmAnHK() end