IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--                                       N©ng CÊp Trang Søc                                       --
----------------------------------------------------------------------------------------------------
TB_UPGRADE_TRANGSUC = {
    [1] = { name = "S¬ CÊp",    nMetal = 10,  nScript = 10,  money = 1000000,  targetLvl = 2,  reqLvl = 1 },
    [2] = { name = "Trung CÊp", nMetal = 50,  nScript = 50,  money = 5000000,  targetLvl = 6,  reqLvl = 2 },
    [3] = { name = "Cao CÊp",   nMetal = 100, nScript = 100, money = 10000000, targetLvl = 10, reqLvl = 6 }
}
nTierTS = 0 
szTSName = "Vâ L©m Trang Søc"

function nangcaptrangsuc()
    local tbSay = {
        "H·y chän ph­¬ng thøc n©ng cÊp!",
        "N©ng cÊp Vâ L©m Trang Søc - S¬ CÊp/TS_Tier_1",
        "N©ng cÊp Vâ L©m Trang Søc - Trung CÊp/TS_Tier_2",
        "N©ng cÊp Vâ L©m Trang Søc - Cao CÊp/TS_Tier_3",
        "§Ó t«i suy nghÜ thªm ®·./OnCancel"
    }
    CreateTaskSay(tbSay)
end

function ConfirmTS()
    local cfg = TB_UPGRADE_TRANGSUC[nTierTS]
    if (cfg == nil) then return end
    local reqName = ""
    if (nTierTS == 1) then
        reqName = "Trang Søc T©n Thñ"
    elseif (nTierTS == 2) then
        reqName = szTSName.." - S¬ CÊp"
    else
        reqName = szTSName.." - Trung CÊp"
    end
    local szMsg = "§Ó n©ng cÊp <color=Green>"..szTSName.." - "..cfg.name.."<color><enter>"..
                  "Nguyªn liÖu cÇn cã:<enter>"..
                  "<color=Green>- "..reqName.."<enter>"..
                  "- "..cfg.nMetal.." Kim Lo¹i HiÕm<enter>"..
                  "- "..cfg.nScript.." Trang Søc Kinh Th­<enter>"..
                  "- "..(cfg.money/10000).." v¹n l­îng<color><enter>"..
                  "<color=Yellow>L­u ý mang ®ñ vËt phÈm!<color>"
    local tbSayTS = {
        szMsg,
        "Ch¾c ch¾n råi/ActionTS",
        "Quay l¹i/nangcaptrangsuc"
    }
    CreateTaskSay(tbSayTS)
end

function ActionTS()
    local cfg = TB_UPGRADE_TRANGSUC[nTierTS]
    if (cfg == nil) then return end
    if CalcEquiproomItemCount(4, 2045, 1, 1) < cfg.nMetal then
        Say("Hµnh trang kh«ng ®ñ "..cfg.nMetal.." Kim Lo¹i HiÕm.", 0) return
    end
    if CalcEquiproomItemCount(0, 14, 0, cfg.reqLvl) < 1 then
        Say("B¹n kh«ng cã Trang Søc yªu cÇu trong hµnh trang.", 0) return
    end
    if CalcEquiproomItemCount(4, 2057, 1, 1) < cfg.nScript then
        Say("Hµnh trang kh«ng ®ñ "..cfg.nScript.." Trang Søc Kinh Th­.", 0) return
    end
    if GetCash() < cfg.money then
        Say("Kh«ng ®ñ ".. (cfg.money/10000) .." v¹n l­îng.", 0) return
    end
    Pay(cfg.money)
    ConsumeEquiproomItem(cfg.nMetal, 4, 2045, 1, 1)
    ConsumeEquiproomItem(1, 0, 14, 0, cfg.reqLvl)
    ConsumeEquiproomItem(cfg.nScript, 4, 2057, 1, 1)
    local nIdx = AddItem(0, 14, 0, cfg.targetLvl, 0, 0, 0)
    if (nIdx > 0) then
        SyncItem(nIdx)
        Msg2Player("<color=green>N©ng cÊp thµnh c«ng:<color> <color=yellow>"..szTSName.." - "..cfg.name.."<color>")
    end
end

function TS_Tier_1() nTierTS = 1 ConfirmTS() end
function TS_Tier_2() nTierTS = 2 ConfirmTS() end
function TS_Tier_3() nTierTS = 3 ConfirmTS() end

----------------------------------------------------------------------------------------------------
--                                      Trang Søc Hoµng Kim                                       --
----------------------------------------------------------------------------------------------------
function trangsuchoangkim()
    local szMsg = "§Ó n©ng cÊp <color=Yellow>Vâ L©m Trang Søc Hoµng Kim<color> cÇn cã:<enter>"..
                  "<color=Green>- Vâ L©m Trang Søc - Cao CÊp<enter>"..
                  "- Kim Lo¹i Hoµng Kim<enter>"..
                  "- 500 Kim Lo¹i HiÕm<enter>"..
                  "- 100 Trang Søc Kinh Th­<enter>"..
                  "- 10 Hoµng Kim LÖnh<enter>"..
                  "- 5000 v¹n l­îng<color><enter>"..
                  "<color=Yellow>L­u ý mang ®ñ vËt phÈm!<color>"
    local tbSayTSHK = {
        szMsg,
        "Ch¾c ch¾n råi/dotrangsuchoangkim",
        "§Ó t«i suy nghÜ ®·./OnCancel"
    }
    CreateTaskSay(tbSayTSHK)
end

function dotrangsuchoangkim()
	if CalcEquiproomItemCount (4,2051,1,1) < 1 then
		Say("§ïa ta µ, kh«ng cã Kim Lo¹i Hoµng Kim lµm sao ta Ðp ®­îc Vâ L©m Trang Søc Hoµng Kim")
	    return
	end
	if CalcEquiproomItemCount (4,2045,1,1) < 500 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 500 Kim Lo¹i HiÕm.")
	    return
	end
    if CalcEquiproomItemCount (4,2057,1,1) < 100 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 100 Trang Søc Kinh Th­.")
	    return
	end
    if CalcEquiproomItemCount (6,1,4908,-1) < 10 then
		Say("H·y xem l¹i hµnh trang kh«ng ®ñ 10 Hoµng Kim LÖnh.")
	    return
	end
	if CalcEquiproomItemCount (0,14,0,10) < 1 then
		Say("H·y xem l¹i hµnh trang kh«ng cã Vâ L©m Trang Søc - Cao CÊp.")
	    return
	end
	if GetCash() >= 50000000 then
		Pay(50000000)
		ConsumeEquiproomItem (1,4,2051,1,1)
		ConsumeEquiproomItem (500,4,2045,1,1)
        ConsumeEquiproomItem (100,4,2057,1,1)
        ConsumeEquiproomItem (10,6,1,4908,-1)
		ConsumeEquiproomItem (1,0,14,0,10)
        ItemIndex = AddGoldItem(0, 1098)
	    SyncItem(ItemIndex)
		Msg2Player("<color=green>N©ng cÊp thµnh c«ng nhËn<color> <color=yellow>Vâ L©m Trang Søc Hoµng Kim<color>")
	else
		Say("H·y xem l¹i hµnh trang b¹n kh«ng cã ®ñ 5000 v¹n l­îng.")
	end
end