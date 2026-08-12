// -----------------------------------------------------------------------------
// VLTK Mobile — PC event drop-rate source index parser.
// Source of truth: /var/www/jx-pc/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/settings/droprate/event
// Data-only index: records PC file metadata and numeric [N] drop rows; it does
// not alter the existing PcDropRate runtime registry or loot behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcDropRateEventSourceParser
    {
        public const string ManifestFileName = "manifest.tsv";
        public const string CatalogFileName = "catalog.tsv";
        public const string PcSourceRelativeRoot = "Server 6.0/server/home_jxser/server1/settings/droprate/event";

        public static PcDropRateEventCatalog BuildCatalog(string dir)
        {
            var catalog = new PcDropRateEventCatalog();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return catalog;
            foreach (var file in ParseManifestFile(Path.Combine(dir, ManifestFileName)))
                catalog.RegisterFile(file);
            foreach (var row in ParseCatalogFile(Path.Combine(dir, CatalogFileName)))
                catalog.RegisterDropRow(row);
            return catalog;
        }

        public static List<PcDropRateEventFileEntry> ParseManifestFile(string path)
        {
            var rows = new List<PcDropRateEventFileEntry>();
            foreach (var cols in ReadTsv(path))
            {
                if (cols.Length < 13 || IsHeader(cols, "SourceIndex")) continue;
                rows.Add(new PcDropRateEventFileEntry
                {
                    sourceIndex = Int(cols, 0),
                    relativePath = Str(cols, 1),
                    directory = Str(cols, 2),
                    fileName = Str(cols, 3),
                    sizeBytes = Long(cols, 4),
                    sha256 = Str(cols, 5),
                    sectionCount = Int(cols, 6),
                    dropRowCount = Int(cols, 7),
                    mainCount = Int(cols, 8),
                    randRange = Int(cols, 9),
                    magicRate = Int(cols, 10),
                    moneyRate = Int(cols, 11),
                    moneyScale = Int(cols, 12),
                });
            }
            return rows;
        }

        public static List<PcDropRateEventDropRow> ParseCatalogFile(string path)
        {
            var rows = new List<PcDropRateEventDropRow>();
            foreach (var cols in ReadTsv(path))
            {
                if (cols.Length < 9 || IsHeader(cols, "SourceIndex")) continue;
                rows.Add(new PcDropRateEventDropRow
                {
                    sourceIndex = Int(cols, 0),
                    relativePath = Str(cols, 1),
                    sectionIndex = Int(cols, 2),
                    genre = Int(cols, 3),
                    detail = Int(cols, 4),
                    particular = Int(cols, 5),
                    randRate = Int(cols, 6),
                    minItemLevel = Int(cols, 7),
                    maxItemLevel = Int(cols, 8),
                });
            }
            return rows;
        }

        private static IEnumerable<string[]> ReadTsv(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) yield break;
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                yield return line.Split('\t');
            }
        }

        private static bool IsHeader(string[] cols, string first)
            => string.Equals(Str(cols, 0), first, StringComparison.OrdinalIgnoreCase);
        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] cols, int i)
            => int.TryParse(Str(cols, i), out var value) ? value : 0;
        private static long Long(string[] cols, int i)
            => long.TryParse(Str(cols, i), out var value) ? value : 0L;
    }
}
