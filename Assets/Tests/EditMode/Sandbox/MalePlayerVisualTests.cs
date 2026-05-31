// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved.
// Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Male player port smoke tests: verifies the PC SPR part catalog, 8-way move
    /// directions, staged StreamingAssets, and joystick-style continuous movement.
    /// </summary>
    public class MalePlayerVisualTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
            _go = null;
        }

        [Test]
        public void Catalog_MoveAction_HasFullMaleLayerSet()
        {
            var parts = MalePlayerSpriteCatalog.GetParts(PlayerVisualAction.Move).ToList();
            Assert.AreEqual(8, parts.Count, "Move should include shadow, body, head, hair, hands, and both weapon layers.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
        }

        [Test]
        public void DirectionFromMove_MapsEightWayJoystickDirections()
        {
            Assert.AreEqual(6, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.right));
            Assert.AreEqual(5, MalePlayerSpriteCatalog.DirectionFromMove(new Vector2(1, 1)));
            Assert.AreEqual(4, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.up));
            Assert.AreEqual(3, MalePlayerSpriteCatalog.DirectionFromMove(new Vector2(-1, 1)));
            Assert.AreEqual(2, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.left));
            Assert.AreEqual(1, MalePlayerSpriteCatalog.DirectionFromMove(new Vector2(-1, -1)));
            Assert.AreEqual(0, MalePlayerSpriteCatalog.DirectionFromMove(Vector2.down));
            Assert.AreEqual(7, MalePlayerSpriteCatalog.DirectionFromMove(new Vector2(1, -1)));
        }

        [Test]
        public void Visual_LoadsAllRequiredMoveParts_FromStagedSprFiles()
        {
            _go = new GameObject("MaleVisualTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.Move, visual.currentAction);
            Assert.AreEqual(6, visual.direction);
            Assert.IsTrue(visual.HasAllRequiredParts, "All required male SPR layers should be staged in StreamingAssets/Sprites.");
            Assert.AreEqual(8, visual.LoadedPartCount);

            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1, "Move animation should advance through RN01 frames.");
        }

        [Test]
        public void Controller_JoystickInput_MovesTransformAndSwitchesAnimation()
        {
            _go = new GameObject("PlayerControllerTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;
            controller.moveSpeed = 10f;

            controller.SetMoveInput(Vector2.right);
            controller.SimulateMove(0.5f);

            Assert.AreEqual(5f, controller.transform.position.x, 0.001f);
            Assert.AreEqual(0f, controller.transform.position.y, 0.001f);
            Assert.AreEqual(PlayerVisualAction.Move, controller.visual.currentAction);
            Assert.AreEqual(6, controller.visual.direction);

            controller.SetMoveInput(Vector2.zero);
            controller.SimulateMove(0f);
            Assert.AreEqual(PlayerVisualAction.Idle, controller.visual.currentAction);
        }
    }
}
