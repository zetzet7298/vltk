

function TaskShedule()
	--…Ë÷√∑Ω∞∏√˚≥∆
	TaskName(" hoπt ÆÈng x’p hπng <Th∏ch th¯c thÍi gian> mÁi ngµy")
	
	TaskTime(0, 0);
	
	--…Ë÷√º‰∏Ù ±º‰£¨µ•ŒªŒ™∑÷÷”
	TaskInterval(1440) --30∑÷÷”“ª¥Œ
	
	--…Ë÷√¥•∑¢¥Œ ˝£¨0±Ì æŒﬁœﬁ¥Œ ˝
	TaskCountLimit(0)
	OutputMsg("                      BANG XEP HANG <THACH THUC THOI GIAN> MOI NGAY")
	OutputMsg("==========================================================================================")
end

function TaskContent()
	name , value = Ladder_GetLadderInfo(10235, 1);
	value = value * (-1);
	if (name ~= "") then
		local szTime	= format("%s phÛt %s gi©y", floor(value/60), floor(mod(value, 60)));
		local szMsg 	= format("ChÛc mıng <%s> Æ∑ hoµn thµnh <Th∏ch th¯c thÍi gian> thÍi gian v≠Ót ∂i nhanh nh t lµ <%s>", name, szTime);
		GlobalExecute(format("dw AddGlobalNews([[%s]], 10)", szMsg))
		GlobalExecute(format("dw Msg2SubWorld([[%s]])", szMsg))
	end
	Ladder_ClearLadder(10235);
	OutputMsg("=====> Bang Xep Hang Vuot Ai Moi Ngay 00:00 Bat Dau==================");
end

function GameSvrConnected(dwGameSvrIP)
end
function GameSvrReady(dwGameSvrIP)
end


