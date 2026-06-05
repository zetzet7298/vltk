// -----------------------------------------------------------------------------
// VLTK Mobile — lightweight network protocol stubs for editor/test compile.
// -----------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace VLTK.Network
{
    public static class OpCodes
    {
        public const ushort PlayerPosition = 1001;
        public const ushort PlayerState = 1002;
        public const ushort MapChange = 1003;
        public const ushort SkillCast = 3001;
        public const ushort Damage = 3002;
        public const ushort Heal = 3003;
        public const ushort BuffApply = 3004;
        public const ushort Chat = 4001;
        public const ushort ChatEmote = 4002;
        public const ushort GuildCreate = 5001;
        public const ushort TaskComplete = 6001;
        public const ushort TongJinScore = 7001;
        public const ushort BossSpawn = 8001;
        public const ushort Op14 = 9001, Op15 = 9002, Op16 = 9003, Op17 = 9004, Op18 = 9005;
        public const ushort Op19 = 9006, Op20 = 9007, Op21 = 9008, Op22 = 9009, Op23 = 9010;
        public const ushort Op24 = 9011, Op25 = 9012, Op26 = 9013, Op27 = 9014, Op28 = 9015;
        public const ushort Op29 = 9016, Op30 = 9017, Op31 = 9018, Op32 = 9019, Op33 = 9020;
        public const ushort Op34 = 9021, Op35 = 9022, Op36 = 9023, Op37 = 9024, Op38 = 9025;
        public const ushort Op39 = 9026, Op40 = 9027, Op41 = 9028, Op42 = 9029, Op43 = 9030;
        public const ushort Op44 = 9031, Op45 = 9032;
    }

    public static class MessageRouter
    {
        private static readonly Dictionary<ushort, Type> _types = new Dictionary<ushort, Type>();
        public static int RegisteredOpCodes => _types.Count;
        public static void Clear() => _types.Clear();
        public static void Register(ushort opCode, Type type) { if (type != null) _types[opCode] = type; }
        public static Type GetMessageType(ushort opCode) => _types.TryGetValue(opCode, out var t) ? t : null;
        public static bool Unregister(ushort opCode) => _types.Remove(opCode);
        public static void RegisterDefaults()
        {
            Clear();
            Register(OpCodes.PlayerPosition, typeof(PlayerPositionMsg));
            Register(OpCodes.PlayerState, typeof(PlayerStateMsg));
            Register(OpCodes.MapChange, typeof(MapChangeMsg));
            Register(OpCodes.SkillCast, typeof(SkillCastMsg));
            Register(OpCodes.Damage, typeof(DamageMsg));
            Register(OpCodes.Heal, typeof(HealMsg));
            Register(OpCodes.BuffApply, typeof(BuffApplyMsg));
            Register(OpCodes.Chat, typeof(ChatChannelMsg));
            Register(OpCodes.ChatEmote, typeof(ChatEmoteMsg));
            Register(OpCodes.GuildCreate, typeof(GuildCreateMsg));
            Register(OpCodes.TaskComplete, typeof(TaskCompleteMsg));
            Register(OpCodes.TongJinScore, typeof(TongJinScoreMsg));
            Register(OpCodes.BossSpawn, typeof(BossSpawnMsg));
            for (ushort i = OpCodes.Op14; i <= OpCodes.Op45; i++) Register(i, typeof(NetworkPaddingMsg));
        }
    }

    [Serializable] public class PlayerPositionMsg { public int playerId; public float x,y,z; public int direction; }
    [Serializable] public class PlayerStateMsg { public int playerId,hp,mp,stamina,level; public long exp; public int state; public float x,y,z; public int direction; }
    [Serializable] public class MapChangeMsg { public int playerId,mapId; public float x,y,z; }
    [Serializable] public class SkillCastMsg { public int casterId,skillId,targetId,level; public float targetX,targetY; }
    [Serializable] public class DamageMsg { public int sourceId,targetId,amount; }
    [Serializable] public class HealMsg { public int sourceId,targetId,amount; }
    [Serializable] public class BuffApplyMsg { public int targetId,buffId,durationMs,sourceId; }
    [Serializable] public class ChatChannelMsg { public int playerId,channel,targetId; public string message; }
    [Serializable] public class ChatEmoteMsg { public int playerId,emoteId; }
    [Serializable] public class GuildCreateMsg { public int playerId,guildId; public string guildName; }
    [Serializable] public class TaskCompleteMsg { public int playerId,taskId,rewardItemId,rewardCount; }
    [Serializable] public class TongJinScoreMsg { public int songScore,jinScore,timeLeftSec; }
    [Serializable] public class BossSpawnMsg { public int bossId,mapId,despawnTime; public float x,y; }
    [Serializable] public class NetworkPaddingMsg { public int value; }
}
