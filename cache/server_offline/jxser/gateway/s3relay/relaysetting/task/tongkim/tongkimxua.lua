INTERVAL_TIME = 120

function GetNextTime()
	local nhour = tonumber(date("%H"))
	if nhour == 24 then
		nhour = 0
	end
	if mod(nhour, 2) == 0 then
		nhour = nhour + 1
	else
		nhour = nhour + 2
	end
	return nhour, 0
end

function TaskShedule()
	TaskName("T≠¨ng D≠¨ng chi’n dﬁch (tËng kim)")
	local h, m = GetNextTime()
	TaskInterval(INTERVAL_TIME)
	TaskTime(h, m)
	OutputMsg("==========================================================================================")
	OutputMsg(format("                      Chien Truong TONG KIM bat dau vao gio le %02d:%02d", h, m))
	OutputMsg("==========================================================================================")
	TaskCountLimit(0)
end

function TaskContent()
	GlobalExecute("dwf \\script\\global\\mel\\mission\\tongkim.lua OpenTongKim()")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end