// -----------------------------------------------------------------------------
// VLTK.Survivor — PerfBudget (ticket 42, profiling)
// Frame time counter đơn giản: accumulator đo ms/frame, report mỗi interval
// (default 5s). Cho profiling plan (docs/survivor-profiling-plan.md): log
// avg/min/max + fps; đối chiếu budget 16.7ms.
//
//  - PerfBudget = logic thuần (test EditMode): inject dt trực tiếp (dt chính
//    là clock — không cần inject clock riêng), callback report có thể null
//    (test assert qua return Report?). dt âm → clamp 0 (fail-closed).
//  - PerfBudgetMonitor (Mono) ở file riêng — wrapper Tick(Time.unscaledDeltaTime)
//    (unscaled: budget chạy cả khi pause card/settings timescale 0).
// -----------------------------------------------------------------------------

using System;
using UnityEngine;

namespace VLTK.Survivor
{
    /// <summary>Bộ đếm frame time thuần — KHÔNG engine dependency.</summary>
    public sealed class PerfBudget
    {
        /// <summary>Report 1 window: frames, thời gian thật, ms avg/min/max.</summary>
        public struct Report
        {
            public int Frames;
            public double Seconds;
            public double AvgMs;
            public double MinMs;
            public double MaxMs;
        }

        private readonly double _intervalSeconds;
        private readonly Action<Report> _onReport;
        private double _accSeconds;
        private int _frames;
        private double _minMs;
        private double _maxMs;

        /// <summary>interval ≤ 0 → fallback 5s (fail-closed).</summary>
        public PerfBudget(float intervalSeconds = 5f, Action<Report> onReport = null)
        {
            _intervalSeconds = intervalSeconds > 0f ? intervalSeconds : 5f;
            _onReport = onReport;
        }

        /// <summary>
        /// Nạp 1 frame (dt giây). Chưa đủ interval → null. Đủ → report + reset,
        /// trả Report (để test không cần callback). dt âm → 0.
        /// </summary>
        public Report? Tick(double dtSeconds)
        {
            if (dtSeconds < 0d) dtSeconds = 0d;
            double ms = dtSeconds * 1000d;
            if (_frames == 0) { _minMs = ms; _maxMs = ms; }
            else { if (ms < _minMs) _minMs = ms; if (ms > _maxMs) _maxMs = ms; }
            _frames++;
            _accSeconds += dtSeconds;
            if (_accSeconds < _intervalSeconds) return null;
            var r = new Report
            {
                Frames = _frames,
                Seconds = _accSeconds,
                AvgMs = _accSeconds / _frames * 1000d,
                MinMs = _minMs,
                MaxMs = _maxMs,
            };
            _onReport?.Invoke(r);
            _accSeconds = 0d;
            _frames = 0;
            return r;
        }
    }
}
