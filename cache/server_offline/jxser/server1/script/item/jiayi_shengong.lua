Include("\\script\\task\\metempsychosis\\task_func.lua")

function main()
	if (check_zhuansheng_level() == 1) then
		if (GetTask(TSK_ZHUANSHENG_FLAG) == 0) then
			Msg2Player(format("Häc thµnh c«ng <color=green>%s<color>.", "B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn"))
			Say(format("§· häc %s<enter>Cã thÓ ®Õn gÆp <color=green>%s<color> ®Ó <color=green>Trïng Sinh<color>!", "B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn", "TuyÖt §Ønh Vò §Õ"), 0)
			SetTask(TSK_ZHUANSHENG_FLAG,1)
			WriteLog(format("[NhiÖm vô trïng sinh]\t%s\tName:%s\tAccount:%s\t CÊp bËc häc:%d, M«n ph¸i:%d", GetLocalDate("%Y-%m-%d %X"),GetName(), GetAccount(), GetLevel(), GetLastFactionNumber()))
			return 0
		else
			--Msg2Player("§· häc <B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn> råi, chØ cÇn häc thªm <B¾c §Èu Tr­êng Sinh ThuËt - T©m Ph¸p Thiªn> lµ cã thÓ c«ng thµnh danh to¹i ®­îc råi.")
			Msg2Player("§· häc B¾c §Èu Tr­êng Sinh ThuËt - C¬ Së Thiªn råi. Giê ®©y cã thÓ Trïng Sinh Nh©n VËt")
			return 1
		end
	end
	return 1
end