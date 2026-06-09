// -----------------------------------------------------------------------------
// VLTK Mobile — PC dailogsys source index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script/dailogsys
// Imported file: StreamingAssets/Reference/PcDialogSys/dialogsys_source_index.txt
// Catalog only: no Lua execution or behavior emulation is claimed.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcDialogSysSourceIndexParser
    {
        public const string IndexFileName = "dialogsys_source_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int FileStemCol = 3;
        public const int ExtensionCol = 4;
        public const int SizeBytesCol = 5;
        public const int Sha256Col = 6;
        public const int IncludeCountCol = 7;
        public const int FunctionCountCol = 8;
        public const int GlobalSymbolCountCol = 9;
        public const int OptionSurfaceCountCol = 10;
        public const int SaySurfaceCountCol = 11;
        public const int IncludesCol = 12;
        public const int FunctionsCol = 13;
        public const int GlobalSymbolsCol = 14;
        public const int OptionSurfacesCol = 15;
        public const int SaySurfacesCol = 16;

        public static List<PcDialogSysSourceIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcDialogSysSourceIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;
            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= GlobalSymbolsCol) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;
                var entry = new PcDialogSysSourceIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    fileStem = Str(cols, FileStemCol),
                    extension = Str(cols, ExtensionCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                    includeCount = Int(cols, IncludeCountCol),
                    functionCount = Int(cols, FunctionCountCol),
                    globalSymbolCount = Int(cols, GlobalSymbolCountCol),
                    optionSurfaceCount = Int(cols, OptionSurfaceCountCol),
                    saySurfaceCount = Int(cols, SaySurfaceCountCol),
                    includes = SplitList(Str(cols, IncludesCol)),
                    functions = SplitList(Str(cols, FunctionsCol)),
                    globalSymbols = SplitList(Str(cols, GlobalSymbolsCol)),
                    representativeOptionSurfaces = SplitList(Str(cols, OptionSurfacesCol)),
                    representativeSaySurfaces = SplitList(Str(cols, SaySurfacesCol)),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcDialogSysSourceIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcDialogSysSourceIndexRegistry();
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return reg;
            foreach (var entry in ParseFile(Path.Combine(dir, IndexFileName))) reg.Register(entry);
            return reg;
        }

        private static List<string> ReadUtf8Lines(string path)
        {
            var text = File.ReadAllText(path, Encoding.UTF8).TrimStart('\ufeff');
            return new List<string>(text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'));
        }

        private static string Str(string[] cols, int i) => cols != null && i >= 0 && i < cols.Length ? (cols[i] ?? string.Empty).Trim() : string.Empty;
        private static int Int(string[] cols, int i) => int.TryParse(Str(cols, i), out var value) ? value : 0;
        private static long Long(string[] cols, int i) => long.TryParse(Str(cols, i), out var value) ? value : 0L;
        private static string[] SplitList(string value) => string.IsNullOrEmpty(value) || value == "<none>" ? Array.Empty<string>() : value.Split('|');
    }
}
