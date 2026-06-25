using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    // Fix #14 (2026-06-12): PC rare.txt is a RARE MAGIC-ATTRIBUTE / WEAPON-ENCHANT
    // ROLL TABLE, not an NPC spawn table. Verified against PC source:
    //   /var/www/vltksource_new/01_tinh_kiem_source/source/00.src-tinh-kiem/bin/client/settings/rare.txt
    //   /var/www/vltksource_new/.../itemexchange_setting/rare.txt   (byte-identical)
    // Real header (29 cols): NAME, MAGIC_ID, MAG_P1_MIN, MAG_P1_MAX, SWORD, BLADE,
    // WAND, SPEAR, HAMMER, DUALBLADES, DARTS, KNIFE, CROSSBOW, ARMOR, RING,
    // NECKLACE, AMULET, BOOT, BELT, HELM, CUFF, SACHET, PENDANT, METAL, WOOD,
    // WATER, FIRE, EARTH, 11.
    // These tests assert the corrected PcRareEnchantParser schema. No spawn/NPC
    // fields (npcId/mapId/posX/posY/respawn/probability) exist in the source.
    public class PcRareEnchantParserTests
    {
        private static string SamplePath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/rare_sample.txt");

        private static string FullPath => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcNpc/rare.txt");

        [Test]
        public void ParseFile_LoadsFiveSampleRows()
        {
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareEnchantParser.ParseFile(SamplePath);

            // rare_sample.txt has 1 header + 5 data rows.
            Assert.AreEqual(5, rows.Count, "Expected exactly 5 enchant rows from the sample");
        }

        [Test]
        public void ParseFile_NameRawIsNonEmpty()
        {
            // NAME is the magic-attribute name (col 0). Every data row must have one.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareEnchantParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(r.nameRaw),
                    $"NAME (col 0) must be non-empty (magicId={r.magicId})");
            }
        }

        [Test]
        public void ParseFile_MagicIdIsPositive()
        {
            // MAGIC_ID (col 1) keys into magicattrib*.txt and must be > 0.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareEnchantParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.Greater(r.magicId, 0, $"name='{r.nameRaw}' magicId must be > 0");
            }
        }

        [Test]
        public void ParseFile_ParamRangeIsOrdered()
        {
            // MAG_P1_MIN (col 2) <= MAG_P1_MAX (col 3) for every tier.
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareEnchantParser.ParseFile(SamplePath);

            foreach (var r in rows)
            {
                Assert.LessOrEqual(r.magP1Min, r.magP1Max,
                    $"name='{r.nameRaw}' magicId={r.magicId}: MAG_P1_MIN must be <= MAG_P1_MAX");
            }
        }

        [Test]
        public void ParseFile_FirstSampleRowMatchesKnownColumns()
        {
            // Pin the exact column mapping against the first committed sample row:
            //   AddDmgLvl1  126  5  10  2500 2500 2500 3000 2500 2500 2500 2500 2500
            //   0(ARMOR..PENDANT)  100 80 100 30 30  0
            if (!File.Exists(SamplePath))
            {
                Assert.Inconclusive($"Sample file not found at {SamplePath}");
                return;
            }

            var rows = PcRareEnchantParser.ParseFile(SamplePath);
            Assert.GreaterOrEqual(rows.Count, 1, "Need at least one row to verify columns");
            var r0 = rows[0];

            Assert.AreEqual("AddDmgLvl1", r0.nameRaw, "col 0 NAME");
            Assert.AreEqual(126, r0.magicId, "col 1 MAGIC_ID");
            Assert.AreEqual(5, r0.magP1Min, "col 2 MAG_P1_MIN");
            Assert.AreEqual(10, r0.magP1Max, "col 3 MAG_P1_MAX");

            // Weapon weights (col 4..12)
            Assert.AreEqual(2500, r0.wSword, "col 4 SWORD");
            Assert.AreEqual(2500, r0.wBlade, "col 5 BLADE");
            Assert.AreEqual(2500, r0.wWand, "col 6 WAND");
            Assert.AreEqual(3000, r0.wSpear, "col 7 SPEAR");
            Assert.AreEqual(2500, r0.wHammer, "col 8 HAMMER");
            Assert.AreEqual(2500, r0.wDualBlades, "col 9 DUALBLADES");
            Assert.AreEqual(2500, r0.wDarts, "col 10 DARTS");
            Assert.AreEqual(2500, r0.wKnife, "col 11 KNIFE");
            Assert.AreEqual(2500, r0.wCrossbow, "col 12 CROSSBOW");

            // Equipment-slot weights (col 13..22) — all 0 in this row.
            Assert.AreEqual(0, r0.wArmor, "col 13 ARMOR");
            Assert.AreEqual(0, r0.wRing, "col 14 RING");
            Assert.AreEqual(0, r0.wNecklace, "col 15 NECKLACE");
            Assert.AreEqual(0, r0.wAmulet, "col 16 AMULET");
            Assert.AreEqual(0, r0.wBoot, "col 17 BOOT");
            Assert.AreEqual(0, r0.wBelt, "col 18 BELT");
            Assert.AreEqual(0, r0.wHelm, "col 19 HELM");
            Assert.AreEqual(0, r0.wCuff, "col 20 CUFF");
            Assert.AreEqual(0, r0.wSachet, "col 21 SACHET");
            Assert.AreEqual(0, r0.wPendant, "col 22 PENDANT");

            // Element weights (col 23..27)
            Assert.AreEqual(100, r0.wMetal, "col 23 METAL");
            Assert.AreEqual(80, r0.wWood, "col 24 WOOD");
            Assert.AreEqual(100, r0.wWater, "col 25 WATER");
            Assert.AreEqual(30, r0.wFire, "col 26 FIRE");
            Assert.AreEqual(30, r0.wEarth, "col 27 EARTH");

            // Trailing "11" column (col 28) — 0 across all observed data.
            Assert.AreEqual(0, r0.rawTrailing, "col 28 trailing");
        }

        [Test]
        public void BuildTable_GroupsByMagicId()
        {
            // BuildTable scans rare*.txt in the dir and indexes by MAGIC_ID.
            var dir = Path.GetDirectoryName(FullPath);
            if (!File.Exists(FullPath))
            {
                Assert.Inconclusive($"Full rare.txt not found at {FullPath}");
                return;
            }

            var table = PcRareEnchantParser.BuildTable(dir);

            Assert.Greater(table.Count, 0, "Table should load committed rows");
            Assert.Greater(table.MagicIdCount, 0, "Table should index at least one MAGIC_ID");

            // Every row's magicId must be retrievable via GetByMagicId.
            var firstId = table.All.First().magicId;
            var tiers = table.GetByMagicId(firstId);
            Assert.IsNotEmpty(tiers, $"GetByMagicId({firstId}) must return its tiers");
            Assert.IsTrue(tiers.All(t => t.magicId == firstId),
                "GetByMagicId must only return rows for that MAGIC_ID");
        }

        [Test]
        public void ParseFile_SkipsShortRows()
        {
            // A row with fewer than 29 columns is malformed and must be skipped,
            // not partially parsed. (Internal to ParseFile; verified via behavior.)
            string tmp = Path.Combine(Path.GetTempPath(), "rare_short_row_test.txt");
            File.WriteAllText(tmp,
                "NAME\tMAGIC_ID\tMAG_P1_MIN\tMAG_P1_MAX\tSWORD\n" + // header (short, ignored anyway)
                "Broken\t1\t2\t3\n");                                // 4 cols only -> skipped
            try
            {
                var rows = PcRareEnchantParser.ParseFile(tmp);
                Assert.AreEqual(0, rows.Count, "Rows with < 29 columns must be skipped");
            }
            finally
            {
                if (File.Exists(tmp)) File.Delete(tmp);
            }
        }
    }
}
