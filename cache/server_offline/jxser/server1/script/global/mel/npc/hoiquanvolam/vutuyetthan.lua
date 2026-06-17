Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\exchange\\volamlenh.lua")

----------------------------------------------------------------------------------------------------
--                                           Vò TuyÖt ThÇn                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Vò TuyÖt ThÇn<color>!<enter><enter><color=Green>Vâ L©m LÖnh<color> chÝnh lµ thiªn mÖnh, quyÒn uy tèi th­îng<enter>Ng­¬i cã ®ñ søc m¹nh ®Ó g¸nh v¸c hai ch÷ ThÇn LÖnh   nµy kh«ng!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/vutuyetthan.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        if Cfg_VoLamLenh == 1 then
            tinsert(tbSay,"Vâ L©m LÖnh/doivolamlenh")
        end
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Vâ l©m hçn lo¹n, cÇn quy t¾c ®Ó ®Þnh h×nh! <pic=21><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\vutuyetthan.lua")
    end
end

function Add_Npc_VuTuyetThan()
    local tb_npc_hotro = {
        {1686,3345},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1901)
		local npcName = "Vò TuyÖt ThÇn"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\vutuyetthan.lua")
    end
end