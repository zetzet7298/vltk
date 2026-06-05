// -----------------------------------------------------------------------------
// VLTK Mobile — Battlefield + InstanceMap Service Tests
// Tests runtime service behavior với PC data StreamingAssets/Reference/PcMap.
// Vietnamese: "Chiến Trường", "Phó Bản", "Mê Cung", "Săn Boss".
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class BattlefieldServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            // Không throw ngay cả khi không có file (trả về registry rỗng)
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => BattlefieldService.LoadFromStreamingAssets());
        }

        [Test]
        public void Count_NonNegative()
        {
            var svc = BattlefieldService.LoadFromStreamingAssets();
            Assert.GreaterOrEqual(svc.Count, 0);
        }

        [Test]
        public void GetBattlefield_ReturnsNullForInvalidId()
        {
            var svc = BattlefieldService.LoadFromStreamingAssets();
            Assert.IsNull(svc.GetBattlefield(-1));
            Assert.IsNull(svc.GetBattlefield(0));
            Assert.IsNull(svc.GetBattlefield(999_999));
        }

        [Test]
        public void IsBattlefieldMap_ConsistentWithGet()
        {
            var svc = BattlefieldService.LoadFromStreamingAssets();
            Assert.IsFalse(svc.IsBattlefieldMap(-1));
            Assert.IsFalse(svc.IsBattlefieldMap(999_999));
        }

        [Test]
        public void CanJoin_RejectsLevelOutOfRange()
        {
            var reg = new PcBattlefieldRegistry();
            reg.Register(new PcBattlefieldEntry
            {
                mapId = 5000,
                nameVi = "Tống Kim Lâm An",
                minLevel = 50,
                maxLevel = 100,
                maxPlayers = 200,
                teamCount = 2,
                duration = 1800,
            });
            var svc = new BattlefieldService(reg);
            Assert.AreEqual(BattlefieldJoinResult.Allowed, svc.CanJoin(5000, 75, 50));
            Assert.AreEqual(BattlefieldJoinResult.LevelTooLow, svc.CanJoin(5000, 30, 50));
            Assert.AreEqual(BattlefieldJoinResult.LevelTooHigh, svc.CanJoin(5000, 150, 50));
            Assert.AreEqual(BattlefieldJoinResult.Full, svc.CanJoin(5000, 75, 200));
        }

        [Test]
        public void TryJoin_IncrementsCurrentPlayers()
        {
            var reg = new PcBattlefieldRegistry();
            reg.Register(new PcBattlefieldEntry
            {
                mapId = 5001,
                nameVi = "Tống Kim Bắc Kinh",
                minLevel = 60,
                maxLevel = 120,
                maxPlayers = 10,
                teamCount = 2,
                duration = 1800,
            });
            var svc = new BattlefieldService(reg);
            Assert.IsTrue(svc.TryJoin(5001, 80));
            var state = svc.GetState(5001);
            Assert.IsNotNull(state);
            Assert.AreEqual(1, state.currentPlayers);
            Assert.IsTrue(state.isActive);
        }

        [Test]
        public void EndBattle_ResetsState()
        {
            var reg = new PcBattlefieldRegistry();
            reg.Register(new PcBattlefieldEntry
            {
                mapId = 5002,
                nameVi = "Tống Kim Thành Đô",
                minLevel = 70,
                maxLevel = 130,
                maxPlayers = 100,
                teamCount = 2,
                duration = 1800,
            });
            var svc = new BattlefieldService(reg);
            svc.TryJoin(5002, 90);
            int fired = 0;
            svc.OnBattleEnded += (id, team) => fired++;
            Assert.IsTrue(svc.EndBattle(5002, 1)); // Tống thắng
            var state = svc.GetState(5002);
            Assert.IsFalse(state.isActive);
            Assert.AreEqual(1, state.winningTeam);
            Assert.AreEqual(1, fired);
        }
    }

    public class InstanceMapServiceTests
    {
        [Test]
        public void LoadFromStreamingAssets_MatchesCommittedData()
        {
            ServiceStreamingAssetTestUtil.AssertLoadMatchesCommittedData(() => InstanceMapService.LoadFromStreamingAssets());
        }

        [Test]
        public void GetInstance_ReturnsNullForInvalidId()
        {
            var svc = InstanceMapService.LoadFromStreamingAssets();
            Assert.IsNull(svc.GetInstance(-1));
            Assert.IsNull(svc.GetInstance(999_999));
        }

        [Test]
        public void GetByType_FiltersCorrectly()
        {
            var reg = new PcInstanceMapRegistry();
            reg.Register(new PcInstanceMapEntry { mapId = 1, mapType = 1, nameVi = "Mê Cung A", minPartySize = 1, maxPartySize = 6 });
            reg.Register(new PcInstanceMapEntry { mapId = 2, mapType = 1, nameVi = "Mê Cung B", minPartySize = 2, maxPartySize = 4 });
            reg.Register(new PcInstanceMapEntry { mapId = 3, mapType = 3, nameVi = "Boss X", minPartySize = 5, maxPartySize = 10 });
            reg.Register(new PcInstanceMapEntry { mapId = 4, mapType = 0, nameVi = "Normal", minPartySize = 1, maxPartySize = 1 });
            var svc = new InstanceMapService(reg);
            var mazes = svc.GetInstancesByType(1);
            Assert.AreEqual(2, mazes.Count);
            foreach (var m in mazes) Assert.AreEqual(1, m.mapType);
            var bosses = svc.GetInstancesByType(3);
            Assert.AreEqual(1, bosses.Count);
            var normals = svc.GetInstancesByType(0);
            Assert.AreEqual(1, normals.Count);
        }

        [Test]
        public void CanEnter_RejectsLevelTooLow()
        {
            var reg = new PcInstanceMapRegistry();
            reg.Register(new PcInstanceMapEntry
            {
                mapId = 100,
                nameVi = "Phó Bản Cấp 50",
                mapType = 3,
                minLevel = 50,
                maxLevel = 80,
                minPartySize = 1,
                maxPartySize = 6,
                durationMinutes = 30,
            });
            var svc = new InstanceMapService(reg);
            Assert.AreEqual(InstanceEnterResult.Allowed, svc.CanEnter(100, 60, 3));
            Assert.AreEqual(InstanceEnterResult.LevelTooLow, svc.CanEnter(100, 30, 3));
            Assert.AreEqual(InstanceEnterResult.LevelTooHigh, svc.CanEnter(100, 100, 3));
            Assert.AreEqual(InstanceEnterResult.PartyTooSmall, svc.CanEnter(100, 60, 0));
            Assert.AreEqual(InstanceEnterResult.PartyTooBig, svc.CanEnter(100, 60, 99));
        }

        [Test]
        public void TryStartInstance_SetsActive()
        {
            var reg = new PcInstanceMapRegistry();
            reg.Register(new PcInstanceMapEntry
            {
                mapId = 200,
                nameVi = "Võ Đài Liên Server",
                mapType = 2,
                minLevel = 40,
                maxLevel = 150,
                minPartySize = 1,
                maxPartySize = 1,
                durationMinutes = 15,
            });
            var svc = new InstanceMapService(reg);
            int fired = 0;
            svc.OnInstanceStarted += (id, sz) => fired++;
            Assert.IsTrue(svc.TryStartInstance(200, 80, 1));
            var state = svc.GetState(200);
            Assert.IsNotNull(state);
            Assert.IsTrue(state.isActive);
            Assert.AreEqual(1, state.currentPartySize);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void FinishInstance_SetsCleared()
        {
            var reg = new PcInstanceMapRegistry();
            reg.Register(new PcInstanceMapEntry
            {
                mapId = 300,
                nameVi = "Mê Cung Cấp 90",
                mapType = 1,
                minLevel = 80,
                maxLevel = 100,
                minPartySize = 3,
                maxPartySize = 6,
                durationMinutes = 60,
            });
            var svc = new InstanceMapService(reg);
            svc.TryStartInstance(300, 90, 5);
            int fired = 0;
            svc.OnInstanceFinished += (id, ok) => fired++;
            Assert.IsTrue(svc.FinishInstance(300, true));
            var state = svc.GetState(300);
            Assert.IsFalse(state.isActive);
            Assert.IsTrue(state.isCleared);
            Assert.AreEqual(1, fired);
        }
    }
}
