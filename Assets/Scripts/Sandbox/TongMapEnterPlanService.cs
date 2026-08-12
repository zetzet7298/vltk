// -----------------------------------------------------------------------------
// VLTK Mobile — pure Tong/faction map enter plan model.
// PC source: Server 6.0/server/home_jxser/server1/script/tong/tong_mix.lua
// PC source: Server 6.0/server/home_jxser/server1/script/tong/addtongnpc.lua
// Uses imported StreamingAssets/Reference/PcTong/faction_map.txt via FactionMapService.
// This file only returns command plans; it does not mutate host map/player state.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public sealed class TongMapEnterPlanService
    {
        public const int RequiredEnterLevel = 10;
        public const int CityAltarNpcTemplateId = 329;
        public const string PcTongMixSource = "Server 6.0/server/home_jxser/server1/script/tong/tong_mix.lua";
        public const string PcAddTongNpcSource = "Server 6.0/server/home_jxser/server1/script/tong/addtongnpc.lua";
        public const string UnderLevelMessage = "Người chơi cấp 10 trở lên mới có thể bước vào lãnh địa bang hội!";

        private readonly FactionMapService _maps;

        public TongMapEnterPlanService(FactionMapService maps)
        {
            _maps = maps ?? new FactionMapService();
        }

        public static TongMapEnterPlanService LoadFromStreamingAssets()
        {
            return new TongMapEnterPlanService(FactionMapService.LoadFromStreamingAssets());
        }

        public int CatalogRowCount { get { return _maps.Count; } }
        public int CityMapCount { get { return _maps.GetBySourceTable("citymap").Count; } }
        public int DynamicTemplateCount { get { return _maps.GetBySourceTable("aDynMapCopyName").Count; } }
        public int CityAltarNpcMapCount { get { return _maps.GetBySourceTable("jijiu_city").Count; } }

        public TongMapEnterPlan BuildEnterPlan(TongMapEnterRequest request)
        {
            request = request ?? new TongMapEnterRequest();
            var plan = new TongMapEnterPlan
            {
                RequestedMapId = request.TargetMapId,
                PlayerLevel = request.PlayerLevel,
                RequiredLevel = RequiredEnterLevel,
                PcTongMixSource = PcTongMixSource,
                PcAddTongNpcSource = PcAddTongNpcSource
            };

            var map = _maps.GetMap(request.TargetMapId);
            if (map == null)
            {
                plan.Decision = TongMapEnterDecision.MissingMap;
                plan.Message = "Không tìm thấy bản đồ bang hội trong catalog PC.";
                return plan;
            }

            plan.Map = map;
            plan.SourceTable = map.sourceTable ?? string.Empty;
            plan.MapKind = map.mapKind ?? string.Empty;
            plan.RequiredLevel = map.requiredLevel > 0 ? map.requiredLevel : RequiredEnterLevel;

            if (request.PlayerLevel < plan.RequiredLevel)
            {
                plan.Decision = TongMapEnterDecision.UnderLevel;
                plan.Message = UnderLevelMessage;
                return plan;
            }

            if (!map.HasEnterPosition)
            {
                plan.Decision = TongMapEnterDecision.MissingEnterPosition;
                plan.Message = "PC row has no GetMapEnterPos/NewWorld enter coordinates.";
                return plan;
            }

            var kind = request.CommandKind == TongMapEnterCommandKind.SetPos
                ? TongMapEnterCommandKind.SetPos
                : TongMapEnterCommandKind.NewWorld;
            plan.Decision = TongMapEnterDecision.Allowed;
            plan.Commands.Add(new TongMapEnterCommand
            {
                Kind = kind,
                TargetMapId = map.mapId,
                X = map.enterX,
                Y = map.enterY
            });
            return plan;
        }

        public TongCityAltarNpcFact GetCityAltarNpcFact(int mapId)
        {
            foreach (var row in _maps.GetBySourceTable("jijiu_city"))
            {
                if (row.mapId == mapId)
                {
                    return new TongCityAltarNpcFact
                    {
                        Found = true,
                        MapId = row.mapId,
                        NpcTemplateId = row.npcTemplateId,
                        X = row.npcX,
                        Y = row.npcY,
                        ScriptRaw = row.npcScriptRaw ?? string.Empty,
                        NameRaw = row.npcNameRaw ?? string.Empty,
                        PcAddTongNpcSource = PcAddTongNpcSource
                    };
                }
            }
            return new TongCityAltarNpcFact { MapId = mapId, PcAddTongNpcSource = PcAddTongNpcSource };
        }
    }

    public enum TongMapEnterDecision { MissingMap, UnderLevel, MissingEnterPosition, Allowed }
    public enum TongMapEnterCommandKind { None, NewWorld, SetPos }

    public sealed class TongMapEnterRequest
    {
        public int TargetMapId;
        public int PlayerLevel;
        public TongMapEnterCommandKind CommandKind = TongMapEnterCommandKind.NewWorld;
    }

    public sealed class TongMapEnterPlan
    {
        public int RequestedMapId;
        public int PlayerLevel;
        public int RequiredLevel;
        public string SourceTable;
        public string MapKind;
        public string Message;
        public string PcTongMixSource;
        public string PcAddTongNpcSource;
        public PcFactionMapEntry Map;
        public TongMapEnterDecision Decision;
        public readonly List<TongMapEnterCommand> Commands = new List<TongMapEnterCommand>();
        public bool IsAllowed { get { return Decision == TongMapEnterDecision.Allowed; } }
    }

    public sealed class TongMapEnterCommand
    {
        public TongMapEnterCommandKind Kind;
        public int TargetMapId;
        public int X;
        public int Y;

        public override string ToString()
        {
            return Kind + "(" + TargetMapId + "," + X + "," + Y + ")";
        }
    }

    public sealed class TongCityAltarNpcFact
    {
        public bool Found;
        public int MapId;
        public int NpcTemplateId;
        public int X;
        public int Y;
        public string ScriptRaw;
        public string NameRaw;
        public string PcAddTongNpcSource;
    }
}
