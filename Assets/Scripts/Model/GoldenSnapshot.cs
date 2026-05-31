using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>
    /// M1.11 — A reproducible snapshot of a converted map render: dimensions, a
    /// deterministic content signature (perceptual buckets / pixel digest), and
    /// metadata describing how/when it was produced. Pure data, fully serializable
    /// so it can round-trip through JSON for golden comparison.
    /// </summary>
    [Serializable]
    public class GoldenSnapshot
    {
        public string mapId;
        public int width;
        public int height;

        /// <summary>
        /// Deterministic perceptual signature: coarse average-color buckets over a
        /// fixed grid. Order is row-major (y outer, x inner). Comparison is done on
        /// this signature, not the raw pixels, so trivial encoding noise does not
        /// trip a false regression.
        /// </summary>
        public List<int> signature = new();

        /// <summary>Stable digest string derived from the signature (for quick equality).</summary>
        public string contentHash;

        // Metadata
        public long generatedAt;
        public string toolVersion;

        /// <summary>AC#4 — populated only when this snapshot is an intentional golden update.</summary>
        public string goldenUpdateReason;

        public int SignatureLength => signature?.Count ?? 0;
    }
}
