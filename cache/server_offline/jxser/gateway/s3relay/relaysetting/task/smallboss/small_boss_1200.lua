
function TaskShedule()
	TaskName("Boss TiÓu Hoµng Kim 12:00")
	TaskTime(12,00)
	TaskInterval(24 * 60) 
	TaskCountLimit(0)
	OutputMsg("==========================================================================================")
	OutputMsg("                           Khoi dong BOSS TIEU HOANG KIM 12:00")
	OutputMsg("==========================================================================================")
end

function TaskContent()
	local szMsg = "Boss TiÓu Hoµng Kim xuÊt hiÖn trªn b¶n ®å thÕ giíi, quÊy nhiÔu d©n lµnh, c¸c nh©n sü h·y ra søc truy lïng bän chóng."
	GlobalExecute(format("dw Msg2SubWorld([[%s]])", szMsg))
	OutputMsg("==========================================================================================")
	OutputMsg("                           Khoi dong BOSS TIEU HOANG KIM 12:00")
	OutputMsg("==========================================================================================")
	GlobalExecute("dwf \\script\\global\\pgaming\\missions\\bosshoangkim\\bosstieu\\smallboss_main.lua  smallboss_call2world()")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end