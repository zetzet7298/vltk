// -----------------------------------------------------------------------------
// VLTK Mobile — PC HuoYueDu source/config index service.
// Data-only evidence wrapper. It exposes PC file/config row counts and hashes;
// it does not execute script/huoyuedu Lua or claim activity-points gameplay.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class HuoYueDuIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcHuoYueDu";
        public const string PcScriptSourceRoot = "Server 6.0/server/home_jxser/server1/script/huoyuedu";
        public const string PcConfigSourceRoot = "Server 6.0/server/home_jxser/server1/settings/huoyuedu";

        private readonly PcHuoYueDuIndexRegistry _registry;

        public HuoYueDuIndexService() : this(new PcHuoYueDuIndexRegistry()) { }
        public HuoYueDuIndexService(PcHuoYueDuIndexRegistry registry)
        {
            _registry = registry ?? new PcHuoYueDuIndexRegistry();
        }

        public int FileCount => _registry.FileCount;
        public int SourceFileCount => _registry.SourceFileCount;
        public int ConfigFileCount => _registry.ConfigFileCount;
        public int LuaFileCount => _registry.LuaFileCount;
        public int ActivityRowCount => _registry.ActivityRowCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcHuoYueDuFileIndexEntry> Files => _registry.Files;
        public IReadOnlyList<PcHuoYueDuActivityEntry> Activities => _registry.Activities;

        public PcHuoYueDuFileIndexEntry GetSourceFile(string relativePath) => _registry.GetSourceFile(relativePath);
        public PcHuoYueDuFileIndexEntry GetConfigFile(string relativePath) => _registry.GetConfigFile(relativePath);
        public PcHuoYueDuActivityEntry GetActivity(int activityId) => _registry.GetActivity(activityId);

        public static HuoYueDuIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static HuoYueDuIndexService LoadFromDirectory(string dir)
            => new HuoYueDuIndexService(PcHuoYueDuParser.BuildIndexRegistry(dir));
    }
}
