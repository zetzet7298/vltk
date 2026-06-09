// -----------------------------------------------------------------------------
// VLTK Mobile — PC battle script source catalog parser.
// Source of truth: /var/www/vltksource_new/vl_update_27/Server 6.0/server/home_jxser/server1/script/battles
// The committed catalog records file paths only: 183 files total, 182 active
// .lua scripts plus one PC backup file (boss/mission.lua.bak). No Lua execution.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcBattleScriptSourceParser
    {
        public const string CatalogFileName = "battle_scripts.txt";
        public const string PcSourceRelativeRoot = "Server 6.0/server/home_jxser/server1/script/battles";

        public const int RelativePathCol = 0;
        public const int DirectoryCol = 1;
        public const int FileNameCol = 2;
        public const int FileKindCol = 3;
        public const int IsActiveLuaCol = 4;

        public static List<PcBattleScriptSourceEntry> ParseFile(string path)
        {
            var rows = new List<PcBattleScriptSourceEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            int sourceIndex = 0;
            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= IsActiveLuaCol) continue;
                if (string.Equals(Str(cols, RelativePathCol), "RelativePath", StringComparison.OrdinalIgnoreCase)) continue;

                sourceIndex++;
                var entry = new PcBattleScriptSourceEntry
                {
                    sourceIndex = sourceIndex,
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    fileKind = Str(cols, FileKindCol),
                    isActiveLua = Bool(cols, IsActiveLuaCol),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }
            return rows;
        }

        public static PcBattleScriptSourceCatalog BuildCatalog(string dirOrFile)
        {
            var catalog = new PcBattleScriptSourceCatalog();
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

        private static int Int(string[] cols, int i)
            => int.TryParse(Str(cols, i), out var value) ? value : 0;

        private static bool Bool(string[] cols, int i)
        {
            var value = Str(cols, i);
            return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
        }
    }

    [Serializable]
    public sealed class PcBattleScriptSourceEntry
    {
        public int sourceIndex;
        public string relativePath;
        public string directory;
        public string fileName;
        public string fileKind;
        public bool isActiveLua;
    }
}
