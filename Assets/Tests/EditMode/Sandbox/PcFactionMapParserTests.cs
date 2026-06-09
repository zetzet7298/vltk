using System.IO;
using System.Linq;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcFactionMapParserTests
    {
        private static string SourceFile => Path.Combine(
            Directory.GetCurrentDirectory(),
            "Assets/StreamingAssets/Reference/PcTong/faction_map.txt");

        [Test]
        public void ImportedFile_ParsesExactPcTongMapTableRows()
        {
            Assert.IsTrue(File.Exists(SourceFile));
            var rows = PcFactionMapParser.ParseFile(SourceFile);

            Assert.AreEqual(33, rows.Count,
                "33 rows come from PC script/tong/addtongnpc.lua tables: aPublicMap(4), aDynMapCopyName(7), citymap(11), buildingmap(4), jijiu_city(7).");
            Assert.AreEqual(4, rows.Count(r => r.sourceTable == "aPublicMap"));
            Assert.AreEqual(7, rows.Count(r => r.sourceTable == "aDynMapCopyName"));
            Assert.AreEqual(11, rows.Count(r => r.sourceTable == "citymap"));
            Assert.AreEqual(4, rows.Count(r => r.sourceTable == "buildingmap"));
            Assert.AreEqual(7, rows.Count(r => r.sourceTable == "jijiu_city"));
        }

        [Test]
        public void DynamicTemplateRows_PreservePcMapNamesAndEnterPositions()
        {
            var rows = PcFactionMapParser.ParseFile(SourceFile);

            var giangNam = rows.Single(r => r.sourceTable == "aDynMapCopyName" && r.mapId == 593);
            Assert.AreEqual("Giang Nam", giangNam.mapNameRaw);
            Assert.AreEqual("dynamic_template", giangNam.mapKind);
            Assert.AreEqual(1718, giangNam.enterX);
            Assert.AreEqual(3313, giangNam.enterY);
            Assert.AreEqual(10, giangNam.requiredLevel);

            var bienAi = rows.Single(r => r.sourceTable == "aDynMapCopyName" && r.mapId == 591);
            Assert.AreEqual(1712, bienAi.enterX, "PC aMapEnterPos overrides map copy 591 only.");
            Assert.AreEqual(3330, bienAi.enterY);
        }

        [Test]
        public void CityAltarRows_PreservePcNpcCoordinates()
        {
            var rows = PcFactionMapParser.ParseFile(SourceFile);

            var lamAn = rows.Single(r => r.sourceTable == "jijiu_city" && r.mapId == 176);
            Assert.AreEqual("city_altar_npc_map", lamAn.mapKind);
            Assert.AreEqual(329, lamAn.npcTemplateId);
            Assert.AreEqual(1561, lamAn.npcX);
            Assert.AreEqual(2942, lamAn.npcY);
            Assert.AreEqual(@"\\script\\tong\\npc\\jitan.lua", lamAn.npcScriptRaw);
            Assert.IsTrue(lamAn.HasNpcPosition);
        }

        [Test]
        public void Service_LoadFromStreamingAssets_IndexesRowsAndSourceTables()
        {
            var service = FactionMapService.LoadFromStreamingAssets();

            Assert.AreEqual(33, service.Count);
            Assert.AreEqual(7, service.GetBySourceTable("aDynMapCopyName").Count);
            Assert.AreEqual(11, service.GetBySourceTable("citymap").Count);

            var map591 = service.GetMap(591);
            Assert.IsNotNull(map591);
            Assert.AreEqual("aDynMapCopyName", map591.sourceTable);
            Assert.AreEqual(1712, map591.enterX);
            Assert.AreEqual(3330, map591.enterY);
        }
    }
}
