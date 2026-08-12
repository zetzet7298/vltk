Include("\\script\\global\\login_head.lua")
Include("\\script\\task\\task_addplayerexp.lua")
Include("\\script\\lib\\awardtemplet.lua")

function LoginMessage()
	local nLevel = GetLevel()
	if (nLevel == 1) then
		Msg2Player("<color=green>Chµo mõng <color=yellow>"..GetName().."<color> ®· ®Õn víi thÕ giíi Vâ L©m TruyÒn Kú Offline")
	end
	if (nLevel > 1) then		
		Msg2Player("<color=green>Server Offline giµnh cho c¸c b¹n nµo ®am mª game Vâ L©m TruyÒn Kú. Chóc c¸c b¹n cã nhiÒu søc kháe vµ cã nh÷ng gi©y phót th­ gi·n vui vÎ trong game.")
	end
	if GetTask(5751) == 0 then 
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,1266,0,0,0}, nBindState=-2}, "ThÇn Hµnh Phï", 1)
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,438,0,0,0}, nBindState=-2}, "Thæ §Þa Phï VÜnh ViÔn", 1)
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,4903,0,0,0}, nBindState=-2}, "CÈm Nang §ång Hµnh", 1)
	tbAwardTemplet:GiveAwardByList({tbProp = {6,1,4916,0,0,0}, nBindState=-2}, "Héi Qu¸n Th¶o D­îc §¬n", 1)
	OpenStoreBox(1)
	OpenStoreBox(2)
	OpenStoreBox(3)
	for i = 1, 250 do
		AddLeadExp(1000000000)
	end
	AddMagic(210,1)
	Earn(5000)
	SetTask(5751,1)
	end
end

if login_add then login_add(LoginMessage, 1) end