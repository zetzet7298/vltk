// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for waypoint travel (PC waypoint.txt 225 rows).
// Travel flow: validate waypoint exists, validate player level, validate
// destination != current, then consume item + SetPos + SetFightState + Msg.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class WaypointTravelServiceTests
    {
        // --- Fixtures ---------------------------------------------------------

        private static PcWaypointRegistry BuildRegistry(params PcWaypointEntry[] entries)
        {
            var reg = new PcWaypointRegistry();
            foreach (var e in entries) reg.Add(e);
            return reg;
        }

        private sealed class FakeHost : IWaypointHost
        {
            public int PlayerLevel = 50;
            public bool InNoTravelMap = false;
            public bool ConsumeOk = true;
            public int ConsumeCalls;
            public int SetPosCalls;
            public int SetFightCalls;
            public int MsgCalls;
            public int LastMapId, LastX, LastY, LastFight;
            public int LastConsumeWaypointId;
            public string LastMsg;
            public string LastPlayer;

            public int GetPlayerLevel(string player) { LastPlayer = player; return PlayerLevel; }
            public bool IsInNoTravelMap(string player) { LastPlayer = player; return InNoTravelMap; }
            public bool ConsumeWaypointItem(string player, int waypointId)
            {
                ConsumeCalls++;
                LastPlayer = player;
                LastConsumeWaypointId = waypointId;
                return ConsumeOk;
            }
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

        // --- DecideTravel -----------------------------------------------------

        [Test]
        public void DecideTravel_NoRegistry_Denies()
        {
            var svc = new WaypointTravelService(null, null);
            var d = svc.DecideTravel(1, 50, 1);
            Assert.IsFalse(d.Allowed);
            StringAssert.Contains("NoRegistry", d.ReasonVi);
        }

        [Test]
        public void DecideTravel_UnknownWaypoint_Denies()
        {
            var reg = BuildRegistry();
            var svc = new WaypointTravelService(reg, null);
            var d = svc.DecideTravel(999, 50, 1);
            Assert.IsFalse(d.Allowed);
            StringAssert.Contains("UnknownWaypoint", d.ReasonVi);
        }

        [Test]
        public void DecideTravel_LevelTooLow_Denies()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "Ba Lăng Huyện", RequiredLevel = 60, FightState = 0,
            });
            var svc = new WaypointTravelService(reg, null);
            var d = svc.DecideTravel(7, 50, 1);
            Assert.IsFalse(d.Allowed);
            StringAssert.Contains("LevelTooLow", d.ReasonVi);
            Assert.AreEqual(7, d.Waypoint.WaypointId);
        }

        [Test]
        public void DecideTravel_AlreadyAtDestination_Denies()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "Ba Lăng Huyện", RequiredLevel = 30, FightState = 0,
            });
            var svc = new WaypointTravelService(reg, null);
            var d = svc.DecideTravel(7, 50, 53);
            Assert.IsFalse(d.Allowed);
            StringAssert.Contains("AlreadyAtDestination", d.ReasonVi);
        }

        [Test]
        public void DecideTravel_Valid_Allows()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "Ba Lăng Huyện", RequiredLevel = 30, FightState = 0,
            });
            var svc = new WaypointTravelService(reg, null);
            var d = svc.DecideTravel(7, 50, 1);
            Assert.IsTrue(d.Allowed);
            Assert.AreEqual(7, d.Waypoint.WaypointId);
        }

        [Test]
        public void DecideTravel_LevelExactlyRequired_Allows()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "X", RequiredLevel = 50, FightState = 1,
            });
            var svc = new WaypointTravelService(reg, null);
            var d = svc.DecideTravel(7, 50, 1);
            Assert.IsTrue(d.Allowed);
        }

        // --- BuildPlan --------------------------------------------------------

        [Test]
        public void BuildPlan_PopulatesAllFields()
        {
            var wp = new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "Ba Lăng Huyện", RequiredLevel = 30, FightState = 0,
            };
            var svc = new WaypointTravelService(null, null);
            var plan = svc.BuildPlan(wp);
            Assert.AreSame(wp, plan.Waypoint);
            Assert.IsTrue(plan.ConsumeItem);
            Assert.IsTrue(plan.SetPos);
            Assert.IsTrue(plan.SetFightState);
            Assert.IsTrue(plan.SendMessage);
            StringAssert.Contains("Ba Lăng Huyện", plan.MessageVi);
        }

        [Test]
        public void BuildPlan_NullWaypoint_EmptyMessage()
        {
            var svc = new WaypointTravelService(null, null);
            var plan = svc.BuildPlan(null);
            Assert.IsTrue(string.IsNullOrEmpty(plan.MessageVi));
        }

        // --- ExecutePlan ------------------------------------------------------

        [Test]
        public void ExecutePlan_NoHost_NoOp()
        {
            var svc = new WaypointTravelService(null, null);
            var plan = new PcWaypointTravelPlan { Waypoint = new PcWaypointEntry { WaypointId = 1 } };
            Assert.IsFalse(svc.ExecutePlan("alice", plan));
        }

        [Test]
        public void ExecutePlan_AllSteps_Dispatched()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "Ba Lăng Huyện", RequiredLevel = 30, FightState = 0,
            });
            var host = new FakeHost();
            var svc = new WaypointTravelService(reg, host);
            var d = svc.DecideTravel(7, 50, 1);
            Assert.IsTrue(d.Allowed);
            var plan = svc.BuildPlan(d.Waypoint);
            Assert.IsTrue(svc.ExecutePlan("alice", plan));
            Assert.AreEqual(1, host.ConsumeCalls);
            Assert.AreEqual(1, host.SetPosCalls);
            Assert.AreEqual(1, host.SetFightCalls);
            Assert.AreEqual(1, host.MsgCalls);
            Assert.AreEqual(7, host.LastConsumeWaypointId);
            Assert.AreEqual(53, host.LastMapId);
            Assert.AreEqual(200, host.LastX);
            Assert.AreEqual(300, host.LastY);
            Assert.AreEqual(0, host.LastFight);
        }

        [Test]
        public void ExecutePlan_NoTravelMap_DeniesBeforeConsume()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "X", RequiredLevel = 30, FightState = 0,
            });
            var host = new FakeHost { InNoTravelMap = true };
            var svc = new WaypointTravelService(reg, host);
            var plan = svc.BuildPlan(reg.Get(7));
            Assert.IsFalse(svc.ExecutePlan("alice", plan));
            Assert.AreEqual(0, host.ConsumeCalls);
            Assert.AreEqual(0, host.SetPosCalls);
        }

        [Test]
        public void ExecutePlan_ConsumeFails_RollsBack()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "X", RequiredLevel = 30, FightState = 0,
            });
            var host = new FakeHost { ConsumeOk = false };
            var svc = new WaypointTravelService(reg, host);
            var plan = svc.BuildPlan(reg.Get(7));
            Assert.IsFalse(svc.ExecutePlan("alice", plan));
            Assert.AreEqual(0, host.SetPosCalls);
            Assert.AreEqual(0, host.SetFightCalls);
        }

        [Test]
        public void ExecutePlan_NullPlan_NoOp()
        {
            var host = new FakeHost();
            var svc = new WaypointTravelService(null, host);
            Assert.IsFalse(svc.ExecutePlan("alice", null));
            Assert.AreEqual(0, host.SetPosCalls);
        }

        [Test]
        public void ExecutePlan_PlanWithNullWaypoint_NoOp()
        {
            var host = new FakeHost();
            var svc = new WaypointTravelService(null, host);
            Assert.IsFalse(svc.ExecutePlan("alice", new PcWaypointTravelPlan()));
            Assert.AreEqual(0, host.SetPosCalls);
        }

        // --- FightState propagation -------------------------------------------

        [Test]
        public void ExecutePlan_CombatFightState_Propagated()
        {
            var reg = BuildRegistry(new PcWaypointEntry
            {
                WaypointId = 7, MapId = 53, PosX = 200, PosY = 300,
                Name = "PK Zone", RequiredLevel = 30, FightState = 1,
            });
            var host = new FakeHost();
            var svc = new WaypointTravelService(reg, host);
            var d = svc.DecideTravel(7, 50, 1);
            var plan = svc.BuildPlan(d.Waypoint);
            svc.ExecutePlan("alice", plan);
            Assert.AreEqual(1, host.LastFight);  // PC: waypoint forces combat state
        }
    }
}
