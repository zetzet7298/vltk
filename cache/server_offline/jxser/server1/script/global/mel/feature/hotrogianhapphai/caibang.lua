Include("\\script\\task\\newtask\\newtask_head.lua")
Include("\\script\\task\\newtask\\education\\knowmagic.lua")
Include("\\script\\global\\skills_table.lua")
Include( "\\script\\missions\\freshman_match\\head.lua" )
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
npc_name = "§Ö Tö C¸i Bang"

function default_talk()
	player_Faction = GetFaction()
	Uworld1000 = nt_getTask(1000)
	if (( Uworld1000 == 240 ) or ( Uworld1000 == 250 )) and ( GetLastFactionNumber() == 6 ) then
			nt_setTask(1000,250)
			Talk(1,"Uworld1000_jiaoyugaibang", "Ng­¬i ®· lµ ®Ö tö cña bæn gi¸o, kh«ng cÇn ®i n÷a, cã thÓ trùc tiÕp gÆp Long Ngò nãi chuyÖn.")
	elseif (player_Faction == "cuiyan") then
		Talk(1,"","Tõ l©u nghe tiÕng c¸c c« n­¬ng Thóy Yªn ®Ñp nh­ tiªn n÷, vŞ c« n­¬ng nµy cho ¨n mµy ta İt tiÒn uèng r­îu ®­îc ch¨ng?")
	elseif (player_Faction == "emei") then
		Talk(1,"","KÎ ¨n mµy nµy vèn tõ l©u ng­ìng mé Nga My kiÕm thuËt. H«m nay ®­îc gÆp n÷ hiÖp ®©y, thËt tháa lßng mong ­íc!")
	elseif (player_Faction == "tangmen") then
		Talk(1,"","¸m khİ §­êng m«n khiÕn nhiÒu kÎ võa nghe danh ®· biÕn s¾c. §ao ph¸p cña quı m«n còng lîi h¹i kh«ng kĞm!")
	elseif (player_Faction == "wudu") then
		Talk(1,"","X­a nay Tµ chİnh ph©n minh, bän tiÓu nh©n dïng ®éc nh­ c¸c ng­¬i còng d¸m khoe danh ­?")
	elseif (player_Faction == "tianwang") then
		Talk(1,"","Bæn bang vµ quı bang hîp thµnh Thiªn h¹ l­ìng ®¹i ph¸i, chóng ta nªn gióp ®ì nhau nhiÒu h¬n ®Ó thèng lÜnh vâ l©m! Ha! Ha! Ha!")
	elseif (player_Faction == "shaolin") then
		Talk(1,"","Quı ph¸i v× sù tån vong cña Vâ l©m Trung nguyªn, d¸m ®­¬ng ®Çu víi Kim quèc hïng m¹nh! KÎ ¨n mµy nµy mu«n phÇn béi phôc")
	elseif (player_Faction == "wudang") then
		Talk(1,"","Vâ §ang hiÖp kh¸ch lõng danh thiªn h¹, xøng lµ Danh m«n chİnh ph¸i! Xin cho kÎ ¨n mµy nµy göi lêi hái th¨m ®Õn §¹o NhÊt Ch©n Nh©n vµ c¸c vŞ §¹o tr­ëng!")
	elseif (player_Faction == "kunlun") then
		Talk(1,"","Th× ra lµ ®Ö tö cña C«n L«n ph¸i. Th¶o nµo cèt c¸ch bÊt phµm! Râ mÆt anh tµi!")
	elseif (player_Faction == "tianren") then
		Talk(1,"","MÆc dï quı ph¸i lµ chñ, nh­ng C¸i bang vµ Thiªn NhÉn x­a nay vèn kh«ng ®éi trêi chung. Chóng ta kh«ng cÇn ph¶i nhiÒu lêi. HÑn gÆp nhau trªn sa tr­êng!")
	elseif (player_Faction == "gaibang") then
		Say("Cã lêi g× muèn nãi víi bang chñ kh«ng? Hay muèn ta ®­a vÒ Tæng ®µn?",4,"Xin ®­a t¹i h¹ vÒ Tæng ®µn!/return_yes","T×m hiÓu khu vùc luyÖn c«ng/map_help","T×m hiÓu vâ nghÖ bæn m«n/skill_help","Xin chuyÓn dïm lêi hái th¨m s­ phô /no")
	elseif (nt_getTask(75) == 255) then
		Talk(1,"","Tù häc khæ luyÖn! TiÒn ®å rÊt s¸ng l¹n! T¹i h¹ v« cïng béi phôc!")
	else
		UTask_gb = nt_getTask(8)
		if ((UTask_gb > 5*256) and (UTask_gb < 10*256)) then
			Talk(1,"","§¹i hiÖp hiÖn ®ang thùc hiÖn nhiÖm vô nhËp m«n ë D­¬ng Ch©u, kh«ng nªn kĞo dµi qu¸ l©u nh­ vËy!")
		elseif (UTask_gb >= 70*256) and (player_Faction ~= "gaibang") then
			Say("§· l©u kh«ng gÆp! Mäi ng­êi ®Òu rÊt mong nhí! §¹i hiÖp d¹o nµy cã kháe kh«ng?",3,"T×m hiÓu së tr­êng luyÖn c«ng/map_help","T×m hiÓu vâ nghÖ bæn m«n/skill_help","KÕt thóc ®èi tho¹i/no")
		else
			Talk(1,"enroll_select","Bæn bang ®· cã danh x­ng Thiªn h¹ ®Ö nhÊt bang, danh chÊn giang hå.Nh©n tµi ngäa Hæ tµng Long, thêi nµo còng cã ")
		end
	end
end

function defection_get()
	Talk(1,"","NÕu ®· cã ı hèi c¶i, h·y ®Õn gÆp Hoµng Nhan Hïng LiÖt ®Ó xin ly gi¸o, sau ®ã ®Õn diÖn kiÕn Bang chñ Hµ Nh©n Ng· ®Ó xin nhËp m«n")
	nt_setTask(8,5*256+10)
	Msg2Player("NÕu muèn gia nhËp C¸i bang, h·y ®Õn gÆp Hoµn Nhan Hïng LiÖt ®Ó xin ly gi¸o, sau ®ã ®Õn diÖn kiÕn Bang chñ Hµ Nh©n Ng· ®Ó xin nhËp m«n")
	AddNote("NÕu muèn gia nhËp C¸i bang, h·y ®Õn gÆp Hoµn Nhan Hïng LiÖt ®Ó xin ly gi¸o, sau ®ã ®Õn diÖn kiÕn Bang chñ Hµ Nh©n Ng· ®Ó xin nhËp m«n")
end

function defection_no()
	Talk(1,"","X­a nay Thµnh v­¬ng b¹i khÊu, cßn nãi g× ®Õn chuyÖn ®óng sai?! C¸i bang cã lo¹i ®Ö tö cè chÊp nh­ ng­¬i, sím muén g× còng th©n b¹i danh liÖt!")
end

function enroll_select()
	UTask_gb = nt_getTask(8)
	UTask_tr = nt_getTask(4)
	if ((UTask_tr > 5*256) and (UTask_tr < 10*256)) then
		Talk(1,"","C¸i Bang vµ Thiªn NhÉn x­a nay vèn kh«ng ®éi trêi chung! NÕu ng­¬i ®· quyÕt t©m gia nhËp Thiªn NhÉn, ta kh«ng cßn g× ®Ó nãi!")
	elseif (GetSeries() == 3) and (GetCamp() == 0) and (UTask_tr < 5*256) and (UTask_gb < 5*256) then
		if (GetLevel() >= 10) then
			Say("Cã høng thó gia nhËp C¸i bang kh«ng? Mäi ng­êi cã c¬m cïng ¨n, cã r­îu cïng uèng!", 3, "Gia NhËp C¸i bang/go", "§Ó ta suy nghÜ kü l¹i xem/no","T×m hiÓu tinh hoa vâ nghÖ c¸c m«n ph¸i/Uworld1000_knowmagic")
		else
			Talk(1,"","C¨n b¶n cña ng­¬i cßn kĞm l¾m! H·y ®i luyÖn tËp thªm, bao giê ®Õn cÊp 10 l¹i ®Õn t×m ta!")
		end
	end
end

function go()
	Uworld1000 = nt_getTask(1000)
	if ( Uworld1000 == 240 ) or ( Uworld1000 == 250 ) then
		nt_setTask(1000,260)
	end
	SetRevPos(115,53)
	nt_setTask(8, 10*256)
	SetFaction("gaibang")
	SetCamp(1)
	SetCurCamp(1)
	nt_setTask(137,68)
	SetLastFactionNumber(6)
	SetRank(78)

	-- Hç trî Kü N¨ng 10-60 khi gia nhËp m«n ph¸i
	if (HoTroKyNangGiaNhapPhai == 1) then
		add_gb(70)
	else
		add_gb(10)
	end
	
	Msg2Player("Hoan nghªnh b¹n gia nhËp C¸i bang, trë thµnh §Ö tö kh«ng tói! Häc ®­îc KiÕn Nh©n ThÇn Thñ, Diªn M«n Th¸c B¸t")
	AddNote("Gia nhËp C¸i bang, trë thµnh §Ö tö C¸i Bang.")
	Msg2Faction("gaibang",GetName().." tõ h«m nay gia nhËp C¸i bang! Xin b¸i kiÕn c¸c vŞ s­ huynh s­ tû! Mong c¸c vŞ quan t©m chØ gi¸o!",GetName())
	NewWorld(115, 1501, 3672)
end

function return_yes()
	NewWorld(115, 1501, 3672)
end

function gb_check_yes()
	if (GetSeries() ~= 2) then
		Talk(1,"","Ngò hµnh cña ng­¬i kh¸c hÖ Háa víi bän ta!  §Õn ®©y lµm g×? H·y lªn diÔn ®µn häc hái ®i!")
	elseif (HaveMagic(91) >= 0) then
		nt_setTask(1,60*256)
		Talk(1,"","Ta ®· phôc håi l¹i tr¹ng th¸i nhiÖm vô cña ng­¬i! B©y giê ng­¬i cã thÓ ®i nhËn nhiÖm vô xuÊt s­ ")
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

function gb_check_no()
	Say("Cã lêi g× muèn nãi víi bang chñ kh«ng? Hay muèn ta ®­a vÒ Tæng ®µn?",2,"Xin ®­a t¹i h¹ vÒ Tæng ®µn!/return_yes","Xin chuyÓn dïm lêi hái th¨m s­ phô /no")
end

function Uworld1000_jiaoyugaibang()
	nt_setTask(1000,260)
	Msg2Player("Ng­¬i ®· lµ ®Ö tö cña bæn bang, kh«ng cÇn ®i n÷a, cã thÓ trùc tiÕp gÆp Long Ngò nãi chuyÖn.")
end

function no()
end