using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class PcGlobalScriptSourceIndexEntry
    {
        public int sourceIndex;
        public string entryKind;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public long sizeBytes;
        public string sha256;

        public bool IsFile => string.Equals(entryKind, "file", StringComparison.OrdinalIgnoreCase);
        public bool IsDirectory => string.Equals(entryKind, "directory", StringComparison.OrdinalIgnoreCase);
    }

    public sealed class PcGlobalScriptSourceIndexRegistry
    {
        private readonly List<PcGlobalScriptSourceIndexEntry> _all = new List<PcGlobalScriptSourceIndexEntry>();
        private readonly Dictionary<string, PcGlobalScriptSourceIndexEntry> _byPath =
            new Dictionary<string, PcGlobalScriptSourceIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcGlobalScriptSourceIndexEntry>> _byDirectory =
            new Dictionary<string, List<PcGlobalScriptSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int FileCount { get; private set; }
        public int DirectoryCount { get; private set; }
        public int LuaFileCount { get; private set; }
        public int NonLuaFileCount => FileCount - LuaFileCount;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcGlobalScriptSourceIndexEntry> All => _all;

        public void Register(PcGlobalScriptSourceIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath) || _byPath.ContainsKey(e.relativePath)) return;
            _all.Add(e);
            _byPath[e.relativePath] = e;
            if (e.IsDirectory) DirectoryCount++;
            if (e.IsFile)
            {
                FileCount++;
                if (e.isLua) LuaFileCount++;
                TotalSizeBytes += Math.Max(0L, e.sizeBytes);
            }
            var dir = e.directory ?? string.Empty;
            if (!_byDirectory.TryGetValue(dir, out var entries))
            {
                entries = new List<PcGlobalScriptSourceIndexEntry>();
                _byDirectory[dir] = entries;
            }
            entries.Add(e);
        }

        public PcGlobalScriptSourceIndexEntry GetByRelativePath(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcGlobalScriptSourceIndexEntry> GetByDirectory(string directory)
            => _byDirectory.TryGetValue(directory ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcGlobalScriptSourceIndexEntry>)Array.Empty<PcGlobalScriptSourceIndexEntry>();
    }
}
