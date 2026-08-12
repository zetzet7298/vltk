Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\lib\\remoteexc.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\feature\\nhiemvumoingay.lua")
Include("\\script\\global\\mel\\feature\\thunghiemmaychu.lua")
Include("\\script\\global\\mel\\shop\\cuahanghoiquan.lua")

----------------------------------------------------------------------------------------------------
--										   L·nh Ng¹o ThÇn										  --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>Qu¶n Gia<color> ë ®©y!<enter><enter>Quy t¾c cña <color=green>Héi Qu¸n Vâ L©m<color> nµy rÊt ®¬n gi¶n<enter><color=Yellow>TrÝ TuÖ dÉn ®­êng, Vâ Lùc ®Þnh ®o¹t!<color><enter>Quý kh¸ch cÇn ta gióp g× kh«ng?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/lanhngaothan.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
		if ThuNghiemMayChu == 1 then
			tinsert(tbSay,"Thö NghiÖm M¸y Chñ/TestServer")
			tinsert(tbSay,"Cöa Hµng Thö NghiÖm/CuaHangThuNghiem")
			tinsert(tbSay,"Cöa Hµng TuyÖt §Ønh/CuaHangTuyetDinh")
		end
		if NhiemVuMoiNgay == 1 then
            tinsert(tbSay,"NhiÖm Vô Mçi Ngµy/#Task_Daily:main()")
        end
		tinsert(tbSay,"CËp NhËt B¶ng XÕp H¹ng/CapNhatBXH")
		tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

-- CËp nhËt B¶ng XÕp H¹ng
function CapNhatBXH()
	RemoteExc("\\script\\xephang\\worldrank_hook.lua", "RankHook:UpdateRank",{})
	Talk(1, "", "CËp NhËt xÕp h¹ng thµnh c«ng !!")
	return
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
	if NPCAutoChat == 1 then
		local tab_Chat = {
			"<pic=115><pic=115><pic=115><bclr=blue><enter>Chµo mõng quý kh¸ch ®Õn Héi Qu¸n Vâ L©m!<pic=125><color><bclr>",      
		}
		local ran = random(1,getn(tab_Chat))
		NpcChat(nNpcIndex,tab_Chat[ran])
		local ranTimer = random(10,20)
		SetNpcTimer(nNpcIndex,ranTimer*18)
		SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\lanhngaothan.lua")
	end
end

function Add_Npc_LanhNgaoThan()
    local tb_npc_hotro = {
        {1664,3364},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1442)
		local npcName = "L·nh Ng¹o ThÇn"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\lanhngaothan.lua")     
	end
end