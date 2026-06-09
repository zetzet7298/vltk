// -----------------------------------------------------------------------------
// VLTK Mobile — PC partner/pet source index parser.
// Source of truth: /var/www/vltksource_new/vl_update_27 scoped to settings/partner,
// settings/petsys, and obvious partner/pet Lua script roots.
// Imported file: StreamingAssets/Reference/PcPartnerPet/partner_pet_source_index.txt
// Catalog only: preserves file evidence (counts, sizes, SHA-256) and does not
// execute or infer partner/pet runtime behavior.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VLTK.Sandbox
{
    public static class PcPartnerPetSourceIndexParser
    {
        public const string IndexFileName = "partner_pet_source_index.txt";
        public const int SourceIndexCol = 0;
        public const int SourceRootCol = 1;
        public const int RelativePathCol = 2;
        public const int DirectoryCol = 3;
        public const int FileNameCol = 4;
        public const int ExtensionCol = 5;
        public const int CategoryCol = 6;
        public const int IsConfigCol = 7;
        public const int IsLuaCol = 8;
        public const int SizeBytesCol = 9;
        public const int Sha256Col = 10;

        public static List<PcPartnerPetSourceIndexEntry> ParseFile(string path)
        {
            var rows = new List<PcPartnerPetSourceIndexEntry>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            foreach (var raw in ReadUtf8Lines(path))
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                var line = raw.TrimEnd();
                if (line.StartsWith("#", StringComparison.Ordinal) || line.StartsWith("//", StringComparison.Ordinal)) continue;
                var cols = line.Split('\t');
                if (cols.Length <= Sha256Col) continue;
                if (string.Equals(Str(cols, SourceIndexCol), "SourceIndex", StringComparison.OrdinalIgnoreCase)) continue;

                var entry = new PcPartnerPetSourceIndexEntry
                {
                    sourceIndex = Int(cols, SourceIndexCol),
                    sourceRoot = Str(cols, SourceRootCol),
                    relativePath = Str(cols, RelativePathCol),
                    directory = Str(cols, DirectoryCol),
                    fileName = Str(cols, FileNameCol),
                    extension = Str(cols, ExtensionCol),
                    category = Str(cols, CategoryCol),
                    isConfig = Bool(cols, IsConfigCol),
                    isLua = Bool(cols, IsLuaCol),
                    sizeBytes = Long(cols, SizeBytesCol),
                    sha256 = Str(cols, Sha256Col),
                };
                if (entry.sourceIndex > 0 && !string.IsNullOrEmpty(entry.relativePath)) rows.Add(entry);
            }

            return rows;
        }

        public static PcPartnerPetSourceIndexRegistry BuildRegistry(string dir)
        {
            var reg = new PcPartnerPetSourceIndexRegistry();
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
