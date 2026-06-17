---------------Youtube PGaming---------------
IncludeLib("SETTING");
-----------------------------------------------------------
nTSK_USE_TIMES_LIMIT	 = 5757
nTSK_USE_COUNT_LIMIT	 = 5756
nUSE_COUNT_LIMIT	 = 5
-------------------------------
function main()

	local nDate = tonumber(GetLocalDate("%d"))
	if (GetTask(nTSK_USE_TIMES_LIMIT) ~= nDate) then
		SetTask(nTSK_USE_TIMES_LIMIT, nDate)
		SetTask(nTSK_USE_COUNT_LIMIT, 0)
	end
	
	if (GetTask(nTSK_USE_COUNT_LIMIT) >= nUSE_COUNT_LIMIT) then
		Talk(1,"","Mçi Ngµy ChØ ®­îc sö dông 5 Tiªn Th¶o Lé")
		return 1
	end
		AddSkillState(440, 1, 1, 64800);
		SetTask(nTSK_USE_COUNT_LIMIT, GetTask(nTSK_USE_COUNT_LIMIT) + 1)
end


function GetDesc(nItemIdx)
    local nDate = tonumber(GetLocalDate("%d"))
    local nUsed = 0
    
    -- KiÓm tra nÕu Task ngµy khíp víi h«m nay th× míi lÊy sè lÇn dïng
    if (GetTask(nTSK_USE_TIMES_LIMIT) == nDate) then
        nUsed = GetTask(nTSK_USE_COUNT_LIMIT)
    else
        nUsed = 0 -- Tr­êng hîp sang ngµy míi nh­ng ch­a dïng c¸i nµo
    end

    local szDesc = "<color=green>Tiªn Th¶o Lé<color>\n"
    szDesc = szDesc.."<color=blue>T¨ng kinh nghiÖm khi ®¸nh qu¸i.<color>\n"
    szDesc = szDesc.."<color=yellow>H«m nay ®· dïng: <color=white>"..nUsed.." / "..nUSE_COUNT_LIMIT.."<color>"
    
    return szDesc
end