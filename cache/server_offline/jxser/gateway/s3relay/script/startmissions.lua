Include("\\RelaySetting\\battle\\script\\rf_header.lua")
Include("\\script\\event\\expansion\\201006\\fuguijinhe\\fuguijinhe.lua")
Include("\\RelaySetting\\Task\\makegoldboss\\boss_dai_hoangkim_1900.lua")
Include("\\RelaySetting\\Task\\makegoldboss\\tuyetdinhvude.lua")
Include("\\RelaySetting\\Task\\smallboss\\small_boss_1200.lua")
Include("\\RelaySetting\\Task\\tongkim\\battle_1_honour.lua")
Include("\\script\\mission\\boss\\bigboss.lua")
Include("\\script\\gb_modulefuncs.lua")
Include("\\script\\leaguematch\\timetable.lua")
Include("\\script\\leaguematch\\head.lua")
Include("\\script\\leaguematch\\switch.lua")
Include("\\script\\leaguematch\\task.lua")
Include("\\script\\gb_taskfuncs.lua")
Include("\\script\\mission\\sevencity\\war.lua")
Include("\\script\\tong\\tong_header.lua")
IncludeLib("TONG")

----------------------------------------------------------------------------------------------------
--									Khëi §éng Ho¹t §éng M¸y Chñ									  --
----------------------------------------------------------------------------------------------------
-- Tèng Kim Cao CÊp
function StartTongKim_3()
	Battle_StartNewRound(1,3)
	OutputMsg("===> Khoi dong Tong Kim Cao Cap [GM]")
	zMsg2SubWorld  = "<color=yellow>ChiÕn tr­êng Tèng - Kim ®· ®Õn giê b¸o danh, c¸c nh©n sÜ giang hå nhanh ch©n tham gia ®Çu qu©n, Thêi gian b¸o danh lµ 10 phót."
	zAddLocalCountNews = "ChiÕn tr­êng Tèng Kim ®· b¾t ®Çu b¸o danh, c¸c nh©n sÜ giang hå mau ®Õn khu vùc b¸o danh ®Ó tham gia chiÕn tr­êng."
	GlobalExecute(format("dw Msg2SubWorld([[%s]])",zMsg2SubWorld))
	GlobalExecute(format("dw AddLocalCountNews([[%s]], 1)",zAddLocalCountNews))
end

----------------------------------------------------------------------------------------------------
-- Boss §¹i Hoµng Kim
function Call_BigBoss()
	OutputMsg("===> Khoi dong BOSS HOANG KIM MON PHAI [GM]")
	GlobalExecute("dwf \\script\\global\\pgaming\\missions\\bosshoangkim\\bossdai\\goldboss_main.lua bigboss_call2world()")
end

----------------------------------------------------------------------------------------------------
-- Boss TiÓu Hoµng Kim
function Call_SmallBoss()
	OutputMsg("===> Khoi dong BOSS TIEU HOANG KIM [GM]")
	GlobalExecute("dwf \\script\\global\\pgaming\\missions\\bosshoangkim\\bosstieu\\smallboss_main.lua  smallboss_call2world()")
end

----------------------------------------------------------------------------------------------------
-- V­ît ¶i
function VuotAi()
	OutputMsg("===> Khoi dong Vuot Ai [GM]")
	GlobalExecute("dwf \\settings\\trigger_challengeoftime.lua OnTrigger()")
	GlobalExecute(format( "dw Msg2SubWorld([[%s]])", "<color=0xa9ffe0><color=yellow>V­ît ¶i<color> ®· b¾t ®Çu b¸o danh, c¸c nh©n sü nhanh ch©n tíi <color=earth>NhiÕp ThÝ TrÇn<color> ®Ó ®¨ng ký b¸o danh, thêi gian b¸o danh lµ <color=green>10 phót<color>!."))
end

----------------------------------------------------------------------------------------------------
-- Phong L¨ng §é
function PhongLangDo()
    GlobalExecute("dwf \\script\\missions\\fengling_ferry\\fldmap_boat1.lua fenglingdu_main()")
	OutputMsg("===> Khoi dong Phong Lang Do [GM]")
	szMsg = "BÕn thuyÒn Phong L¨ng §é(§Æc BiÖt) ®· b¾t ®Çu më cña, c¸c vÞ §¹i hiÖp mau ®Õn bê nam gÆp thuyÒn phu b¸o danh ra tay tiªu diÖt thñy tÆc. Thêi gian b¸o danh lµ 10 phót."
	zMsg2SubWorld = "<color=0xa9ffe0>BÕn thuyÒn <color=yellow>Phong L¨ng §é <color>®· b¾t ®Çu më cña,c¸c vÞ §¹i hiÖp mau ®Õn bê nam gÆp <color=pink>thuyÒn phu<color> ®Ó b¸o danh ra tay tiªu diÖt thñy tÆc.Thêi gian b¸o danh lµ <color=pink>10<color> phót."
	GlobalExecute(format("dw AddLocalCountNews([[%s]], 2)",szMsg))
	GlobalExecute(format("dw Msg2SubWorld([[%s]])", zMsg2SubWorld))
end

----------------------------------------------------------------------------------------------------
-- Phong Háa Liªn Thµnh
function PhongHoaLienThanh(loai, phe)
	OutputMsg("===> Khoi dong Phong Hoa Lien Thanh 'VÖ quèc liªn thµnh' - Tèng ®· b¾t ®Çu b¸o danh.")
	GlobalExecute("dwf \\script\\gmscript.lua NewCityDefence_OpenMain(1)")
end

----------------------------------------------------------------------------------------------------
-- TuyÖt §Ønh Vò §Õ
function TuyetDinhVuDe()
	OutputMsg("===> Khoi dong BOSS TUYET DINH VU DE [GM]")
	GlobalExecute("dwf \\script\\global\\mel\\mission\\tuyetdinhvude\\goldboss_main.lua vude_call2world()")
end

----------------------------------------------------------------------------------------------------