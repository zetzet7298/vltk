// -----------------------------------------------------------------------------
// VLTK Mobile — UI Panel: World Boss (Boss Thế Giới)
// Bảng UI liệt kê boss thế giới, trạng thái sống/chết, DPS, hạng của tôi.
// Vietnamese: "Boss Thế Giới", "Đang sống", "Đã chết", "Hồi sinh", "DPS của tôi", "Hạng".
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using VLTK.Sandbox;

namespace VLTK.UI
{
    public readonly struct WorldBossPanelRow
    {
        public readonly int bossId;
        public readonly string name;
        public readonly string mapName;
        public readonly int level;
        public readonly int hp;
        public readonly int atk;
        public readonly int def;
        public readonly int respawnSec;
        public readonly int lastKillTimeAgo;
        public readonly bool isActive;
        public readonly int myDps;
        public readonly int myRank;

        public WorldBossPanelRow(int bossId, string name, string mapName, int level, int hp, int atk, int def, int respawnSec, int lastKillTimeAgo, bool isActive, int myDps, int myRank)
        {
            this.bossId = bossId;
            this.name = name ?? string.Empty;
            this.mapName = mapName ?? string.Empty;
            this.level = level;
            this.hp = hp;
            this.atk = atk;
            this.def = def;
            this.respawnSec = respawnSec;
            this.lastKillTimeAgo = lastKillTimeAgo;
            this.isActive = isActive;
            this.myDps = myDps;
            this.myRank = myRank;
        }
    }

    public sealed class WorldBossPanelSnapshot
    {
        public int playerId;
        public int currentMapId;
        public int activeBosses;
        public IReadOnlyList<WorldBossPanelRow> rows;
    }

    public static class WorldBossPanelService
    {
        public const string LabelWorldBoss = "Boss Thế Giới";
        public const string LabelAlive = "Đang sống";
        public const string LabelDead = "Đã chết";
        public const string LabelRespawn = "Hồi sinh";
        public const string LabelMyDps = "DPS của tôi";
        public const string LabelMyRank = "Hạng";

        public static WorldBossPanelSnapshot BuildSnapshot(WorldBossService service, int playerId, int currentMapId)
        {
            return new WorldBossPanelSnapshot { rows = System.Array.Empty<WorldBossPanelRow>() };
        }

        public static IReadOnlyList<WorldBossPanelRow> GetByMap(WorldBossService service, int mapId)
        {
            return System.Array.Empty<WorldBossPanelRow>();
        }

        public static IReadOnlyList<WorldBossPanelRow> GetActive(WorldBossService service, DateTime now)
        {
            return System.Array.Empty<WorldBossPanelRow>();
        }

        public static int ComputeDps(WorldBossService service, int bossId, int damage, int timeMs)
        {
            return 0;
        }

        public static int GetMyRank(WorldBossService service, int bossId, int playerId)
        {
            return 0;
        }

    }
}
