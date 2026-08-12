// -----------------------------------------------------------------------------
// VLTK Mobile — EditMode tests cho ServerEvent, VngEvent, BattleScript services
// Vietnamese: Kiểm thử sự kiện máy chủ, sự kiện VNG, kịch bản chiến đấu.
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ServerEventServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            // Có thể trả về registry rỗng (data thật chưa được ship) nhưng không crash
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => ServerEventService.LoadFromStreamingAssets());
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = ServerEventService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetActive_FiltersByDate()
        {
            var reg = new PcServerEventRegistry();
            // open type (0) → luôn active
            reg.Register(new PcServerEventEntry { eventId = 1, nameVi = "Sự Kiện Mở", type = 0 });
            // limited (1) trong khoảng hợp lệ
            reg.Register(new PcServerEventEntry
            {
                eventId = 2,
                nameVi = "Sự Kiện Giới Hạn",
                type = 1,
                startDate = 20250101,
                endDate = 20251231,
            });
            // limited ngoài khoảng
            reg.Register(new PcServerEventEntry
            {
                eventId = 3,
                nameVi = "Sự Kiện Quá Khứ",
                type = 1,
                startDate = 20200101,
                endDate = 20201231,
            });
            var svc = new ServerEventService(reg);
            int count = 0;
            foreach (var _ in svc.GetActiveEvents(20250615)) count++;
            // Sự kiện 1 (open) + sự kiện 2 (trong khoảng) = 2
            Assert.AreEqual(2, count);

            Assert.IsTrue(svc.IsActive(1, 20250615), "open event luôn active");
            Assert.IsTrue(svc.IsActive(2, 20250615), "limited event trong khoảng");
            Assert.IsFalse(svc.IsActive(3, 20250615), "limited event ngoài khoảng");
            Assert.IsFalse(svc.IsActive(999, 20250615), "event không tồn tại");
        }
    }

    public class VngEventServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => VngEventService.LoadFromStreamingAssets());
        }

        [Test]
        public void CanParticipate_RejectsLevelMismatch()
        {
            var reg = new PcVngEventRegistry();
            reg.Register(new PcVngEventEntry
            {
                eventId = 10,
                nameVi = "VNG Cần Level 50",
                type = 2,  // chỉ cần level
                requiredLevel = 50,
            });
            var svc = new VngEventService(reg);
            Assert.IsFalse(svc.CanParticipate(10, playerLevel: 30, vipLevel: 0), "Level 30 < 50 → không tham gia được");
            Assert.IsTrue(svc.CanParticipate(10, playerLevel: 50, vipLevel: 0), "Level 50 đạt yêu cầu");
            Assert.IsTrue(svc.CanParticipate(10, playerLevel: 100, vipLevel: 0), "Level 100 vượt yêu cầu");
        }

        [Test]
        public void CanParticipate_RequiresVipAndLevel()
        {
            var reg = new PcVngEventRegistry();
            reg.Register(new PcVngEventEntry
            {
                eventId = 20,
                nameVi = "VNG Cần VIP 3 + Level 60",
                type = 3,  // cả VIP + level
                requiredLevel = 60,
                requiredVip = 3,
            });
            var svc = new VngEventService(reg);
            Assert.IsFalse(svc.CanParticipate(20, 100, 0), "Đủ level nhưng thiếu VIP");
            Assert.IsFalse(svc.CanParticipate(20, 30, 5), "Đủ VIP nhưng thiếu level");
            Assert.IsTrue(svc.CanParticipate(20, 60, 3), "Đủ cả 2");
        }

        [Test]
        public void GetEventsForVip_FiltersCorrectly()
        {
            var reg = new PcVngEventRegistry();
            reg.Register(new PcVngEventEntry { eventId = 1, nameVi = "Open", type = 0 });
            reg.Register(new PcVngEventEntry { eventId = 2, nameVi = "VIP1", type = 1, requiredVip = 1 });
            reg.Register(new PcVngEventEntry { eventId = 3, nameVi = "VIP5", type = 1, requiredVip = 5 });
            reg.Register(new PcVngEventEntry { eventId = 4, nameVi = "VIP10", type = 1, requiredVip = 10 });
            var svc = new VngEventService(reg);
            int vip3 = 0;
            foreach (var _ in svc.GetEventsForVip(3)) vip3++;
            // id=1 (open, vip0≤3), id=2 (VIP1≤3) match. id=3 (VIP5>3) and id=4 (VIP10>3) excluded.
            Assert.AreEqual(2, vip3, "VIP 3: 2 event (open + VIP1); VIP5/VIP10 excluded");
        }
    }

    public class BattleScriptServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattleScriptService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetScriptsByTrigger_FiltersCorrectly()
        {
            var reg = new PcBattleScriptRegistry();
            reg.Register(new PcBattleScriptEntry { scriptId = 1, scriptName = "Bắt Đầu Tống Kim", triggerType = 0, mapId = 100 });
            reg.Register(new PcBattleScriptEntry { scriptId = 2, scriptName = "Kết Thúc Tống Kim", triggerType = 1, mapId = 100 });
            reg.Register(new PcBattleScriptEntry { scriptId = 3, scriptName = "Giết Boss Công Thành", triggerType = 2, mapId = 200 });
            reg.Register(new PcBattleScriptEntry { scriptId = 4, scriptName = "Chết Tướng", triggerType = 3, mapId = 200 });
            var svc = new BattleScriptService(reg);
            int start = 0;
            foreach (var _ in svc.GetScriptsByTrigger(0)) start++;
            Assert.AreEqual(1, start, "Chỉ có 1 script trigger=start");
            int end = 0;
            foreach (var _ in svc.GetScriptsByTrigger(1)) end++;
            Assert.AreEqual(1, end);
            int death = 0;
            foreach (var _ in svc.GetScriptsByTrigger(3)) death++;
            Assert.AreEqual(1, death);
        }

        [Test]
        public void GetScriptsForMap_FiltersCorrectly()
        {
            var reg = new PcBattleScriptRegistry();
            reg.Register(new PcBattleScriptEntry { scriptId = 1, scriptName = "A", triggerType = 0, mapId = 100 });
            reg.Register(new PcBattleScriptEntry { scriptId = 2, scriptName = "B", triggerType = 1, mapId = 100 });
            reg.Register(new PcBattleScriptEntry { scriptId = 3, scriptName = "C", triggerType = 2, mapId = 200 });
            var svc = new BattleScriptService(reg);
            int map100 = 0;
            foreach (var _ in svc.GetScriptsForMap(100)) map100++;
            Assert.AreEqual(2, map100);
            int map200 = 0;
            foreach (var _ in svc.GetScriptsForMap(200)) map200++;
            Assert.AreEqual(1, map200);
            int map999 = 0;
            foreach (var _ in svc.GetScriptsForMap(999)) map999++;
            Assert.AreEqual(0, map999, "Map không tồn tại");
        }
    }
}
