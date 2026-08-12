// -----------------------------------------------------------------------------
// VLTK Mobile — city-war transfer-map join router catalog fields/properties.
// PC sources:
// - script/missions/citywar_city/zhongzhuan_map/trap.lua
// - script/missions/citywar_city/head.lua
// - script/missions/citywar_city/camper.lua
// - script/missions/citywar_global/head.lua
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionCatalogEntry
    {
        private const int DefaultCityWarDefenderTransferMapId = 222;
        private const int DefaultCityWarMissionMapId = 221;
        private const int DefaultCityWarMissionId = 6;
        private const int DefaultCityWarMissionStateVar = 1;
        private const int DefaultCityWarMissionKeyVar = 99;
        private const int DefaultCityWarTaskId = 230;
        private const int DefaultCityWarTaskValueId = 231;
        private const int DefaultCityWarTaskKeyId = 232;
        private const int DefaultCityWarTaskCityId = 233;
        private const int DefaultCityWarJoinStateTempTaskId = 242;
        private const int DefaultCityWarJoinLockTempTaskId = 200;

        private static readonly int[] DefaultCityWarCardItemIds =
        {
            363, 362, 355, 354, 367, 366, 359,
            358, 357, 356, 365, 364, 361, 360
        };

        public int cityWarDefenderTransferMapId;
        public int cityWarMissionMapId;
        public int cityWarMissionId;
        public int cityWarMissionStateVar;
        public int cityWarMissionKeyVar;
        public int cityWarTaskId;
        public int cityWarTaskValueId;
        public int cityWarTaskKeyId;
        public int cityWarTaskCityId;
        public int cityWarJoinStateTempTaskId;
        public int cityWarJoinLockTempTaskId;
        public int[] cityWarCardItemIds;
        public string cityWarWaitingMessage;
        public string cityWarNoCardMessage;

        public bool IsCityWarJoinRouter => string.Equals(actionKind, "CityWarJoinRouter", StringComparison.OrdinalIgnoreCase);

        public int CityWarDefenderTransferMapId => cityWarDefenderTransferMapId > 0 ? cityWarDefenderTransferMapId : DefaultCityWarDefenderTransferMapId;
        public int CityWarMissionMapId => cityWarMissionMapId > 0 ? cityWarMissionMapId : (targetMapId > 0 ? targetMapId : DefaultCityWarMissionMapId);
        public int CityWarMissionId => cityWarMissionId > 0 ? cityWarMissionId : DefaultCityWarMissionId;
        public int CityWarMissionStateVar => cityWarMissionStateVar > 0 ? cityWarMissionStateVar : (missionStateVar > 0 ? missionStateVar : DefaultCityWarMissionStateVar);
        public int CityWarMissionKeyVar => cityWarMissionKeyVar > 0 ? cityWarMissionKeyVar : DefaultCityWarMissionKeyVar;
        public int CityWarTaskId => cityWarTaskId > 0 ? cityWarTaskId : DefaultCityWarTaskId;
        public int CityWarTaskValueId => cityWarTaskValueId > 0 ? cityWarTaskValueId : DefaultCityWarTaskValueId;
        public int CityWarTaskKeyId => cityWarTaskKeyId > 0 ? cityWarTaskKeyId : DefaultCityWarTaskKeyId;
        public int CityWarTaskCityId => cityWarTaskCityId > 0 ? cityWarTaskCityId : DefaultCityWarTaskCityId;
        public int CityWarJoinStateTempTaskId => cityWarJoinStateTempTaskId > 0 ? cityWarJoinStateTempTaskId : DefaultCityWarJoinStateTempTaskId;
        public int CityWarJoinLockTempTaskId => cityWarJoinLockTempTaskId > 0 ? cityWarJoinLockTempTaskId : DefaultCityWarJoinLockTempTaskId;
        public string CityWarWaitingMessage => string.IsNullOrEmpty(cityWarWaitingMessage) ? "Phe ta hiện đang tập hợp chuẩn bị vào đấu trường! Xin mọi người hãy bình tĩnh, chuẩn bị tinh thần!" : cityWarWaitingMessage;
        public string CityWarNoCardMessage => string.IsNullOrEmpty(cityWarNoCardMessage) ? "Ngươi không có lệnh bài làm sao vào được! Đi đi!" : cityWarNoCardMessage;

        public int CityWarRouteCamp(int currentMapId)
            => currentMapId == CityWarDefenderTransferMapId ? 1 : 2;

        public int CityWarCampCellX(int camp)
            => camp == 1 ? (enterCellX > 0 ? enterCellX : 1533) : (exitCellX > 0 ? exitCellX : 1903);

        public int CityWarCampCellY(int camp)
            => camp == 1 ? (enterCellY > 0 ? enterCellY : 3211) : (exitCellY > 0 ? exitCellY : 3608);

        public Vector2 CityWarCampWorldPosition(int camp)
            => CellToWorld(CityWarCampCellX(camp), CityWarCampCellY(camp));

        public Vector2 CityWarOuterWorldPosition()
            => CellToWorld(blockedCellX > 0 ? blockedCellX : 1613, blockedCellY > 0 ? blockedCellY : 3185);

        public int CityWarCardItemId(int cityId, bool oddCard)
        {
            var cards = cityWarCardItemIds != null && cityWarCardItemIds.Length >= 14
                ? cityWarCardItemIds
                : DefaultCityWarCardItemIds;
            int index = (cityId - 1) * 2 + (oddCard ? 0 : 1);
            return cityId >= 1 && index >= 0 && index < cards.Length ? cards[index] : 0;
        }
    }
}
