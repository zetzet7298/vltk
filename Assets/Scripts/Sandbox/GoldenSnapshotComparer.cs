using System;
using System.Collections.Generic;
using VLTK.Core;
using VLTK.Model;
using UnityEngine;

namespace VLTK.Sandbox
{
    public class GoldenDiffReport
    {
        public string mapId;
        public bool dimensionsMatch;
        public int signatureLength;
        public int differingBuckets;
        /// <summary>Diagnostic: changed quantized RGB buckets / bucket count.</summary>
        public double differenceRatio;
        /// <summary>Bounded global Rec. 709 luminance SSIM in [-1,1].</summary>
        public double structuralSimilarity;
        /// <summary>Acceptance metric: 1 - structuralSimilarity, clamped to [0,1].</summary>
        public double similarityDistance;
        public double tolerance;
        public bool isRegression;
        public List<string> notes = new();

        public bool IsIdentical => differenceRatio == 0.0 && dimensionsMatch && !isRegression;
    }

    /// <summary>
    /// Builds and compares v2 RGBA32 snapshots. Contract metadata is mandatory at
    /// comparison time: legacy, empty, cross-map, and cross-case inputs fail closed.
    /// </summary>
    public static class GoldenSnapshotComparer
    {
        private const double SsimC1 = 0.0001;
        private const double SsimC2 = 0.0009;
        public const double DefaultTolerance = 0.02;
        public const double MaxBucketDifferenceRatio = 0.02;

        /// <summary>
        /// Converts RGBA32 pixels to 5-bit RGB alpha-premultiplied buckets. Transparent
        /// pixels are deterministic black. gridX/gridY are persisted in v2 metadata.
        /// </summary>
        public static GoldenSnapshot Build(
            string mapId, int width, int height, byte[] rgba,
            int gridX = 16, int gridY = 16, string toolVersion = null, long generatedAt = 0,
            string caseId = "default", int skillId = -1, string faction = null, int frame = -1, long tick = -1)
        {
            gridX = Math.Max(1, gridX);
            gridY = Math.Max(1, gridY);
            var snap = new GoldenSnapshot
            {
                schema = GoldenSnapshot.SchemaV2,
                comparerVersion = GoldenSnapshot.ComparerV1,
                mapId = mapId,
                caseId = caseId,
                width = width,
                height = height,
                gridX = gridX,
                gridY = gridY,
                alphaMode = GoldenSnapshot.AlphaPremultiplyTransparentBlack,
                colorSpace = GoldenSnapshot.ColorRec709LumaBuckets,
                unityColorSpace = QualitySettings.activeColorSpace.ToString(),
                payload = GoldenSnapshot.PayloadRgba32U8,
                skillId = skillId,
                faction = faction,
                frame = frame,
                tick = tick,
                toolVersion = toolVersion ?? SandboxVersion.Version,
                generatedAt = generatedAt,
            };

            if (rgba == null || width <= 0 || height <= 0 || rgba.Length != width * height * 4)
            {
                SubsystemLog.Warn("Golden", $"Invalid pixel payload for snapshot {mapId}");
                snap.contentHash = "EMPTY";
                return snap;
            }

            for (int i = 3; i < rgba.Length; i += 4)
                if (rgba[i] != 0) snap.nonTransparentPixelCount++;

            for (int gy = 0; gy < gridY; gy++)
            {
                int y0 = (int)((long)gy * height / gridY);
                int y1 = (int)((long)(gy + 1) * height / gridY);
                if (y1 <= y0) y1 = y0 + 1;

                for (int gx = 0; gx < gridX; gx++)
                {
                    int x0 = (int)((long)gx * width / gridX);
                    int x1 = (int)((long)(gx + 1) * width / gridX);
                    if (x1 <= x0) x1 = x0 + 1;

                    long sumR = 0, sumG = 0, sumB = 0;
                    long count = 0;
                    for (int y = y0; y < y1 && y < height; y++)
                    {
                        int rowBase = y * width * 4;
                        for (int x = x0; x < x1 && x < width; x++)
                        {
                            int i = rowBase + x * 4;
                            int alpha = rgba[i + 3];
                            sumR += rgba[i] * alpha;
                            sumG += rgba[i + 1] * alpha;
                            sumB += rgba[i + 2] * alpha;
                            count++;
                        }
                    }

                    int r = (int)(sumR / (count * 255L)) >> 3;
                    int g = (int)(sumG / (count * 255L)) >> 3;
                    int b = (int)(sumB / (count * 255L)) >> 3;
                    snap.signature.Add((r << 10) | (g << 5) | b);
                }
            }

            snap.contentHash = ComputeHash(snap.signature);
            return snap;
        }

        public static GoldenDiffReport Compare(
            GoldenSnapshot golden, GoldenSnapshot candidate, double tolerance = DefaultTolerance)
        {
            bool validTolerance = !double.IsNaN(tolerance) && !double.IsInfinity(tolerance) && tolerance >= 0.0;
            var report = new GoldenDiffReport
            {
                mapId = candidate?.mapId ?? golden?.mapId,
                tolerance = validTolerance ? tolerance : 0.0,
            };

            if (golden == null || candidate == null)
                return Regression(report, "Missing snapshot (golden or candidate is null)");
            if (!TryValidate(golden, out var goldenError))
                return Regression(report, $"Invalid golden snapshot: {goldenError}");
            if (!TryValidate(candidate, out var candidateError))
                return Regression(report, $"Invalid candidate snapshot: {candidateError}");
            if (!validTolerance)
                return Regression(report, "Tolerance must be finite and non-negative");
            if (golden.mapId != candidate.mapId || golden.caseId != candidate.caseId ||
                golden.skillId != candidate.skillId || golden.faction != candidate.faction ||
                golden.frame != candidate.frame || golden.tick != candidate.tick ||
                golden.skillFxLayer != candidate.skillFxLayer || golden.skillFxLayerName != candidate.skillFxLayerName)
                return Regression(report, "Capture provenance differs");

            report.dimensionsMatch = golden.width == candidate.width && golden.height == candidate.height;
            if (!report.dimensionsMatch)
                return Regression(report, $"Dimensions differ: golden {golden.width}x{golden.height} vs candidate {candidate.width}x{candidate.height}");
            if (golden.gridX != candidate.gridX || golden.gridY != candidate.gridY)
                return Regression(report, $"Grid differs: golden {golden.gridX}x{golden.gridY} vs candidate {candidate.gridX}x{candidate.gridY}");
            if (golden.alphaMode != candidate.alphaMode || golden.colorSpace != candidate.colorSpace ||
                golden.unityColorSpace != candidate.unityColorSpace || golden.payload != candidate.payload ||
                golden.comparerVersion != candidate.comparerVersion)
                return Regression(report, "Capture metadata differs");

            var ga = golden.signature;
            var ca = candidate.signature;
            report.signatureLength = Math.Max(ga.Count, ca.Count);
            for (int i = 0; i < report.signatureLength; i++)
                if (i >= ga.Count || i >= ca.Count || ga[i] != ca[i]) report.differingBuckets++;
            report.differenceRatio = (double)report.differingBuckets / report.signatureLength;

            if (ga.Count != ca.Count)
                return Regression(report, $"Signature lengths differ: golden {ga.Count} vs candidate {ca.Count}");

            report.structuralSimilarity = ComputeSsim(ga, ca);
            report.similarityDistance = Math.Min(1.0, Math.Max(0.0, 1.0 - report.structuralSimilarity));
            report.isRegression = report.similarityDistance > report.tolerance ||
                                  report.differenceRatio > MaxBucketDifferenceRatio;
            if (report.isRegression)
            {
                if (report.similarityDistance > report.tolerance)
                    report.notes.Add($"SSIM {report.structuralSimilarity:F6} gives distance {report.similarityDistance:F6}, exceeds tolerance {report.tolerance:F6}");
                if (report.differenceRatio > MaxBucketDifferenceRatio)
                    report.notes.Add($"Bucket difference ratio {report.differenceRatio:F6} exceeds hard cap {MaxBucketDifferenceRatio:F6}");
            }
            return report;
        }

        public static bool TryValidate(GoldenSnapshot snapshot, out string error)
        {
            if (snapshot == null) { error = "snapshot is null"; return false; }
            if (snapshot.schema != GoldenSnapshot.SchemaV2) { error = "schema must be vltk.golden-snapshot/v2"; return false; }
            if (snapshot.comparerVersion != GoldenSnapshot.ComparerV1) { error = "unsupported comparer version"; return false; }
            if (string.IsNullOrWhiteSpace(snapshot.mapId) || string.IsNullOrWhiteSpace(snapshot.caseId)) { error = "mapId and caseId are required"; return false; }
            if (snapshot.width <= 0 || snapshot.height <= 0 || snapshot.gridX <= 0 || snapshot.gridY <= 0) { error = "dimensions and grid must be positive"; return false; }
            if (snapshot.alphaMode != GoldenSnapshot.AlphaPremultiplyTransparentBlack || snapshot.colorSpace != GoldenSnapshot.ColorRec709LumaBuckets ||
                string.IsNullOrWhiteSpace(snapshot.unityColorSpace) || snapshot.payload != GoldenSnapshot.PayloadRgba32U8) { error = "capture metadata is incomplete"; return false; }
            if (snapshot.skillId < 0 || string.IsNullOrWhiteSpace(snapshot.faction) || snapshot.frame < 0 || snapshot.tick < 0 ||
                snapshot.skillFxLayer < 0 || snapshot.skillFxLayer > 31 || string.IsNullOrWhiteSpace(snapshot.skillFxLayerName)) { error = "capture provenance is incomplete"; return false; }
            if (snapshot.nonTransparentPixelCount <= 0) { error = "capture is fully transparent"; return false; }
            if (snapshot.signature == null || snapshot.signature.Count != snapshot.gridX * snapshot.gridY || string.IsNullOrWhiteSpace(snapshot.contentHash) || snapshot.contentHash == "EMPTY") { error = "payload signature is empty or malformed"; return false; }
            foreach (var bucket in snapshot.signature)
                if (bucket < 0 || bucket > 0x7fff) { error = "payload bucket is out of range"; return false; }
            if (snapshot.contentHash != ComputeHash(snapshot.signature)) { error = "payload hash does not match signature"; return false; }
            error = null;
            return true;
        }

        public static GoldenSnapshot AcceptAsGolden(GoldenSnapshot candidate, string reason, long generatedAt)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A golden update reason is required", nameof(reason));
            if (!TryValidate(candidate, out var error)) throw new ArgumentException($"Candidate violates golden contract: {error}", nameof(candidate));

            var golden = new GoldenSnapshot
            {
                schema = candidate.schema, comparerVersion = candidate.comparerVersion, mapId = candidate.mapId, caseId = candidate.caseId,
                width = candidate.width, height = candidate.height, gridX = candidate.gridX, gridY = candidate.gridY,
                alphaMode = candidate.alphaMode, colorSpace = candidate.colorSpace, unityColorSpace = candidate.unityColorSpace, payload = candidate.payload,
                signature = new List<int>(candidate.signature), contentHash = candidate.contentHash,
                skillId = candidate.skillId, faction = candidate.faction, frame = candidate.frame, tick = candidate.tick,
                skillFxLayer = candidate.skillFxLayer, skillFxLayerName = candidate.skillFxLayerName,
                nonTransparentPixelCount = candidate.nonTransparentPixelCount,
                toolVersion = candidate.toolVersion, generatedAt = generatedAt, goldenUpdateReason = reason,
            };
            SubsystemLog.Info("Golden", $"Golden updated for {golden.mapId}/{golden.caseId}: {reason}");
            return golden;
        }

        private static GoldenDiffReport Regression(GoldenDiffReport report, string note)
        {
            report.notes.Add(note);
            report.differenceRatio = 1.0;
            report.structuralSimilarity = 0.0;
            report.similarityDistance = 1.0;
            report.isRegression = true;
            return report;
        }

        private static string ComputeHash(List<int> signature)
        {
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                foreach (var value in signature) { hash ^= (uint)value; hash *= 1099511628211UL; }
                return hash.ToString("X16");
            }
        }

        private static double ComputeSsim(List<int> golden, List<int> candidate)
        {
            int count = golden.Count;
            double meanGolden = 0.0, meanCandidate = 0.0;
            for (int i = 0; i < count; i++) { meanGolden += BucketLuminance(golden[i]); meanCandidate += BucketLuminance(candidate[i]); }
            meanGolden /= count;
            meanCandidate /= count;

            double varianceGolden = 0.0, varianceCandidate = 0.0, covariance = 0.0;
            for (int i = 0; i < count; i++)
            {
                double goldenDelta = BucketLuminance(golden[i]) - meanGolden;
                double candidateDelta = BucketLuminance(candidate[i]) - meanCandidate;
                varianceGolden += goldenDelta * goldenDelta;
                varianceCandidate += candidateDelta * candidateDelta;
                covariance += goldenDelta * candidateDelta;
            }
            varianceGolden /= count;
            varianceCandidate /= count;
            covariance /= count;
            double denominator = (meanGolden * meanGolden + meanCandidate * meanCandidate + SsimC1) * (varianceGolden + varianceCandidate + SsimC2);
            if (denominator <= 0.0) return 0.0;
            double ssim = ((2.0 * meanGolden * meanCandidate + SsimC1) * (2.0 * covariance + SsimC2)) / denominator;
            return Math.Max(-1.0, Math.Min(1.0, ssim));
        }

        private static double BucketLuminance(int bucket)
        {
            double r = ((bucket >> 10) & 31) / 31.0;
            double g = ((bucket >> 5) & 31) / 31.0;
            double b = (bucket & 31) / 31.0;
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}
