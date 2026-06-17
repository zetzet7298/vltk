Include("\\script\\global\\login_head.lua")

TASKID_ZMD_EXPIRE_TIME = 2558

function main(nItemIdx)
    local nStatus = GetTask(TASKID_ZMD_EXPIRE_TIME)
    local szMsg = "<#>Tói hµnh trang hiÖn t¹i: <color=yellow>"..zmd_get_expire_date().."<color>"
    if (nStatus == 1) then
        Say(szMsg, 1, "KÕt thóc/cancel")
    else
        Say(szMsg, 2, "KÝch ho¹t VÜnh ViÔn/#recharge("..nItemIdx..")", "Hñy bá/cancel")
    end
    return 1
end

function recharge(nItemIdx)
    if (RemoveItemByIndex(nItemIdx) ~= 1) then
        return
    end
    SetTask(TASKID_ZMD_EXPIRE_TIME, 1)
    SyncTaskValue(TASKID_ZMD_EXPIRE_TIME)
    SetPartnerBagLevel(10)
    Say("<#>Chóc mõng b¹n ®· kÝch ho¹t thµnh c«ng <color=pink>Tói Hµnh Trang<color>.", 0)
end

function zmd_check_expire_timer()
    local nExpireStatus = GetTask(TASKID_ZMD_EXPIRE_TIME)
    if (nExpireStatus == 1) then
        SetPartnerBagLevel(10)
        return
    end
    if (nExpireStatus > 1) then
        local nNowTime = GetCurServerTime()
        if (nExpireStatus < nNowTime) then
            SetPartnerBagLevel(0)
            Msg2Player("Tói hµnh trang ®· hÕt h¹n sö dông.")
        else
            SetPartnerBagLevel(10)
        end
    else
        SetPartnerBagLevel(0)
    end
end

function zmd_get_expire_date()
    local nStatus = GetTask(TASKID_ZMD_EXPIRE_TIME)
    if (nStatus == 1) then
        return "Sö dông vÜnh viÔn"
    elseif (nStatus == 0) then
        return "Ch­a kÝch ho¹t"
    else
        return FormatTime2String("%Y-%m-%d", nStatus)
    end
end

function cancel()
end

login_add(zmd_check_expire_timer, 0)