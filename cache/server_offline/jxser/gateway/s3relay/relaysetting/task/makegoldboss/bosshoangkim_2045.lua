function TaskShedule()
	TaskName("Boss §¹i Hoµng Kim 20:45")
	TaskTime(20,45)
	TaskInterval(1440)
	TaskCountLimit(0)
	OutputMsg("==========================================================================================")
	OutputMsg("                          Khoi dong BOSS HOANG KIM MON PHAI 20:45")
	OutputMsg("==========================================================================================")
end

function TaskContent()
	GlobalExecute("dwf \\script\\global\\pgaming\\missions\\bosshoangkim\\bossdai\\goldboss_main.lua bigboss_call2world()")
	OutputMsg("BOSS HOANG KIM MON PHAI xuat lien luc [20:45 PM]")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end