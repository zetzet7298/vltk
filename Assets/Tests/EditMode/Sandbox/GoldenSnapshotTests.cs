using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Tests.Sandbox
{
    /// <summary>
    /// M1.11 — Visual Golden Snapshot tests. Covers snapshot build/save,
    /// identical diff = 0, over-tolerance regression flag, the documented
    /// golden-update path, and serialize round-trip stability (AC#1–AC#4).
    /// </summary>
    public class GoldenSnapshotTests
    {
        // Build an RGBA32 buffer of a solid color.
        private byte[] SolidRgba(int w, int h, byte r, byte g, byte b, byte a = 255)
        {
            var buf = new byte[w * h * 4];
            for (int i = 0; i < w * h; i++)
            {
                buf[i * 4 + 0] = r;
                buf[i * 4 + 1] = g;
                buf[i * 4 + 2] = b;
                buf[i * 4 + 3] = a;
            }
            return buf;
        }

        // Build a buffer where the left half is colorA and the right half is colorB.
        private byte[] SplitRgba(int w, int h, Color32 left, Color32 right)
        {
            var buf = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int i = (y * w + x) * 4;
                var c = x < w / 2 ? left : right;
                buf[i + 0] = c.r; buf[i + 1] = c.g; buf[i + 2] = c.b; buf[i + 3] = c.a;
            }
            return buf;
        }

        private GoldenSnapshot BuildSnapshot(
            string mapId, int width, int height, byte[] rgba,
            int gridX = 16, int gridY = 16, string toolVersion = null, long generatedAt = 0,
            string caseId = "fixture", int skillId = 1, string faction = "test_faction", int frame = 0, long tick = 0)
        {
            var snapshot = GoldenSnapshotComparer.Build(
                mapId, width, height, rgba, gridX, gridY, toolVersion, generatedAt,
                caseId, skillId, faction, frame, tick);
            snapshot.skillFxLayer = 8;
            snapshot.skillFxLayerName = "SkillFx";
            return snapshot;
        }

        // --- AC#1: build/save snapshot with image dims + metadata ---

        [Test]
        public void Build_ProducesSignatureAndMetadata()
        {
            var rgba = SolidRgba(64, 64, 100, 150, 200);
            var snap = BuildSnapshot("map_fixture", 64, 64, rgba,
                gridX: 16, gridY: 16, toolVersion: "test-1.0", generatedAt: 12345);

            Assert.AreEqual("map_fixture", snap.mapId);
            Assert.AreEqual(64, snap.width);
            Assert.AreEqual(64, snap.height);
            Assert.AreEqual(256, snap.SignatureLength); // 16x16 grid
            Assert.AreEqual("test-1.0", snap.toolVersion);
            Assert.AreEqual(12345, snap.generatedAt);
            Assert.IsNotEmpty(snap.contentHash);
            Assert.AreNotEqual("EMPTY", snap.contentHash);
        }

        [Test]
        public void Build_IsDeterministic_SameInputSameSignature()
        {
            var a = BuildSnapshot("m", 32, 32, SolidRgba(32, 32, 10, 20, 30));
            var b = BuildSnapshot("m", 32, 32, SolidRgba(32, 32, 10, 20, 30));
            CollectionAssert.AreEqual(a.signature, b.signature);
            Assert.AreEqual(a.contentHash, b.contentHash);
        }

        [Test]
        public void Build_InvalidPayload_MarksEmpty()
        {
            var snap = BuildSnapshot("bad", 10, 10, null);
            Assert.AreEqual("EMPTY", snap.contentHash);
            Assert.AreEqual(0, snap.SignatureLength);
        }

        [Test]
        public void Build_EmitsV2ContractMetadata()
        {
            var snap = BuildSnapshot("map_fixture", 16, 16, SolidRgba(16, 16, 1, 2, 3),
                gridX: 4, gridY: 2, caseId: "cast_001", skillId: 42, faction: "cai_bang", frame: 3, tick: 99);

            Assert.AreEqual(GoldenSnapshot.SchemaV2, snap.schema);
            Assert.AreEqual(GoldenSnapshot.ComparerV1, snap.comparerVersion);
            Assert.AreEqual("cast_001", snap.caseId);
            Assert.AreEqual(4, snap.gridX);
            Assert.AreEqual(2, snap.gridY);
            Assert.AreEqual(GoldenSnapshot.AlphaPremultiplyTransparentBlack, snap.alphaMode);
            Assert.AreEqual(GoldenSnapshot.ColorRec709LumaBuckets, snap.colorSpace);
            Assert.IsNotEmpty(snap.unityColorSpace);
            Assert.AreEqual(GoldenSnapshot.PayloadRgba32U8, snap.payload);
            Assert.IsTrue(GoldenSnapshotComparer.TryValidate(snap, out var error), error);
        }

        [Test]
        public void Validation_RejectsDefaultProvenance()
        {
            var snapshot = GoldenSnapshotComparer.Build("map_fixture", 16, 16, SolidRgba(16, 16, 1, 2, 3));

            Assert.IsFalse(GoldenSnapshotComparer.TryValidate(snapshot, out var error));
            StringAssert.Contains("provenance is incomplete", error);
        }

        // --- AC#2/AC#3: identical diff = 0; over-tolerance flags regression ---

        [Test]
        public void Compare_RejectsCrossIdentityEmptyAndNonFiniteTolerance()
        {
            var golden = BuildSnapshot("map_a", 16, 16, SolidRgba(16, 16, 4, 5, 6), caseId: "case_a");
            var crossMap = BuildSnapshot("map_b", 16, 16, SolidRgba(16, 16, 4, 5, 6), caseId: "case_a");
            var crossCase = BuildSnapshot("map_a", 16, 16, SolidRgba(16, 16, 4, 5, 6), caseId: "case_b");
            var empty = BuildSnapshot("map_a", 16, 16, null, caseId: "case_a");

            Assert.IsTrue(GoldenSnapshotComparer.Compare(golden, crossMap).isRegression);
            Assert.IsTrue(GoldenSnapshotComparer.Compare(golden, crossCase).isRegression);
            Assert.IsTrue(GoldenSnapshotComparer.Compare(golden, empty).isRegression);
            Assert.IsTrue(GoldenSnapshotComparer.Compare(golden, golden, double.NaN).isRegression);
            Assert.IsTrue(GoldenSnapshotComparer.Compare(golden, golden, double.PositiveInfinity).isRegression);
        }

        [Test]
        public void Compare_RejectsChromaDifferenceInSameRec709LumaBucket()
        {
            // (0,0,64) and (24,0,0) quantize to distinct RGB buckets in same luma bucket.
            var golden = BuildSnapshot("m", 16, 16, SolidRgba(16, 16, 0, 0, 64), caseId: "chroma");
            var candidate = BuildSnapshot("m", 16, 16, SolidRgba(16, 16, 24, 0, 0), caseId: "chroma");

            var report = GoldenSnapshotComparer.Compare(golden, candidate, tolerance: 1.0);
            Assert.IsTrue(report.isRegression);
            Assert.That(report.notes, Has.Some.Contains("Bucket difference ratio"));
        }

        [Test]
        public void Compare_IdenticalSnapshots_DiffZeroNoRegression()
        {
            var golden = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 50, 60, 70));
            var candidate = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 50, 60, 70));

            var report = GoldenSnapshotComparer.Compare(golden, candidate);
            Assert.AreEqual(0.0, report.differenceRatio);
            Assert.AreEqual(0, report.differingBuckets);
            Assert.AreEqual(1.0, report.structuralSimilarity);
            Assert.AreEqual(0.0, report.similarityDistance);
            Assert.IsTrue(report.IsIdentical);
            Assert.IsFalse(report.isRegression);
        }

        [Test]
        public void Compare_SmallNoiseUnderTolerance_NoRegression()
        {
            // Golden solid; candidate differs in one bucket only (1/256 ≈ 0.39%).
            var golden = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 50, 60, 70));
            var rgba = SolidRgba(64, 64, 50, 60, 70);
            // Flip a 4x4 cell (one grid bucket) to a wildly different color.
            for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                int i = (y * 64 + x) * 4;
                rgba[i] = 255; rgba[i + 1] = 0; rgba[i + 2] = 0;
            }
            var candidate = BuildSnapshot("m", 64, 64, rgba);

            var report = GoldenSnapshotComparer.Compare(golden, candidate, tolerance: 0.02);
            Assert.LessOrEqual(report.differingBuckets, 1);
            Assert.Less(report.differenceRatio, 0.02);
            Assert.IsFalse(report.isRegression);
        }

        [Test]
        public void Compare_UniformLuminanceShift_HighSsimStillHonorsBucketHardCap()
        {
            // Synthetic metric proof, not PC parity: every quantized bucket changes,
            // but a small uniform luminance shift remains structurally similar.
            var golden = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 100, 100, 100));
            var candidate = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 110, 110, 110));

            var report = GoldenSnapshotComparer.Compare(golden, candidate, tolerance: 0.02);
            Assert.AreEqual(1.0, report.differenceRatio, "bucket diagnostic still records every changed bucket");
            Assert.GreaterOrEqual(report.structuralSimilarity, 0.98);
            Assert.LessOrEqual(report.similarityDistance, 0.02);
            Assert.IsTrue(report.isRegression, "full-frame quantized color drift must not pass on luminance SSIM alone");
            Assert.That(report.notes, Has.Some.Contains("Bucket difference ratio"));
        }

        [Test]
        public void Compare_StructuralChange_FlagsRegression()
        {
            // Same palette and mean luminance, opposite bucket layout.
            var golden = BuildSnapshot("m", 64, 64,
                SplitRgba(64, 64, new Color32(0, 0, 0, 255), new Color32(255, 255, 255, 255)));
            var candidate = BuildSnapshot("m", 64, 64,
                SplitRgba(64, 64, new Color32(255, 255, 255, 255), new Color32(0, 0, 0, 255)));

            var report = GoldenSnapshotComparer.Compare(golden, candidate, tolerance: 0.02);
            Assert.Less(report.structuralSimilarity, 0.0);
            Assert.Greater(report.similarityDistance, 0.02);
            Assert.IsTrue(report.isRegression);
        }

        [Test]
        public void Compare_ConstantImages_AreFiniteAndBounded()
        {
            var black = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 0, 0, 0));
            var white = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 255, 255, 255));

            var identical = GoldenSnapshotComparer.Compare(black, black);
            var different = GoldenSnapshotComparer.Compare(black, white, tolerance: 0.02);
            Assert.AreEqual(1.0, identical.structuralSimilarity);
            Assert.GreaterOrEqual(different.structuralSimilarity, -1.0);
            Assert.LessOrEqual(different.structuralSimilarity, 1.0);
            Assert.Less(different.structuralSimilarity, 1.0);
            Assert.IsTrue(different.isRegression);
        }

        [Test]
        public void Compare_FullyTransparentRgbNoise_IsRejected()
        {
            var transparent = BuildSnapshot("m", 32, 32, SolidRgba(32, 32, 255, 0, 255, 0));
            var opaque = BuildSnapshot("m", 32, 32, SolidRgba(32, 32, 0, 255, 0));

            Assert.IsFalse(GoldenSnapshotComparer.TryValidate(transparent, out var error));
            StringAssert.Contains("fully transparent", error);
            Assert.IsTrue(GoldenSnapshotComparer.Compare(transparent, opaque).isRegression);
        }

        [Test]
        public void Compare_OverTolerance_FlagsRegression()
        {
            var golden = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 0, 0, 0));
            // Candidate completely different (every bucket differs).
            var candidate = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 255, 255, 255));

            var report = GoldenSnapshotComparer.Compare(golden, candidate, tolerance: 0.02);
            Assert.AreEqual(1.0, report.differenceRatio);
            Assert.Less(report.structuralSimilarity, 0.01);
            Assert.IsTrue(report.isRegression);
            Assert.IsNotEmpty(report.notes);
        }

        [Test]
        public void Compare_DimensionMismatch_AlwaysRegression()
        {
            var golden = BuildSnapshot("m", 64, 64, SolidRgba(64, 64, 10, 10, 10));
            var candidate = BuildSnapshot("m", 32, 32, SolidRgba(32, 32, 10, 10, 10));

            var report = GoldenSnapshotComparer.Compare(golden, candidate);
            Assert.IsFalse(report.dimensionsMatch);
            Assert.AreEqual(0.0, report.structuralSimilarity);
            Assert.AreEqual(1.0, report.similarityDistance);
            Assert.IsTrue(report.isRegression);
        }

        [Test]
        public void Compare_NullSnapshot_RegressionRatioOne()
        {
            var golden = BuildSnapshot("m", 16, 16, SolidRgba(16, 16, 1, 2, 3));
            var report = GoldenSnapshotComparer.Compare(golden, null);
            Assert.AreEqual(1.0, report.differenceRatio);
            Assert.AreEqual(0.0, report.structuralSimilarity);
            Assert.IsTrue(report.isRegression);
        }

        // --- AC#4: golden-update path records the reason ---

        [Test]
        public void AcceptAsGolden_RecordsReasonAndCopiesSignature()
        {
            var candidate = BuildSnapshot("m", 64, 64,
                SplitRgba(64, 64, new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255)));

            var golden = GoldenSnapshotComparer.AcceptAsGolden(candidate, "Intentional terrain palette change", 999);

            Assert.AreEqual("Intentional terrain palette change", golden.goldenUpdateReason);
            Assert.AreEqual(999, golden.generatedAt);
            CollectionAssert.AreEqual(candidate.signature, golden.signature);
            // The accepted golden now matches the candidate exactly (diff 0).
            var report = GoldenSnapshotComparer.Compare(golden, candidate);
            Assert.IsTrue(report.IsIdentical);
            Assert.IsFalse(report.isRegression);
        }

        [Test]
        public void AcceptAsGolden_EmptyReason_Throws()
        {
            var candidate = BuildSnapshot("m", 16, 16, SolidRgba(16, 16, 1, 1, 1));
            Assert.Throws<System.ArgumentException>(
                () => GoldenSnapshotComparer.AcceptAsGolden(candidate, "  ", 1));
        }

        // --- Integration: serialize round-trip stability ---

        [Test]
        public void Snapshot_JsonRoundTrip_IsStable()
        {
            var snap = BuildSnapshot("m", 48, 48,
                SplitRgba(48, 48, new Color32(12, 34, 56, 255), new Color32(200, 100, 50, 255)),
                toolVersion: "rt-1.0", generatedAt: 555);

            var json = JsonUtility.ToJson(snap);
            var restored = JsonUtility.FromJson<GoldenSnapshot>(json);

            Assert.AreEqual(snap.schema, restored.schema);
            Assert.AreEqual(snap.comparerVersion, restored.comparerVersion);
            Assert.AreEqual(snap.mapId, restored.mapId);
            Assert.AreEqual(snap.caseId, restored.caseId);
            Assert.AreEqual(snap.width, restored.width);
            Assert.AreEqual(snap.gridX, restored.gridX);
            Assert.AreEqual(snap.gridY, restored.gridY);
            Assert.AreEqual(snap.alphaMode, restored.alphaMode);
            Assert.AreEqual(snap.colorSpace, restored.colorSpace);
            Assert.AreEqual(snap.unityColorSpace, restored.unityColorSpace);
            Assert.AreEqual(snap.payload, restored.payload);
            Assert.AreEqual(snap.height, restored.height);
            Assert.AreEqual(snap.contentHash, restored.contentHash);
            Assert.AreEqual(snap.toolVersion, restored.toolVersion);
            Assert.AreEqual(snap.generatedAt, restored.generatedAt);
            CollectionAssert.AreEqual(snap.signature, restored.signature);

            // A restored golden compares identical to the original candidate.
            var report = GoldenSnapshotComparer.Compare(restored, snap);
            Assert.IsTrue(report.IsIdentical);
        }
    }
}
