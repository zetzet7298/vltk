// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for PcSkillVisualAutoMapper and SpritesRuntime resolution.
// Purpose: Verifies that original PC JX skill paths pass through untouched and
// exist in SpritesRuntime under their signed hash names so the fallback loader
// can resolve them automatically.
// -----------------------------------------------------------------------------
using System;
using System.Reflection;
using NUnit.Framework;

namespace VLTK.Tests.Sandbox
{
    public class PcSkillVisualAutoMapperMissilePathOverrideTests
    {
        private const string OldPhiLongFlight =
            @"\spr\skill\丐帮\mag_gb_05_亢龙有悔.spr";
        private const string OldPhiLongExplode =
            @"\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr";
        private const string OldKhangLongBody =
            @"\spr\skill\gb\龙战 yye.spr"; // note: actual text in file is GBK encoded
        private const string OldThienHaVoCauBody =
            @"\spr\skill\丐帮\mag_gb_04_天下无狗.spr";
        private const string OldThienHaVoCauExplode =
            @"\spr\skill\天忍\mag_bz_huo3_爆炸效果.spr";

        // ResolveMissileSprPath is a private static helper. Use reflection to
        // drive it from the test.
        private static string InvokeResolve(string original)
        {
            var t = typeof(VLTK.Sandbox.PcSkillVisualAutoMapper);
            var m = t.GetMethod("ResolveMissileSprPath",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m,
                "PcSkillVisualAutoMapper.ResolveMissileSprPath missing.");
            return (string)m.Invoke(null, new object[] { original, 0 });
        }

        [Test]
        public void PcSkillVisual_PassesOriginalPathsUntouched()
        {
            Assert.AreEqual(OldPhiLongFlight, InvokeResolve(OldPhiLongFlight));
            Assert.AreEqual(OldPhiLongExplode, InvokeResolve(OldPhiLongExplode));
            Assert.AreEqual(OldThienHaVoCauBody, InvokeResolve(OldThienHaVoCauBody));
            Assert.AreEqual(OldThienHaVoCauExplode, InvokeResolve(OldThienHaVoCauExplode));
        }

        [Test]
        public void UnmappedPath_ReturnsOriginalUnchanged()
        {
            const string unrelated = @"\spr\skill\min\tramcam.spr";
            Assert.AreEqual(unrelated, InvokeResolve(unrelated));
        }

        [Test]
        public void EmptyOrNullPath_ReturnsOriginalUnchanged()
        {
            Assert.IsNull(InvokeResolve(null));
            Assert.AreEqual(string.Empty, InvokeResolve(string.Empty));
        }

        [Test]
        public void OriginalPCFiles_ExistInSpritesRuntimeBySignedHash()
        {
            string runtime = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "SpritesRuntime");
            string UidFor(string pcPath)
            {
                // Use the same signed-byte JX FileNameHash as SprRuntimeService.
                return VLTK.Sprites.SprRuntimeService.ComputePathUidHex(pcPath, signedBytes: true);
            }
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(OldPhiLongFlight) + ".spr")),
                $"Phi Long flight SPR not in SpritesRuntime: uid={UidFor(OldPhiLongFlight)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(OldPhiLongExplode) + ".spr")),
                $"Phi Long impact SPR not in SpritesRuntime: uid={UidFor(OldPhiLongExplode)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(OldThienHaVoCauBody) + ".spr")),
                $"Thiên Hạ Vô Cẩu body SPR not in SpritesRuntime: uid={UidFor(OldThienHaVoCauBody)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(OldThienHaVoCauExplode) + ".spr")),
                $"Thiên Hạ Vô Cẩu impact SPR not in SpritesRuntime: uid={UidFor(OldThienHaVoCauExplode)}");
        }
    }
}
