// -----------------------------------------------------------------------------
// VLTK Mobile — Network Message DTOs
// 40+ struct DTOs + OpCodes cho toàn bộ hệ thống PC JX Online.
// Mỗi message map 1-1 với một OpCode (xem OpCodes class cuối file).
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Network
{
    // ════════════════════════════════════════════════════════════════════════
    // Player / NPC movement & state
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Vị trí + hướng player gửi lên server mỗi tick.</summary>
    [Serializable]
    public struct PlayerPositionMsg
    {
        public int playerId;
        public float x;
        public float y;
        public float z;
        public int direction;
    }

    /// <summary>Action (skill, dùng item, nhảy) do player phát động.</summary>
    [Serializable]
    public struct PlayerActionMsg
    {
        public int playerId;
        public int skillId;
        public int targetId;
    }

    /// <summary>Trạng thái tổng quát player: máu, mana, thể lực, cấp, kinh nghiệm.</summary>
    [Serializable]
    public struct PlayerStateMsg
    {
        public int playerId;
        public int hp;
        public int mp;
        public int stamina;
        public int level;
        public int exp;
        public int state;
        public float x;
        public float y;
        public float z;
        public int direction;
    }

    /// <summary>Thông báo player vừa lên cấp + kinh nghiệm nhận được.</summary>
    [Serializable]
    public struct PlayerLevelUpMsg
    {
        public int playerId;
        public int newLevel;
        public int expGained;
    }

    /// <summary>Player mới vào map (broadcast cho client khác).</summary>
    [Serializable]
    public struct PlayerJoinMsg
    {
        public int playerId;
        public string playerName;
        public int sectId;
    }

    /// <summary>Player rời map (đăng xuất, chuyển map, kick).</summary>
    [Serializable]
    public struct PlayerLeaveMsg
    {
        public int playerId;
    }

    // ════════════════════════════════════════════════════════════════════════
    // NPC sync / movement
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Sync HP/state NPC định kỳ.</summary>
    [Serializable]
    public struct NpcSyncMsg
    {
        public int npcId;
        public int hp;
        public int state;
    }

    /// <summary>Di chuyển NPC (quái vật, NPC quest, pet) theo tick.</summary>
    [Serializable]
    public struct NpcMoveMsg
    {
        public int npcId;
        public float x;
        public float y;
        public float z;
        public int direction;
        public int speed;
    }

    /// <summary>NPC tấn công mục tiêu (skill + damage + crit).</summary>
    [Serializable]
    public struct NpcAttackMsg
    {
        public int npcId;
        public int targetId;
        public int damage;
        public int skillId;
        public int hp;
        public bool isCrit;
    }

    /// <summary>Server thông báo NPC spawn vào map.</summary>
    [Serializable]
    public struct NpcSpawnMsg
    {
        public int npcId;
        public int templateId;
        public int mapId;
        public float x;
        public float y;
        public int level;
        public int camp;
    }

    /// <summary>NPC biến mất (chết, hết respawn window, GM xóa).</summary>
    [Serializable]
    public struct NpcDespawnMsg
    {
        public int npcId;
        public int reason;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Combat
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Player/NPC thi triển chiêu thức lên mục tiêu/vị trí.</summary>
    [Serializable]
    public struct SkillCastMsg
    {
        public int casterId;
        public int skillId;
        public int targetId;
        public float targetX;
        public float targetY;
        public int level;
    }

    /// <summary>Damage gây ra từ attacker → victim (kèm crit, miss, loại dmg).</summary>
    [Serializable]
    public struct DamageMsg
    {
        public int attackerId;
        public int victimId;
        public int damage;
        public int damageType;
        public bool isMiss;
        public bool isCrit;
    }

    /// <summary>Hồi máu / nội lực / thể lực (skill, thuốc, buff).</summary>
    [Serializable]
    public struct HealMsg
    {
        public int casterId;
        public int targetId;
        public int amount;
        public int healType;
    }

    /// <summary>Áp dụng buff/debuff lên target (kèm duration ms và nguồn).</summary>
    [Serializable]
    public struct BuffApplyMsg
    {
        public int targetId;
        public int buffId;
        public int durationMs;
        public int sourceId;
    }

    /// <summary>Gỡ buff khỏi target (hết hạn, dispel, tử vong).</summary>
    [Serializable]
    public struct BuffRemoveMsg
    {
        public int targetId;
        public int buffId;
    }

    /// <summary>Victim tử vong (chỉ định killer, exp mất, vật phẩm rơi).</summary>
    [Serializable]
    public struct DeathMsg
    {
        public int victimId;
        public int killerId;
        public int expLoss;
        public int itemDrop;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Item / Inventory
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Nhận vật phẩm từ quái, quest, GM, hoặc thương nhân.</summary>
    [Serializable]
    public struct ItemGainMsg
    {
        public int playerId;
        public int itemId;
        public int count;
        public int source;
    }

    /// <summary>Dùng vật phẩm (thuốc, cuộn TP, đan dược) lên mục tiêu.</summary>
    [Serializable]
    public struct ItemUseMsg
    {
        public int playerId;
        public int itemId;
        public int targetId;
        public bool success;
    }

    /// <summary>Vứt vật phẩm ra đất (drop bag tạm).</summary>
    [Serializable]
    public struct ItemDropMsg
    {
        public int playerId;
        public int itemId;
        public int count;
        public float x;
        public float y;
    }

    /// <summary>Trang bị / thay trang bị vào slot (lưu cả item cũ để swap).</summary>
    [Serializable]
    public struct ItemEquipMsg
    {
        public int playerId;
        public int slot;
        public int itemId;
        public int oldItemId;
    }

    /// <summary>Túi đồ đầy — gửi item id gây đầy để client xử lý.</summary>
    [Serializable]
    public struct InventoryFullMsg
    {
        public int playerId;
        public int itemId;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Quest / Task
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Player nhận nhiệm vụ mới.</summary>
    [Serializable]
    public struct TaskAcceptMsg
    {
        public int playerId;
        public int taskId;
    }

    /// <summary>Tiến độ nhiệm vụ cập nhật (giết quái, thu thập, đối thoại).</summary>
    [Serializable]
    public struct TaskProgressMsg
    {
        public int playerId;
        public int taskId;
        public int progress;
        public int target;
    }

    /// <summary>Hoàn thành nhiệm vụ + phần thưởng.</summary>
    [Serializable]
    public struct TaskCompleteMsg
    {
        public int playerId;
        public int taskId;
        public int rewardItemId;
        public int rewardCount;
    }

    /// <summary>Nhiệm vụ thất bại (hết giờ, chết, hủy).</summary>
    [Serializable]
    public struct TaskFailMsg
    {
        public int playerId;
        public int taskId;
        public int reason;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Team
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Mời vào tổ đội.</summary>
    [Serializable]
    public struct TeamInviteMsg
    {
        public int fromId;
        public int toId;
    }

    /// <summary>Player đồng ý gia nhập tổ đội.</summary>
    [Serializable]
    public struct TeamJoinMsg
    {
        public int teamId;
        public int playerId;
    }

    /// <summary>Rời tổ đội (rời chủ động, kick, tan nhóm).</summary>
    [Serializable]
    public struct TeamLeaveMsg
    {
        public int teamId;
        public int playerId;
        public int reason;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Guild
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Bang hội mới thành lập (server broadcast).</summary>
    [Serializable]
    public struct GuildCreateMsg
    {
        public int playerId;
        public string guildName;
        public int guildId;
    }

    /// <summary>Thành viên gia nhập bang hội.</summary>
    [Serializable]
    public struct GuildJoinMsg
    {
        public int guildId;
        public int playerId;
        public int rank;
    }

    /// <summary>Bang chủ / trưởng lão trục xuất thành viên.</summary>
    [Serializable]
    public struct GuildKickMsg
    {
        public int guildId;
        public int playerId;
        public int byPlayerId;
    }

    /// <summary>Đóng góp quỹ bang (bạc, đồng, vật phẩm).</summary>
    [Serializable]
    public struct GuildDonateMsg
    {
        public int guildId;
        public int playerId;
        public int amount;
        public int currency;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Trade / Auction
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Mời giao dịch trực tiếp 1-1.</summary>
    [Serializable]
    public struct TradeRequestMsg
    {
        public int fromId;
        public int toId;
    }

    /// <summary>Đặt vật phẩm vào khay giao dịch.</summary>
    [Serializable]
    public struct TradeAddItemMsg
    {
        public int tradeId;
        public int itemId;
        public int count;
    }

    /// <summary>Xác nhận/không đồng ý giao dịch.</summary>
    [Serializable]
    public struct TradeConfirmMsg
    {
        public int tradeId;
        public int playerId;
        public bool accept;
    }

    /// <summary>Đặt giá đấu giá.</summary>
    [Serializable]
    public struct AuctionBidMsg
    {
        public int playerId;
        public int auctionId;
        public int bidAmount;
    }

    /// <summary>Thắng đấu giá (nhận vật phẩm + trừ tiền).</summary>
    [Serializable]
    public struct AuctionWinMsg
    {
        public int playerId;
        public int auctionId;
        public int itemId;
        public int finalPrice;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Chat
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Chat đa kênh: 0=thế giới, 1=tổ đội, 2=bang, 3=mật, 4=hệ thống.</summary>
    [Serializable]
    public struct ChatChannelMsg
    {
        public int playerId;
        public int channel;
        public string message;
        public int targetId;
    }

    /// <summary>Biểu cảm (emote/icon) gửi tới người chơi khác.</summary>
    [Serializable]
    public struct ChatEmoteMsg
    {
        public int playerId;
        public int emoteId;
        public int targetId;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Map / Teleport
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Chuyển bản đồ thành công (kèm tọa độ điểm xuất phát).</summary>
    [Serializable]
    public struct MapChangeMsg
    {
        public int playerId;
        public int fromMapId;
        public int toMapId;
        public float x;
        public float y;
    }

    /// <summary>Yêu cầu dịch chuyển bằng cuộn TP / NPC.</summary>
    [Serializable]
    public struct TeleportRequestMsg
    {
        public int playerId;
        public int targetMapId;
        public int scrollId;
    }

    /// <summary>Hồi sinh tại điểm chỉ định (kèm % HP phục hồi).</summary>
    [Serializable]
    public struct ReviveMsg
    {
        public int playerId;
        public int mapId;
        public float x;
        public float y;
        public int hpPercent;
    }

    // ════════════════════════════════════════════════════════════════════════
    // Battle / System events
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>Điểm Tống - Kim cập nhật theo tick (battlefield realtime).</summary>
    [Serializable]
    public struct TongJinScoreMsg
    {
        public int songScore;
        public int jinScore;
        public int timeLeftSec;
    }

    /// <summary>Trạng thái công thành chiến (chủ nhân thành + thời gian còn lại).</summary>
    [Serializable]
    public struct CityWarStateMsg
    {
        public int cityId;
        public int ownerTongId;
        public int state;
        public int timeLeftSec;
    }

    /// <summary>Boss hoàng kim vừa xuất hiện (kèm despawn time).</summary>
    [Serializable]
    public struct BossSpawnMsg
    {
        public int bossId;
        public int mapId;
        public float x;
        public float y;
        public int despawnTime;
    }

    // ════════════════════════════════════════════════════════════════════════
    // OpCodes — map ushort → message struct (dùng cho binary frame header).
    // ════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Bảng OpCodes dùng cho mọi gói tin client ↔ server. Phân nhóm 1000-11003.
    /// Phía client dùng MessageRouter để lookup Type theo opCode.
    /// </summary>
    public static class OpCodes
    {
        // Player
        public const ushort PlayerPosition = 1001;
        public const ushort PlayerAction = 1002;
        public const ushort PlayerState = 1003;
        public const ushort PlayerLevelUp = 1004;
        public const ushort PlayerJoin = 1005;
        public const ushort PlayerLeave = 1006;

        // NPC
        public const ushort NpcSync = 2001;
        public const ushort NpcMove = 2002;
        public const ushort NpcAttack = 2003;
        public const ushort NpcSpawn = 2004;
        public const ushort NpcDespawn = 2005;

        // Combat
        public const ushort SkillCast = 3001;
        public const ushort Damage = 3002;
        public const ushort Heal = 3003;
        public const ushort BuffApply = 3004;
        public const ushort BuffRemove = 3005;
        public const ushort Death = 3006;

        // Item
        public const ushort ItemGain = 4001;
        public const ushort ItemUse = 4002;
        public const ushort ItemDrop = 4003;
        public const ushort ItemEquip = 4004;
        public const ushort InventoryFull = 4005;

        // Task
        public const ushort TaskAccept = 5001;
        public const ushort TaskProgress = 5002;
        public const ushort TaskComplete = 5003;
        public const ushort TaskFail = 5004;

        // Team
        public const ushort TeamInvite = 6001;
        public const ushort TeamJoin = 6002;
        public const ushort TeamLeave = 6003;

        // Guild
        public const ushort GuildCreate = 7001;
        public const ushort GuildJoin = 7002;
        public const ushort GuildKick = 7003;
        public const ushort GuildDonate = 7004;

        // Trade / Auction
        public const ushort TradeRequest = 8001;
        public const ushort TradeAddItem = 8002;
        public const ushort TradeConfirm = 8003;
        public const ushort AuctionBid = 8004;
        public const ushort AuctionWin = 8005;

        // Chat
        public const ushort Chat = 9001;
        public const ushort ChatEmote = 9002;

        // Map
        public const ushort MapChange = 10001;
        public const ushort TeleportRequest = 10002;
        public const ushort Revive = 10003;

        // System
        public const ushort TongJinScore = 11001;
        public const ushort CityWarState = 11002;
        public const ushort BossSpawn = 11003;
    }
}
