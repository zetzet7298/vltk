IncludeLib("ITEM")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--                                 TuyÖt §Ønh Hoµng Kim M«n Ph¸i                                  --
----------------------------------------------------------------------------------------------------
if (not tbHKUpgrade) then
    tbHKUpgrade = {}
end

tbHKUpgrade.nReqTDTT  = 50
tbHKUpgrade.nReqHKL   = 20
tbHKUpgrade.nReqMoney = 20000000
tbHKUpgrade.nReqBook  = 1
tbHKUpgrade.TDTT = {4, 2054, 1}
tbHKUpgrade.HKL  = {6, 1, 4908}
tbHKUpgrade.Data = {
    ["ThiÕu L©m"] = {
        {
            szBranch = "QuyÒn",
            SkillBook = {6, 1, 56},
            Items = {
                {"Méng Long ChÝnh Hång T¨ng M·o", 1, 905},
                {"Méng Long Kim Ti ChÝnh Hång Cµ Sa", 2, 906},
                {"Méng Long HuyÒn Ti Ph¸t §¸i", 3, 907},
                {"Méng Long PhËt Ph¸p HuyÒn Béi", 4, 908},
                {"Méng Long §¹t Ma T¨ng Hµi", 5, 909},
                {"[TrÊn Bang Chi B¶o] Méng Long Tö Kim B¸t Nh· Giíi", 769, 1045}
            }
        },
        {
            szBranch = "Bæng",
            SkillBook = {6, 1, 57},
            Items = {
                {"Phôc Ma Tö Kim C«n", 6, 910},
                {"Phôc Ma HuyÒn Hoµng Cµ Sa", 7, 911},
                {"Phôc Ma ¤ Kim NhuyÔn §iÒu", 8, 912},
                {"Phôc Ma PhËt T©m NhuyÔn KhÊu", 9, 913},
                {"Phôc Ma Phæ §é T¨ng Hµi", 10, 914},
                {"[TrÊn Bang Chi B¶o] Phôc Ma V« L­îng Kim Cang UyÓn", 771, 1046}
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 58},
            Items = {
                {"Tø Kh«ng Gi¸ng Ma Giíi §ao", 11, 915},
                {"Tø Kh«ng Tö Kim Cµ Sa", 12, 916},
                {"Tø Kh«ng Hé ph¸p Yªu §¸i", 13, 917},
                {"Tø Kh«ng NhuyÔn B× Hé UyÓn", 14, 918},
                {"Tø Kh«ng Giíi LuËt Ph¸p Giíi", 15, 919},
                {"[TrÊn Bang Chi B¶o] Tø Kh«ng §¹t Ma T¨ng Hµi", 776, 1047}
            }
        }
    },
    ["Thiªn V­¬ng"] = {
        {
            szBranch = "Chïy",
            SkillBook = {6, 1, 37},
            Items = {
                {"H¸m Thiªn Kim Hoµn §¹i Nh·n ThÇn Chïy", 16, 920},
                {"H¸m Thiªn Vò ThÇn T­¬ng Kim Gi¸p", 17, 921},
                {"H¸m Thiªn Uy Vò Thóc Yªu §¸i", 18, 922},
                {"H¸m Thiªn Hæ ®Çu KhÈn Thóc UyÓn", 19, 923},
                {"H¸m Thiªn Thõa Long ChiÕn Ngoa", 20, 924}
            }
        },
        {
            szBranch = "Th­¬ng",
            SkillBook = {6, 1, 38},
            Items = {
                {"KÕ NghiÖp B«n L«i Toµn Long Th­¬ng", 21, 925},
                {"KÕ NghiÖp HuyÒn Vò Hoµng Kim Kh¶i", 22, 926},
                {"KÕ NghiÖp B¹ch Hæ V« Song KhÊu", 23, 927},
                {"KÕ NghiÖp Háa V©n Kú L©n Thñ", 24, 928},
                {"KÕ NghiÖp Chu T­íc L¨ng V©n Ngoa", 25, 929}
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 39},
            Items = {
                {"Ngù Long L­îng Ng©n B¶o §ao", 26, 930},
                {"Ngù Long ChiÕn ThÇn Phi Qu¶i Gi¸p", 27, 931},
                {"Ngù Long Thiªn M«n Thóc Yªu Hoµn", 28, 932},
                {"Ngù Long TÊn Phong Hé UyÓn", 29, 933},
                {"Ngù Long TuyÖt MÖnh ChØ Hoµn", 30, 934},
                {"[TrÊn Bang Chi B¶o] Ngù Long TÊn Phong Ph¸t C¬", 793, 1048}
            }
        }
    },
    ["Nga My"] = {
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 42},
            Items = {
                {"V« Gian û Thiªn KiÕm", 31, 935},
                {"V« Gian Thanh Phong Truy Y", 32, 936},
                {"V« Gian PhÊt V©n Ti §¸i", 33, 937},
                {"V« Gian CÇm VËn Hé UyÓn", 34, 938},
                {"V« Gian B¹ch Ngäc Bµn ChØ", 35, 939},
                {"[TrÊn Bang Chi B¶o] V« Gian Thanh Phong NhuyÔn KÞch", 796, 1049}
            }
        },
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 43},
            Items = {
                {"V« Ma Ma Ni Qu¸n", 36, 940},
                {"V« Ma Tö Kh©m Cµ Sa", 37, 941},
                {"V« Ma B¨ng Tinh ChØ Hoµn", 38, 942},
                {"V« Ma TÈy T­îng Ngäc KhÊu", 39, 943},
                {"V« Ma Hång Truy NhuyÔn Th¸p Hµi", 40, 944},
                {"[TrÊn Bang Chi B¶o] V« Ma Thu Thñy L­u Quang §¸i", 801, 1050}
            }
        },
        {
            szBranch = "Hç Trî",
            SkillBook = {6, 1, 59},
            Items = {
                {"V« TrÇn Ngäc N÷ Tè T©m Qu¸n", 41, 945},
                {"V« TrÇn Thanh T©m H­íng ThiÖn Ch©u", 42, 946},
                {"V« TrÇn Tõ Bi Ngäc Ban ChØ", 43, 947},
                {"V« TrÇn PhËt T©m Tõ H÷u Yªu Phèi", 44, 948},
                {"V« TrÇn PhËt Quang ChØ Hoµn", 45, 949},
                {"[TrÊn Bang Chi B¶o] V« TrÇn TÞnh ¶nh L­u T«", 808, 1051}
            }
        }
    },
    ["Thóy Yªn"] = {
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 40},
            Items = {
                {"Tª Hoµng Phông Nghi §ao", 46, 950},
                {"Tª Hoµng TuÖ T©m Khinh Sa Y", 47, 951},
                {"Tª Hoµng Phong TuyÕt B¹ch V©n Thóc §¸i", 48, 952},
                {"Tª Hoµng B¨ng Tung CÈm UyÓn", 49, 953},
                {"Tª Hoµng Thóy Ngäc ChØ Hoµn", 50, 954},
                {"[TrÊn Bang Chi B¶o] Tª Hoµng HuÖ T©m Tr­êng Sinh KhÊu", 811, 1052}
            }
        },
        {
            szBranch = "Song §ao",
            SkillBook = {6, 1, 41},
            Items = {
                {"BÝch H¶i Uyªn ¦¬ng Liªn Hoµn §ao", 51, 955},
                {"BÝch H¶i Hoµn Ch©u Vò Liªn", 52, 956},
                {"BÝch H¶i Hång Linh Kim Ti §¸i", 53, 957},
                {"BÝch H¶i Hång L¨ng Ba", 54, 958},
                {"BÝch H¶i Khiªn TÕ ChØ Hoµn", 55, 959},
                {"[TrÊn Bang Chi B¶o] BÝch H¶i Hoµn Ch©u Tuyªn Thanh C©n", 816, 1053}
            }
        }
    },
    ["Ngò §éc"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 47},
            Items = {
                {"U Lung Kim Xµ Ph¸t §¸i", 56, 960},
                {"U Lung XÝch YÕt MËt Trang", 57, 961},
                {"U Lung Thanh Ng« TriÒn Yªu", 58, 962},
                {"U Lung Ng©n ThÒm Hé UyÓn", 59, 963},
                {"U Lung MÆc Thï NhuyÔn Lý", 60, 964}
            }
        },
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 48},
            Items = {
                {"Minh ¶o Tµ S¸t §éc NhËn", 61, 965},
                {"Minh ¶o U §éc ¸m Y", 62, 966},
                {"Minh ¶o §éc YÕt ChØ Hoµn", 63, 967},
                {"Minh ¶o Hñ Cèt Hé UyÓn", 64, 968},
                {"Minh ¶o Song Hoµn Xµ Hµi", 65, 969},
                {"[TrÊn Bang Chi B¶o] Minh ¶o Song Hoµn Xµ KhÊu", 829, 1054}
            }
        },
        {
            szBranch = "Bïa",
            SkillBook = {6, 1, 49},
            Items = {
                {"Chó Ph­îc Ph¸ gi¸p §Çu Hoµn", 66, 970},
                {"Chó Ph­îc DiÖt L«i C¶nh Phï", 67, 971},
                {"Chó Ph­îc U ¶o ChØ Hoµn", 68, 972},
                {"Chó Ph­îc Xuyªn T©m §éc UyÓn", 69, 973},
                {"Chó Ph­îc B¨ng Háa Thùc Cèt Ngoa", 70, 974},
                {"[TrÊn Bang Chi B¶o] Chó Ph­îc Trïng Cèt Ngäc Béi", 834, 1055}
            }
        }
    },
    ["§­êng M«n"] = {
        {
            szBranch = "Phi §ao",
            SkillBook = {6, 1, 45},
            Items = {
                {"B¨ng Hµn §¬n ChØ Phi §ao", 71, 975},
                {"B¨ng Hµn HuyÒn Y Thóc Gi¸p", 72, 976},
                {"B¨ng Hµn T©m TiÔn Yªu KhÊu", 73, 977},
                {"B¨ng Hµn HuyÒn Thiªn B¨ng Háa Béi", 74, 978},
                {"B¨ng Hµn NguyÖt ¶nh Ngoa", 75, 979}
            }
        },
        {
            szBranch = "Tô TiÔn",
            SkillBook = {6, 1, 27},
            Items = {
                {"Thiªn Quang Hoa Vò M¹n Thiªn", 76, 980},
                {"Thiªn Quang §Þnh T©m Ng­ng ThÇn Phï", 77, 981},
                {"Thiªn Quang S©m La Thóc §¸i", 78, 982},
                {"Thiªn Quang Song B¹o Hµn ThiÕt Tr¹c", 79, 983},
                {"Thiªn Quang Thóc Thiªn Ph­îc §Þa Hoµn", 80, 984},
                {"[TrÊn Bang Chi B¶o] Thiªn Quang §Þa Hµnh Thiªn Lý Ngoa", 843, 1056}
            }
        },
        {
            szBranch = "Phi Tiªu",
            SkillBook = {6, 1, 46},
            Items = {
                {"S©m Hoang Phi Tinh §o¹t Hån", 81, 985},
                {"S©m Hoang Kim TiÒn Liªn Hoµn Gi¸p", 82, 986},
                {"S©m Hoang Hån Gi¶o Yªu Thóc", 83, 987},
                {"S©m Hoang HuyÒn ThiÕt T­¬ng Ngäc Béi", 84, 988},
                {"S©m Hoang Tinh VÉn Phi Lý", 85, 989}
            }
        },
        {
            szBranch = "BÉy",
            SkillBook = {6, 1, 28},
            Items = {
                {"§Þa Ph¸ch Ngò hµnh Liªn Hoµn Qu¸n", 86, 990},
                {"§Þa Ph¸ch H¾c DiÖm Xung Thiªn Liªn", 87, 991},
                {"§Þa Ph¸ch TÝch LÞch L«i Háa Giíi", 88, 992},
                {"§Þa Ph¸ch KhÊu T©m Tr¹c", 89, 993},
                {"§Þa Ph¸ch §Þa Hµnh Thiªn Lý Ngoa", 90, 994},
                {"[TrÊn Bang Chi B¶o] §Þa Ph¸ch Phong Hµn Thóc Yªu", 854, 1057}
            }
        }
    },
    ["C¸i Bang"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 54},
            Items = {
                {"§ång Cõu Phi Long §Çu Hoµn", 91, 995},
                {"§ång Cõu Gi¸ng Long C¸i Y", 92, 996},
                {"§ång Cõu TiÒm Long Yªu §¸i", 93, 997},
                {"§ång Cõu Kh¸ng Long Hé UyÓn", 94, 998},
                {"§ång Cõu KiÕn Long Ban ChØ", 95, 999},
                {"[TrÊn Bang Chi B¶o] §ång Cõu Ngù Long Ngäc Béi", 855, 1058}
            }
        },
        {
            szBranch = "Bæng",
            SkillBook = {6, 1, 55},
            Items = {
                {"§Þch Kh¸i Lôc Ngäc Tr­îng", 96, 1000},
                {"§Þch Kh¸i Cöu §¹i C¸i Y", 97, 1001},
                {"§Þch Kh¸i TriÒn M·ng Yªu §¸i", 98, 1002},
                {"§Þch Kh¸i CÈu TÝch B× Hé UyÓn", 99, 1003},
                {"§Þch Kh¸i Th¶o Gian Th¹ch Giíi", 100, 1004}
            }
        }
    },
    ["Thiªn NhÉn"] = {
        {
            szBranch = "Th­¬ng",
            SkillBook = {6, 1, 35},
            Items = {
                {"Ma S¸t Quû Cèc U Minh Th­¬ng", 101, 1005},
                {"Ma S¸t Tµn D­¬ng ¶nh HuyÕt Gi¸p", 102, 1006},
                {"Ma S¸t XÝch Ký Táa Yªu KhÊu", 103, 1007},
                {"Ma S¸t Cö Háa Liªu Thiªn UyÓn", 104, 1008},
                {"Ma S¸t V©n Long Thæ Ch©u Giíi", 105, 1009},
                {"[TrÊn Bang Chi B¶o] Ma S¸t Cö Háa Liªu Thiªn Hoµn", 868, 1059}
            }
        },
        {
            szBranch = "Bïa",
            SkillBook = {6, 1, 53},
            Items = {
                {"Ma Hoµng Kim Gi¸p Kh«i", 106, 1010},
                {"Ma Hoµng ¸n XuÊt Hæ H¹ng Khuyªn", 107, 1011},
                {"Ma Hoµng Khª Cèc Thóc Yªu §¸i", 108, 1012},
                {"Ma Hoµng HuyÕt Y Thó Tr¹c", 109, 1013},
                {"Ma Hoµng §¨ng §¹p Ngoa", 110, 1014},
                {"[TrÊn Bang Chi B¶o] Ma Hoµng Dung Kim §o¹n NhËt Giíi", 874, 1060}
            }
        },
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 36},
            Items = {
                {"Ma ThÞ LiÖt DiÖm Qu¸n MiÖn", 111, 1015},
                {"Ma ThÞ LÖ Ma PhÖ T©m Liªn", 112, 1016},
                {"Ma ThÞ NghiÖp Háa U Minh Giíi", 113, 1017},
                {"Ma ThÞ HuyÕt Ngäc ThÊt S¸t Béi", 114, 1018},
                {"Ma ThÞ S¬n H¶i Phi Hång Lý", 115, 1019},
                {"[TrÊn Bang Chi B¶o] Ma ThÞ LÖ Ma PhÖ T©m §¸i", 876, 1061}
            }
        }
    },
    ["Vâ §ang"] = {
        {
            szBranch = "Ch­ëng",
            SkillBook = {6, 1, 33},
            Items = {
                {"L¨ng Nh¹c Th¸i Cùc KiÕm", 116, 1020},
                {"L¨ng Nh¹c V« Ng· §¹o Bµo", 117, 1021},
                {"L¨ng Nh¹c Né L«i Giíi", 118, 1022},
                {"L¨ng Nh¹c V« Cùc HuyÒn Ngäc Béi", 119, 1023},
                {"L¨ng Nh¹c Thiªn §Þa HuyÒn Hoµng Giíi", 120, 1024},
                {"[TrÊn Bang Chi B¶o] L¨ng Nh¹c V« Ng· Thóc §¸i", 881, 1062}
            }
        },
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 34},
            Items = {
                {"CËp Phong Ch©n Vò KiÕm", 121, 1025},
                {"CËp Phong Tam Thanh Phï", 122, 1026},
                {"CËp Phong HuyÒn Ti Tam §o¹n CÈm", 123, 1027},
                {"CËp Phong Thóy Ngäc HuyÒn Hoµng Béi", 124, 1028},
                {"CËp Phong Thanh Tïng Ph¸p Giíi", 125, 1029},
                {"[TrÊn Bang Chi B¶o] CËp Phong Thóy Ngäc HuyÒn Hoµng UyÓn", 888, 1063}
            }
        }
    },
    ["C«n L«n"] = {
        {
            szBranch = "§ao",
            SkillBook = {6, 1, 50},
            Items = {
                {"S­¬ng Tinh Thiªn Niªn Hµn ThiÕt", 126, 1030},
                {"S­¬ng Tinh Ng¹o S­¬ng §¹o Bµo", 127, 1031},
                {"S­¬ng Tinh Thanh Phong Lò §¸i", 128, 1032},
                {"S­¬ng Tinh Thiªn Tinh B¨ng Tinh Thñ", 129, 1033},
                {"S­¬ng Tinh Phong B¹o ChØ Hoµn", 130, 1034},
                {"[TrÊn Bang Chi B¶o] S­¬ng Tinh L­u Tinh C¶n NguyÖt KhÊu", 891, 1064}
            }
        },
        {
            szBranch = "KiÕm",
            SkillBook = {6, 1, 51},
            Items = {
                {"L«i Khung Hµn Tung B¨ng B¹ch Quan", 131, 1035},
                {"L«i Khung Thiªn §Þa Hé Phï", 132, 1036},
                {"L«i Khung Phong L«i Thanh CÈm §¸i", 133, 1037},
                {"L«i Khung Linh Ngäc UÈn L«i", 134, 1038},
                {"L«i Khung Cöu Thiªn DÉn L«i Giíi", 135, 1039},
                {"[TrÊn Bang Chi B¶o] L«i Khung Linh Ngäc Èn L«i UyÓn", 898, 1065}
            }
        },
        {
            szBranch = "Bïa",
            SkillBook = {6, 1, 52},
            Items = {
                {"Vô ¶o B¾c Minh §¹o Qu¸n",136, 1040},
                {"Vô ¶o Ki B¸n Phï Chó", 137, 1041},
                {"Vô ¶o Thóc T©m ChØ Hoµn", 138, 1042},
                {"Vô ¶o Thanh ¶nh HuyÒn Ngäc Béi", 139, 1043},
                {"Vô ¶o Tung Phong TuyÕt ¶nh Ngoa", 140, 1044},
                {"[TrÊn Bang Chi B¶o] Vô ¶o Th¸i Uyªn Ch©n Vò Liªn", 901, 1066}
            }
        }
    }
}

function tbHKUpgrade:ThieuLam()   self:SelectBranch("ThiÕu L©m") end
function tbHKUpgrade:ThienVuong() self:SelectBranch("Thiªn V­¬ng") end
function tbHKUpgrade:DuongMon()   self:SelectBranch("§­êng M«n") end
function tbHKUpgrade:NguDoc()     self:SelectBranch("Ngò §éc") end
function tbHKUpgrade:NgaMy()      self:SelectBranch("Nga My") end
function tbHKUpgrade:ThuyYen()    self:SelectBranch("Thóy Yªn") end
function tbHKUpgrade:CaiBang()    self:SelectBranch("C¸i Bang") end
function tbHKUpgrade:ThienNhan()  self:SelectBranch("Thiªn NhÉn") end
function tbHKUpgrade:VoDang()     self:SelectBranch("Vâ §ang") end
function tbHKUpgrade:ConLon()     self:SelectBranch("C«n L«n") end

function tbHKUpgrade:SelectBranch(szSect)
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local tbSectData = self.Data[szSect]
        if (not tbSectData) then return end
        local szMsg = "<npc><enter>HÖ ph¸i "..szSect.." rÊt ®a d¹ng<enter>Ng­¬i muèn n©ng c¸p hÖ trang bÞ nµo?"
        local tbOpt = {}
        local nCount = getn(tbSectData)
        for i = 1, nCount do
            local szName = tbSectData[i].szBranch
            tinsert(tbOpt, {"HÖ "..szName, self.SelectItem, {self, szSect, i}})
        end
        tinsert(tbOpt, {"KÕt thóc/cancel"})
        CreateNewSayEx(szMsg, tbOpt)
    end
end

function tbHKUpgrade:SelectItem(szSect, nBranchIdx)
    local tbBranchData = self.Data[szSect][nBranchIdx]
    local tbItems = tbBranchData.Items
    local szMsg = "<npc><enter><color=green>HÖ "..tbBranchData.szBranch.."<color><enter>Danh s¸ch trang bÞ <color=yellow>Hoµng Kim<color>:"
    local tbOpt = {}
    local nCount = getn(tbItems)
    for i = 1, nCount do
        tinsert(tbOpt, {"N©ng cÊp: "..tbItems[i][1], self.ConfirmUpgrade, {self, szSect, nBranchIdx, i}})
    end
    tinsert(tbOpt, {"Quay l¹i", self.SelectBranch, {self, szSect}})
    CreateNewSayEx(szMsg, tbOpt)
end

function tbHKUpgrade:ConfirmUpgrade(szSect, nBranchIdx, nIndexInData)
    local tbBranchData = self.Data[szSect][nBranchIdx]
    local tbItem = tbBranchData.Items[nIndexInData]
    local szMsg = "<npc>Mãn ®å: <color=yellow>"..tbItem[1].."<color>\n" ..
                  "Yªu cÇu:\n" ..
                  "- <color=green>BÝ KÝp "..szSect.." hÖ "..tbBranchData.szBranch.."<color>\n" ..
                  "- <color=green>"..self.nReqTDTT.." TuyÖt §Ønh Tri Thøc<color>\n" ..
                  "- <color=green>"..self.nReqHKL.." Hoµng Kim LÖnh<color>\n" ..
                  "- <color=green>2000 v¹n l­îng Ng©n l­îng.<color>"
    local tbOpt = {
        {"Ta ®· mang ®ñ, n©ng cÊp th«i nµo!", self.OpenUI, {self, szSect, nBranchIdx, nIndexInData}},
        {"Quay l¹i", self.SelectItem, {self, szSect, nBranchIdx}}
    }
    CreateNewSayEx(szMsg, tbOpt)
end

function tbHKUpgrade:OpenUI(szSect, nBranchIdx, nIndexInData)
    local szName = self.Data[szSect][nBranchIdx].Items[nIndexInData][1]
    g_GiveItemUI("N©ng CÊp: "..szName, "Bá vµo:<enter>- Trang bÞ Hoµng Kim<enter>- BÝ KÝp theo hÖ Trang BÞ<enter>- "..self.nReqTDTT.." TuyÖt §Ønh Tri Thøc<enter>- "..self.nReqHKL.." Hoµng Kim LÖnh", {self.OnConfirm, {self, szSect, nBranchIdx, nIndexInData}}, nil, 1)
end

function tbHKUpgrade:OnConfirm(szSect, nBranchIdx, nIndexInData, nNum)
    if (nNum <= 0) then return 0 end
    local tbBranchData = self.Data[szSect][nBranchIdx]
    local tbItemData   = tbBranchData.Items[nIndexInData]
    local szTargetName = tbItemData[1]
    local tbSkillBook  = tbBranchData.SkillBook
    local nEquipmentIdx = -1 
    local nBookCount, nTDTTCount, nHKLCount = 0, 0, 0
    local tbItemsToRemove = {}
    for i = 1, 20 do
        local nIdx = GetGiveItemUnit(i)
        if (nIdx > 0) then
            local g, d, p = GetItemProp(nIdx)
            local szNameInUI = GetItemName(nIdx)
            local nStack = GetItemStackCount(nIdx)
            if (nStack < 1) then nStack = 1 end
            if (szNameInUI == szTargetName) then 
                nEquipmentIdx = nIdx
            elseif (g == tbSkillBook[1] and d == tbSkillBook[2] and p == tbSkillBook[3]) then 
                nBookCount = nBookCount + nStack
            elseif (g == self.TDTT[1] and d == self.TDTT[2]) then 
                nTDTTCount = nTDTTCount + nStack
            elseif (g == self.HKL[1] and d == self.HKL[2] and p == self.HKL[3]) then 
                nHKLCount = nHKLCount + nStack
            end
            tinsert(tbItemsToRemove, nIdx)
        end
    end
    if (nEquipmentIdx == -1) then
        Talk(1, "", "VËt phÈm nµy kh«ng ph¶i <color=yellow>"..szTargetName.."<color>!")
        return 0
    end
    if (nBookCount < self.nReqBook or nTDTTCount < self.nReqTDTT or nHKLCount < self.nReqHKL or GetCash() < self.nReqMoney) then
        Talk(1, "", "Kh«ng ®ñ nguyªn liÖu hoÆc ng©n l­îng!")
        return 0
    end
    for i = 1, getn(tbItemsToRemove) do RemoveItemByIndex(tbItemsToRemove[i]) end
    Pay(self.nReqMoney)
    local NewIdx = AddGoldItem(0, tbItemData[3])
    if (NewIdx > 0) then
        SyncItem(NewIdx)
        Msg2SubWorld("<color=green>"..GetName().."<color> ®· n©ng cÊp thµnh c«ng <color=yellow>TuyÖt §Ønh "..szTargetName.."<color>!")
    end
    return 1
end