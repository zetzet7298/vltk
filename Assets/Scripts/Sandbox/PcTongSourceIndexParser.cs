// -----------------------------------------------------------------------------
// VLTK Mobile — PC Tong/guild source/config index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/*/{script,settings}/tong
// Imported files: StreamingAssets/Reference/PcTongSource/tong_source_index.txt and tong_config_index.txt.
// Catalog evidence only: file paths, directories, sizes, and SHA-256. This does
// not execute or claim Tong runtime behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcTongSourceIndexParser
    {
        public const string SourceIndexFileName = "tong_source_index.txt";
        public const string ConfigIndexFileName = "tong_config_index.txt";
        private const int Sha256Col = 11;

        public static List<PcTongSourceIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcTongSourceIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, 0), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcTongSourceIndexEntry
                {
                    sourceIndex = Int(cols, 0),
                    sourceGroup = Str(cols, 1),
                    sourceKind = Str(cols, 2),
                    sourceRoot = Str(cols, 3),
                    relativePath = Str(cols, 4),
                    directory = Str(cols, 5),
                    fileName = Str(cols, 6),
                    extension = Str(cols, 7),
                    isLua = Bool(cols, 8),
                    isCvsMetadata = Bool(cols, 9),
                    sizeBytes = Long(cols, 10),
                    sha256 = Str(cols, 11),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcTongSourceIndexCatalog BuildCatalog(string dir)
        {
            var catalog = new PcTongSourceIndexCatalog();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return catalog;
            foreach (var entry in ParseFile(Path.Combine(dir, SourceIndexFileName))) catalog.Register(entry);
            foreach (var entry in ParseFile(Path.Combine(dir, ConfigIndexFileName))) catalog.Register(entry);
            return catalog;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            return new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] cols, int i) => int.TryParse(Str(cols, i), out var value) ? value : 0;
        private static long Long(string[] cols, int i) => long.TryParse(Str(cols, i), out var value) ? value : 0L;
        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public sealed class PcTongSourceIndexEntry
    {
        public int sourceIndex;
        public string sourceGroup;
        public string sourceKind;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public bool isCvsMetadata;
        public long sizeBytes;
        public string sha256;
        public string SourceKey => (sourceRoot ?? string.Empty) + "|" + (relativePath ?? string.Empty);
    }

    public sealed class PcTongSourceIndexCatalog
    {
        private readonly Dictionary<string, PcTongSourceIndexEntry> byKey = new Dictionary<string, PcTongSourceIndexEntry>();
        private readonly HashSet<string> roots = new HashSet<string>();
        private readonly HashSet<string> rootDirectories = new HashSet<string>();
        public readonly List<PcTongSourceIndexEntry> entries = new List<PcTongSourceIndexEntry>();

        public int Count => entries.Count;
        public int SourceFileCount => CountKind("source");
        public int ConfigFileCount => CountKind("config");
        public int LuaFileCount => entries.FindAll(e => e.isLua).Count;
        public int CvsMetadataCount => entries.FindAll(e => e.isCvsMetadata).Count;
        public int SourceRootCount => roots.Count;
        public int RootDirectoryCount => rootDirectories.Count;

        public void Register(PcTongSourceIndexEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.SourceKey)) return;
            entries.Add(entry);
            byKey[entry.SourceKey] = entry;
            roots.Add(entry.sourceRoot ?? string.Empty);
            rootDirectories.Add((entry.sourceRoot ?? string.Empty) + "|" + (entry.directory ?? string.Empty));
        }

        public PcTongSourceIndexEntry Get(string sourceRoot, string relativePath)
        {
            byKey.TryGetValue((sourceRoot ?? string.Empty) + "|" + (relativePath ?? string.Empty), out var entry);
            return entry;
        }

        public int CountKind(string sourceKind)
            => entries.FindAll(e => string.Equals(e.sourceKind, sourceKind, StringComparison.OrdinalIgnoreCase)).Count;

        public int CountDirectory(string sourceKind, string directory)
            => entries.FindAll(e => string.Equals(e.sourceKind, sourceKind, StringComparison.OrdinalIgnoreCase)
                && string.Equals(e.directory, directory, StringComparison.OrdinalIgnoreCase)).Count;
    }
}
