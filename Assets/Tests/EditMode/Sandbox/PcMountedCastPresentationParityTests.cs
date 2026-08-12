using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    public class PcMountedCastPresentationParityTests
    {
        private static readonly PcWeaponType[] WeaponFamilies =
        {
            PcWeaponType.EmptyHand, PcWeaponType.ShortWeapon, PcWeaponType.LongWeapon,
            PcWeaponType.DualWeapon, PcWeaponType.HiddenWeapon,
        };

        [TestCase(9, PlayerVisualAction.Attack)]
        [TestCase(10, PlayerVisualAction.Attack1)]
        [TestCase(11, PlayerVisualAction.Magic)]
        public void CharAnim_ResolvesCanonicalPhysicalAndMagicBanks(int charAnimId, PlayerVisualAction expected)
        {
            foreach (var weapon in WeaponFamilies)
            {
                Assert.AreEqual(expected, MalePlayerSpriteCatalog.ResolveAction(charAnimId, weapon));
                Assert.AreEqual(expected, SandboxPlayerController.ResolveAction(charAnimId, weapon));
            }
        }

        [TestCase(PcWeaponType.EmptyHand, 0, "AT01", "AT01", "MG02")]
        [TestCase(PcWeaponType.ShortWeapon, 1, "AT02", "AT03", "MG03")]
        [TestCase(PcWeaponType.LongWeapon, 10, "AT05", "AT04", "MG04")]
        [TestCase(PcWeaponType.DualWeapon, 13, "AT06", "AT07", "MG05")]
        [TestCase(PcWeaponType.HiddenWeapon, 0, "MG01", "MG01", "MG02")]
        public void FootResolver_UsesCanonicalFamilyRows(PcWeaponType weapon, int variant, string attack, string attack1, string magic)
        {
            Assert.AreEqual(attack, MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack, weapon, variant));
            Assert.AreEqual(attack1, MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack1, weapon, variant));
            Assert.AreEqual(magic, MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Magic, weapon, variant));
        }

        [TestCase(PcWeaponType.ShortWeapon, 4, "AT03", "AT02")]
        [TestCase(PcWeaponType.LongWeapon, 10, "AT05", "AT04")]
        [TestCase(PcWeaponType.DualWeapon, 16, "AT07", "AT06")]
        public void FootResolver_AlternateSubclassVariants_SwapOnlyPhysicalOrder(PcWeaponType weapon, int variant, string attack, string attack1)
        {
            Assert.AreEqual(PcWeaponMotionProfile.AlternatePhysicalOrder, MalePlayerSpriteCatalog.ResolveMotionProfile(weapon, variant));
            Assert.AreEqual(attack, MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack, weapon, variant));
            Assert.AreEqual(attack1, MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack1, weapon, variant));
            AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon, weaponVariant: variant), attack);
            AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon, weaponVariant: variant), attack1);
            AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon, weaponVariant: variant), attack);
            AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon, weaponVariant: variant), attack1);
        }

        [Test]
        public void FootResolver_EveryCanonicalBaseWeaponVariant_UsesItsPcPhysicalOrder()
        {
            AssertCanonicalFootVariants(
                PcWeaponType.ShortWeapon,
                new[] { 1, 2, 3, 19 },
                new[] { 4, 5, 6, 20, 21, 22 },
                "AT02", "AT03");
            AssertCanonicalFootVariants(
                PcWeaponType.LongWeapon,
                new[] { 7, 8, 9, 23, 24 },
                new[] { 10, 11, 12, 25, 26 },
                "AT04", "AT05");
            AssertCanonicalFootVariants(
                PcWeaponType.DualWeapon,
                new[] { 13, 14, 15, 27, 28 },
                new[] { 16, 17, 18, 29, 30 },
                "AT06", "AT07");
        }

        [Test]
        public void FootCatalog_UsesCanonicalHandsAndWeapons_ForBothSexesAndFamilies()
        {
            foreach (var weapon in WeaponFamilies)
            {
                string magic = weapon switch
                {
                    PcWeaponType.EmptyHand => "MG02",
                    PcWeaponType.HiddenWeapon => "MG02",
                    PcWeaponType.ShortWeapon => "MG03",
                    PcWeaponType.LongWeapon => "MG04",
                    _ => "MG05",
                };
                string attack = weapon switch
                {
                    PcWeaponType.EmptyHand => "AT01",
                    PcWeaponType.ShortWeapon => "AT02",
                    PcWeaponType.LongWeapon => "AT05",
                    PcWeaponType.DualWeapon => "AT06",
                    _ => "MG01",
                };
                string attack1 = weapon switch
                {
                    PcWeaponType.EmptyHand => "AT01",
                    PcWeaponType.ShortWeapon => "AT03",
                    PcWeaponType.LongWeapon => "AT04",
                    PcWeaponType.DualWeapon => "AT07",
                    _ => "MG01",
                };
                AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, weapon), magic);
                AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, weapon), magic);
                AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon), attack);
                AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon), attack);
                AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon), attack1);
                AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon), attack1);
            }
        }

        [Test]
        public void MountedCatalog_UsesCanonicalBanksAndLayers_ForBothSexesAndFamilies()
        {
            foreach (var weapon in WeaponFamilies)
            {
                string attack = weapon == PcWeaponType.HiddenWeapon ? "HM01" : "HA01";
                string attack1 = weapon == PcWeaponType.HiddenWeapon ? "HM01" : "HA02";
                AssertMounted(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideAttack, weapon), attack, "MA_");
                AssertMounted(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideAttack1, weapon), attack1, "MA_");
                AssertMounted(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, weapon), "HM01", "MA_");
                AssertMounted(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideAttack, weapon), attack, "FM_");
                AssertMounted(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideAttack1, weapon), attack1, "FM_");
                var female = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, weapon);
                AssertMounted(female, "HM01", "FM_");
                Assert.IsTrue(female.Where(p => p.kind is PlayerSpritePartKind.HorseFront or PlayerSpritePartKind.HorseMiddle or PlayerSpritePartKind.HorseRear)
                    .All(p => p.sourcePath.Contains(@"\MA_H")), "Female horse source winner is MA_H*, not guessed FM_H*.");
            }
        }

        [Test]
        public void LiveEquipChange_AppliesWeaponFamilyBeforeExactVariant_ForMaleControllerAndFemaleVisual()
        {
            string root = CreateEmptySpritesRoot();
            var maleGo = new GameObject("LiveEquipMaleOrder");
            var femaleGo = new GameObject("LiveEquipFemaleOrder");
            try
            {
                var controller = maleGo.AddComponent<SandboxPlayerController>();
                controller.allowKeyboardFallback = false;
                controller.EquipWeapon(PcWeaponType.EmptyHand);
                var male = (MalePlayerVisual)controller.visual;
                male.spritesRootOverride = root;
                male.logMissingParts = false;
                controller.visual.SetMounted(false);
                controller.visual.SetAction(PlayerVisualAction.Idle);
                int maleRefreshes = controller.visual.ActionPartsRefreshCount;

                SandboxManager.ApplyEquipmentVisualChange(controller.visual, PlayerEquipSlot.Weapon, 0, 4, controller.EquipWeapon);

                AssertRefreshDelta(controller.visual, maleRefreshes, 1);
                Assert.AreEqual(PcWeaponType.ShortWeapon, controller.EquippedWeapon);
                Assert.AreEqual(PcWeaponType.ShortWeapon, controller.visual.currentWeapon);
                AssertHasMissingPath(controller.visual, MalePlayerSpriteCatalog.BuildPath("RW", 4, "ST04"));
                AssertNoMissingPath(controller.visual, MalePlayerSpriteCatalog.BuildPath("RW", MalePlayerSpriteCatalog.ShortWeaponVariant, "ST04"));

                var female = femaleGo.AddComponent<FemalePlayerVisual>();
                female.spritesRootOverride = root;
                female.logMissingParts = false;
                int femaleRefreshes = female.ActionPartsRefreshCount;

                SandboxManager.ApplyEquipmentVisualChange(female, PlayerEquipSlot.Weapon, 0, 4, female.SetWeapon);

                AssertRefreshDelta(female, femaleRefreshes, 1);
                Assert.AreEqual(PcWeaponType.ShortWeapon, female.currentWeapon);
                AssertHasMissingPath(female, FemalePlayerSpriteCatalog.BuildPath("RW", 4, "ST04"));
                AssertNoMissingPath(female, FemalePlayerSpriteCatalog.BuildPath("RW", FemalePlayerSpriteCatalog.ShortWeaponVariant, "ST04"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(maleGo);
                UnityEngine.Object.DestroyImmediate(femaleGo);
                DeleteRoot(root);
            }
        }

        [Test]
        public void LiveEquipChange_SameFamilyVariantRefreshesOnce_AndExactNoopRefreshesZero()
        {
            string root = CreateEmptySpritesRoot();
            var maleGo = new GameObject("LiveEquipSameFamilyOrder");
            var femaleGo = new GameObject("LiveEquipFemaleNoopOrder");
            try
            {
                var male = maleGo.AddComponent<MalePlayerVisual>();
                male.spritesRootOverride = root;
                male.logMissingParts = false;
                male.SetWeapon(PcWeaponType.ShortWeapon, MalePlayerSpriteCatalog.ShortWeaponVariant);
                int maleRefreshes = male.ActionPartsRefreshCount;

                SandboxManager.ApplyEquipmentVisualChange(male, PlayerEquipSlot.Weapon, 0, 4);

                AssertRefreshDelta(male, maleRefreshes, 1);
                Assert.AreEqual(PcWeaponType.ShortWeapon, male.currentWeapon);
                AssertHasMissingPath(male, MalePlayerSpriteCatalog.BuildPath("RW", 4, "ST04"));
                AssertNoMissingPath(male, MalePlayerSpriteCatalog.BuildPath("RW", MalePlayerSpriteCatalog.ShortWeaponVariant, "ST04"));

                var female = femaleGo.AddComponent<FemalePlayerVisual>();
                female.spritesRootOverride = root;
                female.logMissingParts = false;
                female.SetWeapon(PcWeaponType.ShortWeapon, 4);
                int femaleRefreshes = female.ActionPartsRefreshCount;

                SandboxManager.ApplyEquipmentVisualChange(female, PlayerEquipSlot.Weapon, 0, 4);

                AssertRefreshDelta(female, femaleRefreshes, 0);
                Assert.AreEqual(PcWeaponType.ShortWeapon, female.currentWeapon);
                AssertHasMissingPath(female, FemalePlayerSpriteCatalog.BuildPath("RW", 4, "ST04"));
                AssertNoMissingPath(female, FemalePlayerSpriteCatalog.BuildPath("RW", FemalePlayerSpriteCatalog.ShortWeaponVariant, "ST04"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(maleGo);
                UnityEngine.Object.DestroyImmediate(femaleGo);
                DeleteRoot(root);
            }
        }

        [Test]
        public void MountedCatalog_PreservesExplicitRiderVariants_AndDefaultsTo019_ForBothSexes()
        {
            AssertMountedRiderVariants(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, PcWeaponType.ShortWeapon),
                "MA_", 19, 19, 19, "HM01");
            AssertMountedRiderVariants(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, PcWeaponType.ShortWeapon),
                "FM_", 19, 19, 19, "HM01");

            AssertMountedRiderVariants(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, PcWeaponType.ShortWeapon,
                    bodyVariant: 31, headVariant: 32, hairVariant: 33),
                "MA_", 31, 32, 33, "HM01");
            AssertMountedRiderVariants(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.RideMagic, PcWeaponType.ShortWeapon,
                    bodyVariant: 31, headVariant: 32, hairVariant: 33),
                "FM_", 31, 32, 33, "HM01");
        }

        [Test]
        public void MaleMountedVisual_MissingExplicitNonStagedVariants_FailsClosedOnExactPaths()
        {
            string root = CreateEmptySpritesRoot();
            var go = new GameObject("MountedFailClosedExactVariants");
            try
            {
                var visual = go.AddComponent<MalePlayerVisual>();
                visual.spritesRootOverride = root;
                visual.logMissingParts = false;
                visual.SetEquipVariant(PlayerEquipSlot.Body, 31);
                visual.SetEquipVariant(PlayerEquipSlot.Head, 32);
                visual.SetEquipVariant(PlayerEquipSlot.Hair, 33);
                visual.SetMounted(true);
                visual.SetAction(PlayerVisualAction.Magic);

                Assert.IsFalse(visual.HasAllRequiredParts);
                AssertHasMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("BD", 31, "HM01"));
                AssertHasMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("HD", 32, "HM01"));
                AssertHasMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("HR", 33, "HM01"));
                AssertNoMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("BD", MalePlayerSpriteCatalog.MountArmorVariant, "HM01"));
                AssertNoMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("HD", MalePlayerSpriteCatalog.MountArmorVariant, "HM01"));
                AssertNoMissingPath(visual, MalePlayerSpriteCatalog.BuildPath("HR", MalePlayerSpriteCatalog.MountArmorVariant, "HM01"));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
                DeleteRoot(root);
            }
        }

        [Test]
        public void Shoulder_UsesMaleTablePath_AndFemaleStaysExplicitlyUnresolved()
        {
            var male = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.HiddenWeapon);
            var female = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Magic, PcWeaponType.HiddenWeapon);
            var maleShoulder = male.Single(p => p.kind == PlayerSpritePartKind.Shoulder);
            var femaleShoulder = female.Single(p => p.kind == PlayerSpritePartKind.Shoulder);
            Assert.IsFalse(maleShoulder.required,
                "MA_SH_019 package winner bytes are absent; path stays provenance-only.");
            StringAssert.Contains("MA_SH_019_MG02", maleShoulder.sourcePath);
            Assert.IsFalse(femaleShoulder.required,
                "Unresolved: canonical female shoulder variant bytes/path are unavailable; do not guess FM_SH_050.");
            Assert.IsEmpty(femaleShoulder.sourcePath);
        }

        [Test]
        public void MountedCast_HorseLimitGatesBeforeAction_AndLogicalClockLocksThenRecovers()
        {
            var go = new GameObject("PcMountedCastPresentationParityTests");
            try
            {
                var player = go.AddComponent<SandboxPlayerController>();
                player.allowKeyboardFallback = false;
                player.PlayPcSkillAction(11, 99f, horseLimit: 2);
                Assert.IsFalse(player.IsSkillActionLocked, "HorseLimit=2 must reject off-horse before visual action.");

                player.Mount.Mount(1);
                player.Mount.Tick(1f);
                player.PlayPcSkillAction(11, 99f, horseLimit: 1);
                Assert.IsFalse(player.IsSkillActionLocked, "HorseLimit=1 must reject before visual action.");

                int emitted = 0;
                player.OnPcSkillActionEffect += () => emitted++;
                player.PlayPcSkillAction(11, 99f, horseLimit: 0);
                Assert.IsTrue(player.IsSkillActionLocked);
                Assert.AreEqual(20, player.ForcedActionTotalTicks);
                Assert.AreEqual(20f / 18f, player.ForcedActionDuration, 0.0001f, "20 PC ticks run at GAME_FPS=18; WaitTime is ignored.");
                Assert.AreEqual(12f / 18f, player.ForcedActionEffectTime, 0.0001f);
                player.SetMoveInput(Vector2.right);
                player.SimulateMove(11f / 18f);
                Assert.AreEqual(0, emitted);
                Assert.AreEqual(11f / 20f, player.ForcedActionProgress, 0.0001f);
                Assert.AreEqual(Vector2.zero, player.LastMoveDelta);
                player.SimulateMove(1f / 18f);
                Assert.AreEqual(12f / 20f, player.ForcedActionProgress, 0.0001f);
                Assert.AreEqual(1, emitted, "Effect emits at floor(20 * 60 / 100) = tick 12.");
                player.SimulateMove(8f / 18f);
                Assert.IsFalse(player.IsSkillActionLocked, "Recovery occurs at tick 20.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        [TestCase(0f, 20)]
        [TestCase(0.01f, 20)]
        [TestCase(0.99f, 20)]
        [TestCase(1f, 18)]
        [TestCase(25f, 16)]
        [TestCase(33f, 14)]
        [TestCase(100f, 10)]
        [TestCase(1000f, 1)]
        [TestCase(-0.01f, 20)]
        [TestCase(-0.99f, 20)]
        [TestCase(-1f, 20)]
        [TestCase(-9.99f, 20)]
        [TestCase(-10f, 22)]
        public void PcActionClock_TruncatesIntegerSpeed_FloorsToEvenTicks_AndKeepsMinimumOne(float speedPercent, int expectedTicks)
        {
            Assert.AreEqual(expectedTicks, SandboxPlayerController.ResolvePcActionTicks(speedPercent));
        }

        private static void AssertFoot(PlayerSpritePartSpec[] parts, string suffix)
        {
            Assert.That(parts.Where(p => p.kind is PlayerSpritePartKind.LeftHand or PlayerSpritePartKind.RightHand or PlayerSpritePartKind.LeftWeapon or PlayerSpritePartKind.RightWeapon), Is.Not.Empty);
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains(suffix)), $"Expected {suffix}.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon && p.required));
        }

        private static void AssertCanonicalFootVariants(PcWeaponType weapon, int[] primaryVariants,
            int[] alternateVariants, string primaryAttack, string primaryAttack1)
        {
            foreach (int variant in primaryVariants)
            {
                Assert.AreEqual(PcWeaponMotionProfile.PrimaryPhysicalOrder,
                    MalePlayerSpriteCatalog.ResolveMotionProfile(weapon, variant), $"{weapon}/{variant}");
                AssertVariantBanks(weapon, variant, primaryAttack, primaryAttack1);
            }

            foreach (int variant in alternateVariants)
            {
                Assert.AreEqual(PcWeaponMotionProfile.AlternatePhysicalOrder,
                    MalePlayerSpriteCatalog.ResolveMotionProfile(weapon, variant), $"{weapon}/{variant}");
                AssertVariantBanks(weapon, variant, primaryAttack1, primaryAttack);
            }
        }

        private static void AssertVariantBanks(PcWeaponType weapon, int variant, string attack, string attack1)
        {
            Assert.AreEqual(attack,
                MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack, weapon, variant));
            Assert.AreEqual(attack1,
                MalePlayerSpriteCatalog.ResolveFootActionSuffix(PlayerVisualAction.Attack1, weapon, variant));
            AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon, weaponVariant: variant), attack);
            AssertFoot(MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon, weaponVariant: variant), attack1);
            AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack, weapon, weaponVariant: variant), attack);
            AssertFoot(FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Attack1, weapon, weaponVariant: variant), attack1);
        }

        private static void AssertMounted(PlayerSpritePartSpec[] parts, string suffix, string riderPrefix)
        {
            var required = new[]
            {
                PlayerSpritePartKind.Body, PlayerSpritePartKind.Head, PlayerSpritePartKind.Hair,
                PlayerSpritePartKind.LeftHand, PlayerSpritePartKind.RightHand, PlayerSpritePartKind.RightWeapon,
                PlayerSpritePartKind.HorseFront, PlayerSpritePartKind.HorseMiddle, PlayerSpritePartKind.HorseRear,
            };
            Assert.IsTrue(required.All(kind => parts.Any(p => p.kind == kind && p.required)), "Missing canonical mounted layer.");
            var leftWeapon = parts.Single(p => p.kind == PlayerSpritePartKind.LeftWeapon);
            Assert.IsTrue(!leftWeapon.required || !string.IsNullOrEmpty(leftWeapon.sourcePath),
                "Optional female LongWeapon left layer must stay explicit; required left layers need a path.");
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.Contains(suffix)), $"Expected {suffix}.");
            Assert.IsTrue(parts.Where(p => p.kind is PlayerSpritePartKind.Body or PlayerSpritePartKind.Head or PlayerSpritePartKind.Hair or PlayerSpritePartKind.LeftHand or PlayerSpritePartKind.RightHand)
                .All(p => p.sourcePath.Contains(riderPrefix)));
        }

        private static void AssertMountedRiderVariants(PlayerSpritePartSpec[] parts, string riderPrefix,
            int bodyVariant, int headVariant, int hairVariant, string suffix)
        {
            AssertPartPath(parts, PlayerSpritePartKind.Body, $"{riderPrefix}BD_{bodyVariant:D3}_{suffix}");
            AssertPartPath(parts, PlayerSpritePartKind.Head, $"{riderPrefix}HD_{headVariant:D3}_{suffix}");
            AssertPartPath(parts, PlayerSpritePartKind.Hair, $"{riderPrefix}HR_{hairVariant:D3}_{suffix}");
        }

        private static void AssertPartPath(PlayerSpritePartSpec[] parts, PlayerSpritePartKind kind, string expected)
        {
            var part = parts.Single(p => p.kind == kind);
            StringAssert.Contains(expected, part.sourcePath);
            Assert.IsTrue(part.required, expected);
        }

        private static void AssertRefreshDelta(IPlayerVisual visual, int before, int expected)
        {
            Assert.AreEqual(expected, visual.ActionPartsRefreshCount - before,
                $"Expected {expected} action-parts refresh/decode pass(es).");
        }

        private static void AssertHasMissingPath(IPlayerVisual visual, string sourcePath)
        {
            Assert.IsTrue(visual.LastMissingRequiredParts.Contains(sourcePath),
                $"Expected missing exact path {sourcePath}. Actual: {string.Join("\n", visual.LastMissingRequiredParts)}");
        }

        private static void AssertNoMissingPath(IPlayerVisual visual, string sourcePath)
        {
            Assert.IsFalse(visual.LastMissingRequiredParts.Contains(sourcePath),
                $"Unexpected fallback path {sourcePath}. Actual: {string.Join("\n", visual.LastMissingRequiredParts)}");
        }

        private static string CreateEmptySpritesRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "PcMountedCastPresentationParityTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(root);
            return root;
        }

        private static void DeleteRoot(string root)
        {
            if (!string.IsNullOrEmpty(root) && Directory.Exists(root))
                Directory.Delete(root, true);
        }
    }
}
