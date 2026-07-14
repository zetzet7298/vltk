// -----------------------------------------------------------------------------
// VLTK Mobile — PC VNG event source index parser.
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/vng_event
// Imported file: StreamingAssets/Reference/PcVngEvent/vng_event_index.txt
// Catalog only: records Lua source paths, sizes, and sha256 hashes. It never
// executes Lua and does not infer event behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcVngEventIndexParser
    {
        public const string IndexFileName = "vng_event_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int IsLuaCol = 6;
        public const int SizeBytesCol = 7;
        public const int Sha256Col = 8;

        public static List<PcVngEventIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcVngEventIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcVngEventIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    isLua = Bool(cols, IsLuaCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcVngEventIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcVngEventIndexRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            var path = Path.Combine(dir, IndexFileName);
            if (!File.Exists(path)) return reg;
            reg.sourceDirectoryCount = ReadSourceDirectoryCount(path);
            foreach (var entry in ParseFile(path)) reg.Register(entry);
            return reg;
        }

        private static int ReadSourceDirectoryCount(string path)
        {
            foreach (var raw in ReadUtf8Lines(path))
            {
                var marker = "SourceDirectories=";
                var i = raw.IndexOf(marker, StringComparison.Ordinal);
                if (i < 0) continue;
                var start = i + marker.Length;
                var end = raw.IndexOf(';', start);
                var value = end >= 0 ? raw.Substring(start, end - start) : raw.Substring(start);
                return int.TryParse(value.Trim(), out var parsed) ? parsed : 0;
            }
            return 0;
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
    public class PcVngEventIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public long sizeBytes;
        public string sha256;
    }

    public sealed class PcVngEventIndexRegistry
    {
        private readonly List<PcVngEventIndexEntry> _all = new List<PcVngEventIndexEntry>();
        private readonly Dictionary<string, PcVngEventIndexEntry> _byPath = new Dictionary<string, PcVngEventIndexEntry>();
        private readonly Dictionary<string, List<PcVngEventIndexEntry>> _byDirectory = new Dictionary<string, List<PcVngEventIndexEntry>>();
        private readonly HashSet<string> _luaDirectories = new HashSet<string>();

        public int sourceDirectoryCount;
        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int NonLuaFileCount => Count - LuaFileCount;
        public int LuaDirectoryCount => _luaDirectories.Count;
        public int SourceDirectoryCount => sourceDirectoryCount;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcVngEventIndexEntry> All => _all;

        public void Register(PcVngEventIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath) || _byPath.ContainsKey(e.relativePath)) return;
            _all.Add(e);
            _byPath[e.relativePath] = e;
            TotalSizeBytes += e.sizeBytes;
            if (e.isLua)
            {
                LuaFileCount++;
                _luaDirectories.Add(e.directory ?? string.Empty);
            }
            var dir = e.directory ?? string.Empty;
            if (!_byDirectory.TryGetValue(dir, out var entries))
            {
                entries = new List<PcVngEventIndexEntry>();
                _byDirectory[dir] = entries;
            }
            entries.Add(e);
        }

        public PcVngEventIndexEntry GetByRelativePath(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcVngEventIndexEntry> GetByDirectory(string directory)
            => _byDirectory.TryGetValue(directory ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcVngEventIndexEntry>)Array.Empty<PcVngEventIndexEntry>();
    }
}
