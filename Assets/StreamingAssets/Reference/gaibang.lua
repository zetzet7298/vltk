--µü´úº¯Êý£¬ÓÃÓÚ¼ÆËã¼¼ÄÜÊìÁ·¶È
--¾ßÌå·½·¨£º
--¸ù¾Ý1¼¶ÊìÁ·¶È£¬Éý¼¶¼ÓËÙ¶È£¬¼¶Êý£¬ÖØ¸´ÉËº¦´ÎÊý£¬·¶Î§£¬¼ÆËã³öÏàÓ¦µÈ¼¶ÊìÁ·¶È
-- SkillExp(i) = Exp1*a^(i-1)*time*range
EXP_PER = 60
function SkillExpFunc(Exp0,a,Level,Time,Range)
	return floor(Exp0*(a^(Level-1))*Time*Range/2)
end

SKILLS={
	--Ø¤°ï
	gaibang_bangfa={ --Cai Bang bong phap
		addphysicsdamage_p={{{1,10},{20,150}},{{1,-1},{2,-1}},{{1,2},{2,2}}},
		deadlystrikeenhance_p={{{1,2},{20,25,Conic}},{{1,-1},{2,-1}}}
	},
	gaibang_zhangfa={ --Cai Bang chuong phap
		addfiremagic_v={{{1,25},{20,275}},{{1,-1},{2,-1}}}
	},
	yanmen_tuobo={ --Dien mon thac bat
		seriesdamage_p={{{1,1},{20,10}}},
		physicsenhance_p={{{1,10},{20,55}}},
		firedamage_v={
			[1]={{1,10},{20,100}},
			[3]={{1,10},{20,150}}
		},
		addskilldamage1={
			[1]={{1,359},{2,359}},
			[3]={{1,1},{20,40}}
		},
		addskilldamage2={
			[1]={{1,125},{2,125}},
			[3]={{1,1},{20,35}}
		},
		addskilldamage3={
			[1]={{1,1074},{2,1074}},
			[3]={{1,1},{20,32}}
		},
		missle_speed_v={{{1,20},{20,24}}},
		skill_attackradius={{{1,320},{20,384}}},
		skill_cost_v={{{1,10},{20,10}}}
	},
	jianren_shenshou={ --Kien Nhan than thu
		seriesdamage_p={{{1,1},{20,10}}},
		firedamage_v={
			[1]={{1,15},{20,75}},
			[3]={{1,15},{20,215}}
		},
		addskilldamage1={
			[1]={{1,357},{2,357}},
			[3]={{1,1},{20,50}}
		},
		addskilldamage2={
			[1]={{1,128},{2,128}},
			[3]={{1,1},{20,35}}
		},
		addskilldamage3={
			[1]={{1,1073},{2,1073}},
			[3]={{1,1},{20,40}}
		},
		addskilldamage4={
			[1]={{1,1101},{2,1101}},
			[3]={{1,1},{20,40}}
		},
		missle_speed_v={{{1,20},{20,24}}},
		skill_attackradius={{{1,320},{20,384}}},
		skill_cost_v={{{1,25},{20,25}}}
	},
	huabu_liushou={ --Hoat bat luu thu
		fastwalkrun_p={{{1,9},{20,66}},{{1,18*120},{20,18*180}}},
		skill_cost_v={{{1,24},{20,50}}}
	},
	dagou_zhen={ --Da Cau bong phap
		addphysicsdamage_p={{{1,10},{20,175}},{{1,-1},{30,-1}},{{1,2},{2,2}}},
		--skill_cost_v={{{1,24},{20,50}}}
	},
	xianglong_zhang={ --Giang Long Thap Bat Chuong
		lifemax_p={{{1,-1},{20,-25}},{{1,-1},{2,-1}}},
		manamax_p={{{1,12},{20,50}},{{1,-1},{2,-1}}},
		addfiremagic_v={{{1,35},{15,250},{20,750}},{{1,-1},{2,-1}}},
	},
	bangda_egou={ --Bong Da Ac Cau
		physicsenhance_p={{{1,10},{20,179}}},
		seriesdamage_p={{{1,10},{20,50},{21,52}}},
		firedamage_v={
			[1]={{1,70},{20,360}},
			[3]={{1,70},{20,420}}
		},
		addskilldamage1={
			[1]={{1,359},{2,359}},
			[3]={{1,1},{20,60}}
		},
		addskilldamage2={
			[1]={{1,1074},{2,1074}},
			[3]={{1,1},{20,50}}
		},
		missle_speed_v={{{1,28},{20,32}}},
		skill_attackradius={{{1,448},{20,512}}},
		skill_cost_v={{{1,28},{20,48}}}
	},
	zuidie_kuangwu={ --Tuy diep cuong vu
		allres_p={{{1,1},{30,30}},{{1,18*120},{30,18*180}}},
		addfiremagic_v={{{1,10},{30,215}},{{1,18*120},{30,18*180}}},
		addfiredamage_v={{{1,10},{30,175}},{{1,18*120},{30,18*180}}},
		deadlystrikeenhance_p={{{1,5},{20,30,Conic}},{{1,18*120},{30,18*180}}},
		skill_cost_v={{{1,50},{20,100}}}
	},
	kanglong_youhui={ --Khang Long Huu hoi
		seriesdamage_p={{{1,10},{20,50},{21,52}}},
		firedamage_v={
			[1]={{1,10},{20,536}},
			[3]={{1,10},{20,536}}
		},
		addskilldamage1={
			[1]={{1,357},{2,357}},
			[3]={{1,1},{20,55}}
		},
		addskilldamage2={
			[1]={{1,1073},{2,1073}},
			[3]={{1,1},{20,45}}
		},
		addskilldamage3={
			[1]={{1,1101},{2,1101}},
			[3]={{1,1},{20,45}}
		},
		skill_misslesform_v={{{1,1},{10,1},{10,2},{20,2}}},--plus
		skill_misslenum_v={{{1,1},{10,1},{20,15},{25,18},{26,18}}},
		skill_param1_v={{{1,0},{10,0},{10,2},{20,2},{21,2}}},--khoang cach 2 tia
		missle_speed_v={{{1,28},{20,32}}},
		skill_attackradius={{{1,448},{20,512}}},
		skill_cost_v={{{1,10},{20,50}}}
	},
	huaxian_weiyi={ --Hoa hiem vi di
		meleedamagereturn_p={{{1,4},{20,46}},{{1,-1},{20,-1}}},
		adddefense_v={{{1,48},{20,800}},{{1,-1},{20,-1}}},
	},
	xiaoyao_gong={ --Tieu dieu cong
		attackspeed_v={{{1,6},{20,65}},{{1,-1},{20,-1}}},
		castspeed_v={{{1,6},{20,65}},{{1,-1},{2,-1}}},
	},
	feilong_zaitian={ --Phi Long Tai Thien
		seriesdamage_p={{{1,20},{15,20},{20,60},{21,62}}},
		firedamage_v={
			[1]={{1,10},{15,300},{20,750}},
			[3]={{1,10},{15,300},{20,750}}
		},
		addskilldamage1={
			[1]={{1,1073},{2,1073}},
			[3]={{1,1},{20,25}}
		},
		addskilldamage2={
			[1]={{1,1101},{2,1101}},
			[3]={{1,1},{20,25}}
		},
		missle_speed_v={{{1,20},{20,24},{21,24}}},
		skill_misslesform_v={{{1,1},{11,1},{11,0},{20,0}}},
		skill_misslenum_v={{{1,1},{11,1},{12,2},{15,2},{16,3},{20,4},{21,4}}},
		skill_attackradius={{{1,448},{20,512},{21,512}}},
		skill_cost_v={{{1,10},{20,65}}},
		skill_eventskilllevel={{{1,1},{20,20}}},
		skill_param1_v={{{1,0},{11,0},{11,32},{20,32},{21,32}}},--khoang cach 2 tia plus
--		skill_flyevent={
--			[1]={{1,0},{10,0},{10,1},{20,1}},
--			[2]={{1,4},{2,4}},
--			[3]={{1,389},{20,389}}
--		},
		skill_collideevent={
			[1]={{1,0},{10,0},{10,1},{20,1}},
			[3]={{1,389},{20,389}}
		},
		skill_showevent={{{1,0},{10,0},{10,4},{20,4}}},
		addskillexp1={{{1,357},{2,357}},{{1,EXP_PER},{20,EXP_PER}},{{1,0},{2,0}}},
		skill_skillexp_v={{	{1,SkillExpFunc(8600,1.15,1,1,1)},
							{2,SkillExpFunc(8600,1.15,2,1,1)},
							{3,SkillExpFunc(8600,1.16,3,1,1)},
							{4,SkillExpFunc(8600,1.17,4,1,1)},
							{5,SkillExpFunc(8600,1.18,5,1,1)},
							{6,SkillExpFunc(8600,1.19,6,2,1)},
							{7,SkillExpFunc(8600,1.20,7,2,1)},
							{8,SkillExpFunc(8600,1.21,8,2,1)},
							{9,SkillExpFunc(8600,1.22,9,2,1)},
							{10,SkillExpFunc(8600,1.23,10,2,1)},
							{11,SkillExpFunc(8600,1.24,11,2,1)},
							{12,SkillExpFunc(8600,1.23,12,2,1)},
							{13,SkillExpFunc(8600,1.22,13,2,1)},
							{14,SkillExpFunc(8600,1.21,14,2,1)},
							{15,SkillExpFunc(8600,1.20,15,3,1)},
							{16,SkillExpFunc(8600,1.19,16,3,1)},
							{17,SkillExpFunc(8600,1.18,17,3,1)},
							{18,SkillExpFunc(8600,1.17,18,3,1)},
							{19,SkillExpFunc(8600,1.16,19,3,1)},
							{20,SkillExpFunc(8600,1.15,20,4,1)},
						}},
	},
--	qianlong_zaiyuan={ --Ç±ÁúÔÚÔ¨
--		seriesdamage_p={{{1,20},{20,60}}},
--		firedamage_v={
--			[1]={{1,17},{20,171}},
--			[3]={{1,17},{20,171}}
--		},
--	},
	longzhan_yuye={ --Long Chien U Da
		seriesdamage_p={{{1,20},{20,60},{21,62}}},
		firedamage_v={
			[1]={{1,17},{20,371}},
			[3]={{1,17},{20,371}}
		},
--		skill_eventskilllevel={{{1,1},{20,20}}},
--		skill_collideevent={
--			[1]={{1,0},{15,0},{15,1},{20,1}},
--			[3]={{1,358},{20,358}}
--		},
--		skill_showevent={{{1,0},{15,0},{15,4},{20,4}}},
	},
	tianxia_wugou={ --Thien Ha Vo Cau
		seriesdamage_p={{{1,20},{15,20},{20,60},{21,62}}},
		skill_misslenum_v={{{1,1},{20,3},{21,3}}},
		physicsenhance_p={{{1,12},{15,100},{20,206}}},
		firedamage_v={
			[1]={{1,70},{15,150},{20,285}},
			[3]={{1,70},{15,200},{20,432}}
		},
		addskilldamage1={
			[1]={{1,1074},{2,1074}},
			[3]={{1,1},{20,25}}
		},
		missle_speed_v={{{1,20},{20,24},{21,24}}},
		skill_attackradius={{{1,448},{20,512},{21,512}}},
		skill_cost_v={{{1,20},{20,50}}},
		addskillexp1={{{1,359},{2,359}},{{1,EXP_PER},{20,EXP_PER}},{{1,0},{2,0}}},
		skill_skillexp_v={{	{1,SkillExpFunc(7000,1.15,1,1,1)},
							{2,SkillExpFunc(7000,1.15,2,1,1)},
							{3,SkillExpFunc(7000,1.16,3,1,1)},
							{4,SkillExpFunc(7000,1.17,4,1,1)},
							{5,SkillExpFunc(7000,1.18,5,1,1)},
							{6,SkillExpFunc(7000,1.19,6,1,1)},
							{7,SkillExpFunc(7000,1.20,7,1,1)},
							{8,SkillExpFunc(7000,1.21,8,1,1)},
							{9,SkillExpFunc(7000,1.22,9,1,1)},
							{10,SkillExpFunc(7000,1.23,10,1,1)},
							{11,SkillExpFunc(7000,1.24,11,1,1)},
							{12,SkillExpFunc(7000,1.23,12,1,1)},
							{13,SkillExpFunc(7000,1.22,13,1,1)},
							{14,SkillExpFunc(7000,1.21,14,2,1)},
							{15,SkillExpFunc(7000,1.20,15,2,1)},
							{16,SkillExpFunc(7000,1.19,16,2,1)},
							{17,SkillExpFunc(7000,1.18,17,3,1)},
							{18,SkillExpFunc(7000,1.17,18,3,1)},
							{19,SkillExpFunc(7000,1.16,19,3,1)},
							{20,SkillExpFunc(7000,1.15,20,3,1)},
						}},
	},
	gaibang120={ --Hon Thien Khi cong 120
		autoattackskill={{{1,720*256 + 1},{20,720*256 + 20},{21,720*256 + 21}},{{1,-1},{20,-1}},{{1,12*18*256 + 1},{15,12*18*256 + 5},{20,12*18*256 + 6},{21,12*18*256 + 6}}},
		skill_desc=
			function(level)
				return "X¸c suÊt <color=orange>"..floor(Link(level,SKILLS.gaibang120.autoattackskill[3]) - 12*18*256).."%<color> g©y ho¹i th­¬ng \n"..
				"Ho¹i th­¬ng lµm gi¶m <color=orange>"..floor(-Link(level,SKILLS.gaibang120zuzhou.physicsres_p[1]))..
				"%<color> PTVL, gi¶m <color=orange>"..floor(-Link(level,SKILLS.gaibang120zuzhou.fireres_p[1]))..
				"%<color> KH,\n ®ång thêi lµm gi¶m gi¸ trÞ PTVL lín nhÊt <color=orange>"..floor(-Link(level,SKILLS.gaibang120zuzhou.physicsresmax_p[1]))..
				"%<color>, gi¶m gi¸ trÞ KH lín nhÊt <color=orange>"..floor(-Link(level,SKILLS.gaibang120zuzhou.fireresmax_p[1]))..
				"%<color>\n ®ång thêi ph¶n ®ßn khi bÞ tÊn c«ng tÇm xa gi¶m <color=orange>"..floor(-Link(level,SKILLS.gaibang120zuzhou.rangedamagereturn_p[1]))..
				"%<color> trong <color=orange>"..floor(Link(level,SKILLS.gaibang120zuzhou.physicsres_p[2]) / 18).." gi©y<color>\n"..
				"Trong vßng <color=orange>"..floor((Link(level,SKILLS.gaibang120.autoattackskill[3]) / (18*256))).." gi©y<color> sau míi cã thÓ thi triÓn tiÕp"
			end,
		skill_skillexp_v={{	{1,17851239},
							{2,19487603},
							{3,22760330},
							{4,27669421},
							{5,34214875},
							{6,42396694},
							{7,52214875},
							{8,63669421},
							{9,76760330},
							{10,91487603},
							{11,107851239},
							{12,135669421},
							{13,174942148},
							{14,225669421},
							{15,274418181},
							{16,344618181},
							{17,425738181},
							{18,517778181},
							{19,620738181},
							{20,620738181},
							}},	
	},
	gaibang120zuzhou={ --Quyet chu' 120
		physicsres_p={{{1,-2},{15,-8},{20,-10},{21,-10}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
		fireres_p={{{1,-3},{15,-12},{20,-15},{21,-15}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
		physicsresmax_p={{{1,-1},{15,-1},{20,-6},{21,-6}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
		fireresmax_p={{{1,-1},{15,-2},{20,-12},{21,-12}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
		--meleedamagereturn_p={{{1,-4},{15,-16},{20,-20},{21,-20}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
		rangedamagereturn_p={{{1,-4},{15,-25},{20,-30},{21,-30}},{{1,3*18},{15,8*18},{20,9*18},{21,9*18}}},
	},
	zhanggaibang150={ --Thoi Thua Luc Long-150
		seriesdamage_p={{{1,40},{15,40},{20,80},{21,82}}},
		firedamage_v={
			[1]={{1,24},{15,720},{20,1800},{23,3096},{26,3744}},
			[3]={{1,24},{15,720},{20,1800},{23,3096},{26,3744}}
		},
		missle_speed_v={{{1,24},{20,40},{21,40}}},
		--skill_misslenum_v={{{1,1},{11,1},{12,2},{15,2},{16,2},{20,3},{21,3}}},
		skill_attackradius={{{1,448},{20,512},{21,512}}},
		skill_cost_v={{{1,12},{20,78},{23,98}}},
		skill_eventskilllevel={{{1,1},{20,20}}},
		--startevent: 1101 same zhanggaibang150
		--flyevent: 1103 (no damage)
--		skill_flyevent={
--			[1]={{1,0},{10,0},{10,1},{20,1}},
--			[2]={{1,4},{2,4}},
--			[3]={{1,389},{20,389}}
--		},
		skill_collideevent={
			[1]={{1,0},{10,0},{10,1},{20,1}},
			[3]={{1,1072},{20,1072}}
		},
		skill_showevent={{{1,0},{10,0},{10,4},{20,4}}},
		addskillexp1={{{1,1073},{2,1073}},{{1,EXP_PER},{20,EXP_PER}},{{1,0},{2,0}}},
		skill_skillexp_v={{	{1,10500},
							{2,12180},
							{3,14373},
							{4,17251},
							{5,21056},
							{6,26127},
							{7,32953},
							{8,42238},
							{9,55008},
							{10,72775},
							{11,97788},
							{12,133433},
							{13,184856},
							{14,259967},
							{15,371060},
							{16,537451},
							{17,789823},
							{18,1177459},
							{19,1780405},
							{20,2730112},
						}},
	},
	zhanggaibang150_2={ --Ngu Dieu Can Khon-150 tang 2
		seriesdamage_p={{{1,40},{20,80},{21,82}}},
		firedamage_v={
			[1]={{1,20},{20,450},{23,585},{26,653}},
			[3]={{1,20},{20,450},{23,585},{26,653}}
		},
--		skill_eventskilllevel={{{1,1},{20,20}}},
--		skill_collideevent={
--			[1]={{1,0},{15,0},{15,1},{20,1}},
--			[3]={{1,358},{20,358}}
--		},
--		skill_showevent={{{1,0},{15,0},{15,4},{20,4}}},
	},
	gungaibang150={ --Bong Huynh Luoc Dia-150
		seriesdamage_p={{{1,40},{15,40},{20,80},{21,82}}},
		skill_misslenum_v={{{1,1},{20,5},{21,5}}},
		physicsenhance_p={{{1,10},{15,80},{20,165},{23,267},{26,318}}},
		firedamage_v={
			[1]={{1,60},{15,120},{20,230},{23,362},{26,428}},
			[3]={{1,60},{15,160},{20,345},{23,567},{26,678}}
		},
		missle_speed_v={{{1,24},{20,24},{21,24}}},
		skill_attackradius={{{1,448},{20,512},{21,512}}},
		skill_cost_v={{{1,20},{20,50},{23,59}}},
		addskillexp1={{{1,1074},{2,1074}},{{1,EXP_PER},{20,EXP_PER}},{{1,0},{2,0}}},
		skill_skillexp_v={{	{1,10500},
							{2,12180},
							{3,14373},
							{4,17251},
							{5,21056},
							{6,26127},
							{7,32953},
							{8,42238},
							{9,55008},
							{10,72775},
							{11,97788},
							{12,133433},
							{13,184856},
							{14,259967},
							{15,371060},
							{16,537451},
							{17,789823},
							{18,1177459},
							{19,1780405},
							{20,2730112},
						}},
	},
}
-----------------------------------------------
--Create by yfeng 2004-05-20
-----------------------------------------------

-----------------------------------------------
--¸ù¾Ý2¸öµã£¬ÇóÏßÐÎº¯Êýf(x)=k*x+b
--y= (y2-y1)*(x-x1)/(x2-x1)+y1
--µ±x2=x1, ÓÐx=c,¸ÃÖ±ÏßÊÇÒ»Ìõ´¹Ö±ÓÚxÖáµÄÖ±Ïß
--ÕâÊÇ¿ÉÒÔÈ¡µÃy=ÈÎÒâÖµ
--Òò´Ë£¬Èç¹ûÒÑÖªÁ½µã(x1,y1),(x2,y2)¿ÉÇóµÃ¹ý´Ë2µãµÄ
--º¯ÊýÎª£º
function Line(x,x1,y1,x2,y2)
	if(x2==x1) then
		return y2
	end
	return (y2-y1)*(x-x1)/(x2-x1)+y1
end

-----------------------------------------------
--¸ù¾Ý2¸öµã£¬Çó2´ÎÐÎº¯Êýf(x)=a*x2+c
--y= (y2-y1)*x*x/(x2*x2-x1*x1)-(y2-y1)*x1*x1/(x2*x2-x1*x1)+y1
--µ±x1»òÕßx2 < 0 ,y =0
--µ±x2=x1, ÓÐx=c,ÊÇÒ»Ìõ´¹Ö±ÓÚxÖáµÄÖ±Ïß
--ÕâÊÇ¿ÉÒÔÈ¡µÃy=ÈÎÒâÖµ
--Òò´Ë£¬Èç¹ûÒÑÖªÁ½µã(x1,y1),(x2,y2)¿ÉÇóµÃ¹ý´Ë2µãµÄ
--º¯ÊýÎª£ºextrac
function Conic(x,x1,y1,x2,y2)
	if((x1 < 0) or (x2<0))then 
		return 0
	end
	if(x2==x1) then
		return y2
	end
	return (y2-y1)*x*x/(x2*x2-x1*x1)-(y2-y1)*x1*x1/(x2*x2-x1*x1)+y1
end

-----------------------------------------------
--¸ù¾Ý2¸öµã£¬Çó-2´ÎÐÎº¯Êýf(x)=a*sqrt(x2)+c
--y=(y2-y1)*x/(sqrt(x2)-sqrt(x1))+y1-(y2-y1)/((sqrt(x2)-sqrt(x1))
--µ±x2»òÕßx1<0, y=0,
--µ±x1=x2,ÓÐx=c,ÊÇÒ»Ìõ´¹Ö±ÓÚxÖáµÄÖ±Ïß
--ÕâÊÇ¿ÉÒÔÈ¡µÃy=ÈÎÒâÖµ
--Òò´Ë£¬Èç¹ûÒÑÖªÁ½µã(x1,y1),(x2,y2)¿ÉÇóµÃ¹ý´Ë2µãµÄ
--º¯ÊýÎª£ºextrac
function Extrac(x,x1,y1,x2,y2)
	if((x1 < 0) or (x2<0))then 
		return 0
	end
	if(x2==x1) then
		return y2
	end
	return (y2-y1)*(x-x1)/(x2-x1)+y1
end

-----------------------------------------------
--Ãè»æÁ¬½ÓÏß:Link(x,points)
--¸ù¾ÝpointsÌá¹©µÄÒ»ÏµÁÐµã£¬ÓÃÏàÁÚµÄÁ½¸öµãÃè»æÇúÏß
--return yÖµ
--x ÊäÈëÖµ
--points µã¼¯ºÏ
--ÐÎÈç£ºpointsÊÇÐÎÈç{{x1,y1,func=xxx},{x2,y2,func=xxx},...{xn,yn,func=xxx}}µÄÓ³Éä
function Link(x,points)
	num = getn(points)
	if(num<2) then
		return -1
	end
	for i=1,num do
		if(points[i][3]==nil) then
			points[i][3]=Line
		end
	end
	if(x < points[1][1]) then
		return points[1][3](x,points[1][1],points[1][2],points[2][1],points[2][2])
	end
	if(x > points[num][1]) then
		return points[num][3](x,points[num-1][1],points[num-1][2],points[num][1],points[num][2])
	end
	
	c = 2
	for i=2,num do
		if((x >= points[i-1][1]) and (x <= points[i][1])) then
			c = i
			break
		end
	end
	return points[c][3](x,points[c-1][1],points[c-1][2],points[c][1],points[c][2])
end

------------------------------------------------------
--¼¼ÄÜÉè¶¨¸ñÊ½ÈçÏÂ£º
--SKILLS={
--	¼¼ÄÜÃû³Æ=	{
--		Ä§·¨ÊôÐÔ=	{
--			[1]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[2]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[3]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬	
--		}£¬
--		Ä§·¨ÊôÐÔ=	{
--			[1]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[2]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[3]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬	
--		}£¬
--		¡£¡£¡£¡£¡£
--	}£¬
--	¼¼ÄÜÃû³Æ=	{
--		Ä§·¨ÊôÐÔ=	{
--			[1]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[2]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[3]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬	
--		}£¬
--		Ä§·¨ÊôÐÔ=	{
--			[1]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[2]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬
--			[3]={{¼¶±ð,ÊýÖµ£¬ÇúÏß}£¬{¼¶±ð£¬ÊýÖµ£¬ÇúÏß}£¬¡£¡£¡£¡£}£¬	
--		}£¬
--		¡£¡£¡£¡£¡£
--	}£¬
--	¡£¡£¡£¡£¡£
--}
--Èç£º
--SKILLS={
--	Sanhuan-taoyue={
--		physicsenhance_p={
--			[1]={{1,50},{20,335}},--Ä§·¨ÊôÐÔphysicsenhance_p²ÎÊý1£¬1¼¶Ê±Îª35£¬20¼¶Ê±Îª335£¬ÇúÏß²»Ìî£¬Ä¬ÈÏÏßÐÎ
--			[2]={{1,0},{20,0}},
--		},--Ã»ÓÐ[3]£¬±íÊ¾Ä§·¨ÊôÐÔphysicsenhance_p²ÎÊý2£¬Ä¬ÈÏÎªÈÎºÎÊ±ºò¶¼ÊÇ0
--		lightingdamage_v={
--			[1]={{1,65},{20,350}},
--			[3]={{1,65},{20,350}},
--		}
--	}
--}
--ÒÔÉÏÃèÊö¼¼ÄÜ¡°Èý»·Ì×ÔÂ¡±µÄÄ§·¨ÊôÐÔºÍÊýÖµ
-----------------------------------------------------------
--º¯ÊýGetSkillLevelData(levelname, data, level)
--levelname£ºÄ§·¨ÊôÐÔÃû³Æ
--data£º¼¼ÄÜÃû³Æ
--level£º¼¼ÄÜµÈ¼¶
--return£ºµ±¼¼ÄÜÃû³ÆÎªdata£¬¼¼ÄÜµÈ¼¶Îªlevel
--			Ê±µÄÄ§·¨ÊôÐÔlevelnameËùÐèÇóµÄÈý¸ö²ÎÊýµÄ¾ßÌåÖµ
-----------------------------------------------------------
function GetSkillLevelData(levelname, data, level)
	if(data==nil) then
		return ""
	end
	if(data == "") then
		return ""
	end
	if(SKILLS[data]==nil) then
		return ""
	end
	if(SKILLS[data][levelname]==nil) then
		return ""
	end
	if(type(SKILLS[data][levelname]) == "function") then
		return SKILLS[data][levelname](level)
	end
	if(SKILLS[data][levelname][1]==nil) then
		SKILLS[data][levelname][1]={{0,0},{20,0}}
	end
	if(SKILLS[data][levelname][2]==nil) then
		SKILLS[data][levelname][2]={{0,0},{20,0}}
	end
	if(SKILLS[data][levelname][3]==nil) then
		SKILLS[data][levelname][3]={{0,0},{20,0}}
	end
	p1=floor(Link(level,SKILLS[data][levelname][1]))
	p2=floor(Link(level,SKILLS[data][levelname][2]))
	p3=floor(Link(level,SKILLS[data][levelname][3]))
	return Param2String(p1,p2,p3)
end;


function Param2String(Param1, Param2, Param3)
return Param1..","..Param2..","..Param3
end;

