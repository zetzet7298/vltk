Include("\\script\\xephang\\worldrank_hook.lua")

function TaskShedule()
    TaskName("UpDateRankHook")
    TaskCountLimit(0)
end

function TaskContent()
    OutputMsg("==========================================================================================")
    OutputMsg("====================== Xep Hang => Bat dau cap nhat "..date("%H:%M").." -> Start =======================")
    RankHook:UpdateRank()
    OutputMsg("==================== Xep Hang => Ket thuc cap nhat "..date("%H:%M").." -> Complete =====================")
    OutputMsg("==========================================================================================")
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end