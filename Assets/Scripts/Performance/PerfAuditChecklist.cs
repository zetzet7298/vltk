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

        private static readonly ProfilerRecorder _gcAllocRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated Bytes");

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
            long gcAlloc = _gcAllocRecorder.Valid ? _gcAllocRecorder.LastValue : 0;
            return AuditGcAlloc(gcAlloc);
        }
    }
}
