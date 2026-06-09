// -----------------------------------------------------------------------------
// VLTK Mobile — PC activitysys source/config index service.
// Catalog only: exposes file evidence from activitysys_source_index.txt and
// activitysys_config_index.txt. Runtime activity behavior remains unported here.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class ActivitySysIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcActivitySys";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser/server1/script/activitysys";
        public const string PcConfigRoot = "Server 6.0/server/home_jxser/server1/settings/activitysys";

        private readonly PcActivitySysIndexRegistry _registry;

        public ActivitySysIndexService()
            : this(new PcActivitySysIndexRegistry())
        {
        }

        public ActivitySysIndexService(PcActivitySysIndexRegistry registry)
        {
            _registry = registry ?? new PcActivitySysIndexRegistry();
        }

        public int Count => _registry.Count;
        public int SourceFileCount => _registry.SourceFileCount;
        public int ConfigFileCount => _registry.ConfigFileCount;
        public int LuaFileCount => _registry.LuaFileCount;
        public int TextConfigFileCount => _registry.TextConfigFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcActivitySysIndexEntry> All => _registry.All;

        public PcActivitySysIndexEntry GetSource(string relativePath)
            => _registry.GetByRelativePath("source", relativePath);

        public PcActivitySysIndexEntry GetConfig(string relativePath)
            => _registry.GetByRelativePath("config", relativePath);

        public IReadOnlyList<PcActivitySysIndexEntry> GetSourceDirectory(string directory)
            => _registry.GetByDirectory("source", directory);

        public IReadOnlyList<PcActivitySysIndexEntry> GetConfigDirectory(string directory)
            => _registry.GetByDirectory("config", directory);

        public static ActivitySysIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static ActivitySysIndexService LoadFromDirectory(string dir)
            => new ActivitySysIndexService(PcActivitySysIndexParser.BuildRegistry(dir));
    }
}
