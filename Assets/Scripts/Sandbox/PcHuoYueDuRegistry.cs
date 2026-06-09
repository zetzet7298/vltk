using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public sealed class PcHuoYueDuFileIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string directory;
        public string fileName;
        public string extension;
        public bool isLua;
        public bool isConfig;
        public long sizeBytes;
        public int lineCount;
        public int dataRows;
        public string sha256;
        public string headerColumns;
    }

    [Serializable]
    public sealed class PcHuoYueDuActivityEntry
    {
        public int activityId;
        public string activityName;
        public int countTask;
        public int maxCount;
        public int[] parameters;
        public int weekResetFlag;
    }

    public sealed class PcHuoYueDuIndexRegistry
    {
        private readonly List<PcHuoYueDuFileIndexEntry> _files = new List<PcHuoYueDuFileIndexEntry>();
        private readonly List<PcHuoYueDuActivityEntry> _activities = new List<PcHuoYueDuActivityEntry>();
        private readonly Dictionary<string, PcHuoYueDuFileIndexEntry> _byPath = new Dictionary<string, PcHuoYueDuFileIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, PcHuoYueDuActivityEntry> _activityById = new Dictionary<int, PcHuoYueDuActivityEntry>();

        public int FileCount => _files.Count;
        public int SourceFileCount { get; private set; }
        public int ConfigFileCount { get; private set; }
        public int LuaFileCount { get; private set; }
        public int ActivityRowCount => _activities.Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcHuoYueDuFileIndexEntry> Files => _files;
        public IReadOnlyList<PcHuoYueDuActivityEntry> Activities => _activities;

        public void RegisterFile(PcHuoYueDuFileIndexEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.relativePath)) return;
            var key = entry.isConfig ? $"config:{entry.relativePath}" : $"source:{entry.relativePath}";
            if (_byPath.ContainsKey(key)) return;

            _files.Add(entry);
            _byPath[key] = entry;
            if (entry.isConfig) ConfigFileCount++; else SourceFileCount++;
            if (entry.isLua) LuaFileCount++;
            TotalSizeBytes += Math.Max(0L, entry.sizeBytes);
        }

        public void RegisterActivity(PcHuoYueDuActivityEntry entry)
        {
            if (entry == null || entry.activityId <= 0) return;
            if (_activityById.ContainsKey(entry.activityId)) return;
            _activities.Add(entry);
            _activityById[entry.activityId] = entry;
        }

        public PcHuoYueDuFileIndexEntry GetSourceFile(string relativePath) => GetFile(relativePath, false);
        public PcHuoYueDuFileIndexEntry GetConfigFile(string relativePath) => GetFile(relativePath, true);
        public PcHuoYueDuActivityEntry GetActivity(int activityId)
            => _activityById.TryGetValue(activityId, out var entry) ? entry : null;

        private PcHuoYueDuFileIndexEntry GetFile(string relativePath, bool isConfig)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;
            var key = isConfig ? $"config:{relativePath}" : $"source:{relativePath}";
            return _byPath.TryGetValue(key, out var entry) ? entry : null;
        }
    }
}
