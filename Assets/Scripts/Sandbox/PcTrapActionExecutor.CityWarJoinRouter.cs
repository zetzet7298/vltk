// -----------------------------------------------------------------------------
// VLTK Mobile — city-war transfer-map join router trap action hook.
// PC source: script/missions/citywar_city/zhongzhuan_map/trap.lua.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionExecutor
    {
        private bool TryExecuteCityWarJoinRouter(PcTrapActionCatalogEntry action, out TrapActionExecutionResult result)
        {
            result = null;
            if (action == null || !action.IsCityWarJoinRouter) return false;

            int missionMapId = action.CityWarMissionMapId;
            if (!_host.HasMap(missionMapId))
            {
                result = Failure(action, $"city-war mission map {missionMapId} missing from catalog");
                return true;
            }

            int missionState = _host.GetMissionValue(action.CityWarMissionStateVar);
            if (missionState == 0)
            {
                if (_sideEffects != null)
                    _sideEffects.PostMessage(action.CityWarWaitingMessage);
                result = Success(action, $"GetMissionV({action.CityWarMissionStateVar})==0 -> Say(waiting), no join");
                return true;
            }

            int routeCamp = action.CityWarRouteCamp(_host.GetCurrentMapId());
            int missionKey = _host.GetMissionValue(action.CityWarMissionKeyVar);
            if (_host.GetTaskValue(action.CityWarTaskKeyId) == missionKey &&
                _host.GetTaskValue(action.CityWarTaskValueId) == routeCamp)
            {
                JoinCityWarCamp(action, routeCamp, missionMapId);
                result = Success(action, $"existing city-war task key/value -> JoinCamp({routeCamp},2)");
                return true;
            }

            int cityId = _host.GetCityArea();
            if (cityId <= 0)
            {
                result = Failure(action, "GetWarOfCity()==0 -> no active city-war city");
                return true;
            }

            int oddCardItemId = action.CityWarCardItemId(cityId, oddCard: true);
            int evenCardItemId = action.CityWarCardItemId(cityId, oddCard: false);
            int cardCamp = 0;
            int cardItemId = 0;
            if (oddCardItemId > 0 && _host.HaveItem(oddCardItemId, 1))
            {
                cardItemId = oddCardItemId;
                cardCamp = 2;
            }
            else if (evenCardItemId > 0 && _host.HaveItem(evenCardItemId, 1))
            {
                cardItemId = evenCardItemId;
                cardCamp = 1;
            }

            if (cardCamp == 0)
            {
                if (_sideEffects != null)
                    _sideEffects.PostMessage(action.CityWarNoCardMessage);
                var outerTarget = action.CityWarOuterWorldPosition();
                _host.SetPos(outerTarget);
                result = Success(action,
                    $"JoinWithCard({routeCamp},1) no card for city {cityId} -> Say(no card), SetPos(1613,3185) -> {outerTarget}");
                return true;
            }

            if (!_host.DelItem(cardItemId, 1))
            {
                result = Failure(action, $"DelItemEx({cardItemId}) failed");
                return true;
            }

            _host.SetTaskValue(action.CityWarTaskId, action.CityWarMissionId);
            _host.SetTaskValue(action.CityWarTaskKeyId, missionKey);
            _host.SetTaskValue(action.CityWarTaskValueId, cardCamp);
            _host.SetTaskValue(action.CityWarTaskCityId, cityId);
            JoinCityWarCamp(action, cardCamp, missionMapId);
            result = Success(action,
                $"JoinWithCard city {cityId} item {cardItemId} -> SetTask({action.CityWarTaskId},{action.CityWarMissionId}), SetTask({action.CityWarTaskValueId},{cardCamp}), JoinCamp({cardCamp},2)");
            return true;
        }

        private void JoinCityWarCamp(PcTrapActionCatalogEntry action, int camp, int missionMapId)
        {
            // PC JoinCamp(Camp, Type) in citywar_city/camper.lua: LeaveTeam, mark
            // temp join state, set current camp, disable team/PK punish, set death
            // script/fight state, then NewWorld(CS_CampPosN).
            _host.LeaveTeam();
            _host.SetCurCamp(camp);
            _host.SetTaskTemp(action.CityWarJoinStateTempTaskId, 1);
            _host.SetTaskTemp(action.CityWarJoinLockTempTaskId, 1);
            _host.SetLogoutRv(1);
            _host.SetPunish(0);
            _host.SetCreateTeam(0);
            _host.SetPkFlag(1);
            _host.ForbidChangePk(1);
            _host.SetDeathScript(@"\script\missions\citywar_city\playerdeath.lua");
            _host.SetFightState(0);
            _host.NewWorld(missionMapId, action.CityWarCampWorldPosition(camp));
        }
    }
}
