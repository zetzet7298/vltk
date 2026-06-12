// -----------------------------------------------------------------------------
// VLTK Mobile — Tests cho Hongbao/ItemExchange/SpecialSkill/NpcSkill/
//                  TranslifeSkill/SkillTemplate services.
// Vietnamese: Kiểm thử các dịch vụ Hồng Bao, Đổi Vật Phẩm, Skill Đặc Biệt,
//             Skill Quái, Skill Chuyển Sinh, Template Skill.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class HongbaoServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            // Phải chạy được kể cả khi thư mục không tồn tại
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => HongbaoService.LoadFromStreamingAssets());
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = new HongbaoService(new PcHongbaoRegistry());
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void CanClaim_RejectsLevelMismatch()
        {
            var reg = new PcHongbaoRegistry();
            reg.Register(new PcHongbaoEntry
            {
                id = 1,
                minLevel = 50,
                maxLevel = 100,
                silver = 1000,
            });
            var svc = new HongbaoService(reg);
            Assert.IsTrue(svc.CanClaim(1, 75), "Cấp 75 nằm trong khoảng 50-100");
            Assert.IsFalse(svc.CanClaim(1, 30), "Cấp 30 dưới minLevel");
            Assert.IsFalse(svc.CanClaim(1, 150), "Cấp 150 trên maxLevel");
            Assert.IsFalse(svc.CanClaim(999, 50), "Id không tồn tại");
        }

        [Test]
        public void CanClaim_AlwaysTrueWhenNoLevelRestriction()
        {
            var reg = new PcHongbaoRegistry();
            reg.Register(new PcHongbaoEntry { id = 1, minLevel = 0, maxLevel = 0 });
            var svc = new HongbaoService(reg);
            Assert.IsTrue(svc.CanClaim(1, 1));
            Assert.IsTrue(svc.CanClaim(1, 200));
        }

        [Test]
        public void Claim_FiresEvent()
        {
            var reg = new PcHongbaoRegistry();
            reg.Register(new PcHongbaoEntry { id = 1, minLevel = 0, maxLevel = 0 });
            var svc = new HongbaoService(reg);
            int fired = 0;
            svc.OnHongbaoClaimed += id => fired++;
            Assert.IsTrue(svc.Claim(1, 50));
            Assert.AreEqual(1, fired);
            // minLevel=0 maxLevel=0 → luôn claim được. Production không dedup claim,
            // nên claim lần 2 vẫn thành công (và fire event lần nữa).
            Assert.IsTrue(svc.Claim(1, 10), "minLevel=0 maxLevel=0 → luôn claim được");
            Assert.AreEqual(2, fired);
        }
    }

    public class ItemExchangeServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ItemExchangeService.LoadFromStreamingAssets());
        }

        [Test]
        public void TryExchange_RequiresItems()
        {
            var reg = new PcItemExchangeRegistry();
            reg.Register(new PcItemExchangeEntry
            {
                id = 1,
                nameRaw = "Đổi 5 bình HP → 1 HP Cao Cấp",
                requireGenre = 1, requireDetail = 2, requireParticular = 3, requireCount = 5,
                getGenre = 1, getDetail = 2, getParticular = 4, getCount = 1,
                minLevel = 0,
            });
            var svc = new ItemExchangeService(reg);
            var inv = new Dictionary<int, int>();

            // Túi rỗng → fail
            var r1 = svc.TryExchange(1, playerLevel: 50, inventory: inv);
            Assert.IsFalse(r1.success);
            Assert.IsNotNull(r1.error);
            Assert.IsTrue(r1.error.Contains("Thiếu nguyên liệu"));

            // Túi có 3 → vẫn fail
            int key = ItemExchangeService.EncodeItemKey(1, 2, 3);
            inv[key] = 3;
            var r2 = svc.TryExchange(1, 50, inv);
            Assert.IsFalse(r2.success);

            // Túi có 5 → success
            inv[key] = 5;
            var r3 = svc.TryExchange(1, 50, inv);
            Assert.IsTrue(r3.success);
            Assert.AreEqual(0, inv[key], "Trừ hết 5 nguyên liệu");
            int getKey = ItemExchangeService.EncodeItemKey(1, 2, 4);
            Assert.AreEqual(1, inv[getKey], "Cộng 1 vật phẩm mới");
        }

        [Test]
        public void TryExchange_RequiresMinLevel()
        {
            var reg = new PcItemExchangeRegistry();
            reg.Register(new PcItemExchangeEntry
            {
                id = 1,
                requireGenre = 1, requireDetail = 1, requireParticular = 1, requireCount = 1,
                getGenre = 1, getDetail = 1, getParticular = 2, getCount = 1,
                minLevel = 30,
            });
            var svc = new ItemExchangeService(reg);
            var inv = new Dictionary<int, int>
            {
                [ItemExchangeService.EncodeItemKey(1, 1, 1)] = 10,
            };
            var r1 = svc.TryExchange(1, playerLevel: 20, inventory: inv);
            Assert.IsFalse(r1.success, "Cấp 20 chưa đủ minLevel 30");
            Assert.IsTrue(r1.error.Contains("Cấp"));
        }

        [Test]
        public void GetExchange_ReturnsNullForInvalidId()
        {
            var svc = new ItemExchangeService(new PcItemExchangeRegistry());
            Assert.IsNull(svc.GetExchange(99999));
            Assert.IsNull(svc.GetExchange(0));
            Assert.IsNull(svc.GetExchange(-1));
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = new ItemExchangeService(new PcItemExchangeRegistry());
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }

    public class SpecialSkillServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SpecialSkillService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var reg = new PcSpecialSkillRegistry();
            reg.Register(new PcSpecialSkillEntry { skillId = 1, factionId = 1, nameRaw = "Thiếu Lâm Skill" });
            reg.Register(new PcSpecialSkillEntry { skillId = 2, factionId = 1, nameRaw = "Thiếu Lâm Skill 2" });
            reg.Register(new PcSpecialSkillEntry { skillId = 3, factionId = 2, nameRaw = "Thiên Vương Skill" });
            var svc = new SpecialSkillService(reg);
            var sl = svc.GetByFaction(1);
            Assert.AreEqual(2, sl.Count);
            foreach (var s in sl) Assert.AreEqual(1, s.factionId);
            var tw = svc.GetByFaction(2);
            Assert.AreEqual(1, tw.Count);
            Assert.AreEqual(3, tw[0].skillId);
        }

        [Test]
        public void GetSpecialSkill_ReturnsById()
        {
            var reg = new PcSpecialSkillRegistry();
            reg.Register(new PcSpecialSkillEntry { skillId = 42, nameRaw = "Tuyệt Kỹ 42" });
            var svc = new SpecialSkillService(reg);
            Assert.AreEqual("Tuyệt Kỹ 42", svc.GetSpecialSkill(42).nameRaw);
            Assert.IsNull(svc.GetSpecialSkill(999));
        }
    }

    public class NpcSkillServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => NpcSkillService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByNpcTemplate_FiltersCorrectly()
        {
            var reg = new PcNpcSkillRegistry();
            reg.Register(new PcNpcSkillEntry { skillId = 1, npcTemplateId = 100, damage = 500 });
            reg.Register(new PcNpcSkillEntry { skillId = 2, npcTemplateId = 100, damage = 700 });
            reg.Register(new PcNpcSkillEntry { skillId = 3, npcTemplateId = 200, damage = 1000 });
            var svc = new NpcSkillService(reg);
            var list100 = svc.GetByNpcTemplate(100);
            Assert.AreEqual(2, list100.Count);
            var list200 = svc.GetByNpcTemplate(200);
            Assert.AreEqual(1, list200.Count);
            Assert.AreEqual(1000, list200[0].damage);
        }

        [Test]
        public void GetNpcSkill_ReturnsById()
        {
            var reg = new PcNpcSkillRegistry();
            reg.Register(new PcNpcSkillEntry { skillId = 7, nameRaw = "Boss Skill 7" });
            var svc = new NpcSkillService(reg);
            Assert.AreEqual("Boss Skill 7", svc.GetNpcSkill(7).nameRaw);
            Assert.IsNull(svc.GetNpcSkill(0));
        }
    }

    public class TranslifeSkillServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => TranslifeSkillService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetByTranslifeLevel_FiltersCorrectly()
        {
            var reg = new PcTranslifeSkillRegistry();
            reg.Register(new PcTranslifeSkillEntry { skillId = 1, translifeLevel = 1, nameRaw = "CS1" });
            reg.Register(new PcTranslifeSkillEntry { skillId = 2, translifeLevel = 2, nameRaw = "CS2-1" });
            reg.Register(new PcTranslifeSkillEntry { skillId = 3, translifeLevel = 2, nameRaw = "CS2-2" });
            reg.Register(new PcTranslifeSkillEntry { skillId = 4, translifeLevel = 4, nameRaw = "CS4" });
            var svc = new TranslifeSkillService(reg);
            Assert.AreEqual(1, svc.GetByTranslifeLevel(1).Count);
            Assert.AreEqual(2, svc.GetByTranslifeLevel(2).Count);
            Assert.AreEqual(0, svc.GetByTranslifeLevel(3).Count);
            Assert.AreEqual(1, svc.GetByTranslifeLevel(4).Count);
            Assert.AreEqual("CS4", svc.GetByTranslifeLevel(4)[0].nameRaw);
        }

        [Test]
        public void GetTranslifeSkill_ReturnsById()
        {
            var reg = new PcTranslifeSkillRegistry();
            reg.Register(new PcTranslifeSkillEntry { skillId = 99, nameRaw = "T99" });
            var svc = new TranslifeSkillService(reg);
            Assert.AreEqual("T99", svc.GetTranslifeSkill(99).nameRaw);
            Assert.IsNull(svc.GetTranslifeSkill(100));
        }
    }

    public class SkillTemplateServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SkillTemplateService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetTemplate_ReturnsNullForInvalid()
        {
            var svc = new SkillTemplateService(new PcSkillTemplateRegistry());
            Assert.IsNull(svc.GetTemplate(0));
            Assert.IsNull(svc.GetTemplate(99999));
            Assert.IsNull(svc.GetTemplate(-1));
        }

        [Test]
        public void GetTemplatesForMissle_FiltersCorrectly()
        {
            var reg = new PcSkillTemplateRegistry();
            reg.Register(new PcSkillTemplateEntry { templateId = 1, missleId = 50 });
            reg.Register(new PcSkillTemplateEntry { templateId = 2, missleId = 50 });
            reg.Register(new PcSkillTemplateEntry { templateId = 3, missleId = 60 });
            var svc = new SkillTemplateService(reg);
            int count50 = 0;
            foreach (var t in svc.GetTemplatesForMissle(50)) count50++;
            Assert.AreEqual(2, count50);
            int count60 = 0;
            foreach (var t in svc.GetTemplatesForMissle(60)) count60++;
            Assert.AreEqual(1, count60);
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = new SkillTemplateService(new PcSkillTemplateRegistry());
            Assert.GreaterOrEqual(svc.Count, 0);
        }
    }
}
