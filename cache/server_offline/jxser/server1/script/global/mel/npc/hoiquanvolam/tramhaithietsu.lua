Include("\\script\\global\\global_tiejiang.lua")
Include("\\script\\global\\mel\\configserver.lua")

----------------------------------------------------------------------------------------------------
--										  TrÇm H¶i ThiÕt S­										  --
----------------------------------------------------------------------------------------------------
TIEJIANG_DIALOG = "<dec><npc>T¹i ®©y chuyªn lµm binh khÝ cho nªn rÊt bËn rén.<enter>CÇn lo¹i vò khÝ nµo th× chän tù nhiªn nhÐ."

function main(sel)
	tiejiang_city()
end

function yes()
	if Cfg_CuaHangVuKhi10 ~= 1 then
		Talk(1,"","<color=green>Cöa hµng Vò KhÝ cÊp 10 hiÖn ch­a më!<color>")
	else
		Sale(210)
	end
end