Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\matnamayman.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\matnakinhnghiem.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\matnahoiquan.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\matnadienquan.lua")

----------------------------------------------------------------------------------------------------
--                                         Thiªn DiÖn Qu©n                                        --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Thiªn DiÖn Qu©n<color>!<enter><enter>Trong giang hå hçn lo¹n nµy, thËt gi¶ lu«n lÉn lén!<enter>Ta kh«ng b¸n mÆt n¹ mµ b¸n mét cuéc ®êi míi!<enter>Nh­ng h·y cÈn thËn! Mét khi ®· ®eo vµo, liÖu ng­¬i   cßn nhí m×nh lµ ai?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/thiendienquan.lua")
    if Cfg_NangCapMatNa ~= 1 then
        Talk(1,"","<color=green>TÝnh n¨ng nµy hiÖn ch­a më!<color>")
    else
        local player_name = GetName()
        local tbSay = {format(TITLEDIALOG, GetName())}
            tinsert(tbSay,"N©ng CÊp MÆt N¹ May M¾n/MatNaMayMan")
            tinsert(tbSay,"N©ng CÊp MÆt N¹ Kinh NghiÖm/MatNaKinhNghiem")
            tinsert(tbSay,"N©ng CÊp MÆt N¹ Héi Qu¸n/MatNaHoiQuan")
            tinsert(tbSay,"N©ng CÊp MÆt N¹ DiÖn Qu©n/MatNaDienQuan")
            tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
        CreateTaskSay(tbSay)
        return 1
    end
end

----------------------------------------------------------------------------------------------------
function MatNaMayMan() tbMaskMMUpgrade:Main() end
function MatNaKinhNghiem() tbMaskExpUpgrade:Main() end
function MatNaHoiQuan() tbMaskHoiQuanUpgrade:Main() end
function MatNaDienQuan() tbMaskDienQuanUpgrade:Main() end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
    if NPCAutoChat == 1 then
        local tab_Chat = {
            "<bclr=blue><enter>Ng­¬i nh×n thÊy g×? Mét khu«n mÆt... hay chØ lµ mét¶o ¶nh! <pic=63><color><bclr>",
        }
        local ran = random(1,getn(tab_Chat))
        NpcChat(nNpcIndex,tab_Chat[ran])
        local ranTimer = random(10,20)
        SetNpcTimer(nNpcIndex,ranTimer*18)
        SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\thiendienquan.lua")
    end
end

function Add_Npc_ThienDienQuan()
    local tb_npc_hotro = {
        {1596,3317},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1871)
		local npcName = "Thiªn DiÖn Qu©n"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\thiendienquan.lua")
    end
end