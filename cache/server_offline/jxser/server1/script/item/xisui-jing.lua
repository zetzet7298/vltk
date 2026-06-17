Include("\\script\\global\\pgaming\\configserver\\configall.lua")

function main(sel)
	times = GetTask(81)
	point= {
		{pot=1,msg="mét ®iÓm"},
		{pot=2,msg="mét vµi"},
		{pot=3,msg="mét sè"},
		{pot=4,msg="kh«ng Ýt"},
		{pot=5,msg="h¬i nhiÒu"}
	}
	str ={
		"B¹n ®· xem kü quyÓn TÈy Tñy Kinh nh­ng kh«ng thÓ hiÓu: ( ",
		"B¹n ®· ®äc TÈy Tñy Kinh, nhËn ®­îc %s",
		"B¹n ®· xem kü quyÓn TÈy Tñy Kinh nh­ng kh«ng thÓ hiÓu: ( "
	}
	level = GetLevel()
	if(level < 80) then
		Msg2Player(str[1])
		return 1
	end
	if(times > GioiHanTTK) then
		Msg2Player("Sö dông lÇn thø: "..times.." - "..str[3])
	return 1
	end
	if(level > 89) then
		level = 89
	end
	index = floor((level -80)/2) +1
	AddProp(point[index].pot)
	SetTask(81,times+1)
	Msg2Player(format(str[2],point[index].msg))
	return 0
end