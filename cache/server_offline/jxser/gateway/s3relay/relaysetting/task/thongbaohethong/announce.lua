function TaskShedule()
	TaskName("THONG BAO TIN TUC TU DONG")
	TaskInterval(10)
	local n_cur_h = tonumber(date("%H"))
	local n_cur_m = tonumber(date("%M"))
	if (n_cur_m > 50) then
		n_cur_h = mod((n_cur_h + 1), 24)
	end
	n_cur_m = mod((n_cur_m - mod(n_cur_m, 10) + 10), 60)
	TaskTime( n_cur_h, n_cur_m )
	TaskCountLimit(0)
	OutputMsg("==========================================================================================")
	OutputMsg("                           Khoi dong THONG BAO TIN TUC TU DONG")
	OutputMsg("==========================================================================================")
end

g_strTipMsg = {}
g_nFutureDate = 2019010424
STR_HEAD_FUTURE = "[Ho¹t §éng C÷u NguyÖt Phong Ba]"
g_strFutureMsg = {
	"9 nguyÖt phong ba ho¹t ®éng ®ang tiÕn hµnh, mçi ngµy, mçi tuÇn, mçi th¸ng ®Òu cã th­ëng cho chê ng­¬i. Tõ ®¸o trong thêi gian, tham gia ho¹t ®éng, tû nh­ tèng kim ®¹i chiÕn, d· tÈu nhiÖm vô, s¸t thñ nhiÖm vô ®Òu cã thÓ tèng vi tİch ph©n. Cã thÓ ®i kiÕn vâ l©m truyÒn thô cã lÏ ë trang chİnh kh¸n t×nh h×nh cô thÓ vµ tØ mØ ",
	"9 nguyÖt phong ba ho¹t ®éng ®ang tiÕn hµnh, mçi ngµy vi tİch ph©n kh¶ dÜ ®æi lÊy th­ëng cho, cô thÓ nh­ sau: Mçi tuÇn tæng vi tİch ph©n bµi danh tiÒn m­êi ngo¹n gia t­¬ng thu ®­îc chu th­ëng cho, toµn bé ho¹t ®éng trong lóc, tæng vi tİch ph©n bµi danh tiÒn m­êi, t­¬ng thu ®­îc nguyÖt th­ëng cho. Qu¸n qu©n th­ëng cho vi ®¹i m· n·o nhÉn th­ëng cho cã lÏ ngÉu nhiªn thu ®­îc mét bé ®¹i hoµng kim trang bŞ, kü n¨ng + 1!",
	"? ®iÒu chØnh b¹ch c©u hoµn giíi c¸ch: Tõ 9 nguyÖt 8 nhËt khëi, l©m an ng­êi b¸n hµng rong cã m¹i b¹ch c©u hoµn, gi¸ tiÒn lµ 1 mét ®ång tiÒn / mét: Mçi ngµy tõ 0:00 ®¸o 12:00 ly tuyÕn kinh nghiÖm vÉn nh­ cò t¨ng béi!",
}

function TaskContent()
	str = ""
	local ndateH = tonumber(date("%H"))
	local ndateM = tonumber(date("%M"))
	local ndateD = tonumber(date("%Y%m%d"))
	if (ndateH < 21 and ndateM == 0 and ndateD >= 20090116 and ndateD < 20090216) then
		str	= "HiÖn nay vâ l©m nh©n sÜ ®Ğn NPC ThÇn Tµi nhËn lÔ vµ lÜnh ph¸o hoa "
	elseif (ndateH == 21 and ndateM == 0 and ndateD >= 20090116 and ndateD < 20090216) then
		str	= "Vò l©m nh©n sÜ nhanh lªn mét chót lai lÜnh t­ëng, ®ång thêi cã c¬ héi lÜnh cµng nhiÒu may m¾n th­ëng cho!"
	else
		return
	end
	GlobalExecute(format("dw Msg2SubWorld([[%s]])", str))
end

function getTipMsg()
	nCount = getn(g_strTipMsg)
	nIndex = random(1, nCount)
	return g_strTipMsg[nIndex]
end

function getFutureMsg()
	nCurDate = tonumber(date("%Y%m%d%H"))
	str = ""
	if (g_nFutureDate > nCurDate) then
		nCount = getn(g_strFutureMsg)
		nIndex = random(1, nCount)
		str = STR_HEAD_FUTURE..(g_strFutureMsg[nIndex])
	end
	return str
end

function GameSvrConnected(dwGameSvrIP)
end

function GameSvrReady(dwGameSvrIP)
end