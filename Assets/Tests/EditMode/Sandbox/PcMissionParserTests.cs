using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMissionParserTests
    {
        private static string MissionDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMission");

        [Test]
        public void LoadDirectory_RegistersAllMissions()
        {
            var reg = PcMissionParser.BuildRegistry(MissionDir);
            Assert.GreaterOrEqual(reg.Count, 50);
        }

        [Test]
        public void PlayerTaskDef_PreservesExactPcDataRowsAndFlags()
        {
            var path = Path.Combine(MissionDir, "player_task_def.txt");
            var rows = PcMissionParser.ParseFile(path);
            Assert.AreEqual(647, rows.Count, "PC player_task_def.txt has two header rows and 647 nonblank data rows; blank separator rows must not fabricate task ids.");
            Assert.IsNotNull(rows.Find(e => e.taskIdFirst == 0), "Blank TASK_ID_FIRST data rows must be preserved without fabricated ids.");
            Assert.IsNull(rows.Find(e => e.taskIdFirst == 158), "Blank separator row between 157 and 160 must not become fabricated task id 158.");
            Assert.IsNull(rows.Find(e => e.taskIdFirst == 159), "Blank separator rows must be skipped instead of using an id cursor.");

            var marriage = rows.Find(e => e.taskIdFirst == 151);
            Assert.NotNull(marriage);
            Assert.AreEqual(151, marriage.taskIdLast);
            Assert.AreEqual(1, marriage.syncFlag, "SYNC_FLAG from PC row 151 must be preserved.");
            Assert.AreEqual(0, marriage.clientFlag);

            var clientVisible = rows.Find(e => e.taskIdFirst == 1276);
            Assert.NotNull(clientVisible);
            Assert.AreEqual(1, clientVisible.syncFlag);
            Assert.AreEqual(1, clientVisible.clientFlag, "CLIENT_FLAG from PC row 1276 must be preserved.");

            var range = rows.Find(e => e.taskIdFirst == 4165);
            Assert.NotNull(range);
            Assert.AreEqual(4168, range.taskIdLast, "TASK_ID_LAST ranges must still be preserved.");
            Assert.AreEqual(1, range.syncFlag);
        }

        [Test]
        public void MissionHasVietnameseName()
        {
            var reg = PcMissionParser.BuildRegistry(MissionDir);
            int withName = 0;
            foreach (var m in reg.All)
                if (!string.IsNullOrEmpty(m.nameRaw)) withName++;
            Assert.Greater(withName, 30);
        }

        [Test]
        public void PlayerTaskDef_LoadsPcMetadataRowsAndRanges()
        {
            var reg = PcMissionParser.BuildRegistry(MissionDir);
            Assert.AreEqual(645, reg.Count,
                "PC player_task_def.txt has 645 numeric first-id metadata rows after headers/blank rows.");

            var emei = reg.ResolveId(1);
            Assert.IsNotNull(emei);
            Assert.AreEqual("峨嵋派任务", emei.nameRaw);
            Assert.AreEqual("入门任务、门派任务及出师任务", emei.describe);

            var achievementRange = reg.ResolveId(4140);
            Assert.IsNotNull(achievementRange);
            Assert.AreEqual(4126, achievementRange.taskIdFirst);
            Assert.AreEqual(4150, achievementRange.taskIdLast);
            Assert.AreEqual("成就系统数据", achievementRange.nameRaw);
        }
    }

    public class QuestServicePcImportTests
    {
        private static string MissionDir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMission");

        [Test]
        public void DefaultConstructorLoadsPcPlayerTaskMetadataWithoutSampleQuests()
        {
            var quest = new QuestService();

            Assert.GreaterOrEqual(quest.AllQuests.Count, 600);
            Assert.IsNotNull(quest.GetDefinition(1));
            Assert.IsNull(quest.GetDefinition(7001),
                "Built-in sample quest IDs must stay opt-in/test-only by default.");

            var def = quest.GetDefinition(1);
            Assert.AreEqual(QuestSourceKind.PcPlayerTaskMetadata, def.sourceKind);
            Assert.IsFalse(def.isSampleQuest);
            Assert.AreEqual("峨嵋派任务", def.nameRaw);
            Assert.AreEqual("入门任务、门派任务及出师任务", def.descriptionVi);
            Assert.AreEqual(1, def.pcTaskIdFirst);
        }

        [Test]
        public void ExplicitSampleModeTagsSampleQuestsAndKeepsPcMetadata()
        {
            var quest = new QuestService(includeSampleQuests: true);

            Assert.IsNotNull(quest.GetDefinition(1));
            var sample = quest.GetDefinition(7001);
            Assert.IsNotNull(sample);
            Assert.AreEqual(QuestSourceKind.Sample, sample.sourceKind);
            Assert.IsTrue(sample.isSampleQuest);
            Assert.AreEqual("[Hàng Ngày] Dọn Quái Ba Lăng", sample.nameVi);
        }

        [Test]
        public void CanLoadPcMetadataFromExplicitDirectory()
        {
            var quest = new QuestService();
            quest.LoadPcPlayerTaskMetadata(MissionDir);

            var bossKiller = quest.GetDefinition(1082);
            Assert.IsNotNull(bossKiller);
            Assert.AreEqual("Boss杀手任务变量", bossKiller.nameRaw);
            Assert.AreEqual(QuestSourceKind.PcPlayerTaskMetadata, bossKiller.sourceKind);
        }
    }

    public class PcAdventureParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAdventure");

        [Test]
        public void LoadDirectory_RegistersAllAdventures()
        {
            var reg = PcAdventureParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 1000);
        }
    }

    public class PcTongLevelParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTong");

        [Test]
        public void LoadDirectory_RegistersAllTongLevels()
        {
            var reg = PcTongLevelParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 30, "Full PC tong should expose 33 levels");
            Assert.Greater(reg.MaxLevel, 0);
        }
    }

    public class PcMeridianParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcMeridian");

        [Test]
        public void LoadDirectory_RegistersAllMeridianPoints()
        {
            var reg = PcMeridianParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 100, "Meridian should have ~128 points");
        }
    }

    public class PcPlayerTitleParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTitle");

        [Test]
        public void LoadDirectory_RegistersAllPlayerTitles()
        {
            var reg = PcPlayerTitleParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 100);
        }
    }

    public class PcFactionTitleParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcTitle");

        [Test]
        public void LoadDirectory_RegistersFactionTitles()
        {
            var reg = PcFactionTitleParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 50, "PC has 81 faction titles");
        }
    }

    public class PcLotteryParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcLottery");

        [Test]
        public void LoadDirectory_RegistersAllLotteryTables()
        {
            var reg = PcLotteryParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 50);
        }
    }

    public class PcShopParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcShop");

        [Test]
        public void LoadDirectory_RegistersAllShops()
        {
            var reg = PcShopParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 500, "PC buysell.txt has 1,521 shops");
        }
    }

    public class PcGoodsParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcShop");

        [Test]
        public void LoadDirectory_RegistersAllGoods()
        {
            var reg = PcGoodsParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 10);
        }
    }

    public class PcRecipeParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcRecipe");

        [Test]
        public void LoadDirectory_RegistersAllRecipes()
        {
            var reg = PcRecipeParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 30, "PC platina_def has 1,294 recipes");
        }
    }

    public class PcQuestItemParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcItemFull");

        [Test]
        public void LoadDirectory_RegistersAllQuestItems()
        {
            var reg = PcQuestItemParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 500, "PC questkey.txt has 2,046 quest items");
        }
    }

    public class PcPartnerParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcPartner");

        [Test]
        public void LoadDirectory_RegistersAllPartnerCharacteristics()
        {
            var reg = PcPartnerParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 10);
        }
    }

    public class PcPetParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcPet");

        [Test]
        public void LoadDirectory_RegistersAllPetLevels()
        {
            var reg = PcPetParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 5);
        }
    }

    public class PcAttribConstParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAttrib");

        [Test]
        public void LoadDirectory_RegistersAllAttribSections()
        {
            var reg = PcAttribConstParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 5);
        }
    }

    public class PcMissleParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAttrib");

        [Test]
        public void LoadDirectory_RegistersAllMissles()
        {
            var reg = PcMissleParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 100);
        }
    }

    public class PcEventBonusParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcEvent");

        [Test]
        public void LoadDirectory_RegistersAllEventBonuses()
        {
            var reg = PcEventBonusParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 100, "All event subdirs combined should yield 500+ rows");
        }
    }

    public class PcCityWarParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcEvent");

        [Test]
        public void LoadDirectory_RegistersCityWarAreas()
        {
            var reg = PcCityWarParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 5);
        }
    }

    public class PcAuctionConfigParserTests
    {
        private static string Dir => Path.Combine(Directory.GetCurrentDirectory(), "Assets/StreamingAssets/Reference/PcAuction");

        [Test]
        public void LoadDirectory_RegistersAuctionConfigKeys()
        {
            var reg = PcAuctionConfigParser.BuildRegistry(Dir);
            Assert.GreaterOrEqual(reg.Count, 5);
        }
    }
}
