function TaskShedule()
	TaskName("Tuy÷t ßÿnh VÚ ß’ 12:45")
	TaskTime(12,45)
	TaskInterval(1440)
	TaskCountLimit(0)
	OutputMsg("==========================================================================================")
	OutputMsg("                          Khoi dong BOSS TUYET DINH VU DE 12:45")
	OutputMsg("==========================================================================================")
end

function TaskContent()
	GlobalExecute("dwf \\script\\global\\mel\\mission\\tuyetdinhvude\\goldboss_main.lua bigboss_call2world()")
	OutputMsg("BOSS TUYET DINH VU DE xuat lien luc [12:45 PM]")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end