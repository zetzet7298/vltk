// -----------------------------------------------------------------------------
// VLTK Mobile — ITongMapHost interface
// Host-side abstraction cho TongMapEntranceRuntimeService.
// PC source: faction_map.txt (33 rows) + Server 6.0/script/tong/addtongnpc.lua
// + tong_mix.lua level-10 enter gate + script/tong/map/map_management.lua.
//
// Implementations: real SandboxManager runtime, in-memory fake cho tests/GM preview.
// -----------------------------------------------------------------------------

namespace VLTK.Sandbox
{
    /// <summary>
    /// Host-side abstraction cho Tong map entry decisions. Mỗi method tương ứng
    /// 1 PC lua host API (script/tong/map/entrance_trap.lua + map_management.lua).
    /// </summary>
    public interface ITongMapHost
    {
        // --- Membership (PC: TONG_GetTongMapBan / GetTongName / SubWorldIdx2MapCopy) ---

        /// <summary>Tong đang sở hữu map, hoặc 0 nếu map công cộng.</summary>
        int GetTongOwner(int mapId);

        /// <summary>Tong bị cấm khỏi map này (PC TONG_GetTongMapBan).</summary>
        bool IsTongBanned(int tongId, int mapId);

        /// <summary>Thời hạn sở hữu map (giây epoch unix); 0 = vĩnh viễn.</summary>
        long GetTongExpireTime(int tongId, int mapId);

        /// <summary>Player có thuộc tong hay không (PC GetPlayerTong).</summary>
        bool IsPlayerInTong(string player, int tongId);

        // --- Entry (PC: tong_mix.lua level-10 gate + SetFightState/SetPos/Msg2Player) ---

        /// <summary>Map có cho phép player nhập cảnh theo level + tong không.</summary>
        bool CanEnterTongMap(int mapId, int level, int tongId);

        /// <summary>Set trạng thái chiến đấu của player (PC SetFightState).</summary>
        bool SetFightState(string player, bool fighting);

        /// <summary>Di chuyển player tới vị trí (PC SetPos).</summary>
        bool SetPos(string player, int x, int y);

        /// <summary>Gửi message cho player (PC Msg2Player/Say).</summary>
        bool SendMessage(string player, string message);
    }

    /// <summary>
    /// Quyết định nhập cảnh Tong map. PC source: faction_map.txt 33 rows +
    /// script/tong/addtongnpc.lua map arrays + tong_mix.lua level-10 gate.
    /// </summary>
    public readonly struct TongMapEnterDecision
    {
        public readonly bool Allowed;
        public readonly string ReasonVi;

        public TongMapEnterDecision(bool allowed, string reasonVi)
        {
            Allowed = allowed;
            ReasonVi = reasonVi ?? string.Empty;
        }

        public override string ToString()
            => $"{(Allowed ? "ALLOW" : "DENY")}: {ReasonVi}";
    }
}
