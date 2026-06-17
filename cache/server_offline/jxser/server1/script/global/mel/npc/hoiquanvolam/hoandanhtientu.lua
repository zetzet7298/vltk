Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\doitennhanvat.lua")


----------------------------------------------------------------------------------------------------
--                                       Ho¸n Danh Tiªn Tö                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Ho¸n Danh Tiªn Tö<color>!<enter><enter>Dßng mùc thiªn tiªn nµy cña ta mét khi h¹ bót, tªn còsÏ tan thµnh m©y khãi, giang hå tõ nay sÏ chØ cßn l­udanh mét vÞ hµo kiÖt hoµn toµn míi.<enter>Ng­¬i... ®· s½n sµng t¸i sinh ch­a?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/hoandanhtientu.lua")
    if Cfg_DoiTenNhanVat ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"§æi Tªn Nh©n VËt/DoiTenNhanVat")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>NhÊt bót c¶i vËn - NhÊt mÆc ho¸n danh! <pic=59><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\hoandanhtientu.lua")
    end
end

function Add_Npc_HoanDanhTienTu()
    local tb_npc_hotro = {
        {1680,3294},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1839)
		local npcName = "Ho¸n Danh Tiªn Tö"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\hoandanhtientu.lua")
    end
end