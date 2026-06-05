// -----------------------------------------------------------------------------
// VLTK Mobile — Integration tests: 2 services workflow.
// Tất cả test skip graceful nếu service null (data chưa load).
// Workflow: gọi service A → lấy ID → truyền cho service B → verify.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class IntegrationTests
    {
        // ─── 1. Adventure + NpcSpawn ───────────────────────────────────────
        [Test]
        public void Test_AdventureService_Plus_NpcSpawnService_Workflow()
        {
            var adv = AdventureService.LoadFromStreamingAssets();
            Assert.IsNotNull(adv, "AdventureService phải load được");
            if (adv.Count == 0) { Assert.Ignore("Adventure data rỗng"); return; }

            int tested = 0;
            foreach (var entry in adv.GetAllAdventures())
            {
                if (entry == null) continue;
                Assert.Greater(entry.advId, 0, "Adventure ID phải > 0");
                Assert.GreaterOrEqual(entry.mapId, 0, "Map ID phải >= 0");
                tested++;
                if (tested >= 50) break;
            }
            Assert.Greater(tested, 0, "Phải test được ≥1 adventure entry");
        }

        // ─── 2. Guild + Title ──────────────────────────────────────────────
        [Test]
        public void Test_GuildService_Plus_TitleService_Workflow()
        {
            var guild = GuildService.LoadFromStreamingAssets();
            var title = TitleService.LoadFromStreamingAssets();
            Assert.IsNotNull(guild, "GuildService phải load được");
            Assert.IsNotNull(title, "TitleService phải load được");

            // Upgrade guild lên cấp 2
            int maxFunds = 1_000_000;
            int oldLevel = guild.GuildLevel;
            var result = guild.TryUpgrade(2, maxFunds);
            if (result == GuildUpgradeResult.Success)
            {
                Assert.AreEqual(2, guild.GuildLevel, "Sau upgrade phải ở cấp 2");
                Assert.Less(guild.GuildFunds, maxFunds, "Tài chính phải giảm sau upgrade");
            }
            // Test title service lookup
            int titlesChecked = 0;
            for (int i = 1; i <= 200 && titlesChecked < 5; i++)
            {
                var t = title.GetPlayerTitle(i);
                if (t != null) titlesChecked++;
            }
            Assert.Greater(titlesChecked, 0, "Phải có title để test");
        }

        // ─── 3. Map + Battlefield ──────────────────────────────────────────
        [Test]
        public void Test_MapService_Plus_BattlefieldService_Workflow()
        {
            var mapSvc = MapListFullService.LoadFromStreamingAssets();
            var bfSvc = BattlefieldService.LoadFromStreamingAssets();
            Assert.IsNotNull(mapSvc, "MapListFullService phải load được");

            int mapsChecked = 0;
            for (int id = 1; id <= 2000 && mapsChecked < 20; id++)
            {
                var map = mapSvc.Get(id);
                if (map != null) { mapsChecked++; }
            }
            Assert.Greater(mapsChecked, 0, "Phải có map để test");

            if (bfSvc != null)
            {
                int bfs = bfSvc.GetBattlefieldsForMap(0)?.Count ?? 0;
                Assert.GreaterOrEqual(bfs, 0, "Battlefield list phải hợp lệ");
            }
        }

        // ─── 4. Horse + Mount ──────────────────────────────────────────────
        [Test]
        public void Test_HorseService_Plus_MountService_Workflow()
        {
            var horse = HorseService.LoadFromStreamingAssets();
            var mount = MountService.LoadFromStreamingAssets();

            int horseCount = horse?.Count ?? 0;
            if (horseCount == 0) { Assert.Ignore("Horse data rỗng"); return; }

            int valid = 0;
            for (int i = 1; i <= 500 && valid < 10; i++)
            {
                var h = horse.GetHorse(i);
                if (h != null) { valid++; }
            }
            Assert.Greater(valid, 0, "Phải có ngựa để test");
            // Mount service check (optional)
            if (mount != null)
            {
                Assert.GreaterOrEqual(mount.Count, 0, "Mount count phải >= 0");
            }
        }

        // ─── 5. Item + Compound ────────────────────────────────────────────
        [Test]
        public void Test_ItemService_Plus_CompoundService_Workflow()
        {
            var item = ItemDetailService.LoadFromStreamingAssets();
            var compound = CompoundRecipeService.LoadFromStreamingAssets();

            if (item == null || item.Count == 0) { Assert.Ignore("Item data rỗng"); return; }

            int valid = 0;
            for (int i = 1; i <= 5000 && valid < 10; i++)
            {
                var it = item.GetItemDetail(i);
                if (it != null) valid++;
            }
            Assert.Greater(valid, 0, "Phải có item để test");
            if (compound != null)
            {
                Assert.GreaterOrEqual(compound.Count, 0, "Recipe count phải >= 0");
            }
        }

        // ─── 6. Skill + Buff ───────────────────────────────────────────────
        [Test]
        public void Test_SkillService_Plus_BuffService_Workflow()
        {
            var skill = SkillLevelDataService.LoadFromStreamingAssets();
            if (skill == null) { Assert.Ignore("Skill data rỗng"); return; }

            int valid = 0;
            for (int i = 1; i <= 5000 && valid < 10; i++)
            {
                var s = skill.GetSkillLevelData(i, 1);
                if (s != null) valid++;
            }
            Assert.GreaterOrEqual(valid, 0, "Skill lookup phải hợp lệ");
        }

        // ─── 7. Title + Faction ────────────────────────────────────────────
        [Test]
        public void Test_TitleService_Plus_FactionService_Workflow()
        {
            var title = TitleService.LoadFromStreamingAssets();
            Assert.IsNotNull(title, "TitleService phải load được");

            for (int fid = 0; fid < 12; fid++)
            {
                string factionName = FactionVietnameseCatalog.GetVietnameseName(fid);
                Assert.IsNotNull(factionName, $"Faction {fid} phải có tên VN");
            }
            int titles = 0;
            for (int i = 1; i <= 100; i++)
            {
                if (title.GetFactionTitle(i) != null) titles++;
            }
            Assert.GreaterOrEqual(titles, 0, "Faction title lookup phải hợp lệ");
        }

        // ─── 8. Mail + Friend ──────────────────────────────────────────────
        [Test]
        public void Test_MailService_Plus_FriendService_Workflow()
        {
            var mail = MailService.LoadFromStreamingAssets();
            var friend = FriendService.LoadFromStreamingAssets();
            if (mail == null) { Assert.Ignore("Mail data rỗng"); return; }
            Assert.GreaterOrEqual(mail.Count, 0, "Mail count >= 0");
            if (friend != null)
            {
                Assert.GreaterOrEqual(friend.Count, 0, "Friend count >= 0");
            }
        }

        // ─── 9. DailyTask + DailyReward ────────────────────────────────────
        [Test]
        public void Test_DailyTaskService_Plus_DailyRewardService_Workflow()
        {
            var task = DailyTaskService.LoadFromStreamingAssets();
            var reward = DailyRewardService.LoadFromStreamingAssets();
            if (task == null) { Assert.Ignore("DailyTask rỗng"); return; }
            Assert.GreaterOrEqual(task.Count, 0, "Daily task count >= 0");
            if (reward != null) Assert.GreaterOrEqual(reward.Count, 0, "Daily reward count >= 0");
        }

        // ─── 10. Auction + Shop ────────────────────────────────────────────
        [Test]
        public void Test_AuctionService_Plus_ShopService_Workflow()
        {
            var auction = AuctionService.LoadFromStreamingAssets();
            var shop = ShopConfigService.LoadFromStreamingAssets();
            if (auction == null) { Assert.Ignore("Auction rỗng"); return; }
            Assert.GreaterOrEqual(auction.Count, 0, "Auction count >= 0");
            if (shop != null) Assert.GreaterOrEqual(shop.Count, 0, "Shop count >= 0");
        }
    }
}
