INTERVAL_TIME = 60

function GetNextTime()
    local hour = tonumber(date("%H"))
	local minute = tonumber(date("%M"))
	if (minute >= 45) then
    	if (hour == 23) then
    		hour = 0
    	else
    		hour = hour + 1
		end
	end
    return hour, 45
end

function TaskShedule()
	TaskName("PhongLangDo")
	TaskInterval(INTERVAL_TIME)
	local h, m = GetNextTime()
	TaskTime(h, m)
	OutputMsg("==========================================================================================")
	OutputMsg(format("                                   Phong Lang Do %d:%d ", h, m))
	OutputMsg("==========================================================================================")
	TaskCountLimit(0)
end

function TaskContent()
	GlobalExecute("dwf \\script\\missions\\fengling_ferry\\fldmap_boat1.lua fenglingdu_main()")
	OutputMsg("Khoi Dong Phong Lang Do")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end