// -----------------------------------------------------------------------------
// VLTK.Tests.EditMode.Survivor — SurvivorDirectionTests
// Ticket 48: verify direction convention round-trip — NpcBridge.SetDirection
// (JxNpcVisual) tạo vector từ dir index qua (270° - d*45°), và player/monster
// đều map move vector → dir index bằng MalePlayerSpriteCatalog.DirectionFromMove.
// Round-trip đẳng cấu ⇒ monster + player dùng chung 1 convention 8-hướng JX.
// -----------------------------------------------------------------------------

using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Survivor;

namespace VLTK.Tests.Survivor
{
    public class SurvivorDirectionTests
    {
        // Round-trip: dir index → vector (công thức NpcBridge) → DirectionFromMove → dir index.
        [Test]
        public void NpcBridgeAngleFormula_RoundTrips_AllEightDirections()
        {
            for (int d = 0; d < 8; d++)
            {
                float angle = (270f - d * 45f) * Mathf.Deg2Rad;
                var v = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                Assert.AreEqual(d, MalePlayerSpriteCatalog.DirectionFromMove(v),
                    $"dir {d} (angle {(270f - d * 45f)}°) round-trip fail");
            }
        }

        // Convention anchor: DirectionFromMove cardinal axes (khớp comment JX order 0=S..7=SE).
        [Test]
        public void DirectionFromMove_CardinalAxes_MatchJxOrder()
        {
            Assert.AreEqual(6, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.right), "E");
            Assert.AreEqual(4, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.up), "N");
            Assert.AreEqual(2, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.left), "W");
            Assert.AreEqual(0, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.down), "S");
        }

        [Test]
        public void DirectionFromMove_ZeroVector_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.zero));
        }

        // --- ticket 48: facing-cache semantics (SurvivorPlayer.UpdateFacing) ---

        [Test]
        public void UpdateFacing_Change_ReturnsTrueAndSetsNewFacing()
        {
            int facing = -1;
            Assert.IsTrue(SurvivorPlayer.UpdateFacing(ref facing, Vector2.right), "E");
            Assert.AreEqual(6, facing);
            Assert.IsTrue(SurvivorPlayer.UpdateFacing(ref facing, new Vector2(1f, 1f)), "NE");
            Assert.AreEqual(5, facing);
        }

        [Test]
        public void UpdateFacing_SameDirection_ReturnsFalse_NoSpam()
        {
            int facing = 6; // đang đứng hướng E
            Assert.IsFalse(SurvivorPlayer.UpdateFacing(ref facing, Vector2.right),
                "cùng hướng → không SetDirection lại (tránh spam mỗi frame)");
            Assert.AreEqual(6, facing, "facing không đổi");
        }

        [Test]
        public void UpdateFacing_Idle_KeepsLastFacing()
        {
            int facing = 7; // đang đứng hướng SE
            Assert.IsFalse(SurvivorPlayer.UpdateFacing(ref facing, Vector2.zero),
                "idle → không đổi hướng (giữ hướng cuối, không reset)");
            Assert.AreEqual(7, facing, "idle giữ hướng cuối");
            Assert.IsFalse(SurvivorPlayer.UpdateFacing(ref facing, new Vector2(0.001f, 0f)),
                "move ~ 0 → vẫn idle");
        }
    }
}
