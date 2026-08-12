CurStation = 1
Include("\\script\\global\\station.lua")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--										   Väng Giang Kh¸ch										  --
----------------------------------------------------------------------------------------------------
function main(sel)
	local mapid = SubWorldIdx2ID(SubWorld);
	local tbOpp = {
		"Nh÷ng n¬i ®· ®i qua/WayPointFun",
		"Nh÷ng thµnh thÞ ®· ®i qua/StationFun",
		"Trë l¹i ®Þa ®iÓm cò/TownPortalFun",
		"§i §¶o C©u C¸ - B·i 1/daocauca1",
		"§i §¶o C©u C¸ - B·i 2/daocauca2",
	}
	tinsert(tbOpp, "Kh«ng cÇn ®©u/OnCancel")
	Say("B¹n muèn ®i ®Õn n¬i nµo?", getn(tbOpp), tbOpp)
end

----------------------------------------------------------------------------------------------------
function daocauca1()
	NewWorld(1009, 1566, 2511)
	SetFightState(0)
end

function daocauca2()
	NewWorld(1009, 1241, 3081)
	SetFightState(0)
end

function OnCancel()
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
	if NPCAutoChat == 1 then
		local tab_Chat = {
			"<bclr=blue><enter>Chµo nh©n sü, cã muèn ®i c©u c¸ cïng ta kh«ng? <pic=01><color><bclr>",
		}
		local ran = random(1,getn(tab_Chat))
		NpcChat(nNpcIndex,tab_Chat[ran])
		local ranTimer = random(10,20)
		SetNpcTimer(nNpcIndex,ranTimer*18)
		SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\vonggiangkhach.lua")
	end
end

function Add_Npc_VongGiangKhach()
    local tb_npc_hotro = {
        {1789,3533},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (297)
		local npcName = "Väng Giang Kh¸ch"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\vonggiangkhach.lua")
    end
end