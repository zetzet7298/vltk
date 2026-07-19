using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;
using VLTK.UI;

namespace VLTK.Tests.Sandbox
{
    [TestFixture]
    public class SkillVisualDataDrivenParityTests
    {
        private static string StreamingAssetsRoot =>
            Path.Combine(Directory.GetCurrentDirectory(), "Assets", "StreamingAssets");

        private static MethodInfo DirectionMethod(Type rendererType, string name)
        {
            var method = rendererType.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(method, $"{rendererType.Name}.{name} missing.");
            return method;
        }

        // KMissleRes: width=64/nSprDir; nDir/width; half-up remainder; wrap.
        private static int ExpectedPcDirection(int nDir, int directions)
        {
            int width = 64 / directions;
            int expected = nDir / width;
            if (nDir % width >= 32 / directions) expected++;
            return expected % directions;
        }

        // KMath.cpp g_nCosBuffer/g_nSinBuffer, source-order rows 0..63.
        // These are test-oracle inputs, not production arrays.
        private static readonly int[] PcCos64 =
        {
            0, -100, -199, -297, -391, -482, -568, -649, -724, -791, -851, -903, -946, -979, -1004, -1019,
            -1024, -1019, -1004, -979, -946, -903, -851, -791, -724, -649, -568, -482, -391, -297, -199, -100,
            0, 100, 199, 297, 391, 482, 568, 649, 724, 791, 851, 903, 946, 979, 1004, 1019,
            1024, 1019, 1004, 979, 946, 903, 851, 791, 724, 649, 568, 482, 391, 297, 199, 100,
        };

        private static readonly int[] PcSin64 =
        {
            1024, 1019, 1004, 979, 946, 903, 851, 791, 724, 649, 568, 482, 391, 297, 199, 100,
            0, -100, -199, -297, -391, -482, -568, -649, -724, -791, -851, -903, -946, -979, -1004, -1019,
            -1024, -1019, -1004, -979, -946, -903, -851, -791, -724, -649, -568, -482, -391, -297, -199, -100,
            0, 100, 199, 297, 391, 482, 568, 649, 724, 791, 851, 903, 946, 979, 1004, 1019,
        };

        // Literal expected outputs from KMath.h strict `nSin > g_nSin[i]` scan.
        private static readonly int[] PcTableVectorDirections =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
            16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31,
            31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46,
            47, 48, 49, 50, 51, 52, 53, 54, 55, 56, 57, 58, 59, 60, 61, 62,
        };

        private static string CsvSha256(int[] values)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(Encoding.ASCII.GetBytes(string.Join(",", values))))
                .Replace("-", "").ToLowerInvariant();
        }

        [Test]
        public void CaiBangMissile48_UsesCanonicalPcFrameDirectionAndInterval()
        {
            var mapper = new PcSkillVisualAutoMapper();
            mapper.Initialize(StreamingAssetsRoot);
            var config = mapper.GetVisualConfig(new SkillDefinition { skillId = 900048, childSkillId = 48 });

            // missles1.txt row 48, AnimFile2/AnimFileInfo2: 80,16,1.
            Assert.AreEqual(80, config.flightFrames);
            Assert.AreEqual(16, config.flightDirections);
            Assert.AreEqual(1, config.flightIntervalTicks);
        }

        [Test]
        public void StateAura44_PreservesCanonicalFrameDataThroughVisualService()
        {
            var skill = new SkillDefinition { skillId = 900044, stateSpecialId = 44 };
            var mapper = new PcSkillVisualAutoMapper();
            var config = mapper.GetVisualConfig(skill);
            var fx = new SkillEffectVisualService(null).PlaySkillCast(skill, Vector2.zero, Vector2.zero, 1);

            // 状态与光效图形对照表 state 44: 8 frames, one direction, one tick interval.
            Assert.AreEqual(8, config.stateAuraTotalFrames);
            Assert.AreEqual(8, fx.pcPreCastTotalFrames);
            Assert.AreEqual(1, fx.pcPreCastDirections);
            Assert.AreEqual(1, fx.pcPreCastIntervalTicks);
        }

        [Test]
        public void CaiBangVisuals_KeepNativePcSpriteScale()
        {
            var catalog = PcCombatCatalogFactory.CreateNoviceAndCaiBangCatalog();
            var service = new SkillEffectVisualService(null, catalog);
            var fx = service.PlaySkillCast(catalog.Resolve(128), Vector2.zero, Vector2.right * 400f, 20);

            Assert.IsNotNull(fx);
            Assert.AreEqual(80, fx.pcMissileTotalFrames);
            Assert.AreEqual(16, fx.pcMissileDirections);
            Assert.AreEqual(1, fx.pcMissileIntervalTicks);
            Assert.AreEqual(1f, fx.pcSpriteRenderScale);
        }

        [Test]
        public void PcIntegerDirectionTables_MatchCanonicalKMathHashes()
        {
            var sin = (int[])typeof(SkillEffectRenderer).GetField("PcScanSin",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            var sqrt = (int[])typeof(SkillEffectRenderer).GetField("PcSqrtTable",
                BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            Assert.AreEqual("fcc8947a4ce12baca8932b188f902e95b992701a7e393cce853b844dad638c0e", CsvSha256(sin));
            Assert.AreEqual("a4032c8b5461213d4053c9c451e786f143c3a76dce73545c72e01c3bead53deb", CsvSha256(sqrt));
            Assert.AreEqual("58ce27c0d1a81a77a8ef40ab6473b39a32369c36bcb53d0f2fb66d3aa4eaaaba", CsvSha256(PcSin64));
            Assert.AreEqual("ecc2081989edf5dacbe3184f8efe8d447593552964ef5bdc8a9144e784d1f522", CsvSha256(PcCos64));
        }

        [TestCase(typeof(SkillEffectRenderer))]
        [TestCase(typeof(SkillEffectWorldOverlay))]
        public void DirectionMapper_MatchesPcIntegerTableForAll64Rows(Type rendererType)
        {
            var fromInts = DirectionMethod(rendererType, "ComputePcDirection64FromInts");
            for (int nDir = 0; nDir < 64; nDir++)
            {
                int actual = (int)fromInts.Invoke(null, new object[] { 0, 0, PcCos64[nDir], PcSin64[nDir] });
                Assert.AreEqual(PcTableVectorDirections[nDir], actual, $"KMath row={nDir}");
            }
        }

        [TestCase(typeof(SkillEffectRenderer))]
        [TestCase(typeof(SkillEffectWorldOverlay))]
        public void DirectionMapper_MatchesPcCardinalAndStrictScanBoundaries(Type rendererType)
        {
            var fromInts = DirectionMethod(rendererType, "ComputePcDirection64FromInts");
            var fromVector = DirectionMethod(rendererType, "ComputePcDirection64");
            Assert.AreEqual(-1, (int)fromVector.Invoke(null, new object[] { Vector2.zero, Vector2.zero }));
            Assert.AreEqual(0, (int)fromVector.Invoke(null, new object[] { Vector2.zero, Vector2.up * 1024f }));
            Assert.AreEqual(47, (int)fromVector.Invoke(null, new object[] { Vector2.zero, Vector2.right * 1024f }));

            foreach (var test in new[]
            {
                (0, 0, 0, 0, -1),       // same point
                (0, 0, 0, 1024, 0),     // +Y is PC dir 0, not 32
                (0, 0, 0, -1024, 31),
                (0, 0, 1024, 0, 47),
                (0, 0, -1024, 0, 16),
                (0, 0, 1024, 1024, 55), // PC qsqrt + strict table scan
                (0, 0, -1024, -1024, 23),
                (0, 0, -10000, 990, 15),  // nSin=100: strict boundary stays row 15
                (0, 0, -10000, 1000, 14), // nSin=101: crosses row 15
                (0, 0, -10000, -980, 16),
                (0, 0, -10000, -990, 17),
            })
            {
                int actual = (int)fromInts.Invoke(null,
                    new object[] { test.Item1, test.Item2, test.Item3, test.Item4 });
                Assert.AreEqual(test.Item5, actual, $"from=({test.Item1},{test.Item2}) to=({test.Item3},{test.Item4})");
            }
        }

        [TestCase(typeof(SkillEffectRenderer))]
        [TestCase(typeof(SkillEffectWorldOverlay))]
        public void SpriteDirectionMapper_MatchesPcFormulaForAll64Headings(Type rendererType)
        {
            var map64 = DirectionMethod(rendererType, "MapPc64Direction");
            foreach (int directions in new[] { 1, 8, 16 })
            for (int nDir = 0; nDir < 64; nDir++)
                Assert.AreEqual(ExpectedPcDirection(nDir, directions),
                    (int)map64.Invoke(null, new object[] { nDir, directions }),
                    $"nDir={nDir}, directions={directions}");
        }

        [TestCase(typeof(SkillEffectRenderer))]
        [TestCase(typeof(SkillEffectWorldOverlay))]
        public void DirectionMapper_FrameIndexHonorsPcBucketAndIntervalBoundaries(Type rendererType)
        {
            var frameIndex = DirectionMethod(rendererType, "ComputePcMissileFrameIndex");

            // Auditor counterexamples: nDir=36,d=8 -> 5; nDir=6,d=16 -> 2.
            Assert.AreEqual(50, (int)frameIndex.Invoke(null, new object[] { 36, 80, 8, 0, 2 }));
            Assert.AreEqual(59, (int)frameIndex.Invoke(null, new object[] { 36, 80, 8, 19, 2 }));
            Assert.AreEqual(50, (int)frameIndex.Invoke(null, new object[] { 36, 80, 8, 20, 2 }));
            Assert.AreEqual(14, (int)frameIndex.Invoke(null, new object[] { 6, 80, 16, 8, 2 }));
            Assert.AreEqual(0, (int)frameIndex.Invoke(null, new object[] { 63, 80, 8, 0, 1 }));
        }
    }
}
