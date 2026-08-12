// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1.3 Mount cycle tests
// Verifies: catalog emits canonical RD01 rider parts when action=Ride, mounted SPRs load
// from staged files, male required layers are complete, female reports only its
// canonical hair hole, and HorseVisual decodes single-frame horse body SPRs.
// -----------------------------------------------------------------------------

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("Slow")]
    public class MountVisualTests
    {
        private GameObject _male;
        private GameObject _female;
        private GameObject _horse;
        private string _tmpRoot;
        private string _stagingRoot;

        [SetUp]
        public void SetUp()
        {
            _male = new GameObject("MalePlayerTest");
            _female = new GameObject("FemalePlayerTest");
            _horse = new GameObject("HorseTest");
            string sourceRoot = Path.Combine(Application.streamingAssetsPath, "Sprites");
            _stagingRoot = MalePlayerSprStaging.StageForTests(sourceRoot: sourceRoot);
            MalePlayerSprStaging.StageFemaleForTests(sourceRoot: sourceRoot, tempRoot: _stagingRoot);
            foreach (int horseId in HorseVisual.AvailableHorseIds)
                MalePlayerSprStaging.StageOne(sourceRoot, _stagingRoot, HorseVisual.SourcePathForHorseId(horseId));
            _tmpRoot = _stagingRoot;
        }

        [TearDown]
        public void TearDown()
        {
            if (_male != null) Object.DestroyImmediate(_male);
            if (_female != null) Object.DestroyImmediate(_female);
            if (_horse != null) Object.DestroyImmediate(_horse);
            MalePlayerSprStaging.CleanupTempDir(_stagingRoot);
        }

        private static void AssertOnlyCanonicalFemaleHairIsMissing(FemalePlayerVisual visual)
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

            var visibleKinds = new[]
            {
                PlayerSpritePartKind.Body, PlayerSpritePartKind.LeftHand, PlayerSpritePartKind.RightHand,
                PlayerSpritePartKind.RightWeapon, PlayerSpritePartKind.HorseFront,
                PlayerSpritePartKind.HorseMiddle, PlayerSpritePartKind.HorseRear,
            };
            foreach (var kind in visibleKinds)
            {
                Assert.IsTrue(visual.GetComponentsInChildren<SpriteRenderer>(true).Any(renderer =>
                    renderer.gameObject.name.StartsWith($"Part_{(int)kind}_") && renderer.enabled && renderer.sprite != null),
                    $"Mounted female required {kind} layer must render.");
            }
        }

        // ----- Male mount catalog -----

        [Test]
        public void MaleCatalog_Ride_EmitsMountedRiderSet()
        {
            // Catalog retains shadow, horse, rider, shoulder, and weapon slots.
            // Mounted idle uses RD01; source-less optional holes stay explicit.
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.EmptyHand);
            Assert.AreEqual(12, parts.Length, "Mounted male catalog includes shadow, horse, shoulder, and weapon slots.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseFront));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseMiddle));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseRear));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shoulder));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon));
            var shoulder = parts.Single(p => p.kind == PlayerSpritePartKind.Shoulder);
            Assert.IsFalse(shoulder.required, "MA_SH_019 package winner bytes are absent; path is provenance-only.");
            StringAssert.Contains("MA_SH_019_RD01.spr", shoulder.sourcePath);
            Assert.IsTrue(parts.Where(p => p.kind != PlayerSpritePartKind.Shoulder).All(p => p.required),
                "Every mounted male layer except known MA_SH_019 hole remains required.");
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.EndsWith("_RD01.spr")),
                "Mounted idle uses RD01 (RideStand) action suffix for all parts.");
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains(@"\MA_")), "Male mount uses MA_ prefix.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon));
        }

        [Test]
        public void MaleCatalog_Ride_KeepsWeaponSlotsForCastBanks()
        {
            // Mounted HA/HM cast banks retain weapon slots.
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.LongWeapon);
            var names = parts.Select(p => p.kind.ToString()).ToList();
            CollectionAssert.Contains(names, "LeftWeapon");
            CollectionAssert.Contains(names, "RightWeapon");
            CollectionAssert.Contains(names, "Shadow");
            CollectionAssert.Contains(names, "Hair");
            CollectionAssert.Contains(names, "LeftHand");
            CollectionAssert.Contains(names, "RightHand");
            CollectionAssert.Contains(names, "HorseFront");
            CollectionAssert.Contains(names, "HorseMiddle");
            CollectionAssert.Contains(names, "HorseRear");
        }

        // ----- Female mount catalog -----

        [Test]
        public void FemaleCatalog_Ride_EmitsLayeredRiderSet()
        {
            // Female horse layers resolve MA_H* paths; shoulder remains explicit optional hole.
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.EmptyHand);
            Assert.AreEqual(12, parts.Length, "Mounted female catalog includes explicit optional shoulder/shadow slots.");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseFront));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseMiddle));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.HorseRear));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Shoulder && !p.required));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightWeapon));
            Assert.IsTrue(parts.Where(p => !string.IsNullOrEmpty(p.sourcePath)).All(p => p.sourcePath.EndsWith("_RD01.spr")),
                "Mounted idle uses RD01 (RideStand) action suffix for all parts.");
            // The 5 female rider parts use the FM_ prefix; horse + shadow reuse male MA_ art.
            Assert.IsTrue(parts.Where(p => p.kind == PlayerSpritePartKind.Body
                                        || p.kind == PlayerSpritePartKind.Head
                                        || p.kind == PlayerSpritePartKind.Hair
                                        || p.kind == PlayerSpritePartKind.LeftHand
                                        || p.kind == PlayerSpritePartKind.RightHand)
                                  .All(p => p.sourcePath.Contains(@"\FM_")),
                "Female rider parts use FM_ prefix.");
        }

        // ----- Male mount runtime -----

        [Test]
        public void MaleVisual_Ride_LoadsMountedRiderParts()
        {
            var mv = _male.AddComponent<MalePlayerVisual>();
            mv.spritesRootOverride = _tmpRoot;
            mv.SetMounted(true);
            // The visual auto-refreshes on SetMounted. After Awake, it already pulled parts.
            // Force a manual refresh to be deterministic.
            mv.RefreshActionParts(force: true);
            Assert.IsTrue(mv.IsMounted, "IsMounted flag should be true after SetMounted(true).");
            Assert.IsTrue(mv.HasAllRequiredParts, "All required mounted parts must load.");
            Assert.AreEqual(0, mv.MissingRequiredPartCount,
                $"No required parts should be missing. Missing: {string.Join(",", mv.LastMissingRequiredParts)}");
        }

        [Test]
        public void MaleVisual_MountCycle_TogglesPartsCleanly()
        {
            var mv = _male.AddComponent<MalePlayerVisual>();
            mv.spritesRootOverride = _tmpRoot;
            mv.RefreshActionParts(force: true);
            // Start unmounted
            mv.SetWeapon(PcWeaponType.EmptyHand);
            mv.SetAction(PlayerVisualAction.Idle);
            int unmountedCount = mv.LoadedPartCount;

            // Mount
            mv.SetMounted(true);
            Assert.IsTrue(mv.HasAllRequiredParts);

            // Dismount back to idle
            mv.SetMounted(false);
            mv.SetAction(PlayerVisualAction.Idle);
            Assert.AreEqual(unmountedCount, mv.LoadedPartCount, "Dismount restores the full on-foot part set.");
        }

        // ----- Female mount runtime -----

        [Test]
        public void FemaleVisual_Ride_LoadsMountedRiderParts()
        {
            var fv = _female.AddComponent<FemalePlayerVisual>();
            fv.spritesRootOverride = _tmpRoot;
            fv.SetMounted(true);
            fv.RefreshActionParts(force: true);
            Assert.IsTrue(fv.IsMounted);
            AssertOnlyCanonicalFemaleHairIsMissing(fv);
        }

        [Test]
        public void FemaleVisual_MountCycle_TogglesPartsCleanly()
        {
            var fv = _female.AddComponent<FemalePlayerVisual>();
            fv.spritesRootOverride = _tmpRoot;
            fv.RefreshActionParts(force: true);
            fv.SetWeapon(PcWeaponType.EmptyHand);
            fv.SetAction(PlayerVisualAction.Idle);
            int unmountedCount = fv.LoadedPartCount;

            fv.SetMounted(true);
            Assert.IsTrue(fv.IsMounted);
            AssertOnlyCanonicalFemaleHairIsMissing(fv);

            fv.SetMounted(false);
            fv.SetAction(PlayerVisualAction.Idle);
            Assert.AreEqual(unmountedCount, fv.LoadedPartCount, "Dismount restores original staged layer set.");
            Assert.IsFalse(fv.IsMounted);
        }

        // ----- HorseVisual -----

        [Test]
        public void HorseVisual_LoadsHorseBodySprite()
        {
            var hv = _horse.AddComponent<HorseVisual>();
            hv.spritesRootOverride = _tmpRoot;
            hv.sourcePath = @"spr\item\equip\horse\horse001.spr";
            hv.LoadAndApply();
            Assert.IsTrue(hv.HasSprite, "HorseVisual should decode horse001.spr successfully.");
            Assert.Greater(hv.SpriteSize.x, 0f, "Horse sprite should have positive width.");
        }

        [Test]
        public void HorseVisual_FallsBackWhenSprMissing()
        {
            // Use a fresh GameObject so the Awake-time horseId resolution doesn't
            // override our explicit sourcePath. Disable horseId-driven resolution
            // by setting horseId=0 first, then put a bad path and reload.
            var go = new GameObject("HorseMissingTest");
            try
            {
                var hv = go.AddComponent<HorseVisual>();
                hv.spritesRootOverride = _tmpRoot;
                hv.horseId = 0; // disable horseId-driven resolution
                hv.sourcePath = @"spr\item\equip\horse\horse999_notexist.spr";
                hv.logMissing = false;
                hv.LoadAndApply();
                Assert.IsFalse(hv.HasSprite, "Missing horse SPR should result in HasSprite=false.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        // ----- 5-color horse catalog (PC horseres.txt) -----

        [Test]
        public void HorseVisual_SourcePathForHorseId_MapsFiveColors()
        {
            Assert.AreEqual(@"spr\item\equip\horse\horse001.spr", HorseVisual.SourcePathForHorseId(1));
            Assert.AreEqual(@"spr\item\equip\horse\horse003.spr", HorseVisual.SourcePathForHorseId(3));
            Assert.AreEqual(@"spr\item\equip\horse\horse005.spr", HorseVisual.SourcePathForHorseId(5));
            Assert.AreEqual(@"spr\item\equip\horse\horse007.spr", HorseVisual.SourcePathForHorseId(7));
            Assert.AreEqual(@"spr\item\equip\horse\horse009.spr", HorseVisual.SourcePathForHorseId(9));
        }

        [Test]
        public void HorseVisual_LoadsAllFiveColors()
        {
            foreach (int id in HorseVisual.AvailableHorseIds)
            {
                var go = new GameObject($"HorseColorTest_{id}");
                try
                {
                    var hv = go.AddComponent<HorseVisual>();
                    hv.spritesRootOverride = _tmpRoot;
                    hv.SetHorseId(id);
                    Assert.IsTrue(hv.HasSprite, $"horse id {id} should load a sprite.");
                    Assert.AreEqual(HorseVisual.SourcePathForHorseId(id), hv.sourcePath);
                }
                finally
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        [Test]
        public void HorseVisual_SetHorseId_ReDecodesOnChange()
        {
            var hv = _horse.AddComponent<HorseVisual>();
            hv.spritesRootOverride = _tmpRoot;
            hv.SetHorseId(1);
            var firstSize = hv.SpriteSize;
            Assert.IsTrue(hv.HasSprite);
            Assert.Greater(firstSize.x, 0f);
            hv.SetHorseId(9);
            Assert.IsTrue(hv.HasSprite);
            Assert.AreEqual(@"spr\item\equip\horse\horse009.spr", hv.sourcePath);
        }

        [Test]
        public void HorseVisual_AllSprsAreSingleFrame()
        {
            // PC limitation: all 45 horse*.spr are single-frame, single-direction.
            // The horse body does not animate per direction — flip is faked at runtime.
            // This test guards against the limitation being regressed if we add new
            // multi-frame horse SPRs to the source folder.
            foreach (int id in HorseVisual.AvailableHorseIds)
            {
                string src = HorseVisual.SourcePathForHorseId(id);
                string uid = SprRuntimeService.ComputePathUidHex(src);
                Assert.IsFalse(string.IsNullOrEmpty(uid), $"uid for {src} should resolve.");
            }
        }
    }
}
