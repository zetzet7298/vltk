// -----------------------------------------------------------------------------
// VLTK Mobile — PathfindingService EditMode tests.
// Kiểm tra A* algorithm: trivial path, blocked start/goal, start==goal,
// success/failure host dispatch, max expansion budget, diagonal heuristic.
// PC source: A* algorithm + lua path_event.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class PathfindingServiceTests
    {
        // ── Fake walkability ────────────────────────────────────────────────

        /// <summary>Simple grid: all walkable except listed blocked cells.</summary>
        private sealed class TestGrid : IWalkabilityProvider
        {
            public int width;
            public int height;
            public HashSet<Vector2Int> blocked = new();
            public bool CanWalk(int x, int y) => !blocked.Contains(new Vector2Int(x, y));
            public bool InSearchBounds(int x, int y) => x >= 0 && x < width && y >= 0 && y < height;
        }

        // ── Host fake ────────────────────────────────────────────────────────

        private sealed class FakeHost : IPathfindingHost
        {
            public int OverlayCalls;
            public int FoundCalls;
            public int FailedCalls;
            public int SfxCalls;
            public int LogCalls;
            public int NpcNavCalls;
            public int RewardCalls;
            public int SaveCalls;
            public int LastCellCount;
            public int LastExpanded;
            public string LastFailureReason;
            public int LastPlayerId;
            public Vector2Int[] LastOverlayCells;

            public void ShowPathOverlay(Vector2Int[] cells, Vector2Int start, Vector2Int goal)
            {
                OverlayCalls++;
                LastOverlayCells = cells;
            }
            public void OnPathFound(Vector2Int start, Vector2Int goal, int cellCount, int expandedNodes)
            {
                FoundCalls++;
                LastCellCount = cellCount;
                LastExpanded = expandedNodes;
            }
            public void OnPathFailed(Vector2Int start, Vector2Int goal, string failureReason, int expandedNodes)
            {
                FailedCalls++;
                LastFailureReason = failureReason;
                LastExpanded = expandedNodes;
            }
            public void PlayPathSFX(Vector2Int start, Vector2Int goal) { SfxCalls++; }
            public void LogPathEvent(Vector2Int start, Vector2Int goal, string message) { LogCalls++; }
            public void DispatchNpcNav(int npcId, Vector2Int[] cells) { NpcNavCalls++; }
            public void GrantPathReward(int playerId, int cellCount, int expandedNodes)
            {
                RewardCalls++;
                LastPlayerId = playerId;
            }
            public void SavePathHistory(int playerId, Vector2Int start, Vector2Int goal, int cellCount)
            {
                SaveCalls++;
                LastPlayerId = playerId;
            }
        }

        // ── Ctor ────────────────────────────────────────────────────────────

        [Test]
        public void Constructor_Default()
        {
            var svc = new PathfindingService();
            Assert.IsNotNull(svc);
        }

        [Test]
        public void Constructor_DiagonalFlag()
        {
            var svc = new PathfindingService(allowDiagonal: true);
            Assert.IsNotNull(svc);
        }

        [Test]
        public void AttachHost_Stores()
        {
            var host = new FakeHost();
            var svc = new PathfindingService();
            svc.AttachHost(host);
            var grid = new TestGrid { width = 1, height = 1 };
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(0, 0), grid);
            Assert.AreEqual(1, host.FoundCalls);
        }

        // ── Null world ──────────────────────────────────────────────────────

        [Test]
        public void FindPath_NullWorld_Fails()
        {
            var svc = new PathfindingService();
            var r = svc.FindPath(Vector2Int.zero, new Vector2Int(1, 1), null);
            Assert.IsFalse(r.found);
            Assert.IsNotNull(r.failureReason);
        }

        [Test]
        public void FindPath_NullWorld_DispatchesFailure()
        {
            var host = new FakeHost();
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(Vector2Int.zero, new Vector2Int(1, 1), null);
            Assert.AreEqual(1, host.FailedCalls);
            Assert.AreEqual(1, host.SfxCalls);
        }

        // ── Start == Goal ───────────────────────────────────────────────────

        [Test]
        public void FindPath_StartEqualsGoal_Found()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(5, 5), new Vector2Int(5, 5), grid);
            Assert.IsTrue(r.found);
            Assert.AreEqual(1, r.cells.Count);
        }

        [Test]
        public void FindPath_StartEqualsGoal_DispatchesSuccess()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(5, 5), new Vector2Int(5, 5), grid);
            Assert.AreEqual(1, host.FoundCalls);
        }

        // ── Trivial path ────────────────────────────────────────────────────

        [Test]
        public void FindPath_Adjacent_Found()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 0), grid);
            Assert.IsTrue(r.found);
            Assert.AreEqual(2, r.cells.Count);
        }

        [Test]
        public void FindPath_Manhattan_Found()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 4), grid);
            Assert.IsTrue(r.found);
            Assert.AreEqual(8, r.cells.Count); // 3+4+1 = 8
        }

        // ── Blocked ─────────────────────────────────────────────────────────

        [Test]
        public void FindPath_StartBlocked_Fails()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            grid.blocked.Add(new Vector2Int(0, 0));
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), grid);
            Assert.IsFalse(r.found);
        }

        [Test]
        public void FindPath_GoalBlocked_Fails()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            grid.blocked.Add(new Vector2Int(5, 5));
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), grid);
            Assert.IsFalse(r.found);
        }

        [Test]
        public void FindPath_StartOutOfBounds_Fails()
        {
            var grid = new TestGrid { width = 5, height = 5 };
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(10, 10), new Vector2Int(2, 2), grid);
            Assert.IsFalse(r.found);
        }

        // ── No path ─────────────────────────────────────────────────────────

        [Test]
        public void FindPath_WallSurroundsGoal_NoPath()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            // Wall around (5, 5) goal
            grid.blocked.Add(new Vector2Int(4, 5));
            grid.blocked.Add(new Vector2Int(6, 5));
            grid.blocked.Add(new Vector2Int(5, 4));
            grid.blocked.Add(new Vector2Int(5, 6));
            var svc = new PathfindingService();
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), grid);
            Assert.IsFalse(r.found);
        }

        [Test]
        public void FindPath_NoPath_DispatchesFailure()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            grid.blocked.Add(new Vector2Int(4, 5));
            grid.blocked.Add(new Vector2Int(6, 5));
            grid.blocked.Add(new Vector2Int(5, 4));
            grid.blocked.Add(new Vector2Int(5, 6));
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(5, 5), grid);
            Assert.AreEqual(1, host.FailedCalls);
        }

        // ── Max expansion ───────────────────────────────────────────────────

        [Test]
        public void FindPath_MaxExpansionsLow_Truncates()
        {
            var grid = new TestGrid { width = 50, height = 50 };
            var svc = new PathfindingService(false, 5); // very low
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(40, 40), grid);
            // Either fails by budget or by no path
            Assert.IsFalse(r.found);
        }

        // ── Host dispatch chain ────────────────────────────────────────────

        [Test]
        public void FindPath_Success_DispatchesOverlay()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 0), grid);
            Assert.AreEqual(1, host.OverlayCalls);
            Assert.AreEqual(1, host.FoundCalls);
            Assert.AreEqual(1, host.LogCalls);
            Assert.AreEqual(1, host.SaveCalls);
        }

        [Test]
        public void FindPath_Success_ArgsCorrect()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(false, 100000, host) { PlayerId = 100 };
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 0), grid);
            Assert.AreEqual(4, host.LastCellCount);
            Assert.AreEqual(100, host.LastPlayerId);
        }

        [Test]
        public void FindPath_Success_ShortPath_NoReward()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 0), grid); // 4 cells, < 10
            Assert.AreEqual(0, host.RewardCalls);
        }

        [Test]
        public void FindPath_Success_LongPath_GrantsReward()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 20, height = 20 };
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(15, 0), grid); // 16 cells, > 10
            Assert.AreEqual(1, host.RewardCalls);
        }

        [Test]
        public void FindPath_DispatchesSFX()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 5, height = 5 };
            var svc = new PathfindingService(false, 100000, host);
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), grid);
            Assert.AreEqual(1, host.SfxCalls);
        }

        [Test]
        public void FindPath_WithoutHost_DoesNotThrow()
        {
            var grid = new TestGrid { width = 5, height = 5 };
            var svc = new PathfindingService();
            Assert.DoesNotThrow(() => svc.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), grid));
        }

        [Test]
        public void FindPath_FiresOnPathCompletedEvent()
        {
            var grid = new TestGrid { width = 5, height = 5 };
            var svc = new PathfindingService();
            int fired = 0;
            svc.OnPathCompleted += r => fired++;
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(2, 0), grid);
            Assert.AreEqual(1, fired);
        }

        [Test]
        public void FindPath_Failure_FiresOnPathCompletedEvent()
        {
            var grid = new TestGrid { width = 5, height = 5 };
            var svc = new PathfindingService();
            int fired = 0;
            svc.OnPathCompleted += r => fired++;
            svc.FindPath(new Vector2Int(0, 0), new Vector2Int(1, 1), null);
            Assert.AreEqual(1, fired);
        }

        // ── Diagonal ────────────────────────────────────────────────────────

        [Test]
        public void FindPath_Diagonal_ManhattanHeuristic()
        {
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(allowDiagonal: false);
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 4), grid);
            Assert.IsTrue(r.found);
            Assert.AreEqual(8, r.cells.Count);
        }

        // ── Overlay cells ───────────────────────────────────────────────────

        [Test]
        public void FindPath_Overlay_CellsMatchResult()
        {
            var host = new FakeHost();
            var grid = new TestGrid { width = 10, height = 10 };
            var svc = new PathfindingService(false, 100000, host);
            var r = svc.FindPath(new Vector2Int(0, 0), new Vector2Int(3, 0), grid);
            Assert.AreEqual(r.cells.Count, host.LastOverlayCells.Length);
            Assert.AreEqual(r.cells[0], host.LastOverlayCells[0]);
            Assert.AreEqual(r.cells[r.cells.Count - 1], host.LastOverlayCells[host.LastOverlayCells.Length - 1]);
        }
    }
}
