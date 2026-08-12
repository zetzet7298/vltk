Include("\\script\\maps\\newworldscript_default.lua")
IncludeLib("TIMER")

function CheckMap(nMapId)
	SetProtectTime(18*10)
	AddSkillState(963, 1, 0, 18*10)
	SetFightState(1)
	DisabledUseTownP(0)
	ForbidEnmity(0)
	SetPKFlag(0)
	ForbidChangePK(0)
	SetPunish(1)
end

function OnNewWorld(szParam)
	local nMapId = SubWorldIdx2ID(SubWorld)
	CallPlayerFunction(PlayerIndex, CheckMap,nMapId)
	OnNewWorldDefault(szParam)
end

function OnLeaveWorld(szParam)
	SetProtectTime(18*10)
	AddSkillState(963, 1, 0, 18*10)
	DisabledUseTownP(0)
	ForbidEnmity(0)
	SetPKFlag(0)
	ForbidChangePK(0)
	SetPunish(1)
	OnLeaveWorldDefault(szParam)
end