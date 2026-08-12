IncludeLib("FILESYS")

TB_LEVEL_REMAIN_PROP = {}

function Load_TransLifeSetting()
	
	local b1 = TabFile_Load("\\settings\\task\\metempsychosis\\translife.txt", "TransLifeSetting")
	if b1~=1 then
		print("Load TransLifeSetting Failed!")
		return
	end
	local nRowCount = TabFile_GetRowCount("TransLifeSetting", "LEVEL")
	
	for y = 2, nRowCount do
		local n_level = tonumber(TabFile_GetCell("TransLifeSetting", y, "LEVEL"))
		
		local tb = {}
		
		for z = 1, 5 do
			
			local n_magicpoint = tonumber(TabFile_GetCell("TransLifeSetting", y, "MAGICPOINT"..z))
			local n_prop = tonumber(TabFile_GetCell("TransLifeSetting", y, "PROP"..z))
			local n_resist = tonumber(TabFile_GetCell("TransLifeSetting", y, "RESIST"..z))
			local n_addskilll = tonumber(TabFile_GetCell("TransLifeSetting", y, "SKILLLIMIT"..z))
			
			if (n_magicpoint) then
				tb[getn(tb) + 1] = {n_magicpoint, n_prop, n_resist, n_addskilll}
			end
			
		end
		
		if (n_level ~= nil) then
			TB_LEVEL_REMAIN_PROP[n_level] = tb
		end
	end	
end

Load_TransLifeSetting()

TB_LEVEL_LIMIT = {200, 200, 200, 200, 200}
TB_TRANSTIME_LIMIT = {0, 0, 0, 0, 0}
TBITEMNEED_4 = {
	[1] = {szName = "V‚ L©m L÷nh", tbProb = {6,1,4905}, nCount = 1000},
	[2] = {szName = "TËng Kim L÷nh", tbProb = {6,1,4906}, nCount = 500},
	[3] = {szName = "Phong H·a L÷nh", tbProb = {6,1,4907}, nCount = 1000},
	[4] = {szName = "Hoµng Kim L÷nh", tbProb = {6,1,4908}, nCount = 100},
	[5] = {szName = "Tuy÷t ßÿnh Tri Th¯c", tbProb = {4,2054,1}, nCount = 100},
}

TB_TRANSLIFE_ERRORMSG = {
	[1] = "<dec><npc>TrÔng Sinh c«n ph∂i cÎi b· t t c∂   trang bﬁ tr™n ng≠Íi",
	[2] = "<dec><npc>H◊nh nh≠ ti“n v…n ch≠a ÆÒ <color=green>10.000 vπn l≠Óng<color>",
	[3] = "<dec><npc>Tu luy÷n Bæc ß»u Tr≠Íng Sinh ThuÀt c«n ph∂i b· quan h÷ S≠ ßÂ",
	[4] = "<dec><npc>H◊nh nh≠ nhµ ng≠¨i v…n ch≠a h‰c<enter><color=green>Bæc ß»u Tr≠Íng Sinh ThuÀt - C¨ SÎ Thi™n<color>",
	[5] = "<dec><npc>Vﬁ thi’u hi÷p nµy v…n ch≠a ÆÒ c p Æ” tu luy÷n, h∑y v“ tu luy÷n th™m Æi nh–.",
	[6] = "<dec><npc>Tu luy÷n Bæc ß»u Tr≠Íng Sinh ThuÀt c«n ph∂i b· quan h÷ chi’n ÆÈi v‚ l©m li™n Æ u",
	[7] = "<dec><npc>Bæc ß»u Tr≠Íng Sinh ThuÀt - T©m Ph∏p Thi™n nhi“u nh t chÿ c„ th” tu luy÷n 5 t«ng, ng≠¨i Æ∑ h‰c ÆÒ rÂi.",
	[8] = "<dec><npc>Nhi÷m vÙ S∏t ThÒ v…n ch≠a hoµn thµnh! H∑y hoµn t t nhi÷m vÙ rÂi quay lπi nh–.",
	[9] = "<dec><npc>Nhi÷m vÙ T›n S¯ v…n ch≠a hoµn thµnh! H∑y hoµn t t nhi÷m vÙ rÂi quay lπi nh–.",
	[10]= "<dec><npc>Nhi÷m vÙ D∑ T»u v…n ch≠a hoµn thµnh! H∑y hoµn t t nhi÷m vÙ rÂi quay lπi nh–.",
	[11]= "<dec><npc>D∑ t»u thu Æ≠Óc c¨ hÈi hÒy b· nhi÷m vÙ! H∑y hoµn t t nhi÷m vÙ nµy rÂi quay lπi nh–.",
	[12]= "<dec><npc>Kho∂ng c∏ch 2 l«n trÔng sinh ph∂i lµ %d ngµy.",
	[13]= "<dec><npc>Chuy”n sinh 4 c«n <color=green>20.000 vπn l≠Óng<color>, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
	[14]= "<dec><npc>Chuy”n sinh 4 c«n c„ <color=green>1000<color> c∏i V‚ L©m L÷nh, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
	[15]= "<dec><npc>Chuy”n sinh 4 c«n c„ <color=green>500<color> c∏i TËng Kim L÷nh, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
	[16]= "<dec><npc>Chuy”n sinh 4 c«n c„ <color=green>1000<color> c∏i Phong H·a L÷nh, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
	[17]= "<dec><npc>Chuy”n sinh 4 c«n c„ <color=green>100<color> c∏i Hoµng  Kim L÷nh, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
	[18]= "<dec><npc>Chuy”n sinh 4 c«n c„ <color=green>100<color> cuËn Tuy÷t ßÿnh Tri Th¯c, xin h∑y chu»n bﬁ ÆÒ rÂi Æ’n Æ©y.",
}

TB_BASE_STRG = {35,20,25,30,20}
TB_BASE_DEX = {25,35,25,20,15}
TB_BASE_VIT = {25,20,25,30,25}
TB_BASE_ENG = {15,25,25,20,40}

ZHUANSHENG_DESC		= "METEMPSYCHOSIS"
ZHUANSHENG_TUITION	= 100000000
ZHUANSHENG_XIANDAN_MINEXP	= 2*10e8
ZHUANSHENG_XIANDAN_BASEEXP		= 10e6
ZHUANSHENG_ITEM_BEGIN	= 20090420
ZHUANSHENG_ITEM_ENDLE	= 20090503
ZHUANSHENG_ITEM_EXTIME	= 20090601

LG_SHITULEAGUE = 1
LG_WLLSLEAGUE = 5

TSK_ZHUANSHENG_FLAG = 2547
TSK_ZHUANSHENG_1 = 2548
TSK_ZHUANSHENG_2 = 2549
TSK_ZHUANSHENG_XIANDAN	= 2581
TSK_ZHUANSHENG_AWARD	= 2582


TSK_KILLER_ID = 1082
TSK_MESSENGER_FENG = 1201
TSK_MESSENGER_SHAN = 1202
TSK_MESSENGER_QIAN = 1203
TSK_TASKLINK_STATE = 1028
TSK_TASKLINK_CancelTaskLevel = 2571
TSK_TASKLINK_CancelTaskExp1 = 2570
TSK_TASKLINK_CancelTaskExp2 = 2575

TSK_ZHUANSHENG_GRE = {2577, 2578, 2579}
TSK_ZHUANSHENG_LASTTIME = 2580
TSKM_ZHUANSHENG_RESISTID = 199
TB_BASE_RESIST = {
	[0] = "Ch‰n 1 dﬂng, ch¯c n®ng giËng nhau",
	[1] = "Ch‰n 1 dﬂng, ch¯c n®ng giËng nhau",
	[2] = "Ch‰n 1 dﬂng, ch¯c n®ng giËng nhau",
	[3] = "Ch‰n 1 dﬂng, ch¯c n®ng giËng nhau",
	[4] = "Ch‰n 1 dﬂng, ch¯c n®ng giËng nhau",
	}

TSK_TRANSLIFE_4 = 2908	
TSK_LEAVE_SKILL_POINT_4 = 2909
TSK_USED_SKILL_POINT_4 = 2899
TSK_LAST_UP_LEVEL_4 = 2910
ZHUANSHENG_TUITION_4	= 200000000
CLEAR_SKILL_4_PRICE = 10000000
TB_SKILL_4 = {{1123,0},{1124,0},{1125,0},{1126,0},{1127,0},{1128,0},{1129,0},{1130,0}}

NSTARTLEVEL_4 = 105
NPERPOINTNEEDLEVEL = 5