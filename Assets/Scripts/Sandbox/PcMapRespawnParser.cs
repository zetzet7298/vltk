// -----------------------------------------------------------------------------
// VLTK Mobile — PC respawn.txt / revivepos.txt parser
// Source: settings/respawn.txt (điểm hồi sinh trên map).
// Cols: MapId  RespawnIdx  PosX  PosY  RespawnType (0=normal,1=item,2=skill,3=death,4=town)
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapRespawnParser
    {
        public const int MapIdCol = 0;
        public const int RespawnIdxCol = 1;
        public const int PosXCol = 2;
        public const int PosYCol = 3;
        public const int RespawnTypeCol = 4;

        public const int RespawnNormal = 0;
        public const int RespawnItem = 1;
        public const int RespawnSkill = 2;
        public const int RespawnDeath = 3;
        public const int RespawnTown = 4;

        public static List<PcMapRespawnEntry> ParseFile(string path)
        {
            var rows = new List<PcMapRespawnEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, MapIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapRespawnEntry
                {
                    mapId = id,
                    respawnIdx = PcItemCommon.Int(cols, RespawnIdxCol),
                    posX = PcItemCommon.Int(cols, PosXCol),
                    posY = PcItemCommon.Int(cols, PosYCol),
                    respawnType = PcItemCommon.Int(cols, RespawnTypeCol),
                });
            }
            return rows;
        }

        public static PcMapRespawnRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapRespawnRegistry();
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
    public class PcMapRespawnEntry
    {
        public int mapId;
        public int respawnIdx;
        public int posX;
        public int posY;
        public int respawnType;
    }

    public sealed class PcMapRespawnRegistry
    {
        private readonly List<PcMapRespawnEntry> _all = new();
        public int Count => _all.Count;
        public void Register(PcMapRespawnEntry e) { if (e != null && e.mapId > 0) _all.Add(e); }
        public IReadOnlyList<PcMapRespawnEntry> GetByMap(int mapId)
        {
            var list = new List<PcMapRespawnEntry>();
            foreach (var e in _all) if (e.mapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapRespawnEntry> GetByType(int type)
        {
            var list = new List<PcMapRespawnEntry>();
            foreach (var e in _all) if (e.respawnType == type) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapRespawnEntry> All => new List<PcMapRespawnEntry>(_all);
    }
}
