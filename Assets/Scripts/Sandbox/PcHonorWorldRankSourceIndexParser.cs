// -----------------------------------------------------------------------------
// VLTK Mobile — PC honor/worldrank source index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27 canonical home_jxser
// script/honor, script/global/worldrank, and ranksetting.txt evidence files.
// Imported file: StreamingAssets/Reference/PcHonorWorldRank/honor_worldrank_source_index.txt
// Catalog only: counts and provenance for PC source files. It does not execute or
// infer Lua honor/worldrank semantics, rankings, titles, rewards, or runtime UI.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcHonorWorldRankSourceIndexParser
    {
        public const string IndexFileName = "honor_worldrank_source_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int CategoryCol = 6;
        public const int IsLuaCol = 7;
        public const int IsSettingsCol = 8;
        public const int SizeBytesCol = 9;
        public const int Sha256Col = 10;

        public static List<PcHonorWorldRankSourceIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcHonorWorldRankSourceIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcHonorWorldRankSourceIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    category = Str(cols, CategoryCol),
                    isLua = Bool(cols, IsLuaCol),
                    isSettings = Bool(cols, IsSettingsCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.sourceRoot) && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcHonorWorldRankSourceIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcHonorWorldRankSourceIndexRegistry();
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
    public class PcHonorWorldRankSourceIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public string category;
        public bool isLua;
        public bool isSettings;
        public long sizeBytes;
        public string sha256;
    }

    public sealed class PcHonorWorldRankSourceIndexRegistry
    {
        private readonly List<PcHonorWorldRankSourceIndexEntry> _all = new List<PcHonorWorldRankSourceIndexEntry>();
        private readonly Dictionary<string, PcHonorWorldRankSourceIndexEntry> _bySourcePath = new Dictionary<string, PcHonorWorldRankSourceIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcHonorWorldRankSourceIndexEntry>> _byCategory = new Dictionary<string, List<PcHonorWorldRankSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcHonorWorldRankSourceIndexEntry>> _bySourceRoot = new Dictionary<string, List<PcHonorWorldRankSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int HonorLuaFileCount { get; private set; }
        public int WorldRankLuaFileCount { get; private set; }
        public int SettingsFileCount { get; private set; }
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> All => _all;

        public void Register(PcHonorWorldRankSourceIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.sourceRoot) || string.IsNullOrEmpty(e.relativePath)) return;
            var sourcePath = MakeSourcePath(e.sourceRoot, e.relativePath);
            if (_bySourcePath.ContainsKey(sourcePath)) return;

            _all.Add(e);
            _bySourcePath[sourcePath] = e;
            if (e.isLua) LuaFileCount++;
            if (IsCategory(e, "honor") && e.isLua) HonorLuaFileCount++;
            if (IsCategory(e, "worldrank") && e.isLua) WorldRankLuaFileCount++;
            if (e.isSettings) SettingsFileCount++;
            TotalSizeBytes += Math.Max(0L, e.sizeBytes);
            AddToGroup(_byCategory, e.category ?? string.Empty, e);
            AddToGroup(_bySourceRoot, e.sourceRoot ?? string.Empty, e);
        }

        public PcHonorWorldRankSourceIndexEntry GetBySourcePath(string sourceRoot, string relativePath)
            => _bySourcePath.TryGetValue(MakeSourcePath(sourceRoot, relativePath), out var entry) ? entry : null;

        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> GetByCategory(string category)
            => _byCategory.TryGetValue(category ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcHonorWorldRankSourceIndexEntry>)Array.Empty<PcHonorWorldRankSourceIndexEntry>();

        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> GetBySourceRoot(string sourceRoot)
            => _bySourceRoot.TryGetValue(sourceRoot ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcHonorWorldRankSourceIndexEntry>)Array.Empty<PcHonorWorldRankSourceIndexEntry>();

        private static bool IsCategory(PcHonorWorldRankSourceIndexEntry e, string category)
            => string.Equals(e.category, category, StringComparison.OrdinalIgnoreCase);

        private static string MakeSourcePath(string sourceRoot, string relativePath)
            => (sourceRoot ?? string.Empty).TrimEnd('/', '\\') + "/" + (relativePath ?? string.Empty).TrimStart('/', '\\');

        private static void AddToGroup(Dictionary<string, List<PcHonorWorldRankSourceIndexEntry>> groups, string key, PcHonorWorldRankSourceIndexEntry entry)
        {
            if (!groups.TryGetValue(key, out var entries))
            {
                entries = new List<PcHonorWorldRankSourceIndexEntry>();
                groups[key] = entries;
            }
            entries.Add(entry);
        }
    }
}
