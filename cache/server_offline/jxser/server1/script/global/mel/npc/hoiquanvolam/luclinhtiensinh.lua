Include("\\script\\lib\\timerlist.lua")
Include("\\script\\lib\\player.lua")
Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\lib\\awardtemplet.lua")
Include("\\script\\lib\\basic.lua") 
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--										  Lôc Linh Tiªn Sinh									  --
----------------------------------------------------------------------------------------------------
tbWinCashMsg = {"Ng­êi ch¬i %s ®· ¨n ®­îc %s v¹n l­îng tõ Lôc Linh §¹i Nh©n. ThËt may m¾n!",}
tbWinCoinMsg = {"Ng­êi ch¬i %s ®· ¨n ®­îc %s tiÒn ®ång tõ Lôc Linh §¹i Nh©n. ThËt may m¾n",}
FishGame = FishGame or {
	tbPlayerList = {},
	nResult = {},
	nTotalCash = 0,
	nTotalCoin = 0,
	nStarted = 0,
	nTOTAL_RATE = 0,
	DOUBLE_RATE = 2,
	NORMAL_RATE = 1,
}
tbAllResult = tbAllResult or {}

function FishGame:Init()
	if SubWorldID2Idx(1010) < 0 then
		return
	end
	if self.nStarted == 0 then
		self.TimerID = TimerList:AddTimer(self, 18*60)
		self.nStarted = 1
		self.nStatus = 0
	end
	local nLuckRand= random(1,100)
	self.NORMAL_RATE = nLuckRand
	self.DOUBLE_RATE = nLuckRand * 2
	for i=1,6 do
		for j=1,6 do
			for k=1,6 do
				if i==j or i==k or k==j then
					tinsert(tbAllResult,1,{i,j,k,self.DOUBLE_RATE})
					self.nTOTAL_RATE = self.nTOTAL_RATE + self.DOUBLE_RATE
				else
					tinsert(tbAllResult,1,{i,j,k,self.NORMAL_RATE})
					self.nTOTAL_RATE = self.nTOTAL_RATE + self.NORMAL_RATE
				end
			end
		end
	end
end

function FishGame:Stop()
	self.nStarted = 0
end

function FishGame:GetNextResult()
	print("Da chay qua")
	local nRand = random(1,self.nTOTAL_RATE)
	local nResult = 0
	local nValue = 0
	for i = 1,getn(tbAllResult) do
		nValue = nValue + tbAllResult[i][4]
		if nRand <= nValue then
			nResult = i
			break
		end
	end
	if nResult == 0 then
		return nil
	end
	local tbResult = {ChangeToString(tbAllResult[nResult][1]),ChangeToString(tbAllResult[nResult][2]),ChangeToString(tbAllResult[nResult][3])}
	for i=1,3 do
		self.nResult[tbResult[i]] = self.nResult[tbResult[i]] or 0
		self.nResult[tbResult[i]] = self.nResult[tbResult[i]] + 1
	end
	WriteLog("Ket qua bau cua lan nay"..tbResult[1].."va"..tbResult[2].."va"..tbResult[3])
	local SprThongBao = {SprMsg(tbAllResult[nResult][1]),SprMsg(tbAllResult[nResult][2]),SprMsg(tbAllResult[nResult][3])}
	for i=1,3 do
		self.nResult[SprThongBao[i]] = self.nResult[tbResult[i]] or 0--
		self.nResult[SprThongBao[i]] = self.nResult[tbResult[i]] + 1
	end
	local szMsg = format("<bclr=white>KÕt Qu¶ LÇn Më N¾p Nµy Lµ:<bclr><enter> <enter><color=violet> Ba MÆt<color> %s   --  %s  --  %s <enter> <enter><color=green>Mêi rót tay ra ®Ó ta L¾c Hét !<color>",SprThongBao[1],SprThongBao[2],SprThongBao[3])
	Msg2Map(1010, szMsg)
	return 1
end

function SprMsg(nValue)
	if nValue == 1 then
		return "<pic=146>"
	elseif nValue == 2 then
		return "<pic=147>"
	elseif nValue == 3 then
		return "<pic=148>"
	elseif nValue == 4 then 
		return "<pic=149>"
	elseif nValue == 5 then
		return "<pic=150>"
	elseif nValue == 6 then
		return "<pic=151>"
	end
end

function ChangeToString(nValue)
	if nValue == 1 then
		return "BÇu"
	elseif nValue == 2 then
		return "Cua"
	elseif nValue == 3 then
		return "T«m"
	elseif nValue == 4 then
		return "C¸"
	elseif nValue == 5 then
		return "Gµ"
	elseif nValue == 6 then
		return "Nai"
	end
end

function FishGame:CalAwardForPlayer()
	print("Da chay qua 2")
	local nLoseCash = 0
	local nLoseCoint = 0
	for szName,tbInfo in self.tbPlayerList do
		for szChoice,nPoint in self.nResult do
			local tbKind = tbInfo.tbPut[szChoice]
			if tbKind then
				if tbKind.nCash then
					tbInfo.nAwardCash =  tbInfo.nAwardCash + (nPoint + 1)*tbKind.nCash
					nLoseCash = nLoseCash + (nPoint + 1)*tbKind.nCash
				end
				if tbKind.nCoin then
					tbInfo.nAwardCoin =  tbInfo.nAwardCoin + (nPoint + 1)*tbKind.nCoin
					nLoseCoint = nLoseCoint + (nPoint + 1)*tbKind.nCoin
				end
			end
		end
		tbInfo.tbPut = {}
	end
	WriteLog("Bau cua lan nay tong tien van dat "..self.nTotalCash.." thua "..nLoseCash)
	WriteLog("Bau cua lan nay tong tien don dat "..self.nTotalCoin.." thua "..nLoseCoint)
	local LogOpenItemFileName = openfile("data/baucualogs/baucua_"..date("%d_%m_%Y")..".log", "a")
	write(LogOpenItemFileName, date("%H:%M:%S").."\t ----------------------So Dat Cuoc : "..self.nTotalCoin.."\t So Chi Tra "..nLoseCoint.."\tLoi Tuc\t"..self.nTotalCoin-nLoseCoint.."\n")
	closefile(LogOpenItemFileName)
	self.nTotalCash = 0
	self.nTotalCoin = 0
	self.nResult = {}
end

function FishGame:AddAwardForPlayer()
	print("Da chay qua 3")
	for szName,tbInfo in self.tbPlayerList do
		if callPlayerFunction(tbInfo.nIndex,GetName) == szName and tbInfo.nAwardCash and tbInfo.nAwardCash > 0 then
			callPlayerFunction(tbInfo.nIndex,Earn,tbInfo.nAwardCash*10000)
			callPlayerFunction(tbInfo.nIndex,Msg2Player,format("Ng­¬i nhËn ®­îc sè tiÒn %d v¹n tõ BÇu Cua ®¹i nh©n",tbInfo.nAwardCash))
			if tbInfo.nAwardCash > 100  then
				Msg2SubWorld(format("Nh©n vËt <color=green>%s<color> ¨n ®­îc <color=yellow>%d tiÒn v¹n <color>tõ ho¹t ®éng BÇu cua. ThËt may m¾n",szName,tbInfo.nAwardCash))
				WriteLog(format("Nhan vat %s th¾ng so tien %d v¹n",szName ,tbInfo.nAwardCash))
			end
			tbInfo.nAwardCash = 0
		end
		if callPlayerFunction(tbInfo.nIndex,GetName) == szName and tbInfo.nAwardCoin and tbInfo.nAwardCoin > 0 then
			local nCoin = tbInfo.nAwardCoin
			if nCoin <= 100 then
				callPlayerFunction(tbInfo.nIndex,AddStackItem,nCoin,4,417,1,1,0,0)
			else
				local nRound = nCoin/100
				for i=1,nRound do
					callPlayerFunction(tbInfo.nIndex,AddStackItem,100,4,417,1,1,0,0)
				end
				if mod(nCoin,100) > 0 then
					callPlayerFunction(tbInfo.nIndex,AddStackItem,mod(nCoin,100),4,417,1,1,0,0)
				end
			end
			callPlayerFunction(tbInfo.nIndex,Msg2Player,format("Ng­¬i nhËn ®­îc %d tiÒn ®ång tõ BÇu Cua ®¹i nh©n",nCoin))
			Msg2SubWorld(format("Con b¹c <color=green>%s<color> ®· nhËn ®­îc <enter><color=yellow>%d TiÒn §ång<color> tõ <bclr=white>Sßng BÇu Cua<bclr>",szName,nCoin))
			WriteLog(format("Nhan vat %s th¾ng so tien %d ®ång",szName ,nCoin))
			local LogOpenItemFileName = openfile("data/baucualogs/baucua_"..date("%d_%m_%Y")..".log", "a");
			write(LogOpenItemFileName, date("%H:%M:%S").."\t Name: "..szName.."\t Chien Thang Bau Cua "..nCoin.." Tien Dong\n")
			closefile(LogOpenItemFileName)
			tbInfo.nAwardCoin = 0
		end
	end
end

function main()
	dofile("script/global/mel/npc/hoiquanvolam/luclinhtiensinh.lua")
	local nNowTime = tonumber(date( "%H%M "))
	if (nNowTime > ThoiGianBatDauBauCua and nNowTime < ThoiGianKetThucBauCua ) then  
		Say("Ng­êi anh em cÇn g× ë ta?",3,"BÇu cua/OnChoseKind","Xem c¸c cña ®Æt lÇn nµy/showFishGate","Th«i ta hÕt tiÒn råi/no")
	else
		Say(""..ThoiGianBatDauBauCuaText.."",0)
		return
	end
end

function showFishGate()
	local szName = GetName()
	if not FishGame.tbPlayerList[szName]  then
		Say("Ng­¬i ch­a ®Æt cöa nµo c¶",0)
		return
	end
	local pPlayer =FishGame.tbPlayerList[szName]
	local szMsg = ""
	for szChoice,tbKind in pPlayer.tbPut do
		szMsg = szMsg.."Cöa: <color=green>"..szChoice.."<color> ".."TiÒn ®Æt:"
		if tbKind.nCash then
			szMsg = szMsg.."<color=red>"..tbKind.nCash.."<color> v¹n "
		end
		if tbKind.nCoin then
			szMsg = szMsg.." <color=green>"..tbKind.nCoin.."<color> tiÒn ®ång"
		end
		szMsg=szMsg.."\n"
	end
	Describe(szMsg,0)
end

function OnChoseKind()
	Say("Ng­¬i muèn ch¬i kh« m¸u?",3,"TiÒn v¹n/#joinFishGame(1)","TiÒn ®ång/#joinFishGame(2)","Th«i ta thua nhiÒu qu¸ råi/no")
end

function joinFishGame(nKind)
	if FishGame.nStatus ~= 1 then
		Talk(1,"","<color=green> ===== Ta ®ang l¾c hò ®õng véi! ===== <enter><color=violet>§îi 1 Phót n÷a ta l¾c hò xong råi ®Æt !<color>")
		return
	end
	FishGame:OnPlayerJoin(PlayerIndex,GetName())
	if nKind == 1 then
		local tbSay = {
			"BÇu/#OnChoose(1,1)",
			"Cua/#OnChoose(2,1)",
			"T«m/#OnChoose(3,1)",
			"C¸/#OnChoose(4,1)",
			"Gµ/#OnChoose(5,1)",
			"Nai/#OnChoose(6,1)",
			"Th«i ta kh«ng ch¬i n÷a/no"
		}
		Say("Ng­¬i chän con nµo?<enter><color=yellow>ChØ cã thÓ ®Æt c­îc 1 lÇn duy nhÊt, h·y suy nghÜ kü.<color>",getn(tbSay),tbSay)
	else
		local tbSay = {
			"BÇu/#OnChoose(1,2)",
			"Cua/#OnChoose(2,2)",
			"T«m/#OnChoose(3,2)",
			"C¸/#OnChoose(4,2)",
			"Gµ/#OnChoose(5,2)",
			"Nai/#OnChoose(6,2)",
			"Th«i ta kh«ng ch¬i n÷a/no"
		}
		Say("Ng­¬i chän con nµo?<enter><color=yellow>ChØ cã thÓ ®Æt c­îc 1 lÇn duy nhÊt, h·y suy nghÜ kü.<color>",getn(tbSay),tbSay)
	end
end

function OnChoose(nChoice,nKind)
	if nKind == 1 then
		local nMaxCount = 100
		g_AskClientNumberEx(1,nMaxCount, format("TiÒn (1-%d) v¹n", nMaxCount), {FishGame.GetNumberFromClient,{FishGame,nChoice,nKind} })
	else
		local nMaxCount = 100
		g_AskClientNumberEx(1,nMaxCount, format("TiÒn ®ång (1-%d)", nMaxCount), {FishGame.GetNumberFromClient,{FishGame,nChoice,nKind} })
	end
end

function FishGame:GetNumberFromClient(nChoice,nKind,nPutCash)
	self:OnPlayerPut(PlayerIndex,GetName(),nPutCash,nChoice,nKind)
end

function FishGame:OnPlayerPut(nPlayerIndex,szName,nPutCash,nChoice,nKind)
	if nPutCash > 100 then
		Say("§õng cã ¨n gian...:D",0)
		return
	end
	if not self.tbPlayerList[szName] or self.tbPlayerList[szName].nIndex ~= nPlayerIndex then
		callPlayerFunction(nPlayerIndex,Say,"Cã lçi xay ra vui lßng liÖn hÖ GM ®Ó biÕt thªm chi tiÕt",0)
		return
	end
	local pPlayer = self.tbPlayerList[szName]
	local szChoice =""
	if nChoice == 1 then
		szChoice = "BÇu"
		szChoiceSpr = "<pic=140>"
	elseif nChoice == 2 then
		szChoice = "Cua"
		szChoiceSpr = "<pic=141>"
	elseif nChoice == 3 then
		szChoice = "T«m"
		szChoiceSpr = "<pic=142>"
	elseif nChoice == 4 then
		szChoice = "C¸"
		szChoiceSpr = "<pic=143>"
	elseif nChoice == 5 then
		szChoice = "Gµ"
		szChoiceSpr = "<pic=144>"
	elseif nChoice == 6 then
		szChoice = "Nai"
		szChoiceSpr = "<pic=145>"
	end
	if nKind == 1 then
		if pPlayer.tbPut[szChoice] and pPlayer.tbPut[szChoice].nCash and pPlayer.tbPut[szChoice].nCash + nPutCash > 100 then
			Say("Ng­¬i ®Æt qu¸ 100 v¹n mét cöa råi. Th¾ng ta quÞt tiÒn lu«n ®ã:T...",0)
			return
		end
		if pPlayer.tbPut["Cua"] and pPlayer.tbPut["Cua"].nCash and pPlayer.tbPut["Cua"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["BÇu"] and pPlayer.tbPut["BÇu"].nCash and pPlayer.tbPut["BÇu"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["T«m"] and pPlayer.tbPut["T«m"].nCash and pPlayer.tbPut["T«m"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["Gµ"] and pPlayer.tbPut["Gµ"].nCash and pPlayer.tbPut["Gµ"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["C¸"] and pPlayer.tbPut["C¸"].nCash and pPlayer.tbPut["C¸"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["Nai"] and pPlayer.tbPut["Nai"].nCash and pPlayer.tbPut["Nai"].nCash + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		local nOwnCash = callPlayerFunction(nPlayerIndex,GetCash)
		if nOwnCash < nPutCash * 10000 then
			callPlayerFunction(nPlayerIndex,Say,"Kh«ng ®ñ tiÒn mµ còng d¸m ®Æt cöa. §i chç kh¸c ch¬i gióp kÎ kiÕt x¸c nµy",0)
			return
		end
		callPlayerFunction(nPlayerIndex,Pay,nPutCash*10000)
		pPlayer.tbPut[szChoice] = pPlayer.tbPut[szChoice] or {}
		pPlayer.tbPut[szChoice].nCash = pPlayer.tbPut[szChoice].nCash or 0
		pPlayer.tbPut[szChoice].nCash = pPlayer.tbPut[szChoice].nCash + nPutCash	
		WriteLog(format("Nhan vat %s dat cua %s so tien %d v¹n",szName,szChoice,nPutCash))
		if nPutCash > 0 then--
			MsgTienVan = (format("Con b¹c <color=green>%s<color> ®· ®Æt c­îc:<enter> <enter>Cöa %s víi <bclr=white>%d V¹n<bclr>.",szName,szChoiceSpr,nPutCash))
			Msg2Map(1010, MsgTienVan) 
		end
		self.nTotalCash = self.nTotalCash + nPutCash
	else
		if pPlayer.tbPut[szChoice] and pPlayer.tbPut[szChoice].nCoin and pPlayer.tbPut[szChoice].nCoin + nPutCash > 100 then
			Say("Ng­¬i ®Æt qu¸ 100 tiÒn ®ång mét cöa råi. §õng kh« m¸u nh­ thÕ chø..",0)
			return
		end
		if pPlayer.tbPut["Cua"] and pPlayer.tbPut["Cua"].nCoin and pPlayer.tbPut["Cua"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["BÇu"] and pPlayer.tbPut["BÇu"].nCoin and pPlayer.tbPut["BÇu"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["T«m"] and pPlayer.tbPut["T«m"].nCoin and pPlayer.tbPut["T«m"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["Gµ"] and pPlayer.tbPut["Gµ"].nCoin and pPlayer.tbPut["Gµ"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["C¸"] and pPlayer.tbPut["C¸"].nCoin and pPlayer.tbPut["C¸"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		if pPlayer.tbPut["Nai"] and pPlayer.tbPut["Nai"].nCoin and pPlayer.tbPut["Nai"].nCoin + nPutCash > 0 then
			Say("ChØ ®­îc ®Æt c­îc 1 lÇn",0)
			return
		end
		local nOwnCoin = callPlayerFunction(nPlayerIndex,CalcEquiproomItemCount,4, 417, 1, -1)
		if nOwnCoin < nPutCash then
			callPlayerFunction(nPlayerIndex,Say,"Kh«ng ®ñ tiÒn ®ång mµ còng d¸m ®Æt cöa. TÐ ®i",0)
			return
		end
		if callPlayerFunction(nPlayerIndex,ConsumeEquiproomItem,nPutCash, 4, 417, 1, -1) ~= 1 then
			callPlayerFunction(nPlayerIndex,Say,"Xin lçi cã lçi xÈy nghiªm träng vui lßng liªn hÖ GM")
			return
		end
		pPlayer.tbPut[szChoice] = pPlayer.tbPut[szChoice] or {}
		pPlayer.tbPut[szChoice].nCoin = pPlayer.tbPut[szChoice].nCoin or 0
		pPlayer.tbPut[szChoice].nCoin = pPlayer.tbPut[szChoice].nCoin + nPutCash
		WriteLog(format("Nhan vat %s dat cua %s so tien %d tiÒn ®ång",szName,szChoice,nPutCash))
		local LogOpenItemFileName = openfile("data/baucualogs/baucua_"..date("%d_%m_%Y")..".log", "a")
		write(LogOpenItemFileName, date("%H:%M:%S").."\t Name: "..szName.."\t Dat Cuoc "..szChoice.." Voi "..nPutCash.." Tien Dong\n")
		closefile(LogOpenItemFileName)
		if nPutCash > 0 then
			MsgTienDong = (format("Con b¹c <color=green>%s<color> ®· ®Æt c­îc:<enter> <enter>Cöa %s víi <bclr=white>%d TiÒn §ång<bclr>.",szName,szChoiceSpr,nPutCash))
			Msg2Map(1010, MsgTienDong) 
		end
		self.nTotalCoin = self.nTotalCoin + nPutCash
	end
end

function FishGame:OnPlayerJoin(nPlayerIndex,szName)
	if not self.tbPlayerList[szName] then
		self:AddNewPlayer(nPlayerIndex,szName)
		return
	end
	if self.tbPlayerList[szName].nAwardCash > 0 then
		callPlayerFunction(nPlayerIndex,Earn,self.tbPlayerList[szName].nAwardCash*10000 )
		callPlayerFunction(nPlayerIndex,Msg2Player,format("Ng­¬i nhËn ®­îc sè tiÒn %d v¹n tõ bÇu cua thËt may m¾n",self.tbPlayerList[szName].nAwardCash))
		WriteLog(format("Nhan vat %s th¾ng so tien %d v¹n",szName ,self.tbPlayerList[szName].nAwardCash))
		self.tbPlayerList[szName].nAwardCash = 0
	end
	if self.tbPlayerList[szName].nAwardCoin > 0 then
		local nCoin = self.tbPlayerList[szName].nAwardCoin
		if nCoin <= 100 then
			callPlayerFunction(nPlayerIndex,AddStackItem,nCoin,4,417,1,1,0,0)
		else
			local nRound = nCoin/100
			for i=1,nRound do
				callPlayerFunction(nPlayerIndex,AddStackItem,100,4,417,1,1,0,0)
			end
			if mod(nCoin,100) > 0 then
				callPlayerFunction(nPlayerIndex,AddStackItem,mod(nCoin,100),4,417,1,1,0,0)
			end
		end
		self.tbPlayerList[szName].nAwardCoin = 0
		callPlayerFunction(nPlayerIndex,Msg2Player,format("Ng­¬i nhËn ®­îc %d tiÒn ®ång tõ bÇu cua thËt may m¾n",nCoin))
		local LogOpenItemFileName = openfile("data/baucualogs/baucua_"..date("%d_%m_%Y")..".log", "a");
		write(LogOpenItemFileName, date("%H:%M:%S").."\t Name: "..szName.."\t Chien Thang Bau Cua "..nCoin.."\n")
		closefile(LogOpenItemFileName)
		WriteLog(format("Nhan vat %s th¾ng so tien %d ®ång",szName ,nCoin))
	end
	self.tbPlayerList[szName].nIndex = nPlayerIndex
end

function FishGame:AddNewPlayer(nPlayerIndex,szName)
	self.tbPlayerList[szName] = {
		nIndex = nPlayerIndex,
		tbPut ={},
		nAwardCash = 0,
		nAwardCoin = 0,
	}
end

function FishGame:OnTime()
	print("BAU CUA DA CHAY")
	local nMin = tonumber(GetLocalDate("%M"))
	local nX = mod(nMin,3)
	if nX == 0 then
		FishGame.nStatus = 0
		if FishGame:GetNextResult() ~= 1 then
			print("Bi dien roi")
			return
		end
		FishGame:CalAwardForPlayer()
		FishGame:AddAwardForPlayer()
	else
		FishGame.nStatus = 1
	end
	local nNowTime = tonumber(date( "%H%M "))
	if (nNowTime > ThoiGianBatDauBauCua and nNowTime < ThoiGianKetThucBauCua ) then  
		if nX == 1 then
			Msg2Map(1010, "<bclr=white> B¾t ®Çu ®Æt c­îc bÇu cua, h·y lùa chän cöa ®Ó xuèng x¸c nµo, thêi gian ®Æt c­îc lµ <color=yellow>1<color> phót.<bclr>")
		end
		return 1
	end
	self:Stop()
	return
end

----------------------------------------------------------------------------------------------------
function OnTimer(nNpcIndex,nTimeOut)
	if NPCAutoChat == 1 then
		local nNowTime = tonumber(date( "%H%M "))
		if (nNowTime > ThoiGianBatDauBauCua and nNowTime < ThoiGianKetThucBauCua ) then   
			DynamicExecute("\\script\\global\\mel\\npc\\hoiquanvolam\\luclinhtiensinh.lua", "FishGame:Init")
			local tab_Chat = {
				"<pic=115><pic=115><pic=115><bclr=blue><enter>§Õn ®©y ch¬i BÇu Cua nµo c¸c nh©n sü! <pic=00><color><bclr>",
				"<pic=36><bclr=blue><enter>HÕt tiÒn ch¬i th× h·y l¹i TiÒn Trang rót...! <bclr>",            
			}
			local ran = random(1,getn(tab_Chat))
			NpcChat(nNpcIndex,tab_Chat[ran])
			local ranTimer = random(10,20)
			SetNpcTimer(nNpcIndex,ranTimer*18)
			SetNpcScript(nNpcIndex,"\\script\\global\\mel\\npc\\hoiquanvolam\\luclinhtiensinh.lua")
		else
			local tab_Chat2 = {
				"<pic=35><color=green><enter>"..ThoiGianBatDauBauCuaText.."<color>",
			}
			local ran = random(1,getn(tab_Chat2))
			NpcChat(nNpcIndex,tab_Chat2[ran])
			local ranTimer = random(10,20)
			SetNpcTimer(nNpcIndex,ranTimer*18)
			DynamicExecute("\\script\\global\\mel\\npc\\hoiquanvolam\\luclinhtiensinh.lua", "FishGame:Stop")
		end
	end
end

function Add_Npc_LucLinhTienSinh()
	local tb_npc_hotro = {
		{1676,3430},
	}
	local nMapIndex = SubWorldID2Idx(1010)
	for i=1,getn(tb_npc_hotro) do
		local npcID = (447)
		local npcName = "Lôc Linh Tiªn Sinh"
		local npcdialog = AddNpc(npcID,0,nMapIndex,(tb_npc_hotro[i][1])*32,(tb_npc_hotro[i][2])*32,0,npcName,1)
		SetNpcTimer(npcdialog,5*18)
		SetNpcScript(npcdialog,"\\script\\global\\mel\\npc\\hoiquanvolam\\luclinhtiensinh.lua")     
	end
end