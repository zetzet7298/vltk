// -----------------------------------------------------------------------------
// VLTK Mobile — PC activitysys source/config index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/{script,settings}/activitysys
// Imported files: StreamingAssets/Reference/PcActivitySys/activitysys_source_index.txt
// and activitysys_config_index.txt. Catalog only: no Lua execution or activity
// runtime parity claim is made by this index.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcActivitySysIndexParser
    {
        public const string SourceIndexFileName = "activitysys_source_index.txt";
        public const string ConfigIndexFileName = "activitysys_config_index.txt";
        public const int SourceIndexCol = 0;
        public const int IndexKindCol = 1;
        public const int SourceRootCol = 2;
        public const int RelativePathCol = 3;
        public const int DirectoryCol = 4;
        public const int FileNameCol = 5;
        public const int ExtensionCol = 6;
        public const int IsLuaCol = 7;
        public const int IsTextConfigCol = 8;
        public const int SizeBytesCol = 9;
        public const int Sha256Col = 10;

        public static List<PcActivitySysIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcActivitySysIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcActivitySysIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    indexKind = Str(cols, IndexKindCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    isLua = Bool(cols, IsLuaCol),
                    isTextConfig = Bool(cols, IsTextConfigCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.indexKind) && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcActivitySysIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcActivitySysIndexRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            RegisterFile(reg, Path.Combine(dir, SourceIndexFileName));
            RegisterFile(reg, Path.Combine(dir, ConfigIndexFileName));
            return reg;
        }

        private static void RegisterFile(PcActivitySysIndexRegistry reg, string path)
        {
            foreach (var entry in ParseFile(path)) reg.Register(entry);
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
    public class PcActivitySysIndexEntry
    {
        public int sourceIndex;
        public string indexKind;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public bool isTextConfig;
        public long sizeBytes;
        public string sha256;
    }

    public sealed class PcActivitySysIndexRegistry
    {
        private readonly List<PcActivitySysIndexEntry> _all = new List<PcActivitySysIndexEntry>();
        private readonly Dictionary<string, PcActivitySysIndexEntry> _byKindPath = new Dictionary<string, PcActivitySysIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcActivitySysIndexEntry>> _byKindDirectory = new Dictionary<string, List<PcActivitySysIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int SourceFileCount { get; private set; }
        public int ConfigFileCount { get; private set; }
        public int LuaFileCount { get; private set; }
        public int TextConfigFileCount { get; private set; }
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcActivitySysIndexEntry> All => _all;

        public void Register(PcActivitySysIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.indexKind) || string.IsNullOrEmpty(e.relativePath)) return;
            var pathKey = Key(e.indexKind, e.relativePath);
            if (_byKindPath.ContainsKey(pathKey)) return;

            _all.Add(e);
            _byKindPath[pathKey] = e;
            if (IsKind(e, "source")) SourceFileCount++;
            if (IsKind(e, "config")) ConfigFileCount++;
            if (e.isLua) LuaFileCount++;
            if (e.isTextConfig) TextConfigFileCount++;
            TotalSizeBytes += Math.Max(0L, e.sizeBytes);

            var dirKey = Key(e.indexKind, e.directory ?? string.Empty);
            if (!_byKindDirectory.TryGetValue(dirKey, out var entries))
            {
                entries = new List<PcActivitySysIndexEntry>();
                _byKindDirectory[dirKey] = entries;
            }
            entries.Add(e);
        }

        public PcActivitySysIndexEntry GetByRelativePath(string indexKind, string relativePath)
            => !string.IsNullOrEmpty(indexKind) && !string.IsNullOrEmpty(relativePath) && _byKindPath.TryGetValue(Key(indexKind, relativePath), out var entry) ? entry : null;

        public IReadOnlyList<PcActivitySysIndexEntry> GetByDirectory(string indexKind, string directory)
            => _byKindDirectory.TryGetValue(Key(indexKind, directory ?? string.Empty), out var entries) ? entries : (IReadOnlyList<PcActivitySysIndexEntry>)Array.Empty<PcActivitySysIndexEntry>();

        private static string Key(string kind, string value) => (kind ?? string.Empty).Trim() + "\n" + (value ?? string.Empty).Trim();
        private static bool IsKind(PcActivitySysIndexEntry e, string kind) => string.Equals(e.indexKind, kind, StringComparison.OrdinalIgnoreCase);
    }
}
