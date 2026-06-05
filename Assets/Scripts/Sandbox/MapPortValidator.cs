// -----------------------------------------------------------------------------
// VLTK Mobile — PC Region Data Validator
// -----------------------------------------------------------------------------

using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public readonly struct RegionValidationReport
    {
        public readonly bool ok;
        public readonly string message;
        public readonly int sectionCount;
        public readonly bool hasObstacle;
        public readonly bool hasNpc;
        public readonly bool hasObj;

        public RegionValidationReport(bool ok, string message, int sectionCount, bool hasObstacle, bool hasNpc, bool hasObj)
        {
            this.ok = ok;
            this.message = message;
            this.sectionCount = sectionCount;
            this.hasObstacle = hasObstacle;
            this.hasNpc = hasNpc;
            this.hasObj = hasObj;
        }
    }

    /// <summary>Validate file Region_S trước khi đưa vào streaming runtime.</summary>
    public static class MapPortValidator
    {
        public static RegionValidationReport ValidateRegionData(string regionPath)
        {
            if (string.IsNullOrWhiteSpace(regionPath))
                return new RegionValidationReport(false, "Đường dẫn Region_S rỗng", 0, false, false, false);
            if (!File.Exists(regionPath))
                return new RegionValidationReport(false, $"Không tìm thấy Region_S: {regionPath}", 0, false, false, false);

            var data = File.ReadAllBytes(regionPath);
            var parsed = RegionParser.Parse(data);
            if (!parsed.success)
                return new RegionValidationReport(false, parsed.error, parsed.sectionCount, false, false, false);

            return new RegionValidationReport(true, "Region_S hợp lệ", parsed.sectionCount, parsed.HasObstacle, parsed.HasNpc, parsed.HasObj);
        }

        public static Rect GetMapBounds(int regionCountX, int regionCountY, float regionWidth, float regionHeight)
        {
            return new Rect(0f, 0f, Mathf.Max(0, regionCountX) * regionWidth, Mathf.Max(0, regionCountY) * regionHeight);
        }
    }
}
