// -----------------------------------------------------------------------------
// VLTK Mobile — PC mapconnection.txt map adjacency parser
// Source: server settings/mapconnection.txt (Reference/PcMap).
// Cols: ConnectionId, FromMapId, ToMapId, FromX, FromY, ToX, ToY,
//       RequiredLevel, ConnectionType
// Types: 0=normal, 1=teleport, 2=portal, 3=secret, 4=quest
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcMapConnectionParser
    {
        public const int ConnectionIdCol = 0;
        public const int FromMapIdCol = 1;
        public const int ToMapIdCol = 2;
        public const int FromXCol = 3;
        public const int FromYCol = 4;
        public const int ToXCol = 5;
        public const int ToYCol = 6;
        public const int RequiredLevelCol = 7;
        public const int ConnectionTypeCol = 8;

        public static List<PcMapConnectionEntry> ParseFile(string path)
        {
            var rows = new List<PcMapConnectionEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 4) continue;
                int id = PcItemCommon.Int(cols, ConnectionIdCol);
                if (id <= 0) continue;
                rows.Add(new PcMapConnectionEntry
                {
                    connectionId = id,
                    fromMapId = PcItemCommon.Int(cols, FromMapIdCol),
                    toMapId = PcItemCommon.Int(cols, ToMapIdCol),
                    fromX = PcItemCommon.Int(cols, FromXCol),
                    fromY = PcItemCommon.Int(cols, FromYCol),
                    toX = PcItemCommon.Int(cols, ToXCol),
                    toY = PcItemCommon.Int(cols, ToYCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    connectionType = PcItemCommon.Int(cols, ConnectionTypeCol),
                });
            }
            return rows;
        }

        public static PcMapConnectionRegistry BuildRegistry(string dir)
        {
            var reg = new PcMapConnectionRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }
    }

    [System.Serializable]
    public class PcMapConnectionEntry
    {
        public int connectionId;
        public int fromMapId;
        public int toMapId;
        public int fromX;
        public int fromY;
        public int toX;
        public int toY;
        public int requiredLevel;
        public int connectionType; // 0=normal, 1=teleport, 2=portal, 3=secret, 4=quest
    }

    public sealed class PcMapConnectionRegistry
    {
        private readonly Dictionary<int, PcMapConnectionEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcMapConnectionEntry e) { if (e == null || e.connectionId <= 0) return; _byId[e.connectionId] = e; }
        public PcMapConnectionEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcMapConnectionEntry> GetByFromMap(int mapId)
        {
            var list = new List<PcMapConnectionEntry>();
            foreach (var e in _byId.Values)
                if (e.fromMapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapConnectionEntry> GetByToMap(int mapId)
        {
            var list = new List<PcMapConnectionEntry>();
            foreach (var e in _byId.Values)
                if (e.toMapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcMapConnectionEntry> All => new List<PcMapConnectionEntry>(_byId.Values);
    }
}
