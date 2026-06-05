// -----------------------------------------------------------------------------
// VLTK Mobile — Performance Audit Checklist
// Runtime dev-build guardrails for mobile budgets.
// -----------------------------------------------------------------------------

using UnityEngine;
using UnityEngine.Profiling;
using Unity.Profiling;
using VLTK.Core;

namespace VLTK.Sandbox
{
    /// <summary>
    /// Checklist perf cho mobile. Chạy nhẹ trong development build để cảnh báo sớm
    /// các scene vượt ngân sách draw call / tam giác / RAM / GC.
    /// </summary>
    public static class PerfAuditChecklist
    {
        public const int TargetFps = 30;
        public const int MaxDrawCalls = 100;
        public const int MaxTriangles = 500_000;
        public const long MaxRuntimeMemoryMb = 200;
        public const long MaxGcAllocBytesPerFrame = 16 * 1024;

        // ProfilerRecorder.StartNew chạy ở static init sẽ grab native handle trước khi
        // Unity profiler backend sẵn sàng và không bao giờ Dispose. Khởi tạo lazy lúc audit
        // đầu tiên và reset khi Application quitting để giải phóng native handle.
        private static ProfilerRecorder _gcAllocRecorder;
        private static bool _gcAllocRecorderStarted;
        private static readonly object _gcAllocLock = new();

        private static ProfilerRecorder GetGcAllocRecorder()
        {
            if (_gcAllocRecorderStarted) return _gcAllocRecorder;
            lock (_gcAllocLock)
            {
                if (_gcAllocRecorderStarted) return _gcAllocRecorder;
                _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated Bytes");
                _gcAllocRecorderStarted = true;
                Application.quitting -= ResetGcAllocRecorder;
                Application.quitting += ResetGcAllocRecorder;
            }
            return _gcAllocRecorder;
        }

        public static void ResetGcAllocRecorder()
        {
            if (!_gcAllocRecorderStarted) return;
            lock (_gcAllocLock)
            {
                if (!_gcAllocRecorderStarted) return;
                if (_gcAllocRecorder.Valid) _gcAllocRecorder.Dispose();
                _gcAllocRecorderStarted = false;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoAudit()
        {
            if (!Debug.isDebugBuild) return;
            AuditMemory();
            AuditGcAlloc();
            SubsystemLog.Info("Perf", $"Budget mobile: {TargetFps}fps, <{MaxDrawCalls} draw calls, <{MaxTriangles:n0} tris, <{MaxRuntimeMemoryMb}MB RAM");
        }

        public static bool AuditDrawCalls(int drawCalls)
        {
            bool ok = drawCalls <= MaxDrawCalls;
            if (!ok) SubsystemLog.Warn("Perf", $"Draw calls {drawCalls} vượt budget {MaxDrawCalls}");
            return ok;
        }

        public static bool AuditTriangleCount(int triangles)
        {
            bool ok = triangles <= MaxTriangles;
            if (!ok) SubsystemLog.Warn("Perf", $"Triangles {triangles:n0} vượt budget {MaxTriangles:n0}");
            return ok;
        }

        public static bool AuditTextureMemory(long textureMemoryBytes)
        {
            long mb = textureMemoryBytes / (1024 * 1024);
            bool ok = mb <= MaxRuntimeMemoryMb;
            if (!ok) SubsystemLog.Warn("Perf", $"Texture memory {mb}MB vượt budget {MaxRuntimeMemoryMb}MB");
            return ok;
        }

        public static bool AuditGcAlloc(long gcAllocBytes)
        {
            bool ok = gcAllocBytes <= MaxGcAllocBytesPerFrame;
            if (!ok) SubsystemLog.Warn("Perf", $"GC alloc/frame {gcAllocBytes}B vượt budget {MaxGcAllocBytesPerFrame}B");
            return ok;
        }

        public static bool AuditMemory()
        {
            long totalMb = Profiler.GetTotalAllocatedMemoryLong() / (1024 * 1024);
            bool ok = totalMb <= MaxRuntimeMemoryMb;
            if (!ok) SubsystemLog.Warn("Perf", $"Runtime memory {totalMb}MB vượt budget {MaxRuntimeMemoryMb}MB");
            return ok;
        }

        public static bool AuditGcAlloc()
        {
            var rec = GetGcAllocRecorder();
            long gcAlloc = rec.Valid ? rec.LastValue : 0;
            return AuditGcAlloc(gcAlloc);
        }
    }
}
