Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\shop\\cuahanghoiquan.lua")

----------------------------------------------------------------------------------------------------
--                                         Ng©n Linh Nhi                                          --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Ng©n Linh Nhi<color>!<enter>Chµo mõng Quý Kh¸ch ®Õn víi <color=green>Cöa hµng Ng©n L­îng<color>!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/nganlinhnhi.lua")
    if Cfg_CuaHangTienVan ~= 1 then
        Talk(1,"","<color=green>Cöa hµng Ng©n L­îng hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"Cöa hµng Ng©n L­îng/CuaHangNganLuong")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ng©n l­îng leng keng, Linh Nhi ®ãn kh¸ch! <pic=05><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\nganlinhnhi.lua")
    end
end

function Add_Npc_NganLinhNhi()
    local tb_npc_hotro = {
        {1621,3369},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1140)
		local npcName = "Ng©n Linh Nhi"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\nganlinhnhi.lua")
    end
end