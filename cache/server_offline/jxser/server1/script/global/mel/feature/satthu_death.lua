IncludeLib("ITEM")
IncludeLib("NPCINFO")

function OnDeath()
    local nMySeries = GetNpcSeries(NpcIndex)
    ITEM_DropRateItem(NpcIndex, 8, "\\settings\\droprate\\boss\\bosstask_lev90.ini", 0, 10, nMySeries)
end