// -----------------------------------------------------------------------------
// VLTK Mobile — PC mission script source catalog service.
// Data-only wrapper for script/missions file inventory; it does not execute Lua.
// -----------------------------------------------------------------------------

using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VLTK.Core;

namespace VLTK.Sandbox
{
    public sealed class MissionScriptSourceCatalogService
    {
        public const string LogTag = "MissionScriptSourceCatalog";
        public const string DefaultStreamingDir = "Reference/PcMissionScript";
        public const string PcSourceRelativeRoot = PcMissionScriptSourceParser.PcSourceRelativeRoot;

        private readonly PcMissionScriptSourceCatalog _catalog;

        public int Count => _catalog != null ? _catalog.Count : 0;
        public int ActiveLuaCount => _catalog != null ? _catalog.ActiveLuaCount : 0;
        public int NonLuaFileCount => _catalog != null ? _catalog.NonLuaFileCount : 0;
        public int LuaDirectoryCount => _catalog != null ? _catalog.LuaDirectoryCount : 0;
        public int DirectoryCount => _catalog != null ? _catalog.DirectoryCount : 0;
        public IReadOnlyList<PcMissionScriptSourceEntry> All
            => _catalog != null ? _catalog.All : (IReadOnlyList<PcMissionScriptSourceEntry>)System.Array.Empty<PcMissionScriptSourceEntry>();

        public MissionScriptSourceCatalogService(PcMissionScriptSourceCatalog catalog)
        {
            _catalog = catalog ?? new PcMissionScriptSourceCatalog();
        }

        public PcMissionScriptSourceEntry GetByRelativePath(string relativePath) => _catalog.Get(relativePath);
        public IReadOnlyList<PcMissionScriptSourceEntry> GetByDirectory(string directory) => _catalog.GetByDirectory(directory);
        public int GetDirectoryCount(string directory) => _catalog.GetDirectoryCount(directory);
        public int GetActiveLuaDirectoryCount(string directory) => _catalog.GetActiveLuaDirectoryCount(directory);

        public static MissionScriptSourceCatalogService LoadFromStreamingAssets(string subdir = DefaultStreamingDir)
        {
            string dir = Path.Combine(Application.streamingAssetsPath, subdir);
            var catalog = PcMissionScriptSourceParser.BuildCatalog(dir);
            if (catalog.Count > 0)
                SubsystemLog.Info(LogTag, $"Loaded PC mission script source catalog: {catalog.Count} files, {catalog.ActiveLuaCount} active Lua");
            else
                SubsystemLog.Warn(LogTag, $"PC mission script source catalog missing at {dir}");
            return new MissionScriptSourceCatalogService(catalog);
        }
    }

    public sealed class PcMissionScriptSourceCatalog
    {
        private readonly List<PcMissionScriptSourceEntry> _all = new List<PcMissionScriptSourceEntry>();
        private readonly Dictionary<string, PcMissionScriptSourceEntry> _byPath = new Dictionary<string, PcMissionScriptSourceEntry>();
        private readonly Dictionary<string, List<PcMissionScriptSourceEntry>> _byDirectory = new Dictionary<string, List<PcMissionScriptSourceEntry>>();
        private readonly HashSet<string> _luaDirectories = new HashSet<string>();

        public int Count => _all.Count;
        public int ActiveLuaCount { get; private set; }
        public int NonLuaFileCount => Count - ActiveLuaCount;
        public int DirectoryCount => _byDirectory.Count;
        public int LuaDirectoryCount => _luaDirectories.Count;
        public IReadOnlyList<PcMissionScriptSourceEntry> All => _all;

        public void Register(PcMissionScriptSourceEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.relativePath)) return;
            if (_byPath.ContainsKey(entry.relativePath)) return;

            _all.Add(entry);
            _byPath[entry.relativePath] = entry;

            var dir = string.IsNullOrEmpty(entry.directory) ? "." : entry.directory;
            if (!_byDirectory.TryGetValue(dir, out var list))
            {
                list = new List<PcMissionScriptSourceEntry>();
                _byDirectory[dir] = list;
            }
            list.Add(entry);

            if (entry.isActiveLua)
            {
                ActiveLuaCount++;
                _luaDirectories.Add(dir);
            }
        }

        public PcMissionScriptSourceEntry Get(string relativePath)
            => !string.IsNullOrEmpty(relativePath) && _byPath.TryGetValue(relativePath, out var entry) ? entry : null;

        public IReadOnlyList<PcMissionScriptSourceEntry> GetByDirectory(string directory)
        {
            var key = string.IsNullOrEmpty(directory) ? "." : directory;
            return _byDirectory.TryGetValue(key, out var list)
                ? (IReadOnlyList<PcMissionScriptSourceEntry>)list
                : System.Array.Empty<PcMissionScriptSourceEntry>();
        }

        public int GetDirectoryCount(string directory) => GetByDirectory(directory).Count;

        public int GetActiveLuaDirectoryCount(string directory)
        {
            int count = 0;
            foreach (var entry in GetByDirectory(directory))
                if (entry.isActiveLua) count++;
            return count;
        }
    }
}
