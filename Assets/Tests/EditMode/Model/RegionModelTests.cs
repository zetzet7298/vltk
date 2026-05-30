using NUnit.Framework;
using VLTK.Model;

namespace VLTK.Tests.Model
{
    public class RegionModelTests
    {
        private ObstacleGrid MakeGrid(int w, int h, byte fill = 0x00)
        {
            var grid = new ObstacleGrid
            {
                mapId = 1, regionX = 0, regionY = 0,
                width = w, height = h,
                cells = new byte[w * h],
            };
            for (int i = 0; i < grid.cells.Length; i++)
                grid.cells[i] = fill;
            return grid;
        }

        [Test]
        public void CanWalk_ClearCell_ReturnsTrue()
        {
            var grid = MakeGrid(4, 4, 0x00);
            Assert.IsTrue(grid.CanWalk(2, 2));
        }

        [Test]
        public void CanWalk_WalkBlockedCell_ReturnsFalse()
        {
            var grid = MakeGrid(4, 4, 0x00);
            grid.cells[1 * 4 + 1] = ObstacleGrid.WalkBlocked;
            Assert.IsFalse(grid.CanWalk(1, 1));
        }

        [Test]
        public void CanFly_FlyBlockedCell_ReturnsFalse()
        {
            var grid = MakeGrid(4, 4, 0x00);
            grid.cells[2 * 4 + 3] = ObstacleGrid.FlyBlocked;
            Assert.IsFalse(grid.CanFly(3, 2));
            Assert.IsTrue(grid.CanWalk(3, 2));   // walk not blocked
        }

        [Test]
        public void CanJump_JumpBlockedCell_ReturnsFalse()
        {
            var grid = MakeGrid(4, 4, 0x00);
            grid.cells[0 * 4 + 0] = ObstacleGrid.JumpBlocked;
            Assert.IsFalse(grid.CanJump(0, 0));
        }

        [Test]
        public void OutOfBounds_ReturnsBlockedForWalk()
        {
            var grid = MakeGrid(4, 4, 0x00);
            Assert.IsFalse(grid.CanWalk(-1, 0));
            Assert.IsFalse(grid.CanWalk(0, -1));
            Assert.IsFalse(grid.CanWalk(4, 0));
            Assert.IsFalse(grid.CanWalk(0, 4));
        }

        [Test]
        public void GetRawFlags_OutOfBounds_Returns0xFF()
        {
            var grid = MakeGrid(2, 2, 0x00);
            Assert.AreEqual(0xFF, grid.GetRawFlags(-1, 0));
            Assert.AreEqual(0xFF, grid.GetRawFlags(0, 99));
        }

        [Test]
        public void MultipleFlagBits_CanCoexist()
        {
            var grid = MakeGrid(2, 2, 0x00);
            grid.cells[0] = ObstacleGrid.WalkBlocked | ObstacleGrid.FlyBlocked;
            Assert.IsFalse(grid.CanWalk(0, 0));
            Assert.IsFalse(grid.CanFly(0, 0));
            Assert.IsTrue(grid.CanJump(0, 0));
        }

        [Test]
        public void RegionDefinition_Serializable_DefaultValues()
        {
            var region = new RegionDefinition();
            Assert.AreEqual(0, region.mapId);
            Assert.IsNull(region.sourceRegionPath);
        }
    }
}
