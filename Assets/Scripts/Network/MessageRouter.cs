// -----------------------------------------------------------------------------
// VLTK Mobile — Message Router
// Map ushort opCode → Type để runtime deserialize binary frames.
// Dùng bởi INetworkClient implementations: từ header (opCode) → struct Type,
// sau đó JsonUtility.FromJson(payload) để parse.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace VLTK.Network
{
    /// <summary>
    /// Registry tĩnh map opCode → message struct Type. Threadsafe (dùng lock).
    /// Cho phép nhiều module cùng register / lookup ở runtime.
    /// </summary>
    public static class MessageRouter
    {
        private static readonly Dictionary<ushort, Type> _registry = new();
        private static readonly object _lock = new();

        /// <summary>Số opCode hiện đã đăng ký.</summary>
        public static int RegisteredOpCodes
        {
            get
            {
                lock (_lock) return _registry.Count;
            }
        }

        /// <summary>Đăng ký một opCode → Type. Ghi đè nếu đã tồn tại.</summary>
        public static void Register(ushort op, Type msgType)
        {
            if (msgType == null) return;
            lock (_lock) _registry[op] = msgType;
        }

        /// <summary>Tra Type đã đăng ký; trả null nếu chưa biết opCode.</summary>
        public static Type GetMessageType(ushort op)
        {
            lock (_lock) return _registry.TryGetValue(op, out var t) ? t : null;
        }

        /// <summary>Gỡ đăng ký opCode (dùng cho hot-reload/test cleanup).</summary>
        public static bool Unregister(ushort op)
        {
            lock (_lock) return _registry.Remove(op);
        }

        /// <summary>Xóa toàn bộ đăng ký (test cleanup).</summary>
        public static void Clear()
        {
            lock (_lock) _registry.Clear();
        }

        /// <summary>
        /// Auto-register toàn bộ OpCodes ↔ struct types trong VLTK.Network.
        /// Dùng reflection 1 lần lúc khởi động client.
        /// </summary>
        public static void RegisterDefaults()
        {
            Register(OpCodes.PlayerPosition, typeof(PlayerPositionMsg));
            Register(OpCodes.PlayerAction, typeof(PlayerActionMsg));
            Register(OpCodes.PlayerState, typeof(PlayerStateMsg));
            Register(OpCodes.PlayerLevelUp, typeof(PlayerLevelUpMsg));
            Register(OpCodes.PlayerJoin, typeof(PlayerJoinMsg));
            Register(OpCodes.PlayerLeave, typeof(PlayerLeaveMsg));

            Register(OpCodes.NpcSync, typeof(NpcSyncMsg));
            Register(OpCodes.NpcMove, typeof(NpcMoveMsg));
            Register(OpCodes.NpcAttack, typeof(NpcAttackMsg));
            Register(OpCodes.NpcSpawn, typeof(NpcSpawnMsg));
            Register(OpCodes.NpcDespawn, typeof(NpcDespawnMsg));

            Register(OpCodes.SkillCast, typeof(SkillCastMsg));
            Register(OpCodes.Damage, typeof(DamageMsg));
            Register(OpCodes.Heal, typeof(HealMsg));
            Register(OpCodes.BuffApply, typeof(BuffApplyMsg));
            Register(OpCodes.BuffRemove, typeof(BuffRemoveMsg));
            Register(OpCodes.Death, typeof(DeathMsg));

            Register(OpCodes.ItemGain, typeof(ItemGainMsg));
            Register(OpCodes.ItemUse, typeof(ItemUseMsg));
            Register(OpCodes.ItemDrop, typeof(ItemDropMsg));
            Register(OpCodes.ItemEquip, typeof(ItemEquipMsg));
            Register(OpCodes.InventoryFull, typeof(InventoryFullMsg));

            Register(OpCodes.TaskAccept, typeof(TaskAcceptMsg));
            Register(OpCodes.TaskProgress, typeof(TaskProgressMsg));
            Register(OpCodes.TaskComplete, typeof(TaskCompleteMsg));
            Register(OpCodes.TaskFail, typeof(TaskFailMsg));

            Register(OpCodes.TeamInvite, typeof(TeamInviteMsg));
            Register(OpCodes.TeamJoin, typeof(TeamJoinMsg));
            Register(OpCodes.TeamLeave, typeof(TeamLeaveMsg));

            Register(OpCodes.GuildCreate, typeof(GuildCreateMsg));
            Register(OpCodes.GuildJoin, typeof(GuildJoinMsg));
            Register(OpCodes.GuildKick, typeof(GuildKickMsg));
            Register(OpCodes.GuildDonate, typeof(GuildDonateMsg));

            Register(OpCodes.TradeRequest, typeof(TradeRequestMsg));
            Register(OpCodes.TradeAddItem, typeof(TradeAddItemMsg));
            Register(OpCodes.TradeConfirm, typeof(TradeConfirmMsg));
            Register(OpCodes.AuctionBid, typeof(AuctionBidMsg));
            Register(OpCodes.AuctionWin, typeof(AuctionWinMsg));

            Register(OpCodes.Chat, typeof(ChatChannelMsg));
            Register(OpCodes.ChatEmote, typeof(ChatEmoteMsg));

            Register(OpCodes.MapChange, typeof(MapChangeMsg));
            Register(OpCodes.TeleportRequest, typeof(TeleportRequestMsg));
            Register(OpCodes.Revive, typeof(ReviveMsg));

            Register(OpCodes.TongJinScore, typeof(TongJinScoreMsg));
            Register(OpCodes.CityWarState, typeof(CityWarStateMsg));
            Register(OpCodes.BossSpawn, typeof(BossSpawnMsg));
        }
    }
}
