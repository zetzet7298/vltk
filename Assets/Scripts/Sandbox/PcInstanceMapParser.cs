// -----------------------------------------------------------------------------
// VLTK Mobile — PC instance/mission map (mê cung, phó bản, ...) parser
// Source: settings/maps/instance/* + settings/maps/missions/* (802 phó bản PC).
// File format (GB2312, tab-separated):
//   MapId  Name  Type  MinLevel  MaxLevel  MinParty  MaxParty  DurationMinutes
// Type: 0=normal, 1=maze, 2=arena, 3=boss, 4=farm, 5=event.
// Vietnamese comments.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcInstanceMapParser
    {
        public const int NameCol = 0;
        public const int TypeCol = 1;
        public const int MinLevelCol = 2;
        public const int MaxLevelCol = 3;
        public const int MinPartyCol = 4;
        public const int MaxPartyCol = 5;
        public const int DurationCol = 6;

        /// <summary>Parse 1 file .txt phó bản. Trả về danh sách entries (rỗng nếu lỗi).</summary>
        public static List<PcInstanceMapEntry> ParseFile(string path)
        {
            var rows = new List<PcInstanceMapEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            int autoId = 0;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 3) continue;
                autoId++;
                int mapId = autoId;
                if (int.TryParse(cols[0].Trim(), out int parsed) && parsed > 0) mapId = parsed;
                rows.Add(new PcInstanceMapEntry
                {
                    mapId = mapId,
                    nameVi = PcItemCommon.Str(cols, NameCol),
                    mapType = PcItemCommon.Int(cols, TypeCol, 0),
                    minLevel = PcItemCommon.Int(cols, MinLevelCol, 1),
                    maxLevel = PcItemCommon.Int(cols, MaxLevelCol, 200),
                    minPartySize = PcItemCommon.Int(cols, MinPartyCol, 1),
                    maxPartySize = PcItemCommon.Int(cols, MaxPartyCol, 6),
                    durationMinutes = PcItemCommon.Int(cols, DurationCol, 30),
                });
            }
            return rows;
        }

        /// <summary>
        /// Duyệt thư mục instance/ + missions/ + các file mission*.txt.
        /// Trả về registry rỗng nếu thư mục không tồn tại.
        /// </summary>
        public static PcInstanceMapRegistry BuildRegistry(string rootDir)
        {
            var reg = new PcInstanceMapRegistry();
            if (string.IsNullOrEmpty(rootDir) || !Directory.Exists(rootDir)) return reg;

            foreach (var sub in new[] { "instance", "missions", "phoban" })
            {
                string d = Path.Combine(rootDir, sub);
                if (Directory.Exists(d))
                {
                    foreach (var f in Directory.GetFiles(d, "*.txt", SearchOption.AllDirectories))
                        foreach (var e in ParseFile(f)) reg.Register(e);
                }
            }

            // Quét các file mission*.txt / instance*.txt ở root
            foreach (var pattern in new[] { "mission*.txt", "instance*.txt", "phoban*.txt", "maze*.txt" })
            {
                foreach (var f in Directory.GetFiles(rootDir, pattern, SearchOption.TopDirectoryOnly))
                    foreach (var e in ParseFile(f)) reg.Register(e);
            }

            return reg;
        }
    }

    /// <summary>Một bản đồ phó bản / mê cung / arena.</summary>
    [System.Serializable]
    public class PcInstanceMapEntry
    {
        public int mapId;
        public string nameVi;
        public int mapType;        // 0=normal, 1=maze, 2=arena, 3=boss, 4=farm, 5=event
        public int minLevel;
        public int maxLevel;
        public int minPartySize;
        public int maxPartySize;
        public int durationMinutes;
    }

    public sealed class PcInstanceMapRegistry
    {
        private readonly Dictionary<int, PcInstanceMapEntry> _byId = new();
        private readonly Dictionary<int, List<PcInstanceMapEntry>> _byType = new();
        private readonly List<PcInstanceMapEntry> _all = new();
        public int Count => _byId.Count;

        public void Register(PcInstanceMapEntry e)
        {
            if (e == null || e.mapId <= 0) return;
            if (_byId.ContainsKey(e.mapId)) return;
            _byId[e.mapId] = e;
            _all.Add(e);
            if (!_byType.TryGetValue(e.mapType, out var list))
            {
                list = new List<PcInstanceMapEntry>();
                _byType[e.mapType] = list;
            }
            list.Add(e);
        }

        public PcInstanceMapEntry Get(int mapId)
            => _byId.TryGetValue(mapId, out var v) ? v : null;

        public IReadOnlyList<PcInstanceMapEntry> GetAll() => _all;

        public IReadOnlyList<PcInstanceMapEntry> GetByType(int type)
            => _byType.TryGetValue(type, out var v)
                ? (IReadOnlyList<PcInstanceMapEntry>)v
                : System.Array.Empty<PcInstanceMapEntry>();
    }
}
