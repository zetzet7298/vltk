Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")
Include("\\script\\global\\mel\\shop\\cuahanghoiquan.lua")

----------------------------------------------------------------------------------------------------
--                                         ThiÕt M· Hïng                                    	  --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta lµ <color=green>ThiÕt M· Hïng<color>!<enter>Chµo mõng Quý Kh¸ch ®Õn víi <color=green>Cöa hµng Ngùa<color>!"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/thietmahung.lua")
	if Cfg_CuaHangNgua ~= 1 then
		Talk(1,"","<color=green>Cöa hµng Ngùa hiÖn ch­a më!<color>")
	else
		local player_name = GetName()
		local tbSay = {format(TITLEDIALOG, GetName())}
			tinsert(tbSay,"Cöa hµng Ngùa/CuaHangChienMa")
			tinsert(tbSay,"Phi V©n Hoµng Kim/PhiVanHoangKim")
			tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
		CreateTaskSay(tbSay)
		return 1
	end
end

----------------------------------------------------------------------------------------------------
function PhiVanHoangKim()
	local szMsg = "§Ó së h÷u <color=Yellow>Phi V©n Hoµng Kim<color> cÇn cã:<enter>"..
                  "<color=orange>- ¤ Van §¹p TuyÕt<enter>"..
                  "- XÝch Thè<enter>"..
                  "- TuyÖt ¶nh<enter>"..
                  "- §Ých L«<enter>"..
                  "- ChiÕu D¹ Ngäc S­ Tö<enter>"..
                  "- 10 Linh Hån ChiÕn M·<enter>"..
				  "- B¾c §Èu ThuÇn M· ThuËt<color>"
    local tbSayPVHK = {
        szMsg,
        "Ta ®· mang ®Õn ®ñ råi!/dophivanhoangkim",
        "§Ó t«i suy nghÜ ®·./OnCancel"
    }
    CreateTaskSay(tbSayPVHK)
end

function dophivanhoangkim()
	if CalcEquiproomItemCount (0,10,5,6) < 1 then
		Say("§ïa ta µ?<enter><color=green>TuyÖt ThÕ Danh M·<color> <color=orange>¤ V©n §¹p TuyÕt<color> ®©u?")
		return
	end
	if CalcEquiproomItemCount (0,10,5,7) < 1 then
		Say("§ïa ta µ?<enter><color=green>TuyÖt ThÕ Danh M·<color> <color=orange>XÝch Thè<color> ®©u?")
		return
	end
	if CalcEquiproomItemCount (0,10,5,8) < 1 then
		Say("§ïa ta µ?<enter><color=green>TuyÖt ThÕ Danh M·<color> <color=orange>TuyÖt ¶nh<color> ®©u?")
		return
	end
	if CalcEquiproomItemCount (0,10,5,9) < 1 then
		Say("§ïa ta µ?<enter><color=green>TuyÖt ThÕ Danh M·<color> <color=orange>§Ých L«<color> ®©u?")
		return
	end
	if CalcEquiproomItemCount (0,10,5,10) < 1 then
		Say("§ïa ta µ?<enter><color=green>TuyÖt ThÕ Danh M·<color> <color=orange>ChiÕu D¹ Ngäc S­ Tö<color> ®©u?")
		return
	end
	if CalcEquiproomItemCount (4,2052,1,1) < 9 then
		Say("§ïa ta µ?<enter>Sao kh«ng cã <color=green>10 Linh Hån ChiÕn M·<color>?")
		return
	end
	if CalcEquiproomItemCount (6,1,4894,-1) < 1 then
		Say("§ïa ta µ?<enter>Sao kh«ng cã <color=green>B¾c §Èu ThuÇn M· ThuËt<color>?")
		return
	end
	ConsumeEquiproomItem (1,0,10,5,6)
	ConsumeEquiproomItem (1,0,10,5,7)
	ConsumeEquiproomItem (1,0,10,5,8)
	ConsumeEquiproomItem (1,0,10,5,9)
	ConsumeEquiproomItem (1,0,10,5,10)
	ConsumeEquiproomItem (10,4,2052,1,1)
	ConsumeEquiproomItem (1,6,1,4894,-1)
	ItemIndex = AddGoldItem(0, 1067)
	SyncItem(ItemIndex)
	Msg2Player("<color=green>Chóc mõng b¹n ®· nhËn ®­îc chiÕn m·<color> <color=yellow>Phi V©n Hoµng Kim<color>")
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
	if NPCAutoChat == 1 then
		local tab_Chat = {
			"<bclr=blue><enter>KhÝ lùc ®Ó gh× chÆt yªn c­¬ng... <pic=02><color><bclr>",
		}
		local ran = random(1,getn(tab_Chat))
		NpcChat(nNpcIndex,tab_Chat[ran])
		local ranTimer = random(10,20)
		SetNpcTimer(nNpcIndex,ranTimer*18)
		SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\thietmahung.lua")
	end
end

function Add_Npc_ThietMaHung()
    local tb_npc_hotro = {
        {1629,3376},
    }
    local nMapIndex = SubWorldID2Idx(1010)
    for i=1,getn(tb_npc_hotro) do
		local npcID = (1802)
		local npcName = "ThiÕt M· Hïng"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\thietmahung.lua")
    end
end