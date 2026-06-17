Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\shop\\cuahanghoiquan.lua")

----------------------------------------------------------------------------------------------------
--                                          Ngù Vinh ThÇn                                         --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Ngù Vinh ThÇn<color>!<enter>Chµo mõng Quý Kh¸ch ®Õn víi <color=green>Cöa hµng Vinh Dù<color>!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/nguvinhthan.lua")
	if Cfg_CuaHangVinhDu ~= 1 then
		Talk(1,"","<color=green>Cöa hµng Vinh Dù hiÖn ch­a më!<color>")
	else
		local player_name = GetName()
		local tbSay = {format(TITLEDIALOG, GetName())}
			tinsert(tbSay,"Cöa hµng Vinh Dù/CuaHangVinhDu")
			tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
		CreateTaskSay(tbSay)
		return 1
	end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
	if NPCAutoChat == 1 then
		local tab_Chat = {
			"<bclr=blue><enter>BËc nam tö nªn coi träng Vinh Dù! <pic=77><color><bclr>",
		}
		local ran = random(1,getn(tab_Chat))
		NpcChat(nNpcIndex,tab_Chat[ran])
		local ranTimer = random(10,20)
		SetNpcTimer(nNpcIndex,ranTimer*18)
		SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\nguvinhthan.lua")
	end
end

function Add_Npc_NguVinhThan()
    local tb_npc_hotro = {
        {1606,3375},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1122)
		local npcName = "Ngù Vinh ThÇn"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\nguvinhthan.lua")
    end
end