// -----------------------------------------------------------------------------
// VLTK Mobile — PC global script source index service.
// Catalog only: exposes script/global path evidence; it does not execute Lua.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class GlobalScriptSourceIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcGlobalScript";
        public const string StreamingAssetsRelativePath = "Reference/PcGlobalScript/global_script_index.txt";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser/server1/script/global";

        private readonly PcGlobalScriptSourceIndexRegistry _registry;

        public GlobalScriptSourceIndexService()
            : this(new PcGlobalScriptSourceIndexRegistry())
        {
        }

        public GlobalScriptSourceIndexService(PcGlobalScriptSourceIndexRegistry registry)
        {
            _registry = registry ?? new PcGlobalScriptSourceIndexRegistry();
        }

        public int Count => _registry.Count;
        public int FileCount => _registry.FileCount;
        public int DirectoryCount => _registry.DirectoryCount;
        public int LuaFileCount => _registry.LuaFileCount;
        public int NonLuaFileCount => _registry.NonLuaFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcGlobalScriptSourceIndexEntry> All => _registry.All;

        public PcGlobalScriptSourceIndexEntry GetByRelativePath(string relativePath)
            => _registry.GetByRelativePath(relativePath);

        public IReadOnlyList<PcGlobalScriptSourceIndexEntry> GetByDirectory(string directory)
            => _registry.GetByDirectory(directory);

        public static GlobalScriptSourceIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static GlobalScriptSourceIndexService LoadFromDirectory(string dir)
            => new GlobalScriptSourceIndexService(PcGlobalScriptSourceIndexParser.BuildRegistry(dir));

        public static GlobalScriptSourceIndexService LoadFromFile(string path)
        {
            var reg = new PcGlobalScriptSourceIndexRegistry();
            foreach (var entry in PcGlobalScriptSourceIndexParser.ParseFile(path)) reg.Register(entry);
            return new GlobalScriptSourceIndexService(reg);
        }
    }
}
