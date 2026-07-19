using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("TangMen")]
    public sealed class TangMenVisualResourceParityTests
    {
        private const string DaoTangParent = @"\spr\skill\1502\tm\tm_150_daotang_zd.spr";
        private const string DaoTangChild = @"\spr\skill\1502\tm\tm_150_daotang_bz.spr";
        private const string SanHuaParent = @"\spr\skill\150\tm\tm_150_sanhuatiannv_c_b.spr";
        private const string SanHuaChild = @"\spr\skill\150\tm\tm_150_sanhuatiannv_c_a.spr";

        private static string Root => Directory.GetCurrentDirectory();
        private static string SpritesRuntimeRoot => Path.Combine(Root, "SpritesRuntime");
        private static string PcMissiles => Path.Combine(Root, "Assets", "StreamingAssets", "Reference", "PcAttrib", "missles1.txt");
        private static string PcSkills => Path.Combine(Root, "Assets", "StreamingAssets", "Reference", "PcSkill", "skills.txt");

        private static string Sha256(string file)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file))).Replace("-", "").ToLowerInvariant();
        }

        private static void AssertVisual(PcMissileFullVisualRegistry registry, int missileId,
            string pcPath, string uid, string sha256, int frames, int directions, int interval)
        {
            var visual = registry.Get(missileId);
            Assert.IsNotNull(visual, $"PC missile {missileId} missing");
            Assert.AreEqual(pcPath, visual.PrimaryFlightSpr, $"PC missile {missileId} flight path");
            Assert.AreEqual(frames, visual.PrimaryFlight.totalFrames, $"PC missile {missileId} frames");
            Assert.AreEqual(directions, visual.PrimaryFlight.directions, $"PC missile {missileId} directions");
            Assert.AreEqual(interval, visual.PrimaryFlight.intervalTicks, $"PC missile {missileId} interval");
            Assert.AreEqual(uid, SprRuntimeService.ComputePathUidHex(pcPath, "GB2312", signedBytes: true),
                $"PC missile {missileId} signed GB2312 UID");
            Assert.AreEqual(sha256, Sha256(Path.Combine(SpritesRuntimeRoot, uid + ".spr")),
                $"PC missile {missileId} vendored bytes");
        }

        [Test]
        public void LifecycleSkills_ResolveCanonicalParentAndChildMissiles()
        {
            // Canonical PcSkill/skills.txt chain: root parent missile -> lifecycle child -> visual missile.
            var rows = File.ReadAllLines(PcSkills, Encoding.GetEncoding("ISO-8859-1"));
            var header = rows[0].Split('\t');
            int id = Array.IndexOf(header, "SkillId");
            int child = Array.IndexOf(header, "ChildSkillId");
            int collide = Array.IndexOf(header, "CollidSkillId");
            int fly = Array.IndexOf(header, "FlySkillId");
            int vanish = Array.IndexOf(header, "VanishedSkillId");
            Assert.GreaterOrEqual(id, 0); Assert.GreaterOrEqual(child, 0);
            Assert.GreaterOrEqual(collide, 0); Assert.GreaterOrEqual(fly, 0); Assert.GreaterOrEqual(vanish, 0);

            string[] Row(int skillId) => Array.Find(rows, r => r.Split('\t')[id] == skillId.ToString())?.Split('\t');
            var s1069 = Row(1069); var s1070 = Row(1070); var s1097 = Row(1097);
            var s1098 = Row(1098); var s1110 = Row(1110); var s1113 = Row(1113); var s352 = Row(352);
            Assert.AreEqual("331", s1069[child]); Assert.AreEqual("1097", s1069[collide]);
            Assert.AreEqual("332", s1070[child]); Assert.AreEqual("1098", s1070[fly]);
            Assert.AreEqual("359", s1097[child]); Assert.AreEqual("360", s1098[child]);
            Assert.AreEqual("374", s1110[child]); Assert.AreEqual("1113", s1110[vanish]);
            Assert.AreEqual("161", s1113[child]); Assert.AreEqual("352", s1113[collide]);
            Assert.AreEqual("162", s352[child]);
        }

        [Test]
        public void RelationshipTargets_MaterializeNestedPcLinksAndMapTheirOwnMissiles()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCoreSectCatalog(
                includeWuDang: false, includeShaolin: false, includeTangMen: true,
                includeEMei: false, includeTianWang: false, includeWuDu: false,
                includeCuiYan: false, includeTianRen: false, includeKunLun: false);
            var root = catalog.Resolve(1110);
            var vanished = catalog.Resolve(root.vanishSkillId);
            var collided = catalog.Resolve(vanished.collideSkillId);
            Assert.AreEqual(374, root.childSkillId, "root flight stays its direct PC child");
            Assert.AreEqual(1113, root.vanishSkillId);
            Assert.AreEqual(161, vanished.childSkillId, "vanish event owns its child missile");
            Assert.AreEqual(352, vanished.collideSkillId, "vanish event owns separate collide event");
            Assert.AreEqual(162, collided.childSkillId, "collide event owns its child missile");

            var mapper = new PcSkillVisualAutoMapper();
            mapper.Initialize(Path.Combine(Root, "Assets", "StreamingAssets"));
            var registry = PcMissileFullVisualRegistry.ParseFromFile(PcMissiles);
            var rootVisual = mapper.GetVisualConfig(root);
            var vanishedVisual = mapper.GetVisualConfig(vanished);
            var collidedVisual = mapper.GetVisualConfig(collided);
            Assert.AreEqual(374, rootVisual.missileId, "event links never replace current flight");
            Assert.AreEqual(161, vanishedVisual.missileId);
            Assert.AreEqual(registry.Get(161).PrimaryFlightSpr, vanishedVisual.flightSprPath);
            Assert.AreEqual(162, collidedVisual.missileId);
            Assert.AreEqual(registry.Get(162).PrimaryFlightSpr, collidedVisual.flightSprPath);
        }

        [Test]
        public void TangMenLifecycleSprs_HavePinnedUidBytesAndPcMetadata()
        {
            var registry = PcMissileFullVisualRegistry.ParseFromFile(PcMissiles);
            AssertVisual(registry, 331, DaoTangParent, "d1f0327d",
                "7f9298e68e80b0ff210361c03f6b3c1e1dd4e2507f8287b5133017d10b121988", 16, 16, 1);
            AssertVisual(registry, 359, DaoTangChild, "da0d555d",
                "1bf54340a9eb390e728a5203122775e46f39ba77ed18ee638f448eefdf460b7d", 19, 1, 1);
            AssertVisual(registry, 332, SanHuaParent, "53144a68",
                "ca53c1c08935e20101b3e0d40e34ca9071514f53b07ad168a4180b28c75680ef", 10, 1, 1);
            AssertVisual(registry, 360, SanHuaChild, "56ac3571",
                "1d60bfa9a4717dad6c2dbb9b243333bd63ce4b04cae27c14c2c5ad2d744e24a0", 16, 16, 1);
        }

        [Test]
        public void TangMenLifecycleSprs_PlayEveryPcMetadataFrame()
        {
            AssertPlayback("d1f0327d", SprPlaybackMode.Missile, 16, 16, null);
            AssertPlayback("da0d555d", SprPlaybackMode.Stationary, 19, 1, new HashSet<int> { 0 });
            AssertPlayback("53144a68", SprPlaybackMode.Stationary, 10, 1, null);
            AssertPlayback("56ac3571", SprPlaybackMode.Missile, 16, 16, null);

            var daoTangChild = Decode("da0d555d");
            Assert.AreEqual(20, daoTangChild.frames.Length, "SPR header has one frame beyond PC metadata play count");
            Assert.IsTrue(SprFramePlayback.IsCanonicalEmpty(daoTangChild.frames[0]), "f0 is canonical 1x1 empty");
            for (int i = 1; i < 19; i++)
                Assert.IsTrue(SprFramePlayback.IsPlayable(daoTangChild.frames[i]), $"used f{i} must be visible/playable");
            Assert.AreEqual(18, SprFramePlayback.UsedFrameIndices(SprPlaybackMode.Stationary, 19, 1)[18]);
        }

        private static void AssertPlayback(string uid, SprPlaybackMode mode, int totalFrames, int directions,
            ISet<int> canonicalEmptyFrames)
        {
            var decoded = Decode(uid);
            Assert.IsTrue(SprFramePlayback.TryValidateUsedFrames(decoded.frames, mode, totalFrames, directions,
                canonicalEmptyFrames, out var error), $"{uid}: {error}");
        }

        private static SprDecodeResult Decode(string uid)
        {
            var decoded = SprDecoder.Decode(File.ReadAllBytes(Path.Combine(SpritesRuntimeRoot, uid + ".spr")));
            Assert.IsTrue(decoded.success, $"{uid}: {decoded.error}");
            return decoded;
        }

        [Test, Category("Slow")]
        public void TangMenLifecycleSprs_DecodeThroughRuntimeService()
        {
            var service = new SprRuntimeService(SpritesRuntimeRoot);
            foreach (var path in new[] { DaoTangParent, DaoTangChild, SanHuaParent, SanHuaChild })
            {
                var sprite = service.ResolveSprite(path, 64, 64);
                Assert.IsNotNull(sprite, $"SprRuntimeService failed: {path}");
                Assert.IsNotNull(sprite.texture, $"decoded texture missing: {path}");
                Assert.Greater(sprite.texture.width, 0, $"decoded width: {path}");
                Assert.Greater(sprite.texture.height, 0, $"decoded height: {path}");
            }
        }
    }
}
