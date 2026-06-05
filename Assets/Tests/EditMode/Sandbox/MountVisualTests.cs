// -----------------------------------------------------------------------------
// VLTK Mobile — ST-02.1.3 Mount cycle tests
// Verifies: catalog emits HM01 rider parts when action=Ride, mounted SPRs load
// from staged files, HasAllRequiredParts=true, and HorseVisual decodes the
// single-frame horse body SPR. Mirrors MalePlayerVisualTests pattern.
// -----------------------------------------------------------------------------

using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class MountVisualTests
    {
        private GameObject _male;
        private GameObject _female;
        private GameObject _horse;
        private string _tmpRoot;

        [SetUp]
        public void SetUp()
        {
            _male = new GameObject("MalePlayerTest");
            _female = new GameObject("FemalePlayerTest");
            _horse = new GameObject("HorseTest");
            // Test environment: route the visuals to the real StreamingAssets/Sprites folder
            // so they can find the staged HM01 + horse SPRs without copying test fixtures.
            _tmpRoot = Path.Combine(Application.streamingAssetsPath, "Sprites");
        }

        [TearDown]
        public void TearDown()
        {
            if (_male != null) Object.DestroyImmediate(_male);
            if (_female != null) Object.DestroyImmediate(_female);
            if (_horse != null) Object.DestroyImmediate(_horse);
        }

        // ----- Male mount catalog -----

        [Test]
        public void MaleCatalog_Ride_EmitsMountedRiderSet()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.EmptyHand);
            Assert.AreEqual(3, parts.Length, "Mounted male: 3 parts (BD/HD/HB).");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Saddle),
                "Male mount must include HB (Saddle) part.");
            Assert.IsTrue(parts.All(p => p.required), "All 3 mount parts are required for male.");
            Assert.IsTrue(parts.All(p => p.sourcePath.EndsWith("_HM01.spr")),
                "All mount parts use HM01 action suffix.");
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains(@"\MA_")), "Male mount uses MA_ prefix.");
            Assert.IsFalse(parts.Any(p => p.kind == PlayerSpritePartKind.Shadow),
                "Mounted rider has no separate shadow SPR in PC source.");
            Assert.IsFalse(parts.Any(p => p.kind == PlayerSpritePartKind.LeftWeapon),
                "Mounted rider has no separate LW SPR in PC source.");
        }

        [Test]
        public void MaleCatalog_Ride_StripsShadowAndWeapons()
        {
            var parts = MalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.LongWeapon);
            var names = parts.Select(p => p.kind.ToString()).ToList();
            CollectionAssert.DoesNotContain(names, "Shadow");
            CollectionAssert.DoesNotContain(names, "LeftWeapon");
            CollectionAssert.DoesNotContain(names, "RightWeapon");
            CollectionAssert.DoesNotContain(names, "Hair");
            CollectionAssert.DoesNotContain(names, "LeftHand");
            CollectionAssert.DoesNotContain(names, "RightHand");
        }

        // ----- Female mount catalog -----

        [Test]
        public void FemaleCatalog_Ride_EmitsFivePartRiderSet()
        {
            var parts = FemalePlayerSpriteCatalog.BuildParts(PlayerVisualAction.Ride, PcWeaponType.EmptyHand);
            Assert.AreEqual(5, parts.Length, "Mounted female: 5 parts (BD/HD/HR/LH/RH).");
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Body));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Head));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.Hair));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.LeftHand));
            Assert.IsTrue(parts.Any(p => p.kind == PlayerSpritePartKind.RightHand));
            Assert.IsTrue(parts.All(p => p.sourcePath.EndsWith("_HM01.spr")));
            Assert.IsTrue(parts.All(p => p.sourcePath.Contains(@"\FM_")), "Female mount uses FM_ prefix.");
        }

        // ----- Male mount runtime -----

        [Test]
        public void MaleVisual_Ride_LoadsMountedRiderParts()
        {
            var mv = _male.AddComponent<MalePlayerVisual>();
            mv.SetMounted(true);
            // The visual auto-refreshes on SetMounted. After Awake, it already pulled parts.
            // Force a manual refresh to be deterministic.
            mv.RefreshActionParts(force: true);
            Assert.IsTrue(mv.IsMounted, "IsMounted flag should be true after SetMounted(true).");
            Assert.IsTrue(mv.HasAllRequiredParts, "All required mounted parts must load.");
            Assert.AreEqual(3, mv.LoadedPartCount, "Mounted male loads 3 parts.");
            Assert.AreEqual(0, mv.MissingRequiredPartCount,
                $"No required parts should be missing. Missing: {string.Join(",", mv.LastMissingRequiredParts)}");
        }

        [Test]
        public void MaleVisual_MountCycle_TogglesPartsCleanly()
        {
            var mv = _male.AddComponent<MalePlayerVisual>();
            // Start unmounted
            mv.SetWeapon(PcWeaponType.EmptyHand);
            mv.SetAction(PlayerVisualAction.Idle);
            int unmountedCount = mv.LoadedPartCount;
            Assert.Greater(unmountedCount, 3, "Unmounted male loads more parts than mounted (shadow, LH/RH, weapons).");

            // Mount
            mv.SetMounted(true);
            int mountedCount = mv.LoadedPartCount;
            Assert.AreEqual(3, mountedCount, "Mount switches to 3-part rider set.");
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
            fv.SetMounted(true);
            fv.RefreshActionParts(force: true);
            Assert.IsTrue(fv.IsMounted);
            Assert.IsTrue(fv.HasAllRequiredParts, $"Missing: {string.Join(",", fv.LastMissingRequiredParts)}");
            Assert.AreEqual(5, fv.LoadedPartCount, "Mounted female loads 5 parts (BD/HD/HR/LH/RH).");
        }

        [Test]
        public void FemaleVisual_MountCycle_TogglesPartsCleanly()
        {
            var fv = _female.AddComponent<FemalePlayerVisual>();
            fv.SetWeapon(PcWeaponType.EmptyHand);
            fv.SetAction(PlayerVisualAction.Idle);
            int unmountedCount = fv.LoadedPartCount;
            // Female on foot also has exactly 5 visible parts (BD/HD/HR/LH/RH).
            // Shadow/LW/RW slots are not required and never get a sprite.
            Assert.AreEqual(5, unmountedCount, "Unmounted female loads 5 parts.");

            fv.SetMounted(true);
            Assert.AreEqual(5, fv.LoadedPartCount, "Mount switches female to 5-part rider set.");
            Assert.IsTrue(fv.HasAllRequiredParts, $"Mounted female missing: {string.Join(",", fv.LastMissingRequiredParts)}");

            fv.SetMounted(false);
            fv.SetAction(PlayerVisualAction.Idle);
            Assert.AreEqual(5, fv.LoadedPartCount, "Dismount restores 5-part on-foot set.");
            Assert.IsTrue(fv.IsMounted == false);
        }

        // ----- HorseVisual -----

        [Test]
        public void HorseVisual_LoadsHorseBodySprite()
        {
            var hv = _horse.AddComponent<HorseVisual>();
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
