// -----------------------------------------------------------------------------
// VLTK Mobile
// Copyright (c) 2026 vltk. All rights reserved. Proprietary and confidential. See LICENSE and NOTICE.md at the repo root.
// -----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using Object = UnityEngine.Object;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// Male player port smoke tests: verifies the PC SPR part catalog, 8-way move
    /// directions, runtime-staged weapon fixtures, weapon type switching, and
    /// staff equipment.
    ///
    /// CTS-06: every test routes the visual's SPR loader to a per-fixture temp
    /// directory populated by <see cref="MalePlayerSprStaging.StageForTests"/>.
    /// The catalog-level tests still rely on <see cref="MalePlayerSpriteCatalog"/>
    /// directly (no SPR bytes needed), so they pass without a visual.
    /// </summary>
    [TestFixture, Category("Slow")]
    public class MalePlayerVisualTests
    {
        private GameObject _go;
        private string _stagingRoot;
        private readonly List<Sprite> _syntheticSprites = new();

        [SetUp]
        public void SetUp()
        {
            // Stage every male-player weapon/action combination into a unique
            // temp dir so each test fixture is hermetic. The visual's
            // spritesRootOverride is set per-test after AddComponent.
            _stagingRoot = MalePlayerSprStaging.StageForTests();
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
                Object.DestroyImmediate(_go);
            foreach (var sprite in _syntheticSprites)
                Object.DestroyImmediate(sprite);
            _syntheticSprites.Clear();
            _go = null;
            MalePlayerSprStaging.CleanupTempDir(_stagingRoot);
            _stagingRoot = null;
        }

        private MalePlayerVisual CreateVisual(string name)
        {
            _go = new GameObject(name);
            var visual = _go.AddComponent<MalePlayerVisual>();
            visual.spritesRootOverride = _stagingRoot;
            return visual;
        }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private object PartRuntime(MalePlayerVisual visual, PlayerSpritePartKind kind)
            => ((IDictionary)typeof(MalePlayerVisual).GetField("_parts", PrivateInstance).GetValue(visual))[kind];

        private SpriteRenderer PartRenderer(MalePlayerVisual visual, PlayerSpritePartKind kind)
            => (SpriteRenderer)PartRuntime(visual, kind).GetType().GetField("renderer", PrivateInstance)
                .GetValue(PartRuntime(visual, kind));

        private void SetSyntheticClip(MalePlayerVisual visual, PlayerSpritePartKind kind, int framesPerDirection,
            int directionCount = 8, int spriteCount = -1)
        {
            object runtime = PartRuntime(visual, kind);
            var runtimeType = runtime.GetType();
            var clipField = runtimeType.GetField("clip", PrivateInstance);
            object clip = Activator.CreateInstance(clipField.FieldType, true);
            var clipType = clip.GetType();
            int totalFrames = spriteCount < 0 ? framesPerDirection * directionCount : spriteCount;
            clipType.GetField("totalFrames", PrivateInstance).SetValue(clip, totalFrames);
            clipType.GetField("directionCount", PrivateInstance).SetValue(clip, directionCount);
            clipType.GetField("framesPerDirection", PrivateInstance).SetValue(clip, framesPerDirection);
            var sprites = new Sprite[totalFrames];
            for (int i = 0; i < totalFrames; i++)
            {
                sprites[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
                sprites[i].name = $"{kind}_{i}";
                _syntheticSprites.Add(sprites[i]);
            }
            clipType.GetField("sprites", PrivateInstance).SetValue(clip, sprites);
            clipType.GetField("offsets", PrivateInstance).SetValue(clip, new Vector2[totalFrames]);
            clipField.SetValue(runtime, clip);
            var renderer = PartRenderer(visual, kind);
            renderer.gameObject.SetActive(true);
            renderer.enabled = framesPerDirection > 0;
            if (framesPerDirection <= 0)
                renderer.sprite = null;
        }

        private void SetSyntheticClips(MalePlayerVisual visual, int framesPerDirection = 6)
        {
            foreach (DictionaryEntry part in (IDictionary)typeof(MalePlayerVisual).GetField("_parts", PrivateInstance).GetValue(visual))
                SetSyntheticClip(visual, (PlayerSpritePartKind)part.Key, framesPerDirection);
        }

        [Test]
        public void Catalog_EmptyHandMove_HasFullMaleLayerSet()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(9, parts.Count);
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
            // Empty hand uses RW_000
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.sourcePath.Contains("RW_000")));
        }

        [Test]
        public void Catalog_Default019Shoulder_KeepsPathButIsOptional()
        {
            var shoulder = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.EmptyHand)
                .Single(part => part.kind == PlayerSpritePartKind.Shoulder);
            StringAssert.Contains("MA_SH_019_MG02.spr", shoulder.sourcePath);
            Assert.IsFalse(shoulder.required, "Package winner bytes for SH_019 are absent; path remains provenance-only.");
        }

        [Test]
        public void Catalog_StaffMove_UsesLongWeaponSuffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.LongWeapon).ToList();
            Assert.AreEqual(9, parts.Count);
            // Long staff uses RN03 suffix and RW_010 variant
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body && p.sourcePath.Contains("RN03")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.sourcePath.Contains("RW_010")));
        }

        [Test]
        public void Catalog_StaffMagic_UsesMG04Suffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.LongWeapon).ToList();
            Assert.AreEqual(9, parts.Count);
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("MG04")),
                "Staff magic cast should use 长武器魔法 (MG04) SPR files for all parts.");
        }

        [Test]
        public void Catalog_EmptyHandMagic_UsesMG02Suffix()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(9, parts.Count);
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("MG02")),
                "Empty hand magic uses canonical MG02 SPR files for all parts.");
        }

        [Test]
        public void Catalog_RideIdle_HasFullLayeredHorseAndRider()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(12, parts.Count, "Mounted catalog includes shadow, horse, shoulder, and weapon slots.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseFront));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseMiddle));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseRear));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shoulder));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon));
            // Mounted idle = RideStand = RD01 for every part.
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("RD01")),
                "Mounted idle should use RD01 (RideStand) SPRs for all parts.");
        }

        [Test]
        public void Catalog_RideWalk_UsesHW01WalkForAllParts()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideWalk, PcWeaponType.EmptyHand, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.EmptyWeaponVariant, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.MountHorseVariant).ToList();
            Assert.AreEqual(12, parts.Count);
            // PC mount table: RideWalk = HW01.
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("HW01")),
                "Mounted walk should use HW01 (RideWalk) SPRs for all parts, not HR01 gallop art.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseFront && p.sourcePath.Contains($"HH_{MalePlayerSpriteCatalog.MountHorseVariant:D3}")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body && p.sourcePath.Contains($"BD_{MalePlayerSpriteCatalog.ArmorVariant:D3}")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow && p.sourcePath.Contains("YY_999")));
        }

        [Test]
        public void Catalog_RideMove_UsesHR01GallopForAllParts()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMove, PcWeaponType.EmptyHand, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.EmptyWeaponVariant, MalePlayerSpriteCatalog.ArmorVariant, MalePlayerSpriteCatalog.MountHorseVariant).ToList();
            Assert.AreEqual(12, parts.Count);
            // Mounted move = RideRun = HR01 (gallop), 8-direction.
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("HR01")),
                "Mounted move should use HR01 (RideRun gallop) SPRs for all parts.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseFront && p.sourcePath.Contains($"HH_{MalePlayerSpriteCatalog.MountHorseVariant:D3}")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body && p.sourcePath.Contains($"BD_{MalePlayerSpriteCatalog.ArmorVariant:D3}")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow && p.sourcePath.Contains("YY_999")));
        }

        [Test]
        public void Visual_MountedWalkMode_UsesRideWalkAction()
        {
            var visual = CreateVisual("MaleMountedWalkActionTest");
            visual.SetMounted(true);
            visual.walkMode = true;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.RideWalk, visual.currentAction,
                "Mounted walk should switch to PC RideWalk/HW01 art instead of reusing HR01 gallop art.");
            Assert.AreEqual(6.6f, visual.CurrentPlaybackRate, 0.001f);

            visual.walkMode = false;
            visual.SetMoveInput(Vector2.right);
            Assert.AreEqual(PlayerVisualAction.RideMove, visual.currentAction,
                "Mounted run should switch back to PC RideRun/HR01 gallop art.");
            Assert.AreEqual(12f, visual.CurrentPlaybackRate, 0.001f);
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
        public void ResolveAction_CanonicalCastCharAnimIds_StayIndependentOfWeaponSubclass()
        {
            foreach (var weapon in new[] { PcWeaponType.EmptyHand, PcWeaponType.ShortWeapon, PcWeaponType.LongWeapon, PcWeaponType.DualWeapon, PcWeaponType.HiddenWeapon })
            {
                Assert.AreEqual(PlayerVisualAction.Attack, MalePlayerSpriteCatalog.ResolveAction(9, weapon));
                Assert.AreEqual(PlayerVisualAction.Attack1, MalePlayerSpriteCatalog.ResolveAction(10, weapon));
                Assert.AreEqual(PlayerVisualAction.Magic, MalePlayerSpriteCatalog.ResolveAction(11, weapon));
            }
        }

        [Test]
        public void Catalog_HiddenCastRows_UseMG01PhysicalAndMG02Magic()
        {
            Assert.AreEqual("MG01", MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack, PcWeaponType.HiddenWeapon));
            Assert.AreEqual("MG01", MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack1, PcWeaponType.HiddenWeapon));
            Assert.AreEqual("MG02", MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Magic, PcWeaponType.HiddenWeapon));
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
        public void Visual_UsesNumericDriverAndUnchangedAbsoluteFrameIndex()
        {
            // KNpcRes.cpp:267-299 picks first numeric non-shadow part then shares nCurFrameNo unchanged.
            var visual = CreateVisual("MaleSharedFrameTest");
            visual.playAutomatically = false;
            visual.SetAction(PlayerVisualAction.Magic);
            SetSyntheticClips(visual);
            SetSyntheticClip(visual, PlayerSpritePartKind.Head, 3);
            visual.SetDirection(1);
            visual.SetLogicalActionProgress(0.5f);
            visual.Tick(0f);

            Assert.AreEqual(1, visual.CurrentFrameInDirection);
            Assert.AreEqual("Head_4", PartRenderer(visual, PlayerSpritePartKind.Head).sprite.name);
            Assert.AreEqual("Hair_4", PartRenderer(visual, PlayerSpritePartKind.Hair).sprite.name);
            Assert.AreEqual("Body_4", PartRenderer(visual, PlayerSpritePartKind.Body).sprite.name);
            Assert.AreEqual("RightWeapon_4", PartRenderer(visual, PlayerSpritePartKind.RightWeapon).sprite.name);

            visual.SetMounted(true);
            visual.SetAction(PlayerVisualAction.Magic);
            SetSyntheticClips(visual);
            SetSyntheticClip(visual, PlayerSpritePartKind.Head, 3);
            visual.SetDirection(1);
            visual.SetLogicalActionProgress(0.5f);
            visual.Tick(0f);
            Assert.AreEqual("HorseFront_4", PartRenderer(visual, PlayerSpritePartKind.HorseFront).sprite.name);
            Assert.AreEqual("HorseMiddle_4", PartRenderer(visual, PlayerSpritePartKind.HorseMiddle).sprite.name);
        }

        [Test]
        public void Visual_FallsBackNumericallyAndRecoversFromMissingAbsoluteFrame()
        {
              var visual = CreateVisual("MaleFrameFallbackTest");
              visual.playAutomatically = false;
              visual.SetAction(PlayerVisualAction.Magic);
            SetSyntheticClips(visual);
            SetSyntheticClip(visual, PlayerSpritePartKind.Head, 0);
            SetSyntheticClip(visual, PlayerSpritePartKind.Hair, 3);
            visual.SetDirection(1);
            visual.SetLogicalActionProgress(0.5f);
            visual.Tick(0f);
            Assert.AreEqual(1, visual.CurrentFrameInDirection);
            Assert.AreEqual("Hair_4", PartRenderer(visual, PlayerSpritePartKind.Hair).sprite.name);

            SetSyntheticClip(visual, PlayerSpritePartKind.Hair, 0);
            SetSyntheticClip(visual, PlayerSpritePartKind.Shoulder, 3);
            visual.Tick(0f);
            Assert.AreEqual("Shoulder_4", PartRenderer(visual, PlayerSpritePartKind.Shoulder).sprite.name);

            SetSyntheticClip(visual, PlayerSpritePartKind.Shoulder, 0);
            SetSyntheticClip(visual, PlayerSpritePartKind.Body, 3);
            visual.Tick(0f);
            Assert.AreEqual("Body_4", PartRenderer(visual, PlayerSpritePartKind.Body).sprite.name);

              SetSyntheticClip(visual, PlayerSpritePartKind.RightHand, 6, spriteCount: 4);
              visual.Tick(0f);
              Assert.IsFalse(PartRenderer(visual, PlayerSpritePartKind.RightHand).enabled);
              Assert.IsNull(PartRenderer(visual, PlayerSpritePartKind.RightHand).sprite);
              Assert.IsFalse(visual.HasAllRequiredParts);

              visual.SetLogicalActionProgress(0f);
              visual.Tick(0f);
              Assert.IsTrue(PartRenderer(visual, PlayerSpritePartKind.RightHand).enabled);
              Assert.AreEqual("RightHand_3", PartRenderer(visual, PlayerSpritePartKind.RightHand).sprite.name);
              Assert.IsTrue(visual.HasAllRequiredParts);
        }

        [Test]
        public void Visual_LoadsAllRequiredMoveParts_EmptyHand()
        {
            var visual = CreateVisual("MaleVisualTest");
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.Move, visual.currentAction);
            Assert.AreEqual(6, visual.direction);
            Assert.IsTrue(visual.HasAllRequiredParts);
            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1);
        }

        [Test]
        public void Visual_LoadsStaffIdleParts_FromStagedSprFiles()
        {
            var visual = CreateVisual("MaleStaffIdleTest");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);

            Assert.AreEqual(PcWeaponType.LongWeapon, visual.currentWeapon);
            Assert.AreEqual(PlayerVisualAction.Idle, visual.currentAction);
            // Staff idle uses ST05 suffix. Long staff has no left weapon SPR so only 7 parts load.
            Assert.IsTrue(visual.HasAllRequiredParts,
                "All required staff idle SPR layers (ST05) should be staged.");
        }

        [Test]
        public void Visual_LoadsStaffMagicParts_FromStagedSprFiles()
        {
            var visual = CreateVisual("MaleStaffMagicTest");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);
            visual.SetAction(PlayerVisualAction.Magic);

            Assert.AreEqual(PlayerVisualAction.Magic, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts,
                "All required staff magic SPR layers (MG04) should be staged.");
            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1);
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsShortWeaponParts_FromPakStagedSprFiles(PlayerVisualAction action)
        {
            var visual = CreateVisual($"MaleShortWeapon{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.ShortWeapon);
            visual.SetAction(action);

            Assert.AreEqual(PcWeaponType.ShortWeapon, visual.currentWeapon);
            Assert.AreEqual(action, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_DefaultDualWeaponParts_AreCompleteWhenStaged(PlayerVisualAction action)
        {
            var visual = CreateVisual($"MaleDualWeapon{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.DualWeapon);
            visual.SetAction(action);

            Assert.AreEqual(PcWeaponType.DualWeapon, visual.currentWeapon);
            Assert.AreEqual(action, visual.currentAction);
            // SpritesRuntime contains canonical DualWeaponVariant 013 LW/RW bytes for ST06 and RN04.
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Catalog_DualWeapon_UsesBothSongKiem013Layers()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.DualWeapon).ToList();
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon && p.sourcePath.Contains("LW_013_RN04")));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.sourcePath.Contains("RW_013_RN04")));
        }

        [Test]
        public void Visual_LoadsEmptyHandMagicParts_FromStagedSprFiles()
        {
            var visual = CreateVisual("MaleMagicVisualTest");
            visual.playAutomatically = false;
            visual.SetAction(PlayerVisualAction.Magic);

            Assert.AreEqual(PlayerVisualAction.Magic, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts);
        }

        [Test]
        public void Visual_LoadsStaffAttackParts_FromStagedSprFiles()
        {
            var visual = CreateVisual("MaleStaffAttackTest");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);
            visual.SetAction(PlayerVisualAction.Attack);

            Assert.AreEqual(PlayerVisualAction.Attack, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Visual_LoadsEmptyHandAttackParts_FromPakStagedSprFiles()
        {
            var visual = CreateVisual("MaleEmptyHandAttackTest");
            visual.playAutomatically = false;
            visual.SetAction(PlayerVisualAction.Attack);

            Assert.AreEqual(PlayerVisualAction.Attack, visual.currentAction);
            Assert.IsTrue(visual.HasAllRequiredParts, string.Join("\n", visual.LastMissingRequiredParts));
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Controller_EquipStaff_SwitchesWeaponType()
        {
            _go = new GameObject("PlayerWeaponTest");
            var controller = _go.AddComponent<SandboxPlayerController>();
            controller.followCameraEnabled = false;
            controller.allowKeyboardFallback = false;
            // Awake already auto-created a child visual; route it at the staged
            // fixture dir so the forced refresh inside EquipWeapon resolves parts
            // from the test fixture instead of the global StreamingAssets/Sprites.
            if (controller.visual is MalePlayerVisual maleV)
                maleV.spritesRootOverride = _stagingRoot;

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
            if (controller.visual is MalePlayerVisual maleV)
                maleV.spritesRootOverride = _stagingRoot;
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
            if (controller.visual is MalePlayerVisual maleV)
                maleV.spritesRootOverride = _stagingRoot;
            controller.EquipWeapon(PcWeaponType.LongWeapon);

            controller.SetMoveInput(Vector2.right);
            controller.PlayPcSkillAction(9, 0.5f);
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
            if (controller.visual is MalePlayerVisual maleV)
                maleV.spritesRootOverride = _stagingRoot;

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
