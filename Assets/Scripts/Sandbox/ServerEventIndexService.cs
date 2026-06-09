// -----------------------------------------------------------------------------
// VLTK Mobile — PC server event source index service.
// Catalog only: loads script/event file evidence from server_event_index.txt and
// exposes counts/lookups. Runtime Lua execution remains a separate port.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class ServerEventIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcServerEvent";
        public const string StreamingAssetsRelativePath = "Reference/PcServerEvent/server_event_index.txt";
        public const string PcSourceRoot = "Server 6.0/server/home_jxser/server1/script/event";

        private readonly PcServerEventIndexRegistry _registry;

        public ServerEventIndexService()
            : this(new PcServerEventIndexRegistry())
        {
        }

        public ServerEventIndexService(PcServerEventIndexRegistry registry)
        {
            _registry = registry ?? new PcServerEventIndexRegistry();
        }

        public int Count => _registry.Count;
        public int LuaFileCount => _registry.LuaFileCount;
        public int NonLuaFileCount => _registry.NonLuaFileCount;
        public int CvsMetadataFileCount => _registry.CvsMetadataFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcServerEventIndexEntry> All => _registry.All;

        public PcServerEventIndexEntry GetByRelativePath(string relativePath)
            => _registry.GetByRelativePath(relativePath);

        public IReadOnlyList<PcServerEventIndexEntry> GetByDirectory(string directory)
            => _registry.GetByDirectory(directory);

        public static ServerEventIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static ServerEventIndexService LoadFromDirectory(string dir)
            => new ServerEventIndexService(PcServerEventIndexParser.BuildRegistry(dir));

        public static ServerEventIndexService LoadFromFile(string path)
        {
            var reg = new PcServerEventIndexRegistry();
            foreach (var entry in PcServerEventIndexParser.ParseFile(path)) reg.Register(entry);
            return new ServerEventIndexService(reg);
        }
    }
}
