function GameSvrConnected(dwGameSvrIP)
	SyncAllLadder(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end

function TaskShedule()
	TaskName( "Relay b¶ng xÕp h¹ng" )
	TaskInterval( 1440 )
	TaskCountLimit( 0 )
	OutputMsg("==========================================================================================")
	OutputMsg("                           Ladder startup. . . 10.001 -> 10500")
	OutputMsg("==========================================================================================")
	for i=10001, 10500 do
		LoadLadder(i)
	end
end

function TaskContent()
	OutputMsg("Khoi dong lai bang Xep Hang" )
	OutputMsg("==========================================================================================")
	OutputMsg(" ")
	OutputMsg("                              VO LAM TRUYEN KY OFFLINE by MEL  ")
	OutputMsg(" ")
	OutputMsg("==========================================================================================")
end