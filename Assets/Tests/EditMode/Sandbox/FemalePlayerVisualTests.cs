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
    /// Female player port smoke tests: verifies the PC npcres/woman SPR catalog,
    /// 8-way move directions, staged StreamingAssets, and per-action SPR loading.
    ///
    /// Female-specific:
    ///   - FM_ rider paths; horse layers use canonical MA_H* paths.
    ///   - Shadow and shoulder are explicit optional holes. LongWeapon LeftWeapon is
    ///     optional; required layers must still load when staged.
    /// </summary>
    public class FemalePlayerVisualTests
    {
        private GameObject _go;
        private string _stagingRoot;
        private readonly List<Sprite> _syntheticSprites = new();

        [SetUp]
        public void SetUp()
        {
            _stagingRoot = MalePlayerSprStaging.StageFemaleForTests();
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

        private FemalePlayerVisual CreateVisual(string name)
        {
            _go = new GameObject(name);
            var visual = _go.AddComponent<FemalePlayerVisual>();
            visual.spritesRootOverride = _stagingRoot;
            return visual;
        }

        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        private object PartRuntime(FemalePlayerVisual visual, PlayerSpritePartKind kind)
            => ((IDictionary)typeof(FemalePlayerVisual).GetField("_parts", PrivateInstance).GetValue(visual))[kind];

        private SpriteRenderer PartRenderer(FemalePlayerVisual visual, PlayerSpritePartKind kind)
            => (SpriteRenderer)PartRuntime(visual, kind).GetType().GetField("renderer", PrivateInstance)
                .GetValue(PartRuntime(visual, kind));

        private void SetSyntheticClip(FemalePlayerVisual visual, PlayerSpritePartKind kind, int framesPerDirection,
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

        private void SetSyntheticClips(FemalePlayerVisual visual, int framesPerDirection = 6)
        {
            foreach (DictionaryEntry part in (IDictionary)typeof(FemalePlayerVisual).GetField("_parts", PrivateInstance).GetValue(visual))
                SetSyntheticClip(visual, (PlayerSpritePartKind)part.Key, framesPerDirection);
        }

        private static void AssertOnlyCanonicalHairIsMissing(FemalePlayerVisual visual)
        {
            var hair = FemalePlayerSpriteCatalog.BuildParts(
                    visual.currentAction, visual.currentWeapon,
                    visual.armorVariant, visual.headVariant, visual.weaponVariant,
                    visual.hairVariant, visual.mountHorseVariant)
                .Single(part => part.kind == PlayerSpritePartKind.Hair);

            Assert.IsFalse(visual.HasAllRequiredParts);
            Assert.AreEqual(1, visual.MissingRequiredPartCount);
            CollectionAssert.AreEqual(new[] { hair.sourcePath }, visual.LastMissingRequiredParts,
                "FM_HR_019 must remain the only fail-closed required hole.");
        }

        [Test]
        public void Catalog_EmptyHandMove_HasFullFemaleLayerSet()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(9, parts.Count, "Catalog includes shoulder slot, even when its canonical source is unresolved.");
            // Required parts
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains(@"\FM_")));
            // Use catalog default, not a stale literal. Shadow and shoulder have no source path.
            var requiredKinds = new[]
            {
                PlayerSpritePartKind.Body,
                PlayerSpritePartKind.Head,
                PlayerSpritePartKind.Hair,
                PlayerSpritePartKind.LeftHand,
                PlayerSpritePartKind.RightHand,
            };
            foreach (var kind in requiredKinds)
            {
                var p = parts.First(x => x.kind == kind);
                Assert.IsTrue(p.sourcePath.Contains($"_{FemalePlayerSpriteCatalog.ArmorVariant:D3}_"),
                    $"Required part {kind} must use catalog base variant (got {p.sourcePath}).");
            }
        }

        [Test]
        public void Catalog_EmptyHandMove_UsesRN01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("RN01")),
                "Female empty-hand move uses canonical RN01.");
        }

        [Test]
        public void Catalog_EmptyHandIdle_UsesST01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("ST01")),
                "Female empty-hand idle uses ST01.");
        }

        [Test]
        public void Catalog_EmptyHandMagic_UsesMG02Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("MG02")),
                "Female empty-hand magic uses canonical MG02.");
        }

        [Test]
        public void Catalog_EmptyHandAttack_UsesAT01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("AT01")),
                "Female empty-hand attack uses AT01.");
        }

        [Test]
        public void Catalog_HiddenCastRows_UseMG01PhysicalAndMG02Magic()
        {
            foreach (var actionAndSuffix in new[]
                     {
                         (PlayerVisualAction.Attack, "MG01"),
                         (PlayerVisualAction.Attack1, "MG01"),
                         (PlayerVisualAction.Magic, "MG02"),
                     })
            {
                var parts = FemalePlayerSpriteCatalog.BuildParts(actionAndSuffix.Item1, PcWeaponType.HiddenWeapon).ToList();
                Assert.IsTrue(parts.Where(part => !string.IsNullOrEmpty(part.sourcePath))
                    .All(part => part.sourcePath.Contains(actionAndSuffix.Item2)), actionAndSuffix.Item2);
            }
        }

        [Test]
        public void Catalog_OptionalFemaleHoles_AreExplicit()
        {
            var empty = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.EmptyHand).ToList();
            var longWeapon = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.LongWeapon).ToList();
            Assert.IsFalse(empty.Single(p => p.kind == PlayerSpritePartKind.Shadow).required);
            Assert.IsFalse(empty.Single(p => p.kind == PlayerSpritePartKind.Shoulder).required);
            Assert.IsFalse(longWeapon.Single(p => p.kind == PlayerSpritePartKind.LeftWeapon).required,
                "Female LongWeapon LeftWeapon has no canonical bytes.");
            Assert.IsTrue(empty.Single(p => p.kind == PlayerSpritePartKind.RightWeapon).required,
                "Required weapon layer must not be hidden as an optional hole.");
        }

        [Test]
        public void DirectionFromMove_MapsEightWayJoystickDirections()
        {
            Assert.AreEqual(6, FemalePlayerSpriteCatalog.DirectionFromMove(Vector2.right));
            Assert.AreEqual(5, FemalePlayerSpriteCatalog.DirectionFromMove(new Vector2(1, 1)));
            Assert.AreEqual(4, FemalePlayerSpriteCatalog.DirectionFromMove(Vector2.up));
            Assert.AreEqual(3, FemalePlayerSpriteCatalog.DirectionFromMove(new Vector2(-1, 1)));
            Assert.AreEqual(2, FemalePlayerSpriteCatalog.DirectionFromMove(Vector2.left));
            Assert.AreEqual(1, FemalePlayerSpriteCatalog.DirectionFromMove(new Vector2(-1, -1)));
            Assert.AreEqual(0, FemalePlayerSpriteCatalog.DirectionFromMove(Vector2.down));
            Assert.AreEqual(7, FemalePlayerSpriteCatalog.DirectionFromMove(new Vector2(1, -1)));
        }

        [Test]
        public void Visual_UsesNumericDriverAndUnchangedAbsoluteFrameIndex()
        {
            // KNpcRes.cpp:267-299 picks first numeric non-shadow part then shares nCurFrameNo unchanged.
            var visual = CreateVisual("FemaleSharedFrameTest");
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
              var visual = CreateVisual("FemaleFrameFallbackTest");
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

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsEmptyHandParts_FromStagedSprFiles(PlayerVisualAction action)
        {
            var visual = CreateVisual($"Female{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.EmptyHand);
            visual.SetAction(action);

            Assert.AreEqual(PcWeaponType.EmptyHand, visual.currentWeapon);
            Assert.AreEqual(action, visual.currentAction);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        [Test]
        public void Visual_LoadsMoveParts_AndSwitchesDirection()
        {
            var visual = CreateVisual("FemaleMoveTest");
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.Move, visual.currentAction);
            Assert.AreEqual(6, visual.direction);
            AssertOnlyCanonicalHairIsMissing(visual);
            visual.Tick(0.1f);
            Assert.GreaterOrEqual(visual.CurrentFrameInDirection, 1);
        }

        [Test]
        public void Visual_ZeroMoveInput_StaysIdle()
        {
            var visual = CreateVisual("FemaleIdleTest");
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.zero);

            Assert.AreEqual(PlayerVisualAction.Idle, visual.currentAction);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        // ── Weapon type tests ──────────────────────────────────────────────

        [Test]
        public void Catalog_ShortWeaponIdle_UsesST04Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.ShortWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("ST04")),
                "Female short-weapon idle uses ST04.");
        }

        [Test]
        public void Catalog_ShortWeaponCastActions_UseAttackAndAttack1Banks()
        {
            var attack = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.ShortWeapon).ToList();
            var attack1 = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, PcWeaponType.ShortWeapon).ToList();
            Assert.IsTrue(attack.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("AT02")));
            Assert.IsTrue(attack1.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains("AT03")));
        }

        [Test]
        public void Catalog_LongWeaponMove_UsesRN03Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.LongWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("RN03")),
                "Female long-weapon move uses RN03.");
        }

        [Test]
        public void Catalog_LongWeaponMagic_UsesMG04Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.LongWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("MG04")),
                "Female long-weapon magic uses MG04.");
        }

        [Test]
        public void Catalog_DualWeaponAttack_UsesAT06Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.DualWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("AT06")),
                "Female dual-weapon attack uses AT06; Attack1 remains AT07.");
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsShortWeaponParts_FromStagedSprFiles(PlayerVisualAction action)
        {
            var visual = CreateVisual($"FemaleShortWeapon{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.ShortWeapon);
            visual.SetAction(action);
            Assert.AreEqual(PcWeaponType.ShortWeapon, visual.currentWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsLongWeaponParts_FromStagedSprFiles(PlayerVisualAction action)
        {
            var visual = CreateVisual($"FemaleLongWeapon{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.LongWeapon);
            visual.SetAction(action);
            Assert.AreEqual(PcWeaponType.LongWeapon, visual.currentWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        [TestCase(PlayerVisualAction.Idle)]
        [TestCase(PlayerVisualAction.Move)]
        [TestCase(PlayerVisualAction.Magic)]
        [TestCase(PlayerVisualAction.Attack)]
        public void Visual_LoadsDualWeaponParts_FromStagedSprFiles(PlayerVisualAction action)
        {
            var visual = CreateVisual($"FemaleDualWeapon{action}Test");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.DualWeapon);
            visual.SetAction(action);
            Assert.AreEqual(PcWeaponType.DualWeapon, visual.currentWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        [Test]
        public void Visual_WeaponSwitch_ReloadsAllParts()
        {
            var visual = CreateVisual("FemaleWeaponSwitchTest");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.EmptyHand);
            visual.SetAction(PlayerVisualAction.Idle);
            AssertOnlyCanonicalHairIsMissing(visual);
            visual.SetWeapon(PcWeaponType.LongWeapon);
            Assert.AreEqual(PcWeaponType.LongWeapon, visual.currentWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
            visual.SetWeapon(PcWeaponType.ShortWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
            visual.SetWeapon(PcWeaponType.DualWeapon);
            AssertOnlyCanonicalHairIsMissing(visual);
        }

        [Test]
        public void SortingOffset_ShadowAndHead_DifferByDirection()
        {
            // Head and shadow should sit at different ordering offsets in the same
            // direction (head paints over shadow) — guards against draw-order typos
            // if the female table is ever edited.
            int headS = FemalePlayerSpriteCatalog.SortingOffset(PlayerSpritePartKind.Head, 0);
            int shadS = FemalePlayerSpriteCatalog.SortingOffset(PlayerSpritePartKind.Shadow, 0);
            Assert.AreNotEqual(headS, shadS);
        }
    }
}
