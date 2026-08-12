// -----------------------------------------------------------------------------
// VLTK Mobile — PC Boss Hoàng Kim parser (32 bosses trên PC)
// Source: settings/boss/bosshoangkim.txt (Reference/PcBoss).
// File format (GB2312, tab-separated):
//   BossId  Name  MapId  PosX  PosY  NpcTemplateId  Level  RespawnSec
//   DropItemId  DropCount
// Trả về registry runtime tra cứu theo bossId / mapId.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcBossHoangKimParser
    {
        public const int NameCol = 1;
        public const int MapIdCol = 2;
        public const int PosXCol = 3;
        public const int PosYCol = 4;
        public const int NpcTemplateIdCol = 5;
        public const int LevelCol = 6;
        public const int RespawnSecCol = 7;
        public const int DropItemIdCol = 8;
        public const int DropCountCol = 9;

        public static List<PcBossHoangKimEntry> ParseFile(string path)
        {
            var rows = new List<PcBossHoangKimEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, 0);
                if (id <= 0) continue;
                rows.Add(new PcBossHoangKimEntry
                {
                    bossId = id,
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    level = PcItemCommon.Int(cols, LevelCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnSecCol),
                    dropItemId = PcItemCommon.Int(cols, DropItemIdCol),
                    dropCount = PcItemCommon.Int(cols, DropCountCol),
                });
            }
            return rows;
        }

        public static PcBossHoangKimRegistry BuildRegistry(string dir)
        {
            var reg = new PcBossHoangKimRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var ext = Path.GetExtension(f);
                if (string.Equals(ext, ".ini", System.StringComparison.OrdinalIgnoreCase)
                    || string.Equals(ext, ".txt", System.StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcBossHoangKimEntry
    {
        public int bossId;
        public string nameRaw;
        public int mapId;
        public int posX;
        public int posY;
        public int npcTemplateId;
        public int level;
        public int respawnSec;
        public int dropItemId;
        public int dropCount;
    }

    public sealed class PcBossHoangKimRegistry
    {
        private readonly Dictionary<int, PcBossHoangKimEntry> _byId = new();

        public int Count => _byId.Count;

        public void Register(PcBossHoangKimEntry e)
        {
            if (e == null || e.bossId <= 0) return;
            _byId[e.bossId] = e;
        }

        public PcBossHoangKimEntry Get(int id)
            => _byId.TryGetValue(id, out var v) ? v : null;

        public IReadOnlyList<PcBossHoangKimEntry> GetByMap(int mapId)
        {
            var list = new List<PcBossHoangKimEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcBossHoangKimEntry> All
            => new List<PcBossHoangKimEntry>(_byId.Values);
    }
}
