// -----------------------------------------------------------------------------
// VLTK Mobile — PC map travel action proof service
// Source: Client 6.0/settings/{waypoint.txt, wharf.txt, revivepos.ini, scroll.txt}
// Purpose: deterministic consumer-facing lookup layer over PcMapTravelRuntimeService.
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.Sandbox
{
    public enum PcMapTravelActionKind
    {
        WaypointTeleport,
        WharfTravel,
        DefaultRevive,
        ScrollValue
    }

    public enum PcMapTravelActionStatus
    {
        Ready,
        DataOnly,
        NotFound,
        Unsupported
    }

    public sealed class PcMapTravelActionResult
    {
        public PcMapTravelActionKind Kind { get; set; }
        public PcMapTravelActionStatus Status { get; set; }
        public int SourceId { get; set; }
        public int CurrentMapId { get; set; }
        public int TargetMapId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Value { get; set; }
        public int SectCount { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool HasTeleport => Status == PcMapTravelActionStatus.Ready && TargetMapId > 0;
    }

    public sealed class PcMapTravelActionService
    {
        private readonly PcMapTravelRuntimeService _runtime;

        public PcMapTravelRuntimeService Runtime => _runtime;

        public PcMapTravelActionService(PcMapTravelRuntimeService runtime)
        {
            _runtime = runtime ?? PcMapTravelRuntimeService.Empty();
        }

        public static PcMapTravelActionService LoadFromDirectory(string pcMapDir)
        {
            return new PcMapTravelActionService(PcMapTravelRuntimeService.LoadFromDirectory(pcMapDir));
        }

        public PcMapTravelActionResult ResolveWaypointTeleport(int waypointId)
        {
            var row = _runtime.GetWaypoint(waypointId);
            if (row == null)
                return Missing(PcMapTravelActionKind.WaypointTeleport, waypointId, "Không tìm thấy waypoint PC.");

            return new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.WaypointTeleport,
                Status = PcMapTravelActionStatus.Ready,
                SourceId = row.WaypointId,
                TargetMapId = row.MapId,
                X = row.PosX,
                Y = row.PosY,
                Message = "Waypoint PC có đủ map/x/y để dịch chuyển."
            };
        }

        public PcMapTravelActionResult ResolveWharfTravelByWharfId(int wharfId)
        {
            var row = _runtime.GetWharf(wharfId);
            if (row == null)
                return Missing(PcMapTravelActionKind.WharfTravel, wharfId, "Không tìm thấy bến tàu PC.");

            return WharfDataOnly(row);
        }

        public IReadOnlyList<PcMapTravelActionResult> ResolveWharfTravelFromMap(int fromMapId)
        {
            var results = new List<PcMapTravelActionResult>();
            foreach (var row in _runtime.GetWharfServiceRowsForMap(fromMapId))
                results.Add(WharfDataOnly(row));
            return results;
        }

        public PcMapTravelActionResult ResolveDefaultRevive(int currentMapId)
        {
            var row = _runtime.GetDefaultRevivePosition(currentMapId);
            if (row == null)
                return Missing(PcMapTravelActionKind.DefaultRevive, currentMapId, "Không tìm thấy điểm hồi sinh mặc định PC cho map.");

            return new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.DefaultRevive,
                Status = PcMapTravelActionStatus.Ready,
                SourceId = row.regionIndex,
                CurrentMapId = currentMapId,
                TargetMapId = row.mapId,
                X = row.x,
                Y = row.y,
                SectCount = row.regionEnd - row.regionStart + 1,
                Message = "Điểm hồi sinh PC có đủ map/x/y."
            };
        }

        public PcMapTravelActionResult ResolveScrollValue(int scrollId)
        {
            var row = _runtime.GetScrollValue(scrollId);
            if (row == null)
                return Missing(PcMapTravelActionKind.ScrollValue, scrollId, "Không tìm thấy giá trị scroll PC.");

            return new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.ScrollValue,
                Status = PcMapTravelActionStatus.DataOnly,
                SourceId = row.scrollId,
                Value = row.cost,
                Message = "scroll.txt PC là bảng giá trị hai cột, không phải hàng teleport theo map."
            };
        }

        public PcMapTravelActionResult ResolveScrollTeleportRowsForMap(int currentMapId)
        {
            var rows = _runtime.GetScrollMapRowsForMap(currentMapId);
            if (rows.Count == 0)
            {
                return new PcMapTravelActionResult
                {
                    Kind = PcMapTravelActionKind.ScrollValue,
                    Status = PcMapTravelActionStatus.Unsupported,
                    CurrentMapId = currentMapId,
                    Message = "Không tự tạo teleport: dữ liệu PC hiện tại chỉ chứng minh bảng giá trị scroll."
                };
            }

            var row = rows[0];
            return new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.ScrollValue,
                Status = PcMapTravelActionStatus.Ready,
                SourceId = row.scrollId,
                CurrentMapId = currentMapId,
                TargetMapId = row.mapId,
                Value = row.value,
                Message = "Có hàng scroll theo map từ runtime registry."
            };
        }

        private static PcMapTravelActionResult WharfDataOnly(PcWharfEntry row)
        {
            return new PcMapTravelActionResult
            {
                Kind = PcMapTravelActionKind.WharfTravel,
                Status = PcMapTravelActionStatus.DataOnly,
                SourceId = row.WharfId,
                CurrentMapId = row.FromMapId,
                X = row.PosX,
                Y = row.PosY,
                SectCount = row.SectCount,
                Message = "wharf.txt PC giữ được số SECT/vị trí, nhưng runtime hiện chưa có danh sách đích nên không tự tạo teleport."
            };
        }

        private static PcMapTravelActionResult Missing(PcMapTravelActionKind kind, int sourceId, string message)
        {
            return new PcMapTravelActionResult
            {
                Kind = kind,
                Status = PcMapTravelActionStatus.NotFound,
                SourceId = sourceId,
                Message = message
            };
        }
    }
}
