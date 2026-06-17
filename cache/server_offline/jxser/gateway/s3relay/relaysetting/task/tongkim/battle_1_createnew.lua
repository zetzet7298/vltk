Include("\\RelaySetting\\battle\\script\\rf_header.lua")

function TaskShedule()
	TaskName("TËng kim chi’n dﬁch tÊng chÿ huy")
	TaskSetMode(1)
	TaskSetStartDay(1,3)
	TaskInterval(7)
	TaskTime(2,0)
	TaskCountLimit(0)
end

function TaskContent()
	OutputMsg("Khoi dong TONG KIM - Chien Dich Chi Huy")
	battle_StartNewIssue(1,1)
	battle_StartNewIssue(1,2)
	battle_StartNewIssue(1,3)
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end