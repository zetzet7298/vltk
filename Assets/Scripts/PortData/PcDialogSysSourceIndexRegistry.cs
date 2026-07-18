using System;
using System.Collections.Generic;

namespace VLTK.Sandbox
{
    [Serializable]
    public class PcDialogSysSourceIndexEntry
    {
        public int sourceIndex;
        public string sourceRoot;
        public string relativePath;
        public string fileStem;
        public string extension;
        public long sizeBytes;
        public string sha256;
        public int includeCount;
        public int functionCount;
        public int globalSymbolCount;
        public int optionSurfaceCount;
        public int saySurfaceCount;
        public string[] includes = Array.Empty<string>();
        public string[] functions = Array.Empty<string>();
        public string[] globalSymbols = Array.Empty<string>();
        public string[] representativeOptionSurfaces = Array.Empty<string>();
        public string[] representativeSaySurfaces = Array.Empty<string>();
    }

    public sealed class PcDialogSysSourceIndexRegistry
    {
        private readonly List<PcDialogSysSourceIndexEntry> _all = new List<PcDialogSysSourceIndexEntry>();
        private readonly Dictionary<string, PcDialogSysSourceIndexEntry> _byPath = new Dictionary<string, PcDialogSysSourceIndexEntry>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcDialogSysSourceIndexEntry>> _byFunction = new Dictionary<string, List<PcDialogSysSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<PcDialogSysSourceIndexEntry>> _bySurface = new Dictionary<string, List<PcDialogSysSourceIndexEntry>>(StringComparer.OrdinalIgnoreCase);

        public int Count => _all.Count;
        public int LuaFileCount { get; private set; }
        public int TotalFunctionCount { get; private set; }
        public int TotalGlobalSymbolCount { get; private set; }
        public int TotalOptionSurfaceCount { get; private set; }
        public int TotalSaySurfaceCount { get; private set; }
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcDialogSysSourceIndexEntry> All => _all;

        public void Register(PcDialogSysSourceIndexEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.relativePath) || _byPath.ContainsKey(e.relativePath)) return;
            _all.Add(e);
            _byPath[e.relativePath] = e;
            if (string.Equals(e.extension, "lua", StringComparison.OrdinalIgnoreCase)) LuaFileCount++;
            TotalFunctionCount += Math.Max(0, e.functionCount);
            TotalGlobalSymbolCount += Math.Max(0, e.globalSymbolCount);
            TotalOptionSurfaceCount += Math.Max(0, e.optionSurfaceCount);
            TotalSaySurfaceCount += Math.Max(0, e.saySurfaceCount);
            TotalSizeBytes += Math.Max(0L, e.sizeBytes);
            AddMany(_byFunction, e.functions, e);
            AddMany(_bySurface, e.representativeOptionSurfaces, e);
            AddMany(_bySurface, e.representativeSaySurfaces, e);
        }

        public PcDialogSysSourceIndexEntry GetByRelativePath(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetByFunction(string functionName)
            => _byFunction.TryGetValue(functionName ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcDialogSysSourceIndexEntry>)Array.Empty<PcDialogSysSourceIndexEntry>();

        public IReadOnlyList<PcDialogSysSourceIndexEntry> GetBySurface(string surface)
            => _bySurface.TryGetValue(surface ?? string.Empty, out var entries) ? entries : (IReadOnlyList<PcDialogSysSourceIndexEntry>)Array.Empty<PcDialogSysSourceIndexEntry>();

        private static void AddMany(Dictionary<string, List<PcDialogSysSourceIndexEntry>> map, string[] keys, PcDialogSysSourceIndexEntry entry)
        {
            if (keys == null) return;
            foreach (var key in keys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (!map.TryGetValue(key, out var list)) map[key] = list = new List<PcDialogSysSourceIndexEntry>();
                list.Add(entry);
            }
        }
    }
}
