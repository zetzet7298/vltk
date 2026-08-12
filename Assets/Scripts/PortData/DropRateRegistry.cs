// -----------------------------------------------------------------------------
// VLTK Mobile — PC drop rate registry
// Loads every npcdroprate*.ini from a directory and indexes by table name and
// by NPC level band so the loot service can pick a table for any NPC.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Runtime registry for PC drop rate tables. Tables are loaded from a folder
    /// and indexed two ways: by exact table name, and by NPC level band.
    /// Level bands are derived from filenames: npcdroprate10.ini covers levels
    /// [10,20), npcdroprate119.ini covers [119,200), npcdroprate.ini covers
    /// [1,10), and special tables (npcdroprate_*.ini) are stored as overrides
    /// keyed by their table name and matched only by explicit name lookup.
    /// </summary>
    public class DropRateRegistry
    {
        private readonly Dictionary<string, DropRateTable> _byName =
            new Dictionary<string, DropRateTable>(StringComparer.OrdinalIgnoreCase);

        private readonly List<DropRateTable> _byLevel = new List<DropRateTable>();
        private readonly List<DropRateTable> _specials = new List<DropRateTable>();

        public IReadOnlyList<DropRateTable> AllTables => _byName.Count > 0
            ? (IReadOnlyList<DropRateTable>)new List<DropRateTable>(_byName.Values)
            : Array.Empty<DropRateTable>();

        public int TableCount => _byName.Count;

        public void LoadDirectory(string directory, string searchPattern = "*.ini")
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) return;
            var files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var table = PcDropRateParser.ParseFile(file);
                if (table == null) continue;
                RegisterLoadedTable(table, Path.GetFileNameWithoutExtension(file));
            }
        }

        public void RegisterLoadedTable(DropRateTable table, string sourceFileName = null)
        {
            if (table == null) return;
            if (string.IsNullOrEmpty(table.tableName))
                table.tableName = sourceFileName ?? string.Empty;
            if (table.minNpcLevel == 0 && table.maxNpcLevel == 0 && !string.IsNullOrEmpty(sourceFileName))
                ApplyLevelBandFromFileName(table, sourceFileName);
            _byName[table.tableName] = table;
            if (IsSpecialTableName(table.tableName) || table.maxNpcLevel == 0)
                _specials.Add(table);
            else
                _byLevel.Add(table);
            SortByLevel();
        }

        public bool TryGetTable(string name, out DropRateTable table)
        {
            if (string.IsNullOrEmpty(name))
            {
                table = null;
                return false;
            }
            return _byName.TryGetValue(name, out table);
        }

        public DropRateTable GetTable(string name)
        {
            return TryGetTable(name, out var t) ? t : null;
        }

        public IEnumerable<DropRateTable> GetTablesForLevel(int npcLevel)
        {
            foreach (var t in _byLevel)
            {
                if (npcLevel >= t.minNpcLevel && npcLevel < t.maxNpcLevel)
                    yield return t;
            }
        }

        public IEnumerable<DropRateTable> SpecialTables => _specials;

        public bool RemoveTable(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (!_byName.TryGetValue(name, out var t)) return false;
            _byName.Remove(name);
            _byLevel.Remove(t);
            _specials.Remove(t);
            return true;
        }

        public void Clear()
        {
            _byName.Clear();
            _byLevel.Clear();
            _specials.Clear();
        }

        private void SortByLevel()
        {
            _byLevel.Sort((a, b) => a.minNpcLevel.CompareTo(b.minNpcLevel));
        }

        /// <summary>
        /// Maps a raw PC source filename to the [min, max) NPC level band it covers.
        /// </summary>
        public static void ApplyLevelBandFromFileName(DropRateTable table, string fileName)
        {
            if (table == null || string.IsNullOrEmpty(fileName)) return;
            string stem = fileName;
            const string prefix = "npcdroprate";
            if (stem.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                stem = stem.Substring(prefix.Length);
            if (string.IsNullOrEmpty(stem))
            {
                table.minNpcLevel = 1;
                table.maxNpcLevel = 10;
                return;
            }
            if (int.TryParse(stem, out int lv))
            {
                table.minNpcLevel = lv;
                table.maxNpcLevel = lv + 10;
                if (lv >= 110) table.maxNpcLevel = 200;
                return;
            }
            table.minNpcLevel = 0;
            table.maxNpcLevel = int.MaxValue;
        }

        private static bool IsSpecialTableName(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name.Equals("npcdroprate", StringComparison.OrdinalIgnoreCase)) return false;
            if (!name.StartsWith("npcdroprate", StringComparison.OrdinalIgnoreCase)) return true;
            string tail = name.Substring("npcdroprate".Length);
            if (string.IsNullOrEmpty(tail)) return false;
            if (tail[0] == '_') return true;
            return !char.IsDigit(tail[0]);
        }
    }
}
