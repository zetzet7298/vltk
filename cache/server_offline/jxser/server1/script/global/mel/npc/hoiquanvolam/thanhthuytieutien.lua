Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\feature\\hoandoidoxanh.lua")
Include("\\script\\task\\system\\task_string.lua")
Include("\\script\\activitysys\\playerfunlib.lua")
Include("\\script\\task\\random\\task_head.lua")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--                                        Thanh Thñy TiÓu Tiªn                                    --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."<enter>§õng thÊy <color=green>Thanh Thñy TiÓu Tiªn<color> ta cßn trÎ mµ véi quayl­ng nhÐ!<enter>Ta cã thÓ dïng Linh KhÝ cña dßng n­íc xanh gét röa   bôi trÇn, c¸c lo¹i ®å vËt hay binh khÝ cò cã thÓ t×m l¹i hµo quang ®Êy!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/thanhthuytieutien.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        tinsert(tbSay,"Ho¸n §æi Vò KhÝ Xanh/HoanDoiVuKhiXanh")
        tinsert(tbSay,"ChÕ T¹o Trang BÞ Xanh/CheTaoTrangBiXanh")
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
function HoanDoiVuKhiXanh()
    if Cfg_HoanDoiVuKhiXanh ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        ITEMBLUE_MakeItem()
    end
end

function CheTaoTrangBiXanh()
    if Cfg_CheTaoTrangBiXanh ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        ITEMBLUE_MakeItemBlue()
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>V¹n vËt trong thiªn h¹... tµn lôi råi sÏ l¹i rùc rì<pic=01><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\thanhthuytieutien.lua")
    end
end

function Add_Npc_ThanhThuyTieuTien()
    local tb_npc_hotro = {
        {1808,3528},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1139)
		local npcName = "Thanh Thñy TiÓu Tiªn"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\thanhthuytieutien.lua")
    end
end