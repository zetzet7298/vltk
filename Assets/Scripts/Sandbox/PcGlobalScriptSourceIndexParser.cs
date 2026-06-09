// -----------------------------------------------------------------------------
// VLTK Mobile — PC global script source index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script/global
// Imported file: StreamingAssets/Reference/PcGlobalScript/global_script_index.txt
// Catalog only: paths, file sizes, SHA-256, and counts. No Lua runtime semantics.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcGlobalScriptSourceIndexParser
    {
        public const string IndexFileName = "global_script_index.txt";
        public const int SourceIndexCol = 0;
        public const int EntryKindCol = 1;
        public const int SourceRootCol = 2;
        public const int RelativePathCol = 3;
        public const int DirectoryCol = 4;
        public const int FileNameCol = 5;
        public const int ExtensionCol = 6;
        public const int IsLuaCol = 7;
        public const int SizeBytesCol = 8;
        public const int Sha256Col = 9;

        public static List<PcGlobalScriptSourceIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcGlobalScriptSourceIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcGlobalScriptSourceIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    entryKind = Str(cols, EntryKindCol),
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

        public static PcGlobalScriptSourceIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcGlobalScriptSourceIndexRegistry();
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
}
