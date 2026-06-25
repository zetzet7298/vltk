// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tong map catalog parser (legacy FactionMap service name)
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/tong/addtongnpc.lua
// Enter gate evidence: script/tong/tong_mix.lua ENTER_TONG_MAP_G requires level 10.
// Imported file: StreamingAssets/Reference/PcTong/faction_map.txt
// Rows are normalized from PC Lua tables only; no owner/capture semantics are
// invented here. Runtime integration remains separate.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcFactionMapParser
    {
        public const int SourceTableCol = 0;
        public const int SourceIndexCol = 1;
        public const int MapIdCol = 2;
        public const int MapNameRawCol = 3;
        public const int MapKindCol = 4;
        public const int EnterXCol = 5;
        public const int EnterYCol = 6;
        public const int RequiredLevelCol = 7;
        public const int NpcTemplateIdCol = 8;
        public const int NpcXCol = 9;
        public const int NpcYCol = 10;
        public const int NpcScriptRawCol = 11;
        public const int NpcNameRawCol = 12;

        public static List<PcFactionMapEntry> ParseFile(string path)
        {
            var rows = new List<PcFactionMapEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= MapIdCol) continue;
                if (string.Equals(Str(cols, SourceTableCol), "SourceTable", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcFactionMapEntry
                {
                    sourceTable = Str(cols, SourceTableCol),
                    sourceIndex = Int(cols, SourceIndexCol),
                    factionId = 0,
                    mapId = Int(cols, MapIdCol),
                    mapNameRaw = Str(cols, MapNameRawCol),
                    mapKind = Str(cols, MapKindCol),
                    enterX = Int(cols, EnterXCol),
                    enterY = Int(cols, EnterYCol),
                    requiredLevel = Int(cols, RequiredLevelCol),
                    ownerBonusPercent = 0,
                    npcTemplateId = Int(cols, NpcTemplateIdCol),
                    npcX = Int(cols, NpcXCol),
                    npcY = Int(cols, NpcYCol),
                    npcScriptRaw = Str(cols, NpcScriptRawCol),
                    npcNameRaw = Str(cols, NpcNameRawCol),
                };
                if (entry.mapId > 0) rows.Add(entry);
            }
            return rows;
        }

        public static PcFactionMapRegistry BuildRegistry(string dir)
        {
            var reg = new PcFactionMapRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            string main = Path.Combine(dir, "faction_map.txt");
            if (File.Exists(main))
                foreach (var entry in ParseFile(main)) reg.Register(entry);
            return reg;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return result;
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            result.AddRange(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
            return result;
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;

        private static int Int(string[] cols, int i)
            => int.TryParse(Str(cols, i), out var value) ? value : 0;
    }

    [Serializable]
    public class PcFactionMapEntry
    {
        public string sourceTable;
        public int sourceIndex;
        public int factionId;
        public int mapId;
        public string mapNameRaw;
        public string mapKind;
        public int enterX;
        public int enterY;
        public int requiredLevel;
        public int ownerBonusPercent;
        public int npcTemplateId;
        public int npcX;
        public int npcY;
        public string npcScriptRaw;
        public string npcNameRaw;
        public bool HasEnterPosition => enterX > 0 && enterY > 0;
        public bool HasNpcPosition => npcTemplateId > 0 && npcX > 0 && npcY > 0;
    }

    public sealed class PcFactionMapRegistry
    {
        private readonly List<PcFactionMapEntry> _rows = new List<PcFactionMapEntry>();
        private readonly Dictionary<int, PcFactionMapEntry> _byMapId = new Dictionary<int, PcFactionMapEntry>();
        private readonly Dictionary<int, List<PcFactionMapEntry>> _byFaction = new Dictionary<int, List<PcFactionMapEntry>>();
        private readonly Dictionary<string, List<PcFactionMapEntry>> _bySourceTable = new Dictionary<string, List<PcFactionMapEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _rows.Count;
        public IEnumerable<PcFactionMapEntry> All => _rows;

        public void Register(PcFactionMapEntry entry)
        {
            if (entry == null || entry.mapId <= 0) return;
            _rows.Add(entry);
            if (!_byMapId.ContainsKey(entry.mapId)) _byMapId[entry.mapId] = entry;

            if (!_byFaction.TryGetValue(entry.factionId, out var factionRows))
            {
                factionRows = new List<PcFactionMapEntry>();
                _byFaction[entry.factionId] = factionRows;
            }
            factionRows.Add(entry);

            if (!string.IsNullOrEmpty(entry.sourceTable))
            {
                if (!_bySourceTable.TryGetValue(entry.sourceTable, out var sourceRows))
                {
                    sourceRows = new List<PcFactionMapEntry>();
                    _bySourceTable[entry.sourceTable] = sourceRows;
                }
                sourceRows.Add(entry);
            }
        }

        public PcFactionMapEntry Get(int mapId) => _byMapId.TryGetValue(mapId, out var value) ? value : null;
        public IReadOnlyList<PcFactionMapEntry> GetByFaction(int factionId) => _byFaction.TryGetValue(factionId, out var value) ? value : (IReadOnlyList<PcFactionMapEntry>)Array.Empty<PcFactionMapEntry>();
        public IReadOnlyList<PcFactionMapEntry> GetBySourceTable(string sourceTable) => !string.IsNullOrEmpty(sourceTable) && _bySourceTable.TryGetValue(sourceTable, out var value) ? value : (IReadOnlyList<PcFactionMapEntry>)Array.Empty<PcFactionMapEntry>();
    }
}
