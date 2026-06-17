Include("\\script\\global\\mel\\mission\\tuyetdinhvude\\lib\\serverlib.lua")
Include("\\script\\global\\pgaming\\configserver\\configall.lua")

BIGBOSS_SERVER_INFO = {
	[1] =	{	szName = "TuyÖt §Ønh Vò §Õ", nBossId = 1875, nRate = 322, nSeries = 0, nLevel = 95},
}

BIGBOSS_FILE_POS = {
	"\\settings\\bosshoangkim\\maps\\bigboss\\bienkinh.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\daily.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\duongchau.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\laman.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\phuongtuong.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\tuongduong.txt",
	"\\settings\\bosshoangkim\\maps\\bigboss\\thanhdo.txt",
}

BIGBOSS_AWARD = {	
	[1875] = { -- TuyÖt §Ønh Vò §Õ
		szNameDoPho = {},
		tbPropDoPho = {},
		tbItemIDTime = {768,770,775,792,795,800,807,810,815,828,833,842,853,854,867,873,875,880,887,890,897,900}, -- ID TrÊn Bang Chi B¶o (Trõ 2)
		szName = {"Hµnh HiÖp Lé Bao","Hoµng Kim LÖnh","Vâ L©m LÖnh","§¹i Lùc Hoµn","Phi Tèc Hoµn","Phóc Duyªn Lé (§¹i)","§¹i Thµnh BÝ KÝp 90","Ch×a Khãa Hoµng Kim","TiÒn §ång","Linh Hån ChiÕn M·","Kim Lo¹i HiÕm","TuyÖt §Ønh Tri Thøc","Ên Kinh Th­","Trang Søc Kinh Th­","M¶nh V¶i GÊm","Tiªn Th¶o Lé","Thiªn S¬n B¶o Lé","B¸ch Qu¶ Lé"},
		nProp = {{6,1,4909,1,0,0},{6,1,4908,1,0,0},{6,1,4905,1,0,0},{6,0,3,1,0,0},{6,0,6,1,0,0},{6,1,124,1,0,0},{6,1,2424,1,0,0},{6,1,4889,1,0,0},{4,417,1,1,0,0},{4,2052,1,1,0,0},{4,2045,1,1,0,0},{4,2054,1,1,0,0},{4,2056,1,1,0,0},{4,2057,1,1,0,0},{4,2058,1,1,0,0},{6,1,71,1,0,0},{6,1,72,1,0,0},{6,1,73,1,0,0}},
		nCount = 5,
		nRate = 90,
	},
}

BOSS_DEATH_SCRIPT = "\\script\\global\\mel\\mission\\tuyetdinhvude\\goldboss_death.lua"
TIME_BIGBOSS_REMOVE = 300*60*18
BossDataSave = {}

function vude_toworld(nNumBoss)
    if not nNumBoss then return end
    local nTime = tonumber(GetLocalDate("%d%H"))
    bigboss_newboss(nTime)
    BossDataSave[nTime].new_boss = {}
    BossDataSave[nTime].record_boss = {}
    BossDataSave[nTime].map_names = {}
    for k=1, nNumBoss do 
        local boss_pak = bigboss_getaboss()
        if boss_pak and boss_pak[1] ~= nil then
            local nNpcIndex = AddNpcEx(unpack(boss_pak))
            SetNpcParam(nNpcIndex, 1, boss_pak[1])
            SetNpcDeathScript(nNpcIndex, BOSS_DEATH_SCRIPT)
            SetNpcTimer(nNpcIndex, TIME_BIGBOSS_REMOVE)
            local nMapIdx = boss_pak[4]
            local map_name = BossDataSave[nTime].map_names[nMapIdx] or "B¶n ®å hµnh tr×nh"
            local szMsg = format("Giang hå ®ån r»ng %s ®· xuÊt hiÖn ë %s ! Giang hå ¾t h¼n sÏ cã mét trËn ®Ém m¸u!", boss_pak[8], map_name)
            local szSub = format("<color=gold>%s<color> ®· xuÊt hiÖn t¹i map: <color=green>%s (%d,%d) <color>", boss_pak[8], map_name, floor((boss_pak[5]/32)/8), floor((boss_pak[6]/32)/16))
            AddGlobalNews(szMsg)
            Msg2SubWorld(szSub)
        else
            print("Lçi: Boss §¹i thø "..k.." không thÓ khëi t¹o.")
        end
    end
end

function bigboss_newboss(nTime)
    if not BossDataSave[nTime] then 
        BossDataSave[nTime] = {record_boss = {}, new_boss = {}, map_names = {}}
    end
    return 1
end

function bigboss_getaboss()
    local ncount = getn(BIGBOSS_FILE_POS)
    local nTime = tonumber(GetLocalDate("%d%H"))
    bigboss_newboss(nTime)
    local item = BossDataSave[nTime]
    local szFile = ""
    local m_loop = 0
    while (1) do 
        m_loop = m_loop + 1
        if m_loop > 500 then break end
        local nRFile = random(1, ncount)
        if not item.record_boss[nRFile] then 
            item.record_boss[nRFile] = 1
            szFile = BIGBOSS_FILE_POS[nRFile]
            break
        end
    end
    if szFile == "" then return nil end
    local nMapBoss, nXBoss, nYBoss, zMapName = _getadata(szFile)
    if not nMapBoss or not nXBoss or not nYBoss then return nil end
    local nMapIdx = SubWorldID2Idx(tonumber(nMapBoss))
    if nMapIdx == -1 then return nil end
    item.map_names[nMapIdx] = zMapName
    local boss_info = {}
    m_loop = 0
    local nMaxBossInfo = getn(BIGBOSS_SERVER_INFO)
    while (1) do 
        m_loop = m_loop + 1
        if m_loop > 200 then break end
        local nRBoss = random(1, nMaxBossInfo)
        if not item.new_boss[nRBoss] then 
            item.new_boss[nRBoss] = 1
            boss_info = BIGBOSS_SERVER_INFO[nRBoss]
            break
        end
    end
    if not boss_info.nBossId then return nil end
    return {boss_info.nBossId, boss_info.nLevel, random(0,4), nMapIdx, tonumber(nXBoss)*32, tonumber(nYBoss)*32, 1, boss_info.szName, 1}
end

function _getadata(file)
    local nHeight = _GetTabFileHeight(file)
    if nHeight <= 1 then 
        print("Lçi: File t?a ®é kh«ng cã d÷ liÖu hoÆc sai ®­êng dÉn: "..tostring(file))
        return nil 
    end
    local totalcount = nHeight - 1
    local id = random(1, totalcount)
    local w = tonumber(_GetTabFileData(file, id + 1, 1))
    local x = tonumber(_GetTabFileData(file, id + 1, 2))
    local y = tonumber(_GetTabFileData(file, id + 1, 3))
    local z = _GetTabFileData(file, id + 1, 4)
    return w, x, y, z
end

function _GetTabFileHeight(mapfile)
    if (TabFile_Load(mapfile, mapfile) == 0) then
        print("Load TabFile Error: "..tostring(mapfile))
        return 0
    end
    return TabFile_GetRowCount(mapfile)
end

function _GetTabFileData(mapfile, row, col)
    if (TabFile_Load(mapfile, mapfile) == 0) then
        return 0
    else
        local szVal = TabFile_GetCell(mapfile, row, col)
        if szVal == nil or szVal == "" then return 0 end
        
        local nVal = tonumber(szVal)
        if nVal then return nVal end
        return szVal
    end
end