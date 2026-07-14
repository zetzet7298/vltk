// -----------------------------------------------------------------------------
// VLTK Mobile — PC Server script/lib source index parser.
// Source of truth: /var/www/jx-source/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/Server/Server/script/lib
// Data-only index of PC Lua library file paths, sizes, and SHA-256 hashes. No Lua runtime claim.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcLibScriptSourceParser
    {
        public const string CatalogFileName = "lib_scripts.txt";
        public const string PcSourceRelativeRoot = "Server 6.0/server/home_jxser/server1/script/lib";
        public const string NoLuaRuntimeClaim = "PcLibScript source index catalogs PC Server script/lib files only; it does not parse or execute Lua runtime behavior.";

        public const int RelativePathCol = 0;
        public const int DirectoryCol = 1;
        public const int FileNameCol = 2;
        public const int FileKindCol = 3;
        public const int IsLuaCol = 4;
        public const int SizeBytesCol = 5;
        public const int Sha256Col = 6;

        public static List<PcLibScriptSourceEntry> ParseFile(string path)
        {
            var rows = new List<PcLibScriptSourceEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            int sourceIndex = 0;
            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, RelativePathCol), "RelativePath", StringComparison.OrdinalIgnoreCase)) continue;

                sourceIndex++;
                var entry = new PcLibScriptSourceEntry
                {
                    sourceIndex = sourceIndex,
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    fileKind = Str(cols, FileKindCol),
                    isLua = Bool(cols, IsLuaCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcLibScriptSourceCatalog BuildCatalog(string dirOrFile)
        {
            var catalog = new PcLibScriptSourceCatalog();
            if (string.IsNullOrEmpty(dirOrFile)) return catalog;
            string path = Directory.Exists(dirOrFile) ? Path.Combine(dirOrFile, CatalogFileName) : dirOrFile;
            if (!File.Exists(path)) return catalog;
            foreach (var entry in ParseFile(path)) catalog.Register(entry);
            return catalog;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var result = new List<string>();
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            result.AddRange(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
            return result;
        }

        private static string Str(string[] cols, int i)
            => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;

        private static long Long(string[] cols, int i)
            => long.TryParse(Str(cols, i), out var value) ? value : 0L;

        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public sealed class PcLibScriptSourceEntry
    {
        public int sourceIndex;
        public string relativePath;
        public string directory;
        public string fileName;
        public string fileKind;
        public bool isLua;
        public long sizeBytes;
        public string sha256;
    }
}
