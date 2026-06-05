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
            var snapshot = new WorldBossPanelSnapshot
            {
                playerId = playerId,
                currentMapId = currentMapId,
                activeBosses = 0,
                rows = Array.Empty<WorldBossPanelRow>()
            };
            if (service == null) return snapshot;
            var all = service.GetAll();
            var rows = new List<WorldBossPanelRow>();
            int active = 0;
            foreach (var boss in all)
            {
                if (boss == null) continue;
                bool isActive = boss.hp > 0;
                if (isActive) active++;
                rows.Add(new WorldBossPanelRow(
                    boss.bossId, boss.nameRaw, boss.mapName, boss.level, boss.hp, boss.atk, boss.def,
                    boss.respawnSec, boss.lastKillTimeAgo, isActive, 0, 0));
            }
            snapshot.activeBosses = active;
            snapshot.rows = rows;
            return snapshot;
        }

        public static IReadOnlyList<WorldBossPanelRow> GetByMap(WorldBossService service, int mapId)
        {
            if (service == null) return Array.Empty<WorldBossPanelRow>();
            var rows = new List<WorldBossPanelRow>();
            foreach (var boss in service.GetAll())
            {
                if (boss == null) continue;
                if (boss.mapId == mapId)
                {
                    rows.Add(new WorldBossPanelRow(
                        boss.bossId, boss.nameRaw, boss.mapName, boss.level, boss.hp, boss.atk, boss.def,
                        boss.respawnSec, boss.lastKillTimeAgo, boss.hp > 0, 0, 0));
                }
            }
            return rows;
        }

        public static IReadOnlyList<WorldBossPanelRow> GetActive(WorldBossService service, DateTime now)
        {
            if (service == null) return Array.Empty<WorldBossPanelRow>();
            var rows = new List<WorldBossPanelRow>();
            foreach (var boss in service.GetAll())
            {
                if (boss == null) continue;
                if (boss.hp > 0)
                {
                    rows.Add(new WorldBossPanelRow(
                        boss.bossId, boss.nameRaw, boss.mapName, boss.level, boss.hp, boss.atk, boss.def,
                        boss.respawnSec, boss.lastKillTimeAgo, true, 0, 0));
                }
            }
            return rows;
        }

        public static int ComputeDps(WorldBossService service, int bossId, int damage, int timeMs)
        {
            if (service == null || damage <= 0 || timeMs <= 0) return 0;
            long seconds = timeMs / 1000;
            if (seconds <= 0) seconds = 1;
            return (int)(damage / seconds);
        }

        public static int GetMyRank(WorldBossService service, int bossId, int playerId)
        {
            if (service == null || bossId <= 0 || playerId <= 0) return 0;
            return service.GetPlayerRank(bossId, playerId);
        }
    }
}
