// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Backend — WorldGridMapperTests
// EditMode test cho WorldGridMapper (pure utility) — verify world ↔ grid
// conversion đúng theo công thức PC:
//   grid = floor(world / tile_size)  (chia nguyên)
//   world = grid * tile_size
//
// Phủ:
//   - Round-trip với world là bội số của tileSize (happy path)
//   - World không phải bội số (floor về phía 0)
//   - Zero và giá trị âm (edge case)
//   - tileSize tuỳ chỉnh (256, 1024) — server region khác nhau có thể
//     dùng tileSize khác (vd mỏ khoáng, dungeon)
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Backend.Movement;

namespace VLTK.Tests.Backend
{
    public class WorldGridMapperTests
    {
        // ============================================================
        // WorldToGridX/Y
        // ============================================================

        [Test]
        public void WorldToGridX_ExactMultiple_ReturnsInteger()
        {
            // 1024 / 512 = 2.0 → gridX = 2
            Assert.AreEqual(2, WorldGridMapper.WorldToGridX(1024f));
            Assert.AreEqual(0, WorldGridMapper.WorldToGridX(0f));
            Assert.AreEqual(1, WorldGridMapper.WorldToGridX(512f));
            Assert.AreEqual(3, WorldGridMapper.WorldToGridX(1536f));
        }

        [Test]
        public void WorldToGridX_FloorsTowardZeroForPositive()
        {
            // 1023.5 / 512 = 1.999... → floor = 1 (không phải 2)
            Assert.AreEqual(1, WorldGridMapper.WorldToGridX(1023.5f));
            // 1023.99 / 512 = 1.99996... → floor = 1 (vẫn < 2)
            Assert.AreEqual(1, WorldGridMapper.WorldToGridX(1023.99f));
            // 511.99 / 512 = 0.9999... → floor = 0
            Assert.AreEqual(0, WorldGridMapper.WorldToGridX(511.99f));
            // 1024.0 / 512 = 2.0 → floor = 2 (đúng bội số)
            Assert.AreEqual(2, WorldGridMapper.WorldToGridX(1024f));
            // 1024.5 / 512 = 2.001... → floor = 2
            Assert.AreEqual(2, WorldGridMapper.WorldToGridX(1024.5f));
        }

        [Test]
        public void WorldToGridY_MatchesWorldToGridX()
        {
            // Y-axis dùng cùng công thức như X. Sanity check: 768 / 512 = 1.
            Assert.AreEqual(1, WorldGridMapper.WorldToGridY(768f));
            Assert.AreEqual(0, WorldGridMapper.WorldToGridY(0f));
            Assert.AreEqual(1, WorldGridMapper.WorldToGridY(512.5f));
        }

        [Test]
        public void WorldToGrid_CustomTileSize256()
        {
            // tileSize 256 (dungeon / mine): 512 / 256 = 2.
            Assert.AreEqual(2, WorldGridMapper.WorldToGridX(512f, tileSize: 256f));
            Assert.AreEqual(0, WorldGridMapper.WorldToGridX(128f, tileSize: 256f));
            Assert.AreEqual(1, WorldGridMapper.WorldToGridY(300f, tileSize: 256f));
        }

        [Test]
        public void WorldToGrid_CustomTileSize1024()
        {
            // tileSize 1024 (region lớn): 1024 / 1024 = 1.
            Assert.AreEqual(1, WorldGridMapper.WorldToGridX(1024f, tileSize: 1024f));
            Assert.AreEqual(0, WorldGridMapper.WorldToGridX(1023.99f, tileSize: 1024f));
        }

        // ============================================================
        // GridToWorld
        // ============================================================

        [Test]
        public void GridToWorld_DefaultTileSize_512()
        {
            Vector2 origin = WorldGridMapper.GridToWorld(0, 0);
            Assert.AreEqual(0f, origin.x);
            Assert.AreEqual(0f, origin.y);

            Vector2 cell2 = WorldGridMapper.GridToWorld(2, 3);
            Assert.AreEqual(1024f, cell2.x); // 2 * 512
            Assert.AreEqual(1536f, cell2.y); // 3 * 512
        }

        [Test]
        public void GridToWorld_CustomTileSize_256()
        {
            Vector2 cell = WorldGridMapper.GridToWorld(3, 4, tileSize: 256f);
            Assert.AreEqual(768f, cell.x);  // 3 * 256
            Assert.AreEqual(1024f, cell.y); // 4 * 256
        }

        // ============================================================
        // Round-trip (theo yêu cầu task FS-04C)
        // ============================================================

        [Test]
        public void WorldToGrid_RoundTrip()
        {
            // Pick a worldX/worldY that are exact multiples of tileSize (512).
            // Round-trip: world → grid → world phải trả về cùng giá trị.
            float[] worlds = new float[] { 0f, 512f, 1024f, 2048f, 5120f, 10240f };
            foreach (float w in worlds)
            {
                int gx = WorldGridMapper.WorldToGridX(w);
                int gy = WorldGridMapper.WorldToGridY(w);
                Vector2 back = WorldGridMapper.GridToWorld(gx, gy);
                Assert.AreEqual(w, back.x, $"round-trip X failed for world={w}");
                Assert.AreEqual(w, back.y, $"round-trip Y failed for world={w}");
            }
        }

        [Test]
        public void WorldToGrid_RoundTrip_DifferentXY()
        {
            // worldX=1024, worldY=2048 (các ô khác nhau).
            int gx = WorldGridMapper.WorldToGridX(1024f);
            int gy = WorldGridMapper.WorldToGridY(2048f);
            Assert.AreEqual(2, gx);
            Assert.AreEqual(4, gy);
            Vector2 back = WorldGridMapper.GridToWorld(gx, gy);
            Assert.AreEqual(1024f, back.x);
            Assert.AreEqual(2048f, back.y);
        }

        [Test]
        public void WorldToGrid_RoundTrip_CustomTileSize()
        {
            // tileSize 256, world 768.
            int gx = WorldGridMapper.WorldToGridX(768f, tileSize: 256f);
            int gy = WorldGridMapper.WorldToGridY(512f, tileSize: 256f);
            Assert.AreEqual(3, gx);
            Assert.AreEqual(2, gy);
            Vector2 back = WorldGridMapper.GridToWorld(gx, gy, tileSize: 256f);
            Assert.AreEqual(768f, back.x);
            Assert.AreEqual(512f, back.y);
        }
    }
}
