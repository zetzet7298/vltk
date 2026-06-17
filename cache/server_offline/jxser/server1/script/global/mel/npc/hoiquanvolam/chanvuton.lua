Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\exchange\\hoangkimlenh.lua")
Include("\\script\\global\\mel\\feature\\exchange\\tinvathoangkim.lua")

----------------------------------------------------------------------------------------------------
--                                            Ch©n Vò T«n                                         --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."<enter>Tr­íc mÆt ta, mäi lo¹i thÇn khÝ còng chØ lµ s¾t vôn!<enter>KÎ phµm phu n­¬ng nhê vò khÝ, h¹ng trung liÖt m¶i    miÕt luyÖn chiªu!<enter><enter>Cßn <color=Green>Ch©n Vò T«n<color> ta... sinh ra ®· lµ hiÖn th©n cña Vâ §¹o!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/chanvuton.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        if Cfg_HoangKimLenh  == 1 then
            tinsert(tbSay,"Hoµng Kim LÖnh/doihoangkimlenh")
        end
        if Cfg_TrangBiKimQuang == 1 then
            tinsert(tbSay,"TÝn VËt Hoµng Kim/TinVatHoangKim")
            tinsert(tbSay,"§æi R­¬ng Kim Quang/DoiRuongKimQuang")
        end
        tinsert(tbSay,"Ph©n t¸ch ®å Hoµng Kim M«n Ph¸i/DoiTrangBiHKMP")
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
function TinVatHoangKim()
    local TITLEDIALOG1 = "Xin chµo <color=green>%s<color>\n".."<enter>C¸c h¹ muèn ®æi lo¹i <color=orange>TÝn VËt<color> nµo?"
    local player_name = GetName() 
	local tbSay = {format(TITLEDIALOG1, GetName())}
        tinsert(tbSay,"§æi TÝn VËt HiÖp Cèt/doihiepcot")
        tinsert(tbSay,"§æi TÝn VËt Nhu T×nh/doinhutinh")
        tinsert(tbSay,"§æi TÝn VËt §Þnh Quèc/doidinhquoc")
        tinsert(tbSay,"§æi TÝn VËt An Bang/doianbang")
		tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
		CreateTaskSay(tbSay)
	return 1
end

function DoiRuongKimQuang()
    tbKimQuangCraft:Main()
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ng­¬i nh×n thÊy hµo quang Hoµng Kim nµy chø? <pic=68><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\chanvuton.lua")
    end
end

function Add_Npc_ChanVuTon()
    local tb_npc_hotro = {
        {1687,3334},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1065)
		local npcName = "Ch©n Vò T«n"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\chanvuton.lua")
    end
end