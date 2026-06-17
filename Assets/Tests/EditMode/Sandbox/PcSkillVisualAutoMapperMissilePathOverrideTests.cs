// -----------------------------------------------------------------------------
// VLTK Mobile — Tests for PcSkillVisualAutoMapper missile path override table.
// Purpose: Lock in the post-migration mapping that replaces the older 2011 PC
// stock SPR paths (which don't exist in any PAK) with the actual Tinh Kiem
// / jx-source PAK paths that DO exist. Regression guard for the Phi Long
// Tại Thiên (357) and friends dragon visual fix.
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
        private const string NewPhiLongFlight =
            @"\spr\skill\150\gb\gb_150_shichengjiulong_a.spr";

        private const string OldPhiLongExplode =
            @"\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr";
        private const string NewPhiLongExplode =
            @"\spr\skill\1502\gb\gb_150_zhanggai_huo.spr";

        private const string OldKhangLongBody =
            @"\spr\skill\gb\龙战于野.spr";
        private const string NewKhangLongBody =
            @"\spr\skill\150\gb\gb_150_shishengliulong_d.spr";

        private const string OldThienHaVoCauBody =
            @"\spr\skill\丐帮\mag_gb_04_天下无狗.spr";
        private const string NewThienHaVoCauBody =
            @"\spr\skill\1502\gb\gb_150_gungai_bz.spr";

        private const string OldThienHaVoCauExplode =
            @"\spr\skill\天忍\mag_bz_huo3_爆炸效果.spr";
        private const string NewThienHaVoCauExplode =
            @"\spr\skill\1502\gb\gb_150_zhanggai_zd.spr";

        // ResolveMissileSprPath is a private static helper. Use reflection to
        // drive it from the test (the override table itself is also private).
        private static string InvokeResolve(string original)
        {
            var t = typeof(VLTK.Sandbox.PcSkillVisualAutoMapper);
            var m = t.GetMethod("ResolveMissileSprPath",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m,
                "PcSkillVisualAutoMapper.ResolveMissileSprPath missing — did the override table compile?");
            return (string)m.Invoke(null, new object[] { original, 0 });
        }

        [Test]
        public void PhiLong_FlightPath_OverridesOldStock2011PathToTinhKiemPakPath()
        {
            Assert.AreEqual(NewPhiLongFlight, InvokeResolve(OldPhiLongFlight),
                "Phi Long Tại Thiên (skill 357 / missile 166) flight path must be remapped to the Tinh Kiem 4-fire-dragon body visual.");
        }

        [Test]
        public void PhiLong_ExplodePath_OverridesOldStock2011PathToTinhKiemPakPath()
        {
            Assert.AreEqual(NewPhiLongExplode, InvokeResolve(OldPhiLongExplode),
                "Phi Long Tại Thiên (skill 357 / missile 166) impact path must be remapped to the Tinh Kiem dragon impact visual.");
        }

        [Test]
        public void KhangLongHuuHoi_BodyPath_OverridesOldStock2011PathToTinhKiemPakPath()
        {
            Assert.AreEqual(NewKhangLongBody, InvokeResolve(OldKhangLongBody),
                "Kháng Long Hữu Hối (skill 358 / missile 167) body path must be remapped.");
        }

        [Test]
        public void ThienHaVoCau_BodyAndImpactPaths_OverrideOldStock2011Paths()
        {
            Assert.AreEqual(NewThienHaVoCauBody, InvokeResolve(OldThienHaVoCauBody),
                "Thiên Hạ Vô Cẩu (skill 359 / missile 168) body path must be remapped.");
            Assert.AreEqual(NewThienHaVoCauExplode, InvokeResolve(OldThienHaVoCauExplode),
                "Thiên Hạ Vô Cẩu (skill 359 / missile 168) impact path must be remapped.");
        }

        [Test]
        public void UnmappedPath_ReturnsOriginalUnchanged()
        {
            const string unrelated = @"\spr\skill\min\tramcam.spr";
            Assert.AreEqual(unrelated, InvokeResolve(unrelated),
                "Paths not in the override table must pass through untouched.");
        }

        [Test]
        public void EmptyOrNullPath_ReturnsOriginalUnchanged()
        {
            Assert.IsNull(InvokeResolve(null));
            Assert.AreEqual(string.Empty, InvokeResolve(string.Empty));
        }

        [Test]
        public void OverrideReplacementFiles_ExistInSpritesRuntime()
        {
            // Verifies that the override targets actually live in SpritesRuntime
            // (so the runtime can resolve them to a real SPR). If this fails,
            // either the override is wrong or the copy step missed a file.
            string runtime = System.IO.Path.Combine(
                System.IO.Directory.GetCurrentDirectory(),
                "SpritesRuntime");
            string UidFor(string pcPath)
            {
                // Use the same signed-byte JX FileNameHash as SprRuntimeService.
                return VLTK.Sprites.SprRuntimeService.ComputePathUidHex(pcPath, signedBytes: true);
            }
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(NewPhiLongFlight) + ".spr")),
                $"Phi Long flight SPR not in SpritesRuntime: uid={UidFor(NewPhiLongFlight)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(NewPhiLongExplode) + ".spr")),
                $"Phi Long impact SPR not in SpritesRuntime: uid={UidFor(NewPhiLongExplode)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(NewKhangLongBody) + ".spr")),
                $"Khang Long body SPR not in SpritesRuntime: uid={UidFor(NewKhangLongBody)}");
            Assert.IsTrue(System.IO.File.Exists(System.IO.Path.Combine(runtime, UidFor(NewThienHaVoCauBody) + ".spr")),
                $"Thiên Hạ Vô Cẩu body SPR not in SpritesRuntime: uid={UidFor(NewThienHaVoCauBody)}");
        }
    }
}
