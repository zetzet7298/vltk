IncludeLib("ITEM")
Include("\\script\\dailogsys\\dailogsay.lua")

----------------------------------------------------------------------------------------------------
--									   Ho¸n §æi TuyÖt §Ønh Ên									  --
----------------------------------------------------------------------------------------------------
AnTuyetDinh = {
    "TuyÖt §Ønh ThiÕu L©m Hoµng Kim Ên",
    "TuyÖt §Ønh Thiªn V­¬ng Hoµng Kim Ên",
    "TuyÖt §Ønh §­êng M«n Hoµng Kim Ên",
    "TuyÖt §Ønh Ngò §éc Hoµng Kim Ên",
    "TuyÖt §Ønh Nga My Hoµng Kim Ên",
    "TuyÖt §Ønh Thóy Yªn Hoµng Kim Ên",
    "TuyÖt §Ønh C¸i Bang Hoµng Kim Ên",
    "TuyÖt §Ønh Thiªn NhÉn Hoµng Kim Ên",
    "TuyÖt §Ønh Vâ §ang Hoµng Kim Ên",
    "TuyÖt §Ønh C«n L«n Hoµng Kim Ên"
}

function hoandoiantuyetdinh()
    dofile("script/global/mel/feature/upgrade/hoandoiantuyetdinh.lua")
    local tbOpt = {
        {"§Æt TuyÖt §Ønh Ên vµo", tuyetdinhan},
        {"KÕt Thóc §èi Tho¹i", No},
    }
    CreateNewSayEx("Ng­¬i muèn ho¸n ®æi TuyÖt §Ønh Ên sang m«n ph¸i kh¸c?\n<enter><color=Green>ChØ bá TuyÖt §Ønh Ên vµo khung!<color>\n<enter><color=Gold>CÇn 500 TiÒn §ång vµ 5000 v¹n Ng©n l­îng.<color>", tbOpt)
end

function tuyetdinhan()
    GiveItemUI("§æi TuyÖt §Ønh Ên kh¸c", "ChØ bá TuyÖt §Ønh Ên vµo khung", "tuyetdinhan1", "onCancel", 1);
end

function tuyetdinhan1(nCount)
    countvk = 0
    if nCount ~= 1 then                     
        Say("Xin kiÓm tra kü, trang bÞ b¹n ®­a ta kh«ng phï hîp yªu cÇu!", 0)
        return 0
    else
        for i = 1, nCount do
            local nItemIndex = GetGiveItemUnit(i)
            szName = GetItemName(nItemIndex)
            for j = 1, getn(%AnTuyetDinh) do
                if szName == %AnTuyetDinh[j] then
                    countvk = countvk + 1
                end
            end
        end
        if countvk ~= 1 then
            Say("VËt phÈm nµy kh«ng ph¶i TuyÖt §Ønh Ên!", 0)
            return 0
        end
        if GetCash() < 50000000 then
            Say("Kh«ng ®ñ 5000 v¹n l­îng!", 0)
            return 0
        end
        if (CalcEquiproomItemCount(4, 417, 1, 1) >= 500) then
            for i = 1, nCount do        
                nItemIndex = GetGiveItemUnit(i)
                k = RemoveItemByIndex(nItemIndex)
                if (k ~= 1) then
                    Say("Cã lçi khi thu håi vËt phÈm!", 0)
                    return 0
                end     
            end
            ConsumeEquiproomItem(500, 4, 417, 1, 1)
            Pay(50000000)
            luachonantuyetdinh()
            Msg2Player("Chóc Mõng "..GetName().." ®· ho¸n ®æi TuyÖt §Ønh Ên thµnh c«ng!")
        else
            Say("Ng­¬i kh«ng mang theo ®ñ 500 TiÒn §ång!", 0);
        end 
    end
end

function luachonantuyetdinh()
    local tbOpt = {
        {"TuyÖt §Ønh ThiÕu L©m Ên", trao_antuyetdinh, {1099}},
        {"TuyÖt §Ønh Thiªn V­¬ng Ên", trao_antuyetdinh, {1100}},
        {"TuyÖt §Ønh §­êng M«n Ên", trao_antuyetdinh, {1101}},
        {"TuyÖt §Ønh Ngò §éc Ên", trao_antuyetdinh, {1102}},
        {"TuyÖt §Ønh Nga My Ên", trao_antuyetdinh, {1103}},
        {"TuyÖt §Ønh Thóy Yªn Ên", trao_antuyetdinh, {1104}},
        {"TuyÖt §Ønh C¸i Bang Ên", trao_antuyetdinh, {1105}},
        {"TuyÖt §Ønh Thiªn NhÉn Ên", trao_antuyetdinh, {1106}},
        {"TuyÖt §Ønh Vâ §ang Ên", trao_antuyetdinh, {1107}},
        {"TuyÖt §Ønh C«n L«n Ên", trao_antuyetdinh, {1108}},
    }
    CreateNewSayEx("<color=green>Chän lo¹i TuyÖt §Ønh Ên muèn ®æi:<color>", tbOpt)
end

function trao_antuyetdinh(nID)
    local nNewIdx = AddGoldItem(0, nID)
    if (nNewIdx > 0) then
        SyncItem(nNewIdx)
    end
end