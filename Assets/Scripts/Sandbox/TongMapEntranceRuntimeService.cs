// -----------------------------------------------------------------------------
// VLTK Mobile — pure Tong map entrance runtime semantics.
// PC source: Server 6.0/.../server1/script/tong/map/entrance_trap.lua
// PC source: Server 6.0/.../gateway/s3relay/script/tong/addtongnpc.lua
// PC source: Server 6.0/.../server1/script/tong/workshop/tongcolltask.lua
// PC source: Server 6.0/.../server1/script/tong/map/map_management.lua
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Standalone model of PC <c>script/tong/map/entrance_trap.lua</c> decisions.
    /// It deliberately returns a deterministic plan instead of touching host APIs.
    /// </summary>
    public sealed class TongMapEntranceRuntimeService
    {
        public const int TaskLpCountId = 1745;
        public const int BorderMapCopyId = 591;
        public const int DefaultEnterCellX = 1718;
        public const int DefaultEnterCellY = 3313;
        public const int BorderEnterCellX = 1712;
        public const int BorderEnterCellY = 3330;

        public const string ProductRegionCnIb = "cn_ib";

        public const string BanWithTaskMessage =
            "Cấm địa bang hội, không được bước vào! Nếu có nhiệm vụ lệnh bài xin hãy tìm xa phu bang hội đối thoại.";

        public const string BanMessage =
            "Cấm địa bang hội, người không được phép không thể bước vào!";

        public const string ExpiredMessage =
            "Khu vực bang hội đã quá thời hạn sử dụng!";

        private static readonly string[] HostApiGaps =
        {
            "GetProductRegion",
            "GetMapType(SubWorld)",
            "GetMapParam(SubWorld, 0)",
            "GetTongName",
            "TONG_GetTongMapBan",
            "TONG_GetTongMapTemplate",
            "tongmap_check_expire",
            "tongmap_get_expire_date",
            "GetTask(TASK_LP_COUNT)",
            "SubWorldIdx2MapCopy(SubWorld)",
            "SetFightState/SetPos/Say/Msg2Player host wiring"
        };

        public TongMapEntrancePlan Evaluate(TongMapEntranceRequest request)
        {
            var plan = new TongMapEntrancePlan();
            plan.TaskLpCountId = TaskLpCountId;
            plan.ProductRegion = request.ProductRegion ?? string.Empty;
            plan.Branch = IsCnIb(request.ProductRegion) ? "cn_ib" : "default";
            plan.MapType = request.MapType;
            plan.MapTongId = request.MapTongId;
            plan.PlayerTongId = request.PlayerTongId;
            plan.RemainingHostApiGaps.AddRange(HostApiGaps);

            if (request.MapType != 1)
            {
                plan.Decision = "not-tong-map";
                return plan;
            }

            if (request.MapTongId == 0)
            {
                plan.Decision = "map-has-no-tong-owner";
                return plan;
            }

            if (IsCnIb(request.ProductRegion))
                EvaluateCnIb(request, plan);
            else
                EvaluateDefault(request, plan);

            return plan;
        }

        public static TongMapEntranceCell GetMapEnterPos(int mapCopyId)
        {
            return mapCopyId == BorderMapCopyId
                ? new TongMapEntranceCell(BorderEnterCellX, BorderEnterCellY)
                : new TongMapEntranceCell(DefaultEnterCellX, DefaultEnterCellY);
        }

        private static void EvaluateDefault(TongMapEntranceRequest request, TongMapEntrancePlan plan)
        {
            if (request.PlayerTongId != request.MapTongId && request.MapBan == 1)
            {
                var pos = GetMapEnterPos(request.CurrentMapCopyId);
                plan.Decision = "default-banned-non-owner";
                plan.Actions.Add(TongMapEntranceAction.SetPos(pos));
                plan.Actions.Add(TongMapEntranceAction.PostMessage(BanMessageForTask(request.TaskLpCountValue)));
                return;
            }

            plan.Decision = "default-allowed";
        }

        private static void EvaluateCnIb(TongMapEntranceRequest request, TongMapEntrancePlan plan)
        {
            bool forbidden = false;
            string forbiddenReason = null;

            if (request.ExpireState == 2)
            {
                forbidden = true;
                forbiddenReason = "cn_ib-expired";
                plan.Actions.Add(TongMapEntranceAction.PostMessage(ExpiredMessage));
            }
            else if (request.ExpireState == 1 && request.PlayerTongId == request.MapTongId && request.NoExpireWarning != 1)
            {
                plan.Decision = "cn_ib-near-expiry-owner-warning";
                plan.Actions.Add(TongMapEntranceAction.PostMessage(NearExpiryMessage(request.ExpireDateText)));
                return;
            }
            else if (request.PlayerTongId != request.MapTongId && request.MapBan == 1)
            {
                forbidden = true;
                forbiddenReason = "cn_ib-banned-non-owner";
                plan.Actions.Add(TongMapEntranceAction.PostMessage(BanMessageForTask(request.TaskLpCountValue)));
            }

            if (forbidden)
            {
                var pos = GetMapEnterPos(request.TemplateMapId);
                plan.Decision = forbiddenReason;
                plan.Actions.Add(TongMapEntranceAction.SetFightState(0));
                plan.Actions.Add(TongMapEntranceAction.SetPos(pos));
                return;
            }

            plan.Decision = "cn_ib-allowed";
        }

        private static bool IsCnIb(string productRegion)
        {
            return string.Equals(productRegion, ProductRegionCnIb, StringComparison.OrdinalIgnoreCase);
        }

        private static string BanMessageForTask(int taskLpCountValue)
        {
            return taskLpCountValue > 0 ? BanWithTaskMessage : BanMessage;
        }

        private static string NearExpiryMessage(string expireDateText)
        {
            var date = string.IsNullOrWhiteSpace(expireDateText) ? "không rõ" : expireDateText;
            return "Khu vực bang hội của quý bang đã sắp đến kỳ hạn " + date + "!";
        }

        // -----------------------------------------------------------------
        // Host-driven entry (PC source: faction_map.txt 33 rows + script/tong/
        // addtongnpc.lua + tong_mix.lua level-10 gate). This wraps the plan-mode
        // Evaluate() and dispatches the resulting actions to ITongMapHost.
        // -----------------------------------------------------------------
        private readonly ITongMapHost _host;

        public TongMapEntranceRuntimeService() : this(null) { }

        public TongMapEntranceRuntimeService(ITongMapHost host)
        {
            _host = host;
        }

        /// <summary>
        /// Quyết định nhập cảnh theo host state. Returns (allowed, reasonVi).
        /// Reason taxonomy:
        ///   - PublicMap: map không phải Tong map.
        ///   - Owner: player thuộc tong đang sở hữu map.
        ///   - Banned: tong bị cấm (PC TONG_GetTongMapBan).
        ///   - Expired: thời hạn sở hữu đã hết (PC tongmap_check_expire).
        ///   - LevelTooLow: không đạt yêu cầu level (PC tong_mix.lua level-10).
        ///   - Allowed: đủ điều kiện.
        /// </summary>
        public TongMapEnterDecision CanPlayerEnter(int mapId, string player, int level, int tongId, long now)
        {
            if (_host == null) return new TongMapEnterDecision(false, "NoHost");

            // Map công cộng (owner = 0) hoặc map không phải Tong map.
            int owner = _host.GetTongOwner(mapId);
            if (owner == 0) return new TongMapEnterDecision(true, "PublicMap");

            // Owner tong → được vào.
            if (owner == tongId && _host.IsPlayerInTong(player, tongId))
                return new TongMapEnterDecision(true, "Owner");

            // Banned (PC TONG_GetTongMapBan).
            if (_host.IsTongBanned(tongId, mapId))
                return new TongMapEnterDecision(false, "Banned");

            // Expired (PC tongmap_check_expire + tongmap_get_expire_date).
            long expire = _host.GetTongExpireTime(tongId, mapId);
            if (expire > 0 && now > expire)
                return new TongMapEnterDecision(false, "Expired");

            // Level gate (PC tong_mix.lua: minimum level 10 to enter Tong map).
            if (!_host.CanEnterTongMap(mapId, level, tongId))
                return new TongMapEnterDecision(false, "LevelTooLow");

            return new TongMapEnterDecision(true, "Allowed");
        }

        /// <summary>
        /// Thực thi nhập cảnh: kiểm tra CanPlayerEnter, nếu cho phép gọi
        /// host.SetPos + host.SetFightState. Nếu từ chối chỉ gọi host.SendMessage.
        /// Returns true nếu cho phép (đã dispatch SetPos+SetFightState),
        /// false nếu từ chối (đã dispatch SendMessage).
        /// </summary>
        public bool EnterTongMap(int mapId, string player, int level, int tongId,
            int x, int y, bool fighting, long now)
        {
            var decision = CanPlayerEnter(mapId, player, level, tongId, now);
            if (!decision.Allowed)
            {
                string denyMsg = DenyMessage(decision.ReasonVi);
                _host?.SendMessage(player, denyMsg);
                return false;
            }

            _host?.SetPos(player, x, y);
            _host?.SetFightState(player, fighting);
            return true;
        }

        private static string DenyMessage(string reason)
        {
            switch (reason)
            {
                case "Banned": return BanMessage;
                case "Expired": return ExpiredMessage;
                case "LevelTooLow": return "Cấp độ chưa đủ để vào khu vực bang hội!";
                case "NoHost": return "Host không khả dụng.";
                default: return "Không thể vào khu vực bang hội!";
            }
        }
    }

    public sealed class TongMapEntranceRequest
    {
        public string ProductRegion;
        public int MapType;
        public int MapTongId;
        public int PlayerTongId;
        public int MapBan;
        public int CurrentMapCopyId;
        public int TemplateMapId;
        public int ExpireState;
        public int NoExpireWarning;
        public int TaskLpCountValue;
        public string ExpireDateText;
    }

    public sealed class TongMapEntrancePlan
    {
        public int TaskLpCountId;
        public string ProductRegion;
        public string Branch;
        public int MapType;
        public int MapTongId;
        public int PlayerTongId;
        public string Decision;
        public readonly List<TongMapEntranceAction> Actions = new List<TongMapEntranceAction>();
        public readonly List<string> RemainingHostApiGaps = new List<string>();
    }

    public sealed class TongMapEntranceAction
    {
        public string Kind;
        public TongMapEntranceCell Position;
        public int FightState;
        public string Message;

        public static TongMapEntranceAction SetPos(TongMapEntranceCell position)
        {
            return new TongMapEntranceAction { Kind = "SetPos", Position = position };
        }

        public static TongMapEntranceAction SetFightState(int fightState)
        {
            return new TongMapEntranceAction { Kind = "SetFightState", FightState = fightState };
        }

        public static TongMapEntranceAction PostMessage(string message)
        {
            return new TongMapEntranceAction { Kind = "PostMessage", Message = message };
        }
    }

    public struct TongMapEntranceCell : IEquatable<TongMapEntranceCell>
    {
        public readonly int X;
        public readonly int Y;

        public TongMapEntranceCell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool Equals(TongMapEntranceCell other)
        {
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            return obj is TongMapEntranceCell other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked { return (X * 397) ^ Y; }
        }

        public override string ToString()
        {
            return X + "," + Y;
        }
    }
}
