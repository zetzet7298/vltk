IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--										  Ho¸n §æi ThÇn M·								  		  --
----------------------------------------------------------------------------------------------------
ThanMa = {
    "Háa Viªm ThÇn M·",
    "H¾c Tinh ThÇn M·",
    "H¾c Hãa ThÇn M·",
    "Giao Phong ThÇn M·"
}

function HoanDoiThanMa()
	dofile("script/global/mel/feature/upgrade/hoandoithanma.lua")
	local tbOpt = {
		{"§Æt ThÇn M· vµo",thanma},
		{"KÕt Thóc §èi Tho¹i",No},
	}
	CreateNewSayEx("§Æt vµo ThÇn M· mµ ng­¬i ®ang së h÷u<enter><enter><color=Green>ChØ bá ThÇn M·!<color> <color=Red>Kh«ng bá TiÒn §ång!<color><enter><enter><color=Gold>CÇn 100 TiÒn §ång vµ 1000 v¹n ®Êy nhÐ!<color>", tbOpt)
end

function thanma()
	GiveItemUI( "§æi qua ThÇn M· kh¸c", "ChØ cÇn bá ThÇn M·<enter>Kh«ng cÇn bá TiÒn §ång", "thanma1", "onCancel",1 );
end

function thanma1( nCount )
	countvk = 0
	if nCount ~= 1 then						
		Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
		return 0
	else
		for i = 1, nCount do
			local nItemIndex = GetGiveItemUnit(i)
			szName = GetItemName(nItemIndex)
			for i=1, getn(%ThanMa) do
				if szName == %ThanMa[i] then
					countvk = countvk + 1
				end
			end
		end
		if countvk ~= 1 then
				Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!",0)
				return 0
		end

		if GetCash() < 50000000 then
        	Say("Kh«ng ®ñ 1000 v¹n l­îng.<enter>TiÒn Ýt mµ ®ßi hÝt ngùa th¬m µ!", 0) return
    	end

		if (CalcEquiproomItemCount(4, 417, 1, 1) >= 500) then
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
			luachonthanma()
			Msg2Player("Chóc Mõng "..GetName().." §æi ThÇn M· thµnh c«ng")
		else
			Say("Ng­¬i ®ang trªu chäc ta h¶? Cã ®ñ 500 TiÒn §ång kh«ng?", 0);
		end	
	end
end

function luachonthanma()
    local tbOpt = {
        {"Háa Viªm ThÇn M·", trao_than_ma, {1074}},
        {"H¾c Tinh ThÇn M·", trao_than_ma, {1075}},
        {"H¾c Hãa ThÇn M·", trao_than_ma, {1076}},
        {"Giao Phong ThÇn M·", trao_than_ma, {1077}},
    }
    CreateNewSayEx("<color=green>Chän lo¹i ThÇn M· mµ ng­¬i muèn së h÷u:<color>", tbOpt)
end

function trao_than_ma(nID)
    local nNewIdx = AddGoldItem(0, nID)
    if (nNewIdx > 0) then
        SyncItem(nNewIdx)
    end
end