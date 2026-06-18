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
    /// Female player port smoke tests: verifies the PC npcres/woman SPR catalog,
    /// 8-way move directions, staged StreamingAssets, and per-action SPR loading.
    ///
    /// Female-specific:
    ///   - Prefix FM_ (not MA_/WO_), variant 050 base outfit.
    ///   - Only 5 required parts (BD/HD/HR/LH/RH). Shadow/LW/RW are built but
    ///     marked not required because npcres/woman has no SPRs for them.
    /// </summary>
    public class FemalePlayerVisualTests
    {
        private GameObject _go;
        private string _stagingRoot;

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

        [Test]
        public void Catalog_EmptyHandMove_HasFullFemaleLayerSet()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.AreEqual(8, parts.Count, "Catalog builds 8 spec slots even if some are not required.");
            // Required parts
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            // FM_ prefix on every slot
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains(@"\FM_")));
            // Required slots (BD/HD/HR/LH/RH) all use base variant 050.
            // Shadow uses 999, weapons use 0/001/010 — those slots are not required
            // because npcres/woman has no SPRs for them.
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
                Assert.IsTrue(p.sourcePath.Contains("_050_"),
                    $"Required part {kind} must use base variant 050 (got {p.sourcePath}).");
            }
        }

        [Test]
        public void Catalog_EmptyHandMove_UsesRN01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Move, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("RN01")),
                "Female empty-hand move uses 空手跑步 (RN01) per PC 男主角未骑马关联表.txt (shared with female).");
        }

        [Test]
        public void Catalog_EmptyHandIdle_UsesST01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("ST01")),
                "Female empty-hand idle uses 空手站立1 (ST01).");
        }

        [Test]
        public void Catalog_EmptyHandMagic_UsesMG01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("MG01")),
                "Female empty-hand magic uses 空手魔法 (MG01).");
        }

        [Test]
        public void Catalog_EmptyHandAttack_UsesAT01Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.EmptyHand).ToList();
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains("AT01")),
                "Female empty-hand attack uses 空手攻击 (AT01).");
        }

        [Test]
        public void Catalog_ShadowAndWeapons_AreNotRequired()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Idle, PcWeaponType.EmptyHand).ToList();
            var shadow = parts.First(p => p.kind == PlayerSpritePartKind.Shadow);
            var lw = parts.First(p => p.kind == PlayerSpritePartKind.LeftWeapon);
            var rw = parts.First(p => p.kind == PlayerSpritePartKind.RightWeapon);
            Assert.IsFalse(shadow.required, "npcres/woman has no Shadow SPR — slot must be not required.");
            Assert.IsFalse(lw.required, "npcres/woman has no LeftWeapon SPR — slot must be not required.");
            Assert.IsFalse(rw.required, "npcres/woman has no RightWeapon SPR — slot must be not required.");
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
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"All required female {action} SPR layers must be staged. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            // 5 required parts load (BD/HD/HR/LH/RH). Shadow/LW/RW skip silently.
            Assert.AreEqual(5, visual.LoadedPartCount, "5 of 8 spec slots have SPRs (no Shadow/LW/RW in npcres/woman).");
            Assert.AreEqual(0, visual.MissingRequiredPartCount);
        }

        [Test]
        public void Visual_LoadsMoveParts_AndSwitchesDirection()
        {
            var visual = CreateVisual("FemaleMoveTest");
            visual.playAutomatically = false;
            visual.SetMoveInput(Vector2.right);

            Assert.AreEqual(PlayerVisualAction.Move, visual.currentAction);
            Assert.AreEqual(6, visual.direction);
            Assert.IsTrue(visual.HasAllRequiredParts);
            Assert.AreEqual(5, visual.LoadedPartCount);

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
            Assert.IsTrue(visual.HasAllRequiredParts);
            Assert.AreEqual(5, visual.LoadedPartCount);
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
        public void Catalog_ShortWeaponAttack_UsesAT03Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.ShortWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("AT03")),
                "Female short-weapon attack uses AT03.");
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
        public void Catalog_DualWeaponAttack_UsesAT07Suffix()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, PcWeaponType.DualWeapon).ToList();
            Assert.IsTrue(parts.Where(p => p.required).All(p => p.sourcePath.Contains("AT07")),
                "Female dual-weapon attack uses AT07.");
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
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"All required female ShortWeapon {action} SPR layers must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            Assert.AreEqual(5, visual.LoadedPartCount);
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
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"All required female LongWeapon {action} SPR layers must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            Assert.AreEqual(5, visual.LoadedPartCount);
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
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"All required female DualWeapon {action} SPR layers must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            Assert.AreEqual(5, visual.LoadedPartCount);
        }

        [Test]
        public void Visual_WeaponSwitch_ReloadsAllParts()
        {
            var visual = CreateVisual("FemaleWeaponSwitchTest");
            visual.playAutomatically = false;
            visual.SetWeapon(PcWeaponType.EmptyHand);
            visual.SetAction(PlayerVisualAction.Idle);
            Assert.IsTrue(visual.HasAllRequiredParts);
            visual.SetWeapon(PcWeaponType.LongWeapon);
            Assert.AreEqual(PcWeaponType.LongWeapon, visual.currentWeapon);
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"LongWeapon parts must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            visual.SetWeapon(PcWeaponType.ShortWeapon);
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"ShortWeapon parts must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
            visual.SetWeapon(PcWeaponType.DualWeapon);
            Assert.IsTrue(visual.HasAllRequiredParts,
                $"DualWeapon parts must load. Missing: {string.Join(", ", visual.LastMissingRequiredParts)}");
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
