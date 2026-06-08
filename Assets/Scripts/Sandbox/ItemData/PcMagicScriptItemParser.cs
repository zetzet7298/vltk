// -----------------------------------------------------------------------------
// VLTK Mobile — PC magicscript item-definition parser
// Source: settings/item/004/magicscript.txt (script items, including GM token).
// Keeps PC tuple (ItemGenre/DetailType/ParticularType) so Lua AddItem(6,1,4890)
// resolves to the same mobile item definition.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public static class PcMagicScriptItemParser
    {
        public const int NameCol = 0;
        public const int ItemGenreCol = 1;
        public const int DetailTypeCol = 2;
        public const int ParticularTypeCol = 3;
        public const int SpritePathCol = 4;
        public const int DescriptionCol = 8;
        public const int StackableCol = 12;
        public const int ScriptPathCol = 13;
        public const int MaxStackCol = 20;
        public const string EvidenceNote = "pc_item_004_magicscript";

        public static List<ItemDefinition> ParseFile(string path)
        {
            var rows = new List<ItemDefinition>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return rows;

            var lines = PcItemCommon.ReadServerLines(path);
            bool headerSkipped = false;
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (!headerSkipped) { headerSkipped = true; continue; }

                var cols = line.Split('\t');
                if (cols.Length <= ScriptPathCol) continue;

                var item = ParseRow(cols);
                if (item != null) rows.Add(item);
            }
            return rows;
        }

        public static ItemDefinition ParseRow(string[] cols)
        {
            if (cols == null || cols.Length <= ScriptPathCol) return null;

            int genre = PcItemCommon.Int(cols, ItemGenreCol);
            int detail = PcItemCommon.Int(cols, DetailTypeCol);
            int particular = PcItemCommon.Int(cols, ParticularTypeCol);
            if (genre <= 0 || particular <= 0) return null;
            // Keep the script-item slice used by PC AddItem(6,1,particular).
            if (genre != 6 || detail != 1) return null;

            string name = PcItemCommon.Str(cols, NameCol);
            string desc = PcItemCommon.Str(cols, DescriptionCol);
            string script = PcItemCommon.Str(cols, ScriptPathCol);

            var item = new ItemDefinition
            {
                // Script items are addressed by ParticularType in PC Lua.
                itemId = particular,
                itemGenre = genre,
                detailType = detail,
                particularType = particular,
                nameRaw = name,
                nameNormalized = NormalizeName(genre, detail, particular, name),
                description = NormalizeDescription(genre, detail, particular, desc),
                scriptPath = script,
                iconSourceId = PcItemCommon.BuildIconSourceId(PcItemCommon.Str(cols, SpritePathCol), EvidenceNote),
                iconResolved = false,
                setId = 0,
                refineLevel = 0,
            };

            if (PcItemCommon.Int(cols, StackableCol) != 0 || PcItemCommon.Int(cols, MaxStackCol) > 0)
                item.warnings.Add($"Script item {genre}/{detail}/{particular} stack metadata present");
            return item;
        }

        public static ItemContractImporter ImportInto(string itemFullDir, ItemContractImporter importer = null)
        {
            if (importer == null) importer = new ItemContractImporter();
            string path = FindMagicScriptFile(itemFullDir);
            var bundle = new ItemContractBundle { version = "pc_item_004_magicscript" };
            bundle.items.AddRange(ParseFile(path));
            importer.Import(bundle);
            return importer;
        }

        private static string FindMagicScriptFile(string dir)
        {
            if (string.IsNullOrEmpty(dir)) return string.Empty;
            string direct = Path.Combine(dir, "magicscript.txt");
            if (File.Exists(direct)) return direct;
            if (!Directory.Exists(dir)) return direct;
            var matches = Directory.GetFiles(dir, "magicscript*.txt", SearchOption.TopDirectoryOnly);
            return matches.Length > 0 ? matches[0] : direct;
        }

        private static string NormalizeName(int genre, int detail, int particular, string raw)
        {
            if (genre == 6 && detail == 1 && particular == 4890) return "Lệnh bài GM Test Server";
            if (string.IsNullOrWhiteSpace(raw)) return $"Item_{particular}";
            return raw;
        }

        private static string NormalizeDescription(int genre, int detail, int particular, string raw)
        {
            if (genre == 6 && detail == 1 && particular == 4890)
                return "Lệnh bài này chỉ được GM sử dụng.";
            return raw ?? string.Empty;
        }
    }
}
