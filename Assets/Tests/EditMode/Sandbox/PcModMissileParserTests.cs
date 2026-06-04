using System.IO;
using NUnit.Framework;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcModMissileParserTests
    {
        private static string StreamingAssetsPath => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets");
        private static string ModMissilesPath => Path.Combine(StreamingAssetsPath, "Reference/ModMissles.txt");

        [Test]
        public void ParseLines_WithMockData_LoadsPropertiesCorrectly()
        {
            var lines = new[]
            {
                "Id\tName\tCol2\tCol3\tCol4\tCol5\tMinRad\tCol7\tMaxRad\tCol9\tLife\tSpeed\tCol12\tCol13\tCount\tCol15\tCol16\tCol17\tFlyEvt\tCol19\tCollEvt\tVanEvt\tCol22\tCol23\tCol24\tCol25\tCol26\tCol27\tCol28\tSprFile\tCol30\tCol31\tCol32\tCol33",
                "350\tFireBall\t0\t0\t0\t0\t5\t0\t10\t0\t30\t15\t0\t0\t2\t0\t0\t0\t88\t0\t99\t100\t0\t0\t0\t0\t0\t0\t0\t\\spr\\skills\\fire.spr\t0\t0\t0\t0"
            };

            var rows = PcModMissileParser.ParseLines(lines);

            Assert.AreEqual(1, rows.Count);
            var r = rows[0];
            Assert.AreEqual(350, r.missileId);
            Assert.AreEqual("FireBall", r.nameRaw);
            Assert.AreEqual("FireBall", r.nameNormalized);
            Assert.AreEqual(5, r.minRadius);
            Assert.AreEqual(10, r.maxRadius);
            Assert.AreEqual(30, r.lifetime);
            Assert.AreEqual(15, r.speed);
            Assert.AreEqual(2, r.count);
            Assert.AreEqual(88, r.flyEventId);
            Assert.AreEqual(99, r.collideEventId);
            Assert.AreEqual(100, r.vanishEventId);
            Assert.AreEqual("\\spr\\skills\\fire.spr", r.sprFile);
        }

        [Test]
        public void ToMissileEntries_ConvertsRowsToEntries()
        {
            var rows = new System.Collections.Generic.List<PcModMissileRow>
            {
                new PcModMissileRow
                {
                    missileId = 400,
                    nameRaw = "IceShard",
                    nameNormalized = "IceShard",
                    speed = 20,
                    lifetime = 45,
                    count = 3,
                    minRadius = 2,
                    maxRadius = 8,
                    sprFile = "\\spr\\skills\\ice.spr",
                    flyEventId = 1,
                    collideEventId = 2,
                    vanishEventId = 3
                }
            };

            var entries = PcModMissileParser.ToMissileEntries(rows);

            Assert.AreEqual(1, entries.Count);
            var e = entries[0];
            Assert.AreEqual(400, e.missileId);
            Assert.AreEqual("IceShard", e.nameRaw);
            Assert.AreEqual("IceShard", e.nameNormalized);
            Assert.AreEqual(20, e.speed);
            Assert.AreEqual(45, e.lifetime);
            Assert.AreEqual(3, e.count);
            Assert.AreEqual(2, e.minRadius);
            Assert.AreEqual(8, e.maxRadius);
            Assert.AreEqual("\\spr\\skills\\ice.spr", e.sprFile);
            Assert.AreEqual(1, e.flyEventId);
            Assert.AreEqual(2, e.collideEventId);
            Assert.AreEqual(3, e.vanishEventId);
        }

        [Test]
        public void MergeMissilesWithoutOverwriting_PreservesBaseRowsAndAddsNewIds()
        {
            var baseRows = new System.Collections.Generic.List<PcMissileEntry>
            {
                new PcMissileEntry { missileId = 1, nameRaw = "Tứ Tượng Đồng Quy", sprFile = "\\spr\\pc.spr", speed = 12 },
                new PcMissileEntry { missileId = 2, nameRaw = "Thiếu Sprite", sprFile = string.Empty, speed = 20 },
            };
            var fallbackRows = new System.Collections.Generic.List<PcMissileEntry>
            {
                new PcMissileEntry { missileId = 1, nameRaw = "Mojibake", sprFile = "\\spr\\mod-overwrite.spr", speed = 99 },
                new PcMissileEntry { missileId = 2, nameRaw = "Fallback Sprite", sprFile = "\\spr\\mod-fill.spr", speed = 21 },
                new PcMissileEntry { missileId = 500, nameRaw = "Missile mới", sprFile = "\\spr\\mod-new.spr", speed = 30 },
            };

            int added = PcConfigParser.MergeMissilesWithoutOverwriting(baseRows, fallbackRows);

            Assert.AreEqual(1, added, "Only truly new missile ids should be appended");
            Assert.AreEqual(3, baseRows.Count);
            Assert.AreEqual("Tứ Tượng Đồng Quy", baseRows[0].nameRaw, "Fallback must not overwrite localized base row names");
            Assert.AreEqual("\\spr\\pc.spr", baseRows[0].sprFile, "Fallback must not overwrite non-empty PC sprite paths");
            Assert.AreEqual(12, baseRows[0].speed, "Fallback must not overwrite PC gameplay values");
            Assert.AreEqual("\\spr\\mod-fill.spr", baseRows[1].sprFile, "Fallback may fill missing sprite paths");
            Assert.AreEqual(500, baseRows[2].missileId);
        }

        [Test]
        public void ParseFile_LoadsRealModMissiles_IfFileExists()
        {
            if (File.Exists(ModMissilesPath))
            {
                var rows = PcModMissileParser.ParseFile(ModMissilesPath);
                Assert.Greater(rows.Count, 0, "ModMissles.txt should have parsed entries");
            }
        }

        [Test]
        public void PcMissileRegistry_CanResolveMissile()
        {
            // Clear and initialize registry from StreamingAssets
            PcMissileRegistry.ClearAndInitialize(StreamingAssetsPath);

            // Real missiles check
            // Usually, missile 48 is normal fireball / standard missile
            if (PcMissileRegistry.TryGet(48, out var entry))
            {
                Assert.AreEqual(48, entry.missileId);
                Assert.IsFalse(string.IsNullOrEmpty(entry.sprFile), "Base missile 48 sprFile should not be null or empty");
            }

            // Custom mod missile check (id >= 300)
            if (PcMissileRegistry.TryGet(301, out var modEntry))
            {
                Assert.AreEqual(301, modEntry.missileId);
                Assert.IsFalse(string.IsNullOrEmpty(modEntry.sprFile), "Mod missile 301 sprFile should not be null or empty");
            }
        }
    }
}
