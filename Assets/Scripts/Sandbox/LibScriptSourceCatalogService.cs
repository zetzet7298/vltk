// -----------------------------------------------------------------------------
// VLTK Mobile — PC Server script/lib source catalog service.
// Data-only wrapper for script/lib file paths, sizes, and hashes; it does not execute Lua.
// -----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class LibScriptSourceCatalogService
    {
        public const string LogTag = "LibScriptSourceCatalog";
        public const string DefaultStreamingDir = "Reference/PcLibScript";
        public const string PcSourceRelativeRoot = PcLibScriptSourceParser.PcSourceRelativeRoot;
        public const string NoLuaRuntimeClaim = PcLibScriptSourceParser.NoLuaRuntimeClaim;

        private readonly PcLibScriptSourceCatalog _catalog;

        public int Count => _catalog != null ? _catalog.Count : 0;
        public int LuaCount => _catalog != null ? _catalog.LuaCount : 0;
        public int DirectoryCount => _catalog != null ? _catalog.DirectoryCount : 0;
        public long TotalSizeBytes => _catalog != null ? _catalog.TotalSizeBytes : 0L;
        public IReadOnlyList<PcLibScriptSourceEntry> All
            => _catalog != null ? _catalog.All : (IReadOnlyList<PcLibScriptSourceEntry>)Array.Empty<PcLibScriptSourceEntry>();

        public LibScriptSourceCatalogService(PcLibScriptSourceCatalog catalog)
        {
            _catalog = catalog ?? new PcLibScriptSourceCatalog();
        }

        public PcLibScriptSourceEntry GetByRelativePath(string relativePath) => _catalog.Get(relativePath);
        public IReadOnlyList<PcLibScriptSourceEntry> GetByDirectory(string directory) => _catalog.GetByDirectory(directory);
        public int GetDirectoryCount(string directory) => _catalog.GetDirectoryCount(directory);

        public static LibScriptSourceCatalogService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcLibScriptSourceParser.BuildCatalog(dir);
            if (catalog.Count > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC lib script source catalog: {catalog.Count} files, {catalog.LuaCount} Lua");
            else
                SubsystemLog.Warn(LogTag, $"PC lib script source catalog missing at {dir}");
            return new LibScriptSourceCatalogService(catalog);
        }
    }

    public sealed class PcLibScriptSourceCatalog
    {
        private readonly List<PcLibScriptSourceEntry> _all = new List<PcLibScriptSourceEntry>();
        private readonly Dictionary<string, PcLibScriptSourceEntry> _byPath = new Dictionary<string, PcLibScriptSourceEntry>();
        private readonly Dictionary<string, List<PcLibScriptSourceEntry>> _byDirectory = new Dictionary<string, List<PcLibScriptSourceEntry>>();

        public int Count => _all.Count;
        public int LuaCount { get; private set; }
        public int DirectoryCount => _byDirectory.Count;
        public long TotalSizeBytes { get; private set; }
        public IReadOnlyList<PcLibScriptSourceEntry> All => _all;

        public void Register(PcLibScriptSourceEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.relativePath)) return;
            if (_byPath.ContainsKey(entry.relativePath)) return;

            _all.Add(entry);
            _byPath[entry.relativePath] = entry;
            TotalSizeBytes += Math.Max(0L, entry.sizeBytes);
            if (entry.isLua) LuaCount++;

            var dir = string.IsNullOrEmpty(entry.directory) ? "." : entry.directory;
            if (!_byDirectory.TryGetValue(dir, out var list))
            {
                list = new List<PcLibScriptSourceEntry>();
                _byDirectory[dir] = list;
            }
            list.Add(entry);
        }

        public PcLibScriptSourceEntry Get(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcLibScriptSourceEntry> GetByDirectory(string directory)
        {
            var key = string.IsNullOrEmpty(directory) ? "." : directory;
            return _byDirectory.TryGetValue(key, out var list)
                ? (IReadOnlyList<PcLibScriptSourceEntry>)list
                : Array.Empty<PcLibScriptSourceEntry>();
        }

        public int GetDirectoryCount(string directory)
        {
            var list = GetByDirectory(directory);
            return list.Count;
        }
    }
}
