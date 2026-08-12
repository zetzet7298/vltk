Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\nangcapan.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\hoandoian.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\hoandoiantuyetdinh.lua")

----------------------------------------------------------------------------------------------------
--                                         Ngò Nguyªn S­                                          --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Ngò Nguyªn S­<color>!<enter><enter>Ta kh«ng ®óc s¾t mµ lµ ®óc <color=green>VËn MÖnh<color><enter>Nµo! H·y chän lo¹i Ên cÇn n©ng cÊp!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/ngunguyensu.lua")
    if Cfg_NangCapAn ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"N©ng CÊp Ên/nangcapan")
            tinsert(tbSay,"N©ng CÊp Ên Hoµng Kim/nangcapanhoangkim")
            tinsert(tbSay,"Ho¸n §æi Ên Hoµng Kim/hoandoian")
            tinsert(tbSay,"Ho¸n §æi Ên TuyÖt §Ønh/hoandoiantuyetdinh")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ngò hµnh t­¬ng sinh lµ Phóc, t­¬ng kh¾c lµ Häa! <pic=19><pic=19><pic=19><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\ngunguyensu.lua")
    end
end

function Add_Npc_NguNguyenSu()
    local tb_npc_hotro = {
        {1580,3309},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (366)
		local npcName = "Ngò Nguyªn S­"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\ngunguyensu.lua")
    end
end