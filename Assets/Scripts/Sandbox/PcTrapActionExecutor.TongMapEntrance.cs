// -----------------------------------------------------------------------------
// VLTK Mobile — Tong map entrance trap action hook.
// PC source: script/tong/map/entrance_trap.lua.
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed partial class PcTrapActionExecutor
    {
        private const string TongBanWithTaskMessage =
            "Cấm địa bang hội, không được bước vào! Nếu có nhiệm vụ lệnh bài xin hãy tìm xa phu bang hội đối thoại.";

        private const string TongBanMessage =
            "Cấm địa bang hội, người không được phép không thể bước vào!";

        private const string TongExpiredMessage =
            "Khu vực bang hội đã quá thời hạn sử dụng!";

        private const string TongCatalogGap =
            "catalog-driven; host lacks PC GetProductRegion/GetMapParam/GetTongName/TONG_GetTongMapBan/TONG_GetTongMapTemplate APIs";

        private bool TryExecuteTongMapEntrance(PcTrapActionCatalogEntry action, out TrapActionExecutionResult result)
        {
            result = null;
            if (!action.IsTongMapEntrance) return false;

            if (_host == null)
            {
                result = Failure(action, "trap travel host unavailable");
                return true;
            }

            if (action.tongMapType != 1)
            {
                result = Success(action, $"TongMapEntrance mapType={action.tongMapType} -> no action ({TongCatalogGap})");
                return true;
            }

            int mapTongId = action.tongMapTongId;
            if (mapTongId == 0)
            {
                result = Success(action, $"TongMapEntrance mapTongId=0 -> no action ({TongCatalogGap})");
                return true;
            }

            int playerTongId = ResolveTongPlayerId(action);
            bool isCnIb = string.Equals(action.tongProductRegion, "cn_ib", StringComparison.OrdinalIgnoreCase);
            if (isCnIb)
                ExecuteCnIbTongEntrance(action, mapTongId, playerTongId, out result);
            else
                ExecuteDefaultTongEntrance(action, mapTongId, playerTongId, out result);
            return true;
        }

        private void ExecuteDefaultTongEntrance(PcTrapActionCatalogEntry action, int mapTongId, int playerTongId,
            out TrapActionExecutionResult result)
        {
            if (playerTongId != mapTongId && action.tongMapBan == 1)
            {
                int mapCopyId = action.tongCurrentMapCopyId > 0 ? action.tongCurrentMapCopyId : _host.GetCurrentMapId();
                Vector2 target = action.TongMapEnterWorldPosition(mapCopyId);
                _host.SetPos(target);
                PostTongMessage(GetTongBanMessage(action));
                result = Success(action, $"TongMapEntrance default banned mapCopy={mapCopyId} -> SetPos({target}) ({TongCatalogGap})");
                return;
            }

            result = Success(action, $"TongMapEntrance default allowed -> no action ({TongCatalogGap})");
        }

        private void ExecuteCnIbTongEntrance(PcTrapActionCatalogEntry action, int mapTongId, int playerTongId,
            out TrapActionExecutionResult result)
        {
            bool forbidden = false;
            string reason = "allowed";
            string message = null;

            if (action.tongExpireState == 2)
            {
                forbidden = true;
                reason = "expired";
                message = string.IsNullOrWhiteSpace(action.message) ? TongExpiredMessage : action.message;
            }
            else if (action.tongExpireState == 1 && playerTongId == mapTongId && action.tongNoExpireWarning != 1)
            {
                reason = "expire-warning";
                PostTongMessage(TongExpireWarning(action));
            }
            else if (playerTongId != mapTongId && action.tongMapBan == 1)
            {
                forbidden = true;
                reason = "banned";
                message = GetTongBanMessage(action);
            }

            if (forbidden)
            {
                int templateMapId = action.tongTemplateMapId > 0
                    ? action.tongTemplateMapId
                    : (action.tongCurrentMapCopyId > 0 ? action.tongCurrentMapCopyId : _host.GetCurrentMapId());
                Vector2 target = action.TongMapEnterWorldPosition(templateMapId);
                PostTongMessage(message);
                _host.SetFightState(0);
                _host.SetPos(target);
                result = Success(action, $"TongMapEntrance cn_ib {reason} template={templateMapId} -> SetFightState(0), SetPos({target}) ({TongCatalogGap})");
                return;
            }

            result = Success(action, $"TongMapEntrance cn_ib {reason} -> no action ({TongCatalogGap})");
        }

        private int ResolveTongPlayerId(PcTrapActionCatalogEntry action)
            => action.tongPlayerTongIdTaskId > 0 ? _host.GetTaskValue(action.tongPlayerTongIdTaskId) : action.tongPlayerTongId;

        private int ResolveTongTaskLpCount(PcTrapActionCatalogEntry action)
        {
            int taskId = action.tongTaskLpCountId > 0
                ? action.tongTaskLpCountId
                : PcTrapActionCatalogEntry.TongMapEntranceTaskLpCountId;
            return _host.GetTaskValue(taskId);
        }

        private string GetTongBanMessage(PcTrapActionCatalogEntry action)
        {
            if (action.messages != null && action.messages.Length > 0)
                return ResolveTongTaskLpCount(action) > 0 ? action.messages[0] : action.messages[Math.Min(1, action.messages.Length - 1)];
            if (!string.IsNullOrWhiteSpace(action.message))
                return action.message;
            return ResolveTongTaskLpCount(action) > 0 ? TongBanWithTaskMessage : TongBanMessage;
        }

        private static string TongExpireWarning(PcTrapActionCatalogEntry action)
        {
            string date = string.IsNullOrWhiteSpace(action.tongExpireDate) ? "" : action.tongExpireDate;
            return $"Khu vực bang hội của quý bang đã sắp đến kỳ hạn {date}!";
        }

        private void PostTongMessage(string message)
        {
            if (_sideEffects == null || string.IsNullOrWhiteSpace(message)) return;
            _sideEffects.PostMessage(message);
        }
    }
}
