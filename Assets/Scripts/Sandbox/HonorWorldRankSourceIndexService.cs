// -----------------------------------------------------------------------------
// VLTK Mobile — PC honor/worldrank source index service.
// Catalog only: exposes PC source file counts/lookups from honor_worldrank_source_index.txt.
// Runtime honor/worldrank behavior remains a separate port.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class HonorWorldRankSourceIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcHonorWorldRank";
        public const string StreamingAssetsRelativePath = "Reference/PcHonorWorldRank/honor_worldrank_source_index.txt";
        public const string HonorPcSourceRoot = "Server 6.0/server/home_jxser/server1/script/honor";
        public const string WorldRankPcSourceRoot = "Server 6.0/server/home_jxser/server1/script/global/worldrank";
        public const string ServerRankSettingPcSourceRoot = "Server 6.0/server/home_jxser/server1/settings";
        public const string ClientRankSettingPcSourceRoot = "Client 6.0/settings";

        private readonly PcHonorWorldRankSourceIndexRegistry _registry;

        public HonorWorldRankSourceIndexService()
            : this(new PcHonorWorldRankSourceIndexRegistry())
        {
        }

        public HonorWorldRankSourceIndexService(PcHonorWorldRankSourceIndexRegistry registry)
        {
            _registry = registry ?? new PcHonorWorldRankSourceIndexRegistry();
        }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int HonorLuaFileCount => _registry.HonorLuaFileCount;
        public int WorldRankLuaFileCount => _registry.WorldRankLuaFileCount;
        public int SettingsFileCount => _registry.SettingsFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> All => _registry.All;

        public PcHonorWorldRankSourceIndexEntry GetBySourcePath(string sourceRoot, string relativePath)
            => _registry.GetBySourcePath(sourceRoot, relativePath);

        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> GetByCategory(string category)
            => _registry.GetByCategory(category);

        public IReadOnlyList<PcHonorWorldRankSourceIndexEntry> GetBySourceRoot(string sourceRoot)
            => _registry.GetBySourceRoot(sourceRoot);

        public static HonorWorldRankSourceIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static HonorWorldRankSourceIndexService LoadFromDirectory(string dir)
            => new HonorWorldRankSourceIndexService(PcHonorWorldRankSourceIndexParser.BuildRegistry(dir));

        public static HonorWorldRankSourceIndexService LoadFromFile(string path)
        {
            var reg = new PcHonorWorldRankSourceIndexRegistry();
            foreach (var entry in PcHonorWorldRankSourceIndexParser.ParseFile(path)) reg.Register(entry);
            return new HonorWorldRankSourceIndexService(reg);
        }
    }
}
