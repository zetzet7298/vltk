// -----------------------------------------------------------------------------
// VLTK Mobile — PC shop-related Lua source index parser.
// Source of truth: /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem only:
//   */server1/script/shop, */server1/script/item/dynamic_shop, */server1/script/item/ib_shop
// Imported file: StreamingAssets/Reference/PcShopScript/shop_script_index.txt
// Data-only catalog: preserves PC Lua file path/size/SHA-256. No Lua execution,
// no shop runtime behavior, and no semantic inference from script names.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcShopScriptIndexParser
    {
        public const string IndexFileName = "shop_script_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int SourceSubdirCol = 2;
        public const int RelativePathCol = 3;
        public const int DirectoryCol = 4;
        public const int FileNameCol = 5;
        public const int ExtensionCol = 6;
        public const int IsLuaCol = 7;
        public const int SizeBytesCol = 8;
        public const int Sha256Col = 9;

        public static List<PcShopScriptIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcShopScriptIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcShopScriptIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    sourceSubdir = Str(cols, SourceSubdirCol),
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

        public static PcShopScriptIndexRegistry BuildRegistry(string dirOrFile)
        {
            var reg = new PcShopScriptIndexRegistry();
            if (string.IsNullOrEmpty(dirOrFile)) return reg;
            var path = Directory.Exists(dirOrFile) ? Path.Combine(dirOrFile, IndexFileName) : dirOrFile;
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
    public sealed class PcShopScriptIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string sourceSubdir;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public long sizeBytes;
        public string sha256;
    }
}
