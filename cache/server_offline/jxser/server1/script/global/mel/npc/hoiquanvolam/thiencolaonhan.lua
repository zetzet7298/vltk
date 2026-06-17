Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\item\\lenhbaitinhnang.lua")

----------------------------------------------------------------------------------------------------
--                                       Thiªn C¬ L·o Nh©n                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."<enter>Haha! §õng hái l·o phu vÒ ngµy mai, h·y hái xem t©m ng­¬i ®· s½n sµng cho ngµy h«m nay ch­a?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/thiencolaonhan.lua")
    if Cfg_TrieuHoiBossHoangKim ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"TriÖu håi Boss TiÓu Hoµng Kim/TrieuHoiBossTieu")
            tinsert(tbSay,"TriÖu håi Boss §¹i Hoµng Kim/TrieuHoiBossDai")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function TrieuHoiBossTieu()
    local nMatDoThanBi = CalcEquiproomItemCount(6,1,196,-1)
    if nMatDoThanBi > 199 then
        ConsumeEquiproomItem(200,6,1,196,-1)
        StartMissions(1)
        Talk(1,"","<color=green>Tèt l¾m! Boss TiÓu Hoµng Kim ®· hiÖn th©n råi<enter>H·y mau ®i tiªu diÖt chóng!<color>")
    else
        Talk(1,"","Mang ®Õn <color=green>200 MËt §å ThÇn BÝ<color> ta míi cã thÓ gióp ng­¬i t×m vÞ trÝ <color=yellow>Boss TiÓu Hoµng Kim<color> ®­îc!")
    end
end

function TrieuHoiBossDai()
    local nLenhBaiThanBi = CalcEquiproomItemCount(6,1,4958,-1)
    if nLenhBaiThanBi > 39 then
        ConsumeEquiproomItem(40,6,1,4958,-1)
        StartMissions(2)
        Talk(1,"","<color=green>Ta ®· triÖu håi Boss §¹i Hoµng Kim råi ®Êy<enter>H·y mau ®i tiªu diÖt chóng!<color>")
    else
        Talk(1,"","Mang ®Õn <color=green>40 LÖnh Bµi ThÇn BÝ<color> ta míi cã thÓ gióp ng­¬itriÖu håi <color=yellow>Boss §¹i Hoµng Kim<color> ®­îc!")
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<enter><color=green>QuÎ gieo xuèng, hµo ©m hµo d­¬ng ®· ®Þnh!<color> <pic=01>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\thiencolaonhan.lua")
    end
end

function Add_Npc_ThienCoLaoNhan()
    local tb_npc_hotro = {
        {1631,3404},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (286)
		local npcName = "Thiªn C¬ L·o Nh©n"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\thiencolaonhan.lua")
    end
end