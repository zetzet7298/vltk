using System;
using System.Collections.Generic;
using System.Text;
using VLTK.Core;
using VLTK.Model;

namespace VLTK.Sandbox
{
    /// <summary>M1.11 — result of comparing a candidate snapshot against a golden.</summary>
    public class GoldenDiffReport
    {
        public string mapId;
        public bool dimensionsMatch;
        public int signatureLength;
        public int differingBuckets;
        /// <summary>Normalized difference ratio in [0,1] across the perceptual signature.</summary>
        public double differenceRatio;
        public double tolerance;
        public bool isRegression;
        public List<string> notes = new();

        public bool IsIdentical => differenceRatio == 0.0 && dimensionsMatch;
    }

    /// <summary>
    /// M1.11 — builds deterministic snapshots from captured pixel payloads and
    /// compares a candidate against a golden, flagging visual regressions beyond a
    /// tolerance. Pure C# (no live render) so it is fully EditMode-testable; the
    /// live RenderTexture capture is a thin documented MonoBehaviour wrapper.
    /// </summary>
    public static class GoldenSnapshotComparer
    {
        public const double DefaultTolerance = 0.02; // 2% of buckets may drift.

        /// <summary>
        /// Build a snapshot from an RGBA32 pixel buffer (length = width*height*4),
        /// reducing it to a fixed grid of average-color buckets for a deterministic,
        /// noise-tolerant signature. Channels are quantized to 5 bits each (0..31).
        /// </summary>
        public static GoldenSnapshot Build(
            string mapId, int width, int height, byte[] rgba,
            int gridX = 16, int gridY = 16, string toolVersion = null, long generatedAt = 0)
        {
            var snap = new GoldenSnapshot
            {
                mapId = mapId,
                width = width,
                height = height,
                toolVersion = toolVersion ?? SandboxVersion.Version,
                generatedAt = generatedAt,
            };

            if (rgba == null || width <= 0 || height <= 0 || rgba.Length < width * height * 4)
            {
                SubsystemLog.Warn("Golden", $"Invalid pixel payload for snapshot {mapId}");
                snap.contentHash = "EMPTY";
                return snap;
            }

            gridX = Math.Max(1, gridX);
            gridY = Math.Max(1, gridY);

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
                            sumR += rgba[i];
                            sumG += rgba[i + 1];
                            sumB += rgba[i + 2];
                            count++;
                        }
                    }
                    if (count == 0) count = 1;

                    int r = (int)(sumR / count) >> 3; // 0..31
                    int g = (int)(sumG / count) >> 3;
                    int b = (int)(sumB / count) >> 3;
                    int bucket = (r << 10) | (g << 5) | b; // 15-bit perceptual bucket
                    snap.signature.Add(bucket);
                }
            }

            snap.contentHash = ComputeHash(snap.signature);
            return snap;
        }

        private static string ComputeHash(List<int> sig)
        {
            unchecked
            {
                ulong h = 1469598103934665603UL; // FNV-1a 64
                foreach (var v in sig)
                {
                    h ^= (uint)v;
                    h *= 1099511628211UL;
                }
                return h.ToString("X16");
            }
        }

        /// <summary>
        /// AC#2/AC#3 — compare a candidate snapshot to a golden, producing a
        /// deterministic difference report and flagging a regression when the
        /// difference ratio exceeds the tolerance.
        /// </summary>
        public static GoldenDiffReport Compare(
            GoldenSnapshot golden, GoldenSnapshot candidate, double tolerance = DefaultTolerance)
        {
            var report = new GoldenDiffReport
            {
                mapId = candidate?.mapId ?? golden?.mapId,
                tolerance = tolerance,
            };

            if (golden == null || candidate == null)
            {
                report.notes.Add("Missing snapshot (golden or candidate is null)");
                report.differenceRatio = 1.0;
                report.isRegression = true;
                return report;
            }

            report.dimensionsMatch = golden.width == candidate.width && golden.height == candidate.height;
            if (!report.dimensionsMatch)
                report.notes.Add($"Dimensions differ: golden {golden.width}x{golden.height} vs candidate {candidate.width}x{candidate.height}");

            var ga = golden.signature ?? new List<int>();
            var ca = candidate.signature ?? new List<int>();
            int len = Math.Max(ga.Count, ca.Count);
            report.signatureLength = len;

            int differing = 0;
            for (int i = 0; i < len; i++)
            {
                int gv = i < ga.Count ? ga[i] : -1;
                int cv = i < ca.Count ? ca[i] : -1;
                if (gv != cv) differing++;
            }
            report.differingBuckets = differing;
            report.differenceRatio = len == 0 ? 0.0 : (double)differing / len;

            // A dimension mismatch is always a regression; otherwise threshold the ratio.
            report.isRegression = !report.dimensionsMatch || report.differenceRatio > tolerance;
            if (report.isRegression && report.notes.Count == 0)
                report.notes.Add($"Difference ratio {report.differenceRatio:F4} exceeds tolerance {tolerance:F4}");

            return report;
        }

        /// <summary>
        /// AC#4 — produce a new golden from a candidate, recording the documented
        /// update reason and a fresh timestamp. The reason is required; an empty
        /// reason is rejected so intentional updates are always traceable.
        /// </summary>
        public static GoldenSnapshot AcceptAsGolden(GoldenSnapshot candidate, string reason, long generatedAt)
        {
            if (candidate == null) throw new ArgumentNullException(nameof(candidate));
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("A golden update reason is required", nameof(reason));

            var golden = new GoldenSnapshot
            {
                mapId = candidate.mapId,
                width = candidate.width,
                height = candidate.height,
                signature = new List<int>(candidate.signature ?? new List<int>()),
                contentHash = candidate.contentHash,
                toolVersion = candidate.toolVersion,
                generatedAt = generatedAt,
                goldenUpdateReason = reason,
            };
            SubsystemLog.Info("Golden", $"Golden updated for {golden.mapId}: {reason}");
            return golden;
        }
    }
}
