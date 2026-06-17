Include("\\script\\task\\newtask\\newtask_head.lua")
Include("\\script\\task\\newtask\\education\\knowmagic.lua")
Include("\\script\\global\\skills_table.lua")
Include( "\\script\\missions\\freshman_match\\head.lua" )
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
npc_name = "Thóy Yªn Hoa Sø"

function default_talk()
	player_Faction = GetFaction()
	Uworld1000 = nt_getTask(1000)
	if (( Uworld1000 == 240 ) or ( Uworld1000 == 250 )) and ( GetLastFactionNumber() == 5 ) then
		nt_setTask(1000,250)
		Talk(1,"Uworld1000_jiaoyucuiyan", "Ng­¬i ®· lµ ®Ö tö cña bæn ph¸i, kh«ng cÇn ph¶i ®i, cã thÓ gÆp trùc tiÕp Long Ngò nãi chuyÖn.")
	elseif (player_Faction == "cuiyan") then
		Say("Muéi l¹i lĞn ®i ch¬i n÷a ­?",4,"Uhm! PhiÒn tû tû ®­a muéi vÒ!/return_yes","T×m hiÓu khu vùc luyÖn c«ng/map_help","T×m hiÓu vâ nghÖ bæn m«n/skill_help","Tû cø gi¶ vê nh­ kh«ng thÊy muéi lµ ®­îc råi!/no")
	elseif (player_Faction == "emei") then
		Talk(1,"","Xinh ®Ñp nh­ hoa t­¬i mµ h»ng ngµy cø ®èi diÖn víi Thanh §¨ng Cæ PhËt!  Ta kh«ng thİch c¸c ng­¬i ë ®iÓm nµy! ")
	elseif (player_Faction == "tangmen") then
		Talk(1,"","Hai ph¸i chóng ta liªn minh l¹i, kh«ng cÇn ph¶i ph©n tranh n÷a! Ch¼ng ph¶i lµ hû sù ­?")
	elseif (player_Faction == "wudu") then
		Talk(1,"","Ta ghĞt nhÊt lµ c¸i bän len lĞn h¹ ®éc h¹i ng­êi!  Êy!  Kh«ng ph¶i ta nãi ng­¬i! ")
	elseif (player_Faction == "tianwang") then
		Talk(1,"","D­¬ng Hå cña Thiªn V­¬ng bang c¸c ng­¬i cßn kĞm xa tû tû cña ta, thiªn h¹ nam nh©n qu¶ nhiªn ch¼ng ra g× hÕt! ")
	elseif (player_Faction == "shaolin") then
		Talk(1,"","Hßa th­îng ®Çu träc sao l¹i ®Õn n¬i cña c¸c n÷ nhi thÕ nµy?! ")
	elseif (player_Faction == "wudang") then
		Talk(1,"","Quı m«n lÊy hiÖp nghÜa lµm träng, nh­ng kh«ng biÕt §¬n T­ Nam cã biÕt hæ thÑn hay kh«ng mµ cø b¸m theo c¸c s­ tû xinh ®Ñp cña chóng ta")
	elseif (player_Faction == "kunlun") then
		Talk(1,"","C«n L«n ph¸i ph¸t triÓn thÇn tèc! TiÓu n÷ rÊt ng­ìng mé! ")
	elseif (player_Faction == "tianren") then
		Talk(1,"","Ai lµm hoµng ®Õ th× còng mÆc!  Nh­ng c¸c ng­¬i tµn s¸t sinh linh th× bæn c« n­¬ng kh«ng bá qua! ")
	elseif (player_Faction == "gaibang") then
		Talk(2,"","§¹i hiÖp!  §õng qua ®©y! ","¸! Bä chĞt!!! ")
	elseif (nt_getTask(75) == 255) then
		Talk(1,"","Tù häc khæ luyÖn!  T¹i h¹ v« cïng béi phôc! ")
	else
		UTask_cy = nt_getTask(6)
		if ((UTask_cy > 5*256) and (UTask_cy < 10*256)) then
			Talk(1,"","Muéi tiÕp nhËn nhiÖm vô nhËp m«n ®· l©u vÉn ch­a hoµn thµnh! ChØ cÇn th«ng qua kh¶o nghiÖm Hoa Kh«i trËn th× chóng ta sÏ lµ tû muéi tèt")
		elseif (UTask_cy >= 70*256) and (player_Faction ~= "cuiyan") then
			Say("Tû tû t¹i sao xuÊt s­ vËy!  Cø ë ®©y tù t¹i ch¼ng ph¶i vui vÎ l¾m sao!?",3,"T×m hiÓu së tr­êng luyÖn c«ng/map_help","T×m hiÓu vâ nghÖ bæn m«n/skill_help","KÕt thóc ®èi tho¹i/no")
		else
			Talk(1,"enroll_select","Thóy Yªn m«n chóng ta tuy toµn lµ n÷, nh­ng vâ nghÖ lÊy 'Khinh, Kho¸i, Kú, Mü' mµ næi tiÕng giang hå. Tû muéi tÒ t©m!  Trong giang hå kh«ng ai d¸m coi khinh")
		end
	end
end

function defection_get()
	Talk(1,"","ChØ cÇn muéi ®Õn thØnh cÇu Thanh HiÓu S­ Th¸i xin chuyÓn qua Thóy Yªn m«n, sau ®ã ®Õn tr×nh kiÕn víi Do·n Hµm Yªn ch­ëng m«n cña ta th× cã thÓ ®­îc!  Yªn t©m!  Thanh HiÓu S­ Th¸i th«ng t×nh ®¹t lı, sÏ kh«ng lµm khã muéi ®©u! ")
	nt_setTask(6,5*256+10)
	Msg2Player("Muèn gia nhËp Thóy Yªn m«n, chØ cÇn ®Õn xin phĞp Thanh HiÓu S­ Th¸i sau ®ã ®Õn tr×nh kiÕn víi Do·n Hµm Yªn lµ ®­îc!")
end

function enroll_select()
	UTask_em = nt_getTask(1)
	UTask_cy = nt_getTask(6)
	if ((UTask_em > 5*256) and (UTask_em < 10*256)) then
		Talk(1,"","L¹i cã thªm 1 ng­êi muèn ®Õn cÇu kinh niÖm phËt! ThËt lµ ®¸ng tiÕc! ")
	elseif (GetSeries() == 2) and (GetCamp() == 0) and (UTask_em < 5*256) and (UTask_cy < 5*256) then
		if (GetLevel() >= 10) then
			Say("Muéi cã muèn gia nhËp Thóy Yªn m«n cña chóng ta kh«ng?", 3, "Gia nhËp Thóy Yªn M«n/go", "§Ó ta suy nghÜ kü l¹i xem/no","T×m hiÓu tinh hoa vâ nghÖ c¸c m«n ph¸i/Uworld1000_knowmagic")
		else
			Talk(1,"","Tr­íc tiªn muéi h·y luyÖn tËp l¹i c¨n b¶n, ®¹t ®Õn <color=Red>cÊp 10<color> chóng ta sÏ lµ tû muéi 1 nhµ! ")
		end
	end
end

function go()
	Uworld1000 = nt_getTask(1000)
	if ( Uworld1000 == 240 ) or ( Uworld1000 == 250 ) then
		nt_setTask(1000,260)
	end
	SetRevPos(154,61)
	nt_setTask(6,10*256)
	SetFaction("cuiyan")
	SetCamp(3)
	SetCurCamp(3)
	nt_setTask(137,66)
	SetLastFactionNumber(5)
	SetRank(77)

	-- Hç trî Kü N¨ng 10-60 khi gia nhËp m«n ph¸i
	if (HoTroKyNangGiaNhapPhai == 1) then
		add_cy(70)
	else
		add_cy(10)
	end
	
	Msg2Player("Hoan nghªnh b¹n gia nhËp Thóy Yªn m«n! Trë thµnh Hoa Tú. Häc ®­îc Phong Hoa TuyÕt NguyÖt, Phong QuyÓn Tµn TuyÕt")
	AddNote("Gia nhËp Thóy Yªn m«n, trë thµnh Hoa Tú")
	Msg2Faction("cuiyan",GetName().." tõ h«m nay gia nhËp Thóy Yªn m«n. Xin b¸i kiÕn c¸c vŞ s­ tû. Mong c¸c vŞ quan t©m chØ gi¸o!",GetName())
	NewWorld(154, 403, 1361)
end

function return_yes()
	NewWorld(154, 403, 1361)
end

function cy_check_yes()
	if (GetSeries() ~= 2) then
		Talk(1,"","Ngò hµnh cña ng­¬i kh¸c víi bän ta!  §Õn ®©y lµm g×? H·y lªn diÔn ®µn häc hái thªm!")
	elseif (HaveMagic(91) >= 0) then
		nt_setTask(1,60*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· cã thÓ ®i nhËn nhiÖm vô xuÊt s­.")
	elseif (HaveMagic(88) >= 0) then
		nt_setTask(1,50*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· hoµn thµnh nhiÖm vô cÊp 40.")
	elseif (HaveMagic(85) >= 0) then
		nt_setTask(1,40*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· hoµn thµnh nhiÖm vô cÊp 30.")
	elseif (HaveMagic(82) >= 0) then
		nt_setTask(1,30*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· hoµn thµnh nhiÖm vô cÊp 20.")
	elseif (HaveMagic(77) >= 0) then
		nt_setTask(1,20*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· hoµn thµnh nhiÖm vô cÊp 10.")
	else
		nt_setTask(1,10*256)
		Talk(1,"","Ta ®· chØnh lı l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i. Ng­¬i ®· cã thÓ nhËn nhiÖm vô cÊp 10.")
	end
end

function cy_check_no()
	Say("Muéi l¹i lĞn ®i ch¬i n÷a ­?",2,"Uhm! PhiÒn tû tû ®­a muéi vÒ!/return_yes","Tû cø gi¶ vê nh­ kh«ng thÊy muéi lµ ®­îc råi!/no")
end

function Uworld1000_jiaoyucuiyan()
	nt_setTask(1000,260)
	Msg2Player("§Ö tö tiÕp dÉn Thóy Yªn m«n b¶o b¹n cã thÓ trùc tiÕp ®i gÆp Long Ngò nãi chuyÖn.")
end

function no()
end