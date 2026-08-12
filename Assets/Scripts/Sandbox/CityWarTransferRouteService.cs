// -----------------------------------------------------------------------------
// VLTK Mobile — pure CityWar NPC transfer-route split proof.
// PC source: 00.src-tinh-kiem Server 6.0/server/home_jxser/server1/script/missions/
// - citywar_global/infocenter.lua:209-247 (GoCityWarDefend/Attack -> 222/223)
// - citywar_city/zhongzhuan_map/trap.lua:25-38 (222 -> camp 1, else camp 2)
// - citywar_city/head.lua:71-72 (join map 221 camp spawns; handled by JoinRouter)
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class CityWarTransferRouteService
    {
        public const int MissionId = CityWarJoinRouterRuntimeService.MissionId;
        public const int MissionMapId = CityWarJoinRouterRuntimeService.MissionMapId;
        public const int DefenderTransferMapId = CityWarJoinRouterRuntimeService.DefenderTransferMapId;
        public const int AttackerTransferMapId = 223;
        public const int DefenderCamp = CityWarJoinRouterRuntimeService.DefenderCamp;
        public const int AttackerCamp = CityWarJoinRouterRuntimeService.AttackerCamp;
        public const string RejectReason = "identity/card/task fallback did not match PC CityWar side";

        public const int TransferPointAX = 1614;
        public const int TransferPointAY = 3172;
        public const int TransferPointBX = 1629;
        public const int TransferPointBY = 3193;

        public CityWarTransferRoute BuildNpcRoute(CityWarTransferRouteInput input, CityWarCardSide requestedSide)
        {
            var route = new CityWarTransferRoute(requestedSide);
            if (input == null)
                return route.Reject("input unavailable");
            if (input.CityId < 1 || input.CityId > 7)
                return route.Reject("Ctc3tru_WhichWarBegin()==0");
            if (requestedSide != CityWarCardSide.Defender && requestedSide != CityWarCardSide.Attacker)
                return route.Reject("unsupported CityWar side");

            int requiredCamp = requestedSide == CityWarCardSide.Defender ? DefenderCamp : AttackerCamp;
            string requiredTong = requestedSide == CityWarCardSide.Defender ? input.DefenderTongName : input.AttackerTongName;
            int requiredCard = CityWarPcConstants.GetCardItemIdForCity(input.CityId, requestedSide);

            bool tongMatches = !string.IsNullOrEmpty(input.TongName) && string.Equals(input.TongName, requiredTong, StringComparison.Ordinal);
            bool cardMatches = input.GetItemCount(requiredCard) >= 1;
            bool taskFallbackMatches = input.TaskCityId == input.CityId && input.TaskValue == requiredCamp && input.TaskId == MissionId;

            if (!tongMatches && !cardMatches && !taskFallbackMatches)
                return route.Reject(RejectReason);

            int transferMapId = requestedSide == CityWarCardSide.Defender ? DefenderTransferMapId : AttackerTransferMapId;
            route.Accept(transferMapId, tongMatches, cardMatches, taskFallbackMatches);
            return route;
        }

        public static int RouteCampFromTransferMap(int currentMapId)
        {
            return currentMapId == DefenderTransferMapId ? DefenderCamp : AttackerCamp;
        }
    }

    public sealed class CityWarTransferRouteInput
    {
        public int CityId;
        public string TongName;
        public string DefenderTongName;
        public string AttackerTongName;
        public int TaskCityId;
        public int TaskValue;
        public int TaskId;
        public readonly Dictionary<int, int> ItemCounts = new Dictionary<int, int>();

        public int GetItemCount(int itemId)
        {
            if (itemId <= 0)
                return 0;
            int count;
            return ItemCounts.TryGetValue(itemId, out count) ? count : 0;
        }
    }

    public sealed class CityWarTransferRoute
    {
        public readonly CityWarCardSide RequestedSide;
        public bool Accepted;
        public string FailureReason;
        public int TransferMapId;
        public int RouteCamp;
        public bool MatchedTong;
        public bool MatchedCard;
        public bool MatchedTaskFallback;
        public readonly List<CityWarCell> PossibleNewWorlds = new List<CityWarCell>();

        public CityWarTransferRoute(CityWarCardSide requestedSide)
        {
            RequestedSide = requestedSide;
        }

        public CityWarTransferRoute Reject(string reason)
        {
            Accepted = false;
            FailureReason = reason;
            return this;
        }

        public void Accept(int transferMapId, bool matchedTong, bool matchedCard, bool matchedTaskFallback)
        {
            Accepted = true;
            FailureReason = null;
            TransferMapId = transferMapId;
            RouteCamp = CityWarTransferRouteService.RouteCampFromTransferMap(transferMapId);
            MatchedTong = matchedTong;
            MatchedCard = matchedCard;
            MatchedTaskFallback = matchedTaskFallback;
            PossibleNewWorlds.Add(new CityWarCell(transferMapId, CityWarTransferRouteService.TransferPointAX, CityWarTransferRouteService.TransferPointAY));
            PossibleNewWorlds.Add(new CityWarCell(transferMapId, CityWarTransferRouteService.TransferPointBX, CityWarTransferRouteService.TransferPointBY));
        }
    }
}
