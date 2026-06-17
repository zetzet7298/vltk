// -----------------------------------------------------------------------------
// VLTK Mobile — PC event config source index parser.
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem only:
//   Client 6.0/settings/event
//   Server 6.0/server/home_jxser*/server1/settings/event
// Catalog/schema evidence only. This does not claim event runtime behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcEventConfigIndexParser
    {
        public const string IndexFileName = "event_config_source_index.txt";
        public const int SourceIndexCol = 0;
        public const int RootIdCol = 1;
        public const int SideCol = 2;
        public const int SourceRootCol = 3;
        public const int RelativePathCol = 4;
        public const int DirectoryCol = 5;
        public const int FileNameCol = 6;
        public const int ExtensionCol = 7;
        public const int IsTextLikeCol = 8;
        public const int SizeBytesCol = 9;
        public const int Sha256Col = 10;
        public const int EncodingProbeCol = 11;
        public const int DataRowCountCol = 12;
        public const int ColumnCountCol = 13;
        public const int HeaderSignatureCol = 14;

        public static List<PcEventConfigIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcEventConfigIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= HeaderSignatureCol) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcEventConfigIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    rootId = Str(cols, RootIdCol),
                    side = Str(cols, SideCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    isTextLike = Bool(cols, IsTextLikeCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                    encodingProbe = Str(cols, EncodingProbeCol),
                    dataRowCount = Int(cols, DataRowCountCol),
                    columnCount = Int(cols, ColumnCountCol),
                    headerSignature = Str(cols, HeaderSignatureCol),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.rootId) && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcEventConfigIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcEventConfigIndexRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            var path = Path.Combine(dir, IndexFileName);
            if (!File.Exists(path)) return reg;
            foreach (var entry in ParseFile(path)) reg.Register(entry);
            return reg;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            return new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;

        private static int Int(string[] cols, int i)
            => int.TryParse(Str(cols, i), out var value) ? value : 0;

        private static long Long(string[] cols, int i)
            => long.TryParse(Str(cols, i), out var value) ? value : 0L;

        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public class PcEventConfigIndexEntry
    {
        public int sourceIndex;
        public string rootId;
        public string side;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isTextLike;
        public long sizeBytes;
        public string sha256;
        public string encodingProbe;
        public int dataRowCount;
        public int columnCount;
        public string headerSignature;
    }

    public sealed class PcEventConfigIndexRegistry
    {
        private readonly List<PcEventConfigIndexEntry> _all = new List<PcEventConfigIndexEntry>();
        private readonly Dictionary<string, PcEventConfigIndexEntry> _byRootPath = new Dictionary<string, PcEventConfigIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcEventConfigIndexEntry>> _byRoot = new Dictionary<string, List<PcEventConfigIndexEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcEventConfigIndexEntry>> _bySide = new Dictionary<string, List<PcEventConfigIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int TextFileCount { get; private set; }
        public int ClientFileCount => GetBySide("client").Count;
        public int ServerFileCount => GetBySide("server").Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcEventConfigIndexEntry> All => _all;

        public void Register(PcEventConfigIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.rootId) || string.IsNullOrEmpty(e.relativePath)) return;
            var key = MakeKey(e.rootId, e.relativePath);
            if (_byRootPath.ContainsKey(key)) return;
            _all.Add(e);
            _byRootPath[key] = e;
            Add(_byRoot, e.rootId, e);
            Add(_bySide, e.side, e);
            if (e.isTextLike) TextFileCount++;
            TotalSizeBytes += Math.Max(0L, e.sizeBytes);
        }

        public PcEventConfigIndexEntry GetByRootPath(string rootId, string relativePath)
            => _byRootPath.TryGetValue(MakeKey(rootId, relativePath), out var entry) ? entry : null;

        public IReadOnlyList<PcEventConfigIndexEntry> GetByRoot(string rootId)
            => _byRoot.TryGetValue(rootId ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcEventConfigIndexEntry>)Array.Empty<PcEventConfigIndexEntry>();

        public IReadOnlyList<PcEventConfigIndexEntry> GetBySide(string side)
            => _bySide.TryGetValue(side ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcEventConfigIndexEntry>)Array.Empty<PcEventConfigIndexEntry>();

        private static void Add(Dictionary<string, List<PcEventConfigIndexEntry>> map, string key, PcEventConfigIndexEntry entry)
        {
            key = key ?? string.Empty;
            if (!map.TryGetValue(key, out var entries))
            {
                entries = new List<PcEventConfigIndexEntry>();
                map[key] = entries;
            }
            entries.Add(entry);
        }

        private static string MakeKey(string rootId, string relativePath)
            => (rootId ?? string.Empty) + "\n" + (relativePath ?? string.Empty);
    }
}
