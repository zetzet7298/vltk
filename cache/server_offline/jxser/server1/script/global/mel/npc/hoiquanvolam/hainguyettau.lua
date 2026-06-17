Include("\\script\\dailogsys\\dailogsay.lua")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--										   H¶i NguyÖt TÈu										  --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta b¸n t¹p hãa ë ®©y!<enter>Mäi ng­êi t¹i Linh Thñy Ng­ Th«n nµy ®Òu ®Õn t×m ta  mua vËt dông. Nhµ ng­¬i cÇn g×?"

function main()
	dofile("script/global/mel/npc/hoiquanvolam/hainguyettau.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        tinsert(tbSay,"Mua vËt dông/MuaVatDung")
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

function MuaVatDung()
	if AllowCheTaoDoTim ~= 1 then
		Talk(1,"","<color=green>TÝnh n¨ng chÕ t¹o ®å TÝm ®· ®ãng!<enter>Ta còng ®ãng cöa hµng lu«n!<color>")
	else
		Sale(211)
	end
end