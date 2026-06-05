// -----------------------------------------------------------------------------
// VLTK Mobile — PC Faction Quest Area parser
// Source: factionquestarea.txt — khu vực nhiệm vụ theo môn phái.
// Cols: QuestAreaId, FactionId, FactionName, MapId, QuestCount, RequiredLevel, Description.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;

namespace VLTK.Sandbox
{
    public static class PcFactionQuestAreaParser
    {
        public const int QuestAreaIdCol = 0;
        public const int FactionIdCol = 1;
        public const int FactionNameCol = 2;
        public const int MapIdCol = 3;
        public const int QuestCountCol = 4;
        public const int RequiredLevelCol = 5;
        public const int DescriptionCol = 6;

        public static List<PcFactionQuestAreaEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionQuestAreaEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }
                var cols = line.Split('\t');
                if (cols.Length < 2) continue;
                int id = PcItemCommon.Int(cols, QuestAreaIdCol);
                if (id <= 0) continue;
                rows.Add(new PcFactionQuestAreaEntry
                {
                    questAreaId = id,
                    factionId = PcItemCommon.Int(cols, FactionIdCol),
                    factionNameRaw = PcItemCommon.Str(cols, FactionNameCol),
                    mapId = PcItemCommon.Int(cols, MapIdCol),
                    questCount = PcItemCommon.Int(cols, QuestCountCol),
                    requiredLevel = PcItemCommon.Int(cols, RequiredLevelCol),
                    descriptionRaw = PcItemCommon.Str(cols, DescriptionCol),
                });
            }
            return rows;
        }

        public static PcFactionQuestAreaRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionQuestAreaRegistry();
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
    public class PcFactionQuestAreaEntry
    {
        public int questAreaId;
        public int factionId;
        public string factionNameRaw;
        public int mapId;
        public int questCount;
        public int requiredLevel;
        public string descriptionRaw;
    }

    public sealed class PcFactionQuestAreaRegistry
    {
        private readonly Dictionary<int, PcFactionQuestAreaEntry> _byId = new();
        public int Count => _byId.Count;
        public void Register(PcFactionQuestAreaEntry e) { if (e == null || e.questAreaId <= 0) return; _byId[e.questAreaId] = e; }
        public PcFactionQuestAreaEntry Get(int id) => _byId.TryGetValue(id, out var v) ? v : null;
        public IReadOnlyList<PcFactionQuestAreaEntry> All => new List<PcFactionQuestAreaEntry>(_byId.Values);

        public IReadOnlyList<PcFactionQuestAreaEntry> GetByFaction(int factionId)
        {
            var list = new List<PcFactionQuestAreaEntry>();
            foreach (var e in _byId.Values)
                if (e.factionId == factionId) list.Add(e);
            return list;
        }

        public IReadOnlyList<PcFactionQuestAreaEntry> GetByMap(int mapId)
        {
            var list = new List<PcFactionQuestAreaEntry>();
            foreach (var e in _byId.Values)
                if (e.mapId == mapId) list.Add(e);
            return list;
        }

        public int GetTotalQuestsForFaction(int factionId)
        {
            int total = 0;
            foreach (var e in _byId.Values)
                if (e.factionId == factionId) total += e.questCount;
            return total;
        }
    }
}
