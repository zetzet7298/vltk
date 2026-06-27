// -----------------------------------------------------------------------------
// VLTK Mobile — PC item batch loader (M5.x item-data port)
// Source: server/settings/item/004/*.txt (12 files, GB2312, tab-separated)
// Purpose: load armor / helm / boot / cuff / belt / ring / amulet / pendant /
// meleeweapon / rangeweapon / horse / potion in a single call and import the
// resulting items into the existing ItemContractImporter. Pure C# (no
// MonoBehaviour) so it is fully EditMode-testable.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using VLTK.Model;

namespace VLTK.Sandbox.ItemData
{
    public sealed class PcItemBatchResult
    {
        public Dictionary<string, int> perFileCounts = new();
        public List<ItemDefinition> items = new();
        public List<string> warnings = new();
        public int totalLoaded;
    }

    public static class PcItemBatchLoader
    {
        public const int CategoryBase = 1;
        public const int CategoryStride = 100000;

        public const string ArmorFile = "armor.txt";
        public const string HelmFile = "helm.txt";
        public const string BootFile = "boot.txt";
        public const string CuffFile = "cuff.txt";
        public const string BeltFile = "belt.txt";
        public const string RingFile = "ring.txt";
        public const string AmuletFile = "amulet.txt";
        public const string PendantFile = "pendant.txt";
        public const string MeleeWeaponFile = "meleeweapon.txt";
        public const string RangeWeaponFile = "rangeweapon.txt";
        public const string HorseFile = "horse.txt";
        public const string PotionFile = "potion.txt";
        public const string GoldEquipFile = "goldequip.txt";
        public const string PlatinaEquipFile = "platinaequip.txt";

        private static readonly (string key, string stem)[] CategoryStems =
        {
            ("armor", "armor"),
            ("helm", "helm"),
            ("boot", "boot"),
            ("cuff", "cuff"),
            ("belt", "belt"),
            ("ring", "ring"),
            ("amulet", "amulet"),
            ("pendant", "pendant"),
            ("meleeweapon", "meleeweapon"),
            ("rangeweapon", "rangeweapon"),
            ("horse", "horse"),
            ("potion", "potion"),
            ("goldequip", "goldequip"),
            ("platinaequip", "platinaequip"),
        };

        public static PcItemBatchResult LoadAll(string itemDir)
        {
            var result = new PcItemBatchResult();
            if (string.IsNullOrEmpty(itemDir))
            {
                result.warnings.Add("Empty item directory path");
                return result;
            }
            if (!Directory.Exists(itemDir))
            {
                result.warnings.Add($"Item directory not found: {itemDir}");
                return result;
            }

            for (int i = 0; i < CategoryStems.Length; i++)
            {
                var (key, stem) = CategoryStems[i];
                int categoryId = (CategoryBase + i) * CategoryStride;
                string path = FindFile(itemDir, stem);
                var items = ParseForStem(stem, path);
                ApplyCategoryIds(items, categoryId, stem);
                Append(result, key, items);
            }
            return result;
        }

        public static ItemContractImporter ImportInto(string itemDir, ItemContractImporter importer = null)
        {
            if (importer == null) importer = new ItemContractImporter();
            var batch = LoadAll(itemDir);
            var bundle = new ItemContractBundle { version = "pc_item_004" };
            bundle.items.AddRange(batch.items);
            importer.Import(bundle);
            return importer;
        }

        private static List<ItemDefinition> ParseForStem(string stem, string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return new List<ItemDefinition>();
            switch (stem)
            {
                case "armor": return PcArmorParser.ParseFile(path);
                case "helm": return PcHelmParser.ParseFile(path);
                case "boot": return PcBootParser.ParseFile(path);
                case "cuff": return PcCuffParser.ParseFile(path);
                case "belt": return PcBeltParser.ParseFile(path);
                case "ring": return PcRingParser.ParseFile(path);
                case "amulet": return PcAmuletParser.ParseFile(path);
                case "pendant": return PcPendantParser.ParseFile(path);
                case "meleeweapon": return PcMeleeWeaponParser.ParseFile(path);
                case "rangeweapon": return PcRangeWeaponParser.ParseFile(path);
                case "horse": return PcHorseParser.ParseFile(path);
                case "potion": return PcPotionParser.ParseFile(path);
                case "goldequip": return PcGoldEquipParser.ParseFile(path);
                case "platinaequip": return PcPlatinaEquipParser.ParseFile(path);
                default: return new List<ItemDefinition>();
            }
        }

        internal static void ApplyCategoryIds(List<ItemDefinition> items, int categoryId, string stem)
        {
            if (items == null) return;
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                if (it == null) continue;
                it.itemId = categoryId + (i + 1);

                // particularType = 1-based row index within the category file (for internal use)
                it.particularType = it.particularType != 0 ? it.particularType : (i + 1);

                // itemGenre/detailType: parsers now read from cols 1/2/3 (verified from PC source).
                // Only apply fallback for stems that don't have a full header (goldequip/platinaequip handled separately).
                // PC source verified detailType per file:
                //   helm.txt=7, armor.txt=2, ring.txt=3, pendant.txt=9, cuff.txt=8, belt.txt=6,
                //   boot.txt=5, amulet.txt=4, meleeweapon.txt=0, rangeweapon.txt=1, horse.txt=10
                if (it.itemGenre == 0 && it.detailType == 0 && stem != "goldequip" && stem != "platinaequip")
                {
                    // Fallback only if parser didn't set detailType (e.g. helm all-0 rows)
                    it.detailType = stem switch
                    {
                        "helm"        => 7,
                        "armor"       => 2,
                        "ring"        => 3,
                        "pendant"     => 9,  // Hộ Thân Phù — equip_pendant (D9)
                        "cuff"        => 8,  // Tay (Bracers)
                        "belt"        => 6,  // Đai lưng
                        "boot"        => 5,  // Giày
                        "amulet"      => 4,  // Phù
                        "meleeweapon" => 0,  // Vũ khí cận chiến
                        "rangeweapon" => 1,  // Vũ khí tầm xa
                        "horse"       => 10, // Thú cưỡi
                        _             => 0
                    };
                }

                if (stem == "potion")
                {
                    it.itemGenre = 4; // Tiêu hao / Dược phẩm
                    it.detailType = 1;
                }
            }
        }

        private static void Append(PcItemBatchResult result, string key, List<ItemDefinition> items)
        {
            if (items == null)
            {
                result.perFileCounts[key] = 0;
                return;
            }
            result.perFileCounts[key] = items.Count;
            result.totalLoaded += items.Count;
            result.items.AddRange(items);
        }

        private static string FindFile(string dir, string stem)
        {
            string direct = Path.Combine(dir, stem + ".txt");
            if (File.Exists(direct)) return direct;
            var matches = Directory.GetFiles(dir, stem + "*.txt", SearchOption.TopDirectoryOnly);
            return matches.Length > 0 ? matches[0] : direct;
        }
    }
}
