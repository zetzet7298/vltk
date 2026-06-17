Include("\\script\\missions\\maze\\head.lua")
Include("\\script\\missions\\bairenleitai\\head.lua")
Include("\\script\\missions\\maze\\addstatdata.lua")
Include("\\script\\global\\addxishancunnpc.lua")
Include("\\script\\task\\tollgate\\killbosshead.lua")
Include("\\script\\task\\tollgate\\addtollgatenpc.lua")
Include("\\script\\task\\partner\\partner_head.lua")
Include("\\script\\missions\\freshman_match\\add_championnpc.lua")
Include("\\script\\event\\mid_autumn\\add_zhongqiunpc.lua")
Include("\\script\\missions\\newcitydefence\\cd_addsignnpc.lua")
Include("\\script\\misc\\league_cityinfo.lua")
Include("\\script\\missions\\leaguematch\\wlls_autoexec.lua")
Include("\\script\\missions\\statinfo\\head.lua")
Include("\\script\\event\\wulin_2nd\\wulin_addmaster.lua")
Include("\\script\\global\\addspreadernpc.lua")
Include("\\script\\global\\pgaming\\cauca\\addnpccauca.lua")
Include("\\script\\global\\pgaming\\cauca\\map\\mapcauca.lua")
Include("\\script\\missions\\tongwar\\tongwar_autoexec.lua")
Include("\\script\\task\\lv120skill\\head.lua")
Include("\\script\\tong\\addtongnpc.lua")
Include("\\script\\ÖÐÔ­±±Çø\\ãê¾©\\ãê¾©\\trap\\bianjing_ximen_trap.lua")
Include("\\script\\½­ÄÏÇø\\½ðÉ½µº\\addnpcandtrap.lua")
Include("\\script\\event\\jiefang_jieri\\200904\\denggao\\npc.lua")
Include("\\script\\event\\change_destiny\\npc.lua")
Include("\\script\\missions\\newpracticemap\\addnpc.lua")
Include("\\script\\event\\jiefang_jieri\\201004\\beat_tiger\\head.lua")
Include("\\script\\misc\\timeline\\timelinemanager.lua")
Include("\\script\\global\\autoexec_head.lua")
Include("\\script\\activitysys\\npcfunlib.lua")
Include("\\script\\Î÷ÄÏÄÏÇø\\´óÀí¸®\\´óÀí¸®\\trap\\dali_heidong_trap.lua")
Include("\\script\\global\\pgaming\\npc\\freepk\\chuyendoitrangbihkmp.lua")
Include("\\script\\global\\pgaming\\npc\\freepk\\doivatpham.lua")
Include("\\script\\global\\pgaming\\npc\\freepk\\vatphamhotro.lua")
Include("\\script\\global\\pgaming\\cobac\\baucua\\baucua.lua")
Include("\\script\\global\\pgaming\\npc\\thaydoingoaitrang.lua")
Include("\\script\\global\\pgaming\\npc\\giftcode.lua")
Include("\\script\\global\\pgaming\\doivukhixanh\\doivukhixanh.lua")
Include("\\script\\global\\pgaming\\configserver\\configall.lua")

----------------------------------------------------------------------------------------------------
if (GetProductRegion() =="cn"or GetProductRegion() =="cn_ib") then
	Include("\\script\\task\\lord\\head.lua")
end

if (GetProductRegion() =="vn") then
	Include("\\script\\event\\mid_autumn06\\autoexe.lua")
	Include("\\script\\global\\judgeoffline_special.lua")
	Include("\\script\\event\\collect_juanzhou\\autoaddnpc.lua")
	Include("\\script\\event\\act2years_yn\\head.lua")
	Include("\\script\\event\\newyear_2008\\cailu\\addnpc.lua")
	Include("\\script\\Á½ºþÇø\\ÄÏÔÀÕò\\heisha_autoexec.lua")
	Include("\\script\\event\\great_night\\great_night_head.lua")
end

MSID_LOCALNEWS = 17
maptab={}
maptab[1] = 209
maptab[2] = 210
maptab[3] = 211
maptab[1] = 212
clearskillmap = {243, 245, 247, 249, 251, 253,255}
adddialognpc = {
	{822,387,1306,2564,"\\script\\task\\tollgate\\messenger\\fengzhiqi\\messenger_flynpc.lua","B¹ch Dùc"},
	{822,388,1306,2564,"\\script\\task\\tollgate\\messenger\\fengzhiqi\\messenger_flynpc.lua","B¹ch Dùc"},
	{822,389,1306,2564,"\\script\\task\\tollgate\\messenger\\fengzhiqi\\messenger_flynpc.lua","B¹ch Dùc"},
	{832,390,1586,2600,"\\script\\task\\tollgate\\messenger\\shanshenmiao\\messenger_temnpc.lua","N¹p Lan Thanh Thµnh "},
	{832,391,1586,2600,"\\script\\task\\tollgate\\messenger\\shanshenmiao\\messenger_temnpc.lua","N¹p Lan Thanh Thµnh "},
	{832,392,1586,2600,"\\script\\task\\tollgate\\messenger\\shanshenmiao\\messenger_temnpc.lua","N¹p Lan Thanh Thµnh "},
	{842,393,1386,2442,"\\script\\task\\tollgate\\messenger\\qianbaoku\\messenger_turenpc.lua","Tiªu trÊn "},
	{842,394,1386,2442,"\\script\\task\\tollgate\\messenger\\qianbaoku\\messenger_turenpc.lua","Tiªu trÊn "},
	{842,395,1386,2442,"\\script\\task\\tollgate\\messenger\\qianbaoku\\messenger_turenpc.lua","Tiªu trÊn "},
	{377,387,1570,3132,"\\script\\task\\tollgate\\messenger\\messenger_flyrukou.lua","DÞch quan"},
	{377,388,1570,3132,"\\script\\task\\tollgate\\messenger\\messenger_flyrukou.lua","DÞch quan"},
	{377,389,1570,3132,"\\script\\task\\tollgate\\messenger\\messenger_flyrukou.lua","DÞch quan"},
	{377,390,1320,3185,"\\script\\task\\tollgate\\messenger\\messenger_templerukou.lua","DÞch quan"},
	{377,391,1320,3185,"\\script\\task\\tollgate\\messenger\\messenger_templerukou.lua","DÞch quan"},
	{377,392,1320,3185,"\\script\\task\\tollgate\\messenger\\messenger_templerukou.lua","DÞch quan"},
	{377,393,1412,3203,"\\script\\task\\tollgate\\messenger\\messenger_turerukou.lua","DÞch quan"},
	{377,394,1412,3203,"\\script\\task\\tollgate\\messenger\\messenger_turerukou.lua","DÞch quan"},
	{377,395,1412,3203,"\\script\\task\\tollgate\\messenger\\messenger_turerukou.lua","DÞch quan"},
	{387,176,1314,3194,"\\script\\½­ÄÏÇø\\ÁÙ°²\\ÁÙ°²\\npc\\Ö°ÄÜ_ÉñÃØÌú½³.lua","Thî RÌn ThÇn BÝ"},
	{769,11,3210,4974,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,1,1506,3198,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,37,1647,3050,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,176,1372,3010,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,162,1573,3227,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,78,1512,3206,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{769,80,1700,2963,"\\script\\task\\tollgate\\killer\\nieshichen.lua","NhiÕp ThÝ TrÇn"},
	{1783,174,1644,3308,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,121,2036,4507,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,153,1609,3257,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,101,1694,3118,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,99,1578,3209,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,100,1614,3167,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,20,3534, 6197,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
	{1783,53,1593, 3188,"\\script\\global\\npc\\quanlybanghoi.lua","Ng­êi h­íng dÉn bang héi"},
}
nw_npclist = {
	{1434, 176, 1478, 3238,"\\script\\nationalwar\\npc.lua","Néi c¸c th­îng th­"},
	{1435, 37,  1785, 3041,"\\script\\nationalwar\\npc.lua","Néi c¸c th­îng th­"},
}
npclist_sevencity = {
	{950, 1, 	1642, 3270, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 11, 	3186, 5182, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 162, 	1680, 3276, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 37, 	1692, 3212, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 78, 	1582, 3380, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 80, 	1692, 3218, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
	{950, 176, 	1689, 3289, "\\script\\missions\\sevencity\\dialog_npc.lua", "TiÕp §Çu C«ng Thµnh ChiÕn"},
}
addnewtasknpc={
	{663,80,4,20,3545,6223,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,53,1618,3169,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,121,2010,4484,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,99,1625,3194,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,100,1621,3170,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,101,1692,3140,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,174,1639,3291,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{663,80,4,153,1622,3242,0,"Long Ngò",0,"\\script\\task\\newtask\\education\\Â·ÈË_ÁúÎå.lua"},
	{697,80,4,37,1679,3045,0,"Th¸c B¹t Hoµi Xuyªn",0,"\\script\\ÖÐÔ­±±Çø\\ãê¾©\\ãê¾©\\npc\\passerby_tuoba.lua"},
	{698,30,4,78,1615,3185,0,"L­u UÈn Cæ ",0,"\\script\\ÖÐÔ­ÄÏÇø\\ÏåÑô\\ÏåÑô\\npc\\passerby_liuyungu.lua"},
	{699,35,5,176,1695,3387,0,"Thi Nghi Sinh",0,"\\script\\task\\newtask\\branch\\xiepai\\enemy_shiyisheng.lua"},
	{700,25,5,80,1796,3393,0,"ChÝnh Vâ SÜ ",0,"\\script\\task\\newtask\\branch\\xiepai\\enemy_shenfeng.lua"},
	{701,45,5,78,1781,3213,0,"NguyÔn Minh ViÔn",0,"\\script\\task\\newtask\\branch\\xiepai\\enemy_ruanmingyuan.lua"},
	{702,55,5,78,1359,3514,0,"ThÞnh Do·n",0,"\\script\\task\\newtask\\branch\\xiepai\\enemy_shengyin.lua"},
	{671,25,5,1,1764,3052,0,"Phan Nh­ Long ",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_panrulong.lua"},
	{672,45,5,11,3369,4865,0,"Du T­¬ng T©n ",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_yuxiangjin.lua"},
	{668,55,5,176,1666,2562,0,"TÒ Tøc Phong ",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_qijifeng.lua"},
	{660,80,4,1,1587,3303,0,"Ng¹o V©n T«ng ",0,"\\script\\Î÷±±ÄÏÇø\\·ïÏè\\Â·ÈËnpc\\passerby_aoyunzong.lua"},
	{662,50,4,80,1849,3050,0,"Hçn Hçn",0,"\\script\\ÖÐÔ­ÄÏÇø\\ÑïÖÝ\\npc\\passerby_hunhun.lua"},
	{661,80,4,176,1368,3050,0,"LiÔu Nam V©n ",0,"\\script\\½­ÄÏÇø\\ÁÙ°²\\ÁÙ°²\\npc\\passerby_liunanyun.lua"},
	{675,35,5,131,1878,3400,0,"Hµ Hoa ®¹o BÝch Ngäc",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_hehuadaobiyu.lua"},
	{675,35,5,131,1898,3430,0,"Hµ Hoa ®¹o Nh­ Yªn",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_hehuadaoruyan.lua"},
	{675,35,5,131,1889,3404,0,"Hµ Hoa ®¹o T¨ng Méng",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_hehuadaocengmeng.lua"},
	{676,55,5,162,1728,2969,0,"Tö diÖn Ma V­¬ng Long Truy vò ",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_longzhuiwu.lua"},
	{670,30,5,80,1395,3443,0," (Nh©n vËt vâ l©m) L©u Hoµng Thiªn",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_wulinrenwulouqianxue.lua"},
	{670,30,5,80,1383,3447,0," (Nh©n vËt vâ l©m) TÇn Thêi Phong",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_wulinrenwuqinshifeng.lua"},
	{670,30,5,80,1377,3435,0," (Nh©n vËt vâ l©m) Lam ChiÕn",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_wulinrenwulanzhan.lua"},
	{670,30,5,80,1381,3418,0," (Nh©n vËt vâ l©m) Do·n Song §ång",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_wulinrenwuyinshuangtong.lua"},
	{670,30,5,80,1390,3405,0," (Nh©n vËt vâ l©m) PhÝ L·nh",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_wulinrenwufeileng.lua"},
	{669,25,5,80,2004,2876,0,"TriÒu Thiªn bang chñ Lé Tr­êng Thiªn",0,"\\script\\task\\newtask\\branch\\zhongli\\enemy_luchangtian.lua"},
	{646,80,4,162,1469,3168,0,"M¹c SÇu",0,"\\script\\Î÷ÄÏÄÏÇø\\´óÀí¸®\\´óÀí¸®\\npc\\Â·ÈË_Äª³î.lua"},
	{648,80,4,162,1468,3167,0,"M¹c X¶o Nhi",0,"\\script\\Î÷ÄÏÄÏÇø\\´óÀí¸®\\´óÀí¸®\\npc\\Â·ÈË_ÄªÇÉ¶ù.lua"},
	{649,80,4,11,3226,5118,0,"C«ng Tö TiÕu",0,"\\script\\Î÷ÄÏ±±Çø\\³É¶¼\\³É¶¼\\Â·ÈËnpc\\Â·ÈË_¹«×ÓÐ¦.lua"},
	{678,20,5,179,2033,2755,0,"Ninh t­íng qu©n",0,"\\script\\task\\newtask\\master\\zhongli\\enemy_ningjiangjun.lua"},
	{647,20,5,162,1635,2977,0,"M¹c SÇu",0,"\\script\\task\\newtask\\master\\zhengpai\\Õ½¶·_Äª³î.lua"},
	{653,30,5,141,1540,3324,0,"Giíi L­u Phong",0,"\\script\\task\\newtask\\master\\zhengpai\\Õ½¶·_½çÁ÷·ç.lua"},
	{679,30,5,136,1613,3194,0,"L­ ThiÖn T­îng",0,"\\script\\task\\newtask\\master\\zhongli\\enemy_lushanxiang.lua"},
	{654,40,5,173,1555,3054,0,"Tö §ao HiÖp",0,"\\script\\task\\newtask\\master\\zhengpai\\Õ½¶·_×Ïµ¶ÏÀ.lua"},
	{680,40,5,5,1455,3437,0,"Tõ Tù Lùc",0,"\\script\\task\\newtask\\master\\zhongli\\enemy_xuzili.lua"},
	{655,50,5,24,2114,3323,0,"O¸n §éc",0,"\\script\\task\\newtask\\master\\zhengpai\\Õ½¶·_Ô¹¶¾.lua"},
	{681,50,5,66,1596,3300,0,"Hµn Ng­ ¤ng ",0,"\\script\\task\\newtask\\master\\zhongli\\enemy_hanjiangdudiaosou.lua"},
	{667,60,5,79,1683,3144,0,"Ng­êi thÇn bÝ ",0,"\\script\\task\\newtask\\master\\zhengpai\\Õ½¶·_ÄÏ¹¬·ÉÔÆ.lua"},
	{666,60,5,103,1750,2668,0,"Kh«ng TÞch §¹i S­",0,"\\script\\task\\newtask\\master\\zhongli\\enemy_shaolinkongji.lua"},
	{651,80,4,176,1630,2992,0,"M·nh Phµm",0,"\\script\\½­ÄÏÇø\\ÁÙ°²\\ÁÙ°²\\npc\\Â·ÈË_ÃÏ·².lua"},
	{650,80,4,80,1703,3119,0,"H¹ V« Th¶ ",0,"\\script\\ÖÐÔ­ÄÏÇø\\ÑïÖÝ\\npc\\Â·ÈË_ÏÄÎÞÇÒ.lua"},
	{665,80,4,103,1774,2842,0,"Kh«ng TÞch §¹i S­",0,"\\script\\ÖÐÔ­±±Çø\\ÉÙÁÖÅÉ\\ÉÙÁÖÅÉ\\npc\\passerby_shaolinkongji.lua"},
	{677,80,4,59,1640,3186,0,"Hµn Ng­ ¤ng ",0,"\\script\\Á½ºþÇø\\ÌìÍõ°ï\\ÌìÍõ°ï\\npc\\passerby_hanjiangdudiaosou.lua"},
	{723,25,5,332,1262,2821,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1220,2833,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1244,2881,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1252,2934,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1250,3002,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1307,3007,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1368,3060,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1428,3046,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1476,3049,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{723,25,5,332,1470,3097,0,"Tµng B¶o kh¸ch",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_cangbaoke.lua"},
	{738,35,5,90,1792,3137,0,"¸c lang",0,"\\script\\task\\newtask\\branch\\zhengpai\\enemy_elang.lua"},
	{720,45,2,195,593,3070,0,"Lôc Phi ",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_Â½·Ç.lua"},
	{718,90,2,121,2016,4490,0,"§oan Méc Thanh",0,"\\script\\task\\newtask\\master\\xiepai\\Â·ÈË_¶ËÄ¾Çå.lua"},
	{714,90,2,176,1628,3209,0,"Th¸i C«ng C«ng",0,"\\script\\task\\newtask\\master\\xiepai\\Â·ÈË_²Ì¹«¹«.lua"},
	{722,30,2,90,1814,3283,0,"TiÓu Kú Nhi ",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_Ð¡ì÷¶ù.lua"},
	{719,90,2,80,1592,3118,0,"Tiªu s­ ",0,"\\script\\task\\newtask\\master\\xiepai\\ÑïÖÝ_ïÚÊ¦.lua"},
	{716,45,2,21,2710,3947,0,"H¹ HÇu Phôc",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_ÏÄºî¸´.lua"},
	{715,90,2,174,1593,3256,0,"Phã L«i Th­ ",0,"\\script\\task\\newtask\\master\\xiepai\\Â·ÈË_¸µÀ×Êé.lua"},
	{717,55,2,92,1944,3214,0,"TiÕu V« Th­êng ",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_Ð¤ÎÞ³£.lua"},
	{721,65,2,40,1688,3032,0,"§å §¹i nh©n",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_Í½µ¥´óÈË.lua"},
	{694,80,2,94,1551,3149,0,"Liªu §Þnh",0,"\\script\\task\\newtask\\master\\xiepai\\Õ½¶·_ÁÎ¶¨.lua"},
}

addmasknpc = {}

Include([[\script\event\springfestival07\head.lua]])

tab_springfestivalNPC = {}
tab_zingplay_npc = {}
local tbActNpcList = {
	{1393,176,1657,3261,"\\script\\missions\\dangboss\\gongnv_npc.lua","Ch­ëng §¨ng Cung N÷"},
	{1393,176,1577,2957,"\\script\\missions\\dangboss\\gongnv_npc.lua","Ch­ëng §¨ng Cung N÷"},
	{1393,176,1439,3267,"\\script\\missions\\dangboss\\gongnv_npc.lua","Ch­ëng §¨ng Cung N÷"},
	{1393,176,1385,2977,"\\script\\missions\\dangboss\\gongnv_npc.lua","Ch­ëng §¨ng Cung N÷"},
}

Include("\\script\\activitysys\\g_activity.lua")
Include("\\script\\event\\other\\shensuanzi\\class.lua")
Include("\\script\\activitysys\\functionlib.lua")
Include("\\script\\task\\150skilltask\\g_task.lua")
Include("\\script\\huashan\\npcfaction.lua")

----------------------------------------------------------------------------------------------------
--											Héi Qu¸n Vâ L©m										  --
----------------------------------------------------------------------------------------------------
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\lanhngaothan.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\camtunuong.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\vonggiangkhach.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\ruongchuado.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\thiethuyettuong.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\nganlinhnhi.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\luclinhtiensinh.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\hongduyenmuoi.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\nguvinhthan.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\thietmahung.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\ngunguyensu.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\dieulongsu.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\thiendienquan.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\kimcauthanh.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\tranvutuong.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\nguphonghoa.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\vutuyetthan.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\chanvuton.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\thanhthuytieutien.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\tuyetdinhvude.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\hoandanhtientu.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\vanphapvodanh.lua")
Include("\\script\\global\\mel\\npc\\hoiquanvolam\\thiencolaonhan.lua")
Include("\\script\\global\\mel\\npc\\hoiquannpc.lua")
Include("\\script\\global\\mel\\feature\\taobaitrain.lua")
Include("\\script\\global\\mel\\feature\\huyencocac.lua")

----------------------------------------------------------------------------------------------------
function main()
	-- PGaming
	if ChinhServerPkNhanFullDoVaCap == 1 and ChuyenDoiTrangBiHoangKim == 1 then
		Add_Npc_ChuyenDoiTrangBiHKMP()
	end
	if ChinhServerPkNhanFullDoVaCap == 1 and DoiVatPham == 1 then
		Add_Npc_DoiVatPham()
	end
	if ChinhServerPkNhanFullDoVaCap == 1 and BanItemHoTro == 1 then
		Add_Npc_VatPhamHoTro()
	end
	if ChinhServerPkNhanFullDoVaCap == 1 and DoiVuKhiXanh == 1 then
		Add_Npc_DoiVuKhiXanh()
	end
	if NangCapNgua == 1 then
		local nIndex = AddNpc(229,1,SubWorldID2Idx(78),1581*32,3204*32,0,"N©ng CÊp ThÇn M·")
		SetNpcScript(nIndex,"\\script\\global\\pgaming\\nangcapngua\\npcnangcapngua.lua")
	end
	if DoiTenNhanVat == 1 then
		local nIndex2 = AddNpc(1801,1,SubWorldID2Idx(176),1422*32,3297*32,0,"Vâ L©m Minh Chñ")
		SetNpcScript(nIndex2,"\\script\\global\\general\\thunghiem\\doiten.lua")
	end
	if BauCua == 1 then
		Add_Npc_BauCua()
	end
	if ThayDoiNgoaiTrang == 1 then
		Add_Npc_ThayDoiNgoaiTrang()
	end
	if GiftCode == 1 then
		Add_Npc_GiftCode()
	end

----------------------------------------------------------------------------------------------------
--											Héi Qu¸n Vâ L©m										  --
----------------------------------------------------------------------------------------------------
	Add_Npc_LanhNgaoThan()
	Add_Npc_CamTuNuong()
	Add_Npc_LucLinhTienSinh()
	Add_Npc_VongGiangKhach()
	Add_Npc_RuongChuaDo()
	Add_Npc_ThietHuyetTuong()
	Add_Npc_NganLinhNhi()
	Add_Npc_HongDuyenMuoi()
	Add_Npc_NguVinhThan()
	Add_Npc_ThietMaHung()
	Add_Npc_NguNguyenSu()
	Add_Npc_DieuLongSu()
	Add_Npc_ThienDienQuan()
	Add_Npc_KimCauThanh()
	Add_Npc_TranVuTuong()
	Add_Npc_NguPhongHoa()
	Add_Npc_VuTuyetThan()
	Add_Npc_ChanVuTon()
	Add_Npc_ThanhThuyTieuTien()
	Add_Npc_TuyetDinhVuDe()
	Add_Npc_HoanDanhTienTu()
	Add_Npc_VanPhapVoDanh()
	Add_Npc_ThienCoLaoNhan()
	Add_Npc_HoiQuan()
	tao_bai_train()
	tao_bai_train_HuyenCoCac()
	
----------------------------------------------------------------------------------------------------
	DynamicExecute("\\script\\missions\\tianchimijing\\floor1\\gamefloor1.lua","GameFloor1:Start")
	DynamicExecute("\\script\\missions\\tianchimijing\\floor2\\gamefloor2.lua","GameFloor2:Start")
	DynamicExecute("\\script\\missions\\tianchimijing\\floor3\\gamefloor3.lua","GameFloor3:Start")
	DynamicExecute("\\script\\missions\\tianchimijing\\floor4\\gamefloor4.lua","GameFloor4:Start")
	DynamicExecute("\\script\\missions\\yuegedao\\yuegedao\\yuegedaoworld.lua","YueGeDaoWorld:Start")
	DynamicExecute("\\script\\missions\\yuegedao\\yuegemigu\\yuegemiguworld.lua","YueGeMiGuWorld:Start")
	DynamicExecute("\\script\\changefeature\\initmap.lua","AddDailogNpc")
	DynamicExecute("\\script\\global\\npc\\huoke.lua","CallHuoKeInit")
	DynamicExecute("\\script\\missions\\tianchimijing\\floor1\\yunchihead.lua","AddEnterNpc")
	tbShenSuanZi:Init()
	OpenGlbMission(8)
	OpenGlbMission(MSID_STAT_GOODS_SALE)
	mapcount = getn(maptab)
	for i = 1, mapcount do
		idx = SubWorldID2Idx(maptab[i])
		if (idx ~= -1) then
			SubWorld = idx
			OpenMission(2)
		end
	end
	add_npccauca(npccauca)
	buildAllCityInfoLeague()
	add_dialognpc(hoason_faction)
	load_mission_aexp()
	add_dialognpc(adddialognpc)
	add_dialognpc(nw_npclist)
	add_dialognpc(npclist_sevencity)
	add_newtasknpc(addnewtasknpc)
	add_xishancunnpc(xishancunnpc)
	add_spreadernpc(spreadernpc)
	add_killertasknpc(addkillertasknpc)
	add_alltollgatenpc()
	add_allpartnernpc()
	add_zhongqiunpc()
	cd_addsignnpc()
	wlls_autoexe()
	add_dialognpc(%tbActNpcList)
	tongwar_addsignnpc()
	add_tongnpc()
	local n_date = tonumber(GetLocalDate("%Y%m%d"))
	local szRegion = GetProductRegion()
	if (szRegion =="cn"or szRegion =="cn_ib") then
		add_dialognpc(addmasknpc)
		add_wulinnpc_2nd()
		add_lv120skillnpc()
		add_dialognpc(ch06_addchristmasnpc)
		add_lottery_npc()
		addGraveStone()
		qm07_addGraveStone()
	end
	if (szRegion =="cn_ib") then
		addEmissaryNpc()
	end
	if (szRegion =="vn") then
		add_dialognpc(ACT2YEAR_YN_NPC)
		AnonymVale_auto()
		add_dialognpc(addmasknpc)
		add_dialognpc(tab_springfestivalNPC)
		add_dialognpc(addmoontown_mulao)
		add_dialognpc(tbaddJinShanDao_NpcAndTrap.tbDialogNpc)
		tbaddJinShanDao_NpcAndTrap:AddTrapR()
		tbaddJinShanDao_NpcAndTrap:AddTrapL()
		tbaddJinShanDao_NpcAndTrap:AddMons()
		if (n_date <= 20200101) then
			add_dialognpc(tab_zingplay_npc)
		end
		if(SubWorldID2Idx(11) >= 0) then
			local npcidx = AddNpc(245, 1, SubWorldID2Idx(11), 390 * 8 * 32, 317 * 16 * 32, 1,"Thiªn S¬n §ång L·o")
			SetNpcScript(npcidx,"\\script\\event\\jiefang_jieri\\200904\\denggao\\npc.lua")
		end
		add_dialognpc(au06_tab_kidnpc)
		tbChangeDestiny:addNpc()
		tbNewPracticeMap:addNpc()
		tbBeatTiger:AddNpc()
	end
	add_trap_daliheidong()
	Add_Trap_HuyenCoCac()
	add_trap_bianjingximen()
	add_heisha_entertrap()
	G_ACTIVITY:LoadActivitys()
	G_TASK:LoadAllConfig()
	G_ACTIVITY:OnMessage("ServerStart")
	tbTimeLineManager:LoadAllTimeLine(tbTimeLineList)
	AutoFunctions:Run()
	local szFile ="\\script\\event\\great_night\\great_night_head.lua"
	DynamicExecute(szFile,"OnGreatNightServerStart")
end

function load_mission_aexp()
	mapTab = {235, 236, 237, 238, 239, 240, 241,53,11,153}
	nCount = getn(mapTab)
	oldSubWorld = SubWorld
	for i = 1, nCount do
		idx = SubWorldID2Idx(mapTab[i])
		if (idx ~= -1) then
			SubWorld = idx
			OpenMission(9)
		end
	end
	SubWorld = oldSubWorld
end

function add_dialognpc(Tab)
	for i = 1 , getn(Tab) do
		local itemlist = Tab[i]
		SId = SubWorldID2Idx(itemlist[2])
		if (SId >= 0) then
			npcindex = AddNpc(itemlist[1], 1, SId, itemlist[3] * 32, itemlist[4] * 32, 1, itemlist[6])
			SetNpcScript(npcindex, itemlist[5])
		else
			if itemlist[1] == 1454 then
				print(itemlist[6], itemlist[2])
			end
		end
	end
end

function add_newtasknpc(Tab1)
	for i = 1 , getn(Tab1) do
		Mid = SubWorldID2Idx(Tab1[i][4])
		if (Mid >= 0 ) then
			TabValue5 = Tab1[i][5] * 32
			TabValue6 = Tab1[i][6] * 32
			newtasknpcindex = AddNpc(Tab1[i][1],Tab1[i][2],Mid,TabValue5,TabValue6,Tab1[i][7],Tab1[i][8])
			SetNpcScript(newtasknpcindex, Tab1[i][10])
		end
	end
end

local tbTiFuNpc = {
	tbNpc = {
		{nNpcId=377, szName="LÔ Quan", nPosX=1532, nPosY=3231, szScript="\\script\\global\\Â·ÈË_Àñ¹Ù.lua"},
		{nNpcId=389, szName="Chñ d­îc ®iÕm", nPosX=1556, nPosY=3242, szScript="\\script\\global\\npc\\yaodian.lua"},
		{nNpcId=240, szName="ThuyÒn Phu", nPosX=1519, nPosY=3237, szScript="\\script\\global\\npc\\chuanfu.lua"},
		{nNpcId=309, szName="C«ng B×nh Tö", nPosX=1570, nPosY=3233, szScript="\\script\\missions\\bw\\bwmanager.lua"},
	},
	tbMap = {235, 236, 237, 238},
}

function add_tifu_npc()
	local tbMap = %tbTiFuNpc.tbMap
	local tbNpc = %tbTiFuNpc.tbNpc
	local nMapCount = getn(tbMap)
	local nNpcCount = getn(tbNpc)
	for i=1, nMapCount do
		local nSubWorldIdx = SubWorldID2Idx(tbMap[i])
		if nSubWorldIdx >= 0 then
			ClearMapNpc(tbMap[i])
			ClearMapTrap(tbMap[i])
			for j=1, nNpcCount do
				local tbNpcInfo = tbNpc[j]
				local nNpcIndex = AddNpcEx(tbNpcInfo.nNpcId, 1, random(0,4), nSubWorldIdx, tbNpcInfo.nPosX*32, tbNpcInfo.nPosY*32, 0, tbNpcInfo.szName, 0)
				SetNpcScript(nNpcIndex, tbNpcInfo.szScript)
			end
		end
	end
end

Include("\\script\\lib\\getrectangle_point.lua")

function add_trap_daliheidong()
	local tbpoint = {
		tbtoppoint={1832,3232},
		nleftstep = 80,
		nrightstep = 75,
	}
	local nMapID = 162
	local szScriptfile = "\\script\\Î÷ÄÏÄÏÇø\\´óÀí¸®\\´óÀí¸®\\trap\\´óÀíºÚ¶´.lua"
	local tballpoint = getRectanglePoint(tbpoint)
	for nx,tbp in tballpoint do
		AddMapTrap(nMapID,floor(tbp[1]*32),floor(tbp[2]*32),szScriptfile)
	end
end

Include("\\script\\global\\mel\\feature\\libs\\obs.lua")

function Add_Trap_HuyenCoCac()
	local tbpos = {50592, 102784, 25, 32, 1} 
	local coords = GetObstacles(tbpos)
	local szTrapPath = "\\script\\global\\mel\\map\\traphuyencocac.lua"
	for j = 1, getn(coords) do
		AddMapTrap(1025, coords[j][1], coords[j][2], szTrapPath)
	end
end