Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\nangcapphiphong.lua")

----------------------------------------------------------------------------------------------------
--                                         TrÊn Vò T­íng                                          --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>TrÊn Vò T­íng<color>!<enter><enter><color=green>Phi Phong<color> cña ta lu«n lµm kinh ®éng giíi vâ l©m!<enter>§¹i hiÖp, ng­¬i ®· s½n sµng kho¸c lªn m×nh søc nÆng  cña giang s¬n, ®øng trªn ®Ønh cao nh×n xuèng ch­a!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/tranvutuong.lua")
    if Cfg_NangCapPhiPhong ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"N©ng CÊp Phi Phong/nangcapphiphong")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Phi Phong cña bËc v­¬ng gi¶ lu«n tao ra nÐt uy nghi<pic=137><pic=136><pic=135><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\tranvutuong.lua")
    end
end

function Add_Npc_TranVuTuong()
    local tb_npc_hotro = {
        {1572,3316},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1902)
		local npcName = "TrÊn Vò T­íng"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\tranvutuong.lua")
    end
end