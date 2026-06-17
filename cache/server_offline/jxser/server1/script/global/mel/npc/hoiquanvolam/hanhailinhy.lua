Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--										   Hµn H¶i Linh Y										  --
----------------------------------------------------------------------------------------------------
TITLEDIALOG = "Chµo mõng <color=green>%s<color>\n".."Ta chuyªn c¸c lo¹i thuèc! Mäi ng­êi t¹i Linh Thñy Ng­ Th«n nµy khi bÞ th­¬ng th­êng hay t×m ta ch÷a trÞ."

function main()
	dofile("script/global/mel/npc/hoiquanvolam/hanhailinhy.lua")
	local player_name = GetName()
	local tbSay = {format(TITLEDIALOG, GetName())}
        tinsert(tbSay,"Mua thuèc/MuaThuoc")
        tinsert(tbSay,"KÕt thóc ®èi tho¹i./no")
	CreateTaskSay(tbSay)
	return 1
end

function MuaThuoc()
    Sale(212)
end