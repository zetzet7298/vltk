// -----------------------------------------------------------------------------
// VLTK Mobile — Map data parser/registry tests
// Verifies registry Count, GetByXxx filters work correctly with in-memory data.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class MapDataParserTests
    {
        private static PcMapListFullEntry MakeMap(int id, int type) =>
            new PcMapListFullEntry { mapId = id, nameRaw = "Map" + id, type = type };

        [Test]
        public void PcMapListFullRegistry_Count_NonNegative()
        {
            var reg = new PcMapListFullRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapListFullRegistry_Register_DoesNotDuplicate()
        {
            var reg = new PcMapListFullRegistry();
            reg.Register(MakeMap(1, PcMapListFullParser.TypeCity));
            reg.Register(MakeMap(1, PcMapListFullParser.TypeField));
            Assert.AreEqual(1, reg.Count);
            Assert.AreEqual(PcMapListFullParser.TypeField, reg.Get(1).type);
        }

        [Test]
        public void PcMapListFullRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcMapListFullRegistry();
            reg.Register(MakeMap(1, PcMapListFullParser.TypeCity));
            reg.Register(MakeMap(2, PcMapListFullParser.TypeField));
            reg.Register(MakeMap(3, PcMapListFullParser.TypeCity));
            var cities = reg.GetByType(PcMapListFullParser.TypeCity);
            Assert.AreEqual(2, cities.Count);
        }

        [Test]
        public void PcMapListFullRegistry_GetByLevel_FiltersCorrectly()
        {
            var reg = new PcMapListFullRegistry();
            reg.Register(new PcMapListFullEntry { mapId = 1, type = 0, requiredLevel = 10, maxLevel = 50 });
            reg.Register(new PcMapListFullEntry { mapId = 2, type = 0, requiredLevel = 60, maxLevel = 100 });
            reg.Register(new PcMapListFullEntry { mapId = 3, type = 0, requiredLevel = 1, maxLevel = 0 });
            var result = reg.GetByLevel(20);
            Assert.AreEqual(2, result.Count);
        }

        [Test]
        public void PcMapListFullRegistry_GetBattlefieldMaps_EmptyByDefault()
        {
            var reg = new PcMapListFullRegistry();
            Assert.AreEqual(0, reg.GetBattlefieldMaps().Count);
        }

        [Test]
        public void PcMapElementRegistry_Count_NonNegative()
        {
            var reg = new PcMapElementRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapElementRegistry_GetByElement_FiltersCorrectly()
        {
            var reg = new PcMapElementRegistry();
            reg.Register(new PcMapElementEntry { mapId = 1, elementType = 0, power = 50 });
            reg.Register(new PcMapElementEntry { mapId = 2, elementType = 1, power = 30 });
            reg.Register(new PcMapElementEntry { mapId = 3, elementType = 0, power = 70 });
            var metal = reg.GetByElement(0);
            Assert.AreEqual(2, metal.Count);
        }

        [Test]
        public void PcMapRespawnRegistry_Count_NonNegative()
        {
            var reg = new PcMapRespawnRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapRespawnRegistry_GetByMap_FiltersCorrectly()
        {
            var reg = new PcMapRespawnRegistry();
            reg.Register(new PcMapRespawnEntry { mapId = 1, posX = 100, posY = 200, respawnType = 0 });
            reg.Register(new PcMapRespawnEntry { mapId = 2, posX = 300, posY = 400, respawnType = 4 });
            reg.Register(new PcMapRespawnEntry { mapId = 1, posX = 500, posY = 600, respawnType = 3 });
            var list = reg.GetByMap(1);
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void PcMapRespawnRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcMapRespawnRegistry();
            reg.Register(new PcMapRespawnEntry { mapId = 1, respawnType = 4 });
            reg.Register(new PcMapRespawnEntry { mapId = 2, respawnType = 0 });
            var town = reg.GetByType(4);
            Assert.AreEqual(1, town.Count);
        }

        [Test]
        public void PcMapBlockRegistry_Count_NonNegative()
        {
            var reg = new PcMapBlockRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapBlockRegistry_GetByMap_FiltersCorrectly()
        {
            var reg = new PcMapBlockRegistry();
            reg.Register(new PcMapBlockEntry { mapId = 1, blockX = 0, blockY = 0, width = 100, height = 100, blockType = 0 });
            reg.Register(new PcMapBlockEntry { mapId = 2, blockX = 0, blockY = 0, width = 50, height = 50, blockType = 1 });
            var list = reg.GetByMap(1);
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void PcMapBlockRegistry_GetByType_FiltersCorrectly()
        {
            var reg = new PcMapBlockRegistry();
            reg.Register(new PcMapBlockEntry { mapId = 1, blockType = 0 });
            reg.Register(new PcMapBlockEntry { mapId = 2, blockType = 1 });
            reg.Register(new PcMapBlockEntry { mapId = 3, blockType = 0 });
            var trees = reg.GetByType(0);
            Assert.AreEqual(2, trees.Count);
        }

        [Test]
        public void PcMapNpcRespawnRegistry_Count_NonNegative()
        {
            var reg = new PcMapNpcRespawnRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapNpcRespawnRegistry_GetByTemplate_FiltersCorrectly()
        {
            var reg = new PcMapNpcRespawnRegistry();
            reg.Register(new PcMapNpcRespawnEntry { mapId = 1, npcId = 100, npcTemplateId = 50 });
            reg.Register(new PcMapNpcRespawnEntry { mapId = 2, npcId = 101, npcTemplateId = 60 });
            reg.Register(new PcMapNpcRespawnEntry { mapId = 3, npcId = 102, npcTemplateId = 50 });
            var list = reg.GetByTemplate(50);
            Assert.AreEqual(2, list.Count);
        }

        [Test]
        public void PcMapNpcRespawnRegistry_GetByMap_FiltersCorrectly()
        {
            var reg = new PcMapNpcRespawnRegistry();
            reg.Register(new PcMapNpcRespawnEntry { mapId = 1, npcId = 100 });
            reg.Register(new PcMapNpcRespawnEntry { mapId = 2, npcId = 101 });
            var list = reg.GetByMap(1);
            Assert.AreEqual(1, list.Count);
        }

        [Test]
        public void PcMapMusicRegistry_Count_NonNegative()
        {
            var reg = new PcMapMusicRegistry();
            Assert.GreaterOrEqual(reg.Count, 0);
        }

        [Test]
        public void PcMapMusicRegistry_Get_ReturnsNullForInvalid()
        {
            var reg = new PcMapMusicRegistry();
            Assert.IsNull(reg.Get(99999));
        }

        [Test]
        public void PcMapMusicRegistry_Register_AndRetrieve()
        {
            var reg = new PcMapMusicRegistry();
            reg.Register(new PcMapMusicEntry { mapId = 1, musicId = 10, dayMusicId = 20, nightMusicId = 30, battleMusicId = 40 });
            var e = reg.Get(1);
            Assert.IsNotNull(e);
            Assert.AreEqual(20, e.dayMusicId);
        }
        [Test]
        public void PcMapListFullParser_BuildRegistry_ScansSubdirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            try
            {
                // maplist.ini is an INI section, not TSV: "N=path", "N_name=",
                // "N_MapPos=x,y", "N_MapType=City|Field|...".
                File.WriteAllText(Path.Combine(nested, "maplist_test.ini"),
                    "[List]\n1001=Region\\BaLang\n1001_name=Ba Lang\n1001_MapPos=12,34\n1001_MapType=Field\n");
                var reg = PcMapListFullParser.BuildRegistry(root);
                Assert.IsNotNull(reg.Get(1001));
                Assert.AreEqual(PcMapListFullParser.TypeField, reg.Get(1001).type);
                Assert.AreEqual("Ba Lang", reg.Get(1001).nameRaw);
                Assert.AreEqual(12, reg.Get(1001).mapPosX);
                Assert.AreEqual(34, reg.Get(1001).mapPosY);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void PcMapBlockParser_BuildRegistry_ScansSubdirectories()
        {
            string root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string nested = Path.Combine(root, "nested");
            Directory.CreateDirectory(nested);
            try
            {
                File.WriteAllText(Path.Combine(nested, "mapblock_test.txt"), "MapId\tBlockX\tBlockY\tWidth\tHeight\tBlockType\tPassable\n1001\t1\t2\t3\t4\t3\t1\n");
                var reg = PcMapBlockParser.BuildRegistry(root);
                Assert.AreEqual(1, reg.GetByMap(1001).Count);
                Assert.IsTrue(reg.GetByMap(1001)[0].passable);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

    }
}
