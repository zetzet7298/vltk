using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcPartnerPetSourceIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public string category;
        public bool isConfig;
        public bool isLua;
        public long sizeBytes;
        public string sha256;
    }

    public sealed class PcPartnerPetSourceIndexRegistry
    {
        private readonly List<PcPartnerPetSourceIndexEntry> _all = new List<PcPartnerPetSourceIndexEntry>();
        private readonly Dictionary<string, PcPartnerPetSourceIndexEntry> _byRootPath = new Dictionary<string, PcPartnerPetSourceIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcPartnerPetSourceIndexEntry>> _byCategory = new Dictionary<string, List<PcPartnerPetSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcPartnerPetSourceIndexEntry>> _bySourceRoot = new Dictionary<string, List<PcPartnerPetSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcPartnerPetSourceIndexEntry>> _byFileName = new Dictionary<string, List<PcPartnerPetSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int ConfigFileCount => _all.FindAll(e => e.isConfig).Count;
        public int LuaFileCount => _all.FindAll(e => e.isLua).Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcPartnerPetSourceIndexEntry> All => _all;

        public void Register(PcPartnerPetSourceIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath)) return;
            _all.Add(e);
            TotalSizeBytes += e.sizeBytes;
            _byRootPath[MakeKey(e.sourceRoot, e.relativePath)] = e;
            Add(_byCategory, e.category ?? string.Empty, e);
            Add(_bySourceRoot, e.sourceRoot ?? string.Empty, e);
            Add(_byFileName, e.fileName ?? string.Empty, e);
        }

        public PcPartnerPetSourceIndexEntry GetBySourceRootPath(string sourceRoot, string relativePath)
            => _byRootPath.TryGetValue(MakeKey(sourceRoot, relativePath), out var entry) ? entry : null;

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetByCategory(string category)
            => GetMany(_byCategory, category);

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetBySourceRoot(string sourceRoot)
            => GetMany(_bySourceRoot, sourceRoot);

        public IReadOnlyList<PcPartnerPetSourceIndexEntry> GetByFileName(string fileName)
            => GetMany(_byFileName, fileName);

        private static string MakeKey(string sourceRoot, string relativePath)
            => (sourceRoot ?? string.Empty) + "\n" + (relativePath ?? string.Empty);

        private static void Add(Dictionary<string, List<PcPartnerPetSourceIndexEntry>> map, string key, PcPartnerPetSourceIndexEntry e)
        {
            if (!map.TryGetValue(key, out var entries)) map[key] = entries = new List<PcPartnerPetSourceIndexEntry>();
            entries.Add(e);
        }

        private static IReadOnlyList<PcPartnerPetSourceIndexEntry> GetMany(Dictionary<string, List<PcPartnerPetSourceIndexEntry>> map, string key)
            => map.TryGetValue(key ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcPartnerPetSourceIndexEntry>)Array.Empty<PcPartnerPetSourceIndexEntry>();
    }
}
