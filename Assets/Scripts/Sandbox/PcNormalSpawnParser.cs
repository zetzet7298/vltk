// -----------------------------------------------------------------------------
// VLTK Mobile — PC settings/normal.txt compatibility parser.
//
// The checked-in PC normal.txt sample is item equipment data, not monster-spawn
// data. To keep tests and the runtime MapSpawnRegistry on one contract, this
// parser emits VLTK.Model.SpawnPoint records keyed by template/item id while
// leaving absent map/spawn fields at 0 and attaching a warning per row.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    [System.Serializable]
    public class PcNormalSpawnEntry : SpawnPoint
    {
        public int npcId
        {
            get => npcTemplateId;
            set => npcTemplateId = value;
        }

        public int posX
        {
            get => x;
            set => x = value;
        }

        public int posY
        {
            get => y;
            set => y = value;
        }
    }

    public sealed class PcNormalSpawnRegistry
    {
        private readonly Dictionary<int, List<SpawnPoint>> _byTemplate = new();
        private readonly Dictionary<int, List<SpawnPoint>> _byMap = new();
        private readonly List<SpawnPoint> _all = new();

        public int Count => _all.Count;

        public void Register(SpawnPoint e)
        {
            if (e == null) return;

            _all.Add(e);

            if (!_byTemplate.TryGetValue(e.npcTemplateId, out var tl))
            {
                tl = new List<SpawnPoint>();
                _byTemplate[e.npcTemplateId] = tl;
            }
            tl.Add(e);

            if (!_byMap.TryGetValue(e.mapId, out var ml))
            {
                ml = new List<SpawnPoint>();
                _byMap[e.mapId] = ml;
            }
            ml.Add(e);
        }

        public SpawnPoint Get(int id)
        {
            if (_byTemplate.TryGetValue(id, out var v) && v.Count > 0) return v[0];
            return null;
        }

        public List<SpawnPoint> GetByMap(int mapId)
            => _byMap.TryGetValue(mapId, out var v) ? v : new List<SpawnPoint>();

        public IEnumerable<SpawnPoint> All => _all;
    }

    public static class PcNormalSpawnParser
    {
        public const int NameCol = 0;
        public const int NpcTemplateIdCol = 1;
        public const int LevelCol = 7;
        public const int MinItemEquipmentColumns = 8;
        private const string ItemEquipmentWarning = "normal.txt is item equipment data; map/spawn fields are absent and default to 0";

        public static List<SpawnPoint> ParseFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<SpawnPoint>();
            var lines = PcText.ReadLinesTcvn3(path);
            return ParseLines(lines, "normal.txt");
        }

        public static List<SpawnPoint> ParseLines(IEnumerable<string> lines, string sourceFile = "normal.txt")
        {
            var rows = new List<SpawnPoint>();
            if (lines == null) return rows;

            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (!headerSkipped)
                {
                    headerSkipped = true;
                    continue;
                }

                var cols = line.Split('\t');
                if (cols.Length < MinItemEquipmentColumns) continue;

                var point = new SpawnPoint
                {
                    nameRaw = PcItemCommon.Str(cols, NameCol),
                    npcTemplateId = PcItemCommon.Int(cols, NpcTemplateIdCol),
                    level = PcItemCommon.Int(cols, LevelCol),
                    mapId = 0,
                    x = 0,
                    y = 0,
                    direction = 0,
                    count = 0,
                    respawnSec = 0,
                    aiMode = 0,
                    groupId = 0,
                    sourceFile = string.IsNullOrWhiteSpace(sourceFile) ? "normal.txt" : sourceFile,
                    rowIndex = rows.Count,
                };

                if (IsNormalSource(point.sourceFile))
                    point.warnings.Add(ItemEquipmentWarning);

                rows.Add(point);
            }

            return rows;
        }

        public static bool IsReplacementCharPresent(string s)
            => PcItemCommon.ContainsReplacementChar(s);

        public static PcNormalSpawnRegistry BuildRegistry(string dir)
        {
            var reg = new PcNormalSpawnRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "normal*.txt"))
                foreach (var e in ParseFile(f)) reg.Register(e);
            return reg;
        }

        private static bool IsNormalSource(string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(sourceFile)) return true;
            return Path.GetFileName(sourceFile) == "normal.txt";
        }
    }
}
