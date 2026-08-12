INTERVAL_TIME = 120

function GetNextTime()
    local nhour = tonumber(date("%H"))
	if mod(nhour, 2) == 0 then
		nhour = nhour + 2
    	return nhour, 0
	else
		nhour = nhour + 1
	end
	if (nhour == 24) then
		nhour = 0
	end
	return nhour, 0
end

function TaskShedule()
	TaskName("Phong hoa lien thanh")	
	local h, m = GetNextTime()
	TaskInterval(INTERVAL_TIME)
	TaskTime(h, m)
	OutputMsg("==========================================================================================")
	OutputMsg(format("                     Khoi dong PHONG HOA LIEN THANH, bat dau luc %02d:%02d", h, m))
	OutputMsg("==========================================================================================")
	TaskCountLimit(0)
end

function TaskContent()
	GlobalExecute("dwf \\script\\gmscript.lua NewCityDefence_OpenMain(1)")
	OutputMsg("==========================================================================================")
	OutputMsg("                        PHONG HOA LIEN THANH - bat dau luc 12:00")
	OutputMsg("==========================================================================================")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end