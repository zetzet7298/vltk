// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved. Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Male player port smoke tests: verifies the PC SPR part catalog, 8-way move
    /// directions, staged StreamingAssets, weapon type switching, and staff equipment.
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
        public void Catalog_EmptyHandMove_HasFullMaleLayerSet()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(8, parts.Count);
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
            // Empty hand uses RW_000
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.sourcePath.Contains("RW_000")));
        }

        [Test]
        public void Catalog_StaffMove_UsesLongWeaponSuffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.LongWeapon).ToList();
            Assert.AreEqual(8, parts.Count);
            // Long staff uses RN03 suffix and RW_010 variant
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body && p.sourcePath.Contains("RN03")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.sourcePath.Contains("RW_010")));
        }

        [Test]
        public void Catalog_StaffMagic_UsesMG04Suffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.LongWeapon).ToList();
            Assert.AreEqual(8, parts.Count);
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("MG04")),
                "Staff magic cast should use 长武器魔法 (MG04) SPR files for all parts.");
        }

        [Test]
        public void Catalog_EmptyHandMagic_UsesMG01Suffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(8, parts.Count);
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("MG01")),
                "Empty hand magic should use 空手魔法 (MG01) SPR files for all parts.");
        }

        [Test]
        public void ResolveAction_CharAnimId11_ReturnsMagic()
        {
            Assert.AreEqual(PlayerVisualAction.Magic,
                MalePlayerSpriteCatalog.ResolveAction(11, PcWeaponType.LongWeapon));
            Assert.AreEqual(PlayerVisualAction.Magic,
                MalePlayerSpriteCatalog.ResolveAction(11, PcWeaponType.EmptyHand));
        }

        [Test]
        public void ResolveAction_CharAnimId14_ReturnsNull()
        {
            Assert.IsNull(MalePlayerSpriteCatalog.ResolveAction(14, PcWeaponType.LongWeapon));
        }

        [Test]
        public void ResolveAction_AttackCharAnimIds_ReturnAttack()
        {
            Assert.AreEqual(PlayerVisualAction.Attack,
                MalePlayerSpriteCatalog.ResolveAction(7, PcWeaponType.LongWeapon));
            Assert.AreEqual(PlayerVisualAction.Attack,
                MalePlayerSpriteCatalog.ResolveAction(8, PcWeaponType.EmptyHand));
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
        public void Visual_LoadsAllRequiredMoveParts_EmptyHand()
        {
            _go = new GameObject("MaleVisualTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.Move, visual.currentAction);
            Assert.AreEqual(6, visual.direction);
            Assert.IsTrue(visual.HasAllRequiredParts);
            Assert.AreEqual(8, visual.LoadedPartCount);

            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1);
        }

        [Test]
        public void Visual_LoadsStaffIdleParts_FromStagedSprFiles()
        {
            _go = new GameObject("MaleStaffIdleTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);

            Assert.AreEqual(PcWeaponType.LongWeapon, visual.currentWeapon);
            Assert.AreEqual(PlayerVisualAction.Idle, visual.currentAction);
            // Staff idle uses ST05 suffix. Long staff has no left weapon SPR so only 7 parts load.
            Assert.IsTrue(visual.HasAllRequiredParts,
                "All required staff idle SPR layers (ST05) should be staged.");
            Assert.AreEqual(7, visual.LoadedPartCount, "Staff has no left weapon SPR — 7 of 8 parts load.");
        }

        [Test]
        public void Visual_LoadsStaffMagicParts_FromStagedSprFiles()
        {
            _go = new GameObject("MaleStaffMagicTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);
            visual.SetAction(PlayerVisualAction.Magic);

            Assert.AreEqual(PlayerVisualAction.Magic, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts,
                "All required staff magic SPR layers (MG04) should be staged.");
            Assert.AreEqual(7, visual.LoadedPartCount, "Staff has no left weapon SPR — 7 of 8 parts load.");

            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1);
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsShortWeaponParts_FromPakStagedSprFiles(PlayerVisualAction action)
        {
            _go = new GameObject($"MaleShortWeapon{action}Test");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.ShortWeapon);
            visual.SetAction(action);

            Assert.AreEqual(PcWeaponType.ShortWeapon, visual.currentWeapon);
            Assert.AreEqual(action, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(8, visual.LoadedPartCount);
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Visual_LoadsEmptyHandMagicParts_FromStagedSprFiles()
        {
            _go = new GameObject("MaleMagicVisualTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetAction(PlayerVisualAction.Magic);

            Assert.AreEqual(PlayerVisualAction.Magic, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts);
            Assert.AreEqual(8, visual.LoadedPartCount);
        }

        [Test]
        public void Visual_LoadsStaffAttackParts_FromStagedSprFiles()
        {
            _go = new GameObject("MaleStaffAttackTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);
            visual.SetAction(PlayerVisualAction.Attack);

            Assert.AreEqual(PlayerVisualAction.Attack, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(7, visual.LoadedPartCount, "Staff attack AT05 has no left weapon SPR — 7 of 8 parts load.");
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Visual_LoadsEmptyHandAttackParts_FromPakStagedSprFiles()
        {
            _go = new GameObject("MaleEmptyHandAttackTest");
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.playAutomatically = false;
            visual.SetAction(PlayerVisualAction.Attack);

            Assert.AreEqual(PlayerVisualAction.Attack, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(8, visual.LoadedPartCount);
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Controller_EquipStaff_SwitchesWeaponType()
        {
            _go = new GameObject("PlayerWeaponTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;

            Assert.AreEqual(PcWeaponType.EmptyHand, controller.EquippedWeapon);

            controller.EquipWeapon(PcWeaponType.LongWeapon);
            Assert.AreEqual(PcWeaponType.LongWeapon, controller.EquippedWeapon);
            Assert.AreEqual(PcWeaponType.LongWeapon, controller.visual.currentWeapon);
        }

        [Test]
        public void Controller_StaffSkillAction_LocksMagicAnimation()
        {
            _go = new GameObject("PlayerStaffMagicTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;
            controller.EquipWeapon(PcWeaponType.LongWeapon);

            controller.SetMoveInput(Vector2.right);
            controller.PlayPcSkillAction(11, 0.5f);
            controller.SimulateMove(0.1f);

            Assert.AreEqual(PlayerVisualAction.Magic, controller.visual.currentAction);
            Assert.AreEqual(PcWeaponType.LongWeapon, controller.visual.currentWeapon);
        }

        [Test]
        public void Controller_StaffAttackSkillAction_LocksAttackAnimation()
        {
            _go = new GameObject("PlayerStaffAttackTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;
            controller.EquipWeapon(PcWeaponType.LongWeapon);

            controller.SetMoveInput(Vector2.right);
            controller.PlayPcSkillAction(8, 0.5f);
            controller.SimulateMove(0.1f);

            Assert.AreEqual(PlayerVisualAction.Attack, controller.visual.currentAction);
            Assert.AreEqual(PcWeaponType.LongWeapon, controller.visual.currentWeapon);
        }

        [Test]
        public void Controller_JoystickInput_MovesTransformAndSwitchesAnimation()
        {
            _go = new GameObject("PlayerControllerTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;
            controller.moveSpeed = 10f;
            controller.clampToMapBounds = false; // unit test: movement logic only

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
