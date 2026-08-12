Include("\\script\\global\\vinh\\simcity\\head.lua")
simTK = {

}
function SimCityChienTranh:taoNV_TK(id, camp, worldInfo, walkPathNames, nt, theosau, capHP, extraConfig)
	if not walkPathNames then
		return nil
	end

	local mapID = worldInfo.worldId
	local name = "Kim"
	local rank = 1
	local realCamp = 5
	if camp == 1 then
		name = "Tèng"
		realCamp = 0
	end
	
	local hardsetName = (nt == 1 and SimCityNPCInfo:generateName()) or SimCityNPCInfo:getName(id)
	if worldInfo.isTongKim == 1 then
		realCamp = camp
		hardsetName = (nt == 1 and SimCityNPCInfo:generateName()) or nil
	end
 
	local tbNpc = {
		mode = "chiendau",
		szName = name or "",

		nNpcId = id,                            -- required, main char ID
		nMapId = mapID,                         -- required, map
		camp = realCamp,                        -- optional, camp

		walkMode = (theosau and "formation") or "preset", -- optional: random, keoxe, or formation for formation
		walkVar = (theosau and 3) or 4,         -- random walk of radius of 4*2
		walkPathNames = walkPathNames,

		noStop = 1,          -- optional: cannot pause any stop (otherwise 90% walk 10% stop)
		leaveFightWhenNoEnemy = 5, -- optional: leave fight instantly after no enemy, otherwise there's waiting period

		noRevive = 0,        -- optional: 0: keep reviving, 1: dead
 

		CHANCE_ATTACK_PLAYER = 1, -- co hoi tan cong nguoi choi neu di ngang qua
		CHANCE_ATTACK_NPC = 1, -- co hoi bat chien dau khi thay NPC khac phe
		CHANCE_JOIN_FIGHT = 1, -- co hoi tang cong NPC neu di ngang qua NPC danh nhau
		RADIUS_FIGHT_PLAYER = 15, -- scan for player around and randomly attack
		RADIUS_FIGHT_NPC = 15, -- scan for NPC around and start randomly attack,
		RADIUS_FIGHT_SCAN = 15, -- scan for fight around and join/leave fight it
 
		kind = 0,           -- quai mode
		TIME_FIGHTING_minTs = 6000,
		TIME_FIGHTING_maxTs = 6000,
		TIME_RESTING_minTs = 0,
		TIME_RESTING_maxTs = 1,

		resetPosWhenRevive = 1,

		tongkim = 1,
		tongkim_name = name,

		ngoaitrang = nt or 0,
		hardsetName = hardsetName,

		capHP = 2,
		rank = rank or 1,
		childrenSetup = theosau or nil,
		childrenCheckDistance = (theosau and 8) or nil -- force distance check for child

	}

	if extraConfig then
		for k,v in extraConfig do
			tbNpc[k] = v
		end
	end

	return SimCitizen:New(tbNpc)
end


function simTK:removeSimTK(mapid)
	--print("remove simTK in mapid "..mapid)
	SimCityChienTranh:removeAll(mapid)	
end
function simTK:add_npc_simcity_by_camp(nIdMap,nIdNpc,forCamp)
	SimCityChienTranh:init(nIdMap)
	local worldInfo = SimCityWorld:Get(nIdMap)
	local myPath = SimCityChienTranh:genWalkPath(forCamp)
	local fighter = SimCityChienTranh:taoNV_TK(nIdNpc, forCamp, worldInfo, myPath, 1)	
end
function simTK:call_npc_simcity(nIdMap,startNPCIndex, stopNPCIndex, nCount ,ngoaitrang)
	local nIdNpc = startNPCIndex
	for i = 1, nCount do 
		self:add_npc_simcity_by_camp(nIdMap,nIdNpc,1)
		self:add_npc_simcity_by_camp(nIdMap,nIdNpc,2)
		nIdNpc = nIdNpc + 1
		if nIdNpc > stopNPCIndex then
			nIdNpc = startNPCIndex
		end
	end

end
function simTK:add_npc_simcity(idMap)
		self:call_npc_simcity(idMap, 1906,1924,50,1)
end