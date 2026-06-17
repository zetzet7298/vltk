Include("\\script\\global\\pgaming\\configserver\\configall.lua")
Include("\\script\\global\\systemconfig.lua")
Include("\\script\\trip\\define.lua")
Include("\\script\\maps\\checkmap.lua")

aryMap = {
	11,
	1,
	37,
	176,
	162,
	78,
	80,
	174,
	121,
	153,
	101,
	99,
	100,
	20,
	53,
	54,
	175,
	44, 326, 327, 328, 329, 330, 331, 344, 345, 346, 347, 348, 349, 350, 351, 352, 353, 354, 355, 357, 358, 359, 360, 361, 362, 363, 364, 365, 366, 367, 368, 369, 370, 371, 372, 373, 374,375,376,377,378,379,380,381,382,383,384,385,386,
	323,324,325,
	221,222,223,
	208,
	605,606,607,
	608,609,610,611,612,613,
	520,521,522,523,524,525,526,
	1010, 1025,
}

OtherMap = {m_Maps = {}}

function OtherMap:Initialize()
	for i = 1, getn(aryMap) do
		self.m_Maps[aryMap[i]] = 1
	end
end

function OtherMap:AddMap(mapid)
	self.m_Maps[mapid] = 1
end

function OtherMap:DelMap(mapid)
	self.m_Maps[mapid] = nil
end

function OtherMap:Check(mapid)
	if (self.m_Maps[mapid] ~= nil) then
		return 1
	else
		return 0
	end
end

OtherMap:Initialize()

function IsShopMap(nMapID)
	if (IsCityMap(nMapID) == 1 or
		IsFreshmanMap(nMapID) == 1 or
		IsTongMap(nMapID) == 1 or
		OtherMap:Check(nMapID) == 1) then
		return 1
	else
		return 0
	end
end

function main()
	if OpenShopKTC == 1 and GetTask(5733) < DiemNapTheSuDungKTC then
		Msg2Player("<color=yellow>§iÓm n¹p thÎ cña b¹n kh«ng ®ñ <color=red>"..DiemNapTheSuDungKTC.."<color> kh«ng thÓ sö dông tÝnh n¨ng nµy<color>")
		return 0
	end
	if OpenShopKTC ~= 1 then
		Msg2Player("<color=yellow>HiÖn t¹i Kú Tr©n C¸c ch­a më!<color>")
		return 0
	end
	if SYSCFG_SHOP_OPEN ~= 1 then
		Msg2Player("<color=yellow>HiÖn t¹i Kú Tr©n C¸c vÉn ch­a më! <color>")
		return 0
	end
	if GetTripMode() == TRIP_MODE_SERVER then
		Msg2Player("<color=yellow>HiÖn t¹i Kú Tr©n C¸c ch­a më!<color>")
		return 0
	end
	local nMapID, _, _ = GetWorldPos()
	if (GetFightState() >= 1 or IsShopMap(nMapID) == 0) then
		Msg2Player("<color=yellow>Kú Tr©n C¸c chØ cã thÓ më t¹i c¸c n¬i thµnh thÞ, th«n trang vµ mét sè khu vùc phi chiÕn ®Êu kh¸c!<color>")
		return 0
	else
		return 1
	end
	Msg2Player("Phiªn b¶n C«ng Thµnh ChiÕn thÓ sö dông Kú Tr©n C¸c!")
end