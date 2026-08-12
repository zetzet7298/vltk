Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\shop\\cuahanghoiquan.lua")

----------------------------------------------------------------------------------------------------
--                                        V¹n Ph¸p V« Danh                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "§¹i hiÖp, ng­êi muèn t×m kiÕm mét chiªu thøc ®Ó th¾ngng­êi, hay ®ang t×m kiÕm ch©n lý ®Ó th¾ng chÝnh m×nh?<enter>Ng­¬i muèn thÊy sù biÕn hãa cña v¹n ph¸p hay muèn t×ml¹i b¶n ng· ®· mÊt gi÷a nh÷ng trang bÝ kÝp phï hoa?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/vanphapvodanh.lua")
    if Cfg_CuaHangBiKip ~= 1 then
        Talk(1,"","<color=green>Cöa hµng BÝ KÝp hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"Cöa hµng BÝ KÝp/CuaHangBiKip")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<color=orange><enter>Khi ta cÇm kiÕm, ta lµ KiÕm!<enter>Khi ta dïng quyÒn, ta lµ QuyÒn<color>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\vanphapvodanh.lua")
    end
end

function Add_Npc_VanPhapVoDanh()
    local tb_npc_hotro = {
        {1715,3387},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (333)
		local npcName = "V¹n Ph¸p V« Danh"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\vanphapvodanh.lua")
    end
end