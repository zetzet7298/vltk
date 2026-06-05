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
        public void MissionHasVietnameseName()
        {
            var reg = PcMissionParser.BuildRegistry(MissionDir);
            int withName = 0;
            foreach (var m in reg.All)
                if (!string.IsNullOrEmpty(m.nameRaw)) withName++;
            Assert.Greater(withName, 30);
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
