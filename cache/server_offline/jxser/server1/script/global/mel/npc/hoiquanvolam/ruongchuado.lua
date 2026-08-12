Include("\\script\\global\\mel\\configserver.lua")
----------------------------------------------------------------------------------------------------
--										 	R­¬ng Chøa §å										  --
----------------------------------------------------------------------------------------------------
function main()
	OpenBox()
	SetRevPos(1010,1)
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>C¸c nh©n sü cã thÓ göi vËt phÈm ë ®©y!<color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\ruongchuado.lua")
    end
end

function Add_Npc_RuongChuaDo()
    local tb_npc_hotro = {
        {1648,3375},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (625)
		local npcName = "ThiÕt Khè Tiªn Sinh"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\ruongchuado.lua")
    end
end