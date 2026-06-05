// -----------------------------------------------------------------------------
// VLTK Mobile — UI Loading Screen Panel Service (Màn Hình Tải)
// Theo dõi tiến trình load: connect, download catalog, parse, build UI, ready.
// Vietnamese: "Đang tải", "Kết nối máy chủ", "Tải dữ liệu", "Sẵn sàng".
// -----------------------------------------------------------------------------

using System.Collections.Generic;

namespace VLTK.UI
{
    public struct LoadingScreenStep
    {
        public string name;
        public float percent;
        public int status; // 0=pending, 1=loading, 2=done, 3=failed

        public LoadingScreenStep(string name, float percent, int status)
        {
            this.name = name ?? string.Empty;
            this.percent = percent;
            this.status = status;
        }
    }

    public sealed class LoadingScreenSnapshot
    {
        public int totalSteps;
        public int completedSteps;
        public LoadingScreenStep currentStep;
        public float totalPercent;
        public bool isComplete;
        public bool hasFailed;
    }

    /// <summary>
    /// Panel service Màn Hình Tải — quản lý tiến trình load game.
    /// </summary>
    public static class LoadingScreenPanelService
    {
        public const int StepConnectServer = 0;
        public const int StepDownloadCatalog = 1;
        public const int StepParseSkills = 2;
        public const int StepParseItems = 3;
        public const int StepParseMaps = 4;
        public const int StepLoadPlayerData = 5;
        public const int StepBuildUI = 6;
        public const int StepReady = 7;

        public const int StatusPending = 0;
        public const int StatusLoading = 1;
        public const int StatusDone = 2;
        public const int StatusFailed = 3;

        public const int TotalSteps = 8;

        private static readonly List<LoadingScreenStep> _steps = new List<LoadingScreenStep>();
        private static bool _initialized = false;

        private static void EnsureInit()
        {
            if (_initialized) return;
            _steps.Clear();
            _steps.Add(new LoadingScreenStep("Kết nối máy chủ", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Tải dữ liệu catalog", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Phân tích kỹ năng", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Phân tích vật phẩm", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Phân tích bản đồ", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Tải dữ liệu nhân vật", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Xây dựng giao diện", 0f, StatusPending));
            _steps.Add(new LoadingScreenStep("Sẵn sàng", 0f, StatusPending));
            _initialized = true;
        }

        public static LoadingScreenSnapshot BuildSnapshot()
        {
            EnsureInit();
            var snap = new LoadingScreenSnapshot
            {
                totalSteps = _steps.Count,
                completedSteps = 0,
                currentStep = _steps.Count > 0 ? _steps[0] : new LoadingScreenStep(),
                totalPercent = 0f,
                isComplete = false,
                hasFailed = false,
            };
            try
            {
                float pct = 0f;
                int done = 0;
                bool failed = false;
                int currentIdx = 0;
                for (int i = 0; i < _steps.Count; i++)
                {
                    var s = _steps[i];
                    if (s.status == StatusDone) { done++; pct += 1f; }
                    else if (s.status == StatusFailed) { failed = true; pct += 0.5f; }
                    else if (s.status == StatusLoading) { currentIdx = i; pct += 0.5f; break; }
                    else break;
                }
                if (done == _steps.Count && !failed) snap.isComplete = true;
                if (failed) snap.hasFailed = true;
                snap.completedSteps = done;
                snap.totalPercent = _steps.Count > 0 ? (pct / _steps.Count) * 100f : 0f;
                if (currentIdx < _steps.Count)
                    snap.currentStep = _steps[currentIdx];
            }
            catch { }
            return snap;
        }

        public static LoadingScreenStep? GetStep(int idx)
        {
            EnsureInit();
            if (idx < 0 || idx >= _steps.Count) return null;
            return _steps[idx];
        }

        public static bool SetStepStatus(int idx, int status)
        {
            EnsureInit();
            if (idx < 0 || idx >= _steps.Count) return false;
            if (status < StatusPending || status > StatusFailed) return false;
            _steps[idx] = new LoadingScreenStep(_steps[idx].name, _steps[idx].percent, status);
            return true;
        }

        public static float GetTotalPercent()
        {
            return BuildSnapshot().totalPercent;
        }

        public static void Reset()
        {
            _initialized = false;
            _steps.Clear();
            EnsureInit();
        }

        public static void Skip()
        {
            for (int i = 0; i < _steps.Count; i++)
                _steps[i] = new LoadingScreenStep(_steps[i].name, 100f, StatusDone);
        }
    }
}
