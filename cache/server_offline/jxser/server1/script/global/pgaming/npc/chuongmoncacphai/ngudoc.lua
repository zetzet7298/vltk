Include("\\script\\global\\skills_table.lua")
Include([[\script\event\teachersday06_v\prize_qingyika.lua]])
Include("\\script\\task\\lv120skill\\head.lua")
Include ("\\script\\event\\springfestival08\\allbrother\\findnpctask.lua")
Include("\\script\\misc\\daiyitoushi\\toushi_function.lua")
Include("\\script\\vng_event\\LunarYear2011\\npc\\mastergift.lua")
Include("\\script\\dailogsys\\g_dialog.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\nangcapantuyetdinh.lua")
Include("\\script\\global\\mel\\feature\\upgrade\\tuyetdinhhoangkimmonphai.lua")

function main()
	if tbVNG_LY2011:isActive() == 1 then
		local tbSay =
			{
				"TÆng b¸nh ngµy xu©n/#tbMasterGift:getGift('wudu')",
				"Muèn thØnh gi¸o viÖc kh¸c/oldMain"
			}
		Say("N¨m míi an khang thÞnh v­îng", getn(tbSay), tbSay)
		return
	end
	oldMain()
end

function oldMain()
	if allbrother_0801_CheckIsDialog(4) == 1 then
		allbrother_0801_FindNpcTaskDialog(4)
		return 0
	end
	if (vt06_isactive() ~= 0) then
		Say("T×m ta cã viÖc g×?", 2, "Muèn thØnh gi¸o ®¹i s­!/oldentence", "Mõng lÔ ¢n S­, t¹i h¹ ®· t×m ®ñ “ThÎ ¢n S­” vµ “ThÎ Cao §å”./vt06_prizeenter")
		return
	end
	if (GetLevel() >= 120 and GetTask(LV120_SKILL_STATE) ~= 19 and GetLastFactionNumber() == 3) then
		Describe("T×m ta cã viÖc g×?", 2,
			"Häc kü n¨ng cÊp 120./lvl120skill_learn",
			"Muèn thØnh gi¸o viÖc kh¸c/oldentence"
			)
		return
	end
	oldentence()
end

function oldentence()
	local UTask_wu = GetTask(10)
	local nFactID = GetLastFactionNumber()
	if (UTask_wu == 70*256) and (GetFaction() == "wudu") then
		SetFaction("")
		Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
		return
	elseif (UTask_wu == 70*256) and (GetTask(2) >= 5*256) and (GetTask(2) < 10*256) then
		SetTask(2,0)
		Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
		return
	elseif (UTask_wu == 80*256) and (GetCamp() == 4) then
		SetTask(10,70*256)
		Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
		return
	elseif (GetTask(2) == 70*256) and (GetTask(10) == 70*256) then
		SetTask(2,75*256)
		Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
		return
	elseif (UTask_wu == 80*256 and nFactID == 3 and GetCamp() == 2 and GetFaction() == "Míi nhËp giang hå ") then
		 local _, nTongID = GetTong()
		 if (nTongID == 0) then
			SetFaction("wudu")
			Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
			return
		end
	elseif (UTask_wu == 70*256 and nFactID == 3 and GetCamp() ~= 4 and GetFaction() == "Míi nhËp giang hå ") then
		 local _, nTongID = GetTong()
		 if (nTongID == 0) then
			SetFaction("")
			SetCurCamp(GetCamp())
			Say("HÖ thèng ph¸t hiÖn sai sãt, ®· kÞp thêi håi phuc!",0)
			return
		end
	end
	local tbDes = {
		"N©ng cÊp Trang BÞ TuyÖt §Ønh Ngò §éc/#tbHKUpgrade:NguDoc()",
		"N©ng cÊp Ên TuyÖt §Ønh Ngò §éc/#tbAnUpgrade:NguDoc()",
		"§¸i nghÖ ®Çu s­/#daiyitoushi_main(3)",
		"Muèn thØnh gi¸o viÖc kh¸c/common_talk"}
	Say("GÇn ®©y ta cã rÊt nhiÒu viÖc gi¶i quyÕt, ng­¬i ®Õn ®©y cã viÖc g×.", getn(tbDes), tbDes)
end

function common_talk()
	local UTask_wu = GetTask(10)
	if (GetTask(39) == 10) and (GetBit(GetTask(40),10) == 0) then
		Talk(1,"","§éc C« KiÕm muèn liªn Tèng chèng Kim? Cã g× hay ho chø? Bæn gi¸o vµ Kim quèc vèn lµ chç liªn minh mËt thiÕt. C¸c ng­¬isøc yÕu thÕ c« nh­ vËy liÖu cã ®­¬ng cù næi kh«ng")
		Uworld40 = SetBit(GetTask(40),10,1)
		SetTask(40,Uworld40)
	elseif (GetSeries() == 1) and (GetFaction() == "wudu") then
		if (UTask_wu == 60*256+20) and (HaveItem(222) == 1) then
			Talk(2,"L60_prise","Thuéc h¹ ®· ®o¹t l¹i b¶o vËt cña gi¸o ph¸i, xin tr¹i chñ xem thö!","Giái! Hay l¾m! Cùc kú giái Qu¶ nhiªn ng­¬i ®· kh«ng phô sù kú väng cña ta. ThËt kh«ng hæ danh cña 'ngò ®éc gi¸o ph¸i',VËy lµ ng­¬i cã thÓ thuËn lîi xuÊt s­ råi! Ngµy sau nÕu cã vang danh vâ l©m, th× ®õng quªn nh¾c ®Õn tªn cña bæn gi¸o nhÐ ")
		elseif (UTask_wu == 60*256) and (GetLevel() >= 50) then
			Say("Bæn gi¸o vµ Nh¹n §·ng ph¸i vèn kh«ng ®éi trêi chung. 10 n¨m tr­íc bæn gi¸o vµ Nh¹n §·ng ph¸i ®· tiÕn hµnh mét cuéc ¸c chiÕn. Bæn gi¸o ®· bÞ ¸m to¸n. TÝch §éc Chu cña bæn gi¸o ®· bÞ Nh¹n §·ng ph¸i c­íp ®i. Bän chóng ®· ®em vÒ ®éng D­¬ng Gi¸c ë Nh¹n §·ng s¬n. ChuyÖn nµy ®Õn nµy vÉn cßn lµ mèi tr¨n trë cña ta. Ngµy nµo ch­a lÊy ®­îc nã vÒ th× ta ch­a thÓ an lßng. NÕu nh­ ng­¬i muèn xuÊt s­ th× l¹i ®i ®o¹t l¹i TÝch §éc Chu vÒ ®©y cho ta",2,"Thuéc h¹ sÏ ®em hÕt t©m søc cña m×nh ®Ó mang ®­îc b¸u vËt cña bæn m«n vÒ /L60_get_yes","§Ö tö tµi mßn søc kÐm. ChØ e kh«ng hoµn thµnh ®­îc nhiÖm vô nµy! /no")
		elseif (UTask_wu == 80*256) then
			Say("Ng­¬i ®ang muèn xuÊt s­ ph¶i kh«ng? §óng lóc! H·y cho giíi vâ l©m ë Trung Nguyªn thÊy ®­îc sù lîi h¹i cña ®Ö tö bæn m«n ",2,"D¹! §a t¹ gi¸o chñ! /goff_yes","§Ö tö tù thÊy m×nh vâ nghÖ vÉn ch­a ®ñ, e r»ng sÏ phô lßng cña gi¸o chñ! /no")
		else
			Talk(1,"","§õng cã qu¸ coi träng c¸i bän C¸i Bang tù x­ng lµ Vâ L©m ®Ö nhÊt bang ph¸i kia, ThiÕu L©m míi chÝnh lµ thiªn h¹ ®Ö nhÊt m«n ph¸i! Hõ,cho dï lµ ®Ö nhÊt cao thñ còng khã mµ ®Þch næi víi ®éc d­îc tuyÖt phÈm cña ta")
		end
	elseif (GetSeries() == 1) and (GetCamp() == 4) and (GetLevel() >= 60) and (UTask_wu == 70*256) and ((GetTask(2) < 5*256) or (GetTask(2) == 75*256)) then
		Say("Ng­¬i muèn trë l¹i bæn gi¸o? ChuyÖn nµy nãi lín th× lµ lín, mµ nãi nhá th× lµ nhá. ChØ cÇn ng­¬i n¾m râ ®­îc nh÷ng quy luËt th× cã thÓ quyÕt ®Þnh. ",2,"Xin gi¸o chñ chiÕu cè /return_yes","Xin cho phÐp ®Ö tö suy nghÜ thªm mét chót /no")
	elseif (GetCamp() == 4) and ((UTask_wu == 70*256+10) or (UTask_wu == 70*256+20)) then
		Say("Nh÷ng ®iÒu ng­¬i ®· nãi ®ã, ®· chuÈn bÞ xong ch­a",2,"ChuÈn bÞ xong råi /return_complete","Xin h·y cho ®Ö tö thªm mét Ýt thêi gian n÷a /return_uncompleted")
	else
		Talk(1,"","Cho phÐp ®Ö tö trong bæn ph¸i tù h¹ ®éc lÉn nhau lµ néi ®Þnh cña bæn ph¸i. KÎ nµo c­êng lùc h¬n th× sÏ tån t¹i")
	end
end

function defection_complete()
	Msg2Player("Hoan nghªnh b¹n gia nhËp Ngò §éc gi¸o, ng­¬i ®· trë thµnh Ngò §éc §ång Tö råi ")
	SetRevPos(183,70)
	SetTask(10,10*256)
	SetFaction("wudu")
	SetCamp(2)
	SetCurCamp(2)
	SetRank(49)
	AddMagic(63)
	Msg2Player("Häc ®­îc §éc Sa Ch­ëng ")
	AddNote("Gia nhËp Ngò §éc gi¸o, Trë thµnh Ngò §éc ®ång tö, häc ®­îc §éc Sa Ch­ëng ")
	Msg2Faction("wudu",GetName().." tõ §­êng M«n ®Õn gia nhËp Ngò §éc gi¸o. Lùc l­îng bæn ph¸i ®· m¹nh l¹i cµng m¹nh!",GetName())
end

function goff_yes()
	Talk(1,"","§­îc! XuÊt s­ råi ®õng lµm cho danh tiÕng cña bæn ph¸i bÞ « danh trong vâ l©m nhÐ ")
	SetTask(10,70*256)
	AddNote("Rêi Ngò §éc gi¸o, tiÕp tôc chu du giang hå ")
	Msg2Player("B¹n rêi Ngò §éc gi¸o, tiÕp tôc chu du giang hå ")
	SetFaction("")
	SetCamp(4)
	SetCurCamp(4)
end

function return_yes()
	Talk(3,"","ë ®©y ®Ö tö cã 50000 l­îng ng©n l­îng. xin gi¸o chñ tiÕp nhËn","Tèt l¾m! Ng­¬i ®· cã thµnh ý nh­ vËy, ta còng khã mµ tõ chèi","Xin cho phÐp ®Ö tö ®i chuÈn bÞ vµi thø. ")
	SetTask(10,70*256+20)
	AddNote("Giao nép 50000 ng©n l­îng th× cã thÓ trë l¹i Ngò §éc Gi¸o ")
	Msg2Player("Giao nép 50000 ng©n l­îng th× cã thÓ trë l¹i Ngò §éc Gi¸o ")
end

function return_complete()
	if(GetCash() >= 50000) then
		Talk(1,"","Çy! Ng­¬i b©y giê ®· lµ ®Ö tö cña bæn gi¸o råi! Ta sÏ ®Ò b¹t ng­¬i lµ U Minh Quû V­¬ng ")
		Pay(50000)
		SetTask(10,80*256)
		SetRank(80)
		add_wu(70)
		Msg2Player("B¹n häc ®­îc tuyÖt häc trÊn ph¸i Ngò §éc kú Kinh, Vâ c«ng Thiªn C­¬ng §Þa S¸t. Chu C¸p Thanh Minh ")
		SetFaction("wudu")
		SetCamp(2)
		SetCurCamp(2)
		AddNote("§· quay trë l¹i Ngò §éc Gi¸o, tiÕp tôc tËp luyÖn vâ nghÖ ")
		Msg2Player(GetName().."§· quay trë l¹i Ngò §éc Gi¸o, ®­îc phong lµm U Minh quû V­¬ng. ")
	else
		Talk(2,"","§· muèn xuÊt s­ råi, nh­ng râ rµng lµ ng­¬i ch­a chuÈn bÞ g× c¶!","Xin lçi! §Ó ta kiÓm tra l¹i.")
	end
end

function return_uncompleted()
	Talk(1,"","Mau Lªn!")
end

function L60_get_yes()
	SetTask(10,60*256+10)
	AddNote("§o¹t l¹i Tõ §éc Chu b¶o bèi cña ch¸nh ph¸i vèn ®· bÞ Nh¹n §·ng ph¸i c­íp ®i ")
	Msg2Player("§o¹t l¹i Tõ §éc Chu b¶o bèi cña ch¸nh ph¸i vèn ®· bÞ Nh¹n §·ng ph¸i c­íp ®i ")
end

function L60_prise()
	Talk(1,"","Thuéc h¹ kh¾c cèt ghi t©m. TuyÖt kh«ng d¸m quªn")
	SetTask(10,70*256)
	DelItem(222)
	SetRank(70)
	SetFaction("")
	SetCamp(4)
	SetCurCamp(4)
	AddNote("§o¹t l¹i Tõ ®éc Chu tõ tay cña Nh¹n §·ng ph¸i, tr¶ l¹i cho Ngò §éc Gi¸o. Hoµn thµnh nhiÖm vô xuÊt s­. §­îc phong lµ U Minh Quû Sø, thuËn lîi xuÊt s­ ")
	Msg2Player("Chóc mõng B¹n! §· thµnh c«ng xuÊt s­. B¹n ®· ®­îc phong lµ U Minh Quû Sø ")
end

function no()
end