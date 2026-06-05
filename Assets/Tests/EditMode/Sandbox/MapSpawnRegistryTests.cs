using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MapSpawnRegistryTests
    {
        [Test]
        public void EmptyRegistry_HasZeroCount()
        {
            var registry = new MapSpawnRegistry();
            Assert.AreEqual(0, registry.TotalCount);
            Assert.AreEqual(0, registry.TemplateCount);
            Assert.AreEqual(0, registry.CountForMap(0));
            Assert.AreEqual(0, registry.CountForMap(79));
        }

        [Test]
        public void Register_AddsToAllAndByTemplate()
        {
            var registry = new MapSpawnRegistry();
            var p = new SpawnPoint { npcTemplateId = 31, level = 10, nameRaw = "金猫" };
            registry.Register(p);

            Assert.AreEqual(1, registry.TotalCount);
            Assert.AreEqual(1, registry.TemplateCount);
            Assert.AreEqual(1, registry.CountForTemplate(31));
        }

        [Test]
        public void TryGetByTemplateId_ReturnsFirstEntry()
        {
            var registry = new MapSpawnRegistry();
            registry.Register(new SpawnPoint { npcTemplateId = 31, level = 10, nameRaw = "first" });
            registry.Register(new SpawnPoint { npcTemplateId = 31, level = 11, nameRaw = "second" });

            Assert.IsTrue(registry.TryGetByTemplateId(31, out var got));
            Assert.AreEqual("first", got.nameRaw);
            Assert.AreEqual(2, registry.GetAllByTemplateId(31).Count);
        }

        [Test]
        public void TryGetByTemplateId_MissingReturnsFalse()
        {
            var registry = new MapSpawnRegistry();
            Assert.IsFalse(registry.TryGetByTemplateId(9999, out var got));
            Assert.IsNull(got);
        }

        [Test]
        public void GetSpawnsForMap_AlwaysEmpty_BecauseSourceHasNoMapColumn()
        {
            var registry = new MapSpawnRegistry();
            registry.Register(new SpawnPoint { npcTemplateId = 1, level = 1, nameRaw = "a" });
            registry.Register(new SpawnPoint { npcTemplateId = 2, level = 1, nameRaw = "b" });
            Assert.AreEqual(0, registry.CountForMap(79));
            Assert.AreEqual(0, registry.GetSpawnsForMap(79).Count());
        }

        [Test]
        public void Load_AddsAllPoints()
        {
            var registry = new MapSpawnRegistry();
            var pts = new List<SpawnPoint>
            {
                new SpawnPoint { npcTemplateId = 1, level = 5, nameRaw = "a" },
                new SpawnPoint { npcTemplateId = 2, level = 6, nameRaw = "b" },
                new SpawnPoint { npcTemplateId = 3, level = 7, nameRaw = "c" },
            };
            registry.Load(pts);
            Assert.AreEqual(3, registry.TotalCount);
            Assert.AreEqual(3, registry.TemplateCount);
        }

        [Test]
        public void Load_NullIsNoOp()
        {
            var registry = new MapSpawnRegistry();
            registry.Load(null);
            Assert.AreEqual(0, registry.TotalCount);
        }

        [Test]
        public void Register_NullIsNoOp()
        {
            var registry = new MapSpawnRegistry();
            registry.Register(null);
            Assert.AreEqual(0, registry.TotalCount);
        }

        [Test]
        public void Clear_RemovesAll()
        {
            var registry = new MapSpawnRegistry();
            registry.Register(new SpawnPoint { npcTemplateId = 1 });
            registry.Register(new SpawnPoint { npcTemplateId = 2 });
            registry.Clear();
            Assert.AreEqual(0, registry.TotalCount);
            Assert.AreEqual(0, registry.TemplateCount);
        }

        [Test]
        public void SampleRegistry_RealSampleHasPositiveCounts()
        {
            var rows = ReadSample();
            if (rows == null) { Assert.Inconclusive("sample not available"); return; }

            var registry = new MapSpawnRegistry();
            registry.Load(rows);

            Assert.AreEqual(20, registry.TotalCount, "Sample contributes 20 entries");
            var distinctTemplates = registry.AllSpawns.Select(p => p.npcTemplateId).Distinct().Count();
            Assert.AreEqual(distinctTemplates, registry.TemplateCount, "TemplateCount must equal distinct templateIds");
        }

        [Test]
        public void SampleRegistry_LookupByTemplateIdReturnsKnownEntry()
        {
            var rows = ReadSample();
            if (rows == null) { Assert.Inconclusive("sample not available"); return; }

            var registry = new MapSpawnRegistry();
            registry.Load(rows);

            var firstId = rows[0].npcTemplateId;
            Assert.IsTrue(registry.TryGetByTemplateId(firstId, out var got));
            Assert.AreEqual(firstId, got.npcTemplateId);
            Assert.AreEqual(rows[0].level, got.level);
        }

        [Test]
        public void SampleRegistry_CountForMapIsAlwaysZero()
        {
            var rows = ReadSample();
            if (rows == null) { Assert.Inconclusive("sample not available"); return; }

            var registry = new MapSpawnRegistry();
            registry.Load(rows);

            Assert.AreEqual(0, registry.CountForMap(79));
            Assert.AreEqual(0, registry.CountForMap(0));
        }

        private static List<SpawnPoint> ReadSample()
        {
            var dataPath = System.IO.Path.Combine(Application.dataPath,
                "StreamingAssets", "Reference", "PcSpawn", "normal_sample.txt");
            if (!System.IO.File.Exists(dataPath)) return null;
            return PcNormalSpawnParser.ParseFile(dataPath);
        }
    }
}
