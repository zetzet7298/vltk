CurStation = 1
Include("\\script\\global\\station.lua")

----------------------------------------------------------------------------------------------------
--                                       HuyÒn C¬ L·o Nh©n                                        --
----------------------------------------------------------------------------------------------------
function main(sel)
	local mapid = SubWorldIdx2ID(SubWorld)
	local tbOpp = {
		"Nh÷ng n¬i ®· ®i qua/WayPointFun",
		"Nh÷ng thµnh thÞ ®· ®i qua/StationFun",
        "Trë l¹i ®Þa ®iÓm cò/TownPortalFun",
		"Quay l¹i Linh Thñy Ng­ Th«n/KhuVucLangChai",
		"§i ®Õn n¬i lµm nhiÖm vô D· TÈu/tl_moveToTaskMap",
        "§i ®Õn n¬i lµm nhiÖm vô Boss S¸t Thñ/GoToBossSatThu",
	}
	tinsert(tbOpp, "Kh«ng cÇn ®©u/OnCancel")
	Say("B¹n muèn ®i ®Õn n¬i nµo?", getn(tbOpp), tbOpp)
end

function KhuVucLangChai()
	NewWorld(1010,1762,3481)
	SetFightState(0)
end
----------------------------------------------------------------------------------------------------
tbBossTeleData = {
    [141] = {93, 1644, 3279, "Cæ Giíi Nh©n"},
    [142] = {93, 1646, 3058, "TrÞnh Cöu NhËt"},
    [143] = {93, 1736, 3213, "Chu Së B¸"},
    [144] = {93, 1610, 3152, "Trang Minh Trung"},
    [145] = {225, 1590, 3325, "Cam ChÝnh C«"},
    [146] = {225, 1261, 3247, "Vò NhÊt ThÕ"},
    [147] = {225, 1452, 3377, "D­¬ng Phong DËt"},
    [148] = {225, 1425, 3107, "Hµ Sinh Vong"},
    [149] = {75, 1711, 3187, "T»ng ChØ O¸n"},
    [150] = {75, 1752, 3124, "VÖ Biªn Thµnh"},
    [151] = {75, 1831, 3190, "Cè Thñ §»ng"},
    [152] = {75, 1639, 3159, "Ch­ C¸t Kinh Hång"},
    [153] = {321, 1253, 3002, "Phan Ng¹t Nhan"},
    [154] = {321, 1483, 2742, "Liªn Kinh Th¸i"},
    [155] = {321, 1289, 2613, "B¶o TriÖt S¬n"},
    [156] = {321, 1113, 2569, "V¹n Hå Tinh"},
    [157] = {340, 1217, 2740, "Trö Thiªn MÉn"},
    [158] = {340, 1723, 2765, "§o¹n L¨ng NguyÖt"},
    [159] = {340, 1275, 2749, "T¶ DËt Danh"},
    [160] = {340, 1932, 2759, "Nh©m Th­¬ng Khung"},
}

function GoToBossSatThu()
    local nBossID = GetTask(1082)
    local tbInfo = tbBossTeleData[nBossID]

    if (not tbInfo) then
        Say("Ng­¬i hiÖn t¹i kh«ng cã nhiÖm vô Boss S¸t Thñ nµo phï hîp.", 0)
        return
    end

    local szBossName = tbInfo[4]
    local tbSay = {
        "H·y ®­a ta ®Õn ®ã nµo!/ExecTeleportBoss",
        "Th«i ta kh«ng muèn ®i./OnCancel"
    }
    Say("Ta thÊy ng­¬i ®ang truy t×m <color=red>"..szBossName.."<color>. Ng­¬i cã muèn ®i ngay kh«ng?", getn(tbSay), tbSay)
end

function ExecTeleportBoss()
    local nBossID = GetTask(1082)
    local tbInfo = tbBossTeleData[nBossID]
    
    if (tbInfo) then
        local nMap = tbInfo[1]
        local nX = tbInfo[2]
        local nY = tbInfo[3]
        
        Msg2Player("DÞch chuyÓn ®Õn vÞ trÝ Boss: <color=yellow>"..tbInfo[4].."<color>")
        NewWorld(nMap, nX, nY)
        SetFightState(1)
    end
end