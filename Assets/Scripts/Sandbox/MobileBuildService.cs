// -----------------------------------------------------------------------------
// VLTK Mobile — ST-06.2 Mobile Build, Asset Pipeline & Performance
// Build config, asset bundle pipeline, performance checkpoints & budgets.
// PC source: Mobile build constraints, memory budgets, draw call limits.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    [Serializable]
    public class BuildTargetConfig
    {
        public string platform;           // "android" / "ios"
        public int targetSdkVersion;
        public int minSdkVersion;
        public string architecture;       // "arm64-v8a" / "universal"
        public long maxApkSizeMb = 200;
        public int targetFps = 30;
        public bool developmentBuild;
    }

    [Serializable]
    public class PerformanceBudget
    {
        public int maxDrawCalls = 150;
        public int maxTriangles = 80000;
        public int maxVertices = 60000;
        public long maxMemoryMb = 512;
        public float maxLoadTimeSeconds = 5f;
        public int maxSpritesOnScreen = 200;
        public int maxEnemiesActive = 30;
    }

    [Serializable]
    public class AssetPipelineConfig
    {
        public int spriteAtlasMaxSize = 2048;
        public string textureCompression = "ASTC 6x6";   // Mobile default
        public int audioSampleRate = 22050;               // Giảm cho mobile
        public string audioFormat = "Vorbis";
        public bool stripUnusedAssets = true;
        public bool bundleAssets = true;
    }

    /// <summary>
    /// Service quản lý build config, asset pipeline và performance budgets cho mobile.
    /// </summary>
    public class MobileBuildService
    {
        private BuildTargetConfig _buildConfig;
        private PerformanceBudget _perfBudget;
        private AssetPipelineConfig _pipelineConfig;
        private readonly Dictionary<string, float> _checkpoints = new();

        public BuildTargetConfig BuildConfig => _buildConfig;
        public PerformanceBudget PerfBudget => _perfBudget;
        public AssetPipelineConfig PipelineConfig => _pipelineConfig;

        public MobileBuildService()
        {
            _buildConfig = new BuildTargetConfig
            {
                platform = "android",
                targetSdkVersion = 33,
                minSdkVersion = 24,
                architecture = "arm64-v8a",
                targetFps = 30,
            };
            _perfBudget = new PerformanceBudget();
            _pipelineConfig = new AssetPipelineConfig();
        }

        // ── Performance Checkpoints ────────────────────────────────────────

        /// <summary>Ghi nhận thời gian thực hiện một bước.</summary>
        public void RecordCheckpoint(string name, float elapsedSeconds)
        {
            _checkpoints[name] = elapsedSeconds;
            if (elapsedSeconds > _perfBudget.maxLoadTimeSeconds)
                SubsystemLog.Warn("Perf", $"Checkpoint '{name}' vượt budget: {elapsedSeconds:F2}s > {_perfBudget.maxLoadTimeSeconds}s");
        }

        /// <summary>Kiểm tra performance budget hiện tại.</summary>
        public bool ValidateBudget(int drawCalls, int triangles, long memoryMb)
        {
            bool ok = true;
            if (drawCalls > _perfBudget.maxDrawCalls)
            {
                SubsystemLog.Warn("Perf", $"Draw calls {drawCalls} > budget {_perfBudget.maxDrawCalls}");
                ok = false;
            }
            if (triangles > _perfBudget.maxTriangles)
            {
                SubsystemLog.Warn("Perf", $"Triangles {triangles} > budget {_perfBudget.maxTriangles}");
                ok = false;
            }
            if (memoryMb > _perfBudget.maxMemoryMb)
            {
                SubsystemLog.Warn("Perf", $"Memory {memoryMb}MB > budget {_perfBudget.maxMemoryMb}MB");
                ok = false;
            }
            return ok;
        }

        /// <summary>Lấy tất cả checkpoints.</summary>
        public IReadOnlyDictionary<string, float> GetCheckpoints() => _checkpoints;

        // ── Asset Pipeline ─────────────────────────────────────────────────

        /// <summary>Ước tính kích thước bundle.</summary>
        public long EstimateBundleSize(int spriteCount, int audioCount, int mapCount)
        {
            // Sprite: ~4KB each compressed, Audio: ~100KB each, Map: ~500KB each
            long sprites = spriteCount * 4L * 1024;
            long audio = audioCount * 100L * 1024;
            long maps = mapCount * 500L * 1024;
            return (sprites + audio + maps) / (1024 * 1024); // MB
        }

        /// <summary>Khuyến nghị cài đặt pipeline dựa trên target device.</summary>
        public string GetPipelineRecommendation(long availableMemoryMb)
        {
            if (availableMemoryMb < 2048)
                return "Thiết bị thấp cấp: Giảm texture xuống ASTC 8x8, giới hạn 20 enemies, FPS 24";
            if (availableMemoryMb < 4096)
                return "Thiết bị trung bình: ASTC 6x6, 30 enemies, FPS 30";
            return "Thiết bị cao cấp: ASTC 4x4, 50 enemies, FPS 60";
        }
    }
}
