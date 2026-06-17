Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\exchange\\phonghoalenh.lua")

----------------------------------------------------------------------------------------------------
--                                           Ngù Phong Háa                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Ngù Phong Háa<color>!<enter><enter>Héi Qu¸n Vâ L©m nµy lµ n¬i ta dõng ch©n!<enter>Uy danh cña l·o phu ch­a bao giê t¾t!<enter>Muèn ®µm ®¹o víi ta? Tr­íc tiªn h·y mang theo khÝ thÕcña ng­êi kh«ng sî chÕt!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/nguphonghoa.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        if Cfg_PhongHoaLenh == 1 then
            tinsert(tbSay,"Phong Háa LÖnh/doiphonghoalenh")
        end
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ng­¬i thÊy giã rÝt - Ta thÊy thêi c¬! <pic=59><color><bclr>",
            "<bclr=blue><enter>Ng­¬i thÊy löa ch¸y - Ta thÊy vinh quang! <pic=60><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\nguphonghoa.lua")
    end
end

function Add_Npc_NguPhongHoa()
    local tb_npc_hotro = {
        {1680,3339},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1584)
		local npcName = "Ngù Phong Háa"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\nguphonghoa.lua")
    end
end