Include("\\script\\gb_modulefuncs.lua")

tab_GameSetting = {
	/*
	"Vuot Ai",
	"Phong Lang Do",
	"Tong Kim",
	"Boss Hoang Kim",
	"Boss Tieu Hoang Kim",
	"That Thanh Dai Chien",
	"Doan Hoa Dang",
	"Lien Dau",
	*/
}

function TaskShedule()
	TaskName("Thi’t k’ h÷ thËng trﬂ ch¨i")
	TaskInterval(1000000)
	TaskCountLimit(0)
	OutputMsg("==========================================================================================")
	OutputMsg("                BAT DAU KHOI DONG CAC TINH NANG CUA GAME VO LAM TRUYEN KY")
	OutputMsg("==========================================================================================")
end

function TaskContent()
	for i = 1, getn(tab_GameSetting) do
		gb_AutoStartModule(tab_GameSetting[i])
	end
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end