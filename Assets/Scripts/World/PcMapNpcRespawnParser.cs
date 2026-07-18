// -----------------------------------------------------------------------------
// VLTK Mobile — PC npcrespawn.txt parser
// Source: settings/npcrespawn.txt (NPC spawn points trên map).
// Cols: MapId  NpcId  NpcTemplateId  PosX  PosY  RespawnSec  GroupId  MaxCount
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapNpcRespawnParser
    {
        public const int MapIdCol = 0;
        public const int NpcIdCol = 1;
        public const int NpcTemplateIdCol = 2;
        public const int PosXCol = 3;
        public const int PosYCol = 4;
        public const int RespawnSecCol = 5;
        public const int GroupIdCol = 6;
        public const int MaxCountCol = 7;

        public static List<PcMapNpcRespawnEntry> ParseFile(string path)
        {
            var rows = new List<PcMapNpcRespawnEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 6) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                int npcId = PcItemCommon.Int(cols, NpcIdCol);
                if (id <= 0 || npcId <= 0) continue;
                rows.Add(new PcMapNpcRespawnEntry
                {
                    mapId = id,
                    npcId = npcId,
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    respawnSec = PcItemCommon.Int(cols, RespawnSecCol),
                    groupId = PcItemCommon.Int(cols, GroupIdCol),
                    maxCount = PcItemCommon.Int(cols, MaxCountCol),
                });
            }
            return rows;
        }

        public static PcMapNpcRespawnRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapNpcRespawnRegistry();
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
    public class PcMapNpcRespawnEntry
    {
        public int mapId;
        public int npcId;
        public int npcTemplateId;
        public int posX;
        public int posY;
        public int respawnSec;
        public int groupId;
        public int maxCount;
    }

    public sealed class PcMapNpcRespawnRegistry
    {
        private readonly List<PcMapNpcRespawnEntry> _all = new();
        public int Count => _all.Count;
        public void Register(PcMapNpcRespawnEntry e) { if (e != null && e.mapId > 0 && e.npcId > 0) _all.Add(e); }
        public IReadOnlyList<PcMapNpcRespawnEntry> GetByMap(int mapId)
        {
            var list = new List<PcMapNpcRespawnEntry>();
            foreach (var e in _all) if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapNpcRespawnEntry> GetByTemplate(int templateId)
        {
            var list = new List<PcMapNpcRespawnEntry>();
            foreach (var e in _all) if (e.npcTemplateId == templateId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapNpcRespawnEntry> All => new List<PcMapNpcRespawnEntry>(_all);
    }
}
