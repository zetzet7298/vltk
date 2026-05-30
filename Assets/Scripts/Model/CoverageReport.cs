using System;
using System.Collections.Generic;

namespace VLTK.Model
{
    /// <summary>M1.12 — Project coverage report models.</summary>

    public enum ReportSeverity { Info, Warning, Error }
    public enum ReportKind { Map, Region, Sprite, Audio, Config, Unknown }

    [Serializable]
    public class CoverageIssue
    {
        public int mapId;
        public string regionFile;
        public string sourceId;
        public ReportKind kind;
        public ReportSeverity severity;
        public string message;
    }

    [Serializable]
    public class MapCoverageEntry
    {
        public int mapId;
        public string displayName;
        public ConversionStatus status;
        public int totalRegions;
        public int regionsConverted;
        public int regionsMissing;
        public int regionsFailed;
        public int assetsMissing;
        public int assetsInvalid;
        public List<CoverageIssue> issues = new();
    }

    [Serializable]
    public class ProjectCoverageReport
    {
        public int totalMaps;
        public int mapsAvailable;
        public int mapsMissing;
        public int totalRegions;
        public int regionsWithObstacle;
        public int regionsWithGround;
        public int totalAssetsMissing;
        public int totalAssetsInvalid;
        public string generatedAt;
        public List<MapCoverageEntry> maps = new();
        public List<CoverageIssue> globalIssues = new();

        /// <summary>Filter issues by severity across all maps.</summary>
        public List<CoverageIssue> GetIssues(ReportSeverity? severity = null, ReportKind? kind = null)
        {
            var result = new List<CoverageIssue>();
            foreach (var m in maps)
                foreach (var issue in m.issues)
                    if ((severity == null || issue.severity == severity) &&
                        (kind == null || issue.kind == kind))
                        result.Add(issue);
            foreach (var issue in globalIssues)
                if ((severity == null || issue.severity == severity) &&
                    (kind == null || issue.kind == kind))
                    result.Add(issue);
            return result;
        }
    }
}
