Include("\\script\\global\\login_head.lua")
function main(nItemIdx)
dofile("script/item/goldbag.lua")
local nDate = tonumber(GetLocalDate("%d"))
if ( GetTask(DAY) ~= nDate ) then
		SetTask(DAY, nDate);
		SetTask(352,0);
	if (GetTask(352) <= 10) then
local k = random(1,120)
SetTask(352,GetTask(352) + 1)
		if (k <= 6) then 
	AddStackItem(1,4,417,1,1,0,0,0)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=gold>TiÒn §ång<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc  TiÒn §ång")
		elseif (k >= 53) and (k <= 54) then
	Earn(1000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=gold> 1000 L­îng<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc  1000 L­îng")
		elseif (k >= 113) then
		local x = random(1,12)
			if x == 2 then
 	Earn(10000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=green>1 V¹n L­îng<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc  1 V¹n L­îng")
			elseif x == 4 then
	Earn(100000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=green>10 V¹n L­îng<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc  10 V¹n L­îng")
			elseif x == 6 then
	Earn(1000000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=green>100 V¹n L­îng<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc  100 V¹n L­îng")
			elseif (x == 1) or (x == 7) or (x == 8) then
	AddOwnExp(10000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=green>10000 Kinh NghiÖm<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc 10000 Kinh NghiÖm")
			elseif (x == 9) or (x == 10) or (x == 11) then
	AddOwnExp(100000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=pink>100.000 Kinh NghiÖm<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc 100.000 Kinh NghiÖm")
			elseif (x == 3) or (x == 12) then
	AddOwnExp(200000)	
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=gold>100.000 Kinh NghiÖm<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc 200.000 Kinh NghiÖm")
			else
	AddOwnExp(1000000)
	AddGlobalNews("§¹i hiÖp <color=green>"..GetName().."<color> më tói Hoµng Kim , may m¾n nhËn ®­îc <color=gold>1.000.000 §iÓm Kinh NghiÖm<color> !")
	Msg2Player("Më cÈm nang Hoµng Kim, nhËn ®­îc 1.000.000 §iÓm Kinh NghiÖm")
			end
		elseif (k >=63) and (k <= 112) then
	local m = random (5,45)
	n = GetLevel()
	AddOwnExp(n*m*30)
		else
	local m = random (5,45)
	n = GetLevel()
	Earn(n*m*5)
		end
	else
	Talk(1,"","<color=green>H«m nay ®· më ®ñ 10 C¶m nang Hoµng Kim råi, ngµy mai h·y më tiÕp<color>")
	end
end
end 