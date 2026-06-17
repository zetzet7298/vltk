// -----------------------------------------------------------------------------
// VLTK Mobile — PC server event script index parser.
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/event
// Imported file: StreamingAssets/Reference/PcServerEvent/server_event_index.txt
// This is a catalog of source files only. It does not execute or infer Lua event
// semantics. The PC directory has 455 files total: 427 Lua scripts and 28 CVS
// metadata files preserved as source evidence, so 455 is not claimed as 455
// runnable events.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcServerEventIndexParser
    {
        public const string IndexFileName = "server_event_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int IsLuaCol = 6;
        public const int IsCvsMetadataCol = 7;
        public const int SizeBytesCol = 8;
        public const int Sha256Col = 9;

        public static List<PcServerEventIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcServerEventIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcServerEventIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    isLua = Bool(cols, IsLuaCol),
                    isCvsMetadata = Bool(cols, IsCvsMetadataCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcServerEventIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcServerEventIndexRegistry();
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
    public class PcServerEventIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public bool isCvsMetadata;
        public long sizeBytes;
        public string sha256;
    }

    public sealed class PcServerEventIndexRegistry
    {
        private readonly List<PcServerEventIndexEntry> _all = new List<PcServerEventIndexEntry>();
        private readonly Dictionary<string, PcServerEventIndexEntry> _byPath = new Dictionary<string, PcServerEventIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcServerEventIndexEntry>> _byDirectory = new Dictionary<string, List<PcServerEventIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int NonLuaFileCount => Count - LuaFileCount;
        public int CvsMetadataFileCount { get; private set; }
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcServerEventIndexEntry> All => _all;

        public void Register(PcServerEventIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath) || _byPath.ContainsKey(e.relativePath)) return;
            _all.Add(e);
            _byPath[e.relativePath] = e;
            if (e.isLua) LuaFileCount++;
            if (e.isCvsMetadata) CvsMetadataFileCount++;
            TotalSizeBytes += Math.Max(0L, e.sizeBytes);
            var dir = e.directory ?? string.Empty;
            if (!_byDirectory.TryGetValue(dir, out var entries))
            {
                entries = new List<PcServerEventIndexEntry>();
                _byDirectory[dir] = entries;
            }
            entries.Add(e);
        }

        public PcServerEventIndexEntry GetByRelativePath(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcServerEventIndexEntry> GetByDirectory(string directory)
            => _byDirectory.TryGetValue(directory ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcServerEventIndexEntry>)Array.Empty<PcServerEventIndexEntry>();
    }
}
