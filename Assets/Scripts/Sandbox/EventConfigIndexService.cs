// -----------------------------------------------------------------------------
// VLTK Mobile — PC event config source index service.
// Loads file/schema evidence for PC Client/Server settings/event directories.
// Catalog only: no event runtime, scheduling, Lua, or reward behavior is claimed.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class EventConfigIndexService
    {
        public const string StreamingAssetsRelativeDirectory = "Reference/PcEventConfig";
        public const string StreamingAssetsRelativePath = "Reference/PcEventConfig/event_config_source_index.txt";
        public const string ClientRootId = "client_settings_event";
        public const string ServerJxserRootId = "server_jxser_settings_event";
        public const string ServerBachKimRootId = "server_bachkim_settings_event";

        private readonly PcEventConfigIndexRegistry _registry;

        public EventConfigIndexService()
            : this(new PcEventConfigIndexRegistry())
        {
        }

        public EventConfigIndexService(PcEventConfigIndexRegistry registry)
        {
            _registry = registry ?? new PcEventConfigIndexRegistry();
        }

        public int Count => _registry.Count;
        public int ClientFileCount => _registry.ClientFileCount;
        public int ServerFileCount => _registry.ServerFileCount;
        public int TextFileCount => _registry.TextFileCount;
        public long TotalSizeBytes => _registry.TotalSizeBytes;
        public IReadOnlyList<PcEventConfigIndexEntry> All => _registry.All;

        public PcEventConfigIndexEntry GetByRootPath(string rootId, string relativePath)
            => _registry.GetByRootPath(rootId, relativePath);

        public IReadOnlyList<PcEventConfigIndexEntry> GetByRoot(string rootId)
            => _registry.GetByRoot(rootId);

        public IReadOnlyList<PcEventConfigIndexEntry> GetBySide(string side)
            => _registry.GetBySide(side);

        public static EventConfigIndexService LoadFromStreamingAssets()
        {
            var dir = Path.Combine(Application.streamingAssetsPath, StreamingAssetsRelativeDirectory);
            return LoadFromDirectory(dir);
        }

        public static EventConfigIndexService LoadFromDirectory(string dir)
            => new EventConfigIndexService(PcEventConfigIndexParser.BuildRegistry(dir));

        public static EventConfigIndexService LoadFromFile(string path)
        {
            var reg = new PcEventConfigIndexRegistry();
            foreach (var entry in PcEventConfigIndexParser.ParseFile(path)) reg.Register(entry);
            return new EventConfigIndexService(reg);
        }
    }
}
