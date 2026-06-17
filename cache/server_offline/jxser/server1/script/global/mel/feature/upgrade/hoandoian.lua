IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--									   Ho¸n §æi Ên Hoµng Kim								  	  --
----------------------------------------------------------------------------------------------------
AnHoangKim = {
    "ThiÕu L©m Hoµng Kim Ên",
    "Thiªn V­¬ng Hoµng Kim Ên",
    "§­êng M«n Hoµng Kim Ên",
	"Ngò §éc Hoµng Kim Ên",
	"Nga My Hoµng Kim Ên",
	"Thóy Yªn Hoµng Kim Ên",
	"C¸i Bang Hoµng Kim Ên",
	"Thiªn NhÉn Hoµng Kim Ên",
	"Vâ §ang Hoµng Kim Ên",
    "C«n L«n Hoµng Kim Ên"
}

function hoandoian()
	dofile("script/global/mel/feature/upgrade/hoandoian.lua")
	local tbOpt = {
		{"§Æt Ên Hoµng Kim vµo",hoangkiman},
		{"KÕt Thóc §èi Tho¹i",No},
	}
	CreateNewSayEx("§Æt vµo Ên Hoµng Kim mµ ng­¬i ®ang së h÷u<enter><enter><color=Green>ChØ bá Ên Hoµng Kim!<color> <color=Red>Kh«ng bá TiÒn §ång!<color><enter><enter><color=Gold>CÇn 500 TiÒn §ång vµ 5000 v¹n ®Êy nhÐ!<color>", tbOpt)
end

function hoangkiman()
	GiveItemUI( "§æi qua Ên Hoµng Kim kh¸c", "ChØ cÇn bá Ên Hoµng Kim<enter>Kh«ng cÇn bá TiÒn §ång", "hoangkiman1", "onCancel",1 );
end

function hoangkiman1( nCount )
	countvk = 0
	if nCount ~= 1 then						
		Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
		return 0
	else
		for i = 1, nCount do
			local nItemIndex = GetGiveItemUnit(i)
			szName = GetItemName(nItemIndex)
			for i=1, getn(%AnHoangKim) do
				if szName == %AnHoangKim[i] then
					countvk = countvk + 1
				end
			end
		end
		if countvk ~= 1 then
				Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
				return 0
		end
		if GetCash() < 50000000 then
        	Say("Kh«ng ®ñ 5000 v¹n l­îng.<enter>TiÒn ®©u sao kh«ng ®ñ!", 0) return
    	end
		if (CalcEquiproomItemCount(4, 417, 1, 1)>=500) then
			for i = 1, nCount do		
				nItemIndex = GetGiveItemUnit( i )
				k = RemoveItemByIndex( nItemIndex )
				if ( k ~= 1 ) then
					Say("ChuyÖn g× vËy, thÕ nµy lµ sao?",0)
					return 0
				end		
			end
			ConsumeEquiproomItem(500, 4, 417, 1, 1)
			Pay(50000000)
			luachonan()
			Msg2Player("Chóc Mõng "..GetName().." §æi Ên Hoµng Kim thµnh c«ng")
		else
			Say("Kh¸ch quan ®ang trªu chäc tiÓu nh©n hay thËt sù ng­êi cã ®ñ 500 TiÒn §ång kh«ng?", 0);
		end	
	end
end

function luachonan()
    local tbOpt = {
		{"ThiÕu L©m Hoµng Kim Ên", trao_an, {1088}},
    	{"Thiªn V­¬ng Hoµng Kim Ên", trao_an, {1089}},
    	{"§­êng M«n Hoµng Kim Ên", trao_an, {1090}},
		{"Ngò §éc Hoµng Kim Ên", trao_an, {1091}},
		{"Nga My Hoµng Kim Ên", trao_an, {1092}},
		{"Thóy Yªn Hoµng Kim Ên", trao_an, {1093}},
		{"C¸i Bang Hoµng Kim Ên", trao_an, {1094}},
		{"Thiªn NhÉn Hoµng Kim Ên", trao_an, {1095}},
		{"Vâ §ang Hoµng Kim Ên", trao_an, {1096}},
    	{"C«n L«n Hoµng Kim Ên", trao_an, {1097}},
    }
    CreateNewSayEx("<color=green>Chän lo¹i Ên mµ ng­¬i muèn së h÷u:<color>", tbOpt)
end

function trao_an(nID)
    local nNewIdx = AddGoldItem(0, nID)
    if (nNewIdx > 0) then
        SyncItem(nNewIdx)
    end
end