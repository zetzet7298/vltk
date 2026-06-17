--Íæ¼ÒËÀÍö½Å±¾

IncludeLib("BATTLE")
Include("\\script\\battles\\battlehead.lua")
Include("\\script\\battles\\marshal\\head.lua")

function OnDeath(Launcher)
	local plus_point = 2
	local msg_str=""
	if isCuoiTuan() == 1 then
		msg_str = " (Cuèi tuÇn x2)"
	end
	-- if isGioCaoDiem() == 1 then
	-- 	msg_str = msg_str.." (Giê cao ®iÓm x2)"
	-- end
	State = GetMissionV(MS_STATE) ;
	if (State ~= 2) then
		return
	end;
	if (bt_CheckDeathValid() == 0) then
		return
	end
	PlayerIndex1 = NpcIdx2PIdx(Launcher);
	OrgPlayer  = PlayerIndex;
	DeathName = GetName();
	deathcamp = GetCurCamp();
	currank = BT_GetData(PL_CURRANK)

	if (PlayerIndex1 > 0) then
		PlayerIndex = PlayerIndex1;
		launchrank = BT_GetData(PL_CURRANK)
		if (GetCurCamp() ~= deathcamp) then
			LaunName = GetName();
			--¸üÐÂÉ±NpcÊýÄ¿ºÍÅÅÐÐ°ñ
			BT_SetData(PL_KILLPLAYER, BT_GetData(PL_KILLPLAYER) + 1); --¼ÇÂ¼Íæ¼ÒÉ±ÆäËüÍæ¼ÒµÄ×ÜÊý
			serieskill = BT_GetData(PL_SERIESKILL) + 1;
			BT_SetData(PL_SERIESKILL, serieskill); 
			serieskill_r = GetTask(TV_SERIESKILL_REALY) or 0
			serieskill_r = serieskill_r + 1
			SetTask(TV_SERIESKILL_REALY,serieskill_r)
			local npoint_lientram = 0
			if (serieskill_r >= 3) then
				if serieskill_r > 10 then
					serieskill_r = 10
				end
				if (deathcamp == 1) then
					npoint_lientram = BT_GetTypeBonus(PL_MAXSERIESKILL, 2) + (serieskill_r * 10)
				else
					npoint_lientram = BT_GetTypeBonus(PL_MAXSERIESKILL, 1) + (serieskill_r * 10)
				end
			end
			if (BT_GetData(PL_MAXSERIESKILL) < serieskill) then 
				BT_SetData(PL_MAXSERIESKILL, serieskill) -- Í³¼ÆÍæ¼ÒµÄ×î´óÁ¬Õ¶Êý
			end
			
			local rankradio = 1;
			
			if ( RANK_PKBONUS[launchrank] == nil or RANK_PKBONUS[launchrank][currank] == nil) then
				rankradio = 1
				print("battle rank tab error!!!please check it !")
			else
				rankradio = RANK_PKBONUS[launchrank][currank]
			end
			local earnbonus = 0
			earnbonus = POINT_TK * rankradio
			earnbonus = (earnbonus * plus_point) + npoint_lientram
			local npoint_tmp = earnbonus
			if isGioCaoDiem() == 1 then
				local nlastpoint = BT_GetData(PL_BATTLEPOINT) or 0
				local nfirstpoint = GetTask(TASKID_FIRST_POINT) or 0
				local ntotalpoint = nlastpoint - nfirstpoint
				if ntotalpoint <= POINT_TK_MAX then
					npoint_tmp = bt_addtotalpoint(earnbonus)
				end				
			end
			
			mar_addmissionpoint(earnbonus)
			
			local rankname = "";
			rankname = tbRANKNAME[currank]
			launchrank = BT_GetData(PL_CURRANK);
			launrankname = tbRANKNAME[launchrank]
			
			BT_SortLadder();
			BT_BroadSelf();
		
			if (GetCurCamp()  == 1) then
				str  = launrankname.." <color=yellow>"..LaunName.."<color> h¹ gôc "..rankname.." <color=yellow>"..DeathName.."<color>/<color=green>liªn tr¶m "..serieskill_r.."<color>/<color=yellow> tÝch lòy "..npoint_tmp.."<color><color=green>".. msg_str;
			else
				str  = launrankname.." <color=yellow>"..LaunName.."<color> h¹ gôc "..rankname.." <color=yellow>"..DeathName.."<color>/<color=green>liªn tr¶m "..serieskill_r.."<color>/<color=yellow> tÝch lòy "..npoint_tmp.."<color><color=green>".. msg_str;
			end
			-- Msg2Player("Chóc mõng! B¹n ®· h¹ ®­îc:"..rankname.." <color=yellow>"..DeathName.."<color>, Tæng PK lµ <color=yellow>"..BT_GetData(PL_KILLPLAYER) .. msg_str);
			Msg2MSAll(MISSIONID, str);
		end
		PlayerIndex = OrgPlayer;
	end;

	BT_SetData(PL_BEKILLED, BT_GetData(PL_BEKILLED) + 1)
	BT_SetData(PL_SERIESKILL, 0)
	SetTask(TV_SERIESKILL_REALY,0)
	
	BT_SortLadder();
	BT_BroadSelf();
	sf_onplayerleave()
	BT_SetData(PL_LASTDEATHTIME, GetGameTime())
end
