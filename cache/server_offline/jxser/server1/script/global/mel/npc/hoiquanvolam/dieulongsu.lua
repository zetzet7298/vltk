Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\nangcaptrangsuc.lua")

----------------------------------------------------------------------------------------------------
--                                         §iªu Long S­                                           --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>§iªu Long S­<color>!<enter><enter>Ta kh«ng chØ t¹c ngäc...<enter>Ta dïng c«ng lùc ®¸nh thøc <color=green>Linh Hån<color><enter>Nµo! H·y chän lo¹i trang søc cÇn n©ng cÊp!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/dieulongsu.lua")
    if Cfg_NangCapTrangSuc ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            if Cfg_NangCapTrangSuc == 1 then
                tinsert(tbSay,"N©ng CÊp Trang Søc/nangcaptrangsuc")
                tinsert(tbSay,"N©ng CÊp Trang Søc Hoµng Kim/trangsuchoangkim")
            end
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ch¹m mét nÐt lµ §Þnh ThÇn, kh¾c mét ®­êng lµ Tô KhÝ<pic=61><pic=61><pic=61><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\dieulongsu.lua")
    end
end

function Add_Npc_DieuLongSu()
    local tb_npc_hotro = {
        {1583,3312},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (365)
		local npcName = "§iªu Long S­"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\dieulongsu.lua")
    end
end