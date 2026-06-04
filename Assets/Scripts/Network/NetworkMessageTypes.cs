// -----------------------------------------------------------------------------
// VLTK Mobile — Network Message DTOs
// -----------------------------------------------------------------------------

using System;

namespace VLTK.Network
{
    [Serializable]
    public struct PlayerPositionMsg
    {
        public int playerId;
        public float x;
        public float y;
        public float z;
        public int direction;
    }

    [Serializable]
    public struct PlayerActionMsg
    {
        public int playerId;
        public int skillId;
        public int targetId;
    }

    [Serializable]
    public struct ChatMsg
    {
        public int playerId;
        public string message;
    }

    [Serializable]
    public struct PlayerJoinMsg
    {
        public int playerId;
        public string playerName;
        public int sectId;
    }

    [Serializable]
    public struct PlayerLeaveMsg
    {
        public int playerId;
    }

    [Serializable]
    public struct NpcSyncMsg
    {
        public int npcId;
        public int hp;
        public int state;
    }
}
