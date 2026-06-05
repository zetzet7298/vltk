// -----------------------------------------------------------------------------
// VLTK Mobile — PC encounter.txt parser
// Source: settings/encounter/encounter.txt (Kỳ Ngộ - 100+ sự kiện ngẫu nhiên).
// Columns: EncounterId Type TriggerMapId Probability ResultId Description
// Type: 0=item, 1=npc, 2=trap, 3=portal, 4=event
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcEncounterParser
    {
        public const int EncounterIdCol = 0;
        public const int TypeCol = 1;
        public const int TriggerMapIdCol = 2;
        public const int ProbabilityCol = 3;
        public const int ResultIdCol = 4;
        public const int DescriptionCol = 5;

        public const int TypeItem = 0;
        public const int TypeNpc = 1;
        public const int TypeTrap = 2;
        public const int TypePortal = 3;
        public const int TypeEvent = 4;

        public static List<PcEncounterEntry> ParseFile(string path)
        {
            var rows = new List<PcEncounterEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, EncounterIdCol);
                if (id <= 0) continue;
                rows.Add(new PcEncounterEntry
                {
                    encounterId = id,
                    type = PcItemCommon.Int(cols, TypeCol),
                    triggerMapId = PcItemCommon.Int(cols, TriggerMapIdCol),
                    probability = PcItemCommon.Int(cols, ProbabilityCol),
                    resultId = PcItemCommon.Int(cols, ResultIdCol),
                    description = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcEncounterRegistry BuildRegistry(string dir)
        {
            var reg = new PcEncounterRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                var name = Path.GetFileName(f).ToLowerInvariant();
                if (name.StartsWith("encounter"))
                {
                    foreach (var s in ParseFile(f)) reg.Register(s);
                }
            }
            return reg;
        }
    }

    [System.Serializable]
    public class PcEncounterEntry
    {
        public int encounterId;
        public int type;
        public int triggerMapId;
        public int probability;
        public int resultId;
        public string description;
    }

    public sealed class PcEncounterRegistry
    {
        private readonly Dictionary<int, PcEncounterEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcEncounterEntry e) { if (e == null || e.encounterId <= 0) return; _byId[e.encounterId] = e; }
        public PcEncounterEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcEncounterEntry> GetByType(int type)
        {
            var list = new List<PcEncounterEntry>();
            foreach (var e in _byId.Values)
                if (e.type == type) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcEncounterEntry> GetByMap(int mapId)
        {
            var list = new List<PcEncounterEntry>();
            foreach (var e in _byId.Values)
                if (e.triggerMapId == mapId) list.Add(e);
            return list;
        }
        public IReadOnlyList<PcEncounterEntry> All => new List<PcEncounterEntry>(_byId.Values);
    }
}
