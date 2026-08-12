IncludeLib("ITEM")
Include("\\script\\dailogsys\\g_dialog.lua")

----------------------------------------------------------------------------------------------------
--                                       TuyÖt §Ønh Vò KhÝ                                        --
----------------------------------------------------------------------------------------------------
if (not tbUltimateUpgrade) then
    tbUltimateUpgrade = {}
end

tbUltimateUpgrade.nReqMetal = 500
tbUltimateUpgrade.nReqTDTT  = 50
tbUltimateUpgrade.nReqStone = 50
tbUltimateUpgrade.nReqMoney = 100000000
tbUltimateUpgrade.Metal = {4, 2045, 1}
tbUltimateUpgrade.TDTT  = {4, 2054, 1}
tbUltimateUpgrade.Stone = {6, 1, 4959}
tbUltimateUpgrade.Data = {
    ["ThiÕu L©m"] = {
        {
            szBranch = "QuyÒn",
            SkillBook = {6, 1, 56},
            TargetID = 1119,
            TargetName = "TuyÖt §Ønh §¹t Ma TriÒn Thñ",
            SetItems = {
                "Méng Long ChÝnh Hång T¨ng M·o",
                "Méng Long Kim Ti ChÝnh Hång Cµ Sa",
                "Méng Long HuyÒn Ti Ph¸t §¸i",
                "Méng Long PhËt Ph¸p HuyÒn Béi",
                "Méng Long §¹t Ma T¨ng Hµi"
            }
        },
        {
            szBranch = "Bæng",
            SkillBook = {6, 1, 57},
            TargetID = 1120,
            TargetName = "TuyÖt §Ønh Hoµnh T¶o C«n",
            SetItems = {
                "Phôc Ma Tö Kim C«n",
                "Phôc Ma HuyÒn Hoµng Cµ Sa",
                "Phôc Ma ¤ Kim NhuyÔn §iÒu",
                "Phôc Ma PhËt T©m NhuyÔn KhÊu",
                "Phôc Ma Phæ §é T¨ng Hµi"
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 58},
            TargetID = 1121,
            TargetName = "TuyÖt §Ønh V« T­íng §ao",
            SetItems = {
                "Tø Kh«ng Gi¸ng Ma Giíi §ao",
                "Tø Kh«ng Tö Kim Cµ Sa",
                "Tø Kh«ng Hé ph¸p Yªu §¸i",
                "Tø Kh«ng NhuyÔn B× Hé UyÓn",
                "Tø Kh«ng Giíi LuËt Ph¸p Giíi"
            }
        }
    },
    ["Thiªn V­¬ng"] = {
        {
            szBranch = "Chïy",
            SkillBook = {6, 1, 37},
            TargetID = 1122,
            TargetName = "TuyÖt §Ønh Truy Phong Chïy",
            SetItems = {
                "H¸m Thiªn Kim Hoµn §¹i Nh·n ThÇn Chïy",
                "H¸m Thiªn Vò ThÇn T­¬ng Kim Gi¸p",
                "H¸m Thiªn Uy Vò Thóc Yªu §¸i",
                "H¸m Thiªn Hæ ®Çu KhÈn Thóc UyÓn",
                "H¸m Thiªn Thõa Long ChiÕn Ngoa"
            }
        },
        {
            szBranch = "Th­¬ng",
            SkillBook = {6, 1, 38},
            TargetID = 1123,
            TargetName = "TuyÖt §Ønh Truy Tinh Th­¬ng",
            SetItems = {
                "KÕ NghiÖp B«n L«i Toµn Long Th­¬ng",
                "KÕ NghiÖp HuyÒn Vò Hoµng Kim Kh¶i",
                "KÕ NghiÖp B¹ch Hæ V« Song KhÊu",
                "KÕ NghiÖp Háa V©n Kú L©n Thñ",
                "KÕ NghiÖp Chu T­íc L¨ng V©n Ngoa"
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 39},
            TargetID = 1124,
            TargetName = "TuyÖt §Ønh Ph¸ Thiªn §ao",
            SetItems = {
                "Ngù Long L­îng Ng©n B¶o §ao",
                "Ngù Long ChiÕn ThÇn Phi Qu¶i Gi¸p",
                "Ngù Long Thiªn M«n Thóc Yªu Hoµn",
                "Ngù Long TÊn Phong Hé UyÓn",
                "Ngù Long TuyÖt MÖnh ChØ Hoµn"
            }
        }
    },
    ["Nga My"] = {
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 42},
            TargetID = 1125,
            TargetName = "TuyÖt §Ønh Tam Nga KiÕm",
            SetItems = {
                "V« Gian û Thiªn KiÕm",
                "V« Gian Thanh Phong Truy Y",
                "V« Gian PhÊt V©n Ti §¸i",
                "V« Gian CÇm VËn Hé UyÓn",
                "V« Gian B¹ch Ngäc Bµn ChØ"
            }
        },
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 43},
            TargetID = 1126,
            TargetName = "TuyÖt §Ønh Phong S­¬ng TriÒn Thñ",
            SetItems = {
                "V« Ma Ma Ni Qu¸n",
                "V« Ma Tö Kh©m Cµ Sa",
                "V« Ma B¨ng Tinh ChØ Hoµn",
                "V« Ma TÈy T­îng Ngäc KhÊu",
                "V« Ma Hång Truy NhuyÔn Th¸p Hµi"
            }
        }
    },
    ["Thóy Yªn"] = {
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 40},
            TargetID = 1127,
            TargetName = "TuyÖt §Ønh B¨ng Tung §ao",
            SetItems = {
                "Tª Hoµng Phông Nghi §ao",
                "Tª Hoµng TuÖ T©m Khinh Sa Y",
                "Tª Hoµng Phong TuyÕt B¹ch V©n Thóc §¸i",
                "Tª Hoµng B¨ng Tung CÈm UyÓn",
                "Tª Hoµng Thóy Ngäc ChØ Hoµn"
            }
        },
        {
            szBranch = "Song §ao",
            SkillBook = {6, 1, 41},
            TargetID = 1128,
            TargetName = "TuyÖt §Ønh B¨ng T©m Song §ao",
            SetItems = {
                "BÝch H¶i Uyªn ¦¬ng Liªn Hoµn §ao",
                "BÝch H¶i Hoµn Ch©u Vò Liªn",
                "BÝch H¶i Hång Linh Kim Ti §¸i",
                "BÝch H¶i Hång L¨ng Ba",
                "BÝch H¶i Khiªn TÕ ChØ Hoµn"
            }
        }
    },
    ["Ngò §éc"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 47},
            TargetID = 1129,
            TargetName = "TuyÖt §Ønh ¢m Phong TriÒn Thñ",
            SetItems = {
                "U Lung Kim Xµ Ph¸t §¸i",
                "U Lung XÝch YÕt MËt Trang",
                "U Lung Thanh Ng« TriÒn Yªu",
                "U Lung Ng©n ThÒm Hé UyÓn",
                "U Lung MÆc Thï NhuyÔn Lý"
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 48},
            TargetID = 1130,
            TargetName = "TuyÖt §Ønh HuyÒn ¢m §ao",
            SetItems = {
                "Minh ¶o Tµ S¸t §éc NhËn",
                "Minh ¶o U §éc ¸m Y",
                "Minh ¶o §éc YÕt ChØ Hoµn",
                "Minh ¶o Hñ Cèt Hé UyÓn",
                "Minh ¶o Song Hoµn Xµ Hµi"
            }
        }
    },
    ["§­êng M«n"] = {
        {
            szBranch = "Phi §ao",
            SkillBook = {6, 1, 45},
            TargetID = 1131,
            TargetName = "TuyÖt §Ønh NhiÕp Hån Phi §ao",
            SetItems = {
                "B¨ng Hµn §¬n ChØ Phi §ao",
                "B¨ng Hµn HuyÒn Y Thóc Gi¸p",
                "B¨ng Hµn T©m TiÔn Yªu KhÊu",
                "B¨ng Hµn HuyÒn Thiªn B¨ng Háa Béi",
                "B¨ng Hµn NguyÖt ¶nh Ngoa"
            }
        },
        {
            szBranch = "Tô TiÔn",
            SkillBook = {6, 1, 27},
            TargetID = 1132,
            TargetName = "TuyÖt §Ønh B¹o Vò Tô TiÔn",
            SetItems = {
                "Thiªn Quang Hoa Vò M¹n Thiªn",
                "Thiªn Quang §Þnh T©m Ng­ng ThÇn Phï",
                "Thiªn Quang S©m La Thóc §¸i",
                "Thiªn Quang Song B¹o Hµn ThiÕt Tr¹c",
                "Thiªn Quang Thóc Thiªn Ph­îc §Þa Hoµn"
            }
        },
        {
            szBranch = "Phi Tiªu",
            SkillBook = {6, 1, 46},
            TargetID = 1133,
            TargetName = "TuyÖt §Ønh Cöu Cung Phi Tiªu",
            SetItems = {
                "S©m Hoang Phi Tinh §o¹t Hån",
                "S©m Hoang Kim TiÒn Liªn Hoµn Gi¸p",
                "S©m Hoang Hån Gi¶o Yªu Thóc",
                "S©m Hoang HuyÒn ThiÕt T­¬ng Ngäc Béi",
                "S©m Hoang Tinh VÉn Phi Lý"
            }
        }
    },
    ["C¸i Bang"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 54},
            TargetID = 1134,
            TargetName = "TuyÖt §Ønh Phi Long TriÒn Thñ",
            SetItems = {
                "§ång Cõu Phi Long §Çu Hoµn",
                "§ång Cõu Gi¸ng Long C¸i Y",
                "§ång Cõu TiÒm Long Yªu §¸i",
                "§ång Cõu Kh¸ng Long Hé UyÓn",
                "§ång Cõu KiÕn Long Ban ChØ"
            }
        },
        {
            szBranch = "Bæng",
            SkillBook = {6, 1, 55},
            TargetID = 1135,
            TargetName = "TuyÖt §Ønh Thiªn H¹ Bæng",
            SetItems = {
                "§Þch Kh¸i Lôc Ngäc Tr­îng",
                "§Þch Kh¸i Cöu §¹i C¸i Y",
                "§Þch Kh¸i TriÒn M·ng Yªu §¸i",
                "§Þch Kh¸i CÈu TÝch B× Hé UyÓn",
                "§Þch Kh¸i Th¶o Gian Th¹ch Giíi"
            }
        }
    },
    ["Thiªn NhÉn"] = {
        {
            szBranch = "Th­¬ng",
            SkillBook = {6, 1, 35},
            TargetID = 1136,
            TargetName = "TuyÖt §Ønh V©n Long Th­¬ng",
            SetItems = {
                "Ma S¸t Quû Cèc U Minh Th­¬ng",
                "Ma S¸t Tµn D­¬ng ¶nh HuyÕt Gi¸p",
                "Ma S¸t XÝch Ký Táa Yªu KhÊu",
                "Ma S¸t Cö Háa Liªu Thiªn UyÓn",
                "Ma S¸t V©n Long Thæ Ch©u Giíi"
            }
        },
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 36},
            TargetID = 1137,
            TargetName = "TuyÖt §Ønh Thiªn Ngo¹i §ao",
            SetItems = {
                "Ma ThÞ LiÖt DiÖm Qu¸n MiÖn",
                "Ma ThÞ LÖ Ma PhÖ T©m Liªn",
                "Ma ThÞ NghiÖp Háa U Minh Giíi",
                "Ma ThÞ HuyÕt Ngäc ThÊt S¸t Béi",
                "Ma ThÞ S¬n H¶i Phi Hång Lý"
            }
        }
    },
    ["Vâ §ang"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 33},
            TargetID = 1138,
            TargetName = "TuyÖt §Ønh Thiªn §Þa KiÕm",
            SetItems = {
                "L¨ng Nh¹c Th¸i Cùc KiÕm",
                "L¨ng Nh¹c V« Ng· §¹o Bµo",
                "L¨ng Nh¹c Né L«i Giíi",
                "L¨ng Nh¹c V« Cùc HuyÒn Ngäc Béi",
                "L¨ng Nh¹c Thiªn §Þa HuyÒn Hoµng Giíi"
            }
        },
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 34},
            TargetID = 1139,
            TargetName = "TuyÖt §Ønh Nh©n KiÕm KiÕm",
            SetItems = {
                "CËp Phong Ch©n Vò KiÕm",
                "CËp Phong Tam Thanh Phï",
                "CËp Phong HuyÒn Ti Tam §o¹n CÈm",
                "CËp Phong Thóy Ngäc HuyÒn Hoµng Béi",
                "CËp Phong Thanh Tïng Ph¸p Giíi"
            }
        }
    },
    ["C«n L«n"] = {
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 50},
            TargetID = 1140,
            TargetName = "TuyÖt §Ønh Ng¹o TuyÕt §ao",
            SetItems = {
                "S­¬ng Tinh Thiªn Niªn Hµn ThiÕt",
                "S­¬ng Tinh Ng¹o S­¬ng §¹o Bµo",
                "S­¬ng Tinh Thanh Phong Lò §¸i",
                "S­¬ng Tinh Thiªn Tinh B¨ng Tinh Thñ",
                "S­¬ng Tinh Phong B¹o ChØ Hoµn"
            }
        },
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 51},
            TargetID = 1141,
            TargetName = "TuyÖt §Ønh L«i §éng KiÕm",
            SetItems = {
                "L«i Khung Hµn Tung B¨ng B¹ch Quan",
                "L«i Khung Thiªn §Þa Hé Phï",
                "L«i Khung Phong L«i Thanh CÈm §¸i",
                "L«i Khung Linh Ngäc UÈn L«i",
                "L«i Khung Cöu Thiªn DÉn L«i Giíi"
            }
        }
    }
}

function tbUltimateUpgrade:SelectSect()
    local szMsg = "<npc><enter>Ng­¬i muèn n©ng cÊp Vò KhÝ <color=gold>TuyÖt §Ønh<color> nµo?"
    local tbOpt = {
        {"ThiÕu L©m", self.SelectBranch, {self, "ThiÕu L©m"}},
        {"Thiªn V­¬ng", self.SelectBranch, {self, "Thiªn V­¬ng"}},
        {"Nga My", self.SelectBranch, {self, "Nga My"}},
        {"Thóy Yªn", self.SelectBranch, {self, "Thóy Yªn"}},
        {"Ngò §éc", self.SelectBranch, {self, "Ngò §éc"}},
        {"§­êng M«n", self.SelectBranch, {self, "§­êng M«n"}},
        {"C¸i Bang", self.SelectBranch, {self, "C¸i Bang"}},
        {"Thiªn NhÉn", self.SelectBranch, {self, "Thiªn NhÉn"}},
        {"Vâ §ang", self.SelectBranch, {self, "Vâ §ang"}},
        {"C«n L«n", self.SelectBranch, {self, "C«n L«n"}},
        {"KÕt thóc/cancel"}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUltimateUpgrade:SelectBranch(szSect)
    local tbSectData = self.Data[szSect]
    if (not tbSectData) then return end
    local szMsg = "<npc>HÖ ph¸i "..szSect.." rÊt ®a d¹ng<enter>Ng­¬i muèn n©ng cÊp hÖ trang bÞ nµo?"
    local tbOpt = {}
    for i = 1, getn(tbSectData) do
        tinsert(tbOpt, {"HÖ "..tbSectData[i].szBranch, self.ConfirmUpgrade, {self, szSect, i}})
    end
    tinsert(tbOpt, {"KÕt thóc/cancel"})
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUltimateUpgrade:ConfirmUpgrade(szSect, nBranchIdx)
    local tbBranchData = self.Data[szSect][nBranchIdx]
    local szMsg = "<npc><enter>Vò khÝ: <color=yellow>"..tbBranchData.TargetName.."<color> yªu cÇu:\n" ..
                  "- <color=green>Trän bé Hoµng Kim M«n Ph¸i hÖ "..tbBranchData.szBranch.."<color>\n" ..
                  "  <color=orange>(L­u ý kh«ng cÇn TrÊn Bang Chi B¶o)<color>\n" ..
                  "- <color=green>BÝ KÝp "..szSect.." hÖ "..tbBranchData.szBranch.."<color>\n" ..
                  "- <color=green>"..self.nReqMetal.." Kim Lo¹i HiÕm<color>\n" ..
                  "- <color=green>"..self.nReqTDTT.." TuyÖt §Ønh Tri Thøc<color>\n" ..
                  "- <color=green>"..self.nReqStone.." TuyÖt §Ønh Th¹ch<color>\n" ..
                  "- <color=green>10000 v¹n l­îng Ng©n l­îng.<color>"
    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng cÊp th«i nµo!", self.OpenUI, {self, szSect, nBranchIdx}},
        {"Quay l¹i", self.SelectBranch, {self, szSect}}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbUltimateUpgrade:OpenUI(szSect, nBranchIdx)
    local szName = self.Data[szSect][nBranchIdx].TargetName
    g_GiveItemUI("N©ng CÊp: "..szName, "Bá vµo:<enter>- Bé Hoµng Kim M«n Ph¸i<enter>- BÝ kÝp theo hÖ<enter>- 500 Kim Lo¹i HiÕm<enter>- 50 TuyÖt §Ønh Tri Thøc<enter>- 50 TuyÖt §Ønh Th¹ch", {self.OnConfirm, {self, szSect, nBranchIdx}}, nil, 1)
end

function tbUltimateUpgrade:OnConfirm(szSect, nBranchIdx, nNum)
    if (nNum <= 0) then return 0 end
    local tbBranchData = self.Data[szSect][nBranchIdx]
    local tbSkillBook  = tbBranchData.SkillBook
    local nSetCount = 0
    local nBookCount, nMetalCount, nTDTTCount, nStoneCount = 0, 0, 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szNameInUI = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            for j = 1, 5 do
                if (szNameInUI == tbBranchData.SetItems[j]) then
                    nSetCount = nSetCount + 1
                    break
                end
            end
            if (g == tbSkillBook[1] and d == tbSkillBook[2] and p == tbSkillBook[3]) then
                nBookCount = nBookCount + nStack
            elseif (g == self.Metal[1] and d == self.Metal[2]) then
                nMetalCount = nMetalCount + nStack
            elseif (g == self.TDTT[1] and d == self.TDTT[2]) then
                nTDTTCount = nTDTTCount + nStack
            elseif (g == self.Stone[1] and d == self.Stone[2] and p == self.Stone[3]) then
                nStoneCount = nStoneCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nSetCount < 5) then
        Talk(1, "", "B¹n ph¶i bá vµo ®ñ trän bé <color=yellow>Hoµng Kim M«n Ph¸i<color> hÖ nµy!<enter>L­u ý: Kh«ng cÇn <color=orange>TrÊn Bang Chi B¶o<color>")
        return 0
    end
    if (nBookCount < 1 or nMetalCount < self.nReqMetal or nTDTTCount < self.nReqTDTT or nStoneCount < self.nReqStone or GetCash() < self.nReqMoney) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu hoÆc ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do RemoveItemByIndex(tbItemsToRemove[i]) end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, tbBranchData.TargetID)
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng<enter><color=yellow>"..tbBranchData.TargetName.."<color>!")
    end
    return 1
end