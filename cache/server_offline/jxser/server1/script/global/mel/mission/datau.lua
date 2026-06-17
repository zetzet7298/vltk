IncludeLib("SETTING")
IncludeLib("ITEM")
IncludeLib("FILESYS")
IncludeLib("RELAYLADDER")
Include("\\script\\task\\newtask\\tasklink\\tasklink_head.lua")
Include("\\script\\task\\newtask\\tasklink\\tasklink_award.lua")
Include("\\script\\event\\storm\\function.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\log.lua")
Include("\\script\\activitysys\\g_activity.lua")
Include("\\script\\activitysys\\playerfunlib.lua")
Include("\\script\\task\\system\\task_string.lua")
Include("\\script\\global\\pgaming\\configserver\\configall.lua")

----------------------------------------------------------------------------------------------------
--										  Hoµn Thµnh D· TÈu								  	  	  --
----------------------------------------------------------------------------------------------------
DTL_TASK = 8000
DTL_LIMIT= 40

function lamnhiemvudatau()
	if HoanThanhDaTau == 1 then
		lamnhiemvudatau1()
	else
		Say("Tİnh n¨ng nµy ch­a ®­îc më!")
	end
end

function lamnhiemvudatau1()
	dofile("script/global/mel/mission/datau.lua")
	local ndate = tonumber(GetLocalDate("%m%d"))
	local nUseTimes = GetBitTask(DTL_TASK, 0, 8) 
	local nLastUseDate = GetBitTask(DTL_TASK, 8, 24) 
	if( nLastUseDate ~= ndate) then
		nLastUseDate =  ndate
		nUseTimes = 0
	end
	if (nUseTimes >= DTL_LIMIT) then
		Say(format("Mét ngµy chØ cã thÓ dïng tİnh n¨ng nµy 40 lÇn th«i", DTL_LIMIT), 0)
		return 1
	end
	if GetCash() < (SoTienHoanThanhDaTau * 10000) then
		Say("Kh«ng ®ñ <color=green>"..SoTienHoanThanhDaTau.."<color> v¹n l­îng th× ta kh«ng thÓ gióp.", 0)
		return 1
	end
	if tl_gettaskcourse() == 0  then
		Say("NhËn nhiÖm vô d· tÈu tr­íc ®· chø",0)
		return 1
	elseif tl_gettaskcourse() == 1 then
		Task_AwardRecord()
		Msg2Player("<color=green>Chóc mõng <color=yellow>"..GetName().."<color> ®· sö dông <color=yellow>Ng©n L­îng ®Ó hoµn thµnh nhiÖm vô D· TÈu<color>!<color>")
		Say("§· hoµn thµnh xong nhiÖm vô, ®èi tho¹i víi D· TÈu ®Ó nhËn th­ëng")
		nUseTimes = nUseTimes + 1
		SetBitTask(DTL_TASK, 0, 8,  nUseTimes)
		SetBitTask(DTL_TASK, 8, 24, nLastUseDate)
		Pay(SoTienHoanThanhDaTau * 10000)
		return 0
	else 
		Say("NhËn nhiÖm vô d· tÈu tr­íc ®·",0)
		return 1
	end
end