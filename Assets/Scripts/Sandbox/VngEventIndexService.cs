// -----------------------------------------------------------------------------
// VLTK Mobile — PC VNG event source index service.
// Catalog only: loads script/vng_event file evidence from vng_event_index.txt
// and exposes counts/lookups. Runtime Lua execution remains a separate port.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class VngEventIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcVngEvent";
        public const string StreamingAssetsRelativePath = "Reference/PcVngEvent/vng_event_index.txt";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser/server1/script/vng_event";

        private readonly PcVngEventIndexRegistry _registry;

        public VngEventIndexService()
            : this(new PcVngEventIndexRegistry())
        {
        }

        public VngEventIndexService(PcVngEventIndexRegistry registry)
        {
            _registry = registry ?? new PcVngEventIndexRegistry();
        }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int NonLuaFileCount => _registry.NonLuaFileCount;
        public int LuaDirectoryCount => _registry.LuaDirectoryCount;
        public int SourceDirectoryCount => _registry.SourceDirectoryCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcVngEventIndexEntry> All => _registry.All;

        public PcVngEventIndexEntry GetByRelativePath(string relativePath)
            => _registry.GetByRelativePath(relativePath);

        public IReadOnlyList<PcVngEventIndexEntry> GetByDirectory(string directory)
            => _registry.GetByDirectory(directory);

        public static VngEventIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static VngEventIndexService LoadFromDirectory(string dir)
            => new VngEventIndexService(PcVngEventIndexParser.BuildRegistry(dir));

        public static VngEventIndexService LoadFromFile(string path)
        {
            var reg = new PcVngEventIndexRegistry();
            foreach (var entry in PcVngEventIndexParser.ParseFile(path)) reg.Register(entry);
            return new VngEventIndexService(reg);
        }
    }
}
