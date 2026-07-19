using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VLTK.Core;
using VLTK.Model;
using VLTK.Sandbox;

namespace VLTK.Sandbox
{
    /// <summary>
    /// M1.12 AC#5 — GM Tools tab for browsing the coverage report.
    /// Allows filtering issues by severity and kind.
    /// </summary>
    public class GMToolsTab : MonoBehaviour
    {
        [Header("UI References")]
        public Text reportSummaryText;
        public Text issueListText;
        public Button generateReportButton;
        public Button clearButton;
        public Dropdown filterSeverityDropdown;  // All/Info/Warning/Error
        public Dropdown filterKindDropdown;      // All/Map/Region/Sprite/Config

        [Header("Manual golden capture")]
        [Tooltip("Required stable case identity. Invoke CaptureActiveSkillFx from explicit GM UI only.")]
        public string goldenCaptureCaseId;

        private ProjectCoverageReport _report;
        private const int MAX_ISSUES_DISPLAY = 200;

        private void Start()
        {
            if (generateReportButton != null)
                generateReportButton.onClick.AddListener(GenerateReport);
            if (clearButton != null)
                clearButton.onClick.AddListener(ClearReport);

            if (filterSeverityDropdown != null)
                filterSeverityDropdown.onValueChanged.AddListener(_ => RefreshDisplay());
            if (filterKindDropdown != null)
                filterKindDropdown.onValueChanged.AddListener(_ => RefreshDisplay());
        }

        public void GenerateReport()
        {
            var mgr = SandboxManager.Instance;
            if (mgr == null)
            {
                SetSummary("SandboxManager not found");
                return;
            }

            _report = CoverageReportBuilder.Build(
                mgr.MapManager,
                mgr.AssetRegistry,
                null);  // RegionCatalog integrated in future

            SubsystemLog.Info("GMToolsTab", $"Coverage report generated: {_report.totalMaps} maps");
            RefreshDisplay();
        }

        /// <summary>Explicit GM-only capture. It never accepts this snapshot as a golden.</summary>
        public void CaptureActiveSkillFx()
        {
            try
            {
                var snapshot = GoldenSnapshotCaptureDriver.CaptureActive(SandboxManager.Instance, goldenCaptureCaseId);
                SetSummary($"Captured {snapshot.mapId}/{snapshot.caseId} skill={snapshot.skillId} frame={snapshot.frame} tick={snapshot.tick}");
                SubsystemLog.Info("Golden", $"GM captured {snapshot.mapId}/{snapshot.caseId}; not accepted as golden");
            }
            catch (System.Exception ex)
            {
                SetSummary($"Golden capture failed: {ex.Message}");
                SubsystemLog.Warn("Golden", $"GM capture failed: {ex.Message}");
            }
        }

        public void ClearReport()
        {
            _report = null;
            SetSummary("No report generated.");
            SetIssueList("");
        }

        private void RefreshDisplay()
        {
            if (_report == null)
            {
                SetSummary("No report. Click 'Generate Report'.");
                SetIssueList("");
                return;
            }

            // Summary
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Coverage Report ===");
            sb.AppendLine($"Generated: {_report.generatedAt}");
            sb.AppendLine($"Maps: {_report.totalMaps} ({_report.mapsAvailable} available, {_report.mapsMissing} missing)");
            sb.AppendLine($"Regions: {_report.totalRegions} ({_report.regionsWithObstacle} w/ obstacle, {_report.regionsWithGround} w/ ground)");
            sb.AppendLine($"Assets: {_report.totalAssetsMissing} missing, {_report.totalAssetsInvalid} invalid");

            var allErrors = _report.GetIssues(ReportSeverity.Error);
            var allWarnings = _report.GetIssues(ReportSeverity.Warning);
            sb.AppendLine($"Issues: {allErrors.Count} errors, {allWarnings.Count} warnings");
            SetSummary(sb.ToString());

            // Filtered issues
            var severity = GetSeverityFilter();
            var kind = GetKindFilter();
            var filtered = _report.GetIssues(severity, kind);

            var isb = new System.Text.StringBuilder();
            isb.AppendLine($"Showing {Mathf.Min(filtered.Count, MAX_ISSUES_DISPLAY)}/{filtered.Count} issues:");
            int shown = 0;
            foreach (var issue in filtered)
            {
                if (shown >= MAX_ISSUES_DISPLAY) break;
                var badge = issue.severity switch
                {
                    ReportSeverity.Error   => "[ERR]",
                    ReportSeverity.Warning => "[WRN]",
                    _                      => "[INF]",
                };
                isb.AppendLine($"{badge} Map {issue.mapId}: {issue.message}");
                shown++;
            }
            SetIssueList(isb.ToString());
        }

        private ReportSeverity? GetSeverityFilter()
        {
            if (filterSeverityDropdown == null) return null;
            return filterSeverityDropdown.value switch
            {
                1 => ReportSeverity.Info,
                2 => ReportSeverity.Warning,
                3 => ReportSeverity.Error,
                _ => null,
            };
        }

        private ReportKind? GetKindFilter()
        {
            if (filterKindDropdown == null) return null;
            return filterKindDropdown.value switch
            {
                1 => ReportKind.Map,
                2 => ReportKind.Region,
                3 => ReportKind.Sprite,
                4 => ReportKind.Config,
                _ => null,
            };
        }

        private void SetSummary(string text)
        {
            if (reportSummaryText != null)
                reportSummaryText.text = text;
        }

        private void SetIssueList(string text)
        {
            if (issueListText != null)
                issueListText.text = text;
        }
    }
}
