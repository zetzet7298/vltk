IncludeLib("SETTING")
IncludeLib("ITEM")
Include("\\script\\global\\fuyuan.lua")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\misc\\eventsys\\type\\npc.lua")

----------------------------------------------------------------------------------------------------
--											  Test Server								  		  --
----------------------------------------------------------------------------------------------------
function TestServer()
	local tbSay = {"Hç trî ng­êi ch¬i tham gia test game"};
		tinsert(tbSay, "NhËn c¸c lo¹i ®iÓm/nhandiem")
		tinsert(tbSay, "NhËn c¸c lo¹i nguyªn liÖu/nhannguyenlieu")
		tinsert(tbSay, "NhËn trang bÞ Hoµng Kim M«n Ph¸i/nhantrangbihkmp")
		tinsert(tbSay, "KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
--											C¸c Lo¹i §iÓm								  		  --
----------------------------------------------------------------------------------------------------
function nhandiem()
	local tbSay  = {"Ng­¬i muèn nhËn lo¹i ®iÓm nµo?"}
		tinsert(tbSay, "NhËn §iÓm Kinh NghiÖm/nhankinhnghiem")
		tinsert(tbSay, "NhËn CÊp §é/nhancapdo")
		tinsert(tbSay, "NhËn TiÒn V¹n/nhantienvan")
		tinsert(tbSay, "NhËn TiÒn §ång/nhantiendong")
		tinsert(tbSay, "NhËn Phóc Duyªn/nhanphucduyen")
		tinsert(tbSay, "NhËn §iÓm Tèng Kim/nhandiemtongkim")
		tinsert(tbSay, "NhËn §iÓm Vinh Dù/nhandiemvinhdu")
		tinsert(tbSay, "KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
end
----------------------------------------------------------------------------------------------------
--§iÓm Kinh NghiÖm
function nhankinhnghiem()
	AskClientForNumber("nhankinhnghiem1",0,9999999999999999,"NhËp EXP")
end

function nhankinhnghiem1(nNum)
	AddOwnExp(nNum)
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..nNum.."<color> kinh nghiÖm.") 
end

----------------------------------------------------------------------------------------------------
--CÊp §é
function nhancapdo()
	AskClientForNumber("nhancapdo1",0,200,"NhËp CÊp §é:") 
end

function nhancapdo1(num)
	local nCurLevel = GetLevel()
	local nAddLevel = num - nCurLevel
	ST_LevelUp(nAddLevel)
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..num.."<color> cÊp ®é.") 
end

----------------------------------------------------------------------------------------------------
--NhËn TiÒn V¹n
function nhantienvan()
	AskClientForNumber("nhantienvan1",0,10000000,"NhËp Sè L­îng:") 
end

function nhantienvan1(slkv)
	local money= slkv*10000
	local giatrikv=money/10000
	Earn(money)
	Msg2Player(format("B¹n nhËn ®­îc <color=yellow>%s v¹n<color>.",giatrikv))
end

----------------------------------------------------------------------------------------------------
--NhËn TiÒn §ång
function nhantiendong()
	AskClientForNumber("nhantiendong1",0,1999,"NhËp Sè L­îng:") 
end

function nhantiendong1(sltiendong)
	for i = 1, sltiendong do
		AddStackItem(1,4,417,1,1,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..sltiendong.." <color>tiÒn ®ång.")
end

----------------------------------------------------------------------------------------------------
--NhËn Phóc Duyªn
function nhanphucduyen()
	AskClientForNumber("nhanphucduyen1",0,500000,"NhËp Sè L­îng:") 
end

function nhanphucduyen1(nNum)
	FuYuan_Start();			
	FuYuan_Add(nNum);
	Msg2Player("B¹n nhËn ®­îc "..nNum.." ®iÓm Phóc Duyªn.")
end

----------------------------------------------------------------------------------------------------
--§iÓm Tèng Kim
function nhandiemtongkim()
	AskClientForNumber("nhandiemtongkim1",0,1000000,"NhËp Sè L­îng:") 
end

function nhandiemtongkim1(nNum)
	SetTask(747, GetTask(747) + nNum)
	Msg2Player("B¹n nhËn ®­îc "..nNum.." §iÓm Tèng Kim.")
end

----------------------------------------------------------------------------------------------------
--§iÓm Vinh Dù
function nhandiemvinhdu()
	AskClientForNumber("nhandiemvinhdu1",0,1000000,"NhËp Sè L­îng:") 
end

function nhandiemvinhdu1(nNum)
	SetTask(2501, GetTask(2501) + nNum)
	Msg2Player("B¹n nhËn ®­îc "..nNum.." §iÓm Vinh Dù.")
end

----------------------------------------------------------------------------------------------------
--											 Nguyªn LiÖu								  		  --
----------------------------------------------------------------------------------------------------
function nhannguyenlieu()
	local tbSay  = {"Ng­¬i muèn nhËn lo¹i nguyªn liÖu nµo?"}
		tinsert(tbSay, "NhËn Kim Lo¹i HiÕm/nhankimloaihiem")
		tinsert(tbSay, "NhËn Vâ L©m LÖnh/nhanvolamlenh")
		tinsert(tbSay, "NhËn Tèng Kim LÖnh/nhantongkimlenh")
		tinsert(tbSay, "NhËn Phong Háa LÖnh/nhanphonghoalenh")
		tinsert(tbSay, "NhËn Hoµng Kim LÖnh/nhanhoangkimlenh")
		tinsert(tbSay, "KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
--NhËn Kim Lo¹i HiÕm
function nhankimloaihiem()
	AskClientForNumber("nhankimloaihiem1",0,2000,"NhËp Sè L­îng:") 
end

function nhankimloaihiem1(slklh)
	for i = 1, slklh do
		AddStackItem(1,4,2045,1,1,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..slklh.." <color>Kim Lo¹i HiÕm.")
end

----------------------------------------------------------------------------------------------------
--NhËn Vâ L©m LÖnh
function nhanvolamlenh()
	AskClientForNumber("nhanvolamlenh1",0,2000,"NhËp Sè L­îng:") 
end

function nhanvolamlenh1(slvll)
	for i = 1, slvll do
		AddStackItem(1,6,1,4905,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..slvll.." <color>Vâ L©m LÖnh.")
end

----------------------------------------------------------------------------------------------------
--NhËn Tèng Kim LÖnh
function nhantongkimlenh()
	AskClientForNumber("nhantongkimlenh1",0,2000,"NhËp Sè L­îng:") 
end

function nhantongkimlenh1(sltkl)
	for i = 1, sltkl do
		AddStackItem(1,6,1,4906,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..sltkl.." <color>Tèng Kim LÖnh.")
end

----------------------------------------------------------------------------------------------------
--NhËn Phong Háa LÖnh
function nhanphonghoalenh()
	AskClientForNumber("nhanphonghoalenh1",0,2000,"NhËp Sè L­îng:") 
end

function nhanphonghoalenh1(slphl)
	for i = 1, slphl do
		AddStackItem(1,6,1,4907,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..slphl.." <color>Phong Háa LÖnh.")
end

----------------------------------------------------------------------------------------------------
--NhËn Hoµng Kim LÖnh
function nhanhoangkimlenh()
	AskClientForNumber("nhanhoangkimlenh1",0,2000,"NhËp Sè L­îng:") 
end

function nhanhoangkimlenh1(slphl)
	for i = 1, slphl do
		AddStackItem(1,6,1,4908,0,0,0)
	end
	Msg2Player("B¹n nhËn ®­îc <color=yellow>"..slphl.." <color>Hoµng Kim LÖnh.")
end

----------------------------------------------------------------------------------------------------
--									 NhËn §å Hoµng Kim M«n Ph¸i								  	  --
----------------------------------------------------------------------------------------------------
TRANGBI_HKMP = {
	[0] = {
		["ThiÕu L©m QuyÒn"]			= {1, 5},
		["ThiÕu L©m C«n"]			= {6, 10},
		["ThiÕu L©m §ao"]			= {11, 15},
	},

	[1] = {
		["Thiªn V­¬ng Chïy"]		= {16, 20},
		["Thiªn V­¬ng Th­¬ng"]		= {21, 25},
		["Thiªn V­¬ng §ao"]			= {26, 30},
	},

	[2] = {
		["§­êng M«n Phi §ao"]		= {71, 75},
		["§­êng M«n Tô TiÔn"]		= {76, 80},
		["§­êng M«n Phi Tiªu"]		= {81, 85},
		["§­êng M«n BÉy"]			= {86, 90},
	},
	
	[3] = {
		["Ngò §éc Ch­ëng"]			= {56, 60},
		["Ngò §éc §ao"]				= {61, 65},
		["Ngò §éc Bïa"]				= {66, 70},
	},
	
	[4] = {
		["Nga My KiÕm"]				= {31, 35},
		["Nga My Ch­ëng"]			= {36, 40},
		["Nga My Phô Trî"]			= {41, 45},
	},
	
	[5] = {
		["Thóy Yªn §ao"]			= {46, 50},
		["Thóy Yªn Ch­ëng"]			= {51, 55},
	},
	
	[6] = {
		["C¸i Bang Ch­ëng"]			= {91, 95},
		["C¸i Bang Bæng"]			= {96, 100},
	},
	
	[7] = {
		["Thiªn NhÉn KÝch"]			= {101, 105},
		["Thiªn NhÉn Bïa"]			= {106, 110},
		["Thiªn NhÉn Ch­ëng"]		= {111, 115},
	},
	
	[8] = {
		["Vâ §ang Ch­ëng"]			= {116, 120},
		["Vâ §ang KiÕm"]			= {121, 125},
	},
	
	[9] = {
		["C«n L«n §ao"]				= {126, 130},
		["C«n L«n Ch­ëng"]			= {131, 135},
		["C«n L«n Bïa"]				= {136, 140},
	},
}

EQUIP_FACTION = {
	[0] = "ThiÕu L©m",
	[1] = "Thiªn V­¬ng",
	[2] = "§­êng M«n",
	[3] = "Ngò §éc",
	[4] = "Nga My",
	[5] = "Thóy Yªn",
	[6] = "C¸i Bang",
	[7] = "Thiªn NhÉn",
	[8] = "Vâ §ang",
	[9] = "C«n L«n",
};

function nhantrangbihkmp()
	local tbSay = {
        "H·y chän lo¹i trang bÞ muèn nhËn.<enter><color=Green>Nhí chän cho ®óng ph¸i nhÐ!<color>",
        "Trang bÞ Hoµng Kim M«n Ph¸i/nhanhkmp",
		"TrÊn Bang Chi B¶o/nhantbcb",
        "§Ó ta suy nghÜ thªm ®·./no"
    }
	CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
--Hoµng Kim M«n Ph¸i
function nhanhkmp()
	if (CalcFreeItemCellCount() < 20) then
		Talk(1, "", "Hµnh trang kh«ng ®ñ 20 « trèng ®Ó nhËn.")
	return end
	
	local n_Faction = GetLastFactionNumber();
	if (n_Faction < 0) then
		Talk(1, "", "B¹n ch­a gia nhËp m«n ph¸i, kh«ng thÓ nhËn trang bÞ nµy")
	return end
	
	if (n_Faction > 9) then
		Talk(1, "", "HiÖn t¹i ch­a cã trang bÞ hoµng kim m«n ph¸i nµo nµo cho <color=red>Hoa S¬n ph¸i<color> c¶!")
	return end
	
	local szTitle = "<dec>B¹n muèn nhËn trang bÞ cña m«n ph¸i nµo?";
	local tbOption = {};
	local tb_Equip = TRANGBI_HKMP;
	local tb_Faction = EQUIP_FACTION;
	for i = 0, (getn(tb_Equip)-0) do
		tinsert(tbOption, {format("Trang bÞ ph¸i %s", tb_Faction[i]), nhanhkmp1, {tb_Equip[i]}})
	end
		tinsert(tbOption, {"KÕt thóc ®èi tho¹i."})
	CreateNewSayEx(szTitle, tbOption)
end

function nhanhkmp1(tb_EquipFaction)
	local tb_Equip = tb_EquipFaction;
	local tb_Faction = EQUIP_FACTION;
	local tbOption = {};
	local szTitle = "<dec>Mêi b¹n chän ®­êng tÊn c«ng c¬ b¶n?";
	for x, y in tb_Equip do
		tinsert(tbOption, {format("%s", x), nhanhkmp2, {tb_Equip[x]}})
	end
		tinsert(tbOption, {"KÕt thóc ®èi tho¹i."})
	CreateNewSayEx(szTitle, tbOption)
end

function nhanhkmp2(tb_EquipKind)
	for nID = tb_EquipKind[1], tb_EquipKind[2] do
		AddGoldItem(0, nID)
	end
end

----------------------------------------------------------------------------------------------------
--TrÊn Bang Chi B¶o
TRANGBI_TBCB = {
    ["ThiÕu L©m"]   = {769, 771, 776},
    ["Thiªn V­¬ng"] = {793},
    ["Nga My"]      = {796, 801, 808},
    ["Thóy Yªn"]    = {811, 816},
    ["Ngò §éc"]     = {829, 834},
    ["§­êng M«n"]   = {843, 854},
    ["C¸i Bang"]    = {855},
    ["Thiªn NhÉn"]  = {868, 874, 876},
    ["Vâ §ang"]     = {881, 888},
    ["C«n L«n"]     = {891, 898, 901},
}

function nhantbcb()
    local szMsg = "Xin chµo <color=red>"..GetName().."<color>. H·y chän m«n ph¸i muèn nhËn ®å:"
    local tbSay = {
        szMsg,
        "ThiÕu L©m/Give_TL",
        "Thiªn V­¬ng/Give_TV",
        "Nga My/Give_NM",
        "Thóy Yªn/Give_TY",
        "Ngò §éc/Give_ND",
        "§­êng M«n/Give_DM",
        "C¸i Bang/Give_CB",
        "Thiªn NhÉn/Give_TN",
        "Vâ §ang/Give_VD",
        "C«n L«n/Give_CL",
        "Tho¸t/OnCancel"
    }
    CreateTaskSay(tbSay)
end

function ExecuteGive(szSect)
    local tbItems = TRANGBI_TBCB[szSect]
    if (tbItems == nil) then return end

    if (CountFreeRoomByWH(2, 5) <= 0) then
        Talk(1, "", "Hµnh trang kh«ng cã kho¶ng trèng <color=yellow>2 « ngang vµ 5 « däc<color> liªn tôc.")
        return
    end

    for i=1, getn(tbItems) do
        AddGoldItem(0, tbItems[i])
    end
    
    Msg2Player("B¹n nhËn ®­îc trang bÞ c¬ b¶n cña ph¸i "..szSect)
end

function Give_TL() ExecuteGive("ThiÕu L©m") end
function Give_TV() ExecuteGive("Thiªn V­¬ng") end
function Give_NM() ExecuteGive("Nga My") end
function Give_TY() ExecuteGive("Thóy Yªn") end
function Give_ND() ExecuteGive("Ngò §éc") end
function Give_DM() ExecuteGive("§­êng M«n") end
function Give_CB() ExecuteGive("C¸i Bang") end
function Give_TN() ExecuteGive("Thiªn NhÉn") end
function Give_VD() ExecuteGive("Vâ §ang") end
function Give_CL() ExecuteGive("C«n L«n") end