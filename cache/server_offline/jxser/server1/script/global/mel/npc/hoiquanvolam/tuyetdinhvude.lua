Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\task\\metempsychosis\\npc_saodiseng.lua")
Include("\\script\\global\\mel\\item\\lenhbaitinhnang.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\matnatuyetdinh.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tuyetdinhtrangsuc.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tuyetdinhphiphong.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tuyetdinhgioichi.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tuyetdinhvukhi.lua")

----------------------------------------------------------------------------------------------------
--                                        TuyÖt §Ønh Vò §Õ                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."<enter><color=green>TuyÖt §Ønh Vò §Õ<color> chØ lµ h­ danh mµ thÕ gian ban tÆng<enter>Ta ch­a bao giê d¸m nhËn m×nh lµ kÎ v« ®Þch!<enter>§¹i hiÖp! §õng nh×n vµo hµo quang cña ta, h·y nh×n   vµo con ®­êng chÝnh nghÜa mµ ng­êi ®ang ®i!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/tuyetdinhvude.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        tinsert(tbSay,"Th¸ch ®Êu TuyÖt §Ønh Vò §Õ/ThachDauTDVD")
        tinsert(tbSay,"Trïng Sinh Nh©n VËt/TrungSinhNhanVat")
        tinsert(tbSay,"N©ng cÊp TuyÖt §Ønh MÆt N¹/MatNaTuyetDinh")
        tinsert(tbSay,"N©ng cÊp TuyÖt §Ønh Trang Søc/TuyetDinhTrangSuc")
        tinsert(tbSay,"N©ng cÊp TuyÖt §Ønh Phi Phong/TuyetDinhPhiPhong")
        tinsert(tbSay,"N©ng cÊp TuyÖt §Ønh Giíi ChØ/TuyetDinhGioiChi")
        tinsert(tbSay,"N©ng cÊp TuyÖt §Ønh Vò KhÝ/TuyetDinhVuKhi")
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

----------------------------------------------------------------------------------------------------
function ThachDauTDVD()
    if Cfg_BossTuyetDinhVuDe ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local tbSay = {
            "Sao c¬ thùc sù nhµ ng­êi muèn th¸ch ®Êu víi ta?<enter>Ng­¬i cÇn chuÈn bÞ:<enter><color=green>- 400 MËt §å ThÇn BÝ<enter>- 60 LÖnh Bµi ThÇn BÝ<enter>- 10 TuyÖt §Ønh Tri Thøc<color><enter>§ñ nh÷ng vËt phÈm trªn míi chøng minh ®­îc ng­¬i cã  n¨ng lùc ®Ó th¸ch ®Êu víi <bclr=pink>Vâ L©m Minh Chñ<bclr> nµy ®Êy!",
            "Ta ®· cã ®ñ råi! §¸nh mét trËn nµo!/DoThachDauTDVD",
            "Kh«ng ta chØ ®ïa th«i./No"
        }
        CreateTaskSay(tbSay)
    end
end

function DoThachDauTDVD()
    local nMatDoThanBi = CalcEquiproomItemCount(6,1,196,-1)
    if nMatDoThanBi < 400 then
        Say("Ng­¬i kh«ng cã ®ñ <color=green>400 MËt §å ThÇn BÝ<color>.", 0) return
    end
    local nLenhBaiThanBi = CalcEquiproomItemCount(6,1,4958,-1)
    if nLenhBaiThanBi < 60 then
        Say("Ng­¬i kh«ng cã ®ñ <color=green>60 LÖnh Bµi ThÇn BÝ<color><enter>VËt phÈm kh«ng cã mµ cßn ®ßi th¸ch ®Êu?", 0) return
    end
    local nTuyetDinhTriThuc = CalcEquiproomItemCount(4,2054,1,1)
    if nTuyetDinhTriThuc < 10 then
        Say("Ng­¬i kh«ng cã ®ñ <color=green>10 TuyÖt §Ønh Tri Thøc<color><enter>Haha! Ta thÊy ng­¬i h·y tu luyÖn thªm cho ®ñ Tri Thøcråi h·y quay l¹i ®©y.", 0) return
    end
    ConsumeEquiproomItem(400,6,1,196,-1)
    ConsumeEquiproomItem(60,6,1,4958,-1)
    ConsumeEquiproomItem(10,4,2045,1,1)
    StartMissions(7)
    Talk(1,"","<color=green>Haha! Kh¸ l¾m, ta sÏ chê ng­¬i ë ngoµi thµnh!<color>")
end

----------------------------------------------------------------------------------------------------
tbNoiDungTS = {
    [1] = "§iÒu kiÖn Trïng Sinh 1:<enter><color=green>- §¹t cÊp ®é 200<enter>- §· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<enter>- 10.000 v¹n l­îng<color><enter><enter>Sau khi Trïng Sinh nhËn ®­îc <color=orange>100 ®iÓm TiÒm N¨ng, Giíi h¹n t¨ng Kü N¨ng +1<color>",
    [2] = "§iÒu kiÖn Trïng Sinh 2:<enter><color=green>- §¹t cÊp ®é 200<enter>- §· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<enter>- 10.000 v¹n l­îng<color><enter><enter>Sau khi Trïng Sinh nhËn ®­îc <color=orange>100 ®iÓm TiÒm N¨ng, Giíi h¹n t¨ng Kü N¨ng +1, Giíi h¹n Kh¸ng TÊt C¶ +1<color>",
    [3] = "§iÒu kiÖn Trïng Sinh 3:<enter><color=green>- §¹t cÊp ®é 200<enter>- §· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<enter>- 10.000 v¹n l­îng<color><enter><enter>Sau khi Trïng Sinh nhËn ®­îc <color=orange>100 ®iÓm TiÒm N¨ng, Giíi h¹n t¨ng Kü N¨ng +1, Giíi h¹n Kh¸ng TÊt C¶ +1<color>",
    [4] = "§iÒu kiÖn Trïng Sinh 4:<enter><color=green>- §¹t cÊp ®é 200<enter>- §· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<enter>- 20.000 v¹n l­îng<enter>- 1000 Vâ L©m LÖnh, 1000 Phong Háa LÖnh, 500 Tèng Kim LÖnh, 100 Hoµng Kim LÖnh, 100 TuyÖt §Ønh Tri Thøc<color><enter>Sau khi Trïng Sinh nhËn ®­îc <color=orange>200 ®iÓm TiÒm N¨ng, Giíi h¹n t¨ng Kü N¨ng +2, Giíi h¹n Kh¸ng TÊt C¶ +2<color>",
    [5] = "§iÒu kiÖn Trïng Sinh 5:<enter><color=green>- §¹t cÊp ®é 200<enter>- §· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<enter>- 10.000 v¹n l­îng<color><enter><enter>Sau khi Trïng Sinh nhËn ®­îc <color=orange>100 ®iÓm TiÒm N¨ng, Giíi h¹n t¨ng Kü N¨ng +1, Giíi h¹n Kh¸ng TÊt C¶ +1<color>",
}

function No()
end

function TrungSinhNhanVat()
    local szMsg = "Trïng Sinh h¶?<enter>Ng­¬i ®· ®ñ ®iÒu kiÖn ch­a mµ ®ßi Trïng Sinh?<enter>NÕu ch­a biÕt ®iÒu kiÖn lµ g× th× ta cã thÓ cho nhµ  ng­¬i biÕt."
    local tbSay = {
        szMsg,
        "§ñ ®iÒu kiÖn råi, h·y gióp ta Trïng Sinh/DoTrungSinhNhanVat",
        "§iÒu kiÖn ®Ó Trïng Sinh lµ g×?/DieuKienTrungSinh",
        "§Ó ta suy nghÜ ®·/No"
    }
    CreateTaskSay(tbSay)
end

function DoTrungSinhNhanVat()
    if (Cfg_TrungSinh ~= 1) then
        Talk(1, "", "<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
        return
    end
    zhuansheng_want_learn()
end

function DieuKienTrungSinh()
    local szMsg = "Mçi nh©n sü cã thÓ <color=green>Trïng Sinh tèi ®a 5 lÇn<color>!<enter>§Ó cã thÓ Trïng Sinh cÇn ®¹t <color=green>CÊp §é 200 vµ ®· häc B¾c§Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn<color>!<enter><enter><color=yellow>H·y lùa chän ®Ó biÕt chi tiÕt h¬n vÒ Trïng Sinh!<color>"
    local tbSay = { szMsg }
    for i = 1, 5 do
        tinsert(tbSay, "Th«ng tin Trïng Sinh "..i.."/ShowThongTinTS")
    end
    tinsert(tbSay, "§­îc råi ta ®· n¾m râ./No")
    CreateTaskSay(tbSay)
end

function ShowThongTinTS(nIndex)
    local nRealIndex = nIndex + 1
    local szMsg = tbNoiDungTS[nRealIndex]
    if (szMsg == nil) then 
        return No() 
    end
    local tbSay = {
        szMsg,
        "Quay l¹i/DieuKienTrungSinh",
        "§­îc råi ta ®· n¾m râ./No"
    }
    CreateTaskSay(tbSay)
end

----------------------------------------------------------------------------------------------------
function MatNaTuyetDinh()
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        tbMaskTuyetDinhUpgrade:Main()
    end
end

function TuyetDinhTrangSuc()
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        nangcaptrangsuctuyetdinh()
    end
end

function TuyetDinhPhiPhong()
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        tbUltimateCloak:Main()
    end
end

function TuyetDinhGioiChi()
    if Cfg_TuyetDinhTrangBi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        tbRingUpgrade:Main()
    end
end

function TuyetDinhVuKhi()
    if Cfg_TuyetDinhVuKhi ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        tbUltimateUpgrade:SelectSect()
    end
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<color=green> Ta lµ <color><bclr=pink>Vâ L©m Minh Chñ<bclr><enter><color=green>§øng trªn ®Ønh cao míi thÊy nhá bÐ gi÷a cµn kh«n v«tËn! <pic=27><pic=27><pic=27><color>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\tuyetdinhvude.lua")
    end
end

function Add_Npc_TuyetDinhVuDe()
    local tb_npc_hotro = {
        {1746,3362},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1801)
		local npcName = "TuyÖt §Ønh Vò §Õ"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\tuyetdinhvude.lua")
    end
end