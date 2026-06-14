// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for revive runtime (PC revivepos.ini 139 sections /
// 241 rows). Revive semantic: in-place for city maps, walk-back inside a
// revivepos.ini section's region, teleport to main city for out-of-region
// death in a non-city map, teleport to bound city for instanced/mission maps.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class ReviveRuntimeServiceTests
    {
        // --- Fixtures ---------------------------------------------------------

        private static PcRevivePosRegistry BuildRegistry(params PcRevivePosEntry[] entries)
        {
            var reg = new PcRevivePosRegistry();
            foreach (var e in entries) reg.Add(e);
            return reg;
        }

        private static readonly PcReviveCity DefaultCity = new PcReviveCity(1, 100, 100, "Thành Chính");

        private sealed class FakeHost : IReviveHost
        {
            public int CurrentMap = 1;
            public int CurrentX = 0;
            public int CurrentY = 0;
            public int SetPosCalls;
            public int SetFightCalls;
            public int MsgCalls;
            public int LastMapId;
            public int LastX, LastY;
            public int LastFight;
            public string LastMsg;
            public string LastPlayer;

            public int GetCurrentMapId(string player) { LastPlayer = player; return CurrentMap; }
            public (int x, int y) GetCurrentPos(string player) { LastPlayer = player; return (CurrentX, CurrentY); }
            public void SetPos(string player, int mapId, int x, int y)
            {
                SetPosCalls++;
                LastPlayer = player; LastMapId = mapId; LastX = x; LastY = y;
            }
            public void SetFightState(string player, int fightState)
            {
                SetFightCalls++;
                LastPlayer = player; LastFight = fightState;
            }
            public void SendMessage(string player, string message)
            {
                MsgCalls++;
                LastPlayer = player; LastMsg = message;
            }
        }

        // --- ResolveRevive ----------------------------------------------------

        [Test]
        public void ResolveRevive_CityMapNoRegion_InPlace()
        {
            // City map (no region): [1] x=100,y=100
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 1, PosX = 100, PosY = 100,
                RegionStart = 0, RegionEnd = 0,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(1, 50, 50);
            Assert.AreEqual(ReviveMode.InPlace, p.Mode);
            Assert.AreEqual(1, p.TargetMapId);
            Assert.AreEqual(100, p.TargetX);
            Assert.AreEqual(100, p.TargetY);
        }

        [Test]
        public void ResolveRevive_InsideRegion_WalkBack()
        {
            // Section [53] region=100,200 → x=200,y=300
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 53, PosX = 200, PosY = 300,
                RegionStart = 100, RegionEnd = 200,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(53, 150, 999);  // death Y irrelevant
            Assert.AreEqual(ReviveMode.WalkBack, p.Mode);
            Assert.AreEqual(53, p.TargetMapId);
            Assert.AreEqual(200, p.TargetX);
            Assert.AreEqual(300, p.TargetY);
        }

        [Test]
        public void ResolveRevive_RegionBoundaryLowInclusive()
        {
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 53, PosX = 200, PosY = 300,
                RegionStart = 100, RegionEnd = 200,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(53, 100, 0);
            Assert.AreEqual(ReviveMode.WalkBack, p.Mode);
        }

        [Test]
        public void ResolveRevive_RegionBoundaryHighInclusive()
        {
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 53, PosX = 200, PosY = 300,
                RegionStart = 100, RegionEnd = 200,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(53, 200, 0);
            Assert.AreEqual(ReviveMode.WalkBack, p.Mode);
        }

        [Test]
        public void ResolveRevive_OutOfRegion_TeleportCity()
        {
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 53, PosX = 200, PosY = 300,
                RegionStart = 100, RegionEnd = 200,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(53, 999, 999);
            Assert.AreEqual(ReviveMode.TeleportCity, p.Mode);
            Assert.AreEqual(DefaultCity.MapId, p.TargetMapId);
            Assert.AreEqual(DefaultCity.PosX, p.TargetX);
            Assert.AreEqual(DefaultCity.PosY, p.TargetY);
            StringAssert.Contains("OutOfRegion", p.ReasonVi);
        }

        [Test]
        public void ResolveRevive_MapNotInTable_TeleportBoundCity()
        {
            var reg = BuildRegistry();  // empty
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(999, 0, 0);
            Assert.AreEqual(ReviveMode.TeleportBoundCity, p.Mode);
            Assert.AreEqual(DefaultCity.MapId, p.TargetMapId);
            StringAssert.Contains("MapNotInReviveTable", p.ReasonVi);
        }

        [Test]
        public void ResolveRevive_NoRegistry_DefaultsToTeleportCity()
        {
            var rt = new ReviveRuntimeService(null, null, DefaultCity);
            var p = rt.ResolveRevive(1, 0, 0);
            Assert.AreEqual(ReviveMode.TeleportCity, p.Mode);
            StringAssert.Contains("NoRegistry", p.ReasonVi);
        }

        [Test]
        public void ResolveRevive_PicksFirstMatchingRegion()
        {
            // Two sections for map 53, two different regions
            var reg = BuildRegistry(
                new PcRevivePosEntry { ReviveId = 1, MapId = 53, PosX = 10, PosY = 10, RegionStart = 0, RegionEnd = 50 },
                new PcRevivePosEntry { ReviveId = 2, MapId = 53, PosX = 20, PosY = 20, RegionStart = 51, RegionEnd = 100 });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var p = rt.ResolveRevive(53, 30, 0);
            Assert.AreEqual(ReviveMode.WalkBack, p.Mode);
            Assert.AreEqual(10, p.TargetX);  // first section wins
        }

        // --- BuildPlan --------------------------------------------------------

        [Test]
        public void BuildPlan_InPlace_ResetsFightAndNoMessage()
        {
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 1, PosX = 100, PosY = 100,
                RegionStart = 0, RegionEnd = 0,
            });
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var plan = rt.BuildPlan(1, 50, 50, 0);
            Assert.AreEqual(ReviveMode.InPlace, plan.Position.Mode);
            Assert.IsTrue(plan.ResetFightState);
            Assert.AreEqual(0, plan.DefaultFightState);
            Assert.IsFalse(plan.SendMessage);  // no message for in-place
        }

        [Test]
        public void BuildPlan_TeleportCity_SendsMessage()
        {
            var reg = BuildRegistry();
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var plan = rt.BuildPlan(999, 0, 0, 0);
            Assert.IsTrue(plan.SendMessage);
            StringAssert.Contains("thành phái", plan.MessageVi);
        }

        [Test]
        public void BuildPlan_PKDeath_AppendsPenalty()
        {
            var reg = BuildRegistry();
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var plan = rt.BuildPlan(999, 0, 0, 1);
            StringAssert.Contains("[PK]", plan.MessageVi);
            StringAssert.Contains("Phạt giết người", plan.MessageVi);
        }

        [Test]
        public void BuildPlan_NormalDeath_NoPenalty()
        {
            var reg = BuildRegistry();
            var rt = new ReviveRuntimeService(reg, null, DefaultCity);
            var plan = rt.BuildPlan(999, 0, 0, 0);
            Assert.IsFalse(plan.MessageVi.Contains("[PK]"));
        }

        // --- ExecutePlan ------------------------------------------------------

        [Test]
        public void ExecutePlan_NoHost_NoOp()
        {
            var rt = new ReviveRuntimeService(null, null, DefaultCity);
            var plan = rt.BuildPlan(999, 0, 0, 0);
            Assert.IsFalse(rt.ExecutePlan("alice", plan));
        }

        [Test]
        public void ExecutePlan_TeleportCity_CallsAllHostMethods()
        {
            var reg = BuildRegistry();
            var host = new FakeHost();
            var rt = new ReviveRuntimeService(reg, host, DefaultCity);
            var plan = rt.BuildPlan(999, 0, 0, 0);
            Assert.IsTrue(rt.ExecutePlan("alice", plan));
            Assert.AreEqual(1, host.SetPosCalls);
            Assert.AreEqual(1, host.SetFightCalls);
            Assert.AreEqual(1, host.MsgCalls);
            Assert.AreEqual(DefaultCity.MapId, host.LastMapId);
            Assert.AreEqual(0, host.LastFight);
        }

        [Test]
        public void ExecutePlan_InPlace_SkipsMessage()
        {
            var reg = BuildRegistry(new PcRevivePosEntry
            {
                ReviveId = 1, MapId = 1, PosX = 100, PosY = 100,
                RegionStart = 0, RegionEnd = 0,
            });
            var host = new FakeHost();
            var rt = new ReviveRuntimeService(reg, host, DefaultCity);
            var plan = rt.BuildPlan(1, 50, 50, 0);
            rt.ExecutePlan("alice", plan);
            Assert.AreEqual(1, host.SetPosCalls);
            Assert.AreEqual(1, host.SetFightCalls);
            Assert.AreEqual(0, host.MsgCalls);
        }

        [Test]
        public void ExecutePlan_NullPlan_NoOp()
        {
            var host = new FakeHost();
            var rt = new ReviveRuntimeService(null, host, DefaultCity);
            Assert.IsFalse(rt.ExecutePlan("alice", null));
            Assert.AreEqual(0, host.SetPosCalls);
        }
    }
}
