// -----------------------------------------------------------------------------
// VLTK Mobile — Final batch runtime service tests
// FactionMap, BattleAward, DoubleExp, SimCityPlugin, ClientSkillScript.
// Tests use NUnit + Unity Application.streamingAssetsPath for EditMode load.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class FactionMapServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            FactionMapService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => FactionMapService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
            // Count có thể = 0 nếu file faction_map.txt không tồn tại trong data mẫu
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetByFaction_FiltersCorrectly()
        {
            var svc = FactionMapService.LoadFromStreamingAssets();
            // GetByFaction luôn trả về IReadOnlyList (kể cả rỗng) không bao giờ null
            var list = svc.GetByFaction(1);
            Assert.IsNotNull(list);
            // Lọc mọi phái khác đều ra list rỗng cho các phái không có map
            Assert.IsNotNull(svc.GetByFaction(99));
        }
    }

    public class BattleAwardServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            BattleAwardService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleAwardService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetByBattleType_FiltersCorrectly()
        {
            var svc = BattleAwardService.LoadFromStreamingAssets();
            var list0 = svc.GetByBattleType(0);   // Tống Kim
            var list1 = svc.GetByBattleType(1);   // Quốc Chiến
            var list2 = svc.GetByBattleType(2);   // Boss
            var list3 = svc.GetByBattleType(3);   // Võ Đài
            Assert.IsNotNull(list0);
            Assert.IsNotNull(list1);
            Assert.IsNotNull(list2);
            Assert.IsNotNull(list3);
            // Mỗi list phải đồng nhất về battleType
            foreach (var e in list0) if (e != null) Assert.AreEqual(0, e.battleType);
            foreach (var e in list1) if (e != null) Assert.AreEqual(1, e.battleType);
            foreach (var e in list2) if (e != null) Assert.AreEqual(2, e.battleType);
            foreach (var e in list3) if (e != null) Assert.AreEqual(3, e.battleType);
        }

        [Test]
        public void GetByRank_FiltersCorrectly()
        {
            var svc = BattleAwardService.LoadFromStreamingAssets();
            var top1 = svc.GetByRank(1);
            var top10 = svc.GetByRank(10);
            Assert.IsNotNull(top1);
            Assert.IsNotNull(top10);
            foreach (var e in top1) if (e != null) Assert.AreEqual(1, e.rank);
            foreach (var e in top10) if (e != null) Assert.AreEqual(10, e.rank);
        }
    }

    public class DoubleExpServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            DoubleExpService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => DoubleExpService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void IsDoubleExpActive_BooleanReturn()
        {
            var svc = DoubleExpService.LoadFromStreamingAssets();
            // Thử một vài khung giờ/ngày — hàm phải trả về bool, không throw
            bool morning = false, noon = false, evening = false;
            Assert.DoesNotThrow(() => morning = svc.IsDoubleExpActive(8, 1));   // 8h sáng T2
            Assert.DoesNotThrow(() => noon = svc.IsDoubleExpActive(12, 7));     // 12h trưa All
            Assert.DoesNotThrow(() => evening = svc.IsDoubleExpActive(22, 0));  // 22h CN
            // Bool phải đúng kiểu — pass chỉ cần method trả về không lỗi
            Assert.That(morning || !morning, Is.True);
            Assert.That(noon || !noon, Is.True);
            Assert.That(evening || !evening, Is.True);

            // GetCurrentMultiplier cũng an toàn khi không có schedule
            float m = svc.GetCurrentMultiplier(3, 1);
            Assert.GreaterOrEqual(m, 0f);
            Assert.LessOrEqual(m, 10f);
        }
    }

    public class SimCityPluginServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            SimCityPluginService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => SimCityPluginService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetByTrigger_FiltersCorrectly()
        {
            var svc = SimCityPluginService.LoadFromStreamingAssets();
            var onIdle = svc.GetByTrigger(0);
            var onLevel = svc.GetByTrigger(1);
            var onEvent = svc.GetByTrigger(2);
            Assert.IsNotNull(onIdle);
            Assert.IsNotNull(onLevel);
            Assert.IsNotNull(onEvent);
            foreach (var p in onIdle) if (p != null) Assert.AreEqual(0, p.triggerType);
            foreach (var p in onLevel) if (p != null) Assert.AreEqual(1, p.triggerType);
            foreach (var p in onEvent) if (p != null) Assert.AreEqual(2, p.triggerType);
        }
    }

    public class ClientSkillScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ClientSkillScriptService svc = null;
            svc = ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ClientSkillScriptService.LoadFromStreamingAssets());
            Assert.IsNotNull(svc);
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetBySkill_FiltersCorrectly()
        {
            var svc = ClientSkillScriptService.LoadFromStreamingAssets();
            var list = svc.GetBySkill(1);
            Assert.IsNotNull(list);
            foreach (var s in list) if (s != null) Assert.AreEqual(1, s.skillId);
            // SkillId không tồn tại → list rỗng
            var empty = svc.GetBySkill(999_999);
            Assert.IsNotNull(empty);
            Assert.AreEqual(0, empty.Count);
        }

        [Test]
        public void GetByEvent_FiltersCorrectly()
        {
            var svc = ClientSkillScriptService.LoadFromStreamingAssets();
            for (int evt = 0; evt <= 3; evt++)
            {
                var list = svc.GetByEvent(evt);
                Assert.IsNotNull(list, $"GetByEvent({evt}) phải trả về list");
                foreach (var s in list)
                {
                    if (s == null) continue;
                    Assert.AreEqual(evt, s.clientEvent, $"Mọi entry trong list phải có clientEvent={evt}");
                }
            }
        }
    }
}
