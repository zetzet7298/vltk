Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\nangcapngua.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tudaithanma.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\hoandoithanma.lua")

----------------------------------------------------------------------------------------------------
--                                           Kim C©u Th¸nh                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Kim C©u Th¸nh<color>!<enter><enter><color=Yellow>Ng­¬i thÊy ¸nh vµng lÊp l¸nh nµy sao?<color><enter>Ta kh«ng rÌn ngùa cho kÎ d¹o ch¬i phè thÞ!<enter>Mét khi chiÕn m· kho¸c lªn líp Hoµng Kim nµy, nã sÏ  ®¹p n¸t v¹n dÆm s¬n hµ!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/kimcauthanh.lua")
    if Cfg_NangCapNgua ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"N©ng CÊp ChiÕn M· Hoµng Kim/ChienMaHoangKim")
            tinsert(tbSay,"N©ng CÊp Tø §¹i ThÇn M·/TuDaiThanMa")
            tinsert(tbSay,"Ho¸n §æi Tø §¹i ThÇn M·/HoanDoiThanMa")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
	return 1
    end
end

----------------------------------------------------------------------------------------------------
function ChienMaHoangKim()
    local szTitle = "<color=Yellow>Kim C©u Th¸nh<color> - BËc thÇy vÒ Ngùa! <enter><enter>ChØ cã <color=Yellow>H¶o H¸n<color> míi cã thÓ n©ng cÊp ChiÕn M·.<enter><enter>Ng­¬i muèn n©ng cÊp lo¹i <color=yellow>Hoµng Kim ChiÕn M·<color> nµo?"
    local tbOp = {
        {"N©ng cÊp B«n Tiªu Hoµng Kim", tamuonnangcap, {"Phi V©n Hoµng Kim"}},
        {"N©ng cÊp Phiªn Vò Hoµng Kim", tamuonnangcap, {"B«n Tiªu Hoµng Kim"}},
        {"N©ng cÊp XÝch Long C©u Hoµng Kim", tamuonnangcap, {"Phiªn Vò Hoµng Kim"}},
        {"N©ng cÊp Du Huy Hoµng Kim", tamuonnangcap, {"XÝch Long C©u Hoµng Kim"}},
        {"N©ng cÊp Siªu Quang Hoµng Kim", tamuonnangcap, {"Du Huy Hoµng Kim"}},
        {"N©ng cÊp H·n HuyÕt Long C©u Hoµng Kim", tamuonnangcap, {"Siªu Quang Hoµng Kim"}},
        {"Kh«ng cã g×, ta chØ ®i ngang qua"}
    }
    CreateNewSayEx(szTitle, tbOp)
end

function tamuonnangcap(szOldHorseName)
    tbUpgradeSystem:ConfirmUpgrade(szOldHorseName)
end

----------------------------------------------------------------------------------------------------
function TuDaiThanMa()
    tbUpgradeThanMa:ConfirmUpgrade()
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Hoµng Kim ThÇn M· kh«ng dµnh cho kÎ yÕu ®uèi! <pic=64><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\kimcauthanh.lua")
    end
end

function Add_Npc_KimCauThanh()
    local tb_npc_hotro = {
        {1562,3326},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1200)
		local npcName = "Kim C©u Th¸nh"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\kimcauthanh.lua")
    end
end