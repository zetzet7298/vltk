npc_hoiquan ={
	{64,80,4,1010,1747,3353,0,"ThiÕu L©m Ph­¬ng Tr­îng",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\thieulam.lua"},
	{73,80,4,1010,1755,3361,0,"Vâ §ang Ch­ëng M«n",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\vodang.lua"},
	{118,80,4,1010,1734,3368,0,"Ngò §éc Gi¸o Chñ",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\ngudoc.lua"},
	{130,80,4,1010,1738,3364,0,"Thiªn NhÉn Gi¸o Chñ",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\thiennhan.lua"},
	{92,80,4,1010,1750,3383,0,"C«n L«n Ch­ëng M«n",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\conlon.lua"},
	{98,80,4,1010,1754,3379,0,"C¸i Bang Bang Chñ",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\caibang.lua"},
	{82,80,4,1010,1757,3375,0,"Nga My Ch­ëng M«n",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\ngamy.lua"},
	{104,80,4,1010,1743,3376,0,"§­êng M«n Ch­ëng M«n",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\duongmon.lua"},
	{125,80,4,1010,1747,3372,0,"Thóy Yªn Ch­ëng M«n",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\thuyyen.lua"},
	{111,80,4,1010,1751,3368,0,"Thiªn V­¬ng Bang Chñ",0,"\\script\\global\\pgaming\\npc\\chuongmoncacphai\\thienvuong.lua"},
	{108,80,4,1010,1661,3387,0,"D· TÈu",0,"\\script\\global\\seasonnpc.lua"},
	{308,80,4,1010,1644,3344,0,"Vâ L©m TruyÒn Nh©n",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\volamtruyennhan.lua"},
	{377,80,4,1010,1654,3334,0,"LÔ Quan",0,"\\script\\global\\pgaming\\npc\\lequan.lua"},
	{769,80,4,1010,1655,3408,0,"NhiÕp ThÝ TrÇn",0,"\\script\\task\\tollgate\\killer\\nieshichen.lua"},
	{235,80,4,1010,1602,3401,0,"Xa Phu",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\xaphu.lua"},
	{236,80,4,1010,1600,3264,0,"Xa Phu",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\xaphu.lua"},
	{237,80,4,1010,1665,3241,0,"Xa Phu",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\xaphu.lua"},
	{240,80,4,1010,1795,3423,0,"Phong L¨ng §é bÕn 3",0,"\\script\\ÖÐÔ­±±Çø\\·çÁê¶É\\npc\\south_boatman3.lua"},
	{240,80,4,1010,1791,3427,0,"Phong L¨ng §é bÕn 2",0,"\\script\\ÖÐÔ­±±Çø\\·çÁê¶É\\npc\\south_boatman2.lua"},
	{240,80,4,1010,1787,3431,0,"Phong L¨ng §é bÕn 1",0,"\\script\\ÖÐÔ­±±Çø\\·çÁê¶É\\npc\\south_boatman1.lua"},
	{228,80,4,1010,1669,3425,0,"TiÒn Trang",0,"\\script\\global\\pgaming\\npc\\tientrang.lua"},
	{625,80,4,1010,1777,3464,0,"TrÊn Ba Thñ Khè",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\ruongchuado2.lua"},
	{203,80,4,1010,1763,3444,0,"Hµn H¶i Linh Y",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\hanhailinhy.lua"},
	{310,80,4,1010,1754,3459,0,"H¶i NguyÖt TÈu",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\hainguyettau.lua"},
	{199,80,4,1010,1748,3481,0,"TrÇm H¶i ThiÕt S­",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\tramhaithietsu.lua"},
	{260,80,4,1010,1755,3509,0,"Ng­ L·o ¤ng",0,"\\script\\global\\pgaming\\cauca\\npc\\canthulaonhan.lua"},
	{203,80,4,1025,1575,3213,0,"HuyÒn C¬ Linh Y",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\hanhailinhy.lua"},
	{95,80,4,1025,1574,3232,0,"HuyÒn C¬ L·o Nh©n",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\xaphu2.lua"},
	{625,80,4,1025,1588,3227,0,"HuyÒn C¬ Thñ Khè",0,"\\script\\global\\mel\\npc\\hoiquanvolam\\ruongchuado3.lua"},
}

function Add_Npc_HoiQuan()
	add_npchoiquan(npc_hoiquan)
end

----------------------------------------------------------------------------------------------------
function add_npchoiquan(tbnpc)
	for i = 1 , getn(tbnpc) do
		Mid = SubWorldID2Idx(tbnpc[i][4])
		if (Mid >= 0 ) then
			TabValue5 = tbnpc[i][5] * 32
			TabValue6 = tbnpc[i][6] * 32
			local nNpcIdx = AddNpc(tbnpc[i][1],tbnpc[i][2],Mid,TabValue5,TabValue6,tbnpc[i][7],tbnpc[i][8])
			SetNpcScript(nNpcIdx, tbnpc[i][10])
		end
	end
end