// -----------------------------------------------------------------------------
// VLTK Mobile — PC skill-goc source index parser.
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/server/home_jxser_bachkim_6.0/server1/script/skill-goc
// Imported file: StreamingAssets/Reference/PcSkillGocScript/skill_goc_source_index.txt
// Catalog only: preserves source file evidence (path/size/SHA) without executing
// Lua or inferring combat/runtime semantics.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcSkillGocScriptIndexParser
    {
        public const string IndexFileName = "skill_goc_source_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int IsLuaCol = 6;
        public const int SizeBytesCol = 7;
        public const int Sha256Col = 8;

        public static List<PcSkillGocScriptIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcSkillGocScriptIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;
                var entry = new PcSkillGocScriptIndexEntry
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

        public static PcSkillGocScriptIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcSkillGocScriptIndexRegistry();
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
        private static int Int(string[] cols, int i) => int.TryParse(Str(cols, i), out var value) ? value : 0;
        private static long Long(string[] cols, int i) => long.TryParse(Str(cols, i), out var value) ? value : 0L;
        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || value.Equals("true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public class PcSkillGocScriptIndexEntry
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

    public sealed class PcSkillGocScriptIndexRegistry
    {
        private readonly List<PcSkillGocScriptIndexEntry> _all = new();
        private readonly Dictionary<string, PcSkillGocScriptIndexEntry> _byRelativePath = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcSkillGocScriptIndexEntry>> _byDirectory = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _directories = new(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int NonLuaFileCount => Count - LuaFileCount;
        public int DirectoryCount => _directories.Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcSkillGocScriptIndexEntry> All => _all;

        public void Register(PcSkillGocScriptIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath)) return;
            _all.Add(e);
            _byRelativePath[e.relativePath] = e;
            if (e.isLua) LuaFileCount++;
            TotalSizeBytes += e.sizeBytes;
            RegisterDirectoryPrefixes(e.directory);
            if (!_byDirectory.TryGetValue(e.directory ?? string.Empty, out var bucket))
                _byDirectory[e.directory ?? string.Empty] = bucket = new List<PcSkillGocScriptIndexEntry>();
            bucket.Add(e);
        }

        public PcSkillGocScriptIndexEntry GetByRelativePath(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byRelativePath.TryGetValue(relativePath, out var v) ? v : null;

        public IReadOnlyList<PcSkillGocScriptIndexEntry> GetByDirectory(string directory)
            => _byDirectory.TryGetValue(directory ?? string.Empty, out var list) ? list : Array.Empty<PcSkillGocScriptIndexEntry>();

        private void RegisterDirectoryPrefixes(string directory)
        {
            if (string.IsNullOrEmpty(directory)) return;
            var parts = directory.Split('/');
            var current = string.Empty;
            foreach (var part in parts)
            {
                current = string.IsNullOrEmpty(current) ? part : current + "/" + part;
                _directories.Add(current);
            }
        }
    }
}
