// -----------------------------------------------------------------------------
// VLTK Mobile — PC shop-related Lua source index service.
// Data-only wrapper for Reference/PcShopScript; it does not execute Lua or claim
// mobile shop runtime support.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class ShopScriptIndexService
    {
        public const string DefaultStreamingDir = "Reference/PcShopScript";
        private readonly PcShopScriptIndexRegistry _registry;

        public ShopScriptIndexService() : this(new PcShopScriptIndexRegistry()) { }
        public ShopScriptIndexService(PcShopScriptIndexRegistry registry) { _registry = registry ?? new PcShopScriptIndexRegistry(); }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int UniqueRelativePathCount => _registry.UniqueRelativePathCount;
        public int DuplicateRelativePathCount => _registry.DuplicateRelativePathCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcShopScriptIndexEntry> All => _registry.All;

        public PcShopScriptIndexEntry GetBySourceAndRelativePath(string sourceRoot, string relativePath)
            => _registry.GetBySourceAndRelativePath(sourceRoot, relativePath);
        public IReadOnlyList<PcShopScriptIndexEntry> GetByRelativePath(string relativePath) => _registry.GetByRelativePath(relativePath);
        public IReadOnlyList<PcShopScriptIndexEntry> GetBySourceSubdir(string sourceSubdir) => _registry.GetBySourceSubdir(sourceSubdir);
        public int GetSourceSubdirCount(string sourceSubdir) => _registry.GetBySourceSubdir(sourceSubdir).Count;

        public static ShopScriptIndexService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
            => LoadFromDirectory(Path.Combine(Application.streamingAssetsPath, subdir));
        public static ShopScriptIndexService LoadFromDirectory(string dir) => new ShopScriptIndexService(PcShopScriptIndexParser.BuildRegistry(dir));
        public static ShopScriptIndexService LoadFromFile(string path) => new ShopScriptIndexService(PcShopScriptIndexParser.BuildRegistry(path));
    }

    public sealed class PcShopScriptIndexRegistry
    {
        private readonly List<PcShopScriptIndexEntry> _all = new List<PcShopScriptIndexEntry>();
        private readonly Dictionary<string, PcShopScriptIndexEntry> _bySourceAndPath = new Dictionary<string, PcShopScriptIndexEntry>();
        private readonly Dictionary<string, List<PcShopScriptIndexEntry>> _byPath = new Dictionary<string, List<PcShopScriptIndexEntry>>();
        private readonly Dictionary<string, List<PcShopScriptIndexEntry>> _bySourceSubdir = new Dictionary<string, List<PcShopScriptIndexEntry>>();

        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int UniqueRelativePathCount => _byPath.Count;
        public int DuplicateRelativePathCount => Count - UniqueRelativePathCount;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcShopScriptIndexEntry> All => _all;

        public void Register(PcShopScriptIndexEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.sourceRoot) || string.IsNullOrEmpty(entry.relativePath)) return;
            var key = entry.sourceRoot + "\n" + entry.relativePath;
            if (_bySourceAndPath.ContainsKey(key)) return;
            _all.Add(entry);
            _bySourceAndPath[key] = entry;
            if (entry.isLua) LuaFileCount++;
            TotalSizeBytes += entry.sizeBytes;
            Add(_byPath, entry.relativePath, entry);
            Add(_bySourceSubdir, entry.sourceSubdir, entry);
        }

        public PcShopScriptIndexEntry GetBySourceAndRelativePath(string sourceRoot, string relativePath)
        {
            if (string.IsNullOrEmpty(sourceRoot) || string.IsNullOrEmpty(relativePath)) return null;
            return _bySourceAndPath.TryGetValue(sourceRoot + "\n" + relativePath, out var entry) ? entry : null;
        }

        public IReadOnlyList<PcShopScriptIndexEntry> GetByRelativePath(string relativePath) => Get(_byPath, relativePath);
        public IReadOnlyList<PcShopScriptIndexEntry> GetBySourceSubdir(string sourceSubdir) => Get(_bySourceSubdir, sourceSubdir);
        private static void Add(Dictionary<string, List<PcShopScriptIndexEntry>> map, string key, PcShopScriptIndexEntry entry)
        {
            key = string.IsNullOrEmpty(key) ? "." : key;
            if (!map.TryGetValue(key, out var list)) map[key] = list = new List<PcShopScriptIndexEntry>();
            list.Add(entry);
        }
        private static IReadOnlyList<PcShopScriptIndexEntry> Get(Dictionary<string, List<PcShopScriptIndexEntry>> map, string key)
        {
            key = string.IsNullOrEmpty(key) ? "." : key;
            return map.TryGetValue(key, out var list) ? (IReadOnlyList<PcShopScriptIndexEntry>)list : System.Array.Empty<PcShopScriptIndexEntry>();
        }
    }
}
