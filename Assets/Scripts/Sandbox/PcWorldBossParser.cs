// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings worldboss.txt parser
// Source: settings/boss/worldboss.txt (Boss Thế Giới spawn list).
// Columns: WorldBossId NpcTemplateId Name MapId PosX PosY Level HP Atk Def
//          RespawnSec DropTableId AnnounceType
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcWorldBossParser
    {
        public const int WorldBossIdCol = 0;
        public const int NpcTemplateIdCol = 1;
        public const int NameCol = 2;
        public const int MapIdCol = 3;
        public const int PosXCol = 4;
        public const int PosYCol = 5;
        public const int LevelCol = 6;
        public const int HpCol = 7;
        public const int AtkCol = 8;
        public const int DefCol = 9;
        public const int RespawnSecCol = 10;
        public const int DropTableIdCol = 11;
        public const int AnnounceTypeCol = 12;

        public static List<PcWorldBossEntry> ParseFile(string path)
        {
            var rows = new List<PcWorldBossEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, WorldBossIdCol);
                if (id <= 0) continue;
                rows.Add(new PcWorldBossEntry
                {
                    worldBossId = id,
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    level = PcItemCommon.Int(cols, LevelCol),
                    hp = PcItemCommon.Int(cols, HpCol),
                    atk = PcItemCommon.Int(cols, AtkCol),
                    def = PcItemCommon.Int(cols, DefCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnSecCol),
                    dropTableId = PcItemCommon.Int(cols, DropTableIdCol),
                    announceType = PcItemCommon.Int(cols, AnnounceTypeCol),
                });
            }
            return rows;
        }

        public static PcWorldBossRegistry BuildRegistry(string dir)
        {
            var reg = new PcWorldBossRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("worldboss") || name.Contains("world_boss"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcWorldBossEntry
    {
        public int worldBossId;
        public int npcTemplateId;
        public string nameRaw;
        public int mapId;
        public int posX;
        public int posY;
        public int level;
        public int hp;
        public int atk;
        public int def;
        public int respawnSec;
        public int dropTableId;
        public int announceType;
    }

    public sealed class PcWorldBossRegistry
    {
        private readonly Dictionary<int, PcWorldBossEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcWorldBossEntry e) { if (e == null || e.worldBossId <= 0) return; _byId[e.worldBossId] = e; }
        public PcWorldBossEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcWorldBossEntry> GetByMap(int mapId)
        {
            var list = new List<PcWorldBossEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcWorldBossEntry> All => new List<PcWorldBossEntry>(_byId.Values);
    }
}
