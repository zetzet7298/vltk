using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace VLTK.Sandbox
{
    public sealed class PcFlipCardProtocolRegistry
    {
        private readonly Dictionary<string, PcFlipCardProtocolEntry> _byKey = new Dictionary<string, PcFlipCardProtocolEntry>();
        public int Count => _byKey.Count;
        public IEnumerable<PcFlipCardProtocolEntry> All => _byKey.Values;
        public void Register(PcFlipCardProtocolEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.key)) return;
            _byKey[entry.key] = entry;
        }
        public PcFlipCardProtocolEntry Get(string key) => key != null && _byKey.TryGetValue(key, out var value) ? value : null;
        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            var entry = Get(key);
            return entry != null && int.TryParse(entry.valueRaw, out value);
        }
    }

    public sealed class FlipCardProtocolService
    {
        public const string DefaultStreamingDir = "Reference/PcFlipCard";
        private readonly PcFlipCardProtocolRegistry _registry;
        public int Count => _registry != null ? _registry.Count : 0;
        public FlipCardProtocolService(PcFlipCardProtocolRegistry registry) { _registry = registry ?? new PcFlipCardProtocolRegistry(); }
        public PcFlipCardProtocolEntry Get(string key) => _registry.Get(key);
        public bool TryGetInt(string key, out int value) => _registry.TryGetInt(key, out value);
        public static FlipCardProtocolService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            return new FlipCardProtocolService(PcFlipCardProtocolParser.BuildRegistry(dir));
        }
    }
}
