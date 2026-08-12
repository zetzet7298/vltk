IncludeLib("ITEM")
IncludeLib("NPCINFO")
IncludeLib("RELAYLADDER")
IncludeLib("FILESYS")
IncludeLib("TASKSYS")
IncludeLib("SETTING")
IncludeLib("TIMER") 
IncludeLib("BATTLE")
IncludeLib("TITLE")
Include("\\script\\task\\task_addplayerexp.lua")


pLibs = {}

function pLibs:FormatNumber(nExp)
    if not nExp or nExp == 0 then
        return "<color=white>0<color>"
    end
    
    local strNum = tostring(nExp)
    local result = ""
    local len = strlen(strNum)
    
    for i = 1, len do
        result = result .. strsub(strNum, i, i)
        local remaining = len - i
        if remaining > 0 and mod(remaining, 3) == 0 then
            result = result .. "."
        end
    end
    
    result = "<color=white>"..result.."<color>"
    return result
end

function pLibs:AddPlayerExp(myExpValue)
	
	if not myExpValue or myExpValue <= 0 then 
		return 
	end
	
	if not GetLevel() then 
		return 
	end
	
	local nTransCont = ST_GetTransLifeCount()
	local nTotalExpAdded = myExpValue
	
	for i = 1, 180 do
		if myExpValue <= 0 then
			break
		end
		
		local nCurrentExp = GetExp()
		local nNextLevel = GetLevel() + 1
		local nExpForNextLevel = tl_getUpLevelExp(nNextLevel, nTransCont)
		local nExpNeeded = nExpForNextLevel - nCurrentExp
		
		if nExpNeeded <= 0 or nExpForNextLevel <= 0 then
			break
		end
		
		if myExpValue >= nExpNeeded then
			AddOwnExp(nExpNeeded)
			myExpValue = myExpValue - nExpNeeded
		else
			AddOwnExp(myExpValue)
			myExpValue = 0
			break
		end
	end
	
	if nTotalExpAdded > 0 then
		Msg2Player("<color=yellow>B¹n nhËn ®­îc " .. self:FormatNumber(nTotalExpAdded) .. " Exp céng dån<color>")
	end
end

function pLibs:SetTask(nTaskID, nTaskValue)
	if not nTaskID or not nTaskValue then
		return
	end
	SetTask(nTaskID, nTaskValue)
	SyncTaskValue(nTaskID)
end