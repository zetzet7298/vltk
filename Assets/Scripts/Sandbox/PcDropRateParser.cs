// -----------------------------------------------------------------------------
// VLTK Mobile — PC drop rate INI parser
// Source: Assets/StreamingAssets/Reference/PcDropRate/npcdroprate*.ini
// PC encoding: GB2312/GB18030
// Format:
//   [Main]
//   Count=55
//   RandRange=33000
//   MagicRate=1
//   MoneyRate=10
//   MoneyScale=50
//   MinItemLevel=1
//   MinItemLevelScale=20
//   MaxItemLevel=10
//   MaxItemLevelScale=10
//
//   [1]
//   Genre=0
//   Detail=0
//   Particular=0
//   RandRate=300
//   MinItemLevel=1
//   MaxItemLevel=10
//   ...
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Parses a single PC npcdroprate*.ini file. The parser is tolerant: missing
    /// [Main] or per-item sections yield default values rather than throwing.
    /// </summary>
    public static class PcDropRateParser
    {
        public const string MainSection = "Main";

        public static DropRateTable ParseFile(string absolutePath, string tableName = null)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return null;
            if (!File.Exists(absolutePath))
                return null;
            string[] lines = ReadPcIniLines(absolutePath);
            return ParseLines(lines, tableName ?? Path.GetFileNameWithoutExtension(absolutePath));
        }

        public static DropRateTable ParseLines(IEnumerable<string> lines, string tableName)
        {
            var table = new DropRateTable
            {
                tableName = tableName ?? string.Empty,
                entries = new List<DropRateEntry>(),
            };
            if (lines == null) return table;

            string currentSection = null;
            DropRateEntry pendingEntry = null;

            foreach (var raw in lines)
            {
                if (raw == null) continue;
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (line[0] == ';' || line[0] == '#') continue;

                if (line[0] == '[' && line[line.Length - 1] == ']')
                {
                    if (pendingEntry != null)
                    {
                        FinalizeEntry(pendingEntry, table);
                        pendingEntry = null;
                    }
                    currentSection = line.Substring(1, line.Length - 2).Trim();
                    if (string.Equals(currentSection, MainSection, StringComparison.OrdinalIgnoreCase))
                    {
                        pendingEntry = null;
                    }
                    else if (int.TryParse(currentSection, NumberStyles.Integer, CultureInfo.InvariantCulture, out int idx))
                    {
                        pendingEntry = new DropRateEntry
                        {
                            sectionIndex = idx,
                        };
                    }
                    else
                    {
                        pendingEntry = null;
                    }
                    continue;
                }

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();

                if (string.Equals(currentSection, MainSection, StringComparison.OrdinalIgnoreCase))
                {
                    ApplyMainKey(table, key, value);
                }
                else if (pendingEntry != null)
                {
                    ApplyEntryKey(pendingEntry, key, value);
                }
            }

            if (pendingEntry != null)
                FinalizeEntry(pendingEntry, table);

            return table;
        }

        public static DropRateTable CreateLevelBandedTable(string tableName, int minLevel, int maxLevel, DropRateTable source)
        {
            if (source == null) return null;
            var copy = new DropRateTable
            {
                tableName = tableName ?? source.tableName,
                minNpcLevel = minLevel,
                maxNpcLevel = maxLevel,
                count = source.count,
                randRange = source.randRange,
                magicRate = source.magicRate,
                moneyRate = source.moneyRate,
                moneyScale = source.moneyScale,
                minItemLevel = source.minItemLevel,
                minItemLevelScale = source.minItemLevelScale,
                maxItemLevel = source.maxItemLevel,
                maxItemLevelScale = source.maxItemLevelScale,
                entries = new List<DropRateEntry>(source.entries.Count),
            };
            foreach (var e in source.entries)
            {
                copy.entries.Add(new DropRateEntry
                {
                    sectionIndex = e.sectionIndex,
                    genre = e.genre,
                    detail = e.detail,
                    particular = e.particular,
                    itemId = e.itemId,
                    randRate = e.randRate,
                    minItemLevel = e.minItemLevel,
                    maxItemLevel = e.maxItemLevel,
                    probability = e.probability,
                });
            }
            return copy;
        }

        private static void ApplyMainKey(DropRateTable table, string key, string value)
        {
            int iv = Int(value);
            switch (key)
            {
                case "Count": table.count = iv; break;
                case "RandRange": table.randRange = iv; break;
                case "MagicRate": table.magicRate = iv; break;
                case "MoneyRate": table.moneyRate = iv; break;
                case "MoneyScale": table.moneyScale = iv; break;
                case "MinItemLevel": table.minItemLevel = iv; break;
                case "MinItemLevelScale": table.minItemLevelScale = iv; break;
                case "MaxItemLevel": table.maxItemLevel = iv; break;
                case "MaxItemLevelScale": table.maxItemLevelScale = iv; break;
            }
        }

        private static void ApplyEntryKey(DropRateEntry entry, string key, string value)
        {
            int iv = Int(value);
            switch (key)
            {
                case "Genre": entry.genre = iv; break;
                case "Detail": entry.detail = iv; break;
                case "Particular": entry.particular = iv; break;
                case "RandRate": entry.randRate = iv; break;
                case "MinItemLevel": entry.minItemLevel = iv; break;
                case "MaxItemLevel": entry.maxItemLevel = iv; break;
            }
        }

        private static void FinalizeEntry(DropRateEntry entry, DropRateTable table)
        {
            entry.itemId = ResolveItemId(entry.genre, entry.detail, entry.particular);
            int range = Math.Max(1, table.randRange);
            entry.probability = (float)entry.randRate / range;
            if (entry.minItemLevel <= 0) entry.minItemLevel = table.minItemLevel;
            if (entry.maxItemLevel <= 0) entry.maxItemLevel = table.maxItemLevel;
            table.entries.Add(entry);
        }

        /// <summary>
        /// PC item id encoding: genre/detail/particular triplet is folded into a
        /// single int using the same formula used elsewhere in the PC pipeline
        /// (matches PcItemRegistry resolution for the npcres-style drop tables).
        /// </summary>
        public static int ResolveItemId(int genre, int detail, int particular)
        {
            return (genre * 1000000) + (detail * 1000) + particular;
        }

        private static int Int(string value)
        {
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;
        }

        private static string[] ReadPcIniLines(string absolutePath)
        {
            Encoding encoding;
            try
            {
                encoding = Encoding.GetEncoding("GB2312");
            }
            catch
            {
                encoding = Encoding.Default;
            }
            var text = File.ReadAllText(absolutePath, encoding);
            if (text.Length > 0 && text[0] == '\ufeff')
                text = text.Substring(1);
            return text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        }
    }
}
