function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end

function TaskShedule()
    TaskName("GlobalExecuteCHAT")
    TaskInterval(15)
    TaskCountLimit(0)
    OutputMsg("==========================================================================================")
    OutputMsg("                               Khoi dong TU DONG TRO CHUYEN")
    OutputMsg("==========================================================================================")
end

function TaskContent()
    GlobalExecute("dwf \\script\\global\\autochat.lua globalexcute()")
end