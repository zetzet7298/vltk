// -----------------------------------------------------------------------------
// VLTK Mobile — Cai Bang Phase 5 visual / SFX resource parity tests.
//
// Purpose: prove the PC-derived visual and audio resources for the Cai Bang
// acceptance skills are physically bound in the mobile runtime roots and resolve
// through the same paths the runtime uses:
//   * SPR  -> SprRuntimeService signed-GB2312 path hash -> SpritesRuntime/{uid}.spr
//   * SFX  -> AudioService PC path -> sound/skill/{name}.wav (StreamingAssets/AudioRuntime)
//   * state aura -> PcSkillVisualAutoMapper.GetStateAuraData (PC 状态与光效图形对照表)
//
// Evidence:
//   openspec/changes/port-caibang-skill-pc-parity/evidence/caibang-visual-sfx-resource-evidence.md
//   openspec/changes/port-caibang-skill-pc-parity/evidence/phi-long-resource-evidence.md
//   openspec/changes/port-caibang-skill-pc-parity/evidence/phi-long-video-evidence.md
// -----------------------------------------------------------------------------

using System.IO;
using NUnit.Framework;
using VLTK.Sandbox;
using VLTK.Sprites;

namespace VLTK.Tests.Sandbox
{
    [TestFixture, Category("CaiBang")]
    public class CaiBangVisualResourceParityTests
    {
        // Proper GBK Chinese PC paths (hash source of truth, not CP1258 mojibake).
        private const string DragonFlightSpr = @"\spr\skill\丐帮\mag_gb_05_亢龙有悔.spr";
        private const string ImpactBurstSpr  = @"\spr\skill\丐帮\mag_gb_bz5_爆炸效果.spr";
        private const string PreCastSpr      = @"\spr\skill\天忍\mag_bz_huo3_爆炸效果.spr";
        private const string KhangLongIcon   = @"\spr\Ui\技能图标\icon_sk_gb_41.spr";

        // Cai Bang state auras from PC 状态与光效图形对照表.txt.
        private const string TuyDiepStateAuraSpr  = @"\spr\skill\丐帮\mag_gb_11_醉蝶狂舞.spr"; // state 43
        private const string DaCauTranStateAuraSpr = @"\spr\skill\丐帮\mag_gb_12_打狗阵.spr";  // state 44

        private static string SpritesRuntimeRoot =>
            Path.Combine(Directory.GetCurrentDirectory(), "SpritesRuntime");

        private static void AssertSprExistsBySignedHash(string pcPath, string label)
        {
            string uid = SprRuntimeService.ComputePathUidHex(pcPath, "GB2312", signedBytes: true);
            Assert.IsNotNull(uid, $"{label}: hash returned null for '{pcPath}'");
            string file = Path.Combine(SpritesRuntimeRoot, uid + ".spr");
            Assert.IsTrue(File.Exists(file), $"{label}: SPR uid={uid} not found in SpritesRuntime ('{pcPath}')");
        }

        [Test]
        public void StateAura_43And44_MatchPcSprPathsAndMetadata()
        {
            // PC 状态与光效图形对照表: state 43 Túy Điệp Cuồng Vũ, state 44 Đả Cẩu Trận.
            var a43 = PcSkillVisualAutoMapper.GetStateAuraData(43);
            Assert.AreEqual(TuyDiepStateAuraSpr, a43.sprPath, "state 43 aura SPR path mismatch vs PC source");
            Assert.AreEqual(3, a43.position, "state 43 aura position (3=body)");
            Assert.AreEqual(4, a43.frameStart, "state 43 aura frameStart");
            Assert.AreEqual(12, a43.frameEnd, "state 43 aura frameEnd");
            Assert.AreEqual(16, a43.totalFrames, "state 43 aura totalFrames");
            Assert.AreEqual(1, a43.directions, "state 43 aura directions");

            var a44 = PcSkillVisualAutoMapper.GetStateAuraData(44);
            Assert.AreEqual(DaCauTranStateAuraSpr, a44.sprPath, "state 44 aura SPR path mismatch vs PC source");
            Assert.AreEqual(2, a44.position, "state 44 aura position (2=feet)");
            Assert.AreEqual(8, a44.totalFrames, "state 44 aura totalFrames");
            Assert.AreEqual(1, a44.directions, "state 44 aura directions");
        }

        [Test]
        public void SignedHash_ReproducesKnownCaiBangMissileUids()
        {
            // Locks the runtime hash to the proper GBK paths so the resolver keeps
            // finding the dragon/impact SPRs regardless of CP1258 mojibake display.
            Assert.AreEqual("a31b9f04", SprRuntimeService.ComputePathUidHex(DragonFlightSpr, "GB2312", signedBytes: true));
            Assert.AreEqual("c33e96c2", SprRuntimeService.ComputePathUidHex(ImpactBurstSpr, "GB2312", signedBytes: true));
            Assert.AreEqual("7d34af1d", SprRuntimeService.ComputePathUidHex(TuyDiepStateAuraSpr, "GB2312", signedBytes: true));
            Assert.AreEqual("202667bb", SprRuntimeService.ComputePathUidHex(DaCauTranStateAuraSpr, "GB2312", signedBytes: true));
            Assert.AreEqual("98055770", SprRuntimeService.ComputePathUidHex(KhangLongIcon, "GB2312", signedBytes: true));
        }

        [Test]
        public void CaiBangVisualResources_ExistInSpritesRuntimeBySignedHash()
        {
            AssertSprExistsBySignedHash(DragonFlightSpr, "Phi Long/Kháng Long dragon flight");
            AssertSprExistsBySignedHash(ImpactBurstSpr, "Impact burst");
            AssertSprExistsBySignedHash(PreCastSpr, "Pre-cast");
            AssertSprExistsBySignedHash(TuyDiepStateAuraSpr, "Túy Điệp state aura (43)");
            AssertSprExistsBySignedHash(DaCauTranStateAuraSpr, "Đả Cẩu Trận state aura (44)");
            AssertSprExistsBySignedHash(KhangLongIcon, "Kháng Long skill icon");

            // Phi Long icon UID is documented in evidence (also staged under SkillIconsPc).
            Assert.IsTrue(File.Exists(Path.Combine(SpritesRuntimeRoot, "d97b70ca.spr")),
                "Phi Long skill icon d97b70ca.spr not found in SpritesRuntime");
        }

        [Test]
        public void CaiBangCastSfx_ExistInRuntimeAudioRoots()
        {
            // PC skills.txt cols 7/8 ManCast/FMCast for 128/357 + missile 166 flight SFX.
            string streaming = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets", "sound", "skill");
            string runtime = Path.Combine(Directory.GetCurrentDirectory(), "AudioRuntime", "Skill");
            foreach (var clip in new[] { "sound_k005.wav", "sound_k010.wav", "sound_k037.wav" })
            {
                bool inStreaming = File.Exists(Path.Combine(streaming, clip));
                bool inRuntime = File.Exists(Path.Combine(runtime, clip));
                Assert.IsTrue(inStreaming || inRuntime,
                    $"cast SFX '{clip}' not found in StreamingAssets/sound/skill or AudioRuntime/Skill");
            }
        }

        [Test, Category("Slow")]
        public void CaiBangDragonImpactAndAuraSpr_DecodeToValidFrames()
        {
            var svc = new SprRuntimeService(SpritesRuntimeRoot);

            foreach (var (pcPath, label) in new[]
            {
                (DragonFlightSpr, "dragon flight"),
                (ImpactBurstSpr, "impact burst"),
                (TuyDiepStateAuraSpr, "Túy Điệp aura"),
            })
            {
                var sprite = svc.ResolveSprite(pcPath, 64, 64);
                Assert.IsNotNull(sprite, $"{label}: SprRuntimeService failed to resolve/decode '{pcPath}'");
                Assert.IsNotNull(sprite.texture, $"{label}: decoded sprite has no texture");
                Assert.Greater(sprite.texture.width, 0, $"{label}: decoded width must be > 0");
                Assert.Greater(sprite.texture.height, 0, $"{label}: decoded height must be > 0");
            }
        }
    }
}
