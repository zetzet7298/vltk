// -----------------------------------------------------------------------------
// VLTK.Survivor — PerfBudgetMonitor (ticket 42, profiling wrapper)
// File riêng: 1 MonoBehaviour/file (Unity 6 resolve scene reference fail với
// multi-class file). Log frame budget mỗi interval — đối chiếu 16.7ms.
//
//  - Wrapper mỏng quanh PerfBudget (logic thuần — PerfBudget.cs):
//    Tick(Time.unscaledDeltaTime) — unscaled để budget vẫn chạy khi pause
//    card/settings (timescale 0).
//  - Scene: thêm lên SurvivorDirector GO.
// -----------------------------------------------------------------------------

using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Wrapper MonoBehaviour — log mỗi 5s (unscaled: chạy cả khi pause).</summary>
    public sealed class PerfBudgetMonitor : MonoBehaviour
    {
        [Tooltip("Interval report giây; ≤0 → 5s.")]
        public float ReportIntervalSeconds = 5f;

        public PerfBudget Budget { get; private set; }
        public PerfBudget.Report? LastReport { get; private set; }

        private void Awake()
        {
            Budget = new PerfBudget(ReportIntervalSeconds, r =>
            {
                LastReport = r;
                int monsters = SurvivorGameDirector.Instance != null
                    ? SurvivorGameDirector.Instance.Monsters.Count : -1;
                Debug.Log($"[PerfBudget] frames={r.Frames} avg={r.AvgMs:F1}ms " +
                          $"min={r.MinMs:F1}ms max={r.MaxMs:F1}ms " +
                          $"({1000d / r.AvgMs:F1}fps) monsters={monsters}");
            });
        }

        private void Update()
        {
            if (Budget != null) Budget.Tick(Time.unscaledDeltaTime); // pause-proof (timescale 0)
        }
    }
}
